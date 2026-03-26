using Microsoft.AspNetCore.Mvc;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// SaaS 用户管理控制器
/// </summary>
[ApiController]
[Route("api/admin/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取用户列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items, total) = await _userRepository.GetPagedAsync(page, pageSize);
        return Ok(new
        {
            success = true,
            data = items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetUser(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "用户不存在" });
        }
        return Ok(new { success = true, data = user });
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateUserStatus(long id, [FromBody] UpdateStatusRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "用户不存在" });
        }

        user.Status = request.Status;
        var result = await _userRepository.UpdateAsync(user);

        return Ok(new { success = result, message = result ? "状态更新成功" : "更新失败" });
    }

    /// <summary>
    /// 更新用户角色
    /// </summary>
    [HttpPut("{id}/role")]
    public async Task<ActionResult> UpdateUserRole(long id, [FromBody] UpdateRoleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "用户不存在" });
        }

        user.Role = request.Role;
        var result = await _userRepository.UpdateAsync(user);

        return Ok(new { success = result, message = result ? "角色更新成功" : "更新失败" });
    }

    /// <summary>
    /// 获取用户统计
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult> GetUserStats()
    {
        var allUsers = await _userRepository.GetAllAsync();
        
        return Ok(new
        {
            success = true,
            data = new
            {
                totalUsers = allUsers.Count,
                activeUsers = allUsers.Count(u => u.Status == "active"),
                todayNewUsers = allUsers.Count(u => u.CreatedAt.Date == DateTime.UtcNow.Date),
                todayActiveUsers = allUsers.Count(u => u.LastLoginAt?.Date == DateTime.UtcNow.Date)
            }
        });
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "active";
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = "user";
}
