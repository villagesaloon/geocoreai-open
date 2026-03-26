using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.CitationSource;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 引用来源分析控制器
/// 功能：4.29 引用来源追踪、4.30 引用集中度分析、4.31 竞品引用来源、4.32 Reddit 活跃度监测
/// </summary>
[ApiController]
[Route("api/citation-sources")]
public class CitationSourceController : ControllerBase
{
    private readonly ILogger<CitationSourceController> _logger;
    private readonly CitationSourceService _sourceService;

    public CitationSourceController(
        ILogger<CitationSourceController> logger,
        CitationSourceService sourceService)
    {
        _logger = logger;
        _sourceService = sourceService;
    }

    /// <summary>
    /// 生成完整引用来源分析报告
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> GenerateReport([FromBody] CitationSourceRequest request)
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

            var report = await _sourceService.GenerateReportAsync(request);
            return Ok(new { success = true, data = report });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationSource] Failed to generate report for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "生成引用来源报告失败" });
        }
    }

    /// <summary>
    /// 4.29 获取引用来源统计
    /// </summary>
    [HttpGet("stats/{taskId}")]
    public async Task<IActionResult> GetSourceStats(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new CitationSourceRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeRedditAnalysis = false,
                IncludeTrends = false
            };

            var report = await _sourceService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    sources = report.SourceStats,
                    totalSources = report.SourceStats.Count,
                    byType = report.SourceStats.GroupBy(s => s.SourceType).ToDictionary(
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
            _logger.LogError(ex, "[CitationSource] Failed to get source stats for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取引用来源统计失败" });
        }
    }

    /// <summary>
    /// 4.30 获取引用集中度分析
    /// </summary>
    [HttpGet("concentration/{taskId}")]
    public async Task<IActionResult> GetConcentration(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new CitationSourceRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeRedditAnalysis = false,
                IncludeTrends = false
            };

            var report = await _sourceService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.Concentration
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationSource] Failed to get concentration for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取引用集中度分析失败" });
        }
    }

    /// <summary>
    /// 4.31 获取竞品引用来源对比
    /// </summary>
    [HttpGet("competitors/{taskId}")]
    public async Task<IActionResult> GetCompetitorSources(
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

            var request = new CitationSourceRequest
            {
                TaskId = taskId,
                Brand = brand,
                Competitors = competitorList,
                IncludeRedditAnalysis = false,
                IncludeTrends = false
            };

            var report = await _sourceService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.CompetitorSources
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationSource] Failed to get competitor sources for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取竞品引用来源失败" });
        }
    }

    /// <summary>
    /// 4.32 获取 Reddit 活跃度分析
    /// </summary>
    [HttpGet("reddit/{taskId}")]
    public async Task<IActionResult> GetRedditActivity(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new CitationSourceRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeRedditAnalysis = true,
                IncludeTrends = false
            };

            var report = await _sourceService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.RedditActivity
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationSource] Failed to get Reddit activity for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取 Reddit 活跃度分析失败" });
        }
    }

    /// <summary>
    /// 获取来源趋势
    /// </summary>
    [HttpGet("trends/{taskId}")]
    public async Task<IActionResult> GetSourceTrends(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new CitationSourceRequest
            {
                TaskId = taskId,
                Brand = brand,
                IncludeRedditAnalysis = false,
                IncludeTrends = true
            };

            var report = await _sourceService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = report.SourceTrends
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationSource] Failed to get source trends for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取来源趋势失败" });
        }
    }

    /// <summary>
    /// 获取优化建议
    /// </summary>
    [HttpGet("suggestions/{taskId}")]
    public async Task<IActionResult> GetSuggestions(int taskId, [FromQuery] string brand, [FromQuery] string? competitors = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new CitationSourceRequest
            {
                TaskId = taskId,
                Brand = brand,
                Competitors = competitors?.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList(),
                IncludeRedditAnalysis = true,
                IncludeTrends = true
            };

            var report = await _sourceService.GenerateReportAsync(request);
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
            _logger.LogError(ex, "[CitationSource] Failed to get suggestions for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取优化建议失败" });
        }
    }
}
