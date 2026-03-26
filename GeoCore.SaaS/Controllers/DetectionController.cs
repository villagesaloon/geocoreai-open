using GeoCore.SaaS.Services.Detection;
using GeoCore.SaaS.Services.SiteAudit;
using GeoCore.SaaS.Services.Suggestion;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 检测系统 API 控制器
/// </summary>
[ApiController]
[Route("api/detection")]
public class DetectionController : ControllerBase
{
    private readonly ITaskQueueService _queueService;
    private readonly ITaskStatusService _statusService;
    private readonly SiteAuditService _auditService;
    private readonly ISuggestionGenerator _suggestionGenerator;
    private readonly ILogger<DetectionController> _logger;

    public DetectionController(
        ITaskQueueService queueService,
        ITaskStatusService statusService,
        SiteAuditService auditService,
        ISuggestionGenerator suggestionGenerator,
        ILogger<DetectionController> logger)
    {
        _queueService = queueService;
        _statusService = statusService;
        _auditService = auditService;
        _suggestionGenerator = suggestionGenerator;
        _logger = logger;
    }

    #region 检测任务 API

    /// <summary>
    /// 开始检测任务
    /// POST /api/detection/start
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartDetection([FromBody] StartDetectionRequest request)
    {
        if (request.ProjectId <= 0)
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "INVALID_PROJECT_ID", Message = "项目 ID 无效" }
            });
        }

        try
        {
            // 使用项目 ID 作为任务 ID
            var taskId = request.ProjectId;

            // 加入队列
            var position = await _queueService.EnqueueDetectionTaskAsync(taskId, request.TaskType ?? "full");

            // 设置初始状态
            await _statusService.SetStatusAsync(taskId, "queued", "任务排队中");
            await _statusService.SetProgressAsync(taskId, 0, "queued", "等待执行");

            _logger.LogInformation("检测任务已创建: {TaskId}, 项目: {ProjectId}", taskId, request.ProjectId);

            return Ok(new ApiResponse<StartDetectionResponse>
            {
                Success = true,
                Data = new StartDetectionResponse
                {
                    TaskId = taskId.ToString(),
                    QueuePosition = (int)position,
                    EstimatedTime = "5-10 分钟"
                },
                Message = "检测任务已加入队列"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建检测任务失败");
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "INTERNAL_ERROR", Message = "服务器内部错误" }
            });
        }
    }

    /// <summary>
    /// 查询任务状态
    /// GET /api/detection/status/{taskId}
    /// </summary>
    [HttpGet("status/{taskId:long}")]
    public async Task<IActionResult> GetStatus(long taskId)
    {
        var status = await _statusService.GetStatusAsync(taskId);

        if (status == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "TASK_NOT_FOUND", Message = "任务不存在" }
            });
        }

        var progress = await _statusService.GetProgressAsync(taskId);

        return Ok(new ApiResponse<TaskStatusResponse>
        {
            Success = true,
            Data = new TaskStatusResponse
            {
                TaskId = taskId.ToString(),
                Status = status,
                Progress = progress?.Progress ?? 0,
                CurrentPhase = progress?.Phase,
                Message = progress?.Message ?? GetStatusMessage(status),
                QueuePosition = status == "queued" ? 1 : null,
                EstimatedTimeRemaining = GetEstimatedTime(status, progress?.Progress ?? 0)
            }
        });
    }

    /// <summary>
    /// 取消任务
    /// POST /api/detection/cancel/{taskId}
    /// </summary>
    [HttpPost("cancel/{taskId:long}")]
    public async Task<IActionResult> CancelTask(long taskId)
    {
        var status = await _statusService.GetStatusAsync(taskId);

        if (status == null)
        {
            return NotFound(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "TASK_NOT_FOUND", Message = "任务不存在" }
            });
        }

        if (status == "completed" || status == "failed")
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "TASK_NOT_CANCELLABLE", Message = "任务已完成，无法取消" }
            });
        }

        // 从队列移除
        await _queueService.RemoveFromQueueAsync(taskId);
        await _statusService.SetStatusAsync(taskId, "cancelled", "任务已取消");

        _logger.LogInformation("任务已取消: {TaskId}", taskId);

        return Ok(new ApiResponse { Success = true, Message = "任务已取消" });
    }

    #endregion

    #region 网站审计 API

    /// <summary>
    /// 执行网站审计
    /// POST /api/detection/audit
    /// </summary>
    [HttpPost("audit")]
    public async Task<IActionResult> AuditWebsite([FromBody] WebsiteAuditRequest request)
    {
        if (string.IsNullOrEmpty(request.Url))
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "INVALID_URL", Message = "URL 不能为空" }
            });
        }

        try
        {
            var auditRequest = new SiteAuditRequest
            {
                Url = request.Url,
                IncludeTechnical = request.IncludeTechnical ?? true,
                IncludeContent = request.IncludeContent ?? true,
                IncludeEEAT = request.IncludeEEAT ?? true
            };

            var result = await _auditService.AuditSiteAsync(auditRequest);

            _logger.LogInformation("网站审计完成: {Url}, 分数: {Score}", request.Url, result.OverallScore);

            return Ok(new ApiResponse<WebsiteAuditResponse>
            {
                Success = true,
                Data = new WebsiteAuditResponse
                {
                    Url = request.Url,
                    OverallScore = result.OverallScore,
                    Grade = result.Grade,
                    Technical = result.Technical != null ? new TechnicalAuditSummary
                    {
                        Score = result.Technical.Score,
                        RobotsTxtExists = result.Technical.RobotsTxt?.Exists ?? false,
                        AiCrawlersAllowed = result.Technical.RobotsTxt?.AllAICrawlersAllowed ?? false,
                        SitemapExists = result.Technical.Sitemap?.Exists ?? false,
                        LlmsTxtExists = result.Technical.LlmsTxt?.Exists ?? false,
                        HttpsEnabled = result.Technical.HttpsCanonical?.IsHttps ?? false
                    } : null,
                    Content = result.Content != null ? new ContentAuditSummary
                    {
                        Score = result.Content.Score,
                        HasSchema = result.Content.Schema?.HasSchema ?? false,
                        H1Count = result.Content.HeadingStructure?.H1Count ?? 0,
                        H2Count = result.Content.HeadingStructure?.H2Count ?? 0,
                        HasTitle = result.Content.MetaTags?.HasTitle ?? false,
                        HasDescription = result.Content.MetaTags?.HasDescription ?? false
                    } : null,
                    EEAT = result.EEAT != null ? new EEATAuditSummary
                    {
                        Score = result.EEAT.Score,
                        HasAuthorInfo = result.EEAT.HasAuthorInfo,
                        HasPublishDate = result.EEAT.HasPublishDate,
                        HasCitations = result.EEAT.HasCitations,
                        ExternalLinkCount = result.EEAT.ExternalLinkCount
                    } : null,
                    IssuesCount = result.Issues.Count,
                    RecommendationsCount = result.Recommendations.Count,
                    AuditedAt = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网站审计失败: {Url}", request.Url);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "AUDIT_FAILED", Message = $"审计失败: {ex.Message}" }
            });
        }
    }

    /// <summary>
    /// 获取审计详情
    /// GET /api/detection/audit/detail
    /// </summary>
    [HttpGet("audit/detail")]
    public async Task<IActionResult> GetAuditDetail([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "INVALID_URL", Message = "URL 不能为空" }
            });
        }

        try
        {
            var auditRequest = new SiteAuditRequest
            {
                Url = url,
                IncludeTechnical = true,
                IncludeContent = true,
                IncludeEEAT = true
            };

            var result = await _auditService.AuditSiteAsync(auditRequest);

            return Ok(new ApiResponse<SiteAuditResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计详情失败: {Url}", url);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "AUDIT_FAILED", Message = $"审计失败: {ex.Message}" }
            });
        }
    }

    #endregion

    #region 建议 API

    /// <summary>
    /// 获取优化建议
    /// GET /api/detection/suggestions
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "INVALID_URL", Message = "URL 不能为空" }
            });
        }

        try
        {
            // 先执行审计
            var auditRequest = new SiteAuditRequest
            {
                Url = url,
                IncludeTechnical = true,
                IncludeContent = true,
                IncludeEEAT = true
            };

            var auditResult = await _auditService.AuditSiteAsync(auditRequest);

            // 构建检测上下文
            var context = new DetectionContextBuilder()
                .WithBrand("", url)
                .FromSiteAuditResult(auditResult)
                .Build();

            // 生成建议
            var suggestions = await _suggestionGenerator.GenerateAsync(context);

            // 统计
            var summary = new SuggestionSummary
            {
                Total = suggestions.Count,
                ByPriority = new Dictionary<string, int>
                {
                    ["high"] = suggestions.Count(s => s.Priority == "high"),
                    ["medium"] = suggestions.Count(s => s.Priority == "medium"),
                    ["low"] = suggestions.Count(s => s.Priority == "low")
                },
                ByCategory = suggestions.GroupBy(s => s.Category)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            _logger.LogInformation("生成建议完成: {Url}, 建议数: {Count}", url, suggestions.Count);

            return Ok(new ApiResponse<SuggestionsResponse>
            {
                Success = true,
                Data = new SuggestionsResponse
                {
                    Url = url,
                    AuditScore = auditResult.OverallScore,
                    AuditGrade = auditResult.Grade,
                    Suggestions = suggestions,
                    Summary = summary
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取建议失败: {Url}", url);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Error = new ApiError { Code = "SUGGESTION_FAILED", Message = $"获取建议失败: {ex.Message}" }
            });
        }
    }

    #endregion

    #region 辅助方法

    private static string GetStatusMessage(string status)
    {
        return status switch
        {
            "queued" => "任务排队中",
            "running" => "正在执行",
            "completed" => "检测完成",
            "failed" => "检测失败",
            "cancelled" => "已取消",
            _ => "未知状态"
        };
    }

    private static string? GetEstimatedTime(string status, int progress)
    {
        return status switch
        {
            "queued" => "约 5 分钟后开始",
            "running" => $"约 {Math.Max(1, (100 - progress) / 10)} 分钟",
            _ => null
        };
    }

    #endregion
}

