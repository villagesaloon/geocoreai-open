using System;

namespace GeoCore.Shared.Models;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserDto : BaseDto
{
    /// <summary>
    /// Firebase UID (来自 Google 登录)
    /// </summary>
    public string FirebaseUid { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 头像 URL
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// 认证提供商 (google.com, password, github.com 等)
    /// </summary>
    public string Provider { get; set; } = "google.com";

    /// <summary>
    /// 公司名称
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// 用户角色 (user, admin)
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// 账户状态 (active, suspended, deleted)
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 登录次数
    /// </summary>
    public int LoginCount { get; set; } = 0;
}
