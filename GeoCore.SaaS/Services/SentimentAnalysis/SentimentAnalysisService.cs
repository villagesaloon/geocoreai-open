using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.SentimentAnalysis;

/// <summary>
/// 情感分析增强服务
/// 功能：4.33 多维情感分析、4.34 情感趋势追踪、4.35 竞品情感对比
/// </summary>
public class SentimentAnalysisService
{
    private readonly ILogger<SentimentAnalysisService> _logger;
    private readonly CitationMonitoringRepository? _repository;

    private static readonly Dictionary<string, string> PositiveKeywords = new()
    {
        { "excellent", "trust" },
        { "great", "satisfaction" },
        { "recommend", "recommendation" },
        { "best", "recommendation" },
        { "reliable", "trust" },
        { "trusted", "trust" },
        { "quality", "satisfaction" },
        { "helpful", "satisfaction" },
        { "love", "satisfaction" },
        { "amazing", "satisfaction" }
    };

    private static readonly Dictionary<string, string> NegativeKeywords = new()
    {
        { "poor", "criticism" },
        { "bad", "criticism" },
        { "terrible", "criticism" },
        { "avoid", "concern" },
        { "issue", "concern" },
        { "problem", "concern" },
        { "expensive", "concern" },
        { "slow", "criticism" },
        { "unreliable", "criticism" },
        { "disappointed", "criticism" }
    };

    public SentimentAnalysisService(
        ILogger<SentimentAnalysisService> logger,
        CitationMonitoringRepository? repository = null)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 生成完整情感分析报告
    /// </summary>
    public async Task<SentimentAnalysisReport> GenerateReportAsync(SentimentAnalysisRequest request)
    {
        _logger.LogInformation("[SentimentAnalysis] Generating report for task {TaskId}, brand {Brand}",
            request.TaskId, request.Brand);

        var results = await GetResultsAsync(request.TaskId);
        if (!results.Any())
        {
            throw new ArgumentException($"任务 {request.TaskId} 没有监测数据");
        }

        var report = new SentimentAnalysisReport
        {
            Brand = request.Brand,
            TaskId = request.TaskId
        };

        // 4.33 多维情感分析
        report.Sentiment = AnalyzeMultiDimensionalSentiment(results, request.Brand);

        // 4.34 情感趋势追踪
        if (request.IncludeTrends)
        {
            report.Trends = AnalyzeSentimentTrends(results, request.Brand);
        }

        // 4.35 竞品情感对比
        if (request.Competitors?.Any() == true)
        {
            report.CompetitorComparisons = AnalyzeCompetitorSentiment(results, request.Brand, request.Competitors);
        }

        // 情感关键词
        if (request.IncludeKeywords)
        {
            report.Keywords = ExtractSentimentKeywords(results, request.Brand);
        }

        // 平台情感分布
        report.PlatformBreakdown = AnalyzePlatformSentiment(results, request.Brand);

        // 情感预警
        if (request.IncludeAlerts)
        {
            report.Alerts = GenerateAlerts(report);
        }

        // 优化建议
        report.Suggestions = GenerateSuggestions(report);

        // 生成摘要
        report.Summary = GenerateSummary(report);

        return report;
    }

