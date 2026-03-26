using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.CitationSource;

/// <summary>
/// 引用来源分析服务
/// 功能：4.29 引用来源追踪、4.30 引用集中度分析、4.31 竞品引用来源、4.32 Reddit 活跃度监测
/// </summary>
public class CitationSourceService
{
    private readonly ILogger<CitationSourceService> _logger;
    private readonly CitationMonitoringRepository? _repository;

    private static readonly Dictionary<string, string> DomainTypeMapping = new()
    {
        { "reddit.com", "reddit" },
        { "quora.com", "quora" },
        { "wikipedia.org", "wikipedia" },
        { "stackoverflow.com", "forum" },
        { "github.com", "developer" },
        { "medium.com", "blog" },
        { "twitter.com", "social" },
        { "x.com", "social" },
        { "linkedin.com", "social" },
        { "youtube.com", "video" },
        { "news.ycombinator.com", "forum" }
    };

    public CitationSourceService(
        ILogger<CitationSourceService> logger,
        CitationMonitoringRepository? repository = null)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 生成完整引用来源分析报告
    /// </summary>
    public async Task<CitationSourceReport> GenerateReportAsync(CitationSourceRequest request)
    {
        _logger.LogInformation("[CitationSource] Generating report for task {TaskId}, brand {Brand}",
            request.TaskId, request.Brand);

        var results = await GetResultsAsync(request.TaskId);
        if (!results.Any())
        {
            throw new ArgumentException($"任务 {request.TaskId} 没有监测数据");
        }

        var report = new CitationSourceReport
        {
            Brand = request.Brand,
            TaskId = request.TaskId
        };

        // 4.29 引用来源追踪
        report.SourceStats = AnalyzeSourceStatistics(results, request.Brand);

        // 4.30 引用集中度分析
        report.Concentration = AnalyzeConcentration(report.SourceStats);

        // 4.31 竞品引用来源
        if (request.Competitors?.Any() == true)
        {
            report.CompetitorSources = AnalyzeCompetitorSources(results, request.Brand, request.Competitors);
        }

        // 4.32 Reddit 活跃度监测
        if (request.IncludeRedditAnalysis)
        {
            report.RedditActivity = AnalyzeRedditActivity(results, request.Brand);
        }

        // 来源趋势
        if (request.IncludeTrends)
        {
            report.SourceTrends = AnalyzeSourceTrends(results, request.Brand);
        }

        // 生成优化建议
        report.Suggestions = GenerateSuggestions(report);

        // 生成摘要
        report.Summary = GenerateSummary(report);

        return report;
    }

    /// <summary>
    /// 4.29 引用来源追踪 - 分析引用来源统计
    /// </summary>
    public List<SourceStatistics> AnalyzeSourceStatistics(List<CitationResultEntity> results, string brand)
    {
        var citedResults = results.Where(r => r.IsCited).ToList();
        if (!citedResults.Any())
        {
            return new List<SourceStatistics>();
        }

        var sourceGroups = new Dictionary<string, SourceStatistics>();

        foreach (var result in citedResults)
        {
            var sources = ExtractSourcesFromResponse(result.Response);
            
            foreach (var source in sources)
            {
                var domain = ExtractDomain(source);
                if (string.IsNullOrEmpty(domain)) continue;

                if (!sourceGroups.ContainsKey(domain))
                {
                    sourceGroups[domain] = new SourceStatistics
                    {
                        Domain = domain,
                        SourceType = GetSourceType(domain),
                        CitationCount = 0,
                        CitedByPlatforms = new List<string>(),
                        ExampleUrls = new List<string>()
                    };
                }

                var stats = sourceGroups[domain];
                stats.CitationCount++;
                
                if (!stats.CitedByPlatforms.Contains(result.Platform))
                {
                    stats.CitedByPlatforms.Add(result.Platform);
                }

                if (stats.ExampleUrls.Count < 3 && !string.IsNullOrEmpty(source))
                {
                    stats.ExampleUrls.Add(source);
                }

                // 累计情感评分
                stats.AverageSentiment = (stats.AverageSentiment * (stats.CitationCount - 1) + result.SentimentScore) / stats.CitationCount;
            }

            // 如果没有提取到明确来源，尝试从上下文推断
            if (!sources.Any())
            {
                var inferredSource = InferSourceFromContext(result.Response, result.CitationContext);
                if (!string.IsNullOrEmpty(inferredSource))
                {
                    if (!sourceGroups.ContainsKey(inferredSource))
                    {
                        sourceGroups[inferredSource] = new SourceStatistics
                        {
                            Domain = inferredSource,
                            SourceType = GetSourceType(inferredSource),
                            CitationCount = 0,
                            CitedByPlatforms = new List<string>()
                        };
                    }
                    sourceGroups[inferredSource].CitationCount++;
                    if (!sourceGroups[inferredSource].CitedByPlatforms.Contains(result.Platform))
                    {
                        sourceGroups[inferredSource].CitedByPlatforms.Add(result.Platform);
                    }
                }
            }
        }

        var totalCitations = sourceGroups.Values.Sum(s => s.CitationCount);
        
        foreach (var stats in sourceGroups.Values)
        {
            stats.CitationRate = totalCitations > 0 ? (double)stats.CitationCount / totalCitations : 0;
            stats.TrustScore = CalculateTrustScore(stats);
        }

        return sourceGroups.Values
            .OrderByDescending(s => s.CitationCount)
            .ToList();
    }

