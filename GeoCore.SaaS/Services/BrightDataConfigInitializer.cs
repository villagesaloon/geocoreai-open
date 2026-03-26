using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.SaaS.Services;

/// <summary>
/// BrightData 和 GoogleTrends 配置初始化器
/// 在 Admin 后台可修改这些配置
/// </summary>
public class BrightDataConfigInitializer
{
    private readonly GeoDbContext _db;

    public BrightDataConfigInitializer(GeoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 初始化默认配置（如果不存在则创建）
    /// </summary>
    public async Task InitializeAsync()
    {
        var configs = new List<SystemConfigEntity>
        {
            // BrightData 配置
            new()
            {
                Category = "BrightData",
                ConfigKey = "ApiKey",
                ConfigValue = "d65530bb-5c41-4754-a2c9-327b3b83462c", // 初始值，后续在 Admin 中修改
                Name = "API Key",
                Description = "Bright Data SERP API 密钥（在 Admin 后台配置）",
                ValueType = "string"
            },
            new()
            {
                Category = "BrightData",
                ConfigKey = "Zone",
                ConfigValue = "serp_api1",
                Name = "Zone",
                Description = "Bright Data Zone 名称",
                ValueType = "string"
            },
            new()
            {
                Category = "BrightData",
                ConfigKey = "BaseUrl",
                ConfigValue = "https://api.brightdata.com/request",
                Name = "Base URL",
                Description = "Bright Data API 基础地址",
                ValueType = "string"
            },
            new()
            {
                Category = "BrightData",
                ConfigKey = "TimeoutSeconds",
                ConfigValue = "60",
                Name = "超时时间",
                Description = "API 请求超时时间（秒）",
                ValueType = "int"
            },
            
            // GoogleTrends 配置
            new()
            {
                Category = "GoogleTrends",
                ConfigKey = "CacheExpirationMinutes",
                ConfigValue = "60",
                Name = "缓存时间",
                Description = "趋势数据缓存时间（分钟）",
                ValueType = "int"
            },
            new()
            {
                Category = "GoogleTrends",
                ConfigKey = "DefaultGeo",
                ConfigValue = "CN",
                Name = "默认地区",
                Description = "默认地理位置代码（CN=中国, US=美国）",
                ValueType = "string"
            },
            new()
            {
                Category = "GoogleTrends",
                ConfigKey = "DefaultDateRange",
                ConfigValue = "today 12-m",
                Name = "默认时间范围",
                Description = "默认趋势时间范围（today 12-m = 过去12个月）",
                ValueType = "string"
            }
        };

        foreach (var config in configs)
        {
            var existing = await _db.Client.Queryable<SystemConfigEntity>()
                .Where(x => x.Category == config.Category && x.ConfigKey == config.ConfigKey)
                .FirstAsync();

            if (existing == null)
            {
                config.UpdatedAt = DateTime.UtcNow;
                await _db.Client.Insertable(config).ExecuteCommandAsync();
                Console.WriteLine($"[BrightDataConfig] 创建配置: {config.Category}:{config.ConfigKey}");
            }
        }
    }

    /// <summary>
    /// 设置 API Key（用于首次配置或更新）
    /// </summary>
    public async Task SetApiKeyAsync(string apiKey)
    {
        var existing = await _db.Client.Queryable<SystemConfigEntity>()
            .Where(x => x.Category == "BrightData" && x.ConfigKey == "ApiKey")
            .FirstAsync();

        if (existing != null)
        {
            existing.ConfigValue = apiKey;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(existing).ExecuteCommandAsync();
        }
        else
        {
            await _db.Client.Insertable(new SystemConfigEntity
            {
                Category = "BrightData",
                ConfigKey = "ApiKey",
                ConfigValue = apiKey,
                Name = "API Key",
                Description = "Bright Data SERP API 密钥",
                ValueType = "string",
                UpdatedAt = DateTime.UtcNow
            }).ExecuteCommandAsync();
        }
    }
}
