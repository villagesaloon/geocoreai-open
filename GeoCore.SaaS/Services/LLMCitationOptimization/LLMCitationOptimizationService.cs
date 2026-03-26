using Microsoft.Extensions.Logging;

namespace GeoCore.SaaS.Services.LLMCitationOptimization;

public class LLMCitationOptimizationService
{
    private readonly Dictionary<string, LLMPlatformPreferences> _platformPreferences;
    private readonly ILogger<LLMCitationOptimizationService> _logger;

    public LLMCitationOptimizationService(ILogger<LLMCitationOptimizationService> logger)
    {
        _logger = logger;
        _platformPreferences = InitializePlatformPreferences();
    }

    #region 7.1 LLM 平台偏好来源数据库

    public List<LLMPlatformPreferences> GetAllPlatformPreferences()
    {
        return _platformPreferences.Values.ToList();
    }

    public LLMPlatformPreferences? GetPlatformPreferences(string platform)
    {
        return _platformPreferences.TryGetValue(platform.ToLower(), out var prefs) ? prefs : null;
    }

    private Dictionary<string, LLMPlatformPreferences> InitializePlatformPreferences()
    {
        return new Dictionary<string, LLMPlatformPreferences>
        {
            ["chatgpt"] = new LLMPlatformPreferences
            {
                Platform = "chatgpt",
                DisplayName = "ChatGPT (OpenAI)",
                LastUpdated = "2026-03",
                Characteristics = new PlatformCharacteristics
                {
                    ContentPreference = "authoritative",
                    TypicalCitationCount = "3-5 sources",
                    PreferredFormat = "structured, factual",
                    KeyFactors = new List<string> { "Domain authority", "Content freshness", "Factual accuracy", "Clear structure" },
                    EffectiveTimeframe = "2-4 weeks"
                },
                TopSources = new List<CitationSource>
                {
                    new() { Rank = 1, Domain = "reddit.com", Category = "forum", CitationShare = 15.2, Actionability = "high", ActionabilityReason = "可直接参与讨论", OptimizationTips = new() { "在相关 subreddit 发布有价值内容", "回答问题时自然提及品牌", "建立专业声誉" } },
                    new() { Rank = 2, Domain = "wikipedia.org", Category = "encyclopedia", CitationShare = 12.8, Actionability = "medium", ActionabilityReason = "需要符合维基百科编辑规则", OptimizationTips = new() { "确保品牌有维基百科页面", "添加可靠来源引用", "保持中立客观" } },
                    new() { Rank = 3, Domain = "youtube.com", Category = "video", CitationShare = 8.5, Actionability = "high", ActionabilityReason = "可创建视频内容", OptimizationTips = new() { "创建教程和解说视频", "优化视频标题和描述", "添加字幕提高可索引性" } },
                    new() { Rank = 4, Domain = "github.com", Category = "code", CitationShare = 6.2, Actionability = "high", ActionabilityReason = "技术品牌必备", OptimizationTips = new() { "开源相关工具", "维护高质量 README", "积极参与社区" } },
                    new() { Rank = 5, Domain = "medium.com", Category = "blog", CitationShare = 5.8, Actionability = "high", ActionabilityReason = "可直接发布文章", OptimizationTips = new() { "发布深度技术文章", "使用相关标签", "建立 publication" } },
                    new() { Rank = 6, Domain = "stackoverflow.com", Category = "qa", CitationShare = 5.1, Actionability = "high", ActionabilityReason = "技术问答平台", OptimizationTips = new() { "回答相关技术问题", "提供详细解决方案", "引用官方文档" } },
                    new() { Rank = 7, Domain = "linkedin.com", Category = "professional", CitationShare = 4.3, Actionability = "high", ActionabilityReason = "B2B 首选平台", OptimizationTips = new() { "发布行业洞察", "建立思想领导力", "参与专业讨论" } },
                    new() { Rank = 8, Domain = "nytimes.com", Category = "news", CitationShare = 3.9, Actionability = "low", ActionabilityReason = "需要新闻价值", OptimizationTips = new() { "发布新闻稿", "建立媒体关系", "提供独家数据" } }
                }
            },
            ["perplexity"] = new LLMPlatformPreferences
            {
                Platform = "perplexity",
                DisplayName = "Perplexity AI",
                LastUpdated = "2026-03",
                Characteristics = new PlatformCharacteristics
                {
                    ContentPreference = "fresh",
                    TypicalCitationCount = "5-10 sources",
                    PreferredFormat = "news, recent updates",
                    KeyFactors = new List<string> { "Content freshness", "Real-time relevance", "Source diversity", "Factual accuracy" },
                    EffectiveTimeframe = "1-2 weeks"
                },
                TopSources = new List<CitationSource>
                {
                    new() { Rank = 1, Domain = "reddit.com", Category = "forum", CitationShare = 18.5, Actionability = "high", ActionabilityReason = "Perplexity 最偏好的来源", OptimizationTips = new() { "保持活跃讨论", "提供实时更新", "回答热门问题" } },
                    new() { Rank = 2, Domain = "news sites", Category = "news", CitationShare = 15.2, Actionability = "medium", ActionabilityReason = "需要新闻价值", OptimizationTips = new() { "发布新闻稿", "提供行业数据", "建立媒体关系" } },
                    new() { Rank = 3, Domain = "youtube.com", Category = "video", CitationShare = 10.1, Actionability = "high", ActionabilityReason = "视频内容被频繁引用", OptimizationTips = new() { "发布时效性视频", "优化 SEO", "添加详细描述" } },
                    new() { Rank = 4, Domain = "twitter.com", Category = "social", CitationShare = 8.7, Actionability = "high", ActionabilityReason = "实时信息来源", OptimizationTips = new() { "发布行业动态", "参与热门话题", "建立专业形象" } },
                    new() { Rank = 5, Domain = "linkedin.com", Category = "professional", CitationShare = 6.3, Actionability = "high", ActionabilityReason = "专业内容平台", OptimizationTips = new() { "发布行业分析", "分享公司动态", "建立思想领导力" } },
                    new() { Rank = 6, Domain = "medium.com", Category = "blog", CitationShare = 5.8, Actionability = "high", ActionabilityReason = "深度内容平台", OptimizationTips = new() { "发布深度分析", "保持更新频率", "使用热门标签" } }
                }
            },
            ["gemini"] = new LLMPlatformPreferences
            {
                Platform = "gemini",
                DisplayName = "Google Gemini",
                LastUpdated = "2026-03",
                Characteristics = new PlatformCharacteristics
                {
                    ContentPreference = "authoritative",
                    TypicalCitationCount = "3-6 sources",
                    PreferredFormat = "structured, comprehensive",
                    KeyFactors = new List<string> { "Google Search ranking", "E-E-A-T signals", "Schema markup", "Mobile-friendly" },
                    EffectiveTimeframe = "2-4 weeks"
                },
                TopSources = new List<CitationSource>
                {
                    new() { Rank = 1, Domain = "Official websites", Category = "official", CitationShare = 22.1, Actionability = "high", ActionabilityReason = "自有网站优化", OptimizationTips = new() { "优化 Schema 标记", "提高 E-E-A-T 信号", "确保移动友好" } },
                    new() { Rank = 2, Domain = "wikipedia.org", Category = "encyclopedia", CitationShare = 14.5, Actionability = "medium", ActionabilityReason = "权威性来源", OptimizationTips = new() { "维护维基百科页面", "添加可靠引用", "保持信息准确" } },
                    new() { Rank = 3, Domain = "youtube.com", Category = "video", CitationShare = 11.2, Actionability = "high", ActionabilityReason = "Google 生态系统", OptimizationTips = new() { "优化 YouTube SEO", "添加详细字幕", "使用章节标记" } },
                    new() { Rank = 4, Domain = "reddit.com", Category = "forum", CitationShare = 8.3, Actionability = "high", ActionabilityReason = "社区讨论", OptimizationTips = new() { "参与相关讨论", "提供有价值回答", "建立声誉" } },
                    new() { Rank = 5, Domain = "linkedin.com", Category = "professional", CitationShare = 5.7, Actionability = "high", ActionabilityReason = "专业内容", OptimizationTips = new() { "发布专业文章", "建立公司页面", "分享行业洞察" } }
                }
            },
            ["claude"] = new LLMPlatformPreferences
            {
                Platform = "claude",
                DisplayName = "Claude (Anthropic)",
                LastUpdated = "2026-03",
                Characteristics = new PlatformCharacteristics
                {
                    ContentPreference = "comprehensive",
                    TypicalCitationCount = "2-4 sources",
                    PreferredFormat = "detailed, nuanced",
                    KeyFactors = new List<string> { "Content depth", "Nuanced analysis", "Authoritative sources", "Clear reasoning" },
                    EffectiveTimeframe = "3-6 weeks"
                },
                TopSources = new List<CitationSource>
                {
                    new() { Rank = 1, Domain = "Academic sources", Category = "academic", CitationShare = 18.2, Actionability = "medium", ActionabilityReason = "需要学术背景", OptimizationTips = new() { "发布研究报告", "引用学术论文", "提供数据支持" } },
                    new() { Rank = 2, Domain = "wikipedia.org", Category = "encyclopedia", CitationShare = 15.1, Actionability = "medium", ActionabilityReason = "权威参考", OptimizationTips = new() { "维护维基百科页面", "确保信息准确", "添加可靠来源" } },
                    new() { Rank = 3, Domain = "Official documentation", Category = "official", CitationShare = 12.8, Actionability = "high", ActionabilityReason = "官方文档", OptimizationTips = new() { "维护详细文档", "提供 API 参考", "保持更新" } },
                    new() { Rank = 4, Domain = "github.com", Category = "code", CitationShare = 9.5, Actionability = "high", ActionabilityReason = "技术参考", OptimizationTips = new() { "开源项目", "详细 README", "示例代码" } },
                    new() { Rank = 5, Domain = "medium.com", Category = "blog", CitationShare = 6.2, Actionability = "high", ActionabilityReason = "深度内容", OptimizationTips = new() { "发布深度分析", "技术教程", "案例研究" } }
                }
            },
            ["grok"] = new LLMPlatformPreferences
            {
                Platform = "grok",
                DisplayName = "Grok (xAI)",
                LastUpdated = "2026-03",
                Characteristics = new PlatformCharacteristics
                {
                    ContentPreference = "real-time",
                    TypicalCitationCount = "3-5 sources",
                    PreferredFormat = "conversational, current",
                    KeyFactors = new List<string> { "X/Twitter presence", "Real-time relevance", "Engagement metrics", "Trending topics" },
                    EffectiveTimeframe = "1 week"
                },
                TopSources = new List<CitationSource>
                {
                    new() { Rank = 1, Domain = "twitter.com/x.com", Category = "social", CitationShare = 35.2, Actionability = "high", ActionabilityReason = "Grok 的核心数据源", OptimizationTips = new() { "保持活跃发帖", "参与热门话题", "建立影响力", "使用相关标签" } },
                    new() { Rank = 2, Domain = "news sites", Category = "news", CitationShare = 18.5, Actionability = "medium", ActionabilityReason = "实时新闻", OptimizationTips = new() { "发布新闻稿", "提供独家信息", "建立媒体关系" } },
                    new() { Rank = 3, Domain = "reddit.com", Category = "forum", CitationShare = 12.1, Actionability = "high", ActionabilityReason = "社区讨论", OptimizationTips = new() { "参与热门讨论", "提供有价值内容", "建立声誉" } },
                    new() { Rank = 4, Domain = "youtube.com", Category = "video", CitationShare = 8.7, Actionability = "high", ActionabilityReason = "视频内容", OptimizationTips = new() { "发布时效性视频", "优化标题描述", "添加字幕" } }
                }
            }
        };
    }

    #endregion

    #region 7.2 平台特定优化建议生成

    public PlatformOptimizationResult GenerateOptimizationStrategy(PlatformOptimizationRequest request)
    {
        var result = new PlatformOptimizationResult
        {
            BrandName = request.BrandName,
            Strategies = new List<PlatformStrategy>(),
            QuickWins = new List<QuickWin>(),
            CrossPlatformAnalysis = AnalyzeCrossPlatform(request.TargetPlatforms),
            Roadmap = Generate30DayRoadmap(request)
        };

        foreach (var platform in request.TargetPlatforms)
        {
            var prefs = GetPlatformPreferences(platform);
            if (prefs == null) continue;

            var strategy = new PlatformStrategy
            {
                Platform = platform,
                DisplayName = prefs.DisplayName,
                PriorityScore = CalculatePriorityScore(platform, request.Industry),
                ActionItems = GenerateActionItems(platform, request),
                ContentRecommendations = GenerateContentRecommendations(platform, request),
                ExpectedTimeframe = prefs.Characteristics.EffectiveTimeframe,
                ExpectedImpact = CalculateExpectedImpact(platform, request.Industry)
            };
            strategy.PriorityLevel = strategy.PriorityScore >= 80 ? "P0" : strategy.PriorityScore >= 60 ? "P1" : "P2";
            result.Strategies.Add(strategy);
        }

        result.QuickWins = GenerateQuickWins(request);
        result.Strategies = result.Strategies.OrderByDescending(s => s.PriorityScore).ToList();

        return result;
    }

