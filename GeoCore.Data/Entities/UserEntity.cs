using System;
using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// 用户表实体 (Storage Adapter 层)
/// </summary>
[SugarTable("users")]
public class UserEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "firebase_uid", Length = 128, IsNullable = false)]
    public string FirebaseUid { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "email", Length = 255, IsNullable = false)]
    public string Email { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "display_name", Length = 100, IsNullable = true)]
    public string? DisplayName { get; set; }

    [SugarColumn(ColumnName = "photo_url", Length = 500, IsNullable = true)]
    public string? PhotoUrl { get; set; }

    [SugarColumn(ColumnName = "provider", Length = 50, IsNullable = false)]
    public string Provider { get; set; } = "google.com";

    [SugarColumn(ColumnName = "company", Length = 200, IsNullable = true)]
    public string? Company { get; set; }

    [SugarColumn(ColumnName = "role", Length = 20, IsNullable = false)]
    public string Role { get; set; } = "user";

    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "active";

    [SugarColumn(ColumnName = "last_login_at", IsNullable = true)]
    public DateTime? LastLoginAt { get; set; }

    [SugarColumn(ColumnName = "login_count", IsNullable = false)]
    public int LoginCount { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}
