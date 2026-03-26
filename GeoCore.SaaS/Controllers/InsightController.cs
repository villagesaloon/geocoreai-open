using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.InsightAnalyzer;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 洞察分析控制器 - 四信号分析 + 三路径建议 + 可执行任务
/// </summary>
[ApiController]
[Route("api/insights")]
public class InsightController : ControllerBase
{
    private readonly ILogger<InsightController> _logger;
    private readonly InsightAnalyzerService _insightService;

    public InsightController(
        ILogger<InsightController> logger,
        InsightAnalyzerService insightService)
    {
        _logger = logger;
        _insightService = insightService;
    }

    /// <summary>
    /// 生成完整洞察报告
    /// </summary>
    [HttpGet("report/{taskId}")]
    public async Task<IActionResult> GetReport(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var report = await _insightService.GenerateReportAsync(taskId, brand);
            return Ok(new { success = true, data = report });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Insight] Failed to generate report for task {TaskId}", taskId);
            return StatusCode(500, new { error = "生成洞察报告失败" });
        }
    }

    /// <summary>
    /// 获取四信号分析
    /// </summary>
    [HttpGet("signals/{taskId}")]
    public async Task<IActionResult> GetSignals(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var report = await _insightService.GenerateReportAsync(taskId, brand);
            return Ok(new
            {
                success = true,
                data = new
                {
                    report.Signals,
                    report.Signals.OverallScore,
                    report.Signals.Summary
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Insight] Failed to get signals for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取信号分析失败" });
        }
    }

    /// <summary>
    /// 获取三路径建议
    /// </summary>
    [HttpGet("recommendations/{taskId}")]
    public async Task<IActionResult> GetRecommendations(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var report = await _insightService.GenerateReportAsync(taskId, brand);
            return Ok(new
            {
                success = true,
                data = new
                {
                    report.Recommendations,
                    report.Recommendations.RecommendedPriority,
                    report.Recommendations.PriorityReason
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Insight] Failed to get recommendations for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取建议失败" });
        }
    }

    /// <summary>
    /// 获取可执行任务列表
    /// </summary>
    [HttpGet("tasks/{taskId}")]
    public async Task<IActionResult> GetTasks(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var report = await _insightService.GenerateReportAsync(taskId, brand);
            return Ok(new
            {
                success = true,
                data = new
                {
                    tasks = report.Tasks,
                    totalTasks = report.Tasks.Count,
                    byCategory = report.Tasks.GroupBy(t => t.Category).ToDictionary(
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
            _logger.LogError(ex, "[Insight] Failed to get tasks for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取任务列表失败" });
        }
    }

    /// <summary>
    /// 获取洞察摘要（轻量级）
    /// </summary>
    [HttpGet("summary/{taskId}")]
    public async Task<IActionResult> GetSummary(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var report = await _insightService.GenerateReportAsync(taskId, brand);
            return Ok(new
            {
                success = true,
                data = new
                {
                    brand = report.Brand,
                    overallScore = report.Signals.OverallScore,
                    summary = report.ReportSummary,
                    signals = new
                    {
                        citationConcentration = report.Signals.CitationConcentration.Strength,
                        currentPosition = report.Signals.CurrentPosition.Strength,
                        brandStrength = report.Signals.BrandStrength.Strength,
                        category = report.Signals.Category.Strength
                    },
                    recommendedAction = report.Recommendations.RecommendedPriority,
                    topTasks = report.Tasks.Take(3).Select(t => new
                    {
                        t.Title,
                        t.Category,
                        t.Priority
                    }),
                    generatedAt = report.GeneratedAt
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Insight] Failed to get summary for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取摘要失败" });
        }
    }

    /// <summary>
    /// 对比多个品牌的洞察
    /// </summary>
    [HttpGet("compare/{taskId}")]
    public async Task<IActionResult> CompareBrands(int taskId, [FromQuery] string brands)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brands))
            {
                return BadRequest(new { error = "品牌列表不能为空" });
            }

            var brandList = brands.Split(',').Select(b => b.Trim()).Where(b => !string.IsNullOrEmpty(b)).ToList();
            if (brandList.Count < 2)
            {
                return BadRequest(new { error = "至少需要两个品牌进行对比" });
            }

            var comparisons = new List<object>();
            foreach (var brand in brandList.Take(5)) // 最多对比5个品牌
            {
                try
                {
                    var report = await _insightService.GenerateReportAsync(taskId, brand);
                    comparisons.Add(new
                    {
                        brand,
                        overallScore = report.Signals.OverallScore,
                        mentionRate = report.Signals.CurrentPosition.OverallMentionRate,
                        firstPositionRate = report.Signals.CurrentPosition.FirstPositionRate,
                        brandStrength = report.Signals.BrandStrength.Strength,
                        recommendedAction = report.Recommendations.RecommendedPriority
                    });
                }
                catch
                {
                    comparisons.Add(new
                    {
                        brand,
                        overallScore = 0.0,
                        mentionRate = 0.0,
                        firstPositionRate = 0.0,
                        brandStrength = 0.0,
                        recommendedAction = "unknown"
                    });
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    taskId,
                    comparisons,
                    winner = comparisons.OrderByDescending(c => ((dynamic)c).overallScore).First()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Insight] Failed to compare brands for task {TaskId}", taskId);
            return StatusCode(500, new { error = "品牌对比失败" });
        }
    }
}
