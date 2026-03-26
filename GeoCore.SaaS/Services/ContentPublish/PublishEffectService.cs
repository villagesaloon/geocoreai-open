using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Text.Json;

namespace GeoCore.SaaS.Services.ContentPublish;

/// <summary>
/// 发布效果关联服务 - SaaS 前台
/// Phase 8.14: 将发布历史与引用监测关联，追踪内容发布后的 AI 引用效果
/// </summary>
public class PublishEffectService
{
    private readonly PublishHistoryRepository _historyRepo;
    private readonly CitationMonitoringRepository _citationRepo;
    private readonly ILogger<PublishEffectService> _logger;

    public PublishEffectService(
        PublishHistoryRepository historyRepo,
        CitationMonitoringRepository citationRepo,
        ILogger<PublishEffectService> logger)
    {
        _historyRepo = historyRepo;
        _citationRepo = citationRepo;
        _logger = logger;
    }

    /// <summary>
    /// 获取发布内容的引用效果摘要
    /// </summary>
    public async Task<PublishEffectSummary> GetEffectSummaryAsync(int userId, int? publishHistoryId = null)
    {
        var history = await _historyRepo.GetByUserIdAsync(userId, 100);
        var publishedItems = history.Where(h => h.Status == "published" && h.PublishedAt.HasValue).ToList();

        if (publishHistoryId.HasValue)
        {
            publishedItems = publishedItems.Where(h => h.Id == publishHistoryId.Value).ToList();
        }

        var summary = new PublishEffectSummary
        {
            TotalPublished = publishedItems.Count,
            PlatformBreakdown = new Dictionary<string, PlatformEffectStats>()
        };

        foreach (var platform in publishedItems.GroupBy(p => p.Platform))
        {
            var platformStats = new PlatformEffectStats
            {
                Platform = platform.Key,
                PublishCount = platform.Count(),
                Items = new List<PublishEffectItem>()
            };

            foreach (var item in platform)
            {
                var effectItem = new PublishEffectItem
                {
                    PublishHistoryId = item.Id,
                    Platform = item.Platform,
                    PlatformUrl = item.PlatformUrl,
                    PublishedAt = item.PublishedAt,
                    DaysSincePublish = item.PublishedAt.HasValue
                        ? (int)(DateTime.UtcNow - item.PublishedAt.Value).TotalDays
                        : 0
                };

                // 查找关联的引用监测结果
                var citationResults = await FindRelatedCitationsAsync(item);
                if (citationResults.Any())
                {
                    effectItem.CitationCount = citationResults.Count;
                    effectItem.CitedPlatforms = citationResults.Select(c => c.Platform).Distinct().ToList();
                    effectItem.FirstCitedAt = citationResults.Min(c => c.CreatedAt);
                    effectItem.LatestCitedAt = citationResults.Max(c => c.CreatedAt);
                    effectItem.AverageCitationPosition = citationResults.Average(c => c.PositionRatio);

                    summary.TotalCitations += citationResults.Count;
                    platformStats.TotalCitations += citationResults.Count;
                }

                platformStats.Items.Add(effectItem);
            }

            summary.PlatformBreakdown[platform.Key] = platformStats;
        }

        summary.OverallCitationRate = summary.TotalPublished > 0
            ? (double)summary.PlatformBreakdown.Values.Sum(p => p.Items.Count(i => i.CitationCount > 0)) / summary.TotalPublished
            : 0;

        return summary;
    }

