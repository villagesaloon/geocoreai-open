using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.AICrawlerAudit;
using GeoCore.SaaS.Services.LlmsTxt;
using GeoCore.SaaS.Services.GA4AITracking;
using GeoCore.SaaS.Services.ContentFreshnessAudit;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// AI 可见度监测 API 控制器
/// Phase 4: AI Visibility Monitoring
/// 功能 4.48-4.51
/// </summary>
[ApiController]
[Route("api/ai-visibility")]
public class AIVisibilityController : ControllerBase
{
    private readonly ILogger<AIVisibilityController> _logger;
    private readonly AICrawlerAuditService _crawlerAuditService;
    private readonly LlmsTxtService _llmsTxtService;
    private readonly GA4AITrackingService _ga4TrackingService;
    private readonly ContentFreshnessAuditService _freshnessAuditService;

    public AIVisibilityController(
        ILogger<AIVisibilityController> logger,
        AICrawlerAuditService crawlerAuditService,
        LlmsTxtService llmsTxtService,
        GA4AITrackingService ga4TrackingService,
        ContentFreshnessAuditService freshnessAuditService)
    {
        _logger = logger;
        _crawlerAuditService = crawlerAuditService;
        _llmsTxtService = llmsTxtService;
        _ga4TrackingService = ga4TrackingService;
        _freshnessAuditService = freshnessAuditService;
    }

    #region 4.48 AI 爬虫配置审计

