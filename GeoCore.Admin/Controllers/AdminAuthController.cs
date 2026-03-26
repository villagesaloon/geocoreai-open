using Microsoft.AspNetCore.Mvc;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// Admin 后台认证控制器
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminRepository _adminRepository;

    public AdminAuthController(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    /// <summary>
    /// 管理员登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Ok(new AdminLoginResponse
            {
                Success = false,
                Error = "用户名和密码不能为空"
            });
        }

        var admin = await _adminRepository.ValidateAndLoginAsync(request.Username, request.Password);

        if (admin == null)
        {
            return Ok(new AdminLoginResponse
            {
                Success = false,
                Error = "用户名或密码错误"
            });
        }

        // 简单的 token 生成（生产环境应使用 JWT）
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        return Ok(new AdminLoginResponse
        {
            Success = true,
            Admin = admin,
            Token = token
        });
    }

    /// <summary>
    /// 获取当前管理员信息
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult> GetCurrentAdmin([FromHeader(Name = "X-Admin-Id")] long? adminId)
    {
        if (adminId == null)
        {
            return Unauthorized(new { error = "未登录" });
        }

        var admin = await _adminRepository.GetByIdAsync(adminId.Value);
        if (admin == null)
        {
            return Unauthorized(new { error = "管理员不存在" });
        }

        return Ok(new { success = true, admin });
    }

    /// <summary>
    /// 初始化超级管理员（仅当没有管理员时可用）
    /// </summary>
    [HttpPost("init")]
    public async Task<ActionResult> InitSuperAdmin([FromBody] AdminLoginRequest request)
    {
        // 检查是否已有管理员
        var admins = await _adminRepository.GetAllAsync();
        if (admins.Any())
        {
            return BadRequest(new { error = "已存在管理员，无法初始化" });
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "用户名和密码不能为空" });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { error = "密码长度至少 6 位" });
        }

        var admin = new AdminDto
        {
            Username = request.Username,
            DisplayName = "超级管理员",
            Role = "superadmin",
            Status = 1
        };

        var id = await _adminRepository.CreateAdminAsync(admin, request.Password);

        return Ok(new { success = true, message = "超级管理员创建成功", adminId = id });
    }
}
