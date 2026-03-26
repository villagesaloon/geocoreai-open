using GeoCore.SaaS.Services.SiteAudit;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.SaaS.Controllers;

[ApiController]
[Route("api/site-audit")]
public class SiteAuditController : ControllerBase
{
    private readonly SiteAuditService _siteAuditService;
    private readonly ILogger<SiteAuditController> _logger;

    public SiteAuditController(SiteAuditService siteAuditService, ILogger<SiteAuditController> logger)
    {
        _siteAuditService = siteAuditService;
        _logger = logger;
    }

    /// <summary>
    /// 6.5-6.16 完整网站 GEO/SEO 审计
    /// </summary>
    [HttpPost("audit")]
    public async Task<ActionResult<SiteAuditResult>> AuditSite([FromBody] SiteAuditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest("URL is required");
        }

        try
        {
            var result = await _siteAuditService.AuditSiteAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Site audit failed for {Url}", request.Url);
            return StatusCode(500, new { error = "Audit failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 6.17 IndexNow 提交
    /// </summary>
    [HttpPost("index-now")]
    public async Task<ActionResult<IndexNowResult>> SubmitToIndexNow([FromBody] IndexNowSubmitRequest request)
    {
        if (request.Urls == null || request.Urls.Count == 0)
        {
            return BadRequest("At least one URL is required");
        }

        try
        {
            var result = await _siteAuditService.SubmitToIndexNowAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IndexNow submission failed");
            return StatusCode(500, new { error = "Submission failed", message = ex.Message });
        }
    }

    /// <summary>
    /// 6.18 Sitemap 生成器
    /// </summary>
    [HttpPost("generate/sitemap")]
    public ActionResult<SitemapGenerateResult> GenerateSitemap([FromBody] SitemapGenerateRequest request)
    {
        if (request.Urls == null || request.Urls.Count == 0)
        {
            return BadRequest("At least one URL is required");
        }

        var result = _siteAuditService.GenerateSitemap(request);
        return Ok(result);
    }

    /// <summary>
    /// 6.19 llms.txt 生成器
    /// </summary>
    [HttpPost("generate/llms-txt")]
    public ActionResult<LlmsTxtGenerateResult> GenerateLlmsTxt([FromBody] SiteAuditLlmsTxtRequest request)
    {
        if (request.Urls == null || request.Urls.Count == 0)
        {
            return BadRequest("At least one URL is required");
        }

        var result = _siteAuditService.GenerateLlmsTxt(request.Urls, request.BaseUrl);
        return Ok(result);
    }

    /// <summary>
    /// 6.20 robots.txt 生成器
    /// </summary>
    [HttpPost("generate/robots-txt")]
    public ActionResult<RobotsTxtGenerateResult> GenerateRobotsTxt([FromBody] RobotsTxtGenerateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            return BadRequest("Base URL is required");
        }

        var result = _siteAuditService.GenerateRobotsTxt(request);
        return Ok(result);
    }
}

public class SiteAuditLlmsTxtRequest
{
    public List<string> Urls { get; set; } = new();
    public string BaseUrl { get; set; } = string.Empty;
}
