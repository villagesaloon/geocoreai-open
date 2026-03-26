using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentReuse;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 内容复用 API (5.18-5.19)
/// </summary>
[ApiController]
[Route("api/content-reuse")]
public class ContentReuseController : ControllerBase
{
    private readonly ContentReuseService _service;
    private readonly ILogger<ContentReuseController> _logger;

    public ContentReuseController(ContentReuseService service, ILogger<ContentReuseController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// 转换内容到多个平台 (5.18)
    /// </summary>
    [HttpPost("transform")]
    public ActionResult<ContentReuseResult> TransformContent([FromBody] ContentReuseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalContent))
        {
            return BadRequest(new { error = "内容不能为空" });
        }

        var result = _service.TransformContent(request);
        return Ok(result);
    }

    /// <summary>
    /// 根据内容类型推荐平台 (5.19)
    /// </summary>
    [HttpPost("recommend-platforms")]
    public ActionResult<PlatformSelectionResult> RecommendPlatforms([FromBody] PlatformSelectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "内容不能为空" });
        }

        var result = _service.RecommendPlatforms(request);
        return Ok(result);
    }

    /// <summary>
    /// 获取所有平台配置
    /// </summary>
    [HttpGet("platforms")]
    public ActionResult<List<PlatformConfig>> GetAllPlatforms()
    {
        return Ok(_service.GetAllPlatforms());
    }

    /// <summary>
    /// 获取特定平台配置
    /// </summary>
    [HttpGet("platforms/{platformId}")]
    public ActionResult<PlatformConfig> GetPlatformConfig(string platformId)
    {
        var config = _service.GetPlatformConfig(platformId);
        if (config == null)
        {
            return NotFound(new { error = $"未找到平台配置: {platformId}" });
        }
        return Ok(config);
    }
}
