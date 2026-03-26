using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 缓存管理控制器 - 供 Admin 后台调用刷新缓存
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ConfigCacheService _configCache;
    private readonly ILogger<CacheController> _logger;

    public CacheController(ConfigCacheService configCache, ILogger<CacheController> logger)
    {
        _configCache = configCache;
        _logger = logger;
    }

    /// <summary>
    /// 刷新所有配置缓存（Admin 修改配置后调用）
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAll([FromHeader(Name = "X-Internal-Call")] string? internalCall)
    {
        if (internalCall != "admin")
        {
            return Unauthorized(new { success = false, message = "仅限内部调用" });
        }

        await _configCache.LoadAllAsync();
        _logger.LogInformation("[Cache] 所有配置缓存已刷新（由 Admin 触发）");
        return Ok(new { success = true, message = "缓存已刷新" });
    }

    /// <summary>
    /// 仅刷新模型配置缓存
    /// </summary>
    [HttpPost("refresh/models")]
    public async Task<IActionResult> RefreshModels([FromHeader(Name = "X-Internal-Call")] string? internalCall)
    {
        if (internalCall != "admin")
        {
            return Unauthorized(new { success = false, message = "仅限内部调用" });
        }

        await _configCache.LoadModelConfigsAsync();
        _logger.LogInformation("[Cache] 模型配置缓存已刷新（由 Admin 触发）");
        return Ok(new { success = true, message = "模型配置缓存已刷新" });
    }

    /// <summary>
    /// 仅刷新系统参数缓存
    /// </summary>
    [HttpPost("refresh/system")]
    public async Task<IActionResult> RefreshSystem([FromHeader(Name = "X-Internal-Call")] string? internalCall)
    {
        if (internalCall != "admin")
        {
            return Unauthorized(new { success = false, message = "仅限内部调用" });
        }

        await _configCache.LoadSystemConfigsAsync();
        _logger.LogInformation("[Cache] 系统参数缓存已刷新（由 Admin 触发）");
        return Ok(new { success = true, message = "系统参数缓存已刷新" });
    }

    /// <summary>
    /// 仅刷新平台配置缓存（AI爬虫、LLM Referrer、平台偏好等）
    /// </summary>
    [HttpPost("refresh/platform")]
    public async Task<IActionResult> RefreshPlatform([FromHeader(Name = "X-Internal-Call")] string? internalCall)
    {
        if (internalCall != "admin")
        {
            return Unauthorized(new { success = false, message = "仅限内部调用" });
        }

        await _configCache.LoadPlatformConfigsAsync();
        _logger.LogInformation("[Cache] 平台配置缓存已刷新（由 Admin 触发）");
        return Ok(new { success = true, message = "平台配置缓存已刷新" });
    }

    /// <summary>
    /// 查看当前缓存状态（调试用）
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var models = _configCache.GetAllModelConfigs();
        var systemConfigs = _configCache.GetAllSystemConfigs();
        return Ok(new
        {
            success = true,
            isLoaded = _configCache.IsLoaded,
            modelConfigs = models.Select(m => new { m.ModelId, m.DisplayName, m.ModelName, m.IsEnabled, m.SortOrder }),
            systemConfigCount = systemConfigs.Count,
            systemCategories = systemConfigs.GroupBy(c => c.Category).Select(g => new { category = g.Key, count = g.Count() }),
            platformConfigs = new
            {
                aiCrawlers = _configCache.GetAllAICrawlers().Count,
                llmReferrers = _configCache.GetAllLLMReferrers().Count,
                platformPreferences = _configCache.GetAllPlatformPreferences().Count,
                citationBenchmarks = _configCache.GetAllCitationBenchmarks().Count,
                personaTemplates = _configCache.GetAllPersonaTemplates().Count
            }
        });
    }

    /// <summary>
    /// 从 config.json 初始化模型配置到数据库（仅当数据库为空时）
    /// </summary>
    [HttpPost("init-models")]
    public async Task<IActionResult> InitModelsFromConfigJson(
        [FromServices] GeoCore.Data.DbContext.GeoDbContext dbContext)
    {
        var initializer = new GeoCore.Data.Repositories.ModelConfigInitializer(dbContext);
        await initializer.InitializeFromConfigJsonAsync();
        await _configCache.LoadModelConfigsAsync();
        
        var models = _configCache.GetAllModelConfigs();
        _logger.LogInformation("[Cache] 模型配置初始化完成，共 {Count} 个", models.Count);
        
        return Ok(new 
        { 
            success = true, 
            message = $"初始化完成，共 {models.Count} 个模型配置",
            models = models.Select(m => new { m.ModelId, m.DisplayName, m.IsEnabled })
        });
    }
}
