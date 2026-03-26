using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.AICrawler;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// AI 爬虫 API (4.48-4.53, 4.55-4.58)
/// </summary>
[ApiController]
[Route("api/ai-crawler")]
public class AICrawlerController : ControllerBase
{
    private readonly AICrawlerService _service;
    private readonly ILogger<AICrawlerController> _logger;

    public AICrawlerController(AICrawlerService service, ILogger<AICrawlerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 审计 AI 爬虫配置 (4.48)
    /// </summary>
    [HttpPost("audit")]
    public ActionResult<AICrawlerAuditResult> AuditCrawlerConfig([FromBody] AICrawlerAuditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SiteUrl))
        {
            return BadRequest(new { error = "网站 URL 不能为空" });
        }

        var result = _service.AuditCrawlerConfig(request);
        return Ok(result);
    }

    /// <summary>
    /// 生成 llms.txt (4.49)
    /// </summary>
    [HttpPost("llms-txt/generate")]
    public ActionResult<LlmsTxtResult> GenerateLlmsTxt([FromBody] LlmsTxtRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SiteName))
        {
            return BadRequest(new { error = "网站名称不能为空" });
        }

        var result = _service.GenerateLlmsTxt(request);
        return Ok(result);
    }

    /// <summary>
    /// 获取 GA4 AI 流量追踪配置 (4.50)
    /// </summary>
    [HttpGet("ga4-tracking")]
    public ActionResult<GA4AITrackingConfig> GetGA4TrackingConfig()
    {
        return Ok(_service.GetGA4TrackingConfig());
    }

    /// <summary>
    /// 分析双平台优化 (4.52)
    /// </summary>
    [HttpPost("dual-platform")]
    public ActionResult<DualPlatformResult> AnalyzeDualPlatform([FromBody] DualPlatformRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "内容不能为空" });
        }

        var result = _service.AnalyzeDualPlatform(request);
        return Ok(result);
    }

    /// <summary>
    /// 检测 JS 渲染依赖 (4.53)
    /// </summary>
    [HttpPost("js-rendering")]
    public ActionResult<JSRenderingResult> DetectJSRendering([FromBody] JSRenderingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url) && string.IsNullOrWhiteSpace(request.HtmlContent))
        {
            return BadRequest(new { error = "URL 或 HTML 内容不能为空" });
        }

        var result = _service.DetectJSRendering(request);
        return Ok(result);
    }

    /// <summary>
    /// 分析竞品引用 (4.55-4.58)
    /// </summary>
    [HttpPost("competitor-analysis")]
    public ActionResult<CompetitorAnalysisResult> AnalyzeCompetitors([FromBody] CompetitorAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new { error = "主题不能为空" });
        }

        var result = _service.AnalyzeCompetitors(request);
        return Ok(result);
    }

    /// <summary>
    /// 获取所有已知 AI 爬虫
    /// </summary>
    [HttpGet("crawlers")]
    public ActionResult<List<CrawlerStatus>> GetKnownAICrawlers()
    {
        return Ok(_service.GetKnownAICrawlers());
    }

    /// <summary>
    /// 获取所有 LLM Referrers
    /// </summary>
    [HttpGet("llm-referrers")]
    public ActionResult<List<LLMReferrer>> GetLLMReferrers()
    {
        return Ok(_service.GetLLMReferrers());
    }
}
