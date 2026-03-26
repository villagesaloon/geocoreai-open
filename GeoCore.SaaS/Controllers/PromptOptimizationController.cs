using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.PromptOptimization;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Prompt 优化 API (5.31-5.32)
/// </summary>
[ApiController]
[Route("api/prompt-optimization")]
public class PromptOptimizationController : ControllerBase
{
    private readonly PromptOptimizationService _service;
    private readonly ILogger<PromptOptimizationController> _logger;

    public PromptOptimizationController(
        PromptOptimizationService service,
        ILogger<PromptOptimizationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 分析 Prompt 质量 (5.31)
    /// </summary>
    [HttpPost("analyze")]
    public ActionResult<PromptAnalysisResult> AnalyzePrompt([FromBody] AnalyzePromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { error = "Prompt 不能为空" });
        }

        var result = _service.AnalyzePrompt(request.Prompt, request.Language ?? "zh");
        return Ok(result);
    }

    /// <summary>
    /// 为特定模型优化 Prompt (5.32)
    /// </summary>
    [HttpPost("optimize")]
    public ActionResult<OptimizePromptResponse> OptimizePrompt([FromBody] OptimizePromptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { error = "Prompt 不能为空" });
        }

        var modelFamily = request.ModelFamily ?? "claude";
        var optimized = _service.OptimizeForModel(request.Prompt, modelFamily, request.Language ?? "zh");
        var analysis = _service.AnalyzePrompt(optimized, request.Language ?? "zh");

        return Ok(new OptimizePromptResponse
        {
            OriginalPrompt = request.Prompt,
            OptimizedPrompt = optimized,
            ModelFamily = modelFamily,
            Analysis = analysis
        });
    }

    /// <summary>
    /// 获取所有模型配置
    /// </summary>
    [HttpGet("models")]
    public ActionResult<List<ModelPromptConfig>> GetModelConfigs()
    {
        return Ok(_service.GetAllModelConfigs());
    }

    /// <summary>
    /// 获取特定模型配置
    /// </summary>
    [HttpGet("models/{modelFamily}")]
    public ActionResult<ModelPromptConfig> GetModelConfig(string modelFamily)
    {
        var config = _service.GetModelConfig(modelFamily);
        if (config == null)
        {
            return NotFound(new { error = $"未找到模型配置: {modelFamily}" });
        }
        return Ok(config);
    }
}

public class AnalyzePromptRequest
{
    public string Prompt { get; set; } = "";
    public string? Language { get; set; }
}

public class OptimizePromptRequest
{
    public string Prompt { get; set; } = "";
    public string? ModelFamily { get; set; }
    public string? Language { get; set; }
}

public class OptimizePromptResponse
{
    public string OriginalPrompt { get; set; } = "";
    public string OptimizedPrompt { get; set; } = "";
    public string ModelFamily { get; set; } = "";
    public PromptAnalysisResult Analysis { get; set; } = new();
}
