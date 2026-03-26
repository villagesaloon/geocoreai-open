using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// 高级内容分析器 (3.23-3.27)
/// 提供内容类型策略、结构元素分析、长度优化、可引用性评分、前30%优化
/// </summary>
public class AdvancedContentAnalyzer
{
    private readonly ILogger<AdvancedContentAnalyzer> _logger;
    private readonly ClaimExtractor _claimExtractor;
    private readonly EntityExtractor _entityExtractor;

    // 内容类型引用率基准 (3.23)
    private static readonly List<ContentTypeBenchmark> ContentTypeBenchmarks = new()
    {
        new ContentTypeBenchmark
        {
            ContentType = "guide",
            ContentTypeName = "综合指南",
            BenchmarkCitationRate = 67,
            Priority = 1,
            UseCases = new() { "产品使用教程", "行业入门指南", "最佳实践总结" },
            BestPractices = new() { "覆盖主题的各个方面", "提供具体步骤", "包含专家建议" }
        },
        new ContentTypeBenchmark
        {
            ContentType = "comparison",
            ContentTypeName = "对比分析",
            BenchmarkCitationRate = 61,
            Priority = 2,
            UseCases = new() { "产品对比", "方案选型", "技术比较" },
            BestPractices = new() { "使用表格对比", "客观中立", "提供明确结论" }
        },
        new ContentTypeBenchmark
        {
            ContentType = "faq",
            ContentTypeName = "FAQ问答",
            BenchmarkCitationRate = 58,
            Priority = 3,
            UseCases = new() { "常见问题解答", "产品FAQ", "技术支持" },
            BestPractices = new() { "10+个问题效果最佳", "答案简洁直接", "使用FAQ Schema" }
        },
        new ContentTypeBenchmark
        {
            ContentType = "howto",
            ContentTypeName = "操作指南",
            BenchmarkCitationRate = 54,
            Priority = 4,
            UseCases = new() { "步骤教程", "操作手册", "配置指南" },
            BestPractices = new() { "编号步骤", "配图说明", "使用HowTo Schema" }
        },
        new ContentTypeBenchmark
        {
            ContentType = "listicle",
            ContentTypeName = "列表文章",
            BenchmarkCitationRate = 52,
            Priority = 5,
            UseCases = new() { "Top N 推荐", "技巧汇总", "资源列表" },
            BestPractices = new() { "避免自我推广", "引用第三方来源", "数据支撑" }
        },
        new ContentTypeBenchmark
        {
            ContentType = "review",
            ContentTypeName = "评测评论",
            BenchmarkCitationRate = 45,
            Priority = 6,
            UseCases = new() { "产品评测", "服务体验", "工具测评" },
            BestPractices = new() { "真实体验", "优缺点分析", "评分标准" }
        },
        new ContentTypeBenchmark
        {
            ContentType = "opinion",
            ContentTypeName = "观点文章",
            BenchmarkCitationRate = 18,
            Priority = 7,
            UseCases = new() { "行业观点", "趋势预测", "个人见解" },
            BestPractices = new() { "数据支撑观点", "引用权威来源", "逻辑清晰" }
        }
    };

    // 结构元素影响系数 (3.24)
    private static readonly Dictionary<string, double> ElementImpactFactors = new()
    {
        { "h2", 3.2 },
        { "h3", 2.5 },
        { "table", 2.8 },
        { "list", 2.0 },
        { "faq", 2.56 }, // FAQ 10+ = +156%
        { "code", 1.5 },
        { "blockquote", 1.8 }
    };

    public AdvancedContentAnalyzer(
        ILogger<AdvancedContentAnalyzer> logger,
        ClaimExtractor claimExtractor,
        EntityExtractor entityExtractor)
    {
        _logger = logger;
        _claimExtractor = claimExtractor;
        _entityExtractor = entityExtractor;
    }

    #region 3.23 内容类型引用率基准

