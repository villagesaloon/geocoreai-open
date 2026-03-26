using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// 管理员表实体 (Storage Adapter 层)
/// </summary>
[SugarTable("admins")]
public class AdminEntity
{
    /// <summary>
    /// 管理员 ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户名（登录用）
    /// </summary>
    [SugarColumn(ColumnName = "username", Length = 50, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希（BCrypt）
    /// </summary>
    [SugarColumn(ColumnName = "password_hash", Length = 255, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [SugarColumn(ColumnName = "display_name", Length = 100, IsNullable = false)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱（可选）
    /// </summary>
    [SugarColumn(ColumnName = "email", Length = 255, IsNullable = true)]
    public string? Email { get; set; }

    /// <summary>
    /// 角色：superadmin / admin / operator
    /// </summary>
    [SugarColumn(ColumnName = "role", Length = 20, IsNullable = false)]
    public string Role { get; set; } = "operator";

    /// <summary>
    /// 状态：1=启用, 0=禁用
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false)]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    [SugarColumn(ColumnName = "last_login_at", IsNullable = true)]
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 登录次数
    /// </summary>
    [SugarColumn(ColumnName = "login_count", IsNullable = false)]
    public int LoginCount { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}
