using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.CitationBenchmark;

namespace GeoCore.SaaS.Controllers;

[ApiController]
[Route("api/citation-benchmark")]
public class CitationBenchmarkController : ControllerBase
{
    private readonly CitationBenchmarkService _service;

    public CitationBenchmarkController(CitationBenchmarkService service)
    {
        _service = service;
    }

    /// <summary>
    /// 4.42 获取所有平台引用基准
    /// </summary>
    [HttpGet("platforms")]
    public IActionResult GetAllBenchmarks()
    {
        var benchmarks = _service.GetAllBenchmarks();
        return Ok(benchmarks);
    }

    /// <summary>
    /// 4.42 获取指定平台引用基准
    /// </summary>
    [HttpGet("platforms/{platform}")]
    public IActionResult GetBenchmark(string platform)
    {
        var benchmark = _service.GetBenchmark(platform);
        if (benchmark == null)
            return NotFound(new { error = $"Platform '{platform}' not found" });
        return Ok(benchmark);
    }

    /// <summary>
    /// 4.43 内容结构评分
    /// </summary>
    [HttpPost("structure")]
    public IActionResult AnalyzeStructure([FromBody] ContentStructureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        var result = _service.AnalyzeContentStructure(request);
        return Ok(result);
    }

    /// <summary>
    /// 4.44 多模态内容检测
    /// </summary>
    [HttpPost("multimodal")]
    public IActionResult AnalyzeMultimodal([FromBody] MultimodalAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        var result = _service.AnalyzeMultimodal(request);
        return Ok(result);
    }

    /// <summary>
    /// 4.45 实体密度分析
    /// </summary>
    [HttpPost("entities")]
    public IActionResult AnalyzeEntities([FromBody] EntityDensityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        var result = _service.AnalyzeEntityDensity(request);
        return Ok(result);
    }

    /// <summary>
    /// 综合基准分析
    /// </summary>
    [HttpPost("comprehensive")]
    public IActionResult AnalyzeComprehensive([FromBody] ComprehensiveBenchmarkRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        var result = _service.AnalyzeComprehensive(request);
        return Ok(result);
    }

    #region 4.42 分平台引用基准（扩展）

    /// <summary>
    /// 4.42 获取所有平台引用基准（扩展版）
    /// 包含引用指标、Top 来源、内容指南
    /// </summary>
    [HttpGet("platforms/all")]
    public IActionResult GetAllPlatformBenchmarks()
    {
        var result = _service.GetAllPlatformBenchmarks();
        return Ok(result);
    }

    /// <summary>
    /// 4.42 获取平台引用基准详情
    /// 包含详细的引用指标、Top 来源、内容指南和优化建议
    /// </summary>
    [HttpGet("platforms/{platform}/detail")]
    public IActionResult GetPlatformBenchmarkDetail(string platform, [FromQuery] string? industry = null)
    {
        var result = _service.GetPlatformBenchmarkDetail(new PlatformBenchmarkDetailRequest
        {
            Platform = platform,
            Industry = industry ?? ""
        });
        return Ok(result);
    }

    #endregion

    #region 4.47 平台偏好差异化

    /// <summary>
    /// 4.47 平台偏好差异化分析
    /// 分析内容在不同平台的适配度，生成差异化优化策略
    /// 原理：Perplexity 偏新鲜，Claude 偏深度，AIO 偏 FAQ
    /// </summary>
    [HttpPost("platform-preference-diff")]
    public IActionResult AnalyzePlatformPreferenceDiff([FromBody] PlatformPreferenceDiffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        var result = _service.AnalyzePlatformPreferenceDiff(request);
        return Ok(result);
    }

    #endregion
}