    /// <summary>
    /// 获取内容类型策略建议 (3.23)
    /// </summary>
    public ContentTypeStrategy GetContentTypeStrategy(string content, string language = "zh")
    {
        var detectedType = DetectContentType(content, language);
        var currentBenchmark = ContentTypeBenchmarks.FirstOrDefault(b => b.ContentType == detectedType);

        var suggestions = new List<string>();
        
        if (currentBenchmark != null && currentBenchmark.BenchmarkCitationRate < 50)
        {
            suggestions.Add($"当前内容类型「{currentBenchmark.ContentTypeName}」引用率较低（{currentBenchmark.BenchmarkCitationRate}%），建议转换为综合指南或对比分析");
        }

        if (detectedType == "opinion")
        {
            suggestions.Add("观点类文章引用率最低（18%），建议增加数据支撑和权威引用");
        }

        return new ContentTypeStrategy
        {
            RecommendedTypes = ContentTypeBenchmarks.OrderByDescending(b => b.BenchmarkCitationRate).ToList(),
            DetectedType = detectedType,
            CurrentBenchmark = currentBenchmark?.BenchmarkCitationRate ?? 0,
            Suggestions = suggestions
        };
    }

    /// <summary>
    /// 检测内容类型
    /// </summary>
    private string DetectContentType(string content, string language)
    {
        var lowerContent = content.ToLower();
        
        // FAQ 检测
        var faqPatterns = language == "zh" 
            ? new[] { "问：", "答：", "Q：", "A：", "常见问题", "FAQ" }
            : new[] { "Q:", "A:", "FAQ", "frequently asked", "question:" };
        if (faqPatterns.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            var faqCount = Regex.Matches(content, language == "zh" ? @"问[：:]|Q[：:]" : @"Q:|question:", RegexOptions.IgnoreCase).Count;
            if (faqCount >= 3) return "faq";
        }

        // HowTo 检测
        var howtoPatterns = language == "zh"
            ? new[] { "步骤", "第一步", "第二步", "如何", "怎么", "教程" }
            : new[] { "step 1", "step 2", "how to", "tutorial", "guide" };
        if (howtoPatterns.Any(p => lowerContent.Contains(p.ToLower())))
        {
            var stepCount = Regex.Matches(content, language == "zh" ? @"第[一二三四五六七八九十\d]+步|步骤\s*\d+" : @"step\s*\d+", RegexOptions.IgnoreCase).Count;
            if (stepCount >= 3) return "howto";
        }

        // Comparison 检测
        var comparisonPatterns = language == "zh"
            ? new[] { "对比", "比较", "VS", "versus", "区别", "差异" }
            : new[] { "vs", "versus", "comparison", "compare", "difference" };
        if (comparisonPatterns.Any(p => lowerContent.Contains(p.ToLower())))
            return "comparison";

        // Listicle 检测
        var listicleMatches = Regex.Matches(content, @"^\s*(\d+[\.\)、]|[-•*])\s*", RegexOptions.Multiline);
        if (listicleMatches.Count >= 5)
            return "listicle";

        // Review 检测
        var reviewPatterns = language == "zh"
            ? new[] { "评测", "测评", "体验", "优点", "缺点", "评分" }
            : new[] { "review", "rating", "pros", "cons", "experience" };
        if (reviewPatterns.Any(p => lowerContent.Contains(p.ToLower())))
            return "review";

        // Opinion 检测
        var opinionPatterns = language == "zh"
            ? new[] { "我认为", "我觉得", "个人观点", "我的看法" }
            : new[] { "i think", "i believe", "in my opinion", "my view" };
        if (opinionPatterns.Any(p => lowerContent.Contains(p.ToLower())))
            return "opinion";

        // 默认为 Guide
        return "guide";
    }

    #endregion

    #region 3.24 结构元素影响系数