#region API 模型

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ApiError? Error { get; set; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }
}

public class ApiError
{
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Details { get; set; }
}

public class StartDetectionRequest
{
    public long ProjectId { get; set; }
    public string? TaskType { get; set; }
    public bool ForceRefresh { get; set; }
}

public class StartDetectionResponse
{
    public string TaskId { get; set; } = "";
    public int QueuePosition { get; set; }
    public string EstimatedTime { get; set; } = "";
}

public class TaskStatusResponse
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public string? CurrentPhase { get; set; }
    public string Message { get; set; } = "";
    public int? QueuePosition { get; set; }
    public string? EstimatedTimeRemaining { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WebsiteAuditRequest
{
    public string Url { get; set; } = "";
    public bool? IncludeTechnical { get; set; }
    public bool? IncludeContent { get; set; }
    public bool? IncludeEEAT { get; set; }
}

public class WebsiteAuditResponse
{
    public string Url { get; set; } = "";
    public int OverallScore { get; set; }
    public string Grade { get; set; } = "";
    public TechnicalAuditSummary? Technical { get; set; }
    public ContentAuditSummary? Content { get; set; }
    public EEATAuditSummary? EEAT { get; set; }
    public int IssuesCount { get; set; }
    public int RecommendationsCount { get; set; }
    public DateTime AuditedAt { get; set; }
}

public class TechnicalAuditSummary
{
    public int Score { get; set; }
    public bool RobotsTxtExists { get; set; }
    public bool AiCrawlersAllowed { get; set; }
    public bool SitemapExists { get; set; }
    public bool LlmsTxtExists { get; set; }
    public bool HttpsEnabled { get; set; }
}

public class ContentAuditSummary
{
    public int Score { get; set; }
    public bool HasSchema { get; set; }
    public int H1Count { get; set; }
    public int H2Count { get; set; }
    public bool HasTitle { get; set; }
    public bool HasDescription { get; set; }
}

public class EEATAuditSummary
{
    public int Score { get; set; }
    public bool HasAuthorInfo { get; set; }
    public bool HasPublishDate { get; set; }
    public bool HasCitations { get; set; }
    public int ExternalLinkCount { get; set; }
}

public class SuggestionsResponse
{
    public string Url { get; set; } = "";
    public int AuditScore { get; set; }
    public string AuditGrade { get; set; } = "";
    public List<DetectionSuggestion> Suggestions { get; set; } = new();
    public SuggestionSummary Summary { get; set; } = new();
}

public class SuggestionSummary
{
    public int Total { get; set; }
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
}

#endregion
