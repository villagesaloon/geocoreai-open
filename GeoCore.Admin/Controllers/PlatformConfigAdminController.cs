using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// 平台配置管理控制器 - Admin 后台专用
/// 管理 AI 爬虫、LLM Referrer、平台偏好、引用基准、Persona 模板等
/// </summary>
[ApiController]
[Route("api/admin/platform-config")]
public class PlatformConfigAdminController : ControllerBase
{
    private readonly GeoDbContext _db;
    private readonly ILogger<PlatformConfigAdminController> _logger;

    public PlatformConfigAdminController(GeoDbContext db, ILogger<PlatformConfigAdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region AI 爬虫 (ai_crawlers)

    /// <summary>
    /// 获取所有 AI 爬虫
    /// </summary>
    [HttpGet("ai-crawlers")]
    public async Task<IActionResult> GetAICrawlers([FromQuery] bool enabledOnly = false, [FromQuery] string? crawlerType = null)
    {
        try
        {
            var query = _db.Client.Queryable<AICrawlerEntity>();
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            if (!string.IsNullOrEmpty(crawlerType))
            {
                query = query.Where(x => x.CrawlerType == crawlerType);
            }
            var list = await query.OrderBy(x => x.SortOrder).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get AI crawlers");
            return StatusCode(500, new { error = "获取 AI 爬虫列表失败" });
        }
    }

    /// <summary>
    /// 创建 AI 爬虫
    /// </summary>
    [HttpPost("ai-crawlers")]
    public async Task<IActionResult> CreateAICrawler([FromBody] AICrawlerEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建 AI 爬虫: {Name}", entity.Name);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create AI crawler");
            return StatusCode(500, new { error = "创建 AI 爬虫失败" });
        }
    }

    /// <summary>
    /// 更新 AI 爬虫
    /// </summary>
    [HttpPut("ai-crawlers/{id}")]
    public async Task<IActionResult> UpdateAICrawler(int id, [FromBody] AICrawlerEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新 AI 爬虫: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update AI crawler {Id}", id);
            return StatusCode(500, new { error = "更新 AI 爬虫失败" });
        }
    }

    /// <summary>
    /// 删除 AI 爬虫
    /// </summary>
    [HttpDelete("ai-crawlers/{id}")]
    public async Task<IActionResult> DeleteAICrawler(int id)
    {
        try
        {
            await _db.Client.Deleteable<AICrawlerEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除 AI 爬虫: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete AI crawler {Id}", id);
            return StatusCode(500, new { error = "删除 AI 爬虫失败" });
        }
    }

    #endregion

    #region LLM Referrer (llm_referrers)

    /// <summary>
    /// 获取所有 LLM Referrer
    /// </summary>
    [HttpGet("llm-referrers")]
    public async Task<IActionResult> GetLLMReferrers([FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<LLMReferrerEntity>();
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.SortOrder).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get LLM referrers");
            return StatusCode(500, new { error = "获取 LLM Referrer 列表失败" });
        }
    }

    /// <summary>
    /// 创建 LLM Referrer
    /// </summary>
    [HttpPost("llm-referrers")]
    public async Task<IActionResult> CreateLLMReferrer([FromBody] LLMReferrerEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建 LLM Referrer: {Platform}", entity.PlatformName);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create LLM referrer");
            return StatusCode(500, new { error = "创建 LLM Referrer 失败" });
        }
    }

    /// <summary>
    /// 更新 LLM Referrer
    /// </summary>
    [HttpPut("llm-referrers/{id}")]
    public async Task<IActionResult> UpdateLLMReferrer(int id, [FromBody] LLMReferrerEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新 LLM Referrer: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update LLM referrer {Id}", id);
            return StatusCode(500, new { error = "更新 LLM Referrer 失败" });
        }
    }

    /// <summary>
    /// 删除 LLM Referrer
    /// </summary>
    [HttpDelete("llm-referrers/{id}")]
    public async Task<IActionResult> DeleteLLMReferrer(int id)
    {
        try
        {
            await _db.Client.Deleteable<LLMReferrerEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除 LLM Referrer: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete LLM referrer {Id}", id);
            return StatusCode(500, new { error = "删除 LLM Referrer 失败" });
        }
    }

    #endregion

    #region LLM 平台偏好 (llm_platform_preferences)

    /// <summary>
    /// 获取所有平台偏好
    /// </summary>
    [HttpGet("platform-preferences")]
    public async Task<IActionResult> GetPlatformPreferences([FromQuery] string? platform = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<LLMPlatformPreferenceEntity>();
            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(x => x.PlatformName == platform);
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.PlatformName).OrderBy(x => x.PreferenceCategory).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get platform preferences");
            return StatusCode(500, new { error = "获取平台偏好失败" });
        }
    }

    /// <summary>
    /// 创建平台偏好
    /// </summary>
    [HttpPost("platform-preferences")]
    public async Task<IActionResult> CreatePlatformPreference([FromBody] LLMPlatformPreferenceEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建平台偏好: {Platform}/{Category}", entity.PlatformName, entity.PreferenceCategory);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create platform preference");
            return StatusCode(500, new { error = "创建平台偏好失败" });
        }
    }

    /// <summary>
    /// 更新平台偏好
    /// </summary>
    [HttpPut("platform-preferences/{id}")]
    public async Task<IActionResult> UpdatePlatformPreference(int id, [FromBody] LLMPlatformPreferenceEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新平台偏好: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update platform preference {Id}", id);
            return StatusCode(500, new { error = "更新平台偏好失败" });
        }
    }

    /// <summary>
    /// 删除平台偏好
    /// </summary>
    [HttpDelete("platform-preferences/{id}")]
    public async Task<IActionResult> DeletePlatformPreference(int id)
    {
        try
        {
            await _db.Client.Deleteable<LLMPlatformPreferenceEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除平台偏好: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete platform preference {Id}", id);
            return StatusCode(500, new { error = "删除平台偏好失败" });
        }
    }

    #endregion

    #region 引用基准 (citation_benchmarks)

    /// <summary>
    /// 获取所有引用基准
    /// </summary>
    [HttpGet("citation-benchmarks")]
    public async Task<IActionResult> GetCitationBenchmarks([FromQuery] string? platform = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<CitationBenchmarkEntity>();
            if (!string.IsNullOrEmpty(platform))
            {
                query = query.Where(x => x.PlatformName == platform);
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.PlatformName).OrderBy(x => x.MetricName).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get citation benchmarks");
            return StatusCode(500, new { error = "获取引用基准失败" });
        }
    }

    /// <summary>
    /// 创建引用基准
    /// </summary>
    [HttpPost("citation-benchmarks")]
    public async Task<IActionResult> CreateCitationBenchmark([FromBody] CitationBenchmarkEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建引用基准: {Platform}/{Metric}", entity.PlatformName, entity.MetricName);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create citation benchmark");
            return StatusCode(500, new { error = "创建引用基准失败" });
        }
    }

    /// <summary>
    /// 更新引用基准
    /// </summary>
    [HttpPut("citation-benchmarks/{id}")]
    public async Task<IActionResult> UpdateCitationBenchmark(int id, [FromBody] CitationBenchmarkEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新引用基准: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update citation benchmark {Id}", id);
            return StatusCode(500, new { error = "更新引用基准失败" });
        }
    }

    /// <summary>
    /// 删除引用基准
    /// </summary>
    [HttpDelete("citation-benchmarks/{id}")]
    public async Task<IActionResult> DeleteCitationBenchmark(int id)
    {
        try
        {
            await _db.Client.Deleteable<CitationBenchmarkEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除引用基准: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete citation benchmark {Id}", id);
            return StatusCode(500, new { error = "删除引用基准失败" });
        }
    }

    #endregion

    #region Persona 模板 (persona_templates)

    /// <summary>
    /// 获取所有 Persona 模板
    /// </summary>
    [HttpGet("persona-templates")]
    public async Task<IActionResult> GetPersonaTemplates([FromQuery] string? industry = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<PersonaTemplateEntity>();
            if (!string.IsNullOrEmpty(industry))
            {
                query = query.Where(x => x.Industry == industry);
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.SortOrder).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get persona templates");
            return StatusCode(500, new { error = "获取 Persona 模板失败" });
        }
    }

    /// <summary>
    /// 创建 Persona 模板
    /// </summary>
    [HttpPost("persona-templates")]
    public async Task<IActionResult> CreatePersonaTemplate([FromBody] PersonaTemplateEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建 Persona 模板: {Name}", entity.Name);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create persona template");
            return StatusCode(500, new { error = "创建 Persona 模板失败" });
        }
    }

    /// <summary>
    /// 更新 Persona 模板
    /// </summary>
    [HttpPut("persona-templates/{id}")]
    public async Task<IActionResult> UpdatePersonaTemplate(int id, [FromBody] PersonaTemplateEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新 Persona 模板: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update persona template {Id}", id);
            return StatusCode(500, new { error = "更新 Persona 模板失败" });
        }
    }

    /// <summary>
    /// 删除 Persona 模板
    /// </summary>
    [HttpDelete("persona-templates/{id}")]
    public async Task<IActionResult> DeletePersonaTemplate(int id)
    {
        try
        {
            await _db.Client.Deleteable<PersonaTemplateEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除 Persona 模板: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete persona template {Id}", id);
            return StatusCode(500, new { error = "删除 Persona 模板失败" });
        }
    }

    #endregion

    #region 7.23 AI 爬虫分类策略

    /// <summary>
    /// 获取 AI 爬虫分类统计
    /// 原理：区分训练爬虫和检索爬虫，制定不同策略
    /// </summary>
    [HttpGet("ai-crawler-classification")]
    public async Task<IActionResult> GetAICrawlerClassification()
    {
        try
        {
            var crawlers = await _db.Client.Queryable<AICrawlerEntity>()
                .Where(x => x.IsEnabled)
                .ToListAsync();

            var classification = new
            {
                success = true,
                data = new
                {
                    totalCrawlers = crawlers.Count,
                    byType = new
                    {
                        training = crawlers.Count(x => x.CrawlerType == "training"),
                        retrieval = crawlers.Count(x => x.CrawlerType == "retrieval"),
                        hybrid = crawlers.Count(x => x.CrawlerType == "hybrid")
                    },
                    byPolicy = new
                    {
                        allow = crawlers.Count(x => x.RecommendedRobotsPolicy == "allow"),
                        disallow = crawlers.Count(x => x.RecommendedRobotsPolicy == "disallow"),
                        conditional = crawlers.Count(x => x.RecommendedRobotsPolicy == "conditional")
                    },
                    trainingCrawlers = crawlers
                        .Where(x => x.CrawlerType == "training")
                        .Select(x => new { x.Name, x.Company, x.RecommendedRobotsPolicy, x.PolicyRationale })
                        .ToList(),
                    retrievalCrawlers = crawlers
                        .Where(x => x.CrawlerType == "retrieval")
                        .Select(x => new { x.Name, x.Company, x.RecommendedRobotsPolicy, x.PolicyRationale })
                        .ToList(),
                    hybridCrawlers = crawlers
                        .Where(x => x.CrawlerType == "hybrid")
                        .Select(x => new { x.Name, x.Company, x.RecommendedRobotsPolicy, x.PolicyRationale })
                        .ToList()
                }
            };

            return Ok(classification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get AI crawler classification");
            return StatusCode(500, new { error = "获取 AI 爬虫分类失败" });
        }
    }

    /// <summary>
    /// 生成 robots.txt 建议
    /// 基于爬虫分类生成针对性的 robots.txt 配置
    /// </summary>
    [HttpGet("ai-crawler-robots-suggestion")]
    public async Task<IActionResult> GetRobotsTxtSuggestion([FromQuery] string strategy = "balanced")
    {
        try
        {
            var crawlers = await _db.Client.Queryable<AICrawlerEntity>()
                .Where(x => x.IsEnabled)
                .ToListAsync();

            var robotsTxtLines = new List<string>();
            robotsTxtLines.Add("# AI Crawler Configuration");
            robotsTxtLines.Add($"# Strategy: {strategy}");
            robotsTxtLines.Add($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            robotsTxtLines.Add("");

            // 根据策略生成配置
            switch (strategy.ToLower())
            {
                case "allow_all":
                    robotsTxtLines.Add("# Allow all AI crawlers");
                    foreach (var crawler in crawlers)
                    {
                        robotsTxtLines.Add($"User-agent: {crawler.Name}");
                        robotsTxtLines.Add("Allow: /");
                        robotsTxtLines.Add("");
                    }
                    break;

                case "block_training":
                    robotsTxtLines.Add("# Block training crawlers, allow retrieval crawlers");
                    foreach (var crawler in crawlers.Where(x => x.CrawlerType == "training"))
                    {
                        robotsTxtLines.Add($"# Training crawler: {crawler.Name} ({crawler.Company})");
                        robotsTxtLines.Add($"User-agent: {crawler.Name}");
                        robotsTxtLines.Add("Disallow: /");
                        robotsTxtLines.Add("");
                    }
                    foreach (var crawler in crawlers.Where(x => x.CrawlerType == "retrieval"))
                    {
                        robotsTxtLines.Add($"# Retrieval crawler: {crawler.Name} ({crawler.Company})");
                        robotsTxtLines.Add($"User-agent: {crawler.Name}");
                        robotsTxtLines.Add("Allow: /");
                        robotsTxtLines.Add("");
                    }
                    break;

                case "balanced":
                default:
                    robotsTxtLines.Add("# Balanced strategy based on individual crawler policies");
                    foreach (var crawler in crawlers.OrderBy(x => x.CrawlerType))
                    {
                        robotsTxtLines.Add($"# {crawler.CrawlerType} crawler: {crawler.Name} ({crawler.Company})");
                        robotsTxtLines.Add($"User-agent: {crawler.Name}");
                        switch (crawler.RecommendedRobotsPolicy)
                        {
                            case "allow":
                                robotsTxtLines.Add("Allow: /");
                                break;
                            case "disallow":
                                robotsTxtLines.Add("Disallow: /");
                                break;
                            case "conditional":
                                robotsTxtLines.Add("Allow: /public/");
                                robotsTxtLines.Add("Allow: /blog/");
                                robotsTxtLines.Add("Disallow: /admin/");
                                robotsTxtLines.Add("Disallow: /api/");
                                break;
                        }
                        if (!string.IsNullOrEmpty(crawler.PolicyRationale))
                        {
                            robotsTxtLines.Add($"# Rationale: {crawler.PolicyRationale}");
                        }
                        robotsTxtLines.Add("");
                    }
                    break;
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    strategy,
                    robotsTxt = string.Join("\n", robotsTxtLines),
                    crawlerCount = crawlers.Count,
                    strategies = new[]
                    {
                        new { name = "allow_all", description = "允许所有 AI 爬虫访问" },
                        new { name = "block_training", description = "阻止训练爬虫，允许检索爬虫" },
                        new { name = "balanced", description = "根据每个爬虫的推荐策略配置" }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to generate robots.txt suggestion");
            return StatusCode(500, new { error = "生成 robots.txt 建议失败" });
        }
    }

    /// <summary>
    /// 批量更新爬虫分类
    /// </summary>
    [HttpPost("ai-crawler-batch-classify")]
    public async Task<IActionResult> BatchClassifyCrawlers([FromBody] BatchClassifyRequest request)
    {
        try
        {
            if (request.CrawlerIds == null || request.CrawlerIds.Count == 0)
            {
                return BadRequest(new { error = "CrawlerIds is required" });
            }

            var updateCount = await _db.Client.Updateable<AICrawlerEntity>()
                .SetColumns(x => new AICrawlerEntity
                {
                    CrawlerType = request.CrawlerType,
                    RecommendedRobotsPolicy = request.RecommendedPolicy,
                    PolicyRationale = request.PolicyRationale,
                    UpdatedAt = DateTime.UtcNow
                })
                .Where(x => request.CrawlerIds.Contains(x.Id))
                .ExecuteCommandAsync();

            _logger.LogInformation("[Admin] 批量更新爬虫分类: {Count} crawlers to {Type}", updateCount, request.CrawlerType);

            return Ok(new { success = true, updatedCount = updateCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to batch classify crawlers");
            return StatusCode(500, new { error = "批量更新爬虫分类失败" });
        }
    }

    /// <summary>
    /// 初始化默认爬虫分类数据
    /// </summary>
    [HttpPost("ai-crawler-init-defaults")]
    public async Task<IActionResult> InitDefaultCrawlerClassifications()
    {
        try
        {
            // 定义默认的爬虫分类
            var defaultClassifications = new Dictionary<string, (string type, string policy, string rationale)>
            {
                // 训练爬虫 - 通常建议阻止
                ["GPTBot"] = ("training", "conditional", "OpenAI 训练爬虫，建议条件允许以保护敏感内容"),
                ["CCBot"] = ("training", "disallow", "Common Crawl 训练爬虫，用于大规模数据收集"),
                ["Google-Extended"] = ("training", "conditional", "Google AI 训练爬虫，建议条件允许"),
                ["anthropic-ai"] = ("training", "conditional", "Anthropic 训练爬虫，建议条件允许"),
                ["cohere-ai"] = ("training", "conditional", "Cohere 训练爬虫，建议条件允许"),
                
                // 检索爬虫 - 通常建议允许
                ["ChatGPT-User"] = ("retrieval", "allow", "ChatGPT 用户检索爬虫，允许以获得 AI 引用"),
                ["PerplexityBot"] = ("retrieval", "allow", "Perplexity 检索爬虫，允许以获得 AI 引用"),
                ["ClaudeBot"] = ("retrieval", "allow", "Claude 检索爬虫，允许以获得 AI 引用"),
                ["Applebot-Extended"] = ("retrieval", "allow", "Apple AI 检索爬虫，允许以获得 Siri 引用"),
                
                // 混合爬虫
                ["Googlebot"] = ("hybrid", "allow", "Google 搜索爬虫，同时用于搜索和 AI 功能"),
                ["bingbot"] = ("hybrid", "allow", "Bing 搜索爬虫，同时用于搜索和 Copilot")
            };

            var updatedCount = 0;
            foreach (var (name, (type, policy, rationale)) in defaultClassifications)
            {
                var crawler = await _db.Client.Queryable<AICrawlerEntity>()
                    .FirstAsync(x => x.Name == name);

                if (crawler != null)
                {
                    crawler.CrawlerType = type;
                    crawler.RecommendedRobotsPolicy = policy;
                    crawler.PolicyRationale = rationale;
                    crawler.UpdatedAt = DateTime.UtcNow;
                    await _db.Client.Updateable(crawler).ExecuteCommandAsync();
                    updatedCount++;
                }
            }

            _logger.LogInformation("[Admin] 初始化爬虫分类: {Count} crawlers updated", updatedCount);

            return Ok(new
            {
                success = true,
                updatedCount,
                message = $"已更新 {updatedCount} 个爬虫的分类信息"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to init default crawler classifications");
            return StatusCode(500, new { error = "初始化爬虫分类失败" });
        }
    }

    #endregion
}

/// <summary>
/// 批量分类请求
/// </summary>
public class BatchClassifyRequest
{
    public List<int> CrawlerIds { get; set; } = new();
    public string CrawlerType { get; set; } = "retrieval";
    public string RecommendedPolicy { get; set; } = "allow";
    public string? PolicyRationale { get; set; }
}