    /// <summary>
    /// 获取单个发布记录的详细引用效果
    /// </summary>
    public async Task<PublishEffectDetail?> GetEffectDetailAsync(int userId, int publishHistoryId)
    {
        var historyList = await _historyRepo.GetByUserIdAsync(userId, 100);
        var history = historyList.FirstOrDefault(h => h.Id == publishHistoryId);

        if (history == null)
            return null;

        var detail = new PublishEffectDetail
        {
            PublishHistoryId = history.Id,
            Platform = history.Platform,
            PlatformUrl = history.PlatformUrl,
            PublishedAt = history.PublishedAt,
            Status = history.Status,
            Citations = new List<CitationDetail>()
        };

        var citationResults = await FindRelatedCitationsAsync(history);
        foreach (var citation in citationResults)
        {
            detail.Citations.Add(new CitationDetail
            {
                AIPlatform = citation.Platform,
                Question = citation.Question,
                IsCited = citation.IsCited,
                CitationPosition = citation.CitationPosition,
                PositionRatio = citation.PositionRatio,
                HasLink = citation.HasLink,
                DetectedAt = citation.CreatedAt,
                ResponseSnippet = citation.Response.Length > 500
                    ? citation.Response[..500] + "..."
                    : citation.Response
            });
        }

        detail.TotalCitations = detail.Citations.Count;
        detail.CitedByPlatforms = detail.Citations.Select(c => c.AIPlatform).Distinct().ToList();

        return detail;
    }

    /// <summary>
    /// 创建发布内容的引用监测任务
    /// </summary>
    public async Task<int> CreateMonitoringTaskAsync(int userId, int publishHistoryId, List<string> questions)
    {
        var historyList = await _historyRepo.GetByUserIdAsync(userId, 100);
        var history = historyList.FirstOrDefault(h => h.Id == publishHistoryId);

        if (history == null || string.IsNullOrEmpty(history.PlatformUrl))
        {
            throw new ArgumentException("发布记录不存在或无有效 URL");
        }

        // 从 URL 提取品牌/内容标识
        var brandName = ExtractBrandFromUrl(history.PlatformUrl);

        var task = new CitationMonitoringTaskEntity
        {
            ProjectId = $"publish_{publishHistoryId}",
            BrandName = brandName,
            BrandAliases = "[]",
            Competitors = "[]",
            Questions = JsonSerializer.Serialize(questions),
            TargetPlatforms = JsonSerializer.Serialize(new[] { "chatgpt", "perplexity", "claude", "gemini" }),
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var taskId = await _citationRepo.CreateTaskAsync(task);
        _logger.LogInformation("为发布记录 {HistoryId} 创建引用监测任务 {TaskId}", publishHistoryId, taskId);

        return taskId;
    }

    /// <summary>
    /// 获取发布效果趋势（按时间）
    /// </summary>
    public async Task<List<EffectTrendPoint>> GetEffectTrendAsync(int userId, int days = 30)
    {
        var history = await _historyRepo.GetByUserIdAsync(userId, 200);
        var publishedItems = history
            .Where(h => h.Status == "published" && h.PublishedAt.HasValue)
            .Where(h => h.PublishedAt!.Value >= DateTime.UtcNow.AddDays(-days))
            .ToList();

        var trend = new List<EffectTrendPoint>();
        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
        {
            var dayPublishes = publishedItems.Where(p => p.PublishedAt!.Value.Date == date).ToList();
            var dayCitations = 0;

            foreach (var publish in dayPublishes)
            {
                var citations = await FindRelatedCitationsAsync(publish);
                dayCitations += citations.Count;
            }

            trend.Add(new EffectTrendPoint
            {
                Date = date,
                PublishCount = dayPublishes.Count,
                CitationCount = dayCitations
            });
        }

        return trend;
    }

    #region Private Methods

    private async Task<List<CitationResultEntity>> FindRelatedCitationsAsync(PublishHistoryEntity history)
    {
        if (string.IsNullOrEmpty(history.PlatformUrl))
            return new List<CitationResultEntity>();

        // 从 URL 提取关键词进行匹配
        var keywords = ExtractKeywordsFromUrl(history.PlatformUrl);
        if (!keywords.Any())
            return new List<CitationResultEntity>();

        // 查找包含这些关键词的引用结果
        var allResults = await _citationRepo.GetResultsByTaskIdAsync(0); // 获取所有结果
        var matchedResults = new List<CitationResultEntity>();

        foreach (var result in allResults)
        {
            if (result.CreatedAt < history.PublishedAt)
                continue; // 只考虑发布后的引用

            var responseText = result.Response.ToLower();
            if (keywords.Any(k => responseText.Contains(k.ToLower())))
            {
                matchedResults.Add(result);
            }

            // 检查是否直接引用了 URL
            if (!string.IsNullOrEmpty(history.PlatformUrl) &&
                responseText.Contains(history.PlatformUrl.ToLower()))
            {
                if (!matchedResults.Contains(result))
                    matchedResults.Add(result);
            }
        }

        return matchedResults;
    }

    private static string ExtractBrandFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts.Length > 0)
            {
                // Reddit: /r/subreddit/comments/xxx/title
                if (url.Contains("reddit.com") && pathParts.Length > 4)
                    return pathParts[4].Replace("_", " ");

                // LinkedIn: /posts/xxx
                if (url.Contains("linkedin.com"))
                    return "LinkedIn Post";

                // Medium: /@username/title
                if (url.Contains("medium.com") && pathParts.Length > 1)
                    return pathParts[^1].Replace("-", " ");

                // Twitter: /username/status/xxx
                if (url.Contains("twitter.com") || url.Contains("x.com"))
                    return "Tweet";
            }
            return uri.Host;
        }
        catch
        {
            return "Unknown";
        }
    }

    private static List<string> ExtractKeywordsFromUrl(string url)
    {
        var keywords = new List<string>();
        try
        {
            var uri = new Uri(url);
            var pathParts = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in pathParts)
            {
                // 跳过常见的路径部分
                if (part is "r" or "u" or "comments" or "posts" or "status" or "p")
                    continue;

                // 提取有意义的关键词
                var words = part.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var word in words)
                {
                    if (word.Length > 3 && !long.TryParse(word, out _))
                    {
                        keywords.Add(word);
                    }
                }
            }
        }
        catch { }

        return keywords.Distinct().Take(5).ToList();
    }

    #endregion
}