    private int CalculatePriorityScore(string platform, string industry)
    {
        var baseScores = new Dictionary<string, int>
        {
            ["chatgpt"] = 85,
            ["perplexity"] = 80,
            ["gemini"] = 75,
            ["claude"] = 70,
            ["grok"] = 65
        };

        var score = baseScores.GetValueOrDefault(platform.ToLower(), 50);

        // 行业调整
        if (industry.ToLower().Contains("tech") || industry.ToLower().Contains("软件"))
        {
            if (platform.ToLower() == "chatgpt" || platform.ToLower() == "claude") score += 10;
        }
        else if (industry.ToLower().Contains("news") || industry.ToLower().Contains("媒体"))
        {
            if (platform.ToLower() == "perplexity" || platform.ToLower() == "grok") score += 10;
        }

        return Math.Min(100, score);
    }

    private string CalculateExpectedImpact(string platform, string industry)
    {
        var prefs = GetPlatformPreferences(platform);
        if (prefs == null) return "中等";

        return prefs.Characteristics.ContentPreference switch
        {
            "fresh" => "快速见效，1-2 周内可见引用增长",
            "authoritative" => "中期见效，2-4 周建立权威性",
            "comprehensive" => "长期见效，需要 4-6 周积累",
            "real-time" => "即时见效，但需持续维护",
            _ => "中等影响，2-3 周见效"
        };
    }

    private List<ActionItem> GenerateActionItems(string platform, PlatformOptimizationRequest request)
    {
        var items = new List<ActionItem>();
        var prefs = GetPlatformPreferences(platform);
        if (prefs == null) return items;

        var order = 1;
        foreach (var source in prefs.TopSources.Where(s => s.Actionability == "high").Take(3))
        {
            foreach (var tip in source.OptimizationTips.Take(2))
            {
                items.Add(new ActionItem
                {
                    Order = order++,
                    Action = $"[{source.Domain}] {tip}",
                    Description = $"针对 {prefs.DisplayName} 优化 {source.Domain} 存在",
                    Effort = source.CitationShare > 10 ? "medium" : "low",
                    Impact = source.CitationShare > 10 ? "high" : "medium",
                    Timeline = prefs.Characteristics.EffectiveTimeframe
                });
            }
        }

        return items;
    }

    private List<ContentRecommendation> GenerateContentRecommendations(string platform, PlatformOptimizationRequest request)
    {
        var recommendations = new List<ContentRecommendation>();
        var prefs = GetPlatformPreferences(platform);
        if (prefs == null) return recommendations;

        foreach (var source in prefs.TopSources.Where(s => s.Actionability == "high").Take(3))
        {
            recommendations.Add(new ContentRecommendation
            {
                TargetPlatform = source.Domain,
                ContentType = GetContentTypeForSource(source.Category),
                Description = $"为 {prefs.DisplayName} 优化的 {source.Domain} 内容",
                Template = GetTemplateForSource(source.Category, request.BrandName),
                BestPractices = source.OptimizationTips
            });
        }

        return recommendations;
    }

    private string GetContentTypeForSource(string category)
    {
        return category switch
        {
            "forum" => "讨论帖/问答回复",
            "blog" => "深度文章",
            "video" => "教程视频",
            "social" => "社交帖子",
            "professional" => "行业洞察",
            "code" => "开源项目/技术文档",
            _ => "通用内容"
        };
    }

    private string GetTemplateForSource(string category, string brandName)
    {
        return category switch
        {
            "forum" => $"标题: [问题类型] 关于 {brandName} 的 [具体问题]\n\n正文: 提供有价值的回答，自然提及 {brandName}...",
            "blog" => $"# [吸引人的标题]\n\n## 引言\n简要介绍问题背景...\n\n## {brandName} 的解决方案\n详细说明...\n\n## 结论\n总结要点...",
            "video" => $"视频标题: [How to/教程] 使用 {brandName} 解决 [问题]\n描述: 详细说明视频内容，包含关键词...",
            "social" => $"🔥 [热门话题] + {brandName} 的独特见解\n\n关键要点 1\n关键要点 2\n\n#相关标签",
            _ => $"关于 {brandName} 的内容模板..."
        };
    }

    private List<QuickWin> GenerateQuickWins(PlatformOptimizationRequest request)
    {
        var quickWins = new List<QuickWin>
        {
            new()
            {
                Title = "Reddit AMA 或专家回答",
                Description = $"在相关 subreddit 回答关于 {request.Industry} 的问题，自然提及 {request.BrandName}",
                Platform = "Reddit",
                Effort = "low",
                ExpectedResult = "1-2 周内被 ChatGPT/Perplexity 引用",
                DaysToImplement = 3
            },
            new()
            {
                Title = "Medium 深度文章",
                Description = $"发布一篇关于 {request.Industry} 最佳实践的深度文章",
                Platform = "Medium",
                Effort = "medium",
                ExpectedResult = "2-3 周内建立内容权威性",
                DaysToImplement = 7
            },
            new()
            {
                Title = "LinkedIn 行业洞察",
                Description = "发布行业数据分析或趋势预测",
                Platform = "LinkedIn",
                Effort = "low",
                ExpectedResult = "B2B 场景下被引用概率提升",
                DaysToImplement = 2
            },
            new()
            {
                Title = "YouTube 教程视频",
                Description = $"创建 {request.BrandName} 使用教程或行业解说视频",
                Platform = "YouTube",
                Effort = "high",
                ExpectedResult = "长期引用来源，多平台受益",
                DaysToImplement = 14
            }
        };

        if (request.TargetPlatforms.Contains("grok"))
        {
            quickWins.Insert(0, new QuickWin
            {
                Title = "X/Twitter 活跃发帖",
                Description = "每日发布行业相关内容，参与热门话题讨论",
                Platform = "X/Twitter",
                Effort = "low",
                ExpectedResult = "1 周内被 Grok 引用",
                DaysToImplement = 1
            });
        }

        return quickWins;
    }

    #endregion

    #region 7.3 引用源可操作性评估

    public List<CitationSource> EvaluateActionability(string platform)
    {
        var prefs = GetPlatformPreferences(platform);
        if (prefs == null) return new List<CitationSource>();

        return prefs.TopSources
            .OrderByDescending(s => s.Actionability == "high" ? 3 : s.Actionability == "medium" ? 2 : 1)
            .ThenByDescending(s => s.CitationShare)
            .ToList();
    }

    #endregion

    #region 7.4 跨平台分析

    public CrossPlatformAnalysis AnalyzeCrossPlatform(List<string> platforms)
    {
        var analysis = new CrossPlatformAnalysis
        {
            OverlapPercentage = 11.0, // 研究显示仅 11% 重叠
            Overlaps = new List<PlatformOverlap>(),
            UniqueOpportunities = new List<UniqueOpportunity>(),
            Summary = "研究显示，不同 LLM 平台的引用来源仅有约 11% 重叠，这意味着需要针对每个平台制定独立策略。"
        };

        // 计算平台间重叠
        var platformPairs = new List<(string, string)>
        {
            ("chatgpt", "perplexity"),
            ("chatgpt", "gemini"),
            ("perplexity", "gemini"),
            ("chatgpt", "claude"),
            ("perplexity", "grok")
        };

        foreach (var (p1, p2) in platformPairs)
        {
            if (!platforms.Contains(p1) || !platforms.Contains(p2)) continue;

            var prefs1 = GetPlatformPreferences(p1);
            var prefs2 = GetPlatformPreferences(p2);
            if (prefs1 == null || prefs2 == null) continue;

            var domains1 = prefs1.TopSources.Select(s => s.Domain).ToHashSet();
            var domains2 = prefs2.TopSources.Select(s => s.Domain).ToHashSet();
            var shared = domains1.Intersect(domains2).ToList();

            analysis.Overlaps.Add(new PlatformOverlap
            {
                Platform1 = p1,
                Platform2 = p2,
                OverlapPercentage = (double)shared.Count / Math.Max(domains1.Count, domains2.Count) * 100,
                SharedSources = shared
            });
        }

        // 识别独特机会
        foreach (var platform in platforms)
        {
            var prefs = GetPlatformPreferences(platform);
            if (prefs == null) continue;

            var uniqueSources = prefs.TopSources
                .Where(s => s.Actionability == "high" && s.CitationShare > 10)
                .Take(2);

            foreach (var source in uniqueSources)
            {
                analysis.UniqueOpportunities.Add(new UniqueOpportunity
                {
                    Platform = platform,
                    Source = source.Domain,
                    Reason = $"{prefs.DisplayName} 对 {source.Domain} 有 {source.CitationShare}% 的引用偏好",
                    Actionability = source.Actionability
                });
            }
        }

        return analysis;
    }

    #endregion

    #region 7.5 内容模板

    public ContentTemplateResult GetContentTemplate(ContentTemplateRequest request)
    {
        var templates = new Dictionary<string, ContentTemplateResult>
        {
            ["reddit"] = new ContentTemplateResult
            {
                Platform = "Reddit",
                ContentType = "讨论帖/问答回复",
                Template = $@"**标题**: [问题类型] {request.Topic} - 寻求建议/分享经验

**正文**:

大家好，我想分享一下关于 {request.Topic} 的经验。

**背景**:
- 简要说明你的情况
- 为什么这个话题重要

**我的发现/解决方案**:
1. 第一个要点
2. 第二个要点
3. 第三个要点

**结果**:
- 具体数据或成果
- 可以自然提及 {request.BrandName}

希望对大家有帮助，欢迎讨论！",
                Guidelines = new List<string> { "保持真实和有价值", "不要过度推销", "积极回复评论", "选择合适的 subreddit" },
                DoList = new List<string> { "提供具体数据", "分享真实经验", "回答后续问题", "使用适当的 flair" },
                DontList = new List<string> { "直接广告", "垃圾链接", "忽视社区规则", "过度自我推销" },
                OptimalLength = "300-500 词",
                BestPostingTime = "美国时间上午 9-11 点"
            },
            ["medium"] = new ContentTemplateResult
            {
                Platform = "Medium",
                ContentType = "深度文章",
                Template = $@"# {request.Topic}: 完整指南

![封面图片描述](image-url)

## 引言

简要介绍 {request.Topic} 的重要性和本文将涵盖的内容。

## 为什么 {request.Topic} 很重要

- 背景信息
- 行业趋势
- 常见挑战

## 核心策略

### 策略 1: [标题]
详细说明...

### 策略 2: [标题]
详细说明...

### 策略 3: [标题]
详细说明...

## {request.BrandName} 的方法

自然地介绍你的解决方案...

## 实施步骤

1. 第一步
2. 第二步
3. 第三步

## 结论

总结要点，提供行动号召。

---

*关于作者: [简介]*",
                Guidelines = new List<string> { "使用清晰的标题结构", "添加相关图片", "包含数据支持", "保持专业但易读" },
                DoList = new List<string> { "深度分析", "原创见解", "数据支持", "清晰结构" },
                DontList = new List<string> { "浅层内容", "纯广告", "抄袭", "无价值填充" },
                OptimalLength = "1500-2500 词",
                BestPostingTime = "工作日上午"
            },
            ["linkedin"] = new ContentTemplateResult
            {
                Platform = "LinkedIn",
                ContentType = "行业洞察",
                Template = $@"🔍 {request.Topic} 的 3 个关键洞察

最近我研究了 {request.Topic}，发现了一些有趣的趋势：

1️⃣ **洞察一**
简要说明...

2️⃣ **洞察二**
简要说明...

3️⃣ **洞察三**
简要说明...

💡 我的建议：
- 行动建议 1
- 行动建议 2

你对 {request.Topic} 有什么看法？欢迎在评论区分享！

#行业标签 #相关标签 #{request.BrandName}",
                Guidelines = new List<string> { "使用 emoji 增加可读性", "保持专业", "鼓励互动", "添加相关标签" },
                DoList = new List<string> { "分享洞察", "提供价值", "鼓励讨论", "保持简洁" },
                DontList = new List<string> { "纯销售", "过长内容", "无互动", "垃圾标签" },
                OptimalLength = "150-300 词",
                BestPostingTime = "工作日上午 8-10 点"
            },
            ["youtube"] = new ContentTemplateResult
            {
                Platform = "YouTube",
                ContentType = "教程视频",
                Template = $@"**视频标题**: {request.Topic} 完整教程 | 2026 最新指南

**视频描述**:
在这个视频中，我将向你展示 {request.Topic} 的完整流程。

⏱️ 时间戳:
0:00 - 介绍
1:30 - 第一步
3:45 - 第二步
6:00 - 第三步
8:30 - 总结

🔗 相关资源:
- 链接 1
- 链接 2

📌 关于 {request.BrandName}:
简要介绍...

#关键词 #相关标签",
                Guidelines = new List<string> { "添加时间戳", "优化 SEO", "添加字幕", "使用吸引人的缩略图" },
                DoList = new List<string> { "清晰讲解", "实际演示", "添加字幕", "互动号召" },
                DontList = new List<string> { "过长介绍", "无价值内容", "忽视 SEO", "无字幕" },
                OptimalLength = "8-15 分钟",
                BestPostingTime = "周末下午"
            }
        };

        return templates.GetValueOrDefault(request.Platform.ToLower(), new ContentTemplateResult
        {
            Platform = request.Platform,
            ContentType = "通用内容",
            Template = $"关于 {request.Topic} 的内容...",
            Guidelines = new List<string> { "提供价值", "保持专业", "自然提及品牌" }
        });
    }