    /// <summary>
    /// 4.33 多维情感分析
    /// </summary>
    public MultiDimensionalSentiment AnalyzeMultiDimensionalSentiment(List<CitationResultEntity> results, string brand)
    {
        var brandResults = FilterBrandResults(results, brand);
        if (!brandResults.Any())
        {
            return new MultiDimensionalSentiment { OverallLevel = "neutral" };
        }

        var sentiment = new MultiDimensionalSentiment();

        // 计算整体情感
        var scores = brandResults.Select(r => r.SentimentScore).ToList();
        sentiment.OverallScore = scores.Average();
        sentiment.OverallLevel = GetSentimentLevel(sentiment.OverallScore);

        // 计算情感强度
        sentiment.Intensity = scores.Select(s => Math.Abs(s)).Average();

        // 分类情感
        var positiveResults = brandResults.Where(r => r.SentimentScore > 0.2).ToList();
        var neutralResults = brandResults.Where(r => r.SentimentScore >= -0.2 && r.SentimentScore <= 0.2).ToList();
        var negativeResults = brandResults.Where(r => r.SentimentScore < -0.2).ToList();

        sentiment.Positive = new SentimentDimension
        {
            Rate = (double)positiveResults.Count / brandResults.Count,
            Count = positiveResults.Count,
            AverageIntensity = positiveResults.Any() ? positiveResults.Average(r => r.SentimentScore) : 0,
            Examples = positiveResults.Take(3).Select(r => TruncateText(r.CitationContext ?? r.Response, 100)).ToList()
        };

        sentiment.Neutral = new SentimentDimension
        {
            Rate = (double)neutralResults.Count / brandResults.Count,
            Count = neutralResults.Count,
            AverageIntensity = neutralResults.Any() ? neutralResults.Average(r => Math.Abs(r.SentimentScore)) : 0,
            Examples = neutralResults.Take(3).Select(r => TruncateText(r.CitationContext ?? r.Response, 100)).ToList()
        };

        sentiment.Negative = new SentimentDimension
        {
            Rate = (double)negativeResults.Count / brandResults.Count,
            Count = negativeResults.Count,
            AverageIntensity = negativeResults.Any() ? negativeResults.Average(r => Math.Abs(r.SentimentScore)) : 0,
            Examples = negativeResults.Take(3).Select(r => TruncateText(r.CitationContext ?? r.Response, 100)).ToList()
        };

        // 计算一致性（基于标准差）
        var stdDev = CalculateStandardDeviation(scores);
        sentiment.Consistency = Math.Max(0, 1 - stdDev);

        // 情感细分类别
        sentiment.Categories = AnalyzeSentimentCategories(brandResults);

        return sentiment;
    }