    /// <summary>
    /// 4.30 引用集中度分析
    /// </summary>
    public ConcentrationAnalysis AnalyzeConcentration(List<SourceStatistics> sourceStats)
    {
        var analysis = new ConcentrationAnalysis();

        if (!sourceStats.Any())
        {
            analysis.Analysis = "没有足够的引用数据进行集中度分析";
            return analysis;
        }

        // 计算 HHI (Herfindahl-Hirschman Index) 作为集中度指数
        var hhi = sourceStats.Sum(s => Math.Pow(s.CitationRate, 2));
        analysis.ConcentrationIndex = hhi;

        // 判断集中度级别
        if (hhi > 0.25)
        {
            analysis.ConcentrationLevel = "high";
        }
        else if (hhi > 0.15)
        {
            analysis.ConcentrationLevel = "medium";
        }
        else
        {
            analysis.ConcentrationLevel = "low";
        }

        // 分类来源
        analysis.PrimarySources = sourceStats.Where(s => s.CitationRate >= 0.2).ToList();
        analysis.SecondarySources = sourceStats.Where(s => s.CitationRate >= 0.05 && s.CitationRate < 0.2).ToList();
        analysis.LongTailSources = sourceStats.Where(s => s.CitationRate < 0.05).ToList();

        // 计算多样性评分 (基于来源数量和分布均匀度)
        var sourceCount = sourceStats.Count;
        var evenness = sourceCount > 1 ? 1 - hhi : 0;
        analysis.DiversityScore = Math.Min(100, (sourceCount * 10 + evenness * 50));

        // 生成分析说明
        analysis.Analysis = GenerateConcentrationAnalysis(analysis);

        return analysis;
    }

    /// <summary>
    /// 4.31 竞品引用来源分析
    /// </summary>
    public List<CompetitorSourceComparison> AnalyzeCompetitorSources(
        List<CitationResultEntity> results,
        string brand,
        List<string> competitors)
    {
        var comparisons = new List<CompetitorSourceComparison>();
        var brandSources = GetBrandSources(results, brand);

        foreach (var competitor in competitors)
        {
            var competitorSources = GetBrandSources(results, competitor);
            
            var comparison = new CompetitorSourceComparison
            {
                Competitor = competitor,
                TopSources = competitorSources.Take(5).ToList(),
                SharedSources = brandSources.Select(s => s.Domain)
                    .Intersect(competitorSources.Select(s => s.Domain))
                    .ToList(),
                CompetitorOnlySources = competitorSources.Select(s => s.Domain)
                    .Except(brandSources.Select(s => s.Domain))
                    .Take(5)
                    .ToList(),
                OurOnlySources = brandSources.Select(s => s.Domain)
                    .Except(competitorSources.Select(s => s.Domain))
                    .Take(5)
                    .ToList()
            };

            // 判断优势
            var brandTotal = brandSources.Sum(s => s.CitationCount);
            var competitorTotal = competitorSources.Sum(s => s.CitationCount);
            
            if (brandTotal > competitorTotal * 1.2)
            {
                comparison.Verdict = "领先";
            }
            else if (competitorTotal > brandTotal * 1.2)
            {
                comparison.Verdict = "落后";
            }
            else
            {
                comparison.Verdict = "持平";
            }

            comparisons.Add(comparison);
        }

        return comparisons;
    }

