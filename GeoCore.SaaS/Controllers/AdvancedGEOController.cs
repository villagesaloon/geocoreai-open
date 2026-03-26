using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.AdvancedGEO;

namespace GeoCore.SaaS.Controllers;

[ApiController]
[Route("api/advanced-geo")]
public class AdvancedGEOController : ControllerBase
{
    private readonly AdvancedGEOService _service;

    public AdvancedGEOController(AdvancedGEOService service)
    {
        _service = service;
    }

    #region 7.9 Query Fan-out 分析器

    [HttpPost("query-fanout")]
    public ActionResult<QueryFanoutResult> AnalyzeQueryFanout([FromBody] QueryFanoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MainQuery))
            return BadRequest("主查询不能为空");

        var result = _service.AnalyzeQueryFanout(request);
        return Ok(result);
    }

    #endregion

    #region 7.10 Answer Capsules 检测器

    [HttpPost("answer-capsules")]
    public ActionResult<AnswerCapsuleResult> DetectAnswerCapsules([FromBody] AnswerCapsuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("内容不能为空");

        var result = _service.DetectAnswerCapsules(request);
        return Ok(result);
    }

    #endregion

    #region 7.11 Google 排名-AI 引用相关性

    [HttpPost("ranking-correlation")]
    public ActionResult<RankingCorrelationResult> AnalyzeRankingCorrelation([FromBody] RankingCorrelationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest("域名不能为空");

        var result = _service.AnalyzeRankingCorrelation(request);
        return Ok(result);
    }

    #endregion

    #region 7.12 平台独立性评估

    [HttpPost("platform-independence")]
    public ActionResult<PlatformIndependenceResult> EvaluatePlatformIndependence([FromBody] PlatformIndependenceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Domain))
            return BadRequest("域名不能为空");

        var result = _service.EvaluatePlatformIndependence(request);
        return Ok(result);
    }

    #endregion

    #region 7.13 多语言 AI 可见度

    [HttpPost("multi-language")]
    public ActionResult<MultiLanguageResult> AnalyzeMultiLanguage([FromBody] MultiLanguageRequest request)
    {
        var result = _service.AnalyzeMultiLanguage(request);
        return Ok(result);
    }

    [HttpGet("supported-languages")]
    public ActionResult<object> GetSupportedLanguages()
    {
        var languages = new[]
        {
            new { Code = "en", Name = "English", Potential = 100 },
            new { Code = "zh", Name = "中文", Potential = 85 },
            new { Code = "es", Name = "Español", Potential = 70 },
            new { Code = "de", Name = "Deutsch", Potential = 65 },
            new { Code = "fr", Name = "Français", Potential = 60 },
            new { Code = "ja", Name = "日本語", Potential = 55 },
            new { Code = "ko", Name = "한국어", Potential = 50 },
            new { Code = "pt", Name = "Português", Potential = 45 },
            new { Code = "it", Name = "Italiano", Potential = 40 },
            new { Code = "nl", Name = "Nederlands", Potential = 35 },
            new { Code = "ar", Name = "العربية", Potential = 30 },
            new { Code = "hi", Name = "हिन्दी", Potential = 25 }
        };
        return Ok(languages);
    }

    #endregion

    #region 4.39-4.41 平台依赖度监测

    [HttpPost("dependency-monitor")]
    public ActionResult<PlatformDependencyMonitorResult> MonitorPlatformDependency([FromBody] PlatformDependencyMonitorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
            return BadRequest("品牌不能为空");

        var result = _service.MonitorPlatformDependency(request);
        return Ok(result);
    }

    #endregion

    #region 5.21 跨平台调度

    [HttpPost("cross-platform-schedule")]
    public ActionResult<CrossPlatformScheduleResult> GenerateSchedule([FromBody] CrossPlatformScheduleRequest request)
    {
        if (request.TargetPlatforms == null || request.TargetPlatforms.Count == 0)
            return BadRequest("目标平台不能为空");

        var result = _service.GenerateSchedule(request);
        return Ok(result);
    }

    #endregion

    #region 5.29-5.30 最佳实践/报告

    [HttpPost("best-practices")]
    public ActionResult<BestPracticeResult> ExtractBestPractices([FromBody] BestPracticeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
            return BadRequest("品牌不能为空");

        var result = _service.ExtractBestPractices(request);
        return Ok(result);
    }

    [HttpPost("automated-report")]
    public ActionResult<AutomatedReportResult> GenerateAutomatedReport([FromBody] AutomatedReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
            return BadRequest("品牌不能为空");

        var result = _service.GenerateAutomatedReport(request);
        return Ok(result);
    }

    #endregion
}
