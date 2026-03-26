using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Services.ContentFreshness;

/// <summary>
/// 内容新鲜度监测服务
/// 功能：4.36 内容新鲜度评分、4.37 新鲜度阈值告警、4.38 更新建议生成
/// </summary>
public class ContentFreshnessService
{
    private readonly ILogger<ContentFreshnessService> _logger;
    private readonly CitationMonitoringRepository? _repository;

    public ContentFreshnessService(
        ILogger<ContentFreshnessService> logger,
        CitationMonitoringRepository? repository = null)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 生成内容新鲜度分析报告
    /// </summary>
    public async Task<ContentFreshnessReport> GenerateReportAsync(ContentFreshnessRequest request)
    {
        _logger.LogInformation("Generating content freshness report for task {TaskId}, brand {Brand}",
            request.TaskId, request.Brand);

        var thresholds = request.Thresholds ?? new FreshnessThresholds();
        var results = await GetCitationResultsAsync(request.TaskId);

        var report = new ContentFreshnessReport
        {
            Brand = request.Brand,
            TaskId = request.TaskId
        };

        // 分析内容新鲜度
        report.Items = AnalyzeContentFreshness(results, request.Brand, thresholds);

        // 计算分布统计
        report.Distribution = CalculateDistribution(report.Items);

        // 计算整体评分
        report.OverallScore = CalculateOverallScore(report.Items);
        report.FreshnessLevel = DetermineFreshnessLevel(report.OverallScore);

        // 生成告警
        if (request.IncludeAlerts)
        {
            report.Alerts = GenerateAlerts(report, thresholds);
        }

        // 生成更新建议
        report.Suggestions = GenerateSuggestions(report, thresholds);

        // 趋势分析
        if (request.IncludeTrends)
        {
            report.Trend = AnalyzeTrend(results, request.Brand);
        }

        // 生成摘要
        report.Summary = GenerateSummary(report);

        return report;
    }

    /// <summary>
    /// 分析内容新鲜度
    /// </summary>
    public List<ContentFreshnessItem> AnalyzeContentFreshness(
        List<CitationResultEntity> results,
        string brand,
        FreshnessThresholds thresholds)
    {
        var items = new List<ContentFreshnessItem>();
        var contentGroups = results
            .Where(r => r.IsCited && !string.IsNullOrEmpty(r.DetectedLink))
            .GroupBy(r => ExtractContentIdentifier(r.DetectedLink ?? ""))
            .Where(g => !string.IsNullOrEmpty(g.Key));

        foreach (var group in contentGroups)
        {
            var firstResult = group.First();
            var contentAge = EstimateContentAge(group.ToList());
            var freshnessScore = CalculateFreshnessScore(contentAge, thresholds);
            var freshnessLevel = DetermineItemFreshnessLevel(contentAge, thresholds);

            var item = new ContentFreshnessItem
            {
                ContentUrl = group.Key,
                Title = ExtractTitle(firstResult.Response, group.Key),
                ContentType = DetermineContentType(group.Key),
                AgeDays = contentAge,
                FreshnessScore = freshnessScore,
                FreshnessLevel = freshnessLevel,
                CitationCount = group.Count(),
                CitedByPlatforms = group.Select(r => r.Platform).Distinct().ToList(),
                NeedsUpdate = freshnessLevel == "stale" || freshnessLevel == "outdated",
                UpdatePriority = CalculateUpdatePriority(freshnessScore, group.Count()),
                UpdateRecommendation = GenerateItemRecommendation(freshnessLevel, contentAge, group.Count())
            };

            // 估算发布和更新日期
            var oldestCitation = group.Min(r => r.CreatedAt);
            item.PublishedDate = oldestCitation.AddDays(-contentAge);
            item.LastUpdated = item.PublishedDate;

            items.Add(item);
        }

        return items.OrderByDescending(i => i.UpdatePriority).ToList();
    }

