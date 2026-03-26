using System.Collections.Concurrent;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Services;

/// <summary>
/// 配置缓存服务 - 启动时从数据库加载，运行时从内存读取
/// 支持 Admin 后台通过 API 刷新缓存
/// </summary>
public class ConfigCacheService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConfigCacheService> _logger;

    // 大模型配置缓存：key = modelId (小写)
    private readonly ConcurrentDictionary<string, ModelConfigEntity> _modelConfigs = new();

    // 全局参数缓存：key = "category:configKey"
    private readonly ConcurrentDictionary<string, SystemConfigEntity> _systemConfigs = new();

    // 平台配置缓存（Phase 7 前后台分离）
    private readonly ConcurrentDictionary<int, AICrawlerEntity> _aiCrawlers = new();
    private readonly ConcurrentDictionary<int, LLMReferrerEntity> _llmReferrers = new();
    private readonly ConcurrentDictionary<int, LLMPlatformPreferenceEntity> _platformPreferences = new();
    private readonly ConcurrentDictionary<int, CitationBenchmarkEntity> _citationBenchmarks = new();
    private readonly ConcurrentDictionary<int, PersonaTemplateEntity> _personaTemplates = new();

    private volatile bool _isLoaded = false;

    public ConfigCacheService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConfigCacheService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 启动时从数据库加载所有配置到缓存
    /// </summary>
    public async Task LoadAllAsync()
    {
        await LoadModelConfigsAsync();
        await LoadSystemConfigsAsync();
        await LoadPlatformConfigsAsync();
        _isLoaded = true;
        _logger.LogInformation("[ConfigCache] 缓存加载完成: 模型配置 {ModelCount} 个, 系统参数 {SysCount} 个, AI爬虫 {CrawlerCount} 个, LLM Referrer {ReferrerCount} 个",
            _modelConfigs.Count, _systemConfigs.Count, _aiCrawlers.Count, _llmReferrers.Count);
    }

    #region 大模型配置

    /// <summary>
    /// 从数据库重新加载所有模型配置
    /// </summary>
    public async Task LoadModelConfigsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ModelConfigRepository>();
        var models = await repo.GetAllEnabledAsync();
        _modelConfigs.Clear();
        foreach (var m in models)
        {
            _modelConfigs[m.ModelId.ToLower()] = m;
        }
        _logger.LogInformation("[ConfigCache] 模型配置已加载: {Count} 个", _modelConfigs.Count);
    }

    /// <summary>
    /// 获取模型配置（从缓存）
    /// </summary>
    public (string endpoint, string apiKey, string model) GetModelConfig(string modelId)
    {
        var key = modelId.ToLower();
        if (_modelConfigs.TryGetValue(key, out var config))
        {
            return (config.ApiEndpoint, config.ApiKey, config.ModelName);
        }
        _logger.LogWarning("[ConfigCache] 模型配置未找到: {ModelId}", modelId);
        return ("", "", "");
    }

    /// <summary>
    /// 获取模型的完整配置实体（从缓存）
    /// </summary>
    public ModelConfigEntity? GetModelConfigEntity(string modelId)
    {
        var key = modelId.ToLower();
        _modelConfigs.TryGetValue(key, out var config);
        return config;
    }

    /// <summary>
    /// 获取所有已缓存的模型配置
    /// </summary>
    public List<ModelConfigEntity> GetAllModelConfigs()
    {
        return _modelConfigs.Values.OrderBy(x => x.SortOrder).ToList();
    }

    /// <summary>
    /// 更新单个模型配置的缓存（Admin 修改后调用）
    /// </summary>
    public void UpdateModelConfigCache(ModelConfigEntity entity)
    {
        if (entity.IsEnabled)
        {
            _modelConfigs[entity.ModelId.ToLower()] = entity;
        }
        else
        {
            _modelConfigs.TryRemove(entity.ModelId.ToLower(), out _);
        }
        _logger.LogInformation("[ConfigCache] 模型配置缓存已更新: {ModelId} (enabled={Enabled})", entity.ModelId, entity.IsEnabled);
    }

    /// <summary>
    /// 从缓存中移除模型配置（Admin 删除后调用）
    /// </summary>
    public void RemoveModelConfigCache(string modelId)
    {
        _modelConfigs.TryRemove(modelId.ToLower(), out _);
        _logger.LogInformation("[ConfigCache] 模型配置缓存已移除: {ModelId}", modelId);
    }

    #endregion

    #region 全局参数（system_configs）

    /// <summary>
    /// 从数据库重新加载所有系统参数
    /// </summary>
    public async Task LoadSystemConfigsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<SystemConfigRepository>();
        var configs = await repo.GetAllAsync();
        _systemConfigs.Clear();
        foreach (var c in configs)
        {
            var cacheKey = $"{c.Category}:{c.ConfigKey}";
            _systemConfigs[cacheKey] = c;
        }
        _logger.LogInformation("[ConfigCache] 系统参数已加载: {Count} 个", _systemConfigs.Count);
    }

    /// <summary>
    /// 获取系统参数值（从缓存）
    /// </summary>
    public string? GetSystemValue(string category, string configKey)
    {
        var cacheKey = $"{category}:{configKey}";
        if (_systemConfigs.TryGetValue(cacheKey, out var config))
        {
            return config.ConfigValue;
        }
        return null;
    }

    /// <summary>
    /// 获取系统参数整数值（从缓存）
    /// </summary>
    public int GetSystemIntValue(string category, string configKey, int defaultValue = 0)
    {
        var value = GetSystemValue(category, configKey);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取指定分类下所有系统参数
    /// </summary>
    public List<SystemConfigEntity> GetSystemConfigsByCategory(string category)
    {
        return _systemConfigs.Values
            .Where(x => x.Category == category)
            .ToList();
    }

    /// <summary>
    /// 获取所有系统参数
    /// </summary>
    public List<SystemConfigEntity> GetAllSystemConfigs()
    {
        return _systemConfigs.Values.OrderBy(x => x.Category).ThenBy(x => x.ConfigKey).ToList();
    }

    /// <summary>
    /// 更新单个系统参数的缓存（Admin 修改后调用）
    /// </summary>
    public void UpdateSystemConfigCache(SystemConfigEntity entity)
    {
        var cacheKey = $"{entity.Category}:{entity.ConfigKey}";
        _systemConfigs[cacheKey] = entity;
        _logger.LogInformation("[ConfigCache] 系统参数缓存已更新: {Key}", cacheKey);
    }

    /// <summary>
    /// 从缓存中移除系统参数（Admin 删除后调用）
    /// </summary>
    public void RemoveSystemConfigCache(string category, string configKey)
    {
        var cacheKey = $"{category}:{configKey}";
        _systemConfigs.TryRemove(cacheKey, out _);
        _logger.LogInformation("[ConfigCache] 系统参数缓存已移除: {Key}", cacheKey);
    }

    #endregion

    #region 平台配置缓存（Phase 7 前后台分离）

    /// <summary>
    /// 从数据库加载所有平台配置到缓存
    /// </summary>
    public async Task LoadPlatformConfigsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GeoDbContext>();

        // 加载 AI 爬虫
        var crawlers = await db.Client.Queryable<AICrawlerEntity>().Where(x => x.IsEnabled).ToListAsync();
        _aiCrawlers.Clear();
        foreach (var c in crawlers) _aiCrawlers[c.Id] = c;

        // 加载 LLM Referrer
        var referrers = await db.Client.Queryable<LLMReferrerEntity>().Where(x => x.IsEnabled).ToListAsync();
        _llmReferrers.Clear();
        foreach (var r in referrers) _llmReferrers[r.Id] = r;

        // 加载平台偏好
        var preferences = await db.Client.Queryable<LLMPlatformPreferenceEntity>().Where(x => x.IsEnabled).ToListAsync();
        _platformPreferences.Clear();
        foreach (var p in preferences) _platformPreferences[p.Id] = p;

        // 加载引用基准
        var benchmarks = await db.Client.Queryable<CitationBenchmarkEntity>().Where(x => x.IsEnabled).ToListAsync();
        _citationBenchmarks.Clear();
        foreach (var b in benchmarks) _citationBenchmarks[b.Id] = b;

        // 加载 Persona 模板
        var personas = await db.Client.Queryable<PersonaTemplateEntity>().Where(x => x.IsEnabled).ToListAsync();
        _personaTemplates.Clear();
        foreach (var p in personas) _personaTemplates[p.Id] = p;

        _logger.LogInformation("[ConfigCache] 平台配置已加载: AI爬虫 {Crawlers}, LLM Referrer {Referrers}, 平台偏好 {Prefs}, 引用基准 {Benchmarks}, Persona模板 {Personas}",
            _aiCrawlers.Count, _llmReferrers.Count, _platformPreferences.Count, _citationBenchmarks.Count, _personaTemplates.Count);
    }

    /// <summary>
    /// 获取所有 AI 爬虫（从缓存）
    /// </summary>
    public List<AICrawlerEntity> GetAllAICrawlers()
    {
        return _aiCrawlers.Values.OrderBy(x => x.SortOrder).ToList();
    }

    /// <summary>
    /// 获取所有 LLM Referrer（从缓存）
    /// </summary>
    public List<LLMReferrerEntity> GetAllLLMReferrers()
    {
        return _llmReferrers.Values.OrderBy(x => x.SortOrder).ToList();
    }

    /// <summary>
    /// 按平台名称获取 LLM Referrer
    /// </summary>
    public List<LLMReferrerEntity> GetLLMReferrersByPlatform(string platformName)
    {
        return _llmReferrers.Values
            .Where(x => x.PlatformName.Equals(platformName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ToList();
    }

    /// <summary>
    /// 获取所有平台偏好（从缓存）
    /// </summary>
    public List<LLMPlatformPreferenceEntity> GetAllPlatformPreferences()
    {
        return _platformPreferences.Values.OrderBy(x => x.PlatformName).ThenBy(x => x.PreferenceCategory).ToList();
    }

    /// <summary>
    /// 按平台名称获取平台偏好
    /// </summary>
    public List<LLMPlatformPreferenceEntity> GetPlatformPreferencesByPlatform(string platformName)
    {
        return _platformPreferences.Values
            .Where(x => x.PlatformName.Equals(platformName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.PreferenceCategory)
            .ToList();
    }

    /// <summary>
    /// 获取所有引用基准（从缓存）
    /// </summary>
    public List<CitationBenchmarkEntity> GetAllCitationBenchmarks()
    {
        return _citationBenchmarks.Values.OrderBy(x => x.PlatformName).ThenBy(x => x.MetricName).ToList();
    }

    /// <summary>
    /// 按平台名称获取引用基准
    /// </summary>
    public List<CitationBenchmarkEntity> GetCitationBenchmarksByPlatform(string platformName)
    {
        return _citationBenchmarks.Values
            .Where(x => x.PlatformName.Equals(platformName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.MetricName)
            .ToList();
    }

    /// <summary>
    /// 获取所有 Persona 模板（从缓存）
    /// </summary>
    public List<PersonaTemplateEntity> GetAllPersonaTemplates()
    {
        return _personaTemplates.Values.OrderBy(x => x.SortOrder).ToList();
    }

    /// <summary>
    /// 按行业获取 Persona 模板
    /// </summary>
    public List<PersonaTemplateEntity> GetPersonaTemplatesByIndustry(string industry)
    {
        return _personaTemplates.Values
            .Where(x => x.Industry != null && x.Industry.Equals(industry, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ToList();
    }

    /// <summary>
    /// 生成 robots.txt AI 爬虫配置
    /// </summary>
    public string GenerateRobotsTxtForAICrawlers(bool allowAll = true)
    {
        var lines = new List<string>();
        foreach (var crawler in GetAllAICrawlers())
        {
            lines.Add($"User-agent: {crawler.Name}");
            lines.Add(allowAll ? "Allow: /" : "Disallow: /");
            lines.Add("");
        }
        return string.Join("\n", lines);
    }

    #endregion

    /// <summary>
    /// 缓存是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;
}
