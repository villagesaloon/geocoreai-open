using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 用户认证 API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public AuthController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 用户登录或注册
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.FirebaseUid) || string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { success = false, error = "缺少必要参数" });
        }

        try
        {
            var user = new UserDto
            {
                FirebaseUid = request.FirebaseUid,
                Email = request.Email,
                DisplayName = request.DisplayName,
                PhotoUrl = request.PhotoUrl,
                Provider = request.Provider ?? "google.com"
            };

            var result = await _userRepository.LoginOrRegisterAsync(user);

            return Ok(new
            {
                success = true,
                user = new
                {
                    id = result.Id,
                    email = result.Email,
                    displayName = result.DisplayName,
                    photoUrl = result.PhotoUrl,
                    role = result.Role,
                    loginCount = result.LoginCount,
                    createdAt = result.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "服务器错误: " + ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser([FromQuery] string firebaseUid)
    {
        if (string.IsNullOrEmpty(firebaseUid))
        {
            return BadRequest(new { success = false, error = "缺少 firebaseUid" });
        }

        var user = await _userRepository.GetByFirebaseUidAsync(firebaseUid);

        if (user == null)
        {
            return NotFound(new { success = false, error = "用户不存在" });
        }

        return Ok(new
        {
            success = true,
            user = new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                photoUrl = user.PhotoUrl,
                company = user.Company,
                role = user.Role,
                status = user.Status,
                loginCount = user.LoginCount,
                lastLoginAt = user.LastLoginAt,
                createdAt = user.CreatedAt
            }
        });
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (string.IsNullOrEmpty(request.FirebaseUid))
        {
            return BadRequest(new { success = false, error = "缺少 firebaseUid" });
        }

        var user = await _userRepository.GetByFirebaseUidAsync(request.FirebaseUid);

        if (user == null)
        {
            return NotFound(new { success = false, error = "用户不存在" });
        }

        // 更新允许修改的字段
        if (!string.IsNullOrEmpty(request.DisplayName))
            user.DisplayName = request.DisplayName;
        if (!string.IsNullOrEmpty(request.Company))
            user.Company = request.Company;

        await _userRepository.UpdateAsync(user);

        return Ok(new { success = true, message = "更新成功" });
    }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    public string FirebaseUid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Provider { get; set; }
}

/// <summary>
/// 更新用户资料请求
/// </summary>
public class UpdateProfileRequest
{
    public string FirebaseUid { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Company { get; set; }
}