    /// <summary>
    /// 计算新鲜度评分
    /// </summary>
    public double CalculateFreshnessScore(int ageDays, FreshnessThresholds thresholds)
    {
        if (ageDays <= thresholds.FreshDays)
        {
            // 6个月内：100-80分
            return 100 - (ageDays * 20.0 / thresholds.FreshDays);
        }
        else if (ageDays <= thresholds.GoodDays)
        {
            // 6-12个月：80-60分
            var daysInRange = ageDays - thresholds.FreshDays;
            var rangeSize = thresholds.GoodDays - thresholds.FreshDays;
            return 80 - (daysInRange * 20.0 / rangeSize);
        }
        else if (ageDays <= thresholds.StaleDays)
        {
            // 12-24个月：60-30分
            var daysInRange = ageDays - thresholds.GoodDays;
            var rangeSize = thresholds.StaleDays - thresholds.GoodDays;
            return 60 - (daysInRange * 30.0 / rangeSize);
        }
        else
        {
            // 24个月以上：30-0分
            var extraDays = ageDays - thresholds.StaleDays;
            return Math.Max(0, 30 - (extraDays * 30.0 / 365));
        }
    }

    /// <summary>
    /// 计算分布统计
    /// </summary>
    public FreshnessDistribution CalculateDistribution(List<ContentFreshnessItem> items)
    {
        if (!items.Any())
        {
            return new FreshnessDistribution();
        }

        var total = items.Count;
        var freshCount = items.Count(i => i.FreshnessLevel == "fresh");
        var goodCount = items.Count(i => i.FreshnessLevel == "good");
        var staleCount = items.Count(i => i.FreshnessLevel == "stale");
        var outdatedCount = items.Count(i => i.FreshnessLevel == "outdated");

        var ages = items.Select(i => (double)i.AgeDays).OrderBy(a => a).ToList();

        return new FreshnessDistribution
        {
            FreshRate = (double)freshCount / total,
            GoodRate = (double)goodCount / total,
            StaleRate = (double)staleCount / total,
            OutdatedRate = (double)outdatedCount / total,
            FreshCount = freshCount,
            GoodCount = goodCount,
            StaleCount = staleCount,
            OutdatedCount = outdatedCount,
            AverageAgeDays = ages.Average(),
            MedianAgeDays = ages.Count % 2 == 0
                ? (ages[ages.Count / 2 - 1] + ages[ages.Count / 2]) / 2
                : ages[ages.Count / 2]
        };
    }

    /// <summary>
    /// 计算整体评分
    /// </summary>
    public double CalculateOverallScore(List<ContentFreshnessItem> items)
    {
        if (!items.Any()) return 0;

        // 加权平均：引用次数越多的内容权重越高
        var totalWeight = items.Sum(i => Math.Max(1, i.CitationCount));
        var weightedSum = items.Sum(i => i.FreshnessScore * Math.Max(1, i.CitationCount));

        return weightedSum / totalWeight;
    }

    /// <summary>
    /// 生成告警
    /// </summary>
    public List<FreshnessAlert> GenerateAlerts(ContentFreshnessReport report, FreshnessThresholds thresholds)
    {
        var alerts = new List<FreshnessAlert>();

        // 过时内容告警
        if (report.Distribution.OutdatedCount > 0)
        {
            alerts.Add(new FreshnessAlert
            {
                Level = "critical",
                Type = "outdated_content",
                Title = "发现过时内容",
                Description = $"有 {report.Distribution.OutdatedCount} 个被引用的内容已超过 24 个月未更新",
                AffectedCount = report.Distribution.OutdatedCount,
                SuggestedAction = "立即审查并更新这些内容，或考虑归档不再相关的内容"
            });
        }

        // 陈旧内容告警
        if (report.Distribution.StaleCount > 0)
        {
            alerts.Add(new FreshnessAlert
            {
                Level = "warning",
                Type = "stale_content",
                Title = "内容需要刷新",
                Description = $"有 {report.Distribution.StaleCount} 个被引用的内容已超过 12 个月未更新",
                AffectedCount = report.Distribution.StaleCount,
                SuggestedAction = "计划在未来 1-2 个月内更新这些内容"
            });
        }

        // 高引用但陈旧的内容
        var highCitationStale = report.Items
            .Where(i => i.CitationCount >= 3 && (i.FreshnessLevel == "stale" || i.FreshnessLevel == "outdated"))
            .ToList();

        if (highCitationStale.Any())
        {
            alerts.Add(new FreshnessAlert
            {
                Level = "critical",
                Type = "high_citation_stale",
                Title = "高引用内容需要紧急更新",
                Description = $"有 {highCitationStale.Count} 个高引用内容已过时，可能影响 AI 引用质量",
                AffectedCount = highCitationStale.Count,
                SuggestedAction = "优先更新这些高价值内容以维持 AI 引用率"
            });
        }

        // 整体新鲜度下降
        if (report.OverallScore < 50)
        {
            alerts.Add(new FreshnessAlert
            {
                Level = "warning",
                Type = "low_overall_freshness",
                Title = "整体内容新鲜度偏低",
                Description = $"整体新鲜度评分为 {report.OverallScore:F0}，低于推荐的 50 分",
                AffectedCount = report.Items.Count,
                SuggestedAction = "制定内容更新计划，优先更新高引用内容"
            });
        }

        // ChatGPT 偏好周期提醒
        var chatGptOptimal = report.Items
            .Where(i => i.AgeDays >= thresholds.ChatGptPreferredMinDays &&
                       i.AgeDays <= thresholds.ChatGptPreferredMaxDays)
            .ToList();

        if (chatGptOptimal.Any())
        {
            alerts.Add(new FreshnessAlert
            {
                Level = "info",
                Type = "chatgpt_optimal_window",
                Title = "内容处于 ChatGPT 偏好更新周期",
                Description = $"有 {chatGptOptimal.Count} 个内容处于 ChatGPT 偏好的 393-458 天更新周期内",
                AffectedCount = chatGptOptimal.Count,
                SuggestedAction = "考虑在此窗口期内更新这些内容以获得更好的 ChatGPT 引用"
            });
        }

        return alerts.OrderBy(a => a.Level == "critical" ? 0 : a.Level == "warning" ? 1 : 2).ToList();
    }

