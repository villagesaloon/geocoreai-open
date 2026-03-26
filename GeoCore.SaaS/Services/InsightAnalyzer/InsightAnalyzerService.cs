using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Services.InsightAnalyzer;

/// <summary>
/// 洞察分析服务 - 基于 Omnia/Profound 方法论
/// 提供四信号分析 + 三路径建议 + 可执行任务生成
/// </summary>
public class InsightAnalyzerService
{
    private readonly ILogger<InsightAnalyzerService> _logger;
    private readonly CitationMonitoringRepository _citationRepo;

    public InsightAnalyzerService(
        ILogger<InsightAnalyzerService> logger,
        CitationMonitoringRepository citationRepo)
    {
        _logger = logger;
        _citationRepo = citationRepo;
    }

    /// <summary>
    /// 生成完整洞察报告
    /// </summary>
    public async Task<InsightReport> GenerateReportAsync(int taskId, string brand)
    {
        _logger.LogInformation("[Insight] Generating report for task {TaskId}, brand {Brand}", taskId, brand);

        var task = await _citationRepo.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            throw new ArgumentException($"Task {taskId} not found");
        }

        var results = await _citationRepo.GetResultsByTaskIdAsync(taskId);

        // 1. 四信号分析
        var signals = AnalyzeFourSignals(results, brand);

        // 2. 三路径建议
        var recommendations = GenerateThreePathRecommendations(signals, brand);

        // 3. 生成可执行任务
        var tasks = GenerateActionableTasks(signals, recommendations, brand);

        var report = new InsightReport
        {
            ProjectId = task.ProjectId ?? "",
            Brand = brand,
            Signals = signals,
            Recommendations = recommendations,
            Tasks = tasks,
            GeneratedAt = DateTime.UtcNow,
            ReportSummary = GenerateReportSummary(signals, recommendations)
        };