    #endregion

    #region 7.6 30 天路线图生成

    public OptimizationRoadmap Generate30DayRoadmap(PlatformOptimizationRequest request)
    {
        return new OptimizationRoadmap
        {
            TotalDuration = "30 天",
            Phases = new List<RoadmapPhase>
            {
                new()
                {
                    PhaseNumber = 1,
                    Name = "基础设置",
                    Duration = "第 1-7 天",
                    StartDay = 1,
                    EndDay = 7,
                    Tasks = new List<PhaseTask>
                    {
                        new() { Task = "审计现有在线存在", Description = "检查所有目标平台的现有账号和内容", Owner = "marketing" },
                        new() { Task = "设置/优化平台账号", Description = "确保所有平台账号完整且专业", Owner = "marketing" },
                        new() { Task = "网站技术优化", Description = "确保 robots.txt、llms.txt、Schema 正确配置", Owner = "technical" },
                        new() { Task = "内容库存盘点", Description = "整理可复用的现有内容", Owner = "content" }
                    },
                    ExpectedOutcome = "所有平台账号就绪，技术基础完成"
                },
                new()
                {
                    PhaseNumber = 2,
                    Name = "快速见效",
                    Duration = "第 8-14 天",
                    StartDay = 8,
                    EndDay = 14,
                    Tasks = new List<PhaseTask>
                    {
                        new() { Task = "Reddit 参与", Description = "在相关 subreddit 回答 5-10 个问题", Owner = "content" },
                        new() { Task = "LinkedIn 发帖", Description = "发布 3-5 篇行业洞察", Owner = "marketing" },
                        new() { Task = "Medium 文章", Description = "发布 1-2 篇深度文章", Owner = "content" },
                        new() { Task = "X/Twitter 活跃", Description = "每日发帖，参与行业讨论", Owner = "marketing" }
                    },
                    ExpectedOutcome = "初步内容发布，开始建立存在感"
                },
                new()
                {
                    PhaseNumber = 3,
                    Name = "内容深化",
                    Duration = "第 15-21 天",
                    StartDay = 15,
                    EndDay = 21,
                    Tasks = new List<PhaseTask>
                    {
                        new() { Task = "YouTube 视频", Description = "发布 1-2 个教程视频", Owner = "content" },
                        new() { Task = "GitHub 项目", Description = "开源相关工具或示例代码", Owner = "technical" },
                        new() { Task = "持续 Reddit 参与", Description = "继续回答问题，建立声誉", Owner = "content" },
                        new() { Task = "跨平台内容复用", Description = "将内容适配到不同平台", Owner = "content" }
                    },
                    ExpectedOutcome = "多平台内容矩阵形成"
                },
                new()
                {
                    PhaseNumber = 4,
                    Name = "优化迭代",
                    Duration = "第 22-30 天",
                    StartDay = 22,
                    EndDay = 30,
                    Tasks = new List<PhaseTask>
                    {
                        new() { Task = "效果监测", Description = "使用 AI 可见度监测工具检查引用情况", Owner = "marketing" },
                        new() { Task = "内容优化", Description = "根据数据优化表现不佳的内容", Owner = "content" },
                        new() { Task = "扩展覆盖", Description = "增加新的内容和平台", Owner = "content" },
                        new() { Task = "建立持续流程", Description = "制定长期内容发布计划", Owner = "marketing" }
                    },
                    ExpectedOutcome = "建立可持续的 LLM 引用优化流程"
                }
            },
            Milestones = new List<Milestone>
            {
                new() { Day = 7, Title = "基础就绪", Description = "所有平台账号和技术配置完成", Deliverables = new() { "平台账号清单", "技术审计报告" } },
                new() { Day = 14, Title = "首批内容发布", Description = "在主要平台发布初始内容", Deliverables = new() { "Reddit 回答 5+", "LinkedIn 帖子 3+", "Medium 文章 1+" } },
                new() { Day = 21, Title = "多媒体覆盖", Description = "视频和代码内容发布", Deliverables = new() { "YouTube 视频 1+", "GitHub 项目 1+" } },
                new() { Day = 30, Title = "效果评估", Description = "完成首轮优化并建立持续流程", Deliverables = new() { "效果报告", "持续计划" } }
            },
            ExpectedTimelines = new Dictionary<string, string>
            {
                ["Perplexity"] = "1-2 周见效",
                ["Grok"] = "1 周见效",
                ["ChatGPT"] = "2-4 周见效",
                ["Gemini"] = "2-4 周见效",
                ["Claude"] = "3-6 周见效"
            }
        };
    }

    #endregion

    #region 7.7 效果预期时间线

    public List<EffectTimeline> GetEffectTimelines()
    {
        return new List<EffectTimeline>
        {
            new()
            {
                Platform = "Grok",
                InitialEffectTime = "3-5 天",
                FullEffectTime = "1-2 周",
                Stages = new List<TimelineStage>
                {
                    new() { Stage = "即时", Timeframe = "1-3 天", ExpectedResult = "X/Twitter 内容开始被索引", Actions = new() { "活跃发帖", "参与热门话题" } },
                    new() { Stage = "短期", Timeframe = "1 周", ExpectedResult = "开始在相关查询中被引用", Actions = new() { "保持发帖频率", "增加互动" } },
                    new() { Stage = "稳定", Timeframe = "2 周", ExpectedResult = "建立稳定引用来源", Actions = new() { "持续内容输出", "监测效果" } }
                },
                Notes = "Grok 依赖 X/Twitter 实时数据，见效最快但需持续维护"
            },
            new()
            {
                Platform = "Perplexity",
                InitialEffectTime = "1 周",
                FullEffectTime = "2-3 周",
                Stages = new List<TimelineStage>
                {
                    new() { Stage = "索引", Timeframe = "3-5 天", ExpectedResult = "新内容被爬取", Actions = new() { "发布新鲜内容", "确保可访问性" } },
                    new() { Stage = "引用", Timeframe = "1-2 周", ExpectedResult = "开始出现在搜索结果", Actions = new() { "优化内容质量", "增加来源多样性" } },
                    new() { Stage = "稳定", Timeframe = "3 周", ExpectedResult = "成为稳定引用来源", Actions = new() { "持续更新", "扩展内容覆盖" } }
                },
                Notes = "Perplexity 偏好新鲜内容，Reddit 和新闻来源效果最好"
            },
            new()
            {
                Platform = "ChatGPT",
                InitialEffectTime = "2 周",
                FullEffectTime = "4-6 周",
                Stages = new List<TimelineStage>
                {
                    new() { Stage = "建立", Timeframe = "1-2 周", ExpectedResult = "内容开始被索引", Actions = new() { "发布高质量内容", "建立多平台存在" } },
                    new() { Stage = "权威", Timeframe = "3-4 周", ExpectedResult = "建立内容权威性", Actions = new() { "增加引用和链接", "持续内容输出" } },
                    new() { Stage = "稳定", Timeframe = "5-6 周", ExpectedResult = "成为可靠引用来源", Actions = new() { "维护内容质量", "扩展话题覆盖" } }
                },
                Notes = "ChatGPT 更看重权威性，需要时间积累"
            },
            new()
            {
                Platform = "Gemini",
                InitialEffectTime = "2 周",
                FullEffectTime = "4-6 周",
                Stages = new List<TimelineStage>
                {
                    new() { Stage = "SEO", Timeframe = "1-2 周", ExpectedResult = "Google 搜索排名提升", Actions = new() { "优化 Schema", "提高 E-E-A-T" } },
                    new() { Stage = "集成", Timeframe = "3-4 周", ExpectedResult = "Gemini 开始引用", Actions = new() { "持续 SEO 优化", "增加内容深度" } },
                    new() { Stage = "稳定", Timeframe = "5-6 周", ExpectedResult = "稳定引用来源", Actions = new() { "维护 SEO", "扩展内容" } }
                },
                Notes = "Gemini 与 Google 搜索紧密集成，SEO 优化很重要"
            },
            new()
            {
                Platform = "Claude",
                InitialEffectTime = "3 周",
                FullEffectTime = "6-8 周",
                Stages = new List<TimelineStage>
                {
                    new() { Stage = "深度", Timeframe = "2-3 周", ExpectedResult = "深度内容被索引", Actions = new() { "发布深度分析", "提供详细文档" } },
                    new() { Stage = "权威", Timeframe = "4-5 周", ExpectedResult = "建立专业权威", Actions = new() { "增加学术引用", "提供数据支持" } },
                    new() { Stage = "稳定", Timeframe = "6-8 周", ExpectedResult = "成为权威来源", Actions = new() { "持续深度内容", "维护准确性" } }
                },
                Notes = "Claude 偏好深度、准确的内容，需要较长时间建立信任"
            }
        };
    }

    #endregion

    #region 7.8 平台优先级排序

    public PlatformPriorityResult CalculatePlatformPriority(PlatformPriorityRequest request)
    {
        var rankings = new List<PlatformRanking>();
        var platforms = new[] { "chatgpt", "perplexity", "gemini", "claude", "grok" };

        foreach (var platform in platforms)
        {
            var score = CalculatePlatformScore(platform, request);
            var prefs = GetPlatformPreferences(platform);

            rankings.Add(new PlatformRanking
            {
                Platform = platform,
                DisplayName = prefs?.DisplayName ?? platform,
                Score = score,
                Rationale = GenerateRationale(platform, request),
                KeyActions = GenerateKeyActions(platform, request),
                ROIEstimate = EstimateROI(platform, request)
            });
        }

        rankings = rankings.OrderByDescending(r => r.Score).ToList();
        for (int i = 0; i < rankings.Count; i++)
        {
            rankings[i].Rank = i + 1;
        }

        return new PlatformPriorityResult
        {
            Rankings = rankings,
            Rationale = GenerateOverallRationale(rankings, request),
            Recommendations = GenerateOverallRecommendations(rankings, request)
        };
    }

    private int CalculatePlatformScore(string platform, PlatformPriorityRequest request)
    {
        var baseScore = 50;

        // 行业匹配
        if (request.Industry.ToLower().Contains("tech") || request.Industry.ToLower().Contains("软件"))
        {
            if (platform == "chatgpt" || platform == "claude") baseScore += 20;
            else if (platform == "gemini") baseScore += 15;
        }
        else if (request.Industry.ToLower().Contains("news") || request.Industry.ToLower().Contains("媒体"))
        {
            if (platform == "perplexity" || platform == "grok") baseScore += 20;
        }
        else if (request.Industry.ToLower().Contains("b2b") || request.Industry.ToLower().Contains("企业"))
        {
            if (platform == "chatgpt" || platform == "gemini") baseScore += 15;
        }

        // 内容类型匹配
        if (request.ContentType.ToLower().Contains("technical"))
        {
            if (platform == "chatgpt" || platform == "claude") baseScore += 10;
        }
        else if (request.ContentType.ToLower().Contains("real-time") || request.ContentType.ToLower().Contains("实时"))
        {
            if (platform == "perplexity" || platform == "grok") baseScore += 15;
        }

        // 资源调整
        if (request.Budget <= 2 || request.TeamSize <= 2)
        {
            // 资源有限时，优先快速见效的平台
            if (platform == "perplexity" || platform == "grok") baseScore += 10;
        }

        // 目标匹配
        if (request.Goals.Contains("brand_awareness"))
        {
            if (platform == "chatgpt" || platform == "perplexity") baseScore += 10;
        }
        if (request.Goals.Contains("thought_leadership"))
        {
            if (platform == "claude" || platform == "chatgpt") baseScore += 10;
        }

        return Math.Min(100, baseScore);
    }

    private string GenerateRationale(string platform, PlatformPriorityRequest request)
    {
        return platform switch
        {
            "chatgpt" => $"ChatGPT 是最广泛使用的 AI 助手，对 {request.Industry} 行业有良好覆盖",
            "perplexity" => "Perplexity 偏好新鲜内容，见效快，适合快速建立存在感",
            "gemini" => "Gemini 与 Google 搜索集成，SEO 优化可同时受益",
            "claude" => "Claude 偏好深度内容，适合建立专业权威",
            "grok" => "Grok 依赖 X/Twitter，适合实时话题和快速互动",
            _ => "通用 AI 平台"
        };
    }

    private List<string> GenerateKeyActions(string platform, PlatformPriorityRequest request)
    {
        return platform switch
        {
            "chatgpt" => new() { "优化 Reddit 存在", "发布 Medium 深度文章", "维护 Wikipedia 页面" },
            "perplexity" => new() { "保持内容新鲜度", "活跃 Reddit 参与", "发布时效性内容" },
            "gemini" => new() { "优化 Schema 标记", "提高 E-E-A-T 信号", "YouTube 视频优化" },
            "claude" => new() { "发布深度技术文档", "提供学术级内容", "维护 GitHub 项目" },
            "grok" => new() { "活跃 X/Twitter", "参与热门话题", "实时内容发布" },
            _ => new() { "通用内容优化" }
        };
    }

