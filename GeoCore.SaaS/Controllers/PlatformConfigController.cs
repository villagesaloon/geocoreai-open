using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 平台配置控制器 - SaaS 前台只读 API
/// 从缓存读取 AI 爬虫、LLM Referrer、平台偏好、引用基准等数据
/// </summary>
[ApiController]
[Route("api/platform-config")]
public class PlatformConfigController : ControllerBase
{
    private readonly ConfigCacheService _configCache;
    private readonly ILogger<PlatformConfigController> _logger;

    public PlatformConfigController(ConfigCacheService configCache, ILogger<PlatformConfigController> logger)
    {
        _configCache = configCache;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有 AI 爬虫配置
    /// </summary>
    [HttpGet("ai-crawlers")]
    public IActionResult GetAICrawlers()
    {
        var list = _configCache.GetAllAICrawlers();
        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// 获取 robots.txt AI 爬虫配置建议
    /// </summary>
    [HttpGet("ai-crawlers/robots-txt")]
    public IActionResult GetRobotsTxtConfig([FromQuery] bool allowAll = true)
    {
        var crawlers = _configCache.GetAllAICrawlers();
        var robotsTxt = _configCache.GenerateRobotsTxtForAICrawlers(allowAll);
        return Ok(new { 
            success = true, 
            data = new {
                crawlers = crawlers.Select(c => new { c.Name, c.Company, c.RespectsRobotsTxt }),
                robotsTxt
            }
        });
    }

    /// <summary>
    /// 获取所有 LLM Referrer 配置
    /// </summary>
    [HttpGet("llm-referrers")]
    public IActionResult GetLLMReferrers()
    {
        var list = _configCache.GetAllLLMReferrers();
        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// 获取 GA4 AI 流量追踪配置
    /// </summary>
    [HttpGet("llm-referrers/ga4-config")]
    public IActionResult GetGA4Config()
    {
        var referrers = _configCache.GetAllLLMReferrers();
        var platforms = referrers
            .GroupBy(r => r.PlatformName)
            .Select(g => new {
                platform = g.Key,
                patterns = g.Select(r => r.ReferrerPattern).ToList()
            })
            .ToList();

        return Ok(new { 
            success = true, 
            data = new {
                platforms,
                totalPatterns = referrers.Count,
                ga4EventName = "ai_referral",
                ga4DimensionName = "ai_platform"
            }
        });
    }

    /// <summary>
    /// 获取所有平台偏好数据
    /// </summary>
    [HttpGet("platform-preferences")]
    public IActionResult GetPlatformPreferences([FromQuery] string? platform = null)
    {
        var list = string.IsNullOrEmpty(platform) 
            ? _configCache.GetAllPlatformPreferences()
            : _configCache.GetPlatformPreferencesByPlatform(platform);
        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// 获取所有引用基准数据
    /// </summary>
    [HttpGet("citation-benchmarks")]
    public IActionResult GetCitationBenchmarks([FromQuery] string? platform = null)
    {
        var list = string.IsNullOrEmpty(platform)
            ? _configCache.GetAllCitationBenchmarks()
            : _configCache.GetCitationBenchmarksByPlatform(platform);
        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// 获取所有 Persona 模板
    /// </summary>
    [HttpGet("persona-templates")]
    public IActionResult GetPersonaTemplates([FromQuery] string? industry = null)
    {
        var list = string.IsNullOrEmpty(industry)
            ? _configCache.GetAllPersonaTemplates()
            : _configCache.GetPersonaTemplatesByIndustry(industry);
        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// 获取平台配置汇总
    /// </summary>
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        return Ok(new {
            success = true,
            data = new {
                aiCrawlers = _configCache.GetAllAICrawlers().Count,
                llmReferrers = _configCache.GetAllLLMReferrers().Count,
                platformPreferences = _configCache.GetAllPlatformPreferences().Count,
                citationBenchmarks = _configCache.GetAllCitationBenchmarks().Count,
                personaTemplates = _configCache.GetAllPersonaTemplates().Count,
                cacheLoaded = _configCache.IsLoaded
            }
        });
    }
}