    /// <summary>
    /// 分析结构元素 (3.24)
    /// </summary>
    public StructuralElementsMetric AnalyzeStructuralElements(string content, string language = "zh")
    {
        var elements = new List<StructuralElement>();
        
        // H2 检测
        var h2Count = Regex.Matches(content, @"^##\s+[^#]|<h2[^>]*>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        elements.Add(new StructuralElement
        {
            Type = "h2",
            Count = h2Count,
            ImpactFactor = ElementImpactFactors["h2"],
            IsOptimal = h2Count >= 3 && h2Count <= 10,
            RecommendedRange = "3-10"
        });

        // H3 检测
        var h3Count = Regex.Matches(content, @"^###\s+[^#]|<h3[^>]*>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        elements.Add(new StructuralElement
        {
            Type = "h3",
            Count = h3Count,
            ImpactFactor = ElementImpactFactors["h3"],
            IsOptimal = h3Count >= 2,
            RecommendedRange = "2+"
        });

        // 表格检测
        var tableCount = Regex.Matches(content, @"\|[^\|]+\||\<table[^>]*>", RegexOptions.IgnoreCase).Count > 0 ? 1 : 0;
        tableCount += Regex.Matches(content, @"<table[^>]*>", RegexOptions.IgnoreCase).Count;
        elements.Add(new StructuralElement
        {
            Type = "table",
            Count = tableCount,
            ImpactFactor = ElementImpactFactors["table"],
            IsOptimal = tableCount >= 1,
            RecommendedRange = "1+"
        });

        // 列表检测
        var listCount = Regex.Matches(content, @"^\s*[-•*]\s+|^\s*\d+[\.\)]\s+", RegexOptions.Multiline).Count;
        var listGroups = listCount / 3; // 估算列表组数
        elements.Add(new StructuralElement
        {
            Type = "list",
            Count = listGroups,
            ImpactFactor = ElementImpactFactors["list"],
            IsOptimal = listGroups >= 2,
            RecommendedRange = "2+"
        });

        // FAQ 检测
        var faqPattern = language == "zh" ? @"问[：:]|Q[：:]" : @"Q:|question:";
        var faqCount = Regex.Matches(content, faqPattern, RegexOptions.IgnoreCase).Count;
        elements.Add(new StructuralElement
        {
            Type = "faq",
            Count = faqCount,
            ImpactFactor = faqCount >= 10 ? ElementImpactFactors["faq"] : 1.0,
            IsOptimal = faqCount >= 10,
            RecommendedRange = "10+ (+156%)"
        });

        // 代码块检测
        var codeCount = Regex.Matches(content, @"```[\s\S]*?```|<code[^>]*>", RegexOptions.IgnoreCase).Count;
        elements.Add(new StructuralElement
        {
            Type = "code",
            Count = codeCount,
            ImpactFactor = ElementImpactFactors["code"],
            IsOptimal = true,
            RecommendedRange = "按需"
        });

        // 引用块检测
        var blockquoteCount = Regex.Matches(content, @"^>\s+|<blockquote[^>]*>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        elements.Add(new StructuralElement
        {
            Type = "blockquote",
            Count = blockquoteCount,
            ImpactFactor = ElementImpactFactors["blockquote"],
            IsOptimal = blockquoteCount >= 1,
            RecommendedRange = "1+"
        });

        // 计算综合影响系数
        var impactMultiplier = 1.0;
        foreach (var elem in elements.Where(e => e.Count > 0))
        {
            impactMultiplier *= (1 + (elem.ImpactFactor - 1) * Math.Min(elem.Count, 3) / 10);
        }

        // 计算评分
        var optimalCount = elements.Count(e => e.IsOptimal);
        var score = Math.Min(10, optimalCount * 1.5 + (impactMultiplier - 1) * 5);

        // 生成建议
        var suggestions = new List<string>();
        if (h2Count < 3) suggestions.Add("建议添加更多 H2 标题（3-10个），可提升 3.2x 引用率");
        if (tableCount == 0) suggestions.Add("建议添加表格对比，可提升 2.8x 引用率");
        if (faqCount > 0 && faqCount < 10) suggestions.Add($"当前 FAQ 数量 {faqCount}，建议增加到 10+ 可获得 +156% 引用率提升");
        if (blockquoteCount == 0) suggestions.Add("建议添加专家引语，增强权威性");

        return new StructuralElementsMetric
        {
            Score = Math.Round(score, 1),
            H2Count = h2Count,
            H3Count = h3Count,
            TableCount = tableCount,
            ListCount = listGroups,
            FaqCount = faqCount,
            CodeBlockCount = codeCount,
            BlockquoteCount = blockquoteCount,
            ImpactMultiplier = Math.Round(impactMultiplier, 2),
            Elements = elements,
            Suggestions = suggestions
        };
    }

