namespace GeoCore.Shared.Models;

/// <summary>
/// 管理员 DTO
/// </summary>
public class AdminDto
{
    /// <summary>
    /// 管理员 ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户名（登录用）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱（可选）
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 角色：superadmin / admin / operator
    /// </summary>
    public string Role { get; set; } = "operator";

    /// <summary>
    /// 状态：1=启用, 0=禁用
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 登录次数
    /// </summary>
    public int LoginCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 管理员登录请求
/// </summary>
public class AdminLoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 管理员登录响应
/// </summary>
public class AdminLoginResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public AdminDto? Admin { get; set; }
    public string? Token { get; set; }
}
