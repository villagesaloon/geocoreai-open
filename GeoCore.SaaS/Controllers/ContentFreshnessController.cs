using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using GeoCore.SaaS.Services.ContentFreshness;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 内容时效性管理控制器
/// </summary>
[ApiController]
[Route("api/content-freshness")]
public class ContentFreshnessController : ControllerBase
{
    private readonly ILogger<ContentFreshnessController> _logger;
    private readonly IContentFreshnessRepository _repository;
    private readonly ContentFreshnessService _analysisService;

    public ContentFreshnessController(
        ILogger<ContentFreshnessController> logger,
        IContentFreshnessRepository repository,
        ContentFreshnessService analysisService)
    {
        _logger = logger;
        _repository = repository;
        _analysisService = analysisService;
    }

    /// <summary>
    /// 获取项目的所有内容时效性记录
    /// </summary>
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetByProject(string projectId)
    {
        try
        {
            var contents = await _repository.GetAllAsync(projectId);
            return Ok(new { success = true, data = contents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get contents for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "获取内容列表失败" });
        }
    }

    /// <summary>
    /// 获取已过期的内容
    /// </summary>
    [HttpGet("expired")]
    public async Task<IActionResult> GetExpired([FromQuery] string? projectId = null)
    {
        try
        {
            var contents = await _repository.GetExpiredAsync(projectId);
            return Ok(new { success = true, data = contents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get expired contents");
            return StatusCode(500, new { error = "获取过期内容失败" });
        }
    }

    /// <summary>
    /// 获取即将需要刷新的内容
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming(
        [FromQuery] string? projectId = null,
        [FromQuery] int daysAhead = 7)
    {
        try
        {
            var contents = await _repository.GetUpcomingRefreshAsync(projectId, daysAhead);
            return Ok(new { success = true, data = contents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get upcoming refresh contents");
            return StatusCode(500, new { error = "获取即将刷新内容失败" });
        }
    }

    /// <summary>
    /// 获取项目内容时效性统计
    /// </summary>
    [HttpGet("stats/{projectId}")]
    public async Task<IActionResult> GetStats(string projectId)
    {
        try
        {
            var stats = await _repository.GetStatsAsync(projectId);
            return Ok(new { success = true, data = stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get stats for project {ProjectId}", projectId);
            return StatusCode(500, new { error = "获取统计失败" });
        }
    }

    /// <summary>
    /// 获取内容详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var content = await _repository.GetByIdAsync(id);
            if (content == null)
            {
                return NotFound(new { error = "内容不存在" });
            }
            return Ok(new { success = true, data = content });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get content {Id}", id);
            return StatusCode(500, new { error = "获取内容失败" });
        }
    }

    /// <summary>
    /// 添加内容追踪
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContentFreshnessRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProjectId))
            {
                return BadRequest(new { error = "项目ID不能为空" });
            }
            if (string.IsNullOrWhiteSpace(request.ContentIdentifier))
            {
                return BadRequest(new { error = "内容标识不能为空" });
            }

            // 检查是否已存在
            var existing = await _repository.GetByIdentifierAsync(request.ProjectId, request.ContentIdentifier);
            if (existing != null)
            {
                return BadRequest(new { error = "该内容已在追踪中" });
            }

            var entity = new ContentFreshnessEntity
            {
                ProjectId = request.ProjectId,
                ContentType = request.ContentType ?? "general",
                ContentIdentifier = request.ContentIdentifier,
                Title = request.Title ?? "",
                LastUpdatedAt = request.LastUpdatedAt ?? DateTime.UtcNow,
                RefreshIntervalDays = request.RefreshIntervalDays ?? 90
            };

            var id = await _repository.CreateAsync(entity);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to create content tracking");
            return StatusCode(500, new { error = "添加内容追踪失败" });
        }
    }

    /// <summary>
    /// 更新内容信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContentFreshnessRequest request)
    {
        try
        {
            var content = await _repository.GetByIdAsync(id);
            if (content == null)
            {
                return NotFound(new { error = "内容不存在" });
            }

            if (!string.IsNullOrEmpty(request.Title))
                content.Title = request.Title;
            if (!string.IsNullOrEmpty(request.ContentType))
                content.ContentType = request.ContentType;
            if (request.RefreshIntervalDays.HasValue)
            {
                content.RefreshIntervalDays = request.RefreshIntervalDays.Value;
                content.NextRefreshAt = content.LastUpdatedAt.AddDays(request.RefreshIntervalDays.Value);
            }

            await _repository.UpdateAsync(content);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to update content {Id}", id);
            return StatusCode(500, new { error = "更新内容失败" });
        }
    }