    /// <summary>
    /// 4.32 Reddit 活跃度监测
    /// </summary>
    public RedditActivityAnalysis AnalyzeRedditActivity(List<CitationResultEntity> results, string brand)
    {
        var analysis = new RedditActivityAnalysis();

        var citedResults = results.Where(r => r.IsCited).ToList();
        if (!citedResults.Any())
        {
            analysis.Analysis = "没有足够的引用数据进行 Reddit 分析";
            return analysis;
        }

        // 提取 Reddit 相关引用
        var redditMentions = new List<(CitationResultEntity Result, string Subreddit)>();
        
        foreach (var result in citedResults)
        {
            var subreddits = ExtractSubreddits(result.Response);
            foreach (var subreddit in subreddits)
            {
                redditMentions.Add((result, subreddit));
            }
        }

        // 计算 Reddit 引用占比
        var totalCited = citedResults.Count;
        var redditCited = redditMentions.Select(m => m.Result.Id).Distinct().Count();
        analysis.RedditCitationRate = totalCited > 0 ? (double)redditCited / totalCited : 0;

        // 分析活跃的 Subreddit
        var subredditGroups = redditMentions
            .GroupBy(m => m.Subreddit)
            .Select(g => new SubredditActivity
            {
                Subreddit = g.Key,
                MentionCount = g.Count(),
                SentimentScore = g.Average(m => m.Result.SentimentScore),
                RelatedQuestions = g.Select(m => m.Result.Question).Distinct().Take(3).ToList()
            })
            .OrderByDescending(s => s.MentionCount)
            .ToList();

        analysis.ActiveSubreddits = subredditGroups;

        // 情感分布
        if (redditMentions.Any())
        {
            var sentiments = redditMentions.Select(m => m.Result.SentimentScore).ToList();
            analysis.Sentiment = new SentimentDistribution
            {
                PositiveRate = sentiments.Count(s => s > 0.2) / (double)sentiments.Count,
                NeutralRate = sentiments.Count(s => s >= -0.2 && s <= 0.2) / (double)sentiments.Count,
                NegativeRate = sentiments.Count(s => s < -0.2) / (double)sentiments.Count,
                AverageScore = sentiments.Average()
            };
        }

        // 推荐参与的 Subreddit
        analysis.RecommendedSubreddits = GenerateSubredditRecommendations(subredditGroups, brand);

        // 生成分析说明
        analysis.Analysis = GenerateRedditAnalysis(analysis);

        return analysis;
    }

    /// <summary>
    /// 分析来源趋势
    /// </summary>
    public List<SourceTrend> AnalyzeSourceTrends(List<CitationResultEntity> results, string brand)
    {
        var trends = new List<SourceTrend>();
        
        var citedResults = results.Where(r => r.IsCited).ToList();
        if (citedResults.Count < 2) return trends;

        // 按日期分组
        var dateGroups = citedResults
            .GroupBy(r => r.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        if (dateGroups.Count < 2) return trends;

        // 分析每个主要来源的趋势
        var allSources = citedResults
            .SelectMany(r => ExtractSourcesFromResponse(r.Response))
            .Select(ExtractDomain)
            .Where(d => !string.IsNullOrEmpty(d))
            .GroupBy(d => d)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key)
            .ToList();

        foreach (var domain in allSources.Take(10))
        {
            var trend = new SourceTrend { Domain = domain! };
            
            foreach (var dateGroup in dateGroups)
            {
                var count = dateGroup.Count(r => 
                    ExtractSourcesFromResponse(r.Response)
                        .Select(ExtractDomain)
                        .Contains(domain));
                
                trend.DataPoints.Add(new TrendDataPoint
                {
                    Date = dateGroup.Key,
                    CitationCount = count,
                    CitationRate = dateGroup.Count() > 0 ? (double)count / dateGroup.Count() : 0
                });
            }

            // 计算趋势方向
            if (trend.DataPoints.Count >= 2)
            {
                var firstHalf = trend.DataPoints.Take(trend.DataPoints.Count / 2).Average(p => p.CitationCount);
                var secondHalf = trend.DataPoints.Skip(trend.DataPoints.Count / 2).Average(p => p.CitationCount);
                
                if (secondHalf > firstHalf * 1.2)
                {
                    trend.TrendDirection = "rising";
                    trend.ChangeRate = (secondHalf - firstHalf) / Math.Max(firstHalf, 1);
                }
                else if (firstHalf > secondHalf * 1.2)
                {
                    trend.TrendDirection = "declining";
                    trend.ChangeRate = (firstHalf - secondHalf) / Math.Max(firstHalf, 1) * -1;
                }
                else
                {
                    trend.TrendDirection = "stable";
                    trend.ChangeRate = 0;
                }
            }

            trends.Add(trend);
        }

        return trends.OrderByDescending(t => Math.Abs(t.ChangeRate)).ToList();
    }

