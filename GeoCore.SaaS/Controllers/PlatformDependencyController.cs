using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.PlatformDependency;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 平台依赖度分析控制器
/// </summary>
[ApiController]
[Route("api/platform-dependency")]
public class PlatformDependencyController : ControllerBase
{
    private readonly ILogger<PlatformDependencyController> _logger;
    private readonly PlatformDependencyService _service;

    public PlatformDependencyController(
        ILogger<PlatformDependencyController> logger,
        PlatformDependencyService service)
    {
        _logger = logger;
        _service = service;
    }

    /// <summary>
    /// 生成平台依赖度分析报告
    /// </summary>
    [HttpPost("report")]
    public async Task<IActionResult> GenerateReport([FromBody] PlatformDependencyRequest request)
    {
        try
        {
            if (request.TaskId <= 0)
            {
                return BadRequest(new { error = "任务ID无效" });
            }

            var report = await _service.GenerateReportAsync(request);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlatformDependency] Failed to generate report for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "生成报告失败" });
        }
    }

    /// <summary>
    /// 获取平台曝光占比
    /// </summary>
    [HttpGet("exposure/{taskId}")]
    public async Task<IActionResult> GetExposure(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new PlatformDependencyRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeTrends = false,
                IncludeStrategies = false
            };

            var report = await _service.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    report.DiversificationScore,
                    report.DependencyLevel,
                    report.Platforms
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlatformDependency] Failed to get exposure for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取曝光占比失败" });
        }
    }

    /// <summary>
    /// 获取依赖度警告
    /// </summary>
    [HttpGet("alerts/{taskId}")]
    public async Task<IActionResult> GetAlerts(
        int taskId,
        [FromQuery] string? brand = null,
        [FromQuery] double threshold = 0.5)
    {
        try
        {
            var request = new PlatformDependencyRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                DependencyThreshold = threshold,
                IncludeTrends = false,
                IncludeStrategies = false
            };

            var report = await _service.GenerateReportAsync(request);
            return Ok(new { success = true, data = report.Alerts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlatformDependency] Failed to get alerts for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取警告失败" });
        }
    }

    /// <summary>
    /// 获取分散策略建议
    /// </summary>
    [HttpGet("strategies/{taskId}")]
    public async Task<IActionResult> GetStrategies(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new PlatformDependencyRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeTrends = false,
                IncludeStrategies = true
            };

            var report = await _service.GenerateReportAsync(request);
            return Ok(new { success = true, data = report.Strategies });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlatformDependency] Failed to get strategies for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取策略建议失败" });
        }
    }

    /// <summary>
    /// 获取依赖度趋势
    /// </summary>
    [HttpGet("trend/{taskId}")]
    public async Task<IActionResult> GetTrend(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new PlatformDependencyRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeTrends = true,
                IncludeStrategies = false
            };

            var report = await _service.GenerateReportAsync(request);
            return Ok(new { success = true, data = report.Trend });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlatformDependency] Failed to get trend for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取趋势失败" });
        }
    }
}
