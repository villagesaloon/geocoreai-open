using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.ContentReuse;

/// <summary>
/// 内容复用服务 (5.18-5.19)
/// </summary>
public class ContentReuseService
{
    private readonly ILogger<ContentReuseService> _logger;

    // 平台配置
    private static readonly Dictionary<string, PlatformConfig> PlatformConfigs = new()
    {
        ["reddit"] = new PlatformConfig
        {
            Id = "reddit",
            Name = "Reddit",
            OptimalLength = (150, 500),
            SuitableContentTypes = new() { "discussion", "question", "guide", "comparison" },
            Characteristics = new() { "社区驱动", "反对自我推广", "重视价值内容" },
            BestPractices = new() { "提供真实价值", "参与讨论", "避免直接推广", "使用适当的 Subreddit" },
            AICitationPotential = 0.85
        },
        ["twitter"] = new PlatformConfig
        {
            Id = "twitter",
            Name = "Twitter/X",
            OptimalLength = (50, 280),
            SuitableContentTypes = new() { "news", "opinion", "tips", "announcement" },
            Characteristics = new() { "简短精炼", "实时性强", "话题标签重要" },
            BestPractices = new() { "使用话题标签", "简洁有力", "配图提升互动", "线程展开长内容" },
            AICitationPotential = 0.6
        },
        ["linkedin"] = new PlatformConfig
        {
            Id = "linkedin",
            Name = "LinkedIn",
            OptimalLength = (200, 1300),
            SuitableContentTypes = new() { "professional", "case_study", "industry_insight", "career" },
            Characteristics = new() { "专业导向", "B2B 友好", "长文表现好" },
            BestPractices = new() { "专业语调", "分享行业见解", "使用换行增加可读性", "添加 CTA" },
            AICitationPotential = 0.7
        },
        ["medium"] = new PlatformConfig
        {
            Id = "medium",
            Name = "Medium",
            OptimalLength = (1000, 3000),
            SuitableContentTypes = new() { "guide", "tutorial", "opinion", "story" },
            Characteristics = new() { "长文友好", "SEO 价值高", "专业读者" },
            BestPractices = new() { "深度内容", "良好的标题", "使用小标题", "添加图片" },
            AICitationPotential = 0.8
        },
        ["quora"] = new PlatformConfig
        {
            Id = "quora",
            Name = "Quora",
            OptimalLength = (200, 800),
            SuitableContentTypes = new() { "faq", "explanation", "howto", "comparison" },
            Characteristics = new() { "问答格式", "专家回答", "长尾流量" },
            BestPractices = new() { "直接回答问题", "提供专业见解", "引用来源", "结构化回答" },
            AICitationPotential = 0.75
        },
        ["youtube"] = new PlatformConfig
        {
            Id = "youtube",
            Name = "YouTube (描述)",
            OptimalLength = (100, 500),
            SuitableContentTypes = new() { "tutorial", "review", "howto", "entertainment" },
            Characteristics = new() { "视频为主", "描述辅助 SEO", "长尾价值" },
            BestPractices = new() { "关键词在前", "时间戳", "CTA", "链接资源" },
            AICitationPotential = 0.5
        },
        ["newsletter"] = new PlatformConfig
        {
            Id = "newsletter",
            Name = "Newsletter",
            OptimalLength = (500, 1500),
            SuitableContentTypes = new() { "curated", "insight", "update", "exclusive" },
            Characteristics = new() { "直达用户", "高打开率", "建立关系" },
            BestPractices = new() { "吸引人的主题行", "个人化语调", "明确价值", "一个主要 CTA" },
            AICitationPotential = 0.3
        },
        ["blog"] = new PlatformConfig
        {
            Id = "blog",
            Name = "Blog",
            OptimalLength = (1500, 4000),
            SuitableContentTypes = new() { "guide", "tutorial", "listicle", "case_study" },
            Characteristics = new() { "SEO 核心", "深度内容", "品牌资产" },
            BestPractices = new() { "2500-4000 词最佳", "结构化内容", "内链外链", "Schema 标记" },
            AICitationPotential = 0.9
        }
    };

    // 内容类型检测模式
    private static readonly Dictionary<string, string[]> ContentTypePatterns = new()
    {
        ["guide"] = new[] { "指南", "教程", "完整", "guide", "tutorial", "complete", "ultimate" },
        ["comparison"] = new[] { "对比", "比较", "vs", "versus", "comparison", "差异" },
        ["faq"] = new[] { "问答", "FAQ", "常见问题", "Q&A", "questions" },
        ["howto"] = new[] { "如何", "怎么", "步骤", "how to", "steps", "方法" },
        ["listicle"] = new[] { "个", "种", "条", "大", "tips", "ways", "reasons", "things" },
        ["news"] = new[] { "发布", "宣布", "最新", "release", "announce", "new", "update" },
        ["opinion"] = new[] { "观点", "看法", "认为", "opinion", "think", "believe", "perspective" },
        ["case_study"] = new[] { "案例", "实践", "case study", "example", "实例" }
    };

