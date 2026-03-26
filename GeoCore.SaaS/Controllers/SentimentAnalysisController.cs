using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.SentimentAnalysis;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 情感分析增强控制器
/// 功能：4.33 多维情感分析、4.34 情感趋势追踪、4.35 竞品情感对比
/// </summary>
[ApiController]
[Route("api/sentiment")]
public class SentimentAnalysisController : ControllerBase
{
    private readonly ILogger<SentimentAnalysisController> _logger;
    private readonly SentimentAnalysisService _sentimentService;

    public SentimentAnalysisController(
        ILogger<SentimentAnalysisController> logger,
        SentimentAnalysisService sentimentService)
    {
        _logger = logger;
        _sentimentService = sentimentService;
    }

    /// <summary>
    /// 生成完整情感分析报告
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> GenerateReport([FromBody] SentimentAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            if (request.TaskId <= 0)
            {
                return BadRequest(new { error = "任务ID无效" });
            }

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new { success = true, data = report });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to generate report for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "生成情感分析报告失败" });
        }
    }

    /// <summary>
    /// 4.33 获取多维情感分析
    /// </summary>
    [HttpGet("multi-dimensional/{taskId}")]
    public async Task<IActionResult> GetMultiDimensionalSentiment(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeTrends = false,
                IncludeKeywords = false,
                IncludeAlerts = false
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.Sentiment
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get multi-dimensional sentiment for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取多维情感分析失败" });
        }
    }

    /// <summary>
    /// 4.34 获取情感趋势
    /// </summary>
    [HttpGet("trends/{taskId}")]
    public async Task<IActionResult> GetSentimentTrends(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeTrends = true,
                IncludeKeywords = false,
                IncludeAlerts = false
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.Trends
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get sentiment trends for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取情感趋势失败" });
        }
    }

    /// <summary>
    /// 4.35 获取竞品情感对比
    /// </summary>
    [HttpGet("competitors/{taskId}")]
    public async Task<IActionResult> GetCompetitorSentiment(
        int taskId, 
        [FromQuery] string brand, 
        [FromQuery] string competitors)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            if (string.IsNullOrWhiteSpace(competitors))
            {
                return BadRequest(new { error = "竞品列表不能为空" });
            }

            var competitorList = competitors.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                Competitors = competitorList,
                IncludeTrends = false,
                IncludeKeywords = false,
                IncludeAlerts = false
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.CompetitorComparisons
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get competitor sentiment for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取竞品情感对比失败" });
        }
    }

    /// <summary>
    /// 获取情感关键词
    /// </summary>
    [HttpGet("keywords/{taskId}")]
    public async Task<IActionResult> GetSentimentKeywords(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeTrends = false,
                IncludeKeywords = true,
                IncludeAlerts = false
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    keywords = report.Keywords,
                    positive = report.Keywords.Where(k => k.Sentiment == "positive").ToList(),
                    negative = report.Keywords.Where(k => k.Sentiment == "negative").ToList()
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get sentiment keywords for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取情感关键词失败" });
        }
    }

    /// <summary>
    /// 获取平台情感分布
    /// </summary>
    [HttpGet("platforms/{taskId}")]
    public async Task<IActionResult> GetPlatformSentiment(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeTrends = false,
                IncludeKeywords = false,
                IncludeAlerts = false
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.PlatformBreakdown
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get platform sentiment for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取平台情感分布失败" });
        }
    }

    /// <summary>
    /// 获取情感预警
    /// </summary>
    [HttpGet("alerts/{taskId}")]
    public async Task<IActionResult> GetSentimentAlerts(int taskId, [FromQuery] string brand, [FromQuery] string? competitors = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                Competitors = competitors?.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList(),
                IncludeTrends = true,
                IncludeKeywords = false,
                IncludeAlerts = true
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    alerts = report.Alerts,
                    critical = report.Alerts.Count(a => a.Level == "critical"),
                    warning = report.Alerts.Count(a => a.Level == "warning"),
                    info = report.Alerts.Count(a => a.Level == "info")
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get sentiment alerts for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取情感预警失败" });
        }
    }

    /// <summary>
    /// 获取优化建议
    /// </summary>
    [HttpGet("suggestions/{taskId}")]
    public async Task<IActionResult> GetSuggestions(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new SentimentAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeTrends = true,
                IncludeKeywords = true,
                IncludeAlerts = true
            };

            var report = await _sentimentService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    suggestions = report.Suggestions,
                    totalSuggestions = report.Suggestions.Count,
                    byType = report.Suggestions.GroupBy(s => s.Type).ToDictionary(
                        g => g.Key,
                        g => g.Count()
                    )
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SentimentAnalysis] Failed to get suggestions for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取优化建议失败" });
        }
    }
}