    /// <summary>
    /// 4.34 情感趋势追踪
    /// </summary>
    public SentimentTrendAnalysis AnalyzeSentimentTrends(List<CitationResultEntity> results, string brand)
    {
        var brandResults = FilterBrandResults(results, brand);
        var analysis = new SentimentTrendAnalysis();

        if (brandResults.Count < 2)
        {
            analysis.Analysis = "数据不足，无法分析趋势";
            return analysis;
        }

        // 按日期分组
        var dateGroups = brandResults
            .GroupBy(r => r.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        if (dateGroups.Count < 2)
        {
            analysis.Analysis = "时间跨度不足，无法分析趋势";
            return analysis;
        }

        // 生成趋势数据点
        foreach (var group in dateGroups)
        {
            var dayResults = group.ToList();
            analysis.DataPoints.Add(new SentimentTrendPoint
            {
                Date = group.Key,
                AverageScore = dayResults.Average(r => r.SentimentScore),
                PositiveRate = dayResults.Count(r => r.SentimentScore > 0.2) / (double)dayResults.Count,
                NegativeRate = dayResults.Count(r => r.SentimentScore < -0.2) / (double)dayResults.Count,
                SampleCount = dayResults.Count
            });
        }

        // 计算趋势方向
        var firstHalf = analysis.DataPoints.Take(analysis.DataPoints.Count / 2).ToList();
        var secondHalf = analysis.DataPoints.Skip(analysis.DataPoints.Count / 2).ToList();

        var firstAvg = firstHalf.Average(p => p.AverageScore);
        var secondAvg = secondHalf.Average(p => p.AverageScore);

        if (secondAvg > firstAvg + 0.1)
        {
            analysis.TrendDirection = "improving";
            analysis.ChangeRate = (secondAvg - firstAvg) / Math.Max(Math.Abs(firstAvg), 0.1);
        }
        else if (secondAvg < firstAvg - 0.1)
        {
            analysis.TrendDirection = "declining";
            analysis.ChangeRate = (secondAvg - firstAvg) / Math.Max(Math.Abs(firstAvg), 0.1);
        }
        else
        {
            analysis.TrendDirection = "stable";
            analysis.ChangeRate = 0;
        }

        // 正面趋势
        var firstPositiveRate = firstHalf.Average(p => p.PositiveRate);
        var secondPositiveRate = secondHalf.Average(p => p.PositiveRate);
        analysis.PositiveTrend = new TrendLine
        {
            Direction = secondPositiveRate > firstPositiveRate + 0.05 ? "rising" :
                       secondPositiveRate < firstPositiveRate - 0.05 ? "declining" : "stable",
            Slope = secondPositiveRate - firstPositiveRate,
            ChangePercent = firstPositiveRate > 0 ? (secondPositiveRate - firstPositiveRate) / firstPositiveRate * 100 : 0
        };

        // 负面趋势
        var firstNegativeRate = firstHalf.Average(p => p.NegativeRate);
        var secondNegativeRate = secondHalf.Average(p => p.NegativeRate);
        analysis.NegativeTrend = new TrendLine
        {
            Direction = secondNegativeRate > firstNegativeRate + 0.05 ? "rising" :
                       secondNegativeRate < firstNegativeRate - 0.05 ? "declining" : "stable",
            Slope = secondNegativeRate - firstNegativeRate,
            ChangePercent = firstNegativeRate > 0 ? (secondNegativeRate - firstNegativeRate) / firstNegativeRate * 100 : 0
        };

        // 生成预测
        analysis.Prediction = GeneratePrediction(analysis);

        // 生成分析说明
        analysis.Analysis = GenerateTrendAnalysis(analysis);

        return analysis;
    }

    /// <summary>
    /// 4.35 竞品情感对比
    /// </summary>
    public List<CompetitorSentimentComparison> AnalyzeCompetitorSentiment(
        List<CitationResultEntity> results,
        string brand,
        List<string> competitors)
    {
        var comparisons = new List<CompetitorSentimentComparison>();
        var brandResults = FilterBrandResults(results, brand);

        if (!brandResults.Any())
        {
            return comparisons;
        }

        var ourScore = brandResults.Average(r => r.SentimentScore);
        var ourPositiveRate = brandResults.Count(r => r.SentimentScore > 0.2) / (double)brandResults.Count;
        var ourNegativeRate = brandResults.Count(r => r.SentimentScore < -0.2) / (double)brandResults.Count;

        foreach (var competitor in competitors)
        {
            var competitorResults = FilterBrandResults(results, competitor);
            
            if (!competitorResults.Any())
            {
                continue;
            }

            var competitorScore = competitorResults.Average(r => r.SentimentScore);
            var competitorPositiveRate = competitorResults.Count(r => r.SentimentScore > 0.2) / (double)competitorResults.Count;
            var competitorNegativeRate = competitorResults.Count(r => r.SentimentScore < -0.2) / (double)competitorResults.Count;

            var comparison = new CompetitorSentimentComparison
            {
                Competitor = competitor,
                OurScore = ourScore,
                CompetitorScore = competitorScore,
                Gap = ourScore - competitorScore,
                OurPositiveRate = ourPositiveRate,
                CompetitorPositiveRate = competitorPositiveRate,
                OurNegativeRate = ourNegativeRate,
                CompetitorNegativeRate = competitorNegativeRate
            };

            // 判断对比结果
            if (ourScore > competitorScore + 0.1)
            {
                comparison.Verdict = "leading";
            }
            else if (competitorScore > ourScore + 0.1)
            {
                comparison.Verdict = "trailing";
            }
            else
            {
                comparison.Verdict = "tied";
            }

            // 分析优势领域
            comparison.OurStrengths = IdentifyStrengths(brandResults);
            comparison.CompetitorStrengths = IdentifyStrengths(competitorResults);

            comparisons.Add(comparison);
        }

        return comparisons;
    }

    /// <summary>
    /// 提取情感关键词
    /// </summary>
    public List<SentimentKeyword> ExtractSentimentKeywords(List<CitationResultEntity> results, string brand)
    {
        var brandResults = FilterBrandResults(results, brand);
        var keywords = new Dictionary<string, SentimentKeyword>();

        foreach (var result in brandResults)
        {
            var text = $"{result.Response} {result.CitationContext}".ToLower();

            // 检查正面关键词
            foreach (var (keyword, category) in PositiveKeywords)
            {
                if (text.Contains(keyword))
                {
                    AddOrUpdateKeyword(keywords, keyword, "positive", result.SentimentScore);
                }
            }

            // 检查负面关键词
            foreach (var (keyword, category) in NegativeKeywords)
            {
                if (text.Contains(keyword))
                {
                    AddOrUpdateKeyword(keywords, keyword, "negative", result.SentimentScore);
                }
            }
        }

        // 计算影响度
        var totalCount = keywords.Values.Sum(k => k.Count);
        foreach (var keyword in keywords.Values)
        {
            keyword.Impact = totalCount > 0 ? (double)keyword.Count / totalCount * Math.Abs(keyword.Score) : 0;
        }

        return keywords.Values
            .OrderByDescending(k => k.Impact)
            .Take(20)
            .ToList();
    }

    /// <summary>
    /// 分析平台情感分布
    /// </summary>
    public List<PlatformSentiment> AnalyzePlatformSentiment(List<CitationResultEntity> results, string brand)
    {
        var brandResults = FilterBrandResults(results, brand);
        
        return brandResults
            .GroupBy(r => r.Platform)
            .Select(g => new PlatformSentiment
            {
                Platform = g.Key,
                AverageScore = g.Average(r => r.SentimentScore),
                PositiveRate = g.Count(r => r.SentimentScore > 0.2) / (double)g.Count(),
                NeutralRate = g.Count(r => r.SentimentScore >= -0.2 && r.SentimentScore <= 0.2) / (double)g.Count(),
                NegativeRate = g.Count(r => r.SentimentScore < -0.2) / (double)g.Count(),
                SampleCount = g.Count(),
                Verdict = GetPlatformVerdict(g.Average(r => r.SentimentScore))
            })
            .OrderByDescending(p => p.SampleCount)
            .ToList();
    }

    /// <summary>
    /// 生成情感预警
    /// </summary>
    public List<SentimentAlert> GenerateAlerts(SentimentAnalysisReport report)
    {
        var alerts = new List<SentimentAlert>();

        // 负面情感过高预警
        if (report.Sentiment.Negative.Rate > 0.3)
        {
            alerts.Add(new SentimentAlert
            {
                Level = "critical",
                Type = "negative_spike",
                Title = "负面情感占比过高",
                Description = $"负面情感占比达到 {report.Sentiment.Negative.Rate:P0}，超过警戒线 30%",
                Data = $"负面评价数量: {report.Sentiment.Negative.Count}",
                SuggestedAction = "立即分析负面评价来源，制定应对策略"
            });
        }
        else if (report.Sentiment.Negative.Rate > 0.2)
        {
            alerts.Add(new SentimentAlert
            {
                Level = "warning",
                Type = "negative_spike",
                Title = "负面情感占比偏高",
                Description = $"负面情感占比达到 {report.Sentiment.Negative.Rate:P0}，接近警戒线",
                SuggestedAction = "关注负面评价趋势，准备应对方案"
            });
        }

        // 趋势下降预警
        if (report.Trends.TrendDirection == "declining" && report.Trends.ChangeRate < -0.2)
        {
            alerts.Add(new SentimentAlert
            {
                Level = "warning",
                Type = "trend_decline",
                Title = "情感趋势持续下降",
                Description = $"情感评分下降 {Math.Abs(report.Trends.ChangeRate):P0}",
                SuggestedAction = "分析下降原因，采取改进措施"
            });
        }

        // 竞品差距预警
        foreach (var comp in report.CompetitorComparisons.Where(c => c.Verdict == "trailing" && c.Gap < -0.2))
        {
            alerts.Add(new SentimentAlert
            {
                Level = "warning",
                Type = "competitor_gap",
                Title = $"情感评分落后于 {comp.Competitor}",
                Description = $"情感评分差距: {Math.Abs(comp.Gap):F2}",
                SuggestedAction = $"分析 {comp.Competitor} 的优势，制定追赶策略"
            });
        }

        // 一致性问题预警
        if (report.Sentiment.Consistency < 0.5)
        {
            alerts.Add(new SentimentAlert
            {
                Level = "info",
                Type = "consistency_issue",
                Title = "各平台情感一致性较低",
                Description = $"情感一致性评分: {report.Sentiment.Consistency:F2}",
                SuggestedAction = "检查各平台的品牌表现差异"
            });
        }

        return alerts.OrderByDescending(a => a.Level == "critical" ? 3 : a.Level == "warning" ? 2 : 1).ToList();
    }

    /// <summary>
    /// 生成优化建议
    /// </summary>
    public List<SentimentOptimizationSuggestion> GenerateSuggestions(SentimentAnalysisReport report)
    {
        var suggestions = new List<SentimentOptimizationSuggestion>();

        // 基于负面情感
        if (report.Sentiment.Negative.Rate > 0.2)
        {
            suggestions.Add(new SentimentOptimizationSuggestion
            {
                Type = "reduce_negative",
                Title = "降低负面情感",
                Description = "当前负面情感占比较高，需要针对性改进",
                ActionSteps = new List<string>
                {
                    "分析负面评价的主要来源和原因",
                    "针对常见问题制定改进方案",
                    "在相关平台发布正面内容",
                    "积极回应用户关切"
                },
                Priority = 9,
                ExpectedImpact = "降低负面情感占比 10-20%",
                Difficulty = "medium"
            });
        }

        // 基于正面情感
        if (report.Sentiment.Positive.Rate > 0.5)
        {
            suggestions.Add(new SentimentOptimizationSuggestion
            {
                Type = "leverage_strength",
                Title = "利用正面情感优势",
                Description = "正面情感占比高，可以进一步放大优势",
                ActionSteps = new List<string>
                {
                    "收集和展示用户好评",
                    "鼓励满意用户分享体验",
                    "在营销内容中引用正面评价"
                },
                Priority = 7,
                ExpectedImpact = "提升品牌信任度和转化率",
                Difficulty = "easy"
            });
        }

        // 基于趋势
        if (report.Trends.TrendDirection == "declining")
        {
            suggestions.Add(new SentimentOptimizationSuggestion
            {
                Type = "address_concern",
                Title = "扭转下降趋势",
                Description = "情感趋势正在下降，需要及时干预",
                ActionSteps = new List<string>
                {
                    "识别导致下降的具体事件或问题",
                    "制定针对性的改进计划",
                    "增加正面内容的发布频率",
                    "监控改进效果"
                },
                Priority = 8,
                ExpectedImpact = "稳定并逐步提升情感评分",
                Difficulty = "medium"
            });
        }

        // 基于关键词
        var negativeKeywords = report.Keywords.Where(k => k.Sentiment == "negative").Take(3).ToList();
        if (negativeKeywords.Any())
        {
            suggestions.Add(new SentimentOptimizationSuggestion
            {
                Type = "address_concern",
                Title = "解决高频负面关键词问题",
                Description = $"负面关键词: {string.Join(", ", negativeKeywords.Select(k => k.Keyword))}",
                ActionSteps = negativeKeywords.Select(k => $"针对 '{k.Keyword}' 相关问题制定改进方案").ToList(),
                Priority = 7,
                ExpectedImpact = "减少负面关键词出现频率",
                Difficulty = "medium"
            });
        }

        // 基于平台差异
        var weakPlatforms = report.PlatformBreakdown.Where(p => p.AverageScore < 0).ToList();
        if (weakPlatforms.Any())
        {
            suggestions.Add(new SentimentOptimizationSuggestion
            {
                Type = "improve_positive",
                Title = "改善弱势平台表现",
                Description = $"以下平台情感评分为负: {string.Join(", ", weakPlatforms.Select(p => p.Platform))}",
                ActionSteps = new List<string>
                {
                    "分析这些平台的负面评价原因",
                    "针对平台特点优化内容策略",
                    "增加在这些平台的正面曝光"
                },
                Priority = 6,
                ExpectedImpact = "提升弱势平台的情感评分",
                Difficulty = "medium"
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

    private List<CitationResultEntity> FilterBrandResults(List<CitationResultEntity> results, string brand)
    {
        return results.Where(r =>
            r.Response.Contains(brand, StringComparison.OrdinalIgnoreCase) ||
            r.CitationContext?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true ||
            r.Question.Contains(brand, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private string GetSentimentLevel(double score)
    {
        if (score > 0.5) return "very_positive";
        if (score > 0.2) return "positive";
        if (score > -0.2) return "neutral";
        if (score > -0.5) return "negative";
        return "very_negative";
    }

    private double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    private List<SentimentCategory> AnalyzeSentimentCategories(List<CitationResultEntity> results)
    {
        var categories = new Dictionary<string, SentimentCategory>
        {
            { "trust", new SentimentCategory { Category = "trust", Label = "信任度" } },
            { "satisfaction", new SentimentCategory { Category = "satisfaction", Label = "满意度" } },
            { "recommendation", new SentimentCategory { Category = "recommendation", Label = "推荐意愿" } },
            { "criticism", new SentimentCategory { Category = "criticism", Label = "批评" } },
            { "concern", new SentimentCategory { Category = "concern", Label = "担忧" } }
        };

        foreach (var result in results)
        {
            var text = $"{result.Response} {result.CitationContext}".ToLower();

            foreach (var (keyword, category) in PositiveKeywords)
            {
                if (text.Contains(keyword) && categories.ContainsKey(category))
                {
                    categories[category].Count++;
                    categories[category].Score += result.SentimentScore;
                    if (categories[category].Examples.Count < 2)
                    {
                        categories[category].Examples.Add(TruncateText(result.CitationContext ?? result.Response, 80));
                    }
                }
            }

            foreach (var (keyword, category) in NegativeKeywords)
            {
                if (text.Contains(keyword) && categories.ContainsKey(category))
                {
                    categories[category].Count++;
                    categories[category].Score += result.SentimentScore;
                    if (categories[category].Examples.Count < 2)
                    {
                        categories[category].Examples.Add(TruncateText(result.CitationContext ?? result.Response, 80));
                    }
                }
            }
        }

        // 计算平均分
        foreach (var cat in categories.Values.Where(c => c.Count > 0))
        {
            cat.Score /= cat.Count;
        }

        return categories.Values.Where(c => c.Count > 0).OrderByDescending(c => c.Count).ToList();
    }

    private SentimentPrediction GeneratePrediction(SentimentTrendAnalysis analysis)
    {
        var prediction = new SentimentPrediction();

        if (analysis.DataPoints.Count < 3)
        {
            prediction.Confidence = 0.3;
            prediction.Reasoning = "数据点不足，预测置信度较低";
            return prediction;
        }

        var recentPoints = analysis.DataPoints.TakeLast(3).ToList();
        var recentAvg = recentPoints.Average(p => p.AverageScore);
        var trend = analysis.ChangeRate;

        // 简单线性预测
        prediction.PredictedScore = recentAvg + trend * 0.5;
        prediction.PredictedScore = Math.Max(-1, Math.Min(1, prediction.PredictedScore));
        prediction.PredictedLevel = GetSentimentLevel(prediction.PredictedScore);
        prediction.Confidence = analysis.DataPoints.Count >= 7 ? 0.7 : 0.5;
        prediction.Reasoning = analysis.TrendDirection switch
        {
            "improving" => "基于上升趋势，预计情感将继续改善",
            "declining" => "基于下降趋势，预计情感可能继续下滑",
            _ => "趋势稳定，预计情感将保持当前水平"
        };

        return prediction;
    }

    private string GenerateTrendAnalysis(SentimentTrendAnalysis analysis)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"情感趋势{GetTrendDescription(analysis.TrendDirection)}");
        
        if (analysis.TrendDirection != "stable")
        {
            sb.Append($"，变化幅度 {Math.Abs(analysis.ChangeRate):P0}");
        }

        sb.Append("。");

        if (analysis.PositiveTrend.Direction == "rising")
        {
            sb.Append("正面评价呈上升趋势。");
        }
        else if (analysis.NegativeTrend.Direction == "rising")
        {
            sb.Append("负面评价有所增加，需要关注。");
        }

        return sb.ToString();
    }

    private string GetTrendDescription(string direction)
    {
        return direction switch
        {
            "improving" => "持续改善",
            "declining" => "有所下降",
            _ => "保持稳定"
        };
    }

    private List<string> IdentifyStrengths(List<CitationResultEntity> results)
    {
        var strengths = new List<string>();
        var text = string.Join(" ", results.Select(r => r.Response + " " + r.CitationContext)).ToLower();

        var strengthKeywords = new Dictionary<string, string>
        {
            { "quality", "产品质量" },
            { "service", "服务" },
            { "price", "价格" },
            { "support", "客户支持" },
            { "reliable", "可靠性" },
            { "fast", "速度" },
            { "easy", "易用性" }
        };

        foreach (var (keyword, label) in strengthKeywords)
        {
            if (text.Contains(keyword))
            {
                strengths.Add(label);
            }
        }

        return strengths.Take(3).ToList();
    }

    private void AddOrUpdateKeyword(Dictionary<string, SentimentKeyword> keywords, string keyword, string sentiment, double score)
    {
        if (!keywords.ContainsKey(keyword))
        {
            keywords[keyword] = new SentimentKeyword
            {
                Keyword = keyword,
                Sentiment = sentiment,
                Score = score,
                Count = 1
            };
        }
        else
        {
            keywords[keyword].Count++;
            keywords[keyword].Score = (keywords[keyword].Score * (keywords[keyword].Count - 1) + score) / keywords[keyword].Count;
        }
    }

    private string GetPlatformVerdict(double score)
    {
        if (score > 0.3) return "高度正面";
        if (score > 0.1) return "偏正面";
        if (score > -0.1) return "中性";
        if (score > -0.3) return "偏负面";
        return "高度负面";
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
    }

    private string GenerateSummary(SentimentAnalysisReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.Append($"品牌 {report.Brand} 的情感分析：");
        sb.Append($"整体情感{GetSentimentLevelDescription(report.Sentiment.OverallLevel)}");
        sb.Append($"（评分 {report.Sentiment.OverallScore:F2}）。");

        sb.Append($"正面 {report.Sentiment.Positive.Rate:P0}，");
        sb.Append($"中性 {report.Sentiment.Neutral.Rate:P0}，");
        sb.Append($"负面 {report.Sentiment.Negative.Rate:P0}。");

        if (report.Trends.TrendDirection != "stable")
        {
            sb.Append($"趋势{GetTrendDescription(report.Trends.TrendDirection)}。");
        }

        if (report.Alerts.Any(a => a.Level == "critical"))
        {
            sb.Append("存在需要立即关注的问题。");
        }

        return sb.ToString();
    }

    private string GetSentimentLevelDescription(string level)
    {
        return level switch
        {
            "very_positive" => "非常正面",
            "positive" => "正面",
            "neutral" => "中性",
            "negative" => "负面",
            "very_negative" => "非常负面",
            _ => "中性"
        };
    }

    #endregion
}