    public ContentReuseService(ILogger<ContentReuseService> logger)
    {
        _logger = logger;
    }

    #region 5.18 内容复用工作流

    /// <summary>
    /// 转换内容到多个平台
    /// </summary>
    public ContentReuseResult TransformContent(ContentReuseRequest request)
    {
        var result = new ContentReuseResult();

        // 分析原始内容
        result.OriginalSummary = AnalyzeContent(request.OriginalContent, request.Language);

        // 确定目标平台
        var platforms = request.TargetPlatforms.Count > 0
            ? request.TargetPlatforms
            : GetRecommendedPlatforms(result.OriginalSummary.ContentType);

        // 为每个平台生成内容
        foreach (var platform in platforms)
        {
            if (PlatformConfigs.TryGetValue(platform.ToLower(), out var config))
            {
                var platformContent = TransformForPlatform(
                    request.OriginalContent,
                    request.Title,
                    config,
                    result.OriginalSummary,
                    request.Language
                );
                result.PlatformContents.Add(platformContent);
            }
        }

        // 生成复用建议
        result.Suggestions = GenerateReuseSuggestions(result);

        return result;
    }

    private ContentSummary AnalyzeContent(string content, string language)
    {
        var summary = new ContentSummary();

        // 词数
        summary.WordCount = language == "zh"
            ? content.Length
            : content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // 检测内容类型
        summary.ContentType = DetectContentType(content);

        // 提取关键要点
        summary.KeyPoints = ExtractKeyPoints(content);

        // 提取实体
        summary.Entities = ExtractSimpleEntities(content);

        return summary;
    }

