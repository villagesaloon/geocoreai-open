using GeoCore.Data.Entities;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 系统配置仓储接口
/// </summary>
public interface ISysConfigRepository
{
    Task<string?> GetValueAsync(string group, string key);
    Task<T?> GetValueAsync<T>(string group, string key);
    Task<List<SysConfigEntity>> GetByGroupAsync(string group);
    Task<SysConfigEntity?> GetConfigAsync(string group, string key);
    Task SetValueAsync(string group, string key, string? value);
    Task<SysConfigEntity> CreateOrUpdateAsync(SysConfigEntity config);
    Task<List<SysConfigEntity>> GetAllAsync();
}

/// <summary>
/// 系统配置仓储实现
/// </summary>
public class SysConfigRepository : ISysConfigRepository
{
    private readonly ISqlSugarClient _db;

    public SysConfigRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<string?> GetValueAsync(string group, string key)
    {
        var config = await _db.Queryable<SysConfigEntity>()
            .Where(c => c.ConfigGroup == group && c.ConfigKey == key && c.IsEnabled)
            .FirstAsync();
        return config?.ConfigValue;
    }

    public async Task<T?> GetValueAsync<T>(string group, string key)
    {
        var value = await GetValueAsync(group, key);
        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);
            if (typeof(T) == typeof(decimal))
                return (T)(object)decimal.Parse(value);

            return System.Text.Json.JsonSerializer.Deserialize<T>(value);
        }
        catch
        {
            return default;
        }
    }

    public async Task<List<SysConfigEntity>> GetByGroupAsync(string group)
    {
        return await _db.Queryable<SysConfigEntity>()
            .Where(c => c.ConfigGroup == group)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }

    public async Task<SysConfigEntity?> GetConfigAsync(string group, string key)
    {
        return await _db.Queryable<SysConfigEntity>()
            .Where(c => c.ConfigGroup == group && c.ConfigKey == key)
            .FirstAsync();
    }

    public async Task SetValueAsync(string group, string key, string? value)
    {
        var config = await GetConfigAsync(group, key);
        if (config != null)
        {
            config.ConfigValue = value;
            config.UpdatedAt = DateTime.UtcNow;
            await _db.Updateable(config).ExecuteCommandAsync();
        }
    }

    public async Task<SysConfigEntity> CreateOrUpdateAsync(SysConfigEntity config)
    {
        var existing = await GetConfigAsync(config.ConfigGroup, config.ConfigKey);
        if (existing != null)
        {
            config.Id = existing.Id;
            config.UpdatedAt = DateTime.UtcNow;
            await _db.Updateable(config).ExecuteCommandAsync();
        }
        else
        {
            config.CreatedAt = DateTime.UtcNow;
            config.Id = await _db.Insertable(config).ExecuteReturnIdentityAsync();
        }
        return config;
    }

    public async Task<List<SysConfigEntity>> GetAllAsync()
    {
        return await _db.Queryable<SysConfigEntity>()
            .OrderBy(c => c.ConfigGroup)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();
    }
}

/// <summary>
/// 系统配置初始化器
/// </summary>
public class SysConfigInitializer
{
    private readonly GeoCore.Data.DbContext.GeoDbContext _db;

    public SysConfigInitializer(GeoCore.Data.DbContext.GeoDbContext db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        var repo = new SysConfigRepository(_db.Client);
        var existing = await repo.GetAllAsync();

        if (existing.Any(c => c.ConfigGroup == "resend"))
        {
            return; // 已初始化
        }

        // Resend 邮件服务配置
        var resendConfigs = new List<SysConfigEntity>
        {
            new()
            {
                ConfigGroup = "resend",
                ConfigKey = "api_key",
                ConfigValue = "re_hWT1vEKG_LjfMqWmce2GUCbqmWWnJcsjg",
                Name = "Resend API Key",
                Description = "Resend 邮件服务的 API 密钥",
                ValueType = "string",
                IsSensitive = true,
                SortOrder = 1
            },
            new()
            {
                ConfigGroup = "resend",
                ConfigKey = "from_email",
                ConfigValue = "noreply@geocoreai.com",
                Name = "发件人邮箱",
                Description = "邮件发送时使用的发件人邮箱地址",
                ValueType = "string",
                IsSensitive = false,
                SortOrder = 2
            },
            new()
            {
                ConfigGroup = "resend",
                ConfigKey = "from_name",
                ConfigValue = "GeoCore AI",
                Name = "发件人名称",
                Description = "邮件发送时显示的发件人名称",
                ValueType = "string",
                IsSensitive = false,
                SortOrder = 3
            }
        };

        foreach (var config in resendConfigs)
        {
            await repo.CreateOrUpdateAsync(config);
        }
    }
}
