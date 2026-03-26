using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.Reddit;

/// <summary>
/// Reddit 服务 (5.22, 5.23, 5.29, 5.30, 5.33)
/// </summary>
public class RedditService
{
    private readonly ILogger<RedditService> _logger;

    // 5 阶段参与度模型
    private static readonly List<EngagementStage> EngagementStages = new()
    {
        new EngagementStage
        {
            StageNumber = 1,
            Name = "研究",
            Description = "了解社区规则、文化和热门话题",
            Criteria = new() { "阅读社区规则", "观察热门帖子", "了解社区术语" },
            Actions = new() { "订阅目标 Subreddit", "阅读置顶帖和规则", "记录常见问题类型" }
        },
        new EngagementStage
        {
            StageNumber = 2,
            Name = "观察",
            Description = "潜水学习，理解社区动态",
            Criteria = new() { "观察至少 2 周", "识别活跃用户", "了解投票模式" },
            Actions = new() { "每日浏览 15-30 分钟", "收藏高质量帖子", "分析成功帖子特征" }
        },
        new EngagementStage
        {
            StageNumber = 3,
            Name = "参与",
            Description = "开始评论，提供价值",
            Criteria = new() { "发布 10+ 有价值评论", "获得正向 karma", "无负面反馈" },
            Actions = new() { "回答问题", "分享经验", "避免任何推广" }
        },
        new EngagementStage
        {
            StageNumber = 4,
            Name = "价值创造",
            Description = "发布原创内容，建立声誉",
            Criteria = new() { "发布 3+ 原创帖子", "帖子获得正向反馈", "被社区认可" },
            Actions = new() { "分享独特见解", "创建有用资源", "回答复杂问题" }
        },
        new EngagementStage
        {
            StageNumber = 5,
            Name = "权威建立",
            Description = "成为社区专家，可适度提及品牌",
            Criteria = new() { "被视为领域专家", "高 karma 值", "社区信任" },
            Actions = new() { "在相关讨论中自然提及", "提供专业建议", "保持 90% 价值内容" }
        }
    };

    // 自我推广检测关键词
    private static readonly string[] SelfPromotionKeywords = new[]
    {
        "check out", "visit", "click", "link", "website", "product", "service",
        "buy", "purchase", "discount", "promo", "code", "offer", "deal",
        "我们的", "我的产品", "我的网站", "点击", "链接", "购买", "优惠"
    };

    // 默认归因选项
    private static readonly List<AttributionOption> DefaultAttributionOptions = new()
    {
        new AttributionOption { Id = "google", Label = "Google 搜索", Category = "search", IsDarkFunnel = false },
        new AttributionOption { Id = "ai_search", Label = "AI 搜索 (ChatGPT/Perplexity)", Category = "ai", IsDarkFunnel = true },
        new AttributionOption { Id = "reddit", Label = "Reddit", Category = "social", IsDarkFunnel = true },
        new AttributionOption { Id = "twitter", Label = "Twitter/X", Category = "social", IsDarkFunnel = true },
        new AttributionOption { Id = "linkedin", Label = "LinkedIn", Category = "social", IsDarkFunnel = false },
        new AttributionOption { Id = "friend", Label = "朋友推荐", Category = "wom", IsDarkFunnel = true },
        new AttributionOption { Id = "podcast", Label = "播客/视频", Category = "content", IsDarkFunnel = true },
        new AttributionOption { Id = "other", Label = "其他", Category = "other", IsDarkFunnel = true }
    };

    public RedditService(ILogger<RedditService> logger)
    {
        _logger = logger;
    }

    #region 5.22 Reddit 帖子生成

    /// <summary>
    /// 生成 Reddit 帖子
    /// </summary>
    public RedditPostResult GeneratePost(RedditPostRequest request)
    {
        var result = new RedditPostResult
        {
            PostType = request.PostType
        };

        // 根据帖子类型生成标题
        result.Title = GenerateTitle(request.Content, request.PostType);

        // 生成正文
        result.Body = GenerateBody(request.Content, request.PostType);

        // 推荐 Subreddit
        result.RecommendedSubreddits = RecommendSubreddits(request.Content);

        // 评估质量
        result.QualityScore = EvaluatePostQuality(result.Title, result.Body);

        // 生成建议
        result.Suggestions = GeneratePostSuggestions(result);

        return result;
    }