#region Models

public class PublishEffectSummary
{
    public int TotalPublished { get; set; }
    public int TotalCitations { get; set; }
    public double OverallCitationRate { get; set; }
    public Dictionary<string, PlatformEffectStats> PlatformBreakdown { get; set; } = new();
}

public class PlatformEffectStats
{
    public string Platform { get; set; } = "";
    public int PublishCount { get; set; }
    public int TotalCitations { get; set; }
    public List<PublishEffectItem> Items { get; set; } = new();
}

public class PublishEffectItem
{
    public int PublishHistoryId { get; set; }
    public string Platform { get; set; } = "";
    public string? PlatformUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int DaysSincePublish { get; set; }
    public int CitationCount { get; set; }
    public List<string> CitedPlatforms { get; set; } = new();
    public DateTime? FirstCitedAt { get; set; }
    public DateTime? LatestCitedAt { get; set; }
    public double AverageCitationPosition { get; set; }
}

public class PublishEffectDetail
{
    public int PublishHistoryId { get; set; }
    public string Platform { get; set; } = "";
    public string? PlatformUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string Status { get; set; } = "";
    public int TotalCitations { get; set; }
    public List<string> CitedByPlatforms { get; set; } = new();
    public List<CitationDetail> Citations { get; set; } = new();
}

public class CitationDetail
{
    public string AIPlatform { get; set; } = "";
    public string Question { get; set; } = "";
    public bool IsCited { get; set; }
    public string CitationPosition { get; set; } = "";
    public double PositionRatio { get; set; }
    public bool HasLink { get; set; }
    public DateTime DetectedAt { get; set; }
    public string ResponseSnippet { get; set; } = "";
}

public class EffectTrendPoint
{
    public DateTime Date { get; set; }
    public int PublishCount { get; set; }
    public int CitationCount { get; set; }
}

#endregion
