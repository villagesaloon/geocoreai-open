using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.Reddit;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Reddit API (5.22, 5.23, 5.29, 5.30, 5.33)
/// </summary>
[ApiController]
[Route("api/reddit")]
public class RedditController : ControllerBase
{
    private readonly RedditService _service;
    private readonly ILogger<RedditController> _logger;

    public RedditController(RedditService service, ILogger<RedditController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 生成 Reddit 帖子 (5.22)
    /// </summary>
    [HttpPost("post/generate")]
    public ActionResult<RedditPostResult> GeneratePost([FromBody] RedditPostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "内容不能为空" });
        }

        var result = _service.GeneratePost(request);
        return Ok(result);
    }

    /// <summary>
    /// 生成 Reddit 评论 (5.23)
    /// </summary>
    [HttpPost("comment/generate")]
    public ActionResult<RedditCommentResult> GenerateComment([FromBody] RedditCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PostContent))
        {
            return BadRequest(new { error = "帖子内容不能为空" });
        }

        var result = _service.GenerateComment(request);
        return Ok(result);
    }

    /// <summary>
    /// 评估暗漏斗状态 (5.29)
    /// </summary>
    [HttpPost("dark-funnel/assess")]
    public ActionResult<DarkFunnelResult> AssessDarkFunnel([FromBody] DarkFunnelConfig config)
    {
        if (config.BrandKeywords.Count == 0)
        {
            return BadRequest(new { error = "品牌关键词不能为空" });
        }

        var result = _service.AssessDarkFunnel(config);
        return Ok(result);
    }

    /// <summary>
    /// 获取自报告归因配置 (5.30)
    /// </summary>
    [HttpGet("attribution/config")]
    public ActionResult<SelfReportAttributionConfig> GetAttributionConfig()
    {
        return Ok(_service.GetDefaultAttributionConfig());
    }

    /// <summary>
    /// 分析归因统计 (5.30)
    /// </summary>
    [HttpPost("attribution/analyze")]
    public ActionResult<AttributionStats> AnalyzeAttributions([FromBody] List<string> responses)
    {
        var result = _service.AnalyzeAttributions(responses);
        return Ok(result);
    }

    /// <summary>
    /// 评估 Reddit 参与度 (5.33)
    /// </summary>
    [HttpPost("engagement/assess")]
    public ActionResult<EngagementAssessmentResult> AssessEngagement([FromBody] EngagementAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { error = "品牌名称不能为空" });
        }

        var result = _service.AssessEngagement(request);
        return Ok(result);
    }

    /// <summary>
    /// 获取参与度阶段 (5.33)
    /// </summary>
    [HttpGet("engagement/stages")]
    public ActionResult<List<EngagementStage>> GetEngagementStages()
    {
        return Ok(_service.GetEngagementStages());
    }
}