    private string EstimateROI(string platform, PlatformPriorityRequest request)
    {
        var prefs = GetPlatformPreferences(platform);
        var timeframe = prefs?.Characteristics.EffectiveTimeframe ?? "2-4 周";

        return platform switch
        {
            "grok" => $"快速见效 ({timeframe})，但需持续维护",
            "perplexity" => $"中等投入，{timeframe} 见效",
            "chatgpt" => $"高价值，{timeframe} 建立稳定引用",
            "gemini" => $"SEO 协同效应，{timeframe} 见效",
            "claude" => $"长期价值，{timeframe} 建立权威",
            _ => "中等 ROI"
        };
    }

    private string GenerateOverallRationale(List<PlatformRanking> rankings, PlatformPriorityRequest request)
    {
        var top = rankings.First();
        return $"基于 {request.Industry} 行业特点和 {request.ContentType} 内容类型，建议优先投入 {top.DisplayName}。" +
               $"研究显示不同 LLM 平台的引用来源仅有约 11% 重叠，因此需要针对每个平台制定独立策略。";
    }

    private List<string> GenerateOverallRecommendations(List<PlatformRanking> rankings, PlatformPriorityRequest request)
    {
        var recs = new List<string>
        {
            $"优先投入前 2 个平台: {rankings[0].DisplayName} 和 {rankings[1].DisplayName}",
            "建立跨平台内容复用流程，提高效率",
            "使用 AI 可见度监测工具追踪效果",
            "根据效果数据持续优化策略"
        };

        if (request.Budget <= 2)
        {
            recs.Add("资源有限时，专注于 Reddit 和 LinkedIn，这两个平台投入产出比最高");
        }

        return recs;
    }

    #endregion

    #region 7.14 Wikipedia 风格内容策略

    /// <summary>
    /// 分析内容的 Wikipedia 风格符合度并生成优化建议
    /// 原理：Wikipedia 占 ChatGPT 47.9% 引用
    /// </summary>
    public WikipediaStyleResult AnalyzeWikipediaStyle(WikipediaStyleRequest request)
    {
        var result = new WikipediaStyleResult
        {
            Topic = request.Topic,
            Analysis = AnalyzeContentForWikipediaStyle(request.ExistingContent),
            StyleGuide = GenerateWikipediaStyleGuide(),
            Template = GenerateWikipediaStyleTemplate(request.Topic, request.Industry)
        };

        result.Recommendations = GenerateWikipediaStyleRecommendations(result.Analysis);
        result.OverallScore = CalculateWikipediaStyleScore(result.Analysis);
        result.Summary = GenerateWikipediaStyleSummary(result);

        return result;
    }

    private WikipediaStyleAnalysis AnalyzeContentForWikipediaStyle(string content)
    {
        var analysis = new WikipediaStyleAnalysis
        {
            Issues = new List<string>(),
            Strengths = new List<string>()
        };

        if (string.IsNullOrEmpty(content))
        {
            analysis.NeutralityScore = 0;
            analysis.CitationDensityScore = 0;
            analysis.StructureScore = 0;
            analysis.FactualDensityScore = 0;
            analysis.VerifiabilityScore = 0;
            analysis.Issues.Add("未提供内容进行分析");
            return analysis;
        }

        // 中立性评分
        var promotionalWords = new[] { "最好", "第一", "领先", "独家", "革命性", "突破性", "最佳", "无与伦比" };
        var promotionalCount = promotionalWords.Count(w => content.Contains(w));
        analysis.NeutralityScore = Math.Max(0, 100 - promotionalCount * 15);
        if (promotionalCount > 0) analysis.Issues.Add($"发现 {promotionalCount} 个推广性词汇，影响中立性");
        else analysis.Strengths.Add("语气中立客观");

        // 引用密度评分
        var citationPatterns = new[] { "[1]", "[2]", "根据", "研究表明", "数据显示", "来源：" };
        var citationCount = citationPatterns.Count(p => content.Contains(p));
        analysis.CitationDensityScore = Math.Min(100, citationCount * 20);
        if (citationCount < 3) analysis.Issues.Add("引用来源不足，建议增加权威引用");
        else analysis.Strengths.Add("引用来源丰富");

        // 结构评分
        var hasHeadings = content.Contains("##") || content.Contains("<h2") || content.Contains("<h3");
        var hasList = content.Contains("- ") || content.Contains("* ") || content.Contains("<li");
        analysis.StructureScore = (hasHeadings ? 50 : 0) + (hasList ? 50 : 0);
        if (!hasHeadings) analysis.Issues.Add("缺少清晰的标题结构");
        if (hasList) analysis.Strengths.Add("使用了列表结构");

        // 事实密度评分
        var factPatterns = new[] { "%", "年", "月", "日", "美元", "元", "万", "亿" };
        var factCount = factPatterns.Count(p => content.Contains(p));
        analysis.FactualDensityScore = Math.Min(100, factCount * 15);
        if (factCount >= 5) analysis.Strengths.Add("包含丰富的事实数据");
        else analysis.Issues.Add("建议增加具体数据和事实");

        // 可验证性评分
        var verifiablePatterns = new[] { "http", "www.", ".com", ".org", "官方", "报告" };
        var verifiableCount = verifiablePatterns.Count(p => content.Contains(p));
        analysis.VerifiabilityScore = Math.Min(100, verifiableCount * 20);

        return analysis;
    }

    private WikipediaStyleGuide GenerateWikipediaStyleGuide()
    {
        return new WikipediaStyleGuide
        {
            Principles = new List<WikipediaStylePrinciple>
            {
                new() { Name = "中立观点 (NPOV)", Description = "以中立、客观的语气呈现信息，避免推广性语言", Example = "该公司成立于2020年，主要提供云计算服务。", CounterExample = "该公司是业界最好的云计算服务提供商。" },
                new() { Name = "可验证性", Description = "所有陈述都应有可靠来源支持", Example = "根据2024年Gartner报告，该市场规模达到500亿美元[1]。", CounterExample = "该市场规模非常大。" },
                new() { Name = "无原创研究", Description = "不包含未发表的分析、综合或推测", Example = "研究人员发现该方法可提高效率30%[2]。", CounterExample = "我们认为这种方法可能会更有效。" },
                new() { Name = "结构化呈现", Description = "使用清晰的标题、段落和列表组织内容", Example = "## 历史\n## 产品\n## 市场地位", CounterExample = "一大段没有分隔的文字" }
            },
            ToneGuidelines = new List<string>
            {
                "使用第三人称叙述",
                "避免使用\"我们\"、\"您\"等人称代词",
                "使用被动语态或中性陈述",
                "避免感叹号和夸张表达",
                "使用精确的数字而非模糊描述"
            },
            StructureGuidelines = new List<string>
            {
                "开头段落应概括主题的核心定义",
                "使用层级标题组织内容（H2、H3）",
                "每个段落聚焦一个主题",
                "使用列表呈现并列信息",
                "在文末提供参考来源"
            },
            CitationGuidelines = new List<string>
            {
                "每个事实陈述都应有来源支持",
                "优先使用权威来源（学术论文、官方报告、主流媒体）",
                "使用内联引用格式 [1] [2]",
                "在文末列出完整参考文献",
                "避免使用社交媒体作为主要来源"
            },
            AvoidList = new List<string>
            {
                "推广性语言（最好、第一、领先）",
                "主观评价（优秀、出色、卓越）",
                "未经验证的声明",
                "第一人称或第二人称",
                "感叹号和夸张修辞",
                "模糊的时间表述（最近、不久前）"
            }
        };
    }

    private WikipediaStyleTemplate GenerateWikipediaStyleTemplate(string topic, string industry)
    {
        return new WikipediaStyleTemplate
        {
            IntroductionTemplate = $"**{topic}** 是{industry}领域的[类型描述]。[简要定义和核心特征]。该[主体]成立于[年份]，总部位于[地点]。[1-2句关键事实]。",
            SuggestedSections = new List<string>
            {
                "## 概述",
                "## 历史",
                "## 主要产品/服务",
                "## 市场地位",
                "## 技术特点",
                "## 竞争格局",
                "## 参考来源"
            },
            CitationFormat = "[编号] 作者. \"标题\". 来源名称. 发布日期. URL",
            ConclusionTemplate = "## 参考来源\n\n[1] [来源1详情]\n[2] [来源2详情]\n[3] [来源3详情]"
        };
    }

    private List<WikipediaStyleRecommendation> GenerateWikipediaStyleRecommendations(WikipediaStyleAnalysis analysis)
    {
        var recs = new List<WikipediaStyleRecommendation>();

        if (analysis.NeutralityScore < 70)
        {
            recs.Add(new WikipediaStyleRecommendation
            {
                Category = "tone",
                Issue = "内容包含推广性语言",
                Recommendation = "移除主观评价词汇，使用客观陈述",
                Priority = "high",
                Example = "将\"最好的解决方案\"改为\"一种常用的解决方案\""
            });
        }

        if (analysis.CitationDensityScore < 60)
        {
            recs.Add(new WikipediaStyleRecommendation
            {
                Category = "citation",
                Issue = "引用来源不足",
                Recommendation = "为每个事实陈述添加权威来源引用",
                Priority = "high",
                Example = "添加 [1] 格式的内联引用，并在文末列出参考文献"
            });
        }

        if (analysis.StructureScore < 70)
        {
            recs.Add(new WikipediaStyleRecommendation
            {
                Category = "structure",
                Issue = "内容结构不够清晰",
                Recommendation = "使用 H2/H3 标题划分内容，添加列表",
                Priority = "medium",
                Example = "将长段落拆分为：概述、历史、产品、市场地位等章节"
            });
        }

        if (analysis.FactualDensityScore < 50)
        {
            recs.Add(new WikipediaStyleRecommendation
            {
                Category = "content",
                Issue = "事实密度不足",
                Recommendation = "增加具体数据、日期、数字等可验证信息",
                Priority = "medium",
                Example = "将\"市场份额很大\"改为\"2024年市场份额达到15.3%\""
            });
        }

        return recs;
    }

    private int CalculateWikipediaStyleScore(WikipediaStyleAnalysis analysis)
    {
        return (analysis.NeutralityScore * 25 +
                analysis.CitationDensityScore * 25 +
                analysis.StructureScore * 20 +
                analysis.FactualDensityScore * 15 +
                analysis.VerifiabilityScore * 15) / 100;
    }

    private string GenerateWikipediaStyleSummary(WikipediaStyleResult result)
    {
        var score = result.OverallScore;
        var level = score >= 80 ? "优秀" : score >= 60 ? "良好" : score >= 40 ? "需改进" : "较差";
        return $"Wikipedia 风格评分：{score}/100（{level}）。" +
               $"Wikipedia 占 ChatGPT 47.9% 的引用来源，采用 Wikipedia 风格可显著提升 AI 引用概率。" +
               $"主要改进方向：{string.Join("、", result.Recommendations.Take(2).Select(r => r.Issue))}。";
    }

    #endregion

    #region 7.16 YouTube 引用优化

    /// <summary>
    /// 生成 YouTube 引用优化策略
    /// 原理：YouTube 超越 Reddit 成为 #1 社交引用源（16% vs 10%）
    /// </summary>
    public YouTubeCitationResult GenerateYouTubeStrategy(YouTubeCitationRequest request)
    {
        return new YouTubeCitationResult
        {
            Analysis = GenerateYouTubeCitationAnalysis(),
            ContentStrategies = GenerateYouTubeContentStrategies(request.Industry, request.TopicAreas),
            OptimizationGuide = GenerateYouTubeOptimizationGuide(),
            ActionItems = GenerateYouTubeActionItems(request),
            Benchmarks = GenerateYouTubeBenchmarks(),
            Summary = $"YouTube 已超越 Reddit 成为 AI 平台的 #1 社交引用来源（16% vs 10%）。" +
                     $"针对 {request.Industry} 行业，建议重点投入教程和解说类视频内容。"
        };
    }

    private YouTubeCitationAnalysis GenerateYouTubeCitationAnalysis()
    {
        return new YouTubeCitationAnalysis
        {
            YouTubeShareOfCitations = 16.0,
            RedditShareOfCitations = 10.0,
            WhyYouTubeMatters = "YouTube 视频内容被 AI 平台频繁引用，尤其是教程、解说和评测类内容。视频字幕和描述为 AI 提供了丰富的可索引文本。",
            KeyInsights = new List<string>
            {
                "YouTube 引用占比 16%，超越 Reddit 的 10%",
                "教程和 How-to 视频被引用率最高",
                "视频字幕是 AI 索引的主要内容来源",
                "长视频（10-20分钟）比短视频更易被引用",
                "章节标记可提高特定内容被引用的概率"
            },
            PlatformsThatCiteYouTube = new List<string>
            {
                "Perplexity - 频繁引用 YouTube 视频作为来源",
                "ChatGPT - 引用视频内容和字幕",
                "Gemini - Google 生态系统优势",
                "Claude - 引用教程和技术视频"
            }
        };
    }