    private string GenerateTitle(string content, string postType)
    {
        // 提取关键信息生成标题
        var firstSentence = content.Split(new[] { '.', '。', '!', '！', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim() ?? "";

        return postType switch
        {
            "question" => firstSentence.Length > 100 ? firstSentence[..100] + "?" : firstSentence + "?",
            "discussion" => $"Discussion: {(firstSentence.Length > 80 ? firstSentence[..80] : firstSentence)}",
            "resource" => $"[Resource] {(firstSentence.Length > 80 ? firstSentence[..80] : firstSentence)}",
            "ama" => $"AMA: {(firstSentence.Length > 80 ? firstSentence[..80] : firstSentence)}",
            _ => firstSentence.Length > 100 ? firstSentence[..100] : firstSentence
        };
    }

    private string GenerateBody(string content, string postType)
    {
        var body = content.Trim();

        // 添加格式化
        if (postType == "discussion")
        {
            body = $"**Background:**\n\n{body}\n\n**What are your thoughts?**";
        }
        else if (postType == "question")
        {
            body = $"{body}\n\n---\n\nAny insights would be appreciated!";
        }

        return body;
    }

    private List<SubredditRecommendation> RecommendSubreddits(string content)
    {
        // 基于内容关键词推荐 Subreddit
        var recommendations = new List<SubredditRecommendation>();

        // 技术相关
        if (Regex.IsMatch(content, @"(programming|code|developer|software|API|技术|开发|编程)", RegexOptions.IgnoreCase))
        {
            recommendations.Add(new SubredditRecommendation
            {
                Name = "r/programming",
                MatchScore = 0.8,
                Reason = "技术/编程相关内容",
                Rules = new() { "避免自我推广", "提供有价值的讨论" }
            });
        }

        // 营销相关
        if (Regex.IsMatch(content, @"(marketing|SEO|content|brand|营销|品牌|内容)", RegexOptions.IgnoreCase))
        {
            recommendations.Add(new SubredditRecommendation
            {
                Name = "r/marketing",
                MatchScore = 0.85,
                Reason = "营销相关内容",
                Rules = new() { "分享经验而非推广", "提供可操作建议" }
            });
        }

        // AI 相关
        if (Regex.IsMatch(content, @"(AI|artificial intelligence|machine learning|LLM|GPT|人工智能)", RegexOptions.IgnoreCase))
        {
            recommendations.Add(new SubredditRecommendation
            {
                Name = "r/artificial",
                MatchScore = 0.9,
                Reason = "AI 相关内容",
                Rules = new() { "技术讨论优先", "避免炒作" }
            });
        }

        return recommendations.OrderByDescending(r => r.MatchScore).Take(3).ToList();
    }

    private double EvaluatePostQuality(string title, string body)
    {
        double score = 5;

        // 标题长度
        if (title.Length >= 20 && title.Length <= 100) score += 1;

        // 正文长度
        int bodyWords = body.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (bodyWords >= 100 && bodyWords <= 500) score += 2;

        // 格式化
        if (body.Contains("**") || body.Contains("##")) score += 1;

        // 无自我推广
        if (!SelfPromotionKeywords.Any(k => body.Contains(k, StringComparison.OrdinalIgnoreCase)))
            score += 1;

        return Math.Min(10, score);
    }

    private List<string> GeneratePostSuggestions(RedditPostResult result)
    {
        var suggestions = new List<string>();

        if (result.QualityScore < 7)
        {
            suggestions.Add("建议增加更多细节和背景信息");
        }

        if (result.Title.Length < 20)
        {
            suggestions.Add("标题较短，建议扩展以吸引注意");
        }

        if (result.RecommendedSubreddits.Count == 0)
        {
            suggestions.Add("未找到匹配的 Subreddit，请手动选择");
        }

        return suggestions;
    }

    #endregion

    #region 5.23 Reddit 评论生成

    /// <summary>
    /// 生成 Reddit 评论
    /// 原理：120-180 词价值评论
    /// </summary>
    public RedditCommentResult GenerateComment(RedditCommentRequest request)
    {
        var result = new RedditCommentResult();

        // 生成评论
        result.Comment = GenerateCommentContent(request);

        // 计算词数
        result.WordCount = result.Comment.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // 检查是否在最佳区间
        result.IsOptimalLength = result.WordCount >= 120 && result.WordCount <= 180;

        // 自我推广分析
        result.SelfPromotion = AnalyzeSelfPromotion(result.Comment, request.BrandInfo);

        // 价值评分
        result.ValueScore = CalculateValueScore(result);

        // 生成建议
        result.Suggestions = GenerateCommentSuggestions(result);

        return result;
    }

    private string GenerateCommentContent(RedditCommentRequest request)
    {
        var style = request.Style;
        var content = request.PostContent;

        // 基于风格生成评论框架
        var comment = style switch
        {
            "expert" => $"Based on my experience in this area, I'd like to share some insights:\n\n{ExtractKeyPoints(content)}\n\nHope this helps!",
            "detailed" => $"Great question! Let me break this down:\n\n{ExtractKeyPoints(content)}\n\nFeel free to ask if you need more details.",
            "casual" => $"Hey! {ExtractKeyPoints(content)}\n\nJust my two cents!",
            _ => $"I've dealt with something similar before. {ExtractKeyPoints(content)}\n\nLet me know if you have questions!"
        };

        return comment;
    }

    private string ExtractKeyPoints(string content)
    {
        // 简化：提取前几句作为要点
        var sentences = content.Split(new[] { '.', '。' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(3)
            .Select(s => $"- {s.Trim()}");
        return string.Join("\n", sentences);
    }

    private SelfPromotionAnalysis AnalyzeSelfPromotion(string comment, string? brandInfo)
    {
        var analysis = new SelfPromotionAnalysis();
        var detectedElements = new List<string>();

        // 检测推广关键词
        foreach (var keyword in SelfPromotionKeywords)
        {
            if (comment.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                detectedElements.Add(keyword);
            }
        }

        // 检测品牌提及
        if (!string.IsNullOrEmpty(brandInfo) && comment.Contains(brandInfo, StringComparison.OrdinalIgnoreCase))
        {
            detectedElements.Add($"品牌提及: {brandInfo}");
        }

        // 检测链接
        if (Regex.IsMatch(comment, @"https?://|www\.", RegexOptions.IgnoreCase))
        {
            detectedElements.Add("包含链接");
        }

        analysis.DetectedElements = detectedElements;
        analysis.HasSelfPromotion = detectedElements.Count > 0;
        analysis.Intensity = Math.Min(10, detectedElements.Count * 2);
        analysis.RiskLevel = analysis.Intensity switch
        {
            >= 6 => "high",
            >= 3 => "medium",
            _ => "low"
        };

        return analysis;
    }

    private double CalculateValueScore(RedditCommentResult result)
    {
        double score = 5;

        // 长度评分
        if (result.IsOptimalLength) score += 2;
        else if (result.WordCount >= 80 && result.WordCount <= 200) score += 1;

        // 自我推广扣分
        score -= result.SelfPromotion.Intensity * 0.3;

        // 格式化加分
        if (result.Comment.Contains("-") || result.Comment.Contains("*")) score += 1;

        return Math.Max(0, Math.Min(10, score));
    }

    private List<string> GenerateCommentSuggestions(RedditCommentResult result)
    {
        var suggestions = new List<string>();

        if (!result.IsOptimalLength)
        {
            if (result.WordCount < 120)
                suggestions.Add($"评论较短 ({result.WordCount} 词)，建议扩展到 120-180 词以提供更多价值");
            else
                suggestions.Add($"评论较长 ({result.WordCount} 词)，建议精简到 180 词以内");
        }

        if (result.SelfPromotion.HasSelfPromotion)
        {
            suggestions.Add($"检测到自我推广元素，风险等级: {result.SelfPromotion.RiskLevel}");
            if (result.SelfPromotion.RiskLevel == "high")
                suggestions.Add("强烈建议移除推广内容，专注于提供价值");
        }

        return suggestions;
    }

    #endregion

    #region 5.29 Reddit 暗漏斗监测

    /// <summary>
    /// 评估暗漏斗状态
    /// 原理：Reddit 声誉 = AI 搜索声誉，90 天监测后再营销
    /// </summary>
    public DarkFunnelResult AssessDarkFunnel(DarkFunnelConfig config)
    {
        var result = new DarkFunnelResult
        {
            MonitoringPeriod = new DateRange
            {
                Start = DateTime.UtcNow.AddDays(-config.MonitoringPeriodDays),
                End = DateTime.UtcNow
            }
        };

        // 模拟统计数据（实际需要 Reddit API）
        result.MentionStats = new BrandMentionStats
        {
            TotalMentions = 0,
            PostMentions = 0,
            CommentMentions = 0,
            WeeklyTrends = new List<WeeklyTrend>()
        };

        result.Sentiment = new SentimentSummary
        {
            OverallScore = 0,
            PositiveCount = 0,
            NeutralCount = 0,
            NegativeCount = 0
        };

        // 营销时机建议
        result.TimingAdvice = GenerateTimingAdvice(result, config.MonitoringPeriodDays);

        return result;
    }

    private MarketingTimingAdvice GenerateTimingAdvice(DarkFunnelResult result, int monitoringDays)
    {
        var advice = new MarketingTimingAdvice();

        // 90 天规则
        if (monitoringDays < 90)
        {
            advice.IsReadyForMarketing = false;
            advice.SuggestedWaitDays = 90 - monitoringDays;
            advice.CurrentStage = "观察期";
            advice.Recommendations = new()
            {
                $"建议继续观察 {advice.SuggestedWaitDays} 天",
                "在此期间专注于提供价值内容",
                "建立社区信任和声誉"
            };
        }
        else
        {
            advice.IsReadyForMarketing = result.Sentiment.OverallScore >= 0.3;
            advice.SuggestedWaitDays = 0;
            advice.CurrentStage = advice.IsReadyForMarketing ? "可营销" : "需改善";
            advice.Recommendations = advice.IsReadyForMarketing
                ? new() { "可以开始适度的品牌提及", "保持 90% 价值内容", "监测社区反馈" }
                : new() { "社区情感偏负面，建议先改善声誉", "增加正面互动", "解决用户问题" };
        }

        return advice;
    }

    #endregion

    #region 5.30 自报告归因字段

    /// <summary>
    /// 获取默认归因配置
    /// </summary>
    public SelfReportAttributionConfig GetDefaultAttributionConfig()
    {
        return new SelfReportAttributionConfig
        {
            FieldLabel = "您是如何知道我们的？",
            Options = DefaultAttributionOptions,
            AllowCustomInput = true
        };
    }

    /// <summary>
    /// 分析归因统计
    /// </summary>
    public AttributionStats AnalyzeAttributions(List<string> responses)
    {
        var stats = new AttributionStats
        {
            TotalResponses = responses.Count
        };

        if (responses.Count == 0) return stats;

        // 统计各来源
        var bySource = responses.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        stats.BySource = bySource;

        // 计算暗漏斗占比
        var darkFunnelSources = DefaultAttributionOptions.Where(o => o.IsDarkFunnel).Select(o => o.Id);
        int darkFunnelCount = bySource.Where(kv => darkFunnelSources.Contains(kv.Key)).Sum(kv => kv.Value);
        stats.DarkFunnelPercentage = (double)darkFunnelCount / responses.Count * 100;

        // AI 搜索占比
        if (bySource.TryGetValue("ai_search", out int aiCount))
            stats.AISearchPercentage = (double)aiCount / responses.Count * 100;

        // Reddit 占比
        if (bySource.TryGetValue("reddit", out int redditCount))
            stats.RedditPercentage = (double)redditCount / responses.Count * 100;

        return stats;
    }

    #endregion

    #region 5.33 Reddit 参与度评估

    /// <summary>
    /// 评估 Reddit 参与度
    /// 原理：5 阶段：研究 - 观察 - 参与 - 价值创造 - 权威建立
    /// </summary>
    public EngagementAssessmentResult AssessEngagement(EngagementAssessmentRequest request)
    {
        var result = new EngagementAssessmentResult
        {
            Stages = EngagementStages.Select(s => new EngagementStage
            {
                StageNumber = s.StageNumber,
                Name = s.Name,
                Description = s.Description,
                Criteria = s.Criteria,
                Actions = s.Actions,
                IsCompleted = false,
                IsCurrent = false
            }).ToList()
        };

        // 默认从第一阶段开始
        result.CurrentStage = 1;
        result.StageName = "研究";
        result.StageDescription = "了解社区规则、文化和热门话题";
        result.StageProgress = 0;

        // 标记当前阶段
        result.Stages[0].IsCurrent = true;

        // 生成下一步行动
        result.NextActions = new()
        {
            "订阅目标 Subreddit: " + string.Join(", ", request.TargetSubreddits.Take(3)),
            "阅读各社区的规则和置顶帖",
            "观察热门帖子的特征和讨论风格"
        };

        // 风险提示
        result.Warnings = new()
        {
            "不要在研究阶段发布任何内容",
            "避免过早提及品牌或产品",
            "Reddit 社区对自我推广非常敏感"
        };

        return result;
    }

    /// <summary>
    /// 获取所有参与度阶段
    /// </summary>
    public List<EngagementStage> GetEngagementStages()
    {
        return EngagementStages;
    }

    #endregion
}