    #endregion

    #region 3.25 最佳长度区间检测

    /// <summary>
    /// 检测最佳长度区间 (3.25)
    /// </summary>
    public OptimalLengthMetric AnalyzeOptimalLength(string content, double claimDensity, string language = "zh")
    {
        var wordCount = language == "zh" 
            ? content.Length 
            : content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // 评分逻辑：密度 > 长度
        double score;
        string suggestion;

        if (wordCount >= 2500 && wordCount <= 4000)
        {
            score = 10;
            suggestion = "内容长度在最佳区间（2,500-4,000词）";
        }
        else if (wordCount >= 1500 && wordCount < 2500)
        {
            score = 7 + (wordCount - 1500) / 1000.0 * 3;
            suggestion = $"建议扩展到 2,500+ 词，当前 {wordCount} 词";
        }
        else if (wordCount > 4000 && wordCount <= 6000)
        {
            score = 8 - (wordCount - 4000) / 2000.0 * 2;
            suggestion = "内容略长，确保密度优先于长度";
        }
        else if (wordCount < 1500)
        {
            score = Math.Max(3, wordCount / 500.0 * 2);
            suggestion = $"内容过短（{wordCount}词），建议扩展到 2,500+ 词";
        }
        else
        {
            score = 5;
            suggestion = "内容过长，建议拆分为多篇文章";
        }

        // 密度加成：高密度可以弥补长度不足
        var densityBonus = claimDensity >= 4 ? 1.0 : claimDensity / 4.0;
        score = Math.Min(10, score * (0.7 + 0.3 * densityBonus));

        return new OptimalLengthMetric
        {
            Score = Math.Round(score, 1),
            WordCount = wordCount,
            DensityScore = Math.Round(claimDensity, 1),
            Suggestion = suggestion
        };
    }

    #endregion

    #region 3.26 可引用性评分

    /// <summary>
    /// 分析可引用性 (3.26)
    /// </summary>
    public async Task<CitabilityScoreMetric> AnalyzeCitabilityAsync(string content, string language = "zh")
    {
        // 分割段落
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Trim().Length > 50)
            .ToList();

        var citableParagraphs = new List<CitableParagraph>();
        var paragraphsToOptimize = new List<ParagraphOptimization>();

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var para = paragraphs[i].Trim();
            var wordCount = language == "zh" ? para.Length : para.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            
            // 提取事实
            var claims = await _claimExtractor.ExtractClaimsAsync(para, language);
            var factDensity = wordCount > 0 ? claims.Count / (wordCount / 100.0) : 0;

            // 评估可引用性
            var isOptimalLength = wordCount >= 134 && wordCount <= 167;
            var isSelfContained = !para.StartsWith("这") && !para.StartsWith("它") && !para.StartsWith("This") && !para.StartsWith("It ");
            var isDirectAnswer = Regex.IsMatch(para, language == "zh" 
                ? @"^[^，。]+是[^，。]+[。]" 
                : @"^[A-Z][^.]+\s+(is|are|was|were)\s+", RegexOptions.IgnoreCase);
            var hasFactualContent = claims.Count >= 1;

