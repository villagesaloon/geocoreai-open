using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentAdapter;

namespace GeoCore.SaaS.Controllers;

[ApiController]
[Route("api/content-adapter")]
public class ContentAdapterController : ControllerBase
{
    private readonly ContentAdapterService _service;

    public ContentAdapterController(ContentAdapterService service)
    {
        _service = service;
    }

    #region 5.14 社媒尺寸适配

    /// <summary>
    /// 获取媒体适配建议
    /// </summary>
    [HttpPost("media/adapt")]
    public ActionResult<MediaAdaptResult> GetMediaAdaptations([FromBody] MediaAdaptRequest request)
    {
        var result = _service.GetMediaAdaptations(request);
        return Ok(result);
    }

    /// <summary>
    /// 获取平台媒体规格
    /// </summary>
    [HttpGet("media/specs/{platform}")]
    public ActionResult<List<PlatformMediaSpec>> GetPlatformMediaSpecs(string platform)
    {
        var specs = _service.GetPlatformMediaSpecs(platform);
        return Ok(specs);
    }

    #endregion

    #region 5.15 视频脚本生成

    /// <summary>
    /// 从文章生成视频脚本
    /// </summary>
    [HttpPost("video/script")]
    public ActionResult<VideoScriptResult> GenerateVideoScript([FromBody] VideoScriptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ArticleContent))
        {
            return BadRequest("文章内容不能为空");
        }

        var result = _service.GenerateVideoScript(request);
        return Ok(result);
    }

    #endregion

    #region 5.16 短视频切片建议

    /// <summary>
    /// 从长视频识别短视频切片机会
    /// </summary>
    [HttpPost("video/clips")]
    public ActionResult<ShortClipResult> SuggestShortClips([FromBody] ShortClipRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VideoTranscript))
        {
            return BadRequest("视频文字稿不能为空");
        }

        var result = _service.SuggestShortClips(request);
        return Ok(result);
    }

    #endregion

    #region 5.17 图文卡片生成

    /// <summary>
    /// 生成图文轮播卡片
    /// </summary>
    [HttpPost("carousel/generate")]
    public ActionResult<CarouselCardResult> GenerateCarouselCards([FromBody] CarouselCardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ArticleContent))
        {
            return BadRequest("文章内容不能为空");
        }

        var result = _service.GenerateCarouselCards(request);
        return Ok(result);
    }

    #endregion

    #region 5.20 发布时间建议

    /// <summary>
    /// 获取最佳发布时间建议
    /// </summary>
    [HttpPost("posting-time")]
    public ActionResult<PostingTimeResult> GetPostingTimeSuggestion([FromBody] PostingTimeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Platform))
        {
            return BadRequest("平台不能为空");
        }

        var result = _service.GetPostingTimeSuggestion(request);
        return Ok(result);
    }

    #endregion

    #region 5.24-5.26 Reddit 专项

    /// <summary>
    /// 匹配最佳 Subreddits
    /// </summary>
    [HttpPost("reddit/match")]
    public ActionResult<RedditSubredditMatchResult> MatchSubreddits([FromBody] RedditSubredditMatchRequest request)
    {
        var result = _service.MatchSubreddits(request);
        return Ok(result);
    }

    /// <summary>
    /// 检查 Reddit 规则合规性
    /// </summary>
    [HttpPost("reddit/check-rules")]
    public ActionResult<RedditRuleCheckResult> CheckRedditRules([FromBody] RedditRuleCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subreddit))
        {
            return BadRequest("Subreddit 不能为空");
        }

        var result = _service.CheckRedditRules(request);
        return Ok(result);
    }

    /// <summary>
    /// 生成 Reddit 账号养成计划
    /// </summary>
    [HttpPost("reddit/account-plan")]
    public ActionResult<RedditAccountPlanResult> GenerateAccountPlan([FromBody] RedditAccountPlanRequest request)
    {
        var result = _service.GenerateAccountPlan(request);
        return Ok(result);
    }

    #endregion

    #region 5.27-5.28 效果追踪

    /// <summary>
    /// 计算平台 ROI
    /// </summary>
    [HttpPost("roi/calculate")]
    public ActionResult<PlatformROIResult> CalculatePlatformROI([FromBody] PlatformROIRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Platform))
        {
            return BadRequest("平台不能为空");
        }

        var result = _service.CalculatePlatformROI(request);
        return Ok(result);
    }

    /// <summary>
    /// 分析内容生命周期
    /// </summary>
    [HttpPost("lifecycle/analyze")]
    public ActionResult<ContentLifecycleResult> AnalyzeContentLifecycle([FromBody] ContentLifecycleRequest request)
    {
        var result = _service.AnalyzeContentLifecycle(request);
        return Ok(result);
    }

    #endregion
}