    /// <summary>
    /// 生成更新建议
    /// </summary>
    public List<UpdateSuggestion> GenerateSuggestions(ContentFreshnessReport report, FreshnessThresholds thresholds)
    {
        var suggestions = new List<UpdateSuggestion>();

        // 为过时内容生成建议
        var outdatedItems = report.Items.Where(i => i.FreshnessLevel == "outdated").ToList();
        foreach (var item in outdatedItems.Take(5))
        {
            suggestions.Add(new UpdateSuggestion
            {
                Type = item.CitationCount >= 2 ? "rewrite" : "archive",
                TargetContent = item.ContentUrl,
                Title = item.CitationCount >= 2 ? "重写高价值过时内容" : "归档或删除过时内容",
                Description = $"内容已有 {item.AgeDays} 天未更新，{(item.CitationCount >= 2 ? "但仍有较高引用价值" : "且引用较少")}",
                ActionSteps = item.CitationCount >= 2
                    ? new List<string>
                    {
                        "审查内容准确性和时效性",
                        "更新过时的数据和统计",
                        "添加最新的行业趋势和见解",
                        "优化结构以提高 AI 可提取性",
                        "更新发布日期并重新提交索引"
                    }
                    : new List<string>
                    {
                        "评估内容是否仍有价值",
                        "如无价值，设置 301 重定向到相关内容",
                        "如有价值，进行内容合并"
                    },
                Priority = item.CitationCount >= 2 ? 10 : 5,
                ExpectedImpact = item.CitationCount >= 2 ? "维持或提升 AI 引用率" : "清理内容库",
                Effort = item.CitationCount >= 2 ? "high" : "low",
                SuggestedTimeline = "2 周内"
            });
        }

        // 为陈旧内容生成建议
        var staleItems = report.Items.Where(i => i.FreshnessLevel == "stale").ToList();
        foreach (var item in staleItems.Take(5))
        {
            suggestions.Add(new UpdateSuggestion
            {
                Type = "refresh",
                TargetContent = item.ContentUrl,
                Title = "刷新陈旧内容",
                Description = $"内容已有 {item.AgeDays} 天未更新，被 {item.CitationCount} 个 AI 平台引用",
                ActionSteps = new List<string>
                {
                    "检查并更新过时信息",
                    "添加最新数据和案例",
                    "优化标题和元描述",
                    "检查内部链接是否有效",
                    "更新最后修改日期"
                },
                Priority = Math.Min(10, 5 + item.CitationCount),
                ExpectedImpact = "提升内容新鲜度评分",
                Effort = "medium",
                SuggestedTimeline = "1 个月内"
            });
        }

        // ChatGPT 偏好周期内容建议
        var chatGptWindow = report.Items
            .Where(i => i.AgeDays >= thresholds.ChatGptPreferredMinDays - 30 &&
                       i.AgeDays <= thresholds.ChatGptPreferredMaxDays)
            .OrderByDescending(i => i.CitationCount)
            .Take(3)
            .ToList();

        foreach (var item in chatGptWindow)
        {
            suggestions.Add(new UpdateSuggestion
            {
                Type = "refresh",
                TargetContent = item.ContentUrl,
                Title = "利用 ChatGPT 偏好更新窗口",
                Description = $"内容处于 ChatGPT 偏好的 393-458 天更新周期，是更新的最佳时机",
                ActionSteps = new List<string>
                {
                    "进行内容审查和小幅更新",
                    "添加最新统计数据",
                    "更新发布日期",
                    "重新提交到搜索引擎索引"
                },
                Priority = 8,
                ExpectedImpact = "提升 ChatGPT 引用概率",
                Effort = "low",
                SuggestedTimeline = "2 周内"
            });
        }

        // 整体建议
        if (report.Distribution.StaleRate + report.Distribution.OutdatedRate > 0.3)
        {
            suggestions.Add(new UpdateSuggestion
            {
                Type = "strategy",
                TargetContent = "整体内容策略",
                Title = "建立内容更新机制",
                Description = $"超过 {((report.Distribution.StaleRate + report.Distribution.OutdatedRate) * 100):F0}% 的被引用内容需要更新",
                ActionSteps = new List<string>
                {
                    "建立内容审查日历（每季度）",
                    "设置内容新鲜度监控告警",
                    "优先更新高引用内容",
                    "考虑使用常青内容策略",
                    "建立内容更新 SOP"
                },
                Priority = 9,
                ExpectedImpact = "系统性提升内容新鲜度",
                Effort = "high",
                SuggestedTimeline = "持续进行"
            });
        }

        return suggestions.OrderByDescending(s => s.Priority).ToList();
    }

