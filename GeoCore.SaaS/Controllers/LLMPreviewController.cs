using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.LLMPreview;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// LLM 预览 API (3.31, 4.54)
/// </summary>
[ApiController]
[Route("api/llm-preview")]
public class LLMPreviewController : ControllerBase
{
    private readonly LLMPreviewService _service;
    private readonly ILogger<LLMPreviewController> _logger;

    public LLMPreviewController(LLMPreviewService service, ILogger<LLMPreviewController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 生成 LLM 预览 (3.31)
    /// </summary>
    [HttpPost("generate")]
    public ActionResult<LLMPreviewResult> GeneratePreview([FromBody] LLMPreviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "内容不能为空" });
        }

        var result = _service.GeneratePreview(request);
        return Ok(result);
    }

    /// <summary>
    /// 获取优化循环状态 (4.54)
    /// </summary>
    [HttpPost("optimization-loop/status")]
    public ActionResult<OptimizationLoopStatus> GetLoopStatus([FromBody] OptimizationLoopConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ProjectId))
        {
            return BadRequest(new { error = "项目 ID 不能为空" });
        }

        var result = _service.GetLoopStatus(config);
        return Ok(result);
    }

    /// <summary>
    /// 运行检测阶段 (4.54)
    /// </summary>
    [HttpPost("optimization-loop/detect")]
    public ActionResult<List<DetectedIssue>> RunDetection([FromBody] OptimizationLoopConfig config)
    {
        var result = _service.RunDetection(config);
        return Ok(result);
    }

    /// <summary>
    /// 应用修复 (4.54)
    /// </summary>
    [HttpPost("optimization-loop/fix")]
    public ActionResult<PendingFix> ApplyFix([FromBody] ApplyFixRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IssueId))
        {
            return BadRequest(new { error = "问题 ID 不能为空" });
        }

        var result = _service.ApplyFix(request.IssueId, request.FixDescription);
        return Ok(result);
    }

    /// <summary>
    /// 验证修复效果 (4.54)
    /// </summary>
    [HttpPost("optimization-loop/verify")]
    public ActionResult<VerifiedImprovement> VerifyFix([FromBody] VerifyFixRequest request)
    {
        var result = _service.VerifyFix(request.FixId, request.BeforeMetric, request.AfterMetric);
        if (result == null)
        {
            return Ok(new { success = false, message = "修复效果未达到预期" });
        }
        return Ok(result);
    }
}

/// <summary>
/// 应用修复请求
/// </summary>
public class ApplyFixRequest
{
    public string IssueId { get; set; } = "";
    public string FixDescription { get; set; } = "";
}

/// <summary>
/// 验证修复请求
/// </summary>
public class VerifyFixRequest
{
    public string FixId { get; set; } = "";
    public double BeforeMetric { get; set; }
    public double AfterMetric { get; set; }
}