    /// <summary>
    /// 执行 AI 爬虫配置审计
    /// 检测 14+ AI 爬虫的 robots.txt 配置状态
    /// </summary>
    [HttpPost("crawler-audit")]
    public async Task<ActionResult<AICrawlerAuditReport>> AuditCrawlerConfig([FromBody] AICrawlerAuditRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            _logger.LogInformation("[AIVisibility] Starting crawler audit for {Url}", request.WebsiteUrl);
            var report = await _crawlerAuditService.AuditAsync(request);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] Crawler audit failed for {Url}", request.WebsiteUrl);
            return StatusCode(500, new { error = "Crawler audit failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取支持的 AI 爬虫列表
    /// </summary>
    [HttpGet("crawlers")]
    public ActionResult<List<AICrawlerDefinition>> GetSupportedCrawlers()
    {
        var crawlers = _crawlerAuditService.GetSupportedCrawlers();
        return Ok(crawlers);
    }

    #endregion

    #region 4.49 llms.txt 生成器

    /// <summary>
    /// 生成 llms.txt 文件
    /// 从 sitemap 生成，按优先级排序
    /// </summary>
    [HttpPost("llms-txt/generate")]
    public async Task<ActionResult<LlmsTxtGenerateResult>> GenerateLlmsTxt([FromBody] LlmsTxtGenerateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            _logger.LogInformation("[AIVisibility] Generating llms.txt for {Url}", request.WebsiteUrl);
            var result = await _llmsTxtService.GenerateAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] llms.txt generation failed for {Url}", request.WebsiteUrl);
            return StatusCode(500, new { error = "llms.txt generation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 下载生成的 llms.txt 文件
    /// </summary>
    [HttpPost("llms-txt/download")]
    public async Task<IActionResult> DownloadLlmsTxt([FromBody] LlmsTxtGenerateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            var result = await _llmsTxtService.GenerateAsync(request);
            var bytes = System.Text.Encoding.UTF8.GetBytes(result.Content);
            return File(bytes, "text/plain", "llms.txt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] llms.txt download failed for {Url}", request.WebsiteUrl);
            return StatusCode(500, new { error = "llms.txt download failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 7.15 获取所有支持的行业模板
    /// </summary>
    [HttpGet("llms-txt/industry-templates")]
    public ActionResult<List<LlmsTxtIndustryTemplate>> GetIndustryTemplates()
    {
        var templates = _llmsTxtService.GetAllIndustryTemplates();
        return Ok(templates);
    }

    /// <summary>
    /// 7.15 根据行业生成 llms.txt 模板
    /// </summary>
    [HttpPost("llms-txt/industry-template")]
    public ActionResult<LlmsTxtIndustryTemplateResult> GenerateIndustryTemplate([FromBody] LlmsTxtIndustryTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Industry))
            {
                return BadRequest(new { error = "Industry is required" });
            }

            if (string.IsNullOrEmpty(request.CompanyName))
            {
                return BadRequest(new { error = "CompanyName is required" });
            }

            _logger.LogInformation("[AIVisibility] Generating industry template for {Industry}", request.Industry);
            var result = _llmsTxtService.GenerateIndustryTemplate(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] Industry template generation failed for {Industry}", request.Industry);
            return StatusCode(500, new { error = "Industry template generation failed", message = ex.Message });
        }
    }

    #endregion

    #region 4.50 GA4 AI 流量追踪配置

    /// <summary>
    /// 生成 GA4 AI 流量追踪配置
    /// 包括 LLM referrers 配置和 AI bot 识别
    /// </summary>
    [HttpPost("ga4-tracking/config")]
    public ActionResult<GA4AITrackingConfigResult> GenerateGA4Config([FromBody] GA4AITrackingConfigRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            _logger.LogInformation("[AIVisibility] Generating GA4 config for {Url}", request.WebsiteUrl);
            var result = _ga4TrackingService.GenerateConfig(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] GA4 config generation failed for {Url}", request.WebsiteUrl);
            return StatusCode(500, new { error = "GA4 config generation failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取 AI 流量来源定义
    /// </summary>
    [HttpGet("ga4-tracking/sources")]
    public ActionResult<List<AITrafficSource>> GetAITrafficSources()
    {
        var sources = _ga4TrackingService.GetAITrafficSources();
        return Ok(sources);
    }

    /// <summary>
    /// 下载 GA4 追踪代码
    /// </summary>
    [HttpPost("ga4-tracking/download-code")]
    public ActionResult DownloadGA4Code([FromBody] GA4AITrackingConfigRequest request)
    {
        try
        {
            var result = _ga4TrackingService.GenerateConfig(request);
            if (string.IsNullOrEmpty(result.CustomEventCode))
            {
                return BadRequest(new { error = "No custom event code generated" });
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(result.CustomEventCode);
            return File(bytes, "text/html", "ga4-ai-tracking.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] GA4 code download failed");
            return StatusCode(500, new { error = "GA4 code download failed", message = ex.Message });
        }
    }

    #endregion

    #region 4.51 内容新鲜度审计

    /// <summary>
    /// 执行内容新鲜度审计
    /// 60 天未更新页面标记，引用率下降阈值检测
    /// </summary>
    [HttpPost("freshness-audit")]
    public async Task<ActionResult<ContentFreshnessAuditReport>> AuditContentFreshness([FromBody] ContentFreshnessAuditRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            _logger.LogInformation("[AIVisibility] Starting freshness audit for {Url}", request.WebsiteUrl);
            var report = await _freshnessAuditService.AuditAsync(request);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] Freshness audit failed for {Url}", request.WebsiteUrl);
            return StatusCode(500, new { error = "Freshness audit failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取需要更新的页面列表
    /// </summary>
    [HttpPost("freshness-audit/pages-needing-update")]
    public async Task<ActionResult> GetPagesNeedingUpdate([FromBody] ContentFreshnessAuditRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            var report = await _freshnessAuditService.AuditAsync(request);
            var pagesNeedingUpdate = report.Pages
                .Where(p => p.NeedsUpdate)
                .OrderByDescending(p => p.UpdatePriority)
                .Select(p => new
                {
                    p.Url,
                    p.Title,
                    p.PageType,
                    p.AgeDays,
                    p.FreshnessStatus,
                    p.UpdatePriority,
                    p.RecommendedAction,
                    p.CitationImpact
                })
                .ToList();

            return Ok(new
            {
                TotalPages = report.TotalPages,
                PagesNeedingUpdate = pagesNeedingUpdate.Count,
                Pages = pagesNeedingUpdate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] Get pages needing update failed");
            return StatusCode(500, new { error = "Operation failed", message = ex.Message });
        }
    }

    #endregion

    #region 综合报告

    /// <summary>
    /// 生成 AI 可见度综合报告
    /// 包含爬虫配置、llms.txt、GA4 配置和内容新鲜度
    /// </summary>
    [HttpPost("comprehensive-report")]
    public async Task<ActionResult> GenerateComprehensiveReport([FromBody] ComprehensiveReportRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.WebsiteUrl))
            {
                return BadRequest(new { error = "WebsiteUrl is required" });
            }

            _logger.LogInformation("[AIVisibility] Generating comprehensive report for {Url}", request.WebsiteUrl);

            var tasks = new List<Task>();
            AICrawlerAuditReport? crawlerReport = null;
            LlmsTxtGenerateResult? llmsTxtResult = null;
            GA4AITrackingConfigResult? ga4Config = null;
            ContentFreshnessAuditReport? freshnessReport = null;

            // 并行执行各项审计
            if (request.IncludeCrawlerAudit)
            {
                tasks.Add(Task.Run(async () =>
                {
                    crawlerReport = await _crawlerAuditService.AuditAsync(new AICrawlerAuditRequest
                    {
                        WebsiteUrl = request.WebsiteUrl,
                        ProjectId = request.ProjectId
                    });
                }));
            }

            if (request.IncludeLlmsTxt)
            {
                tasks.Add(Task.Run(async () =>
                {
                    llmsTxtResult = await _llmsTxtService.GenerateAsync(new LlmsTxtGenerateRequest
                    {
                        WebsiteUrl = request.WebsiteUrl,
                        ProjectId = request.ProjectId
                    });
                }));
            }

            if (request.IncludeGA4Config)
            {
                ga4Config = _ga4TrackingService.GenerateConfig(new GA4AITrackingConfigRequest
                {
                    WebsiteUrl = request.WebsiteUrl,
                    MeasurementId = request.GA4MeasurementId,
                    ProjectId = request.ProjectId
                });
            }

            if (request.IncludeFreshnessAudit)
            {
                tasks.Add(Task.Run(async () =>
                {
                    freshnessReport = await _freshnessAuditService.AuditAsync(new ContentFreshnessAuditRequest
                    {
                        WebsiteUrl = request.WebsiteUrl,
                        ProjectId = request.ProjectId
                    });
                }));
            }

            await Task.WhenAll(tasks);

            // 计算综合评分
            var scores = new List<int>();
            if (crawlerReport != null) scores.Add(crawlerReport.OverallScore);
            if (freshnessReport != null) scores.Add(freshnessReport.OverallScore);

            var overallScore = scores.Any() ? (int)scores.Average() : 0;

            return Ok(new
            {
                WebsiteUrl = request.WebsiteUrl,
                GeneratedAt = DateTime.UtcNow,
                OverallScore = overallScore,
                ScoreLevel = overallScore >= 80 ? "excellent" : overallScore >= 60 ? "good" : overallScore >= 40 ? "warning" : "critical",
                CrawlerAudit = crawlerReport,
                LlmsTxt = llmsTxtResult,
                GA4Config = ga4Config,
                FreshnessAudit = freshnessReport,
                Summary = new
                {
                    CrawlerScore = crawlerReport?.OverallScore,
                    FreshnessScore = freshnessReport?.OverallScore,
                    LlmsTxtPages = llmsTxtResult?.PageCount,
                    GA4SourcesConfigured = ga4Config?.TrafficSources.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIVisibility] Comprehensive report failed for {Url}", request.WebsiteUrl);
            return StatusCode(500, new { error = "Comprehensive report failed", message = ex.Message });
        }
    }

    #endregion
}

/// <summary>
/// 综合报告请求
/// </summary>
public class ComprehensiveReportRequest
{
    public string WebsiteUrl { get; set; } = "";
    public string? ProjectId { get; set; }
    public string? GA4MeasurementId { get; set; }
    public bool IncludeCrawlerAudit { get; set; } = true;
    public bool IncludeLlmsTxt { get; set; } = true;
    public bool IncludeGA4Config { get; set; } = true;
    public bool IncludeFreshnessAudit { get; set; } = true;
}