    /// <summary>
    /// 分析趋势
    /// </summary>
    public FreshnessTrend AnalyzeTrend(List<CitationResultEntity> results, string brand)
    {
        var trend = new FreshnessTrend();

        // 按周分组分析
        var weeklyGroups = results
            .Where(r => r.IsCited)
            .GroupBy(r => new { r.CreatedAt.Year, Week = r.CreatedAt.DayOfYear / 7 })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Week)
            .ToList();

        var thresholds = new FreshnessThresholds();

        foreach (var group in weeklyGroups)
        {
            var items = AnalyzeContentFreshness(group.ToList(), brand, thresholds);
            if (!items.Any()) continue;

            trend.DataPoints.Add(new FreshnessTrendPoint
            {
                Date = group.First().CreatedAt.Date,
                AverageScore = items.Average(i => i.FreshnessScore),
                TotalContent = items.Count,
                FreshContent = items.Count(i => i.FreshnessLevel == "fresh" || i.FreshnessLevel == "good"),
                StaleContent = items.Count(i => i.FreshnessLevel == "stale" || i.FreshnessLevel == "outdated")
            });
        }

        // 确定趋势方向
        if (trend.DataPoints.Count >= 2)
        {
            var recentAvg = trend.DataPoints.TakeLast(3).Average(p => p.AverageScore);
            var earlierAvg = trend.DataPoints.Take(3).Average(p => p.AverageScore);

            if (recentAvg > earlierAvg + 5)
            {
                trend.Direction = "improving";
                trend.Analysis = "内容新鲜度呈上升趋势，继续保持定期更新策略";
            }
            else if (recentAvg < earlierAvg - 5)
            {
                trend.Direction = "declining";
                trend.Analysis = "内容新鲜度呈下降趋势，建议加快内容更新频率";
            }
            else
            {
                trend.Direction = "stable";
                trend.Analysis = "内容新鲜度保持稳定，建议关注即将过时的内容";
            }
        }
        else
        {
            trend.Analysis = "数据不足以分析趋势，建议持续监测";
        }

