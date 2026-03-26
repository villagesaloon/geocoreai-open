using GeoCore.SaaS.Services.GoogleTrends;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Google Trends API 控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TrendsController : ControllerBase
{
    private readonly GoogleTrendsService _trendsService;
    private readonly ILogger<TrendsController> _logger;

    public TrendsController(
        GoogleTrendsService trendsService,
        ILogger<TrendsController> logger)
    {
        _trendsService = trendsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取每日热搜
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyTrends(
        [FromQuery] string? geo = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetDailyTrends: geo={Geo}", geo);

        var result = await _trendsService.GetDailyTrendsAsync(geo, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { success = false, error = result.Error });
        }

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 获取关键词趋势数据
    /// </summary>
    [HttpPost("interest")]
    public async Task<IActionResult> GetInterestOverTime(
        [FromBody] InterestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Keywords == null || request.Keywords.Length == 0)
        {
            return BadRequest(new { success = false, error = "Keywords are required" });
        }

        _logger.LogInformation("GetInterestOverTime: keywords={Keywords}, geo={Geo}", 
            string.Join(", ", request.Keywords), request.Geo);

        var result = await _trendsService.GetInterestOverTimeAsync(
            request.Keywords,
            request.Geo,
            request.DateRange,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { success = false, error = result.Error });
        }

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 获取相关搜索词
    /// </summary>
    [HttpGet("related")]
    public async Task<IActionResult> GetRelatedQueries(
        [FromQuery] string keyword,
        [FromQuery] string? geo = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest(new { success = false, error = "Keyword is required" });
        }

        _logger.LogInformation("GetRelatedQueries: keyword={Keyword}, geo={Geo}", keyword, geo);

        var result = await _trendsService.GetRelatedQueriesAsync(keyword, geo, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { success = false, error = result.Error });
        }

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 品牌 vs 竞品趋势对比
    /// </summary>
    [HttpPost("compare")]
    public async Task<IActionResult> CompareBrands(
        [FromBody] CompareRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Brand))
        {
            return BadRequest(new { success = false, error = "Brand is required" });
        }

        _logger.LogInformation("CompareBrands: brand={Brand}, competitors={Competitors}", 
            request.Brand, string.Join(", ", request.Competitors ?? new List<string>()));

        var result = await _trendsService.CompareBrandsAsync(
            request.Brand,
            request.Competitors ?? new List<string>(),
            request.Geo,
            request.DateRange,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { success = false, error = result.Error });
        }

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 验证问题热度（批量）
    /// </summary>
    [HttpPost("validate-questions")]
    public async Task<IActionResult> ValidateQuestionTrends(
        [FromBody] ValidateQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Questions == null || request.Questions.Count == 0)
        {
            return BadRequest(new { success = false, error = "Questions are required" });
        }

        _logger.LogInformation("ValidateQuestionTrends: {Count} questions", request.Questions.Count);

        var results = await _trendsService.ValidateQuestionTrendsAsync(
            request.Questions,
            request.Geo,
            cancellationToken);

        return Ok(new { success = true, data = results });
    }
}

#region Request Models

public class InterestRequest
{
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string? Geo { get; set; }
    public string? DateRange { get; set; }
}

public class CompareRequest
{
    public string Brand { get; set; } = string.Empty;
    public List<string>? Competitors { get; set; }
    public string? Geo { get; set; }
    public string? DateRange { get; set; }
}

public class ValidateQuestionsRequest
{
    public List<string> Questions { get; set; } = new();
    public string? Geo { get; set; }
}

#endregion