            // 计算段落可引用性评分
            var citabilityScore = 0.0;
            if (isOptimalLength) citabilityScore += 3;
            else if (wordCount >= 100 && wordCount <= 200) citabilityScore += 2;
            if (isSelfContained) citabilityScore += 2.5;
            if (isDirectAnswer) citabilityScore += 2;
            if (hasFactualContent) citabilityScore += Math.Min(2.5, factDensity * 0.5);

            if (citabilityScore >= 6)
            {
                citableParagraphs.Add(new CitableParagraph
                {
                    Index = i,
                    Preview = para.Length > 200 ? para.Substring(0, 200) + "..." : para,
                    WordCount = wordCount,
                    CitabilityScore = Math.Round(citabilityScore, 1),
                    IsSelfContained = isSelfContained,
                    FactDensity = Math.Round(factDensity, 1),
                    IsDirectAnswer = isDirectAnswer
                });
            }
            else
            {
                var issues = new List<string>();
                var suggestions = new List<string>();

                if (!isOptimalLength)
                {
                    issues.Add($"长度 {wordCount} 词，不在最佳区间");
                    suggestions.Add("调整到 134-167 词");
                }
                if (!isSelfContained)
                {
                    issues.Add("段落不自包含，依赖上下文");
                    suggestions.Add("避免使用代词开头");
                }
                if (!isDirectAnswer)
                {
                    issues.Add("未直接回答问题");
                    suggestions.Add("使用「X 是 Y」格式开头");
                }
                if (!hasFactualContent)
                {
                    issues.Add("缺少事实性内容");
                    suggestions.Add("添加数据、统计或具体事实");
                }

                if (issues.Count > 0)
                {
                    paragraphsToOptimize.Add(new ParagraphOptimization
                    {
                        Index = i,
                        Preview = para.Length > 100 ? para.Substring(0, 100) + "..." : para,
                        Issues = issues,
                        Suggestions = suggestions
                    });
                }
            }
        }

        // 计算综合评分
        var citableRatio = paragraphs.Count > 0 ? (double)citableParagraphs.Count / paragraphs.Count : 0;
        var avgCitability = citableParagraphs.Count > 0 ? citableParagraphs.Average(p => p.CitabilityScore) : 0;
        var score = citableRatio * 5 + avgCitability * 0.5;

        return new CitabilityScoreMetric
        {
            Score = Math.Round(Math.Min(10, score), 1),
            CitableParagraphCount = citableParagraphs.Count,
            TotalParagraphCount = paragraphs.Count,
            TopCitableParagraphs = citableParagraphs.OrderByDescending(p => p.CitabilityScore).Take(5).ToList(),
            ParagraphsToOptimize = paragraphsToOptimize.Take(5).ToList()
        };
    }

    #endregion

    #region 3.27 内容前 30% 优化

    /// <summary>
    /// 分析内容前 30% (3.27)
    /// </summary>
    public async Task<Front30PercentMetric> AnalyzeFront30PercentAsync(string content, string language = "zh")
    {
        var totalWordCount = language == "zh" 
            ? content.Length 
            : content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        var front30Length = (int)(content.Length * 0.3);
        var front30Content = content.Substring(0, Math.Min(front30Length, content.Length));
        var back70Content = content.Length > front30Length ? content.Substring(front30Length) : "";

        var front30WordCount = language == "zh" 
            ? front30Content.Length 
            : front30Content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // 提取前 30% 的事实
        var front30Claims = await _claimExtractor.ExtractClaimsAsync(front30Content, language);
        var front30ClaimDensity = front30WordCount > 0 ? front30Claims.Count / (front30WordCount / 100.0) : 0;

        // 提取后 70% 的事实
        var back70Claims = await _claimExtractor.ExtractClaimsAsync(back70Content, language);
        var back70WordCount = totalWordCount - front30WordCount;
        var back70ClaimDensity = back70WordCount > 0 ? back70Claims.Count / (back70WordCount / 100.0) : 0;

        // 提取前 30% 的实体
        var front30Entities = await _entityExtractor.ExtractEntitiesAsync(front30Content, language);

        // 检测关键元素
        var elements = new Front30Elements
        {
            HasAnswerCapsule = Regex.IsMatch(front30Content, language == "zh" 
                ? @"^[^，。]{20,100}[。]" 
                : @"^[A-Z][^.]{50,200}\.", RegexOptions.Multiline),
            HasCoreDefinition = Regex.IsMatch(front30Content, language == "zh" 
                ? @"是指|定义为|即|就是" 
                : @"\bis\b|\bare\b|defined as|refers to", RegexOptions.IgnoreCase),
            HasKeyStatistics = Regex.IsMatch(front30Content, @"\d+%|\d+\s*(万|亿|million|billion)|统计|数据|研究表明"),
            HasExpertQuote = Regex.IsMatch(front30Content, language == "zh" 
                ? "[\u201C\u300C\u300E].*?[\u201D\u300D\u300F].*?(说|表示|认为|指出)" 
                : @"""[^""]+"".*?(said|says|according to)", RegexOptions.IgnoreCase),
            ClaimCount = front30Claims.Count,
            EntityCount = front30Entities.Sum(e => e.Count)
        };

        // 计算评分
        var score = 0.0;
        if (front30ClaimDensity >= back70ClaimDensity) score += 3;
        if (elements.HasAnswerCapsule) score += 2;
        if (elements.HasCoreDefinition) score += 1.5;
        if (elements.HasKeyStatistics) score += 1.5;
        if (elements.HasExpertQuote) score += 1;
        score += Math.Min(1, front30ClaimDensity / 4);

        // 生成建议
        var suggestions = new List<string>();
        if (front30ClaimDensity < back70ClaimDensity)
        {
            suggestions.Add($"前 30% 事实密度（{front30ClaimDensity:F1}）低于后 70%（{back70ClaimDensity:F1}），建议将关键事实前置");
        }
        if (!elements.HasAnswerCapsule)
        {
            suggestions.Add("建议在开头添加答案胶囊（40-100词的核心回答）");
        }
        if (!elements.HasCoreDefinition)
        {
            suggestions.Add("建议在前 30% 添加核心定义");
        }
        if (!elements.HasKeyStatistics)
        {
            suggestions.Add("建议在前 30% 添加关键数据或统计");
        }

        return new Front30PercentMetric
        {
            Score = Math.Round(Math.Min(10, score), 1),
            Front30WordCount = front30WordCount,
            TotalWordCount = totalWordCount,
            Front30ClaimDensity = Math.Round(front30ClaimDensity, 1),
            Back70ClaimDensity = Math.Round(back70ClaimDensity, 1),
            Elements = elements,
            Suggestions = suggestions
        };
    }

    #endregion

    #region 3.28 段落长度优化

    /// <summary>
    /// 分析段落长度 (3.28)
    /// 原理：120-180 词段落 +70% 引用率
    /// </summary>
    public ParagraphLengthMetric AnalyzeParagraphLength(string content, string language = "zh")
    {
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p) && !p.TrimStart().StartsWith("#"))
            .ToList();

        var details = new List<ParagraphDetail>();
        int optimal = 0, shortCount = 0, longCount = 0;
        double totalLength = 0;

        for (int i = 0; i < paragraphs.Count; i++)
        {
            var para = paragraphs[i].Trim();
            int wordCount = language == "zh" 
                ? para.Length 
                : para.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            
            totalLength += wordCount;
            string status;
            
            if (wordCount >= 120 && wordCount <= 180)
            {
                optimal++;
                status = "optimal";
            }
            else if (wordCount < 120)
            {
                shortCount++;
                status = "short";
                details.Add(new ParagraphDetail
                {
                    Index = i + 1,
                    WordCount = wordCount,
                    Status = status,
                    Preview = para.Length > 50 ? para[..50] + "..." : para
                });
            }
            else
            {
                longCount++;
                status = "long";
                details.Add(new ParagraphDetail
                {
                    Index = i + 1,
                    WordCount = wordCount,
                    Status = status,
                    Preview = para.Length > 50 ? para[..50] + "..." : para
                });
            }
        }

        int total = paragraphs.Count;
        double optimalRatio = total > 0 ? (double)optimal / total : 0;
        double score = optimalRatio * 10;

        var suggestions = new List<string>();
        if (shortCount > 0)
            suggestions.Add($"有 {shortCount} 个段落过短（<120词），建议扩展内容");
        if (longCount > 0)
            suggestions.Add($"有 {longCount} 个段落过长（>180词），建议拆分");
        if (optimalRatio < 0.5)
            suggestions.Add("建议将更多段落调整到 120-180 词区间以提升引用率");

        return new ParagraphLengthMetric
        {
            Score = Math.Round(score, 1),
            TotalParagraphs = total,
            OptimalParagraphs = optimal,
            ShortParagraphs = shortCount,
            LongParagraphs = longCount,
            AverageLength = total > 0 ? Math.Round(totalLength / total, 1) : 0,
            ParagraphsToOptimize = details.Take(5).ToList(),
            Suggestions = suggestions
        };
    }

    #endregion

    #region 3.29 标题策略优化

    /// <summary>
    /// 分析标题策略 (3.29)
    /// 原理：问号标题 -0.9 引用，直陈式更佳
    /// </summary>
    public TitleStrategyMetric AnalyzeTitleStrategy(string content, string? title = null)
    {
        // 尝试从内容中提取标题
        var detectedTitle = title;
        if (string.IsNullOrEmpty(detectedTitle))
        {
            var h1Match = Regex.Match(content, @"^#\s+(.+)$", RegexOptions.Multiline);
            if (h1Match.Success)
                detectedTitle = h1Match.Groups[1].Value.Trim();
            else
            {
                var firstLine = content.Split('\n').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(firstLine) && firstLine.Length < 100)
                    detectedTitle = firstLine;
            }
        }

        if (string.IsNullOrEmpty(detectedTitle))
        {
            return new TitleStrategyMetric
            {
                Score = 5,
                DetectedTitle = "",
                TitleType = "unknown",
                Suggestions = new() { "未检测到标题，建议添加明确的 H1 标题" }
            };
        }

        // 分析标题类型
        bool isQuestion = detectedTitle.Contains('?') || detectedTitle.Contains('？') ||
                         Regex.IsMatch(detectedTitle, @"^(什么|如何|为什么|怎么|哪些|是否|能否|可以|应该)", RegexOptions.IgnoreCase) ||
                         Regex.IsMatch(detectedTitle, @"^(what|how|why|when|where|which|can|should|is|are|do|does)", RegexOptions.IgnoreCase);
        
        bool hasNumber = Regex.IsMatch(detectedTitle, @"\d+");
        bool hasYear = Regex.IsMatch(detectedTitle, @"20\d{2}");
        bool isHowTo = Regex.IsMatch(detectedTitle, @"(如何|怎么|how to|guide|教程|指南)", RegexOptions.IgnoreCase);
        bool isListicle = Regex.IsMatch(detectedTitle, @"^\d+\s*(个|种|条|大|款|ways|tips|steps|reasons)", RegexOptions.IgnoreCase);

        string titleType;
        double citationImpact;

        if (isQuestion)
        {
            titleType = "question";
            citationImpact = -0.9;
        }
        else if (isListicle)
        {
            titleType = "listicle";
            citationImpact = 0.3;
        }
        else if (isHowTo)
        {
            titleType = "howto";
            citationImpact = 0.5;
        }
        else
        {
            titleType = "statement";
            citationImpact = 1.0;
        }

        // 计算评分
        double score = 5;
        if (titleType == "statement") score += 3;
        if (titleType == "howto") score += 2;
        if (titleType == "listicle") score += 1;
        if (titleType == "question") score -= 2;
        if (hasYear) score += 1;
        if (detectedTitle.Length >= 30 && detectedTitle.Length <= 60) score += 1;

        var suggestions = new List<string>();
        var variants = new List<string>();

        if (isQuestion)
        {
            suggestions.Add("问号标题会降低 0.9 的引用率，建议改为直陈式");
            // 生成变体建议
            var statementVersion = Regex.Replace(detectedTitle, @"[?？]$", "");
            statementVersion = Regex.Replace(statementVersion, @"^(什么是|如何|怎么)", "");
            if (!string.IsNullOrEmpty(statementVersion))
                variants.Add($"直陈式: {statementVersion}");
        }

        if (!hasYear)
        {
            suggestions.Add("添加年份可提升时效性感知（如 2026）");
            variants.Add($"{detectedTitle} (2026)");
        }

        if (detectedTitle.Length < 30)
            suggestions.Add("标题较短，建议扩展到 30-60 字符");
        if (detectedTitle.Length > 70)
            suggestions.Add("标题较长，建议精简到 60 字符以内");

        return new TitleStrategyMetric
        {
            Score = Math.Round(Math.Max(0, Math.Min(10, score)), 1),
            DetectedTitle = detectedTitle,
            TitleType = titleType,
            IsQuestionTitle = isQuestion,
            TitleLength = detectedTitle.Length,
            HasNumber = hasNumber,
            HasYear = hasYear,
            CitationImpact = citationImpact,
            Suggestions = suggestions,
            RecommendedVariants = variants
        };
    }

    #endregion

    #region 3.30 实体密度增强

    /// <summary>
    /// 分析增强实体密度 (3.30)
    /// 原理：实体密度 15+ = 4.8x 引用率
    /// </summary>
    public async Task<EnhancedEntityDensityMetric> AnalyzeEnhancedEntityDensityAsync(string content, string language = "zh")
    {
        var entities = await _entityExtractor.ExtractEntitiesAsync(content, language);
        int wordCount = language == "zh" ? content.Length : content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        double densityPer100 = wordCount > 0 ? (entities.Count * 100.0) / wordCount : 0;

        // 统计实体类型分布
        var typeDistribution = entities
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        // 高价值实体：专有名词、数据、专家名
        var highValueTypes = new[] { "person", "organization", "product", "statistic", "expert" };
        int highValueCount = entities.Count(e => highValueTypes.Contains(e.Type.ToLower()));

        // 检查缺失的实体类型
        var expectedTypes = new[] { "person", "organization", "location", "date", "statistic" };
        var missingTypes = expectedTypes.Where(t => !typeDistribution.ContainsKey(t)).ToList();

        // 计算评分
        double score;
        if (densityPer100 >= 15) score = 10;
        else if (densityPer100 >= 10) score = 7 + (densityPer100 - 10) * 0.6;
        else if (densityPer100 >= 5) score = 4 + (densityPer100 - 5) * 0.6;
        else score = densityPer100 * 0.8;

        var suggestions = new List<string>();
        if (densityPer100 < 15)
        {
            suggestions.Add($"当前实体密度 {densityPer100:F1}/100词，建议提升到 15+ 以获得 4.8x 引用率");
        }
        if (highValueCount < 5)
        {
            suggestions.Add("建议增加高价值实体（专家名、机构名、具体数据）");
        }
        if (missingTypes.Count > 0)
        {
            suggestions.Add($"缺少实体类型: {string.Join(", ", missingTypes)}");
        }

        return new EnhancedEntityDensityMetric
        {
            Score = Math.Round(score, 1),
            TotalEntities = entities.Count,
            DensityPer100Words = Math.Round(densityPer100, 1),
            HighValueEntities = highValueCount,
            EntityTypeDistribution = typeDistribution,
            MissingEntityTypes = missingTypes,
            Suggestions = suggestions
        };
    }

    #endregion
}