    private List<YouTubeContentStrategy> GenerateYouTubeContentStrategies(string industry, List<string> topicAreas)
    {
        var strategies = new List<YouTubeContentStrategy>
        {
            new()
            {
                ContentType = "教程/How-to",
                Description = "分步骤讲解如何完成特定任务",
                CitationPotential = 9,
                BestPractices = new List<string> { "清晰的步骤划分", "使用章节标记", "提供详细字幕", "在描述中总结关键步骤" },
                OptimalLength = "10-15 分钟",
                ExampleTopic = topicAreas.FirstOrDefault() ?? "产品使用教程"
            },
            new()
            {
                ContentType = "解说/Explainer",
                Description = "深入解释概念、技术或趋势",
                CitationPotential = 8,
                BestPractices = new List<string> { "从基础概念开始", "使用可视化辅助", "引用权威数据", "提供实际案例" },
                OptimalLength = "8-12 分钟",
                ExampleTopic = $"{industry} 行业趋势解读"
            },
            new()
            {
                ContentType = "对比评测",
                Description = "对比分析不同产品或方案",
                CitationPotential = 8,
                BestPractices = new List<string> { "客观中立的对比", "明确的评测标准", "数据支持的结论", "适用场景建议" },
                OptimalLength = "12-18 分钟",
                ExampleTopic = $"{industry} 工具对比"
            },
            new()
            {
                ContentType = "专家访谈",
                Description = "与行业专家的深度对话",
                CitationPotential = 7,
                BestPractices = new List<string> { "邀请权威专家", "准备深度问题", "提取关键观点", "添加时间戳" },
                OptimalLength = "20-30 分钟",
                ExampleTopic = $"{industry} 专家观点"
            }
        };

        return strategies;
    }

    private YouTubeOptimizationGuide GenerateYouTubeOptimizationGuide()
    {
        return new YouTubeOptimizationGuide
        {
            TitleOptimization = new List<string>
            {
                "使用问题式标题（How to...、What is...）",
                "包含核心关键词在前 60 字符",
                "避免点击诱饵式标题",
                "使用数字增加吸引力（5 Ways to...、Top 10...）"
            },
            DescriptionOptimization = new List<string>
            {
                "前 2-3 行包含核心内容摘要",
                "添加完整的章节时间戳",
                "包含相关关键词（自然融入）",
                "添加相关链接和资源",
                "字数建议 200-500 字"
            },
            TranscriptOptimization = new List<string>
            {
                "使用 YouTube 自动字幕并手动校正",
                "确保专业术语准确",
                "添加说话人标识（多人视频）",
                "上传 SRT 文件提高准确性"
            },
            ChapterOptimization = new List<string>
            {
                "每个章节 2-5 分钟",
                "章节标题包含关键词",
                "第一个章节从 0:00 开始",
                "章节数量 5-10 个为宜"
            },
            ThumbnailTips = new List<string>
            {
                "使用高对比度颜色",
                "包含关键文字（3-5 个词）",
                "人脸可提高点击率",
                "保持品牌一致性"
            }
        };
    }

    private List<YouTubeActionItem> GenerateYouTubeActionItems(YouTubeCitationRequest request)
    {
        return new List<YouTubeActionItem>
        {
            new() { Priority = 1, Action = "创建频道并完善品牌信息", Description = "设置频道名称、描述、横幅和头像", Impact = "high", Effort = "low" },
            new() { Priority = 2, Action = "发布首个教程视频", Description = $"针对 {request.TopicAreas.FirstOrDefault() ?? request.Industry} 创建 How-to 视频", Impact = "high", Effort = "medium" },
            new() { Priority = 3, Action = "优化视频 SEO", Description = "添加字幕、章节标记、详细描述", Impact = "high", Effort = "low" },
            new() { Priority = 4, Action = "建立发布节奏", Description = "每周 1-2 个视频，保持一致性", Impact = "medium", Effort = "high" },
            new() { Priority = 5, Action = "创建播放列表", Description = "按主题组织视频，提高发现性", Impact = "medium", Effort = "low" }
        };
    }

    private YouTubeBenchmarks GenerateYouTubeBenchmarks()
    {
        return new YouTubeBenchmarks
        {
            AverageCitationRate = "教程类视频被 AI 引用率约 3-5%",
            TopPerformingCategories = "How-to、教程、解说、评测",
            OptimalVideoLength = "10-20 分钟（教程）、5-10 分钟（解说）",
            BestPostingFrequency = "每周 1-2 个视频"
        };
    }

    #endregion

    #region 7.17 AI 流量转化追踪

    /// <summary>
    /// 生成 AI 流量转化追踪配置和建议
    /// 原理：AI 流量转化率 14.2% vs Google 2.8%，13,770 域名大规模验证
    /// </summary>
    public AITrafficConversionResult GenerateAITrafficConversionGuide(AITrafficConversionRequest request)
    {
        return new AITrafficConversionResult
        {
            Benchmarks = GenerateAITrafficBenchmarks(request.Industry),
            SetupGuide = GenerateAITrafficSetupGuide(),
            MetricsToTrack = GenerateAITrafficMetrics(),
            Optimizations = GenerateAITrafficOptimizations(request),
            ROIProjection = GenerateAITrafficROIProjection(request),
            Summary = $"AI 流量转化率（14.2%）是 Google 流量（2.8%）的 5 倍。" +
                     $"基于 13,770 个域名的大规模验证数据，AI 流量具有更高的商业价值。"
        };
    }

    private AITrafficBenchmarks GenerateAITrafficBenchmarks(string industry)
    {
        return new AITrafficBenchmarks
        {
            AIConversionRate = 14.2,
            GoogleConversionRate = 2.8,
            ConversionRateMultiplier = 5.07,
            SampleSize = 13770,
            DataSource = "Conductor 2026 报告",
            IndustryBenchmarks = new List<IndustryBenchmark>
            {
                new() { Industry = "SaaS/科技", AIConversionRate = 16.5, GoogleConversionRate = 3.2, Notes = "技术用户转化意愿高" },
                new() { Industry = "电商", AIConversionRate = 12.8, GoogleConversionRate = 2.5, Notes = "AI 推荐购买转化好" },
                new() { Industry = "金融服务", AIConversionRate = 18.2, GoogleConversionRate = 3.8, Notes = "高价值决策场景" },
                new() { Industry = "教育", AIConversionRate = 13.5, GoogleConversionRate = 2.2, Notes = "学习意图明确" },
                new() { Industry = "医疗健康", AIConversionRate = 15.1, GoogleConversionRate = 2.9, Notes = "信息需求强烈" }
            }
        };
    }

    private AITrafficSetupGuide GenerateAITrafficSetupGuide()
    {
        return new AITrafficSetupGuide
        {
            GA4Steps = new List<TrackingSetupStep>
            {
                new() { Order = 1, Title = "创建自定义维度", Description = "在 GA4 中创建 ai_referrer 自定义维度", Code = "Admin > Custom definitions > Create custom dimension", Notes = new() { "Scope: Session", "Name: AI Referrer" } },
                new() { Order = 2, Title = "配置数据流", Description = "确保数据流正确配置", Code = "Admin > Data streams > Web", Notes = new() { "启用增强型衡量" } },
                new() { Order = 3, Title = "创建 AI 流量细分", Description = "创建 AI 来源的用户细分", Code = "Explore > Segments > New segment", Notes = new() { "条件: referrer contains ai domains" } }
            },
            GTMSteps = new List<TrackingSetupStep>
            {
                new() { Order = 1, Title = "创建 Referrer 变量", Description = "捕获页面 referrer", Code = "Variables > Built-in > Page Referrer", Notes = new() { "启用 Page Referrer 变量" } },
                new() { Order = 2, Title = "创建 AI 检测触发器", Description = "检测 AI 平台来源", Code = "Triggers > New > Page View", Notes = new() { "条件: Referrer matches AI patterns" } },
                new() { Order = 3, Title = "创建事件标签", Description = "发送 AI 流量事件", Code = "Tags > GA4 Event", Notes = new() { "Event: ai_traffic_visit" } }
            },
            CustomDimensionCode = @"// AI Traffic Detection
gtag('set', 'user_properties', {
  'traffic_source_type': detectAISource(document.referrer)
});

function detectAISource(referrer) {
  const aiPatterns = ['chat.openai.com', 'perplexity.ai', 'claude.ai', 'gemini.google.com'];
  for (const pattern of aiPatterns) {
    if (referrer.includes(pattern)) return pattern;
  }
  return 'other';
}",
            EventTrackingCode = @"// AI Conversion Tracking
gtag('event', 'ai_conversion', {
  'ai_source': detectAISource(document.referrer),
  'conversion_type': 'signup', // or 'purchase', 'lead'
  'conversion_value': 100
});",
            ReferrerPatterns = new List<string>
            {
                "chat.openai.com", "chatgpt.com",
                "perplexity.ai",
                "claude.ai", "anthropic.com",
                "gemini.google.com", "bard.google.com",
                "copilot.microsoft.com",
                "you.com", "phind.com"
            }
        };
    }

    private List<AITrafficMetric> GenerateAITrafficMetrics()
    {
        return new List<AITrafficMetric>
        {
            new() { Name = "AI 流量占比", Description = "AI 来源流量占总流量的百分比", Formula = "AI Sessions / Total Sessions", Benchmark = "目标 > 5%", Importance = "high" },
            new() { Name = "AI 转化率", Description = "AI 流量的转化率", Formula = "AI Conversions / AI Sessions", Benchmark = "行业平均 14.2%", Importance = "high" },
            new() { Name = "AI vs Google 转化比", Description = "AI 与 Google 流量转化率对比", Formula = "AI CR / Google CR", Benchmark = "目标 > 3x", Importance = "medium" },
            new() { Name = "AI 流量增长率", Description = "AI 流量月环比增长", Formula = "(Current - Previous) / Previous", Benchmark = "目标 > 10% MoM", Importance = "medium" },
            new() { Name = "AI 来源分布", Description = "各 AI 平台流量占比", Formula = "Platform Sessions / Total AI Sessions", Benchmark = "ChatGPT 通常占 60%+", Importance = "low" }
        };
    }

    private List<AITrafficOptimization> GenerateAITrafficOptimizations(AITrafficConversionRequest request)
    {
        var opts = new List<AITrafficOptimization>
        {
            new() { Area = "内容优化", CurrentState = "待评估", Recommendation = "采用 Wikipedia 风格内容，提高 AI 引用率", ExpectedImpact = "+30-50% AI 流量", Priority = "high" },
            new() { Area = "技术 SEO", CurrentState = "待评估", Recommendation = "确保 AI 爬虫可访问，优化 robots.txt", ExpectedImpact = "+20% 可见度", Priority = "high" },
            new() { Area = "转化路径", CurrentState = "待评估", Recommendation = "为 AI 流量设计专门的落地页", ExpectedImpact = "+25% 转化率", Priority = "medium" }
        };

        if (!request.HasGA4Setup)
        {
            opts.Insert(0, new AITrafficOptimization
            {
                Area = "追踪设置",
                CurrentState = "未配置",
                Recommendation = "立即配置 GA4 AI 流量追踪",
                ExpectedImpact = "获得数据洞察",
                Priority = "critical"
            });
        }

        return opts;
    }

    private AITrafficROIProjection GenerateAITrafficROIProjection(AITrafficConversionRequest request)
    {
        return new AITrafficROIProjection
        {
            CurrentTrafficEstimate = "需要 GA4 数据",
            ProjectedAITrafficGrowth = "优化后预计 +50-100% AI 流量",
            ProjectedConversions = "基于 14.2% 转化率计算",
            ProjectedRevenue = "需要输入客单价计算",
            Assumptions = new List<string>
            {
                "基于 Conductor 2026 报告的 14.2% AI 转化率",
                "假设 AI 流量可通过优化增长 50-100%",
                "转化率可能因行业和产品而异",
                "需要 3-6 个月的优化周期"
            }
        };
    }

    #endregion

    #region 7.18 可影响域名策略

    /// <summary>
    /// 生成可影响域名策略
    /// 原理：74% 高引用域名可被营销影响，50 域名分析
    /// </summary>
    public InfluenceableDomainResult GenerateInfluenceableDomainStrategy(InfluenceableDomainRequest request)
    {
        return new InfluenceableDomainResult
        {
            Analysis = GenerateInfluenceabilityAnalysis(),
            InfluenceableDomains = GenerateInfluenceableDomainList(request.Industry),
            Strategies = GenerateDomainInfluenceStrategies(request),
            Roadmap = GenerateInfluenceabilityRoadmap(request),
            Summary = $"研究显示 74% 的高引用域名可被营销活动影响。" +
                     $"针对 {request.Industry} 行业，建议优先投入 Reddit、Medium 和 LinkedIn。"
        };
    }