    /// <summary>
    /// 生成优化建议
    /// </summary>
    public List<SourceOptimizationSuggestion> GenerateSuggestions(CitationSourceReport report)
    {
        var suggestions = new List<SourceOptimizationSuggestion>();

        // 基于集中度分析
        if (report.Concentration.ConcentrationLevel == "high")
        {
            suggestions.Add(new SourceOptimizationSuggestion
            {
                Type = "new_source",
                TargetSource = "多平台",
                Title = "扩展引用来源覆盖",
                Description = "当前引用来源过于集中，建议在更多平台建立品牌存在感",
                ActionSteps = new List<string>
                {
                    "识别当前未覆盖的高价值平台",
                    "在 2-3 个新平台创建品牌内容",
                    "参与相关社区讨论"
                },
                Priority = 8,
                ExpectedImpact = "提高来源多样性，降低单一来源依赖风险",
                Difficulty = "medium"
            });
        }

        // 基于 Reddit 分析
        if (report.RedditActivity.RedditCitationRate < 0.3 && report.RedditActivity.RedditCitationRate > 0)
        {
            suggestions.Add(new SourceOptimizationSuggestion
            {
                Type = "increase_presence",
                TargetSource = "reddit.com",
                Title = "增加 Reddit 活跃度",
                Description = "Reddit 是 AI 引用的重要来源（Perplexity 46.7% 引用来自 Reddit），建议增加参与度",
                ActionSteps = new List<string>
                {
                    "加入相关 Subreddit",
                    "定期发布有价值的内容",
                    "回答用户问题，自然提及品牌"
                },
                Priority = 9,
                ExpectedImpact = "显著提高 AI 引用概率",
                Difficulty = "medium"
            });
        }

        // 基于竞品分析
        foreach (var comp in report.CompetitorSources.Where(c => c.Verdict == "落后"))
        {
            if (comp.CompetitorOnlySources.Any())
            {
                suggestions.Add(new SourceOptimizationSuggestion
                {
                    Type = "new_source",
                    TargetSource = comp.CompetitorOnlySources.First(),
                    Title = $"进入竞品 {comp.Competitor} 的优势来源",
                    Description = $"竞品在 {string.Join(", ", comp.CompetitorOnlySources.Take(3))} 有引用，而我们没有",
                    ActionSteps = new List<string>
                    {
                        $"分析竞品在 {comp.CompetitorOnlySources.First()} 的内容策略",
                        "创建针对该平台的内容",
                        "建立品牌存在感"
                    },
                    Priority = 7,
                    ExpectedImpact = "缩小与竞品的差距",
                    Difficulty = "medium"
                });
            }
        }

        // 基于推荐的 Subreddit
        foreach (var rec in report.RedditActivity.RecommendedSubreddits.Take(2))
        {
            suggestions.Add(new SourceOptimizationSuggestion
            {
                Type = "increase_presence",
                TargetSource = $"r/{rec.Subreddit}",
                Title = $"参与 r/{rec.Subreddit}",
                Description = rec.Reason,
                ActionSteps = new List<string>
                {
                    $"加入 r/{rec.Subreddit}",
                    rec.SuggestedAction
                },
                Priority = 6,
                ExpectedImpact = $"预计触达 {rec.EstimatedReach:N0} 用户",
                Difficulty = "easy"
            });
        }

        return suggestions.OrderByDescending(s => s.Priority).ToList();
    }

    #region Helper Methods