    private string DetectContentType(string content)
    {
        foreach (var (type, patterns) in ContentTypePatterns)
        {
            if (patterns.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase)))
            {
                return type;
            }
        }
        return "general";
    }

    private List<string> ExtractKeyPoints(string content)
    {
        // 提取带有数字或关键标记的句子
        var sentences = Regex.Split(content, @"[。.!！?？]")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Where(s => Regex.IsMatch(s, @"\d+|关键|重要|核心|key|important|main|critical"))
            .Take(5)
            .Select(s => s.Trim())
            .ToList();

        return sentences;
    }

    private List<string> ExtractSimpleEntities(string content)
    {
        var entities = new List<string>();

        // 提取大写开头的词（英文）
        var capitalWords = Regex.Matches(content, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b")
            .Select(m => m.Value)
            .Distinct()
            .Take(10);
        entities.AddRange(capitalWords);

        // 提取引号内容
        var quoted = Regex.Matches(content, @"[""「]([^""」]+)[""」]")
            .Select(m => m.Groups[1].Value)
            .Where(s => s.Length < 30)
            .Take(5);
        entities.AddRange(quoted);

        return entities.Distinct().ToList();
    }

    private List<string> GetRecommendedPlatforms(string contentType)
    {
        return PlatformConfigs
            .Where(kv => kv.Value.SuitableContentTypes.Contains(contentType))
            .OrderByDescending(kv => kv.Value.AICitationPotential)
            .Take(4)
            .Select(kv => kv.Key)
            .ToList();
    }

    private PlatformContent TransformForPlatform(
        string content,
        string? title,
        PlatformConfig config,
        ContentSummary summary,
        string language)
    {
        var result = new PlatformContent
        {
            Platform = config.Id,
            PlatformDisplayName = config.Name
        };

        // 根据平台转换内容
        switch (config.Id)
        {
            case "twitter":
                result = TransformForTwitter(content, title, summary);
                break;
            case "linkedin":
                result = TransformForLinkedIn(content, title, summary);
                break;
            case "reddit":
                result = TransformForReddit(content, title, summary);
                break;
            case "medium":
                result = TransformForMedium(content, title, summary);
                break;
            case "quora":
                result = TransformForQuora(content, title, summary);
                break;
            default:
                result = TransformGeneric(content, title, config, summary);
                break;
        }

        result.Platform = config.Id;
        result.PlatformDisplayName = config.Name;
        result.WordCount = language == "zh"
            ? result.Content.Length
            : result.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        result.MeetsBestPractices = result.WordCount >= config.OptimalLength.Min
            && result.WordCount <= config.OptimalLength.Max;
        result.PlatformTips = config.BestPractices;

        return result;
    }

    private PlatformContent TransformForTwitter(string content, string? title, ContentSummary summary)
    {
        // Twitter: 简短精炼，280 字符限制
        var keyPoint = summary.KeyPoints.FirstOrDefault() ?? content.Split('.')[0];
        var tweet = keyPoint.Length > 250 ? keyPoint[..250] + "..." : keyPoint;

        // 添加话题标签
        var hashtags = summary.Entities.Take(2).Select(e => $"#{e.Replace(" ", "")}");

        return new PlatformContent
        {
            Title = "",
            Content = $"{tweet}\n\n{string.Join(" ", hashtags)}",
            SuggestedTags = summary.Entities.Take(5).ToList()
        };
    }

    private PlatformContent TransformForLinkedIn(string content, string? title, ContentSummary summary)
    {
        // LinkedIn: 专业语调，分段
        var intro = summary.KeyPoints.FirstOrDefault() ?? "分享一些见解：";
        var points = summary.KeyPoints.Skip(1).Take(3).Select(p => $"✅ {p}");
        var cta = "\n\n你怎么看？欢迎在评论区分享你的观点。";

        var linkedInContent = $"{intro}\n\n{string.Join("\n\n", points)}{cta}";

        return new PlatformContent
        {
            Title = title ?? "",
            Content = linkedInContent,
            SuggestedTags = new() { "行业洞察", "专业分享", "职场" }
        };
    }

    private PlatformContent TransformForReddit(string content, string? title, ContentSummary summary)
    {
        // Reddit: 价值导向，避免推广
        var redditTitle = title ?? summary.KeyPoints.FirstOrDefault() ?? "Discussion";
        var body = $"**背景：**\n\n{summary.KeyPoints.FirstOrDefault()}\n\n";
        body += "**要点：**\n\n";
        body += string.Join("\n", summary.KeyPoints.Skip(1).Take(3).Select(p => $"- {p}"));
        body += "\n\n---\n\n有什么想法或经验分享吗？";

        return new PlatformContent
        {
            Title = redditTitle,
            Content = body,
            SuggestedTags = new() { "discussion", "advice", "experience" }
        };
    }

    private PlatformContent TransformForMedium(string content, string? title, ContentSummary summary)
    {
        // Medium: 保持长度，添加结构
        var mediumTitle = title ?? $"深度解析：{summary.KeyPoints.FirstOrDefault()}";
        
        // 保留大部分原始内容，添加格式
        var body = $"# {mediumTitle}\n\n";
        body += $"*{summary.KeyPoints.FirstOrDefault()}*\n\n";
        body += "---\n\n";
        body += content;
        body += "\n\n---\n\n";
        body += "**关键要点：**\n\n";
        body += string.Join("\n", summary.KeyPoints.Take(5).Select(p => $"- {p}"));

        return new PlatformContent
        {
            Title = mediumTitle,
            Content = body,
            SuggestedTags = summary.Entities.Take(5).ToList()
        };
    }

    private PlatformContent TransformForQuora(string content, string? title, ContentSummary summary)
    {
        // Quora: 问答格式
        var question = title?.Contains("?") == true || title?.Contains("？") == true
            ? title
            : $"关于{summary.Entities.FirstOrDefault() ?? "这个话题"}，有什么建议？";

        var answer = $"根据我的经验，这里有几个关键点：\n\n";
        answer += string.Join("\n\n", summary.KeyPoints.Take(4).Select((p, i) => $"**{i + 1}. {p}**"));
        answer += "\n\n希望这些信息对你有帮助！";

        return new PlatformContent
        {
            Title = question,
            Content = answer,
            SuggestedTags = new() { "专业建议", "经验分享" }
        };
    }

    private PlatformContent TransformGeneric(string content, string? title, PlatformConfig config, ContentSummary summary)
    {
        // 通用转换：根据目标长度截取
        var targetLength = (config.OptimalLength.Min + config.OptimalLength.Max) / 2;
        var transformed = content.Length > targetLength
            ? content[..targetLength] + "..."
            : content;

        return new PlatformContent
        {
            Title = title ?? "",
            Content = transformed,
            SuggestedTags = summary.Entities.Take(3).ToList()
        };
    }

    private List<string> GenerateReuseSuggestions(ContentReuseResult result)
    {
        var suggestions = new List<string>();

        // 检查是否有不符合最佳实践的平台
        var nonOptimal = result.PlatformContents.Where(p => !p.MeetsBestPractices).ToList();
        if (nonOptimal.Count > 0)
        {
            suggestions.Add($"以下平台内容长度需要调整：{string.Join(", ", nonOptimal.Select(p => p.PlatformDisplayName))}");
        }

        // 建议发布顺序
        suggestions.Add("建议发布顺序：Blog → LinkedIn → Medium → Reddit → Twitter");

        // AI 引用潜力提示
        var highPotential = result.PlatformContents
            .Where(p => PlatformConfigs.TryGetValue(p.Platform, out var c) && c.AICitationPotential >= 0.8)
            .Select(p => p.PlatformDisplayName);
        if (highPotential.Any())
        {
            suggestions.Add($"高 AI 引用潜力平台：{string.Join(", ", highPotential)}");
        }

        return suggestions;
    }

    #endregion

    #region 5.19 平台选择建议

    /// <summary>
    /// 根据内容类型推荐平台
    /// </summary>
    public PlatformSelectionResult RecommendPlatforms(PlatformSelectionRequest request)
    {
        var result = new PlatformSelectionResult();

        // 检测内容类型
        result.DetectedContentType = request.ContentType ?? DetectContentType(request.Content);

        // 计算各平台匹配度
        var recommendations = new List<PlatformRecommendation>();
        var notRecommended = new List<PlatformWarning>();

        foreach (var (id, config) in PlatformConfigs)
        {
            var score = CalculatePlatformScore(request, config, result.DetectedContentType);

            if (score >= 50)
            {
                recommendations.Add(new PlatformRecommendation
                {
                    Platform = id,
                    DisplayName = config.Name,
                    MatchScore = score,
                    Reasons = GetMatchReasons(request, config, result.DetectedContentType),
                    BestPractices = config.BestPractices,
                    ExpectedOutcome = GetExpectedOutcome(config, request.MarketingGoal),
                    Priority = score >= 80 ? "high" : score >= 65 ? "medium" : "low"
                });
            }
            else
            {
                notRecommended.Add(new PlatformWarning
                {
                    Platform = config.Name,
                    Reason = GetNotRecommendedReason(config, result.DetectedContentType)
                });
            }
        }

        result.Recommendations = recommendations.OrderByDescending(r => r.MatchScore).ToList();
        result.NotRecommended = notRecommended;

        return result;
    }

    private double CalculatePlatformScore(PlatformSelectionRequest request, PlatformConfig config, string contentType)
    {
        double score = 50;

        // 内容类型匹配
        if (config.SuitableContentTypes.Contains(contentType))
            score += 25;

        // 内容长度匹配
        int wordCount = request.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount >= config.OptimalLength.Min && wordCount <= config.OptimalLength.Max)
            score += 15;
        else if (wordCount >= config.OptimalLength.Min * 0.5 && wordCount <= config.OptimalLength.Max * 1.5)
            score += 5;

        // AI 引用潜力
        score += config.AICitationPotential * 10;

        return Math.Min(100, score);
    }

    private List<string> GetMatchReasons(PlatformSelectionRequest request, PlatformConfig config, string contentType)
    {
        var reasons = new List<string>();

        if (config.SuitableContentTypes.Contains(contentType))
            reasons.Add($"内容类型 ({contentType}) 适合此平台");

        if (config.AICitationPotential >= 0.8)
            reasons.Add("高 AI 引用潜力");

        reasons.AddRange(config.Characteristics.Take(2));

        return reasons;
    }

    private string GetExpectedOutcome(PlatformConfig config, string? goal)
    {
        return goal switch
        {
            "awareness" => $"预期在 {config.Name} 获得品牌曝光",
            "engagement" => $"预期在 {config.Name} 获得用户互动",
            "conversion" => $"预期通过 {config.Name} 引导转化",
            _ => $"在 {config.Name} 建立专业形象"
        };
    }

    private string GetNotRecommendedReason(PlatformConfig config, string contentType)
    {
        if (!config.SuitableContentTypes.Contains(contentType))
            return $"内容类型 ({contentType}) 不适合此平台";
        return "匹配度较低";
    }

    /// <summary>
    /// 获取所有平台配置
    /// </summary>
    public List<PlatformConfig> GetAllPlatforms()
    {
        return PlatformConfigs.Values.ToList();
    }

    /// <summary>
    /// 获取特定平台配置
    /// </summary>
    public PlatformConfig? GetPlatformConfig(string platformId)
    {
        return PlatformConfigs.TryGetValue(platformId.ToLower(), out var config) ? config : null;
    }

    #endregion
}