    private InfluenceabilityAnalysis GenerateInfluenceabilityAnalysis()
    {
        return new InfluenceabilityAnalysis
        {
            InfluenceablePercentage = 74.0,
            TotalDomainsAnalyzed = 50,
            DataSource = "Goodie 2026 研究",
            KeyFindings = new List<string>
            {
                "74% 的高引用域名可被营销活动影响",
                "社区平台（Reddit、论坛）影响力最高",
                "内容平台（Medium、LinkedIn）次之",
                "新闻媒体需要 PR 策略配合",
                "Wikipedia 需要长期、合规的策略"
            },
            Categories = new List<DomainCategory>
            {
                new() { Category = "社区平台", Percentage = 30, Influenceability = "high", Examples = new() { "reddit.com", "quora.com", "stackoverflow.com" } },
                new() { Category = "内容平台", Percentage = 25, Influenceability = "high", Examples = new() { "medium.com", "linkedin.com", "dev.to" } },
                new() { Category = "视频平台", Percentage = 20, Influenceability = "high", Examples = new() { "youtube.com", "vimeo.com" } },
                new() { Category = "新闻媒体", Percentage = 15, Influenceability = "medium", Examples = new() { "techcrunch.com", "forbes.com" } },
                new() { Category = "百科/权威", Percentage = 10, Influenceability = "low", Examples = new() { "wikipedia.org", "britannica.com" } }
            }
        };
    }

    private List<InfluenceableDomain> GenerateInfluenceableDomainList(string industry)
    {
        return new List<InfluenceableDomain>
        {
            new() { Domain = "reddit.com", Category = "社区", InfluenceLevel = "high", InfluenceMethods = new() { "参与讨论", "发布有价值内容", "AMA 活动" }, Effort = "medium", ExpectedImpact = "高引用增长", PriorityScore = 95 },
            new() { Domain = "medium.com", Category = "内容", InfluenceLevel = "high", InfluenceMethods = new() { "发布深度文章", "建立 Publication", "交叉推广" }, Effort = "medium", ExpectedImpact = "稳定引用来源", PriorityScore = 90 },
            new() { Domain = "linkedin.com", Category = "专业", InfluenceLevel = "high", InfluenceMethods = new() { "发布行业洞察", "建立思想领导力", "公司页面优化" }, Effort = "low", ExpectedImpact = "B2B 引用增长", PriorityScore = 88 },
            new() { Domain = "youtube.com", Category = "视频", InfluenceLevel = "high", InfluenceMethods = new() { "创建教程视频", "优化 SEO", "添加字幕" }, Effort = "high", ExpectedImpact = "16% 社交引用份额", PriorityScore = 85 },
            new() { Domain = "github.com", Category = "技术", InfluenceLevel = "high", InfluenceMethods = new() { "开源项目", "技术文档", "社区参与" }, Effort = "high", ExpectedImpact = "技术品牌引用", PriorityScore = 82 },
            new() { Domain = "stackoverflow.com", Category = "问答", InfluenceLevel = "medium", InfluenceMethods = new() { "回答问题", "提供解决方案", "引用官方文档" }, Effort = "medium", ExpectedImpact = "技术可信度", PriorityScore = 78 },
            new() { Domain = "quora.com", Category = "问答", InfluenceLevel = "medium", InfluenceMethods = new() { "回答行业问题", "建立专家形象", "长期参与" }, Effort = "medium", ExpectedImpact = "通用引用来源", PriorityScore = 72 },
            new() { Domain = "twitter.com/x.com", Category = "社交", InfluenceLevel = "medium", InfluenceMethods = new() { "发布行业动态", "参与话题讨论", "建立影响力" }, Effort = "low", ExpectedImpact = "实时引用", PriorityScore = 70 }
        };
    }

    private List<DomainInfluenceStrategy> GenerateDomainInfluenceStrategies(InfluenceableDomainRequest request)
    {
        return new List<DomainInfluenceStrategy>
        {
            new()
            {
                DomainType = "社区平台",
                Strategy = "建立真实社区存在",
                Actions = new() { "识别相关 subreddit/社区", "90 天观察期了解规则", "提供有价值的回答和内容", "避免直接推广", "建立专家声誉" },
                Timeline = "3-6 个月见效",
                Budget = "低（主要是时间投入）",
                ExpectedROI = "高 - 社区引用持久且可信"
            },
            new()
            {
                DomainType = "内容平台",
                Strategy = "建立思想领导力",
                Actions = new() { "每周发布 1-2 篇深度文章", "使用 SEO 优化的标题", "交叉推广到其他平台", "建立邮件订阅", "与其他作者互动" },
                Timeline = "2-4 个月见效",
                Budget = "中（内容创作成本）",
                ExpectedROI = "中高 - 稳定的引用来源"
            },
            new()
            {
                DomainType = "视频平台",
                Strategy = "创建可引用视频内容",
                Actions = new() { "创建教程和解说视频", "优化标题和描述", "添加完整字幕", "使用章节标记", "保持发布节奏" },
                Timeline = "3-6 个月见效",
                Budget = "高（视频制作成本）",
                ExpectedROI = "高 - YouTube 占 16% 社交引用"
            },
            new()
            {
                DomainType = "新闻媒体",
                Strategy = "PR 和媒体关系",
                Actions = new() { "准备新闻稿", "建立媒体联系人", "提供独家数据/洞察", "参与行业活动", "专家评论机会" },
                Timeline = "1-3 个月见效",
                Budget = "高（PR 成本）",
                ExpectedROI = "中 - 权威性高但难以持续"
            }
        };
    }

    private InfluenceabilityRoadmap GenerateInfluenceabilityRoadmap(InfluenceableDomainRequest request)
    {
        return new InfluenceabilityRoadmap
        {
            TotalDuration = "6 个月",
            QuickWins = new List<string>
            {
                "优化 LinkedIn 公司页面和个人资料",
                "在 Medium 发布首篇深度文章",
                "开始 Reddit 观察期",
                "创建 YouTube 频道并发布首个视频"
            },
            LongTermGoals = new List<string>
            {
                "在 Reddit 建立专家声誉",
                "Medium 文章获得稳定阅读量",
                "YouTube 频道达到 1000 订阅",
                "被行业媒体引用"
            },
            Phases = new List<InfluencePhase>
            {
                new() { PhaseNumber = 1, Name = "基础建设", Duration = "第 1-2 周", Actions = new() { "创建/优化各平台账号", "制定内容计划", "研究目标社区" }, TargetDomains = new() { "linkedin.com", "medium.com" }, ExpectedOutcome = "平台存在建立" },
                new() { PhaseNumber = 2, Name = "内容启动", Duration = "第 3-6 周", Actions = new() { "发布首批内容", "开始社区参与", "收集反馈" }, TargetDomains = new() { "medium.com", "youtube.com", "reddit.com" }, ExpectedOutcome = "初始内容库建立" },
                new() { PhaseNumber = 3, Name = "持续运营", Duration = "第 7-12 周", Actions = new() { "保持发布节奏", "深度社区参与", "优化内容策略" }, TargetDomains = new() { "reddit.com", "stackoverflow.com" }, ExpectedOutcome = "稳定的内容产出" },
                new() { PhaseNumber = 4, Name = "规模化", Duration = "第 13-24 周", Actions = new() { "扩展到更多平台", "建立自动化流程", "追踪 AI 引用效果" }, TargetDomains = new() { "全平台" }, ExpectedOutcome = "可持续的引用增长" }
            }
        };
    }

    #endregion

    #region 7.19 Query Fan-out 年份检测

    private static readonly List<string> YearSensitiveKeywords = new()
    {
        "best", "top", "latest", "new", "current", "recent", "updated", "guide", "review",
        "comparison", "vs", "trends", "statistics", "data", "report", "forecast", "predictions",
        "最佳", "最新", "当前", "趋势", "统计", "数据", "报告", "预测", "对比", "评测"
    };

    public QueryYearDetectionResult DetectQueryYearRelevance(QueryYearDetectionRequest request)
    {
        _logger.LogInformation("[LLMCitation] Detecting year relevance for query: {Query}", request.Query);

        var currentYear = DateTime.Now.Year;
        var targetYear = request.TargetYear ?? currentYear;
        var queryLower = request.Query.ToLower();

        // 检测查询中是否包含年份
        var yearPattern = new System.Text.RegularExpressions.Regex(@"\b(20\d{2})\b");
        var queryYearMatch = yearPattern.Match(request.Query);
        var hasYearInQuery = queryYearMatch.Success;
        int? detectedYear = hasYearInQuery ? int.Parse(queryYearMatch.Value) : null;

        // 检测内容中的年份
        var contentYears = new List<int>();
        if (!string.IsNullOrEmpty(request.Content))
        {
            var contentMatches = yearPattern.Matches(request.Content);
            contentYears = contentMatches.Select(m => int.Parse(m.Value)).Distinct().OrderByDescending(y => y).ToList();
        }

        // 检测是否为年份敏感查询
        var matchedKeywords = YearSensitiveKeywords.Where(k => queryLower.Contains(k.ToLower())).ToList();
        var isYearRelevant = matchedKeywords.Count > 0;
        var yearAdditionProbability = isYearRelevant ? 0.281 : 0.05; // 28.1% 基于 Qwairy 研究

        var result = new QueryYearDetectionResult
        {
            Query = request.Query,
            HasYearInQuery = hasYearInQuery,
            DetectedYear = detectedYear,
            ContentHasYearMarker = contentYears.Contains(targetYear),
            YearsFoundInContent = contentYears,
            IsYearRelevantQuery = isYearRelevant,
            YearRelevanceScore = isYearRelevant ? 0.75 + (matchedKeywords.Count * 0.05) : 0.2,
            Analysis = new QueryYearAnalysis
            {
                QueryType = DetermineQueryType(queryLower),
                LikelyToAddYear = isYearRelevant && !hasYearInQuery,
                YearAdditionProbability = yearAdditionProbability,
                YearSensitiveKeywords = matchedKeywords,
                RecommendedYearStrategy = GenerateYearStrategy(isYearRelevant, hasYearInQuery, contentYears.Contains(targetYear))
            }
        };

        result.Recommendations = GenerateYearRecommendations(result, targetYear);
        return result;
    }

    private string DetermineQueryType(string query)
    {
        if (query.Contains("best") || query.Contains("top") || query.Contains("最佳"))
            return "ranking";
        if (query.Contains("how") || query.Contains("如何"))
            return "how-to";
        if (query.Contains("what") || query.Contains("什么"))
            return "definition";
        if (query.Contains("vs") || query.Contains("对比"))
            return "comparison";
        return "general";
    }

    private string GenerateYearStrategy(bool isYearRelevant, bool hasYearInQuery, bool contentHasYear)
    {
        if (!isYearRelevant) return "年份对此查询不重要，保持内容常青";
        if (hasYearInQuery && contentHasYear) return "查询和内容都包含年份，确保年份一致且为最新";
        if (hasYearInQuery && !contentHasYear) return "查询包含年份但内容缺失，需要在内容中添加年份标记";
        if (!hasYearInQuery && contentHasYear) return "内容已包含年份，可能被年份子查询匹配";
        return "建议在标题和内容中添加当前年份以匹配子查询";
    }

    private List<string> GenerateYearRecommendations(QueryYearDetectionResult result, int targetYear)
    {
        var recommendations = new List<string>();

        if (result.IsYearRelevantQuery && !result.ContentHasYearMarker)
        {
            recommendations.Add($"在标题中添加 {targetYear} 年份标记");
            recommendations.Add($"在内容开头提及 \"截至 {targetYear} 年\"");
            recommendations.Add("添加 \"最后更新\" 日期元数据");
        }

        if (result.Analysis.LikelyToAddYear)
        {
            recommendations.Add($"28.1% 的子查询会自动添加年份，确保内容包含 {targetYear}");
            recommendations.Add("在 H2 标题中包含年份以提高匹配率");
        }

        if (result.YearsFoundInContent.Any() && !result.YearsFoundInContent.Contains(targetYear))
        {
            recommendations.Add($"内容中发现旧年份 ({string.Join(", ", result.YearsFoundInContent)})，建议更新为 {targetYear}");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("内容年份标记良好，保持定期更新");
        }

        return recommendations;
    }

    #endregion

    #region 7.20 AutoGEO 内容重写建议

    public AutoGEORewriteResult GenerateAutoGEORewriteSuggestions(AutoGEORewriteRequest request)
    {
        _logger.LogInformation("[LLMCitation] Generating AutoGEO rewrite suggestions");

        var result = new AutoGEORewriteResult
        {
            OriginalContent = request.Content,
            CurrentGEOScore = CalculateCurrentGEOScore(request.Content),
            ExtractedRules = ExtractGEORules(request.TargetPlatform),
            Suggestions = GenerateRewriteSuggestions(request)
        };

        result.PredictedGEOScore = Math.Min(100, result.CurrentGEOScore + result.Suggestions.Sum(s => s.ImpactScore));
        result.ImprovementPercentage = result.CurrentGEOScore > 0 
            ? ((result.PredictedGEOScore - result.CurrentGEOScore) / result.CurrentGEOScore) * 100 
            : 0;

        return result;
    }