    private async Task<List<CitationResultEntity>> GetResultsAsync(int taskId)
    {
        if (_repository == null)
        {
            return new List<CitationResultEntity>();
        }
        return await _repository.GetResultsByTaskIdAsync(taskId);
    }

    private List<string> ExtractSourcesFromResponse(string response)
    {
        var sources = new List<string>();
        
        // URL 正则
        var urlPattern = @"https?://[^\s\)\]\}""'<>]+";
        var matches = Regex.Matches(response, urlPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            sources.Add(match.Value.TrimEnd('.', ',', ')', ']', '}'));
        }

        // 域名引用模式 (如 "according to reddit.com")
        var domainPattern = @"(?:according to|from|on|via|source:?)\s+([a-zA-Z0-9][-a-zA-Z0-9]*(?:\.[a-zA-Z]{2,})+)";
        var domainMatches = Regex.Matches(response, domainPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in domainMatches)
        {
            if (match.Groups.Count > 1)
            {
                sources.Add(match.Groups[1].Value);
            }
        }

        return sources.Distinct().ToList();
    }

    private string? ExtractDomain(string url)
    {
        try
        {
            if (url.StartsWith("http"))
            {
                var uri = new Uri(url);
                return uri.Host.ToLower().Replace("www.", "");
            }
            return url.ToLower().Replace("www.", "");
        }
        catch
        {
            return null;
        }
    }

    private string GetSourceType(string domain)
    {
        foreach (var mapping in DomainTypeMapping)
        {
            if (domain.Contains(mapping.Key))
            {
                return mapping.Value;
            }
        }

        // 推断类型
        if (domain.EndsWith(".gov")) return "government";
        if (domain.EndsWith(".edu")) return "academic";
        if (domain.Contains("news") || domain.Contains("times") || domain.Contains("post")) return "news";
        if (domain.Contains("blog")) return "blog";
        
        return "website";
    }

    private string? InferSourceFromContext(string response, string? context)
    {
        var text = $"{response} {context}";
        
        // 检查常见来源提及
        var sourcePatterns = new Dictionary<string, string>
        {
            { @"\breddit\b", "reddit.com" },
            { @"\bquora\b", "quora.com" },
            { @"\bwikipedia\b", "wikipedia.org" },
            { @"\bstack\s*overflow\b", "stackoverflow.com" },
            { @"\bgithub\b", "github.com" },
            { @"\bmedium\b", "medium.com" },
            { @"\byoutube\b", "youtube.com" }
        };

        foreach (var pattern in sourcePatterns)
        {
            if (Regex.IsMatch(text, pattern.Key, RegexOptions.IgnoreCase))
            {
                return pattern.Value;
            }
        }

        return null;
    }

    private double CalculateTrustScore(SourceStatistics stats)
    {
        var score = 50.0;

        // 基于来源类型
        var typeScores = new Dictionary<string, double>
        {
            { "wikipedia", 90 },
            { "academic", 95 },
            { "government", 95 },
            { "news", 75 },
            { "reddit", 60 },
            { "quora", 55 },
            { "forum", 50 },
            { "blog", 45 },
            { "social", 40 }
        };

        if (typeScores.TryGetValue(stats.SourceType, out var typeScore))
        {
            score = typeScore;
        }

        // 基于多平台引用
        score += stats.CitedByPlatforms.Count * 5;

        // 基于情感
        if (stats.AverageSentiment > 0.3) score += 5;
        if (stats.AverageSentiment < -0.3) score -= 10;

        return Math.Min(100, Math.Max(0, score));
    }

    private List<SourceStatistics> GetBrandSources(List<CitationResultEntity> results, string brand)
    {
        var brandResults = results.Where(r => 
            r.IsCited && 
            (r.Response.Contains(brand, StringComparison.OrdinalIgnoreCase) ||
             r.CitationContext?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true))
            .ToList();

        return AnalyzeSourceStatistics(brandResults, brand);
    }

    private List<string> ExtractSubreddits(string response)
    {
        var subreddits = new List<string>();
        
        // r/subreddit 模式
        var pattern = @"r/([a-zA-Z0-9_]+)";
        var matches = Regex.Matches(response, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                subreddits.Add(match.Groups[1].Value);
            }
        }

