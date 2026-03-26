using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.CitationTracking;
using GeoCore.SaaS.Services.CitationTracking.Models;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 引用追踪 API
/// </summary>
[ApiController]
[Route("api/citation")]
public class CitationTrackingController : ControllerBase
{
    private readonly ILogger<CitationTrackingController> _logger;
    private readonly CitationTrackerService _trackerService;
    private readonly KeywordExtractorService _keywordExtractor;
    private readonly CitationMonitoringRepository _repository;

    public CitationTrackingController(
        ILogger<CitationTrackingController> logger,
        CitationTrackerService trackerService,
        KeywordExtractorService keywordExtractor,
        CitationMonitoringRepository repository)
    {
        _logger = logger;
        _trackerService = trackerService;
        _keywordExtractor = keywordExtractor;
        _repository = repository;
    }

    /// <summary>
    /// 快速监测（不保存）
    /// </summary>
    [HttpPost("quick-monitor")]
    public async Task<IActionResult> QuickMonitor([FromBody] QuickMonitorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BrandName))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            if (request.Questions == null || !request.Questions.Any())
            {
                return BadRequest(new { error = "至少需要一个问题" });
            }

            var taskRequest = new MonitoringTaskRequest
            {
                BrandName = request.BrandName,
                BrandAliases = request.BrandAliases ?? new(),
                Competitors = request.Competitors ?? new(),
                Questions = request.Questions,
                TargetPlatforms = ParsePlatforms(request.Platforms)
            };

            var result = await _trackerService.QuickMonitorAsync(taskRequest, cancellationToken);

            return Ok(new
            {
                success = true,
                data = new
                {
                    result.TotalQueries,
                    result.CompletedQueries,
                    result.CitationFrequency,
                    result.BrandVisibilityScore,
                    result.ShareOfVoice,
                    result.PositiveSentimentRatio,
                    result.LinkedCitationRatio,
                    result.FirstPositionRatio,
                    platformMetrics = result.PlatformMetrics.Select(p => new
                    {
                        platform = p.Key.ToString(),
                        p.Value.TotalQueries,
                        p.Value.CitedCount,
                        p.Value.CitationFrequency,
                        p.Value.AverageVisibilityScore
                    }),
                    competitorShareOfVoice = result.CompetitorShareOfVoice,
                    results = result.Results.Select(r => new
                    {
                        platform = r.Platform.ToString(),
                        r.Question,
                        r.ResponsePreview,
                        r.IsCited,
                        position = r.Position.ToString(),
                        r.HasLink,
                        sentiment = r.Sentiment.ToString(),
                        r.VisibilityScore,
                        r.CompetitorCitations
                    })
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationTracking] Quick monitor failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 创建监测任务
    /// </summary>
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.BrandName))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var taskRequest = new MonitoringTaskRequest
            {
                BrandName = request.BrandName,
                BrandAliases = request.BrandAliases ?? new(),
                Competitors = request.Competitors ?? new(),
                Questions = request.Questions ?? new(),
                TargetPlatforms = ParsePlatforms(request.Platforms),
                ProjectId = request.ProjectId
            };

            var taskId = await _trackerService.CreateTaskAsync(taskRequest);

            return Ok(new { success = true, taskId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationTracking] Create task failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 执行监测任务
    /// </summary>
    [HttpPost("tasks/{taskId}/run")]
    public async Task<IActionResult> RunTask(int taskId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trackerService.RunTaskAsync(taskId, cancellationToken);

            return Ok(new
            {
                success = true,
                data = new
                {
                    result.TaskId,
                    result.RunId,
                    result.TotalQueries,
                    result.CompletedQueries,
                    result.CitationFrequency,
                    result.BrandVisibilityScore,
                    result.ShareOfVoice,
                    result.PositiveSentimentRatio,
                    result.LinkedCitationRatio
                }
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationTracking] Run task failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取 Dashboard 数据
    /// </summary>
    [HttpGet("tasks/{taskId}/dashboard")]
    public async Task<IActionResult> GetDashboard(int taskId)
    {
        try
        {
            var dashboard = await _trackerService.GetDashboardAsync(taskId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    metrics = new
                    {
                        dashboard.CitationFrequency,
                        dashboard.BrandVisibilityScore,
                        dashboard.ShareOfVoice,
                        dashboard.PositiveSentimentRatio,
                        dashboard.LinkedCitationRatio
                    },
                    targets = new
                    {
                        dashboard.CitationFrequencyTarget,
                        dashboard.ShareOfVoiceTarget,
                        dashboard.PositiveSentimentTarget,
                        dashboard.LinkedCitationTarget
                    },
                    trends = new
                    {
                        citationFrequency = dashboard.CitationFrequencyTrend,
                        brandVisibility = dashboard.BrandVisibilityTrend,
                        shareOfVoice = dashboard.ShareOfVoiceTrend
                    },
                    platformData = dashboard.PlatformData.Select(p => new
                    {
                        platform = p.Platform.ToString(),
                        p.TotalQueries,
                        p.CitedCount,
                        p.CitationFrequency,
                        p.Trend
                    }),
                    trendHistory = dashboard.TrendHistory.Select(t => new
                    {
                        date = t.Date.ToString("yyyy-MM-dd"),
                        t.CitationFrequency,
                        t.BrandVisibilityScore,
                        t.ShareOfVoice
                    }),
                    competitorComparison = dashboard.CompetitorComparison
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationTracking] Get dashboard failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取可用平台列表
    /// </summary>
    [HttpGet("platforms")]
    public IActionResult GetPlatforms([FromServices] IEnumerable<IPlatformAdapter> adapters)
    {
        var platforms = adapters.Select(a => new
        {
            name = a.Platform.ToString(),
            weight = a.Weight,
            isAvailable = a.IsAvailable
        });

        return Ok(new { success = true, platforms });
    }

    private List<AIPlatform> ParsePlatforms(List<string>? platforms)
    {
        if (platforms == null || !platforms.Any())
        {
            return new List<AIPlatform>
            {
                AIPlatform.ChatGPT,
                AIPlatform.Perplexity,
                AIPlatform.Claude,
                AIPlatform.Gemini,
                AIPlatform.Grok
            };
        }

        var result = new List<AIPlatform>();
        foreach (var p in platforms)
        {
            if (Enum.TryParse<AIPlatform>(p, true, out var platform))
            {
                result.Add(platform);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取关键词统计
    /// </summary>
    [HttpGet("keywords/{taskId}")]
    public async Task<IActionResult> GetKeywordStats(int taskId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await _repository.GetResultsByTaskIdAsync(taskId, 200);
            if (!results.Any())
            {
                return Ok(new { success = true, data = new List<object>() });
            }

            var responses = results.Select(r => r.Response).Where(r => !string.IsNullOrEmpty(r));
            var keywords = await _keywordExtractor.ExtractAndAggregateKeywordsAsync(responses, limit, cancellationToken);

            return Ok(new
            {
                success = true,
                data = keywords.Select(k => new
                {
                    k.Keyword,
                    k.Count,
                    type = k.Type.ToString()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Citation] Failed to get keyword stats for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取关键词统计失败" });
        }
    }

    /// <summary>
    /// 从文本中提取关键词
    /// </summary>
    [HttpPost("extract-keywords")]
    public async Task<IActionResult> ExtractKeywords([FromBody] ExtractKeywordsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new { error = "文本不能为空" });
            }

            var keywords = await _keywordExtractor.ExtractKeywordsAsync(request.Text, request.MaxKeywords, cancellationToken);

            return Ok(new
            {
                success = true,
                data = keywords.Select(k => new
                {
                    k.Keyword,
                    k.Count,
                    type = k.Type.ToString()
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Citation] Failed to extract keywords");
            return StatusCode(500, new { error = "提取关键词失败" });
        }
    }

    /// <summary>
    /// 单关键词分析 - 查看特定品牌/关键词在各 LLM 的提及情况
    /// </summary>
    [HttpGet("keyword-analysis/{taskId}")]
    public async Task<IActionResult> GetKeywordAnalysis(
        int taskId, 
        [FromQuery] string keyword, 
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { error = "关键词不能为空" });
            }

            var results = await _repository.GetResultsByTaskIdAsync(taskId, limit);
            if (!results.Any())
            {
                return Ok(new { success = true, data = new KeywordAnalysisResult() });
            }

            // 按平台分组统计
            var platformStats = new Dictionary<string, PlatformKeywordStats>();
            var mentionDetails = new List<KeywordMentionDetail>();

            foreach (var result in results)
            {
                if (string.IsNullOrEmpty(result.Response))
                    continue;

                var platform = result.Platform;
                var isMentioned = result.Response.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                
                if (!platformStats.ContainsKey(platform))
                {
                    platformStats[platform] = new PlatformKeywordStats
                    {
                        Platform = platform,
                        TotalResponses = 0,
                        MentionCount = 0,
                        FirstMentionPositions = new List<int>()
                    };
                }

                platformStats[platform].TotalResponses++;
                
                if (isMentioned)
                {
                    platformStats[platform].MentionCount++;
                    
                    // 计算首次提及位置
                    var position = result.Response.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                    var relativePosition = (double)position / result.Response.Length * 100;
                    platformStats[platform].FirstMentionPositions.Add((int)relativePosition);

                    mentionDetails.Add(new KeywordMentionDetail
                    {
                        Platform = platform,
                        Question = result.Question,
                        Position = (int)relativePosition,
                        Context = ExtractContext(result.Response, keyword, 100),
                        CreatedAt = result.CreatedAt
                    });
                }
            }

            // 计算各平台的提及率和平均位置
            foreach (var stat in platformStats.Values)
            {
                stat.MentionRate = stat.TotalResponses > 0 
                    ? (double)stat.MentionCount / stat.TotalResponses * 100 
                    : 0;
                stat.AveragePosition = stat.FirstMentionPositions.Any() 
                    ? stat.FirstMentionPositions.Average() 
                    : -1;
            }

            var analysisResult = new KeywordAnalysisResult
            {
                Keyword = keyword,
                TotalResponses = results.Count,
                TotalMentions = mentionDetails.Count,
                OverallMentionRate = results.Count > 0 
                    ? (double)mentionDetails.Count / results.Count * 100 
                    : 0,
                PlatformStats = platformStats.Values.OrderByDescending(p => p.MentionRate).ToList(),
                RecentMentions = mentionDetails.OrderByDescending(m => m.CreatedAt).Take(10).ToList()
            };

            return Ok(new { success = true, data = analysisResult });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Citation] Failed to analyze keyword {Keyword} for task {TaskId}", keyword, taskId);
            return StatusCode(500, new { error = "关键词分析失败" });
        }
    }

    /// <summary>
    /// 提取关键词上下文
    /// </summary>
    private string ExtractContext(string text, string keyword, int contextLength)
    {
        var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "";

        var start = Math.Max(0, index - contextLength / 2);
        var end = Math.Min(text.Length, index + keyword.Length + contextLength / 2);
        
        var context = text.Substring(start, end - start);
        if (start > 0) context = "..." + context;
        if (end < text.Length) context = context + "...";
        
        return context;
    }
}

public class KeywordAnalysisResult
{
    public string Keyword { get; set; } = "";
    public int TotalResponses { get; set; }
    public int TotalMentions { get; set; }
    public double OverallMentionRate { get; set; }
    public List<PlatformKeywordStats> PlatformStats { get; set; } = new();
    public List<KeywordMentionDetail> RecentMentions { get; set; } = new();
}

public class PlatformKeywordStats
{
    public string Platform { get; set; } = "";
    public int TotalResponses { get; set; }
    public int MentionCount { get; set; }
    public double MentionRate { get; set; }
    public double AveragePosition { get; set; }
    public List<int> FirstMentionPositions { get; set; } = new();
}

public class KeywordMentionDetail
{
    public string Platform { get; set; } = "";
    public string Question { get; set; } = "";
    public int Position { get; set; }
    public string Context { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class QuickMonitorRequest
{
    public string BrandName { get; set; } = "";
    public List<string>? BrandAliases { get; set; }
    public List<string>? Competitors { get; set; }
    public List<string> Questions { get; set; } = new();
    public List<string>? Platforms { get; set; }
}

public class CreateTaskRequest
{
    public string BrandName { get; set; } = "";
    public List<string>? BrandAliases { get; set; }
    public List<string>? Competitors { get; set; }
    public List<string>? Questions { get; set; }
    public List<string>? Platforms { get; set; }
    public string? ProjectId { get; set; }
    public string Frequency { get; set; } = "weekly";
}

public class ExtractKeywordsRequest
{
    public string Text { get; set; } = "";
    public int MaxKeywords { get; set; } = 20;
}