        return trend;
    }

    #region Helper Methods

    private async Task<List<CitationResultEntity>> GetCitationResultsAsync(int taskId)
    {
        if (_repository == null)
        {
            return new List<CitationResultEntity>();
        }

        try
        {
            return await _repository.GetResultsByTaskIdAsync(taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get citation results for task {TaskId}", taskId);
            return new List<CitationResultEntity>();
        }
    }

    private string ExtractContentIdentifier(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";

        try
        {
            var uri = new Uri(url);
            // 返回域名+路径作为标识
            return $"{uri.Host}{uri.AbsolutePath}".TrimEnd('/');
        }
        catch
        {
            return url;
        }
    }

    private string ExtractTitle(string response, string url)
    {
        // 尝试从 URL 提取标题
        var path = url.Split('/').LastOrDefault() ?? "";
        path = path.Replace("-", " ").Replace("_", " ");

        // 首字母大写
        if (!string.IsNullOrEmpty(path))
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(path.ToLower());
        }

        return "未知标题";
    }

    private string DetermineContentType(string url)
    {
        var lowerUrl = url.ToLower();

        if (lowerUrl.Contains("/blog/") || lowerUrl.Contains("/article/"))
            return "article";
        if (lowerUrl.Contains("/product/") || lowerUrl.Contains("/shop/"))
            return "product";
        if (lowerUrl.Contains("/doc/") || lowerUrl.Contains("/documentation/"))
            return "documentation";
        if (lowerUrl.Contains("/faq/") || lowerUrl.Contains("/help/"))
            return "faq";
        if (lowerUrl.Contains("/news/"))
            return "news";

        return "page";
    }

    private int EstimateContentAge(List<CitationResultEntity> citations)
    {
        // 基于引用时间估算内容年龄
        // 假设内容在首次被引用前已存在一段时间
        var oldestCitation = citations.Min(c => c.CreatedAt);
        var daysSinceFirstCitation = (DateTime.UtcNow - oldestCitation).Days;

        // 估算：内容通常在发布后 30-90 天开始被 AI 引用
        return daysSinceFirstCitation + 60;
    }

    private string DetermineItemFreshnessLevel(int ageDays, FreshnessThresholds thresholds)
    {
        if (ageDays <= thresholds.FreshDays) return "fresh";
        if (ageDays <= thresholds.GoodDays) return "good";
        if (ageDays <= thresholds.StaleDays) return "stale";
        return "outdated";
    }

    private string DetermineFreshnessLevel(double score)
    {
        if (score >= 80) return "fresh";
        if (score >= 60) return "good";
        if (score >= 30) return "stale";
        return "outdated";
    }

    private int CalculateUpdatePriority(double freshnessScore, int citationCount)
    {
        // 优先级 = 低新鲜度 + 高引用 = 高优先级
        var freshnessWeight = (100 - freshnessScore) / 10; // 0-10
        var citationWeight = Math.Min(5, citationCount); // 0-5

        return (int)Math.Min(10, freshnessWeight + citationWeight);
    }

    private string GenerateItemRecommendation(string level, int ageDays, int citationCount)
    {
        return level switch
        {
            "fresh" => "内容新鲜，继续监测",
            "good" => citationCount >= 3 ? "计划在 3 个月内更新" : "保持关注",
            "stale" => citationCount >= 2 ? "建议尽快更新" : "考虑更新或归档",
            "outdated" => citationCount >= 2 ? "紧急：需要立即更新" : "建议归档或重写",
            _ => "需要评估"
        };
    }

    private string GenerateSummary(ContentFreshnessReport report)
    {
        var parts = new List<string>();

        parts.Add($"分析了 {report.Items.Count} 个被 AI 引用的内容");
        parts.Add($"整体新鲜度评分 {report.OverallScore:F0} 分（{GetLevelLabel(report.FreshnessLevel)}）");

        if (report.Distribution.FreshCount > 0)
            parts.Add($"{report.Distribution.FreshCount} 个内容保持新鲜");

        if (report.Distribution.StaleCount + report.Distribution.OutdatedCount > 0)
            parts.Add($"{report.Distribution.StaleCount + report.Distribution.OutdatedCount} 个内容需要更新");

        if (report.Alerts.Any(a => a.Level == "critical"))
            parts.Add($"有 {report.Alerts.Count(a => a.Level == "critical")} 个紧急告警需要关注");

        return string.Join("。", parts) + "。";
    }

    private string GetLevelLabel(string level)
    {
        return level switch
        {
            "fresh" => "新鲜",
            "good" => "良好",
            "stale" => "陈旧",
            "outdated" => "过时",
            _ => level
        };
    }

    #endregion
}