    /// <summary>
    /// 标记内容已刷新
    /// </summary>
    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> MarkRefreshed(int id)
    {
        try
        {
            await _repository.MarkRefreshedAsync(id);
            _logger.LogInformation("[ContentFreshness] Content {Id} marked as refreshed", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to mark content {Id} as refreshed", id);
            return StatusCode(500, new { error = "标记刷新失败" });
        }
    }

    /// <summary>
    /// 删除内容追踪
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _repository.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to delete content {Id}", id);
            return StatusCode(500, new { error = "删除内容失败" });
        }
    }

    /// <summary>
    /// 获取需要发送提醒的内容（用于后台任务）
    /// </summary>
    [HttpGet("pending-reminders")]
    public async Task<IActionResult> GetPendingReminders([FromQuery] string? projectId = null)
    {
        try
        {
            var expired = await _repository.GetExpiredAsync(projectId);
            var pending = expired.Where(c => !c.ReminderSent).ToList();
            return Ok(new { success = true, data = pending });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get pending reminders");
            return StatusCode(500, new { error = "获取待提醒内容失败" });
        }
    }

    #region Phase 4I: 内容新鲜度分析 API

    /// <summary>
    /// 生成内容新鲜度分析报告
    /// </summary>
    [HttpPost("analysis/report")]
    public async Task<IActionResult> GenerateAnalysisReport([FromBody] ContentFreshnessRequest request)
    {
        try
        {
            if (request.TaskId <= 0)
            {
                return BadRequest(new { error = "任务ID无效" });
            }

            var report = await _analysisService.GenerateReportAsync(request);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to generate analysis report for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "生成分析报告失败" });
        }
    }

    /// <summary>
    /// 获取新鲜度告警
    /// </summary>
    [HttpGet("analysis/alerts/{taskId}")]
    public async Task<IActionResult> GetAlerts(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new ContentFreshnessRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeAlerts = true,
                IncludeTrends = false
            };

            var report = await _analysisService.GenerateReportAsync(request);
            return Ok(new { success = true, data = report.Alerts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get alerts for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取告警失败" });
        }
    }

    /// <summary>
    /// 获取更新建议
    /// </summary>
    [HttpGet("analysis/suggestions/{taskId}")]
    public async Task<IActionResult> GetSuggestions(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new ContentFreshnessRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeAlerts = false,
                IncludeTrends = false
            };

            var report = await _analysisService.GenerateReportAsync(request);
            return Ok(new { success = true, data = report.Suggestions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get suggestions for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取建议失败" });
        }
    }

    /// <summary>
    /// 获取新鲜度分布统计
    /// </summary>
    [HttpGet("analysis/distribution/{taskId}")]
    public async Task<IActionResult> GetDistribution(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new ContentFreshnessRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeAlerts = false,
                IncludeTrends = false
            };

            var report = await _analysisService.GenerateReportAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    report.OverallScore,
                    report.FreshnessLevel,
                    report.Distribution
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get distribution for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取分布统计失败" });
        }
    }

    /// <summary>
    /// 获取新鲜度趋势
    /// </summary>
    [HttpGet("analysis/trend/{taskId}")]
    public async Task<IActionResult> GetTrend(int taskId, [FromQuery] string? brand = null)
    {
        try
        {
            var request = new ContentFreshnessRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeAlerts = false,
                IncludeTrends = true
            };

            var report = await _analysisService.GenerateReportAsync(request);
            return Ok(new { success = true, data = report.Trend });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get trend for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取趋势失败" });
        }
    }

    /// <summary>
    /// 获取内容新鲜度详情列表
    /// </summary>
    [HttpGet("analysis/items/{taskId}")]
    public async Task<IActionResult> GetItems(
        int taskId,
        [FromQuery] string? brand = null,
        [FromQuery] string? level = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            var request = new ContentFreshnessRequest
            {
                TaskId = taskId,
                Brand = brand ?? "",
                IncludeAlerts = false,
                IncludeTrends = false
            };

            var report = await _analysisService.GenerateReportAsync(request);
            var items = report.Items.AsEnumerable();

            if (!string.IsNullOrEmpty(level))
            {
                items = items.Where(i => i.FreshnessLevel == level);
            }

            if (limit.HasValue && limit.Value > 0)
            {
                items = items.Take(limit.Value);
            }

            return Ok(new { success = true, data = items.ToList() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshness] Failed to get items for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取内容列表失败" });
        }
    }

    #endregion
}

public class CreateContentFreshnessRequest
{
    public string ProjectId { get; set; } = "";
    public string? ContentType { get; set; }
    public string ContentIdentifier { get; set; } = "";
    public string? Title { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public int? RefreshIntervalDays { get; set; }
}

public class UpdateContentFreshnessRequest
{
    public string? Title { get; set; }
    public string? ContentType { get; set; }
    public int? RefreshIntervalDays { get; set; }
}