    private double CalculateCurrentGEOScore(string content)
    {
        if (string.IsNullOrEmpty(content)) return 0;

        double score = 50; // 基础分

        // 检查结构元素
        if (content.Contains("##") || content.Contains("<h2>")) score += 10;
        if (content.Contains("###") || content.Contains("<h3>")) score += 5;
        if (content.Contains("|") || content.Contains("<table>")) score += 8;
        if (content.Contains("1.") || content.Contains("<ol>")) score += 5;
        if (content.Contains("-") || content.Contains("<ul>")) score += 5;

        // 检查长度
        var wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount >= 2500 && wordCount <= 4000) score += 10;

        // 检查引用/来源
        if (content.Contains("according to") || content.Contains("研究表明") || content.Contains("数据显示")) score += 7;

        return Math.Min(100, score);
    }

    private List<string> ExtractGEORules(string platform)
    {
        var commonRules = new List<string>
        {
            "使用 120-180 词的段落长度 (+70% 引用率)",
            "在前 40-60 词提供直接答案 (+140% 引用率)",
            "使用 H2/H3 结构化内容 (3.2x 引用率)",
            "添加表格总结关键信息 (2.8x 引用率)",
            "保持 2,500-4,000 词的最佳长度",
            "实体密度达到 15+ (4.8x 引用率)",
            "避免问号标题 (-0.9 引用率)"
        };

        var platformRules = platform.ToLower() switch
        {
            "chatgpt" => new List<string> { "优先 Wikipedia 风格内容", "中立客观语气", "丰富引用来源" },
            "perplexity" => new List<string> { "强调新鲜度和时效性", "提供多角度观点", "包含最新数据" },
            "claude" => new List<string> { "深度分析优先", "逻辑结构清晰", "证据链完整" },
            "gemini" => new List<string> { "多模态友好", "实体关系明确", "FAQ 格式有效" },
            _ => new List<string>()
        };

        return commonRules.Concat(platformRules).ToList();
    }

    private List<GEORewriteSuggestion> GenerateRewriteSuggestions(AutoGEORewriteRequest request)
    {
        var suggestions = new List<GEORewriteSuggestion>();
        var content = request.Content;

        // 检查段落长度
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var para in paragraphs.Take(3))
        {
            var wordCount = para.Split(' ').Length;
            if (wordCount > 200)
            {
                suggestions.Add(new GEORewriteSuggestion
                {
                    Type = "paragraph_length",
                    OriginalText = para.Length > 100 ? para.Substring(0, 100) + "..." : para,
                    SuggestedText = "拆分为 120-180 词的段落",
                    Reason = "120-180 词段落 +70% 引用率",
                    ImpactScore = 7,
                    Priority = "high"
                });
            }
        }

        // 检查开头是否有直接答案
        if (paragraphs.Length > 0)
        {
            var firstPara = paragraphs[0];
            var firstParaWords = firstPara.Split(' ').Length;
            if (firstParaWords > 60 || !ContainsDirectAnswer(firstPara))
            {
                suggestions.Add(new GEORewriteSuggestion
                {
                    Type = "answer_capsule",
                    OriginalText = firstPara.Length > 100 ? firstPara.Substring(0, 100) + "..." : firstPara,
                    SuggestedText = "在前 40-60 词提供直接答案",
                    Reason = "前 40-60 词直接答案 +140% 引用率",
                    ImpactScore = 14,
                    Priority = "critical"
                });
            }
        }

        // 检查结构元素
        if (!content.Contains("##") && !content.Contains("<h2>"))
        {
            suggestions.Add(new GEORewriteSuggestion
            {
                Type = "structure",
                OriginalText = "",
                SuggestedText = "添加 H2/H3 标题结构",
                Reason = "H2/H3 结构 3.2x 引用率",
                ImpactScore = 10,
                Priority = "high"
            });
        }

        // 检查表格
        if (!content.Contains("|") && !content.Contains("<table>"))
        {
            suggestions.Add(new GEORewriteSuggestion
            {
                Type = "table",
                OriginalText = "",
                SuggestedText = "添加表格总结关键信息",
                Reason = "表格 2.8x 引用率",
                ImpactScore = 8,
                Priority = "medium"
            });
        }

        return suggestions;
    }

    private bool ContainsDirectAnswer(string text)
    {
        var directAnswerPatterns = new[] { "是", "为", "指", "means", "is", "are", "refers to", "定义为" };
        return directAnswerPatterns.Any(p => text.ToLower().Contains(p.ToLower()));
    }

    #endregion

    #region 7.21 平台特定引用策略

    private static readonly Dictionary<string, PlatformCitationProfile> PlatformProfiles = new()
    {
        ["perplexity"] = new PlatformCitationProfile
        {
            TopCitationSource = "Reddit",
            TopSourceMultiplier = 6.1,
            PreferredContentTypes = new() { "讨论帖", "问答", "用户评价" },
            CitationStyle = "多来源验证",
            UpdateFrequencyPreference = "实时/每日"
        },
        ["grok"] = new PlatformCitationProfile
        {
            TopCitationSource = "Reddit",
            TopSourceMultiplier = 2.3,
            PreferredContentTypes = new() { "社区讨论", "热门话题", "实时信息" },
            CitationStyle = "社交优先",
            UpdateFrequencyPreference = "实时"
        },
        ["chatgpt"] = new PlatformCitationProfile
        {
            TopCitationSource = "Wikipedia",
            TopSourceMultiplier = 1.5,
            PreferredContentTypes = new() { "百科内容", "权威指南", "学术资料" },
            CitationStyle = "权威来源",
            UpdateFrequencyPreference = "季度"
        },
        ["claude"] = new PlatformCitationProfile
        {
            TopCitationSource = "学术/技术文档",
            TopSourceMultiplier = 1.3,
            PreferredContentTypes = new() { "深度分析", "技术文档", "研究报告" },
            CitationStyle = "证据链",
            UpdateFrequencyPreference = "月度"
        },
        ["gemini"] = new PlatformCitationProfile
        {
            TopCitationSource = "Google 生态",
            TopSourceMultiplier = 1.4,
            PreferredContentTypes = new() { "FAQ", "结构化数据", "多模态内容" },
            CitationStyle = "平衡多源",
            UpdateFrequencyPreference = "周度"
        }
    };

    public PlatformCitationStrategyResult GeneratePlatformCitationStrategy(PlatformCitationStrategyRequest request)
    {
        _logger.LogInformation("[LLMCitation] Generating platform citation strategy for: {Platform}", request.TargetPlatform);

        var platformKey = request.TargetPlatform.ToLower();
        if (!PlatformProfiles.TryGetValue(platformKey, out var profile))
        {
            profile = PlatformProfiles["chatgpt"]; // 默认
            platformKey = "chatgpt";
        }

        return new PlatformCitationStrategyResult
        {
            Platform = platformKey,
            PlatformDisplayName = GetPlatformDisplayName(platformKey),
            Profile = profile,
            ChannelStrategies = GenerateChannelStrategies(platformKey, request.Industry),
            QuickWins = GeneratePlatformQuickWins(platformKey),
            ActionPlan = GeneratePlatformActionPlan(platformKey, request.Industry)
        };
    }

    private string GetPlatformDisplayName(string platform)
    {
        return platform switch
        {
            "perplexity" => "Perplexity AI",
            "grok" => "Grok (X/Twitter)",
            "chatgpt" => "ChatGPT",
            "claude" => "Claude",
            "gemini" => "Google Gemini",
            _ => platform
        };
    }

    private List<CitationChannelStrategy> GenerateChannelStrategies(string platform, string industry)
    {
        var strategies = new List<CitationChannelStrategy>();

        if (platform == "perplexity" || platform == "grok")
        {
            strategies.Add(new CitationChannelStrategy
            {
                Channel = "Reddit",
                CitationMultiplier = platform == "perplexity" ? 6.1 : 2.3,
                Strategy = "深度社区参与",
                Actions = new() { "识别相关 subreddit", "提供有价值的回答", "建立专家声誉", "避免直接推广" },
                ExpectedImpact = $"{(platform == "perplexity" ? "6.1x" : "2.3x")} 引用率提升"
            });
        }

        strategies.Add(new CitationChannelStrategy
        {
            Channel = "YouTube",
            CitationMultiplier = 1.6,
            Strategy = "视频内容布局",
            Actions = new() { "创建教程视频", "优化视频描述", "添加时间戳章节", "鼓励评论互动" },
            ExpectedImpact = "16% 社交引用份额"
        });

        strategies.Add(new CitationChannelStrategy
        {
            Channel = "LinkedIn",
            CitationMultiplier = 1.4,
            Strategy = "B2B 思想领导力",
            Actions = new() { "发布行业洞察", "参与专业讨论", "建立公司页面", "员工倡导计划" },
            ExpectedImpact = "B2B 领域高引用率"
        });

        return strategies;
    }

    private List<string> GeneratePlatformQuickWins(string platform)
    {
        return platform switch
        {
            "perplexity" => new() { "在 Reddit 回答 3 个行业问题", "确保网站内容新鲜度 < 90 天", "添加多来源引用" },
            "grok" => new() { "参与 X/Twitter 热门讨论", "在 Reddit 建立存在", "发布实时行业评论" },
            "chatgpt" => new() { "创建 Wikipedia 风格内容", "添加结构化数据", "确保权威来源引用" },
            "claude" => new() { "深化技术文档", "添加证据链", "提供详细分析" },
            "gemini" => new() { "优化 FAQ 结构", "添加 Schema 标记", "创建多模态内容" },
            _ => new() { "优化内容结构", "添加权威引用", "保持内容更新" }
        };
    }

    private PlatformActionPlan GeneratePlatformActionPlan(string platform, string industry)
    {
        return new PlatformActionPlan
        {
            TotalDuration = "4 周",
            KPIs = new() { "引用出现次数", "引用来源多样性", "品牌提及增长" },
            Phases = new List<PlatformActionPhase>
            {
                new() { Week = 1, Focus = "基础建设", Tasks = new() { "审计现有内容", "创建平台账号", "制定内容计划" }, ExpectedOutcome = "基础设施就绪" },
                new() { Week = 2, Focus = "内容创建", Tasks = new() { "发布首批内容", "优化现有页面", "建立发布节奏" }, ExpectedOutcome = "内容库建立" },
                new() { Week = 3, Focus = "社区参与", Tasks = new() { "参与目标社区", "回答相关问题", "建立关系" }, ExpectedOutcome = "社区存在建立" },
                new() { Week = 4, Focus = "优化迭代", Tasks = new() { "分析效果数据", "优化策略", "扩展覆盖" }, ExpectedOutcome = "持续改进机制" }
            }
        };
    }

    #endregion

    #region 7.22 LinkedIn B2B 引用优化

    public LinkedInB2BResult GenerateLinkedInB2BStrategy(LinkedInB2BRequest request)
    {
        _logger.LogInformation("[LLMCitation] Generating LinkedIn B2B strategy for: {Company}", request.CompanyName);

        return new LinkedInB2BResult
        {
            CompanyName = request.CompanyName,
            Analysis = new LinkedInCitationAnalysis
            {
                LinkedInCitationShare = 14.5, // 接近 YouTube 的 16%
                ComparisonToYouTube = "LinkedIn 引用量接近 YouTube (14.5% vs 16%)，B2B 领域更高",
                TopPerformingContentTypes = new() { "行业洞察", "数据报告", "专家观点", "案例研究" },
                B2BAdvantage = "B2B 品牌在 LinkedIn 的引用率比 B2C 高 2.3x"
            },
            ContentStrategies = GenerateLinkedInContentStrategies(request.ContentType),
            OptimizationGuide = GenerateLinkedInOptimizationGuide(),
            B2BSpecificTips = GenerateB2BSpecificTips(request.Industry)
        };
    }

    private List<LinkedInContentStrategy> GenerateLinkedInContentStrategies(string contentType)
    {
        return new List<LinkedInContentStrategy>
        {
            new()
            {
                ContentType = "thought_leadership",
                Description = "思想领导力文章",
                BestPractices = new() { "分享独特行业洞察", "基于数据的观点", "提供可操作建议", "保持一致的发布节奏" },
                PostingFrequency = "每周 2-3 篇",
                ExpectedEngagement = "高互动率，适合 AI 引用"
            },
            new()
            {
                ContentType = "data_report",
                Description = "数据报告和研究",
                BestPractices = new() { "原创研究数据", "可视化图表", "关键发现摘要", "提供下载链接" },
                PostingFrequency = "每月 1-2 篇",
                ExpectedEngagement = "高分享率，权威性强"
            },
            new()
            {
                ContentType = "case_study",
                Description = "客户案例研究",
                BestPractices = new() { "具体数字成果", "客户引言", "实施过程", "可复制的方法" },
                PostingFrequency = "每月 2-4 篇",
                ExpectedEngagement = "高转化率，信任建立"
            }
        };
    }

    private LinkedInOptimizationGuide GenerateLinkedInOptimizationGuide()
    {
        return new LinkedInOptimizationGuide
        {
            ProfileOptimization = new()
            {
                "完善公司页面所有字段",
                "使用关键词优化公司描述",
                "添加公司专长标签",
                "定期更新公司动态"
            },
            ContentOptimization = new()
            {
                "使用原生文档而非外链",
                "添加 3-5 个相关话题标签",
                "在前 2 行抓住注意力",
                "使用 emoji 增加可读性（适度）"
            },
            EngagementTactics = new()
            {
                "在发布后 1 小时内回复评论",
                "主动评论行业领袖内容",
                "参与相关群组讨论",
                "鼓励员工分享和互动"
            },
            HashtagStrategy = new()
            {
                "使用 3-5 个标签",
                "混合热门和细分标签",
                "创建品牌专属标签",
                "追踪标签表现"
            }
        };
    }

    private List<string> GenerateB2BSpecificTips(string industry)
    {
        var tips = new List<string>
        {
            "LinkedIn 是 B2B 品牌 AI 引用的首选平台",
            "决策者活跃时间：周二-周四 8-10am",
            "长文章（1500+ 词）比短帖子引用率高 3x",
            "员工倡导可增加 8x 内容覆盖",
            "公司页面关注者每增加 1000，引用率提升 5%"
        };

        var industryTips = industry.ToLower() switch
        {
            "saas" => new List<string> { "分享产品更新和路线图", "技术深度文章表现最佳", "客户成功故事高转化" },
            "finance" => new List<string> { "市场分析和预测受欢迎", "合规和监管内容有需求", "数据可视化增加分享" },
            "healthcare" => new List<string> { "研究成果和临床数据", "患者故事（匿名）", "行业趋势分析" },
            _ => new List<string> { "行业趋势分析", "最佳实践分享", "专家访谈" }
        };

        return tips.Concat(industryTips).ToList();
    }

    #endregion

    #region 7.24 llms.txt 模型定制

    private static readonly Dictionary<string, ModelLlmsTxtProfile> ModelProfiles = new()
    {
        ["claude"] = new ModelLlmsTxtProfile
        {
            PreferredContentType = "证据页",
            PriorityPages = new() { "研究报告", "技术文档", "白皮书", "案例研究" },
            StructurePreference = "深度分析结构",
            DetailLevel = "详细"
        },
        ["chatgpt"] = new ModelLlmsTxtProfile
        {
            PreferredContentType = "规范页",
            PriorityPages = new() { "官方文档", "API 参考", "使用指南", "FAQ" },
            StructurePreference = "标准化结构",
            DetailLevel = "中等"
        },
        ["perplexity"] = new ModelLlmsTxtProfile
        {
            PreferredContentType = "FAQ",
            PriorityPages = new() { "常见问题", "快速入门", "对比页面", "最新更新" },
            StructurePreference = "问答结构",
            DetailLevel = "简洁"
        },
        ["mistral"] = new ModelLlmsTxtProfile
        {
            PreferredContentType = "决策矩阵",
            PriorityPages = new() { "对比分析", "选型指南", "评估标准", "定价页面" },
            StructurePreference = "表格化结构",
            DetailLevel = "结构化"
        },
        ["gemini"] = new ModelLlmsTxtProfile
        {
            PreferredContentType = "多模态",
            PriorityPages = new() { "视觉指南", "信息图", "视频教程", "交互式内容" },
            StructurePreference = "多媒体结构",
            DetailLevel = "丰富"
        }
    };

    public LlmsTxtModelCustomResult GenerateModelCustomLlmsTxt(LlmsTxtModelCustomRequest request)
    {
        _logger.LogInformation("[LLMCitation] Generating model-custom llms.txt for: {Model}", request.TargetModel);

        var modelKey = request.TargetModel.ToLower();
        if (!ModelProfiles.TryGetValue(modelKey, out var profile))
        {
            profile = ModelProfiles["chatgpt"];
            modelKey = "chatgpt";
        }

        var sections = GenerateModelSpecificSections(profile, request);
        var llmsTxt = BuildModelCustomLlmsTxt(request, sections);

        return new LlmsTxtModelCustomResult
        {
            TargetModel = modelKey,
            ModelDisplayName = GetModelDisplayName(modelKey),
            Profile = profile,
            GeneratedLlmsTxt = llmsTxt,
            Sections = sections,
            ModelSpecificTips = GenerateModelSpecificTips(modelKey)
        };
    }

    private string GetModelDisplayName(string model)
    {
        return model switch
        {
            "claude" => "Claude (Anthropic)",
            "chatgpt" => "ChatGPT (OpenAI)",
            "perplexity" => "Perplexity AI",
            "mistral" => "Mistral AI",
            "gemini" => "Google Gemini",
            _ => model
        };
    }

    private List<LlmsTxtSection> GenerateModelSpecificSections(ModelLlmsTxtProfile profile, LlmsTxtModelCustomRequest request)
    {
        var sections = new List<LlmsTxtSection>
        {
            new()
            {
                Name = "Company Overview",
                Content = $"# {request.CompanyName}\n\n> {request.Industry} 领域的领先解决方案提供商",
                IsModelSpecific = false,
                Rationale = "所有模型都需要基本公司信息"
            }
        };

        foreach (var pageType in profile.PriorityPages)
        {
            sections.Add(new LlmsTxtSection
            {
                Name = pageType,
                Content = $"## {pageType}\n\n- [{pageType}]({request.WebsiteUrl}/{pageType.ToLower().Replace(" ", "-")})",
                IsModelSpecific = true,
                Rationale = $"{profile.PreferredContentType} 类型内容对此模型优先级高"
            });
        }

        return sections;
    }

    private string BuildModelCustomLlmsTxt(LlmsTxtModelCustomRequest request, List<LlmsTxtSection> sections)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var section in sections)
        {
            sb.AppendLine(section.Content);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private List<string> GenerateModelSpecificTips(string model)
    {
        return model switch
        {
            "claude" => new() { "Claude 偏好深度分析和证据链", "包含研究引用和数据来源", "使用逻辑结构组织内容" },
            "chatgpt" => new() { "ChatGPT 偏好规范化文档", "包含官方定义和标准", "使用清晰的层级结构" },
            "perplexity" => new() { "Perplexity 偏好 FAQ 格式", "包含最新更新信息", "使用问答结构" },
            "mistral" => new() { "Mistral 偏好决策支持内容", "包含对比表格", "使用结构化数据" },
            "gemini" => new() { "Gemini 偏好多模态内容", "包含视觉元素描述", "使用 Schema 标记" },
            _ => new() { "使用清晰的结构", "包含关键信息", "保持内容更新" }
        };
    }

    #endregion

    #region 7.25 引用表面积分析

    public CitationSurfaceResult AnalyzeCitationSurface(CitationSurfaceRequest request)
    {
        _logger.LogInformation("[LLMCitation] Analyzing citation surface for: {Brand}", request.BrandName);

        return new CitationSurfaceResult
        {
            BrandName = request.BrandName,
            Analysis = new CitationSurfaceAnalysis
            {
                BrandMentionScore = 65, // 示例分数
                BacklinkScore = 45,
                CombinedScore = 72,
                MentionVsBacklinkRatio = "品牌提及 3x 比外链更预测 AI 可见度",
                Insight = "引用+提及组合可带来 40% 更高的连续出现率"
            },
            Channels = GenerateCitationSurfaceChannels(),
            Strategy = GenerateCitationSurfaceStrategy(),
            Recommendations = new List<string>
            {
                "优先建设品牌提及而非外链",
                "在 Reddit/Quora 等平台自然提及品牌",
                "创建可被引用的原创研究",
                "建立行业专家形象增加提及",
                "组合策略：提及 + 外链 = 40% 更高连续出现率"
            }
        };
    }

    private List<CitationSurfaceChannel> GenerateCitationSurfaceChannels()
    {
        return new List<CitationSurfaceChannel>
        {
            new()
            {
                Channel = "Reddit",
                MentionPotential = 9.0,
                BacklinkPotential = 3.0,
                RecommendedFocus = "品牌提及",
                Actions = new() { "参与相关讨论", "提供有价值回答", "自然提及品牌" }
            },
            new()
            {
                Channel = "Medium",
                MentionPotential = 7.0,
                BacklinkPotential = 8.0,
                RecommendedFocus = "组合策略",
                Actions = new() { "发布深度文章", "包含品牌案例", "添加相关链接" }
            },
            new()
            {
                Channel = "LinkedIn",
                MentionPotential = 8.0,
                BacklinkPotential = 6.0,
                RecommendedFocus = "品牌提及",
                Actions = new() { "思想领导力内容", "行业洞察分享", "员工倡导" }
            },
            new()
            {
                Channel = "YouTube",
                MentionPotential = 8.5,
                BacklinkPotential = 7.0,
                RecommendedFocus = "组合策略",
                Actions = new() { "教程视频", "产品演示", "描述区链接" }
            }
        };
    }

    private CitationSurfaceStrategy GenerateCitationSurfaceStrategy()
    {
        return new CitationSurfaceStrategy
        {
            MentionBuildingTactics = new()
            {
                "在社区讨论中自然提及品牌",
                "创建可被引用的原创内容",
                "建立行业专家形象",
                "参与播客和访谈",
                "发布研究报告和数据"
            },
            BacklinkBuildingTactics = new()
            {
                "客座文章发布",
                "资源页面链接",
                "合作伙伴交叉链接",
                "新闻稿发布",
                "目录和列表收录"
            },
            CombinedApproach = new()
            {
                "在高权威平台发布内容（提及+链接）",
                "创建可嵌入的工具或资源",
                "建立品牌大使计划",
                "参与行业活动并获得报道"
            },
            ExpectedImpact = "组合策略可带来 40% 更高的 AI 连续出现率"
        };
    }

    #endregion

    #region 7.26 高权威平台快速索引

    private static readonly List<RapidIndexPlatform> HighAuthorityPlatforms = new()
    {
        new()
        {
            Platform = "LinkedIn",
            DomainRating = 90,
            EstimatedIndexTime = "3 小时",
            Steps = new() { "发布文章到 LinkedIn", "添加相关标签", "分享到个人动态", "请求同事互动" },
            ContentFormat = "长文章 (1500+ 词)",
            BestPractices = new() { "使用原生发布", "添加图片", "前 2 行抓住注意力" }
        },
        new()
        {
            Platform = "Medium",
            DomainRating = 85,
            EstimatedIndexTime = "6 小时",
            Steps = new() { "发布到 Medium", "添加到相关 Publication", "使用热门标签", "分享到社交媒体" },
            ContentFormat = "深度文章 (2000+ 词)",
            BestPractices = new() { "使用引人注目的标题", "添加高质量图片", "结构化内容" }
        },
        new()
        {
            Platform = "GitHub",
            DomainRating = 95,
            EstimatedIndexTime = "2 小时",
            Steps = new() { "创建公开仓库", "添加详细 README", "发布 Release", "添加相关 Topics" },
            ContentFormat = "代码仓库 + 文档",
            BestPractices = new() { "详细的 README", "添加 LICENSE", "使用 GitHub Pages" }
        },
        new()
        {
            Platform = "YouTube",
            DomainRating = 92,
            EstimatedIndexTime = "4 小时",
            Steps = new() { "上传视频", "优化标题和描述", "添加时间戳", "创建播放列表" },
            ContentFormat = "视频 (5-15 分钟)",
            BestPractices = new() { "关键词优化标题", "详细描述", "自定义缩略图" }
        }
    };

    public RapidIndexResult GenerateRapidIndexStrategy(RapidIndexRequest request)
    {
        _logger.LogInformation("[LLMCitation] Generating rapid index strategy for: {Url}", request.ContentUrl);

        var targetPlatforms = request.TargetPlatforms.Count > 0 
            ? HighAuthorityPlatforms.Where(p => request.TargetPlatforms.Contains(p.Platform.ToLower())).ToList()
            : HighAuthorityPlatforms;

        return new RapidIndexResult
        {
            ContentUrl = request.ContentUrl,
            Platforms = targetPlatforms,
            Strategy = new RapidIndexStrategy
            {
                TotalDuration = "24 小时内完成多平台索引",
                PlatformPriority = targetPlatforms.OrderBy(p => p.DomainRating).Select(p => p.Platform).Reverse().ToList(),
                Phases = GenerateRapidIndexPhases(targetPlatforms)
            },
            QuickActions = new List<string>
            {
                "立即在 LinkedIn 发布文章摘要",
                "在 GitHub 创建相关仓库（如适用）",
                "在 Medium 发布完整文章",
                "创建 YouTube 视频版本"
            },
            EstimatedIndexTime = "LinkedIn 3h, GitHub 2h, Medium 6h, YouTube 4h"
        };
    }

    private List<RapidIndexPhase> GenerateRapidIndexPhases(List<RapidIndexPlatform> platforms)
    {
        var phases = new List<RapidIndexPhase>();
        var hour = 0;

        foreach (var platform in platforms.OrderByDescending(p => p.DomainRating))
        {
            phases.Add(new RapidIndexPhase
            {
                Hour = hour,
                Platform = platform.Platform,
                Action = $"发布到 {platform.Platform}",
                ExpectedOutcome = $"预计 {platform.EstimatedIndexTime} 内被索引"
            });
            hour += 2;
        }

        return phases;
    }

    #endregion
}