        // reddit.com/r/subreddit 模式
        var urlPattern = @"reddit\.com/r/([a-zA-Z0-9_]+)";
        var urlMatches = Regex.Matches(response, urlPattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in urlMatches)
        {
            if (match.Groups.Count > 1)
            {
                subreddits.Add(match.Groups[1].Value);
            }
        }

        return subreddits.Distinct().ToList();
    }

    private List<SubredditRecommendation> GenerateSubredditRecommendations(
        List<SubredditActivity> activeSubreddits,
        string brand)
    {
        var recommendations = new List<SubredditRecommendation>();

        // 推荐已有活跃度但可以增加的 Subreddit
        foreach (var sub in activeSubreddits.Where(s => s.SentimentScore >= 0).Take(3))
        {
            recommendations.Add(new SubredditRecommendation
            {
                Subreddit = sub.Subreddit,
                Reason = $"已有 {sub.MentionCount} 次正面提及，情感评分 {sub.SentimentScore:F2}",
                EstimatedReach = sub.MentionCount * 1000,
                SuggestedAction = "增加参与频率，回答更多相关问题"
            });
        }

        // 推荐通用的高价值 Subreddit
        var commonSubreddits = new[]
        {
            ("SEO", "SEO 和数字营销讨论"),
            ("marketing", "营销策略讨论"),
            ("Entrepreneur", "创业者社区"),
            ("smallbusiness", "小企业主社区")
        };

        foreach (var (sub, desc) in commonSubreddits)
        {
            if (!activeSubreddits.Any(a => a.Subreddit.Equals(sub, StringComparison.OrdinalIgnoreCase)))
            {
                recommendations.Add(new SubredditRecommendation
                {
                    Subreddit = sub,
                    Reason = $"高价值社区：{desc}",
                    EstimatedReach = 50000,
                    SuggestedAction = "加入社区，分享专业知识"
                });
            }
        }

        return recommendations.Take(5).ToList();
    }

    private string GenerateConcentrationAnalysis(ConcentrationAnalysis analysis)
    {
        var sb = new System.Text.StringBuilder();

        if (analysis.ConcentrationLevel == "high")
        {
            sb.Append("引用来源高度集中，");
            if (analysis.PrimarySources.Any())
            {
                sb.Append($"主要来自 {string.Join("、", analysis.PrimarySources.Take(2).Select(s => s.Domain))}。");
            }
            sb.Append("建议扩展到更多平台以降低风险。");
        }
        else if (analysis.ConcentrationLevel == "medium")
        {
            sb.Append("引用来源分布适中，");
            sb.Append($"多样性评分 {analysis.DiversityScore:F0}/100。");
            sb.Append("可以继续优化长尾来源。");
        }
        else
        {
            sb.Append("引用来源分布均匀，");
            sb.Append($"多样性评分 {analysis.DiversityScore:F0}/100。");
            sb.Append("来源策略健康。");
        }

        return sb.ToString();
    }

    private string GenerateRedditAnalysis(RedditActivityAnalysis analysis)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"Reddit 引用占比 {analysis.RedditCitationRate:P0}");
        
        if (analysis.RedditCitationRate > 0.3)
        {
            sb.Append("（高于平均水平）。");
        }
        else if (analysis.RedditCitationRate > 0.1)
        {
            sb.Append("（中等水平）。");
        }
        else
        {
            sb.Append("（低于平均水平，建议增加 Reddit 活跃度）。");
        }

        if (analysis.ActiveSubreddits.Any())
        {
            sb.Append($"最活跃的社区：{string.Join("、", analysis.ActiveSubreddits.Take(3).Select(s => $"r/{s.Subreddit}"))}。");
        }

        return sb.ToString();
    }

    private string GenerateSummary(CitationSourceReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"品牌 {report.Brand} 的引用来源分析：");
        
        if (report.SourceStats.Any())
        {
            sb.Append($"共追踪到 {report.SourceStats.Count} 个来源，");
            sb.Append($"主要来源为 {string.Join("、", report.SourceStats.Take(3).Select(s => s.Domain))}。");
        }

        sb.Append(report.Concentration.Analysis);

        if (report.Suggestions.Any())
        {
            sb.Append($"建议优先执行：{report.Suggestions.First().Title}。");
        }

        return sb.ToString();
    }

    #endregion
}
