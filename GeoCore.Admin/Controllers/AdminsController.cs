using Microsoft.AspNetCore.Mvc;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// 管理员管理控制器
/// </summary>
[ApiController]
[Route("api/admin/admins")]
public class AdminsController : ControllerBase
{
    private readonly IAdminRepository _adminRepository;

    public AdminsController(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    /// <summary>
    /// 获取管理员列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAdmins()
    {
        var admins = await _adminRepository.GetAllAsync();
        return Ok(new { success = true, data = admins });
    }

    /// <summary>
    /// 获取管理员详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetAdmin(long id)
    {
        var admin = await _adminRepository.GetByIdAsync(id);
        if (admin == null)
        {
            return NotFound(new { error = "管理员不存在" });
        }
        return Ok(new { success = true, data = admin });
    }

    /// <summary>
    /// 添加管理员
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateAdmin([FromBody] CreateAdminRequest request)
    {
        // 检查用户名是否已存在
        var existing = await _adminRepository.GetByUsernameAsync(request.Username);
        if (existing != null)
        {
            return BadRequest(new { success = false, error = "用户名已存在" });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return BadRequest(new { success = false, error = "密码长度至少 6 位" });
        }

        var admin = new AdminDto
        {
            Username = request.Username,
            DisplayName = request.DisplayName ?? request.Username,
            Email = request.Email,
            Role = request.Role ?? "operator",
            Status = 1
        };

        var id = await _adminRepository.CreateAdminAsync(admin, request.Password);

        return Ok(new { success = true, adminId = id });
    }

    /// <summary>
    /// 更新管理员状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateAdminStatus(long id, [FromBody] UpdateAdminStatusRequest request)
    {
        var admin = await _adminRepository.GetByIdAsync(id);
        if (admin == null)
        {
            return NotFound(new { error = "管理员不存在" });
        }

        // 不能禁用超级管理员
        if (admin.Role == "superadmin" && request.Status == 0)
        {
            return BadRequest(new { success = false, error = "不能禁用超级管理员" });
        }

        admin.Status = request.Status;
        var result = await _adminRepository.UpdateAsync(admin);

        return Ok(new { success = result, message = result ? "状态更新成功" : "更新失败" });
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    [HttpPut("{id}/password")]
    public async Task<ActionResult> ChangePassword(long id, [FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest(new { success = false, error = "密码长度至少 6 位" });
        }

        var result = await _adminRepository.ChangePasswordAsync(id, request.NewPassword);
        return Ok(new { success = result, message = result ? "密码修改成功" : "修改失败" });
    }
}

public class CreateAdminRequest
{
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class UpdateAdminStatusRequest
{
    public int Status { get; set; }
}

public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