        _logger.LogInformation("[Insight] Report generated with {TaskCount} tasks", tasks.Count);
        return report;
    }

    /// <summary>
    /// 四信号分析
    /// </summary>
    public FourSignalInsight AnalyzeFourSignals(List<CitationResultEntity> results, string brand)
    {
        var insight = new FourSignalInsight();

        // 1. 引用集中度分析
        insight.CitationConcentration = AnalyzeCitationConcentration(results, brand);

        // 2. 当前位置分析
        insight.CurrentPosition = AnalyzeCurrentPosition(results, brand);

        // 3. 品牌力分析
        insight.BrandStrength = AnalyzeBrandStrength(results, brand);

        // 4. 品类分析
        insight.Category = AnalyzeCategorySignal(results, brand);

        // 综合评分
        insight.OverallScore = CalculateOverallScore(insight);
        insight.Summary = GenerateInsightSummary(insight, brand);

        return insight;
    }

    private CitationConcentrationSignal AnalyzeCitationConcentration(List<CitationResultEntity> results, string brand)
    {
        var signal = new CitationConcentrationSignal();
        var brandLower = brand.ToLowerInvariant();

        // 按问题分组
        var byQuestion = results.GroupBy(r => r.Question).ToList();
        var questionMentions = new Dictionary<string, (int mentions, int total, List<string> samples)>();

        foreach (var group in byQuestion)
        {
            var question = group.Key;
            var total = group.Count();
            var mentions = group.Count(r => 
                r.Response?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true ||
                r.IsCited);

            if (!questionMentions.ContainsKey(question))
            {
                questionMentions[question] = (mentions, total, new List<string> { question });
            }
        }

        // 计算集中度
        var sortedByRate = questionMentions
            .Select(kv => new { Question = kv.Key, Rate = (double)kv.Value.mentions / kv.Value.total, kv.Value.mentions })
            .OrderByDescending(x => x.Rate)
            .ToList();

        // 高集中度（提及率 > 50%）
        signal.HighConcentrationClusters = sortedByRate
            .Where(x => x.Rate > 0.5 && x.mentions > 0)
            .Take(5)
            .Select(x => new ConcentrationCluster
            {
                QuestionType = ClassifyQuestionType(x.Question),
                Description = x.Question.Length > 100 ? x.Question[..100] + "..." : x.Question,
                MentionCount = x.mentions,
                MentionRate = x.Rate,
                SampleQuestions = new List<string> { x.Question }
            })
            .ToList();

        // 低集中度（提及率 < 20% 但有相关性）
        signal.LowConcentrationClusters = sortedByRate
            .Where(x => x.Rate > 0 && x.Rate < 0.2)
            .Take(5)
            .Select(x => new ConcentrationCluster
            {
                QuestionType = ClassifyQuestionType(x.Question),
                Description = x.Question.Length > 100 ? x.Question[..100] + "..." : x.Question,
                MentionCount = x.mentions,
                MentionRate = x.Rate,
                SampleQuestions = new List<string> { x.Question }
            })
            .ToList();

        // 计算信号强度
        var avgRate = sortedByRate.Any() ? sortedByRate.Average(x => x.Rate) : 0;
        signal.Strength = Math.Min(100, avgRate * 100 * 1.5);

        return signal;
    }

    private CurrentPositionSignal AnalyzeCurrentPosition(List<CitationResultEntity> results, string brand)
    {
        var signal = new CurrentPositionSignal();
        var brandLower = brand.ToLowerInvariant();

        var relevantResults = results.Where(r => 
            r.Response?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true ||
            r.IsCited).ToList();

        if (!relevantResults.Any())
        {
            signal.Strength = 0;
            return signal;
        }

        // 计算位置统计（基于 CitationPosition 字段）
        var firstCount = relevantResults.Count(r => r.CitationPosition == "first");
        var topPositions = relevantResults.Count(r => r.CitationPosition == "first" || r.CitationPosition == "middle");
        
        if (relevantResults.Any())
        {
            signal.FirstPositionRate = (double)firstCount / relevantResults.Count;
            signal.TopThreeRate = (double)topPositions / relevantResults.Count;
            signal.AveragePosition = relevantResults.Average(r => r.PositionRatio) * 10; // 转换为1-10的位置
        }

        signal.OverallMentionRate = (double)relevantResults.Count / results.Count;

        // 按平台分组
        var byPlatform = relevantResults.GroupBy(r => r.Platform).ToList();
        foreach (var group in byPlatform)
        {
            var platformTotal = results.Count(r => r.Platform == group.Key);
            var platformFirst = group.Count(r => r.CitationPosition == "first");
            var platformTop = group.Count(r => r.CitationPosition == "first" || r.CitationPosition == "middle");
            
            signal.ByPlatform[group.Key] = new PositionStats
            {
                FirstPositionRate = group.Any() ? (double)platformFirst / group.Count() : 0,
                TopThreeRate = group.Any() ? (double)platformTop / group.Count() : 0,
                MentionRate = platformTotal > 0 ? (double)group.Count() / platformTotal : 0,
                AveragePosition = group.Any() ? group.Average(r => r.PositionRatio) * 10 : 0,
                TotalQueries = platformTotal
            };
        }

        // 计算信号强度（首位率权重高）
        signal.Strength = Math.Min(100, 
            signal.FirstPositionRate * 50 + 
            signal.TopThreeRate * 30 + 
            signal.OverallMentionRate * 20);

        return signal;
    }

    private BrandStrengthSignal AnalyzeBrandStrength(List<CitationResultEntity> results, string brand)
    {
        var signal = new BrandStrengthSignal();
        var brandLower = brand.ToLowerInvariant();

        var relevantResults = results.Where(r => 
            r.Response?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true || r.IsCited).ToList();

        if (!relevantResults.Any())
        {
            signal.Strength = 0;
            return signal;
        }

        // 分析推荐强度
        var strongPatterns = new[] { "最佳", "首选", "强烈推荐", "best", "top", "highly recommend", "excellent" };
        var moderatePatterns = new[] { "不错", "可以考虑", "推荐", "good", "recommend", "consider" };
        var negativePatterns = new[] { "不推荐", "避免", "问题", "not recommend", "avoid", "issues", "problems" };

        int strongCount = 0, moderateCount = 0, negativeCount = 0, mentionOnlyCount = 0;

        foreach (var result in relevantResults)
        {
            var text = result.Response?.ToLowerInvariant() ?? "";
            
            if (strongPatterns.Any(p => text.Contains(p)))
                strongCount++;
            else if (negativePatterns.Any(p => text.Contains(p)))
                negativeCount++;
            else if (moderatePatterns.Any(p => text.Contains(p)))
                moderateCount++;
            else
                mentionOnlyCount++;
        }

        var total = relevantResults.Count;
        signal.RecommendationStrength = new RecommendationStrength
        {
            StrongRecommendationRate = (double)strongCount / total,
            ModerateRecommendationRate = (double)moderateCount / total,
            MentionOnlyRate = (double)mentionOnlyCount / total,
            NegativeMentionRate = (double)negativeCount / total
        };

        // 情感分析（简化版）
        var positiveCount = strongCount + moderateCount;
        signal.Sentiment = new SentimentBreakdown
        {
            PositiveRate = (double)positiveCount / total,
            NeutralRate = (double)mentionOnlyCount / total,
            NegativeRate = (double)negativeCount / total,
            AverageScore = (positiveCount - negativeCount) / (double)total
        };

        // 提取常见描述词
        signal.CommonDescriptors = ExtractCommonDescriptors(relevantResults, brand);

        // 计算信号强度
        signal.Strength = Math.Min(100, 
            signal.RecommendationStrength.StrongRecommendationRate * 60 +
            signal.RecommendationStrength.ModerateRecommendationRate * 30 +
            (1 - signal.RecommendationStrength.NegativeMentionRate) * 10);

        return signal;
    }

    private CategorySignal AnalyzeCategorySignal(List<CitationResultEntity> results, string brand)
    {
        var signal = new CategorySignal();

        var relevantResults = results.Where(r => 
            r.Response?.Contains(brand, StringComparison.OrdinalIgnoreCase) == true || r.IsCited).ToList();

        if (!relevantResults.Any())
        {
            signal.Strength = 0;
            return signal;
        }

        // 从问题中提取品类关键词
        var categoryKeywords = new Dictionary<string, List<string>>
        {
            ["技术/软件"] = new() { "软件", "工具", "平台", "系统", "app", "software", "tool", "platform" },
            ["营销/推广"] = new() { "营销", "推广", "广告", "SEO", "marketing", "advertising", "promotion" },
            ["电商/零售"] = new() { "电商", "购物", "零售", "商城", "ecommerce", "shopping", "retail" },
            ["金融/投资"] = new() { "金融", "投资", "理财", "银行", "finance", "investment", "banking" },
            ["教育/培训"] = new() { "教育", "培训", "学习", "课程", "education", "training", "learning" },
            ["健康/医疗"] = new() { "健康", "医疗", "医院", "药品", "health", "medical", "healthcare" }
        };

        var categoryCounts = new Dictionary<string, int>();
        foreach (var result in relevantResults)
        {
            var text = (result.Question + " " + result.Response).ToLowerInvariant();
            foreach (var (category, keywords) in categoryKeywords)
            {
                if (keywords.Any(k => text.Contains(k)))
                {
                    categoryCounts[category] = categoryCounts.GetValueOrDefault(category, 0) + 1;
                }
            }
        }

        var sortedCategories = categoryCounts.OrderByDescending(kv => kv.Value).ToList();

        // 主要品类（前3）
        signal.PrimaryCategories = sortedCategories.Take(3).Select(kv => new CategoryAssociation
        {
            Category = kv.Key,
            MentionCount = kv.Value,
            AssociationStrength = (double)kv.Value / relevantResults.Count,
            RelatedKeywords = categoryKeywords[kv.Key].Take(3).ToList()
        }).ToList();

        // 次要品类
        signal.SecondaryCategories = sortedCategories.Skip(3).Take(3).Select(kv => new CategoryAssociation
        {
            Category = kv.Key,
            MentionCount = kv.Value,
            AssociationStrength = (double)kv.Value / relevantResults.Count,
            RelatedKeywords = categoryKeywords[kv.Key].Take(3).ToList()
        }).ToList();

        // 品类覆盖度
        signal.CategoryCoverage = (double)categoryCounts.Count / categoryKeywords.Count;

        // 品类机会
        var missingCategories = categoryKeywords.Keys.Except(categoryCounts.Keys).ToList();
        signal.CategoryOpportunities = missingCategories.Take(3).Select(c => new CategoryOpportunity
        {
            Category = c,
            Reason = $"品牌在 {c} 领域尚未建立 AI 可见度",
            PotentialScore = 0.5,
            SuggestedActions = new List<string> { $"创建 {c} 相关内容", $"在 {c} 问答中增加品牌曝光" }
        }).ToList();

        signal.Strength = Math.Min(100, signal.CategoryCoverage * 50 + 
            (signal.PrimaryCategories.Any() ? signal.PrimaryCategories.First().AssociationStrength * 50 : 0));

        return signal;
    }

    private List<DescriptorFrequency> ExtractCommonDescriptors(List<CitationResultEntity> results, string brand)
    {
        var descriptors = new Dictionary<string, (int count, string sentiment)>();
        var positiveWords = new HashSet<string> { "优秀", "出色", "领先", "创新", "专业", "excellent", "great", "leading", "innovative", "professional" };
        var negativeWords = new HashSet<string> { "昂贵", "复杂", "困难", "expensive", "complex", "difficult" };

        foreach (var result in results)
        {
            var text = result.Response ?? "";
            var brandIndex = text.IndexOf(brand, StringComparison.OrdinalIgnoreCase);
            if (brandIndex < 0) continue;

            // 提取品牌前后的形容词（简化实现）
            var start = Math.Max(0, brandIndex - 50);
            var end = Math.Min(text.Length, brandIndex + brand.Length + 50);
            var context = text[start..end].ToLowerInvariant();

            foreach (var word in positiveWords)
            {
                if (context.Contains(word))
                {
                    var current = descriptors.GetValueOrDefault(word, (0, "positive"));
                    descriptors[word] = (current.Item1 + 1, "positive");
                }
            }
            foreach (var word in negativeWords)
            {
                if (context.Contains(word))
                {
                    var current = descriptors.GetValueOrDefault(word, (0, "negative"));
                    descriptors[word] = (current.Item1 + 1, "negative");
                }
            }
        }

        return descriptors
            .OrderByDescending(kv => kv.Value.Item1)
            .Take(10)
            .Select(kv => new DescriptorFrequency
            {
                Descriptor = kv.Key,
                Count = kv.Value.Item1,
                Sentiment = kv.Value.Item2
            })
            .ToList();
    }

    private double CalculateOverallScore(FourSignalInsight insight)
    {
        return (insight.CitationConcentration.Strength * 0.2 +
                insight.CurrentPosition.Strength * 0.35 +
                insight.BrandStrength.Strength * 0.3 +
                insight.Category.Strength * 0.15);
    }

    private string GenerateInsightSummary(FourSignalInsight insight, string brand)
    {
        var parts = new List<string>();

        // 位置评价
        if (insight.CurrentPosition.FirstPositionRate > 0.3)
            parts.Add($"{brand} 在 AI 回答中表现强劲，首位提及率达 {insight.CurrentPosition.FirstPositionRate:P0}");
        else if (insight.CurrentPosition.OverallMentionRate > 0.5)
            parts.Add($"{brand} 有较好的 AI 可见度，总提及率 {insight.CurrentPosition.OverallMentionRate:P0}");
        else
            parts.Add($"{brand} 的 AI 可见度有提升空间，当前提及率 {insight.CurrentPosition.OverallMentionRate:P0}");

        // 品牌力评价
        if (insight.BrandStrength.RecommendationStrength.StrongRecommendationRate > 0.2)
            parts.Add("品牌获得较多强烈推荐");
        else if (insight.BrandStrength.RecommendationStrength.NegativeMentionRate > 0.1)
            parts.Add("需关注负面提及情况");

        // 品类评价
        if (insight.Category.PrimaryCategories.Any())
            parts.Add($"主要关联品类：{string.Join("、", insight.Category.PrimaryCategories.Take(2).Select(c => c.Category))}");

        return string.Join("。", parts) + "。";
    }

    /// <summary>
    /// 生成三路径建议
    /// </summary>
    public ThreePathRecommendation GenerateThreePathRecommendations(FourSignalInsight signals, string brand)
    {
        var recommendation = new ThreePathRecommendation();

        // 路径1：创建新内容
        recommendation.CreateContent = GenerateContentCreationPath(signals, brand);

        // 路径2：改进现有内容
        recommendation.ImproveContent = GenerateContentImprovementPath(signals, brand);

        // 路径3：获取曝光
        recommendation.GetExposure = GenerateExposurePath(signals, brand);

        // 确定优先级
        var paths = new[]
        {
            ("create", recommendation.CreateContent.Priority),
            ("improve", recommendation.ImproveContent.Priority),
            ("exposure", recommendation.GetExposure.Priority)
        };

        var topPath = paths.OrderByDescending(p => p.Item2).First();
        recommendation.RecommendedPriority = topPath.Item1;
        recommendation.PriorityReason = GetPriorityReason(topPath.Item1, signals);

        return recommendation;
    }

    private ContentCreationPath GenerateContentCreationPath(FourSignalInsight signals, string brand)
    {
        var path = new ContentCreationPath();

        // 根据低集中度场景生成内容建议
        var lowClusters = signals.CitationConcentration.LowConcentrationClusters;
        var categoryOpps = signals.Category.CategoryOpportunities;

        path.Suggestions = new List<ContentSuggestion>();

        // 针对低集中度场景
        foreach (var cluster in lowClusters.Take(3))
        {
            path.Suggestions.Add(new ContentSuggestion
            {
                ContentType = "faq",
                Title = $"{brand} 在 {cluster.QuestionType} 场景的应用",
                Description = $"创建针对 '{cluster.Description}' 类问题的专业内容",
                KeyPoints = new List<string> { "突出品牌优势", "提供具体案例", "包含用户评价" },
                TargetKeyword = cluster.QuestionType,
                EstimatedEffort = 2,
                ExpectedImpact = 4
            });
        }

        // 针对品类机会
        foreach (var opp in categoryOpps.Take(2))
        {
            path.Suggestions.Add(new ContentSuggestion
            {
                ContentType = "guide",
                Title = $"{brand} {opp.Category} 解决方案指南",
                Description = opp.Reason,
                KeyPoints = opp.SuggestedActions,
                TargetKeyword = opp.Category,
                EstimatedEffort = 3,
                ExpectedImpact = 3
            });
        }

        path.TargetScenarios = lowClusters.Select(c => c.QuestionType).ToList();
        path.ExpectedImpact = "预计可提升 AI 可见度 15-25%";

        // 计算优先级
        path.Priority = signals.CurrentPosition.OverallMentionRate < 0.3 ? 9 :
                        signals.CurrentPosition.OverallMentionRate < 0.5 ? 7 : 5;

        return path;
    }

    private ContentImprovementPath GenerateContentImprovementPath(FourSignalInsight signals, string brand)
    {
        var path = new ContentImprovementPath();
        path.Suggestions = new List<ImprovementSuggestion>();

        // 根据品牌力信号生成改进建议
        if (signals.BrandStrength.RecommendationStrength.MentionOnlyRate > 0.5)
        {
            path.Suggestions.Add(new ImprovementSuggestion
            {
                ContentIdentifier = "现有产品页面",
                CurrentIssue = "内容缺乏推荐性语言，AI 仅做中性提及",
                SuggestedImprovement = "增加用户评价、案例研究、专家推荐等社会证明",
                SpecificActions = new List<string>
                {
                    "添加客户成功案例",
                    "引用行业专家评价",
                    "增加具体数据支撑"
                },
                EstimatedEffort = 2,
                ExpectedImpact = 4
            });
        }

        if (signals.BrandStrength.RecommendationStrength.NegativeMentionRate > 0.1)
        {
            path.Suggestions.Add(new ImprovementSuggestion
            {
                ContentIdentifier = "品牌相关内容",
                CurrentIssue = $"存在 {signals.BrandStrength.RecommendationStrength.NegativeMentionRate:P0} 的负面提及",
                SuggestedImprovement = "针对负面反馈进行内容优化和问题解决",
                SpecificActions = new List<string>
                {
                    "分析负面提及的具体原因",
                    "创建问题解决方案内容",
                    "更新产品/服务改进信息"
                },
                EstimatedEffort = 3,
                ExpectedImpact = 5
            });
        }

        // 位置优化
        if (signals.CurrentPosition.AveragePosition > 3)
        {
            path.Suggestions.Add(new ImprovementSuggestion
            {
                ContentIdentifier = "核心产品内容",
                CurrentIssue = $"平均位置 {signals.CurrentPosition.AveragePosition:F1}，未能获得优先推荐",
                SuggestedImprovement = "优化内容结构，增加 AI 友好的格式化信息",
                SpecificActions = new List<string>
                {
                    "添加结构化数据 (Schema)",
                    "优化标题和描述",
                    "增加 FAQ 结构化内容"
                },
                EstimatedEffort = 2,
                ExpectedImpact = 4
            });
        }

        path.ExpectedImpact = "预计可提升推荐强度和位置排名";
        path.Priority = signals.BrandStrength.RecommendationStrength.NegativeMentionRate > 0.1 ? 9 :
                        signals.BrandStrength.RecommendationStrength.MentionOnlyRate > 0.5 ? 7 : 5;

        return path;
    }

    private ExposurePath GenerateExposurePath(FourSignalInsight signals, string brand)
    {
        var path = new ExposurePath();
        path.Opportunities = new List<ExposureOpportunity>();

        // Reddit 机会
        if (signals.CurrentPosition.OverallMentionRate < 0.5)
        {
            path.Opportunities.Add(new ExposureOpportunity
            {
                Channel = "reddit",
                SpecificTarget = "相关行业 subreddit",
                ActionDescription = "在相关讨论中自然提及品牌",
                Steps = new List<string>
                {
                    "找到相关的 subreddit 社区",
                    "参与相关问题讨论",
                    "在合适场景自然推荐品牌",
                    "分享使用经验和案例"
                },
                EstimatedEffort = 2,
                ExpectedImpact = 3
            });
        }

        // Quora 机会
        path.Opportunities.Add(new ExposureOpportunity
        {
            Channel = "quora",
            SpecificTarget = "相关问题回答",
            ActionDescription = "回答与品牌相关的问题",
            Steps = new List<string>
            {
                "搜索品牌相关问题",
                "提供专业详细的回答",
                "在回答中自然引用品牌"
            },
            EstimatedEffort = 2,
            ExpectedImpact = 3
        });

        // 行业论坛
        foreach (var category in signals.Category.PrimaryCategories.Take(2))
        {
            path.Opportunities.Add(new ExposureOpportunity
            {
                Channel = "industry_forum",
                SpecificTarget = $"{category.Category} 行业论坛",
                ActionDescription = $"在 {category.Category} 领域建立品牌权威",
                Steps = new List<string>
                {
                    $"找到 {category.Category} 领域的主要论坛",
                    "发布专业内容和案例",
                    "参与行业讨论"
                },
                EstimatedEffort = 3,
                ExpectedImpact = 4
            });
        }

        path.ExpectedImpact = "预计可增加品牌在 AI 训练数据中的曝光";
        path.Priority = signals.CurrentPosition.OverallMentionRate < 0.3 ? 8 : 5;

        return path;
    }

    private string GetPriorityReason(string priority, FourSignalInsight signals)
    {
        return priority switch
        {
            "create" => $"当前提及率 {signals.CurrentPosition.OverallMentionRate:P0} 较低，建议优先创建新内容提升覆盖",
            "improve" => "现有内容推荐强度不足，建议优先优化现有内容质量",
            "exposure" => "品牌曝光度不足，建议优先在外部渠道增加品牌提及",
            _ => "综合评估后的建议"
        };
    }

    /// <summary>
    /// 生成可执行任务
    /// </summary>
    public List<ActionableTask> GenerateActionableTasks(
        FourSignalInsight signals, 
        ThreePathRecommendation recommendations,
        string brand)
    {
        var tasks = new List<ActionableTask>();
        var taskPriority = 1;

        // 从内容创建路径生成任务
        foreach (var suggestion in recommendations.CreateContent.Suggestions.Take(3))
        {
            tasks.Add(new ActionableTask
            {
                Title = suggestion.Title,
                Description = suggestion.Description,
                Category = "content_creation",
                Priority = taskPriority++,
                EstimatedEffort = suggestion.EstimatedEffort,
                ExpectedImpact = suggestion.ExpectedImpact,
                Steps = suggestion.KeyPoints.Concat(new[] { "发布并监测效果" }).ToList(),
                TargetMetric = "AI 提及率",
                SuccessCriteria = "在目标场景中获得 AI 提及"
            });
        }

        // 从内容改进路径生成任务
        foreach (var suggestion in recommendations.ImproveContent.Suggestions.Take(2))
        {
            tasks.Add(new ActionableTask
            {
                Title = $"优化: {suggestion.ContentIdentifier}",
                Description = suggestion.SuggestedImprovement,
                Category = "content_improvement",
                Priority = taskPriority++,
                EstimatedEffort = suggestion.EstimatedEffort,
                ExpectedImpact = suggestion.ExpectedImpact,
                Steps = suggestion.SpecificActions,
                TargetMetric = "推荐强度",
                SuccessCriteria = "减少中性提及，增加推荐性提及"
            });
        }

        // 从曝光路径生成任务
        foreach (var opp in recommendations.GetExposure.Opportunities.Take(2))
        {
            tasks.Add(new ActionableTask
            {
                Title = $"曝光: {opp.Channel} - {opp.SpecificTarget}",
                Description = opp.ActionDescription,
                Category = "exposure",
                Priority = taskPriority++,
                EstimatedEffort = opp.EstimatedEffort,
                ExpectedImpact = opp.ExpectedImpact,
                Steps = opp.Steps,
                TargetMetric = "外部提及数",
                SuccessCriteria = "在目标渠道获得品牌提及"
            });
        }

        // 按优先级排序
        return tasks.OrderBy(t => t.Priority).ToList();
    }

    private string GenerateReportSummary(FourSignalInsight signals, ThreePathRecommendation recommendations)
    {
        return $"综合评分 {signals.OverallScore:F0}/100。{signals.Summary} " +
               $"建议优先执行「{GetPathName(recommendations.RecommendedPriority)}」策略。{recommendations.PriorityReason}";
    }

    private string GetPathName(string path)
    {
        return path switch
        {
            "create" => "创建新内容",
            "improve" => "改进现有内容",
            "exposure" => "获取曝光",
            _ => path
        };
    }

    private string ClassifyQuestionType(string question)
    {
        var q = question.ToLowerInvariant();
        
        if (q.Contains("最好") || q.Contains("推荐") || q.Contains("best") || q.Contains("recommend"))
            return "推荐类";
        if (q.Contains("如何") || q.Contains("怎么") || q.Contains("how to"))
            return "教程类";
        if (q.Contains("对比") || q.Contains("vs") || q.Contains("compare"))
            return "对比类";
        if (q.Contains("什么是") || q.Contains("what is"))
            return "定义类";
        if (q.Contains("价格") || q.Contains("多少钱") || q.Contains("price") || q.Contains("cost"))
            return "价格类";
        
        return "一般类";
    }
}
