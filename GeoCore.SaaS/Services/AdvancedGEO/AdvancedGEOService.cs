using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.AdvancedGEO;

public class AdvancedGEOService
{
    #region 7.9 Query Fan-out 分析器

    private static readonly Dictionary<string, List<string>> QueryPatterns = new()
    {
        ["what"] = new() { "什么是", "是什么", "what is", "what are", "定义", "概念" },
        ["how"] = new() { "如何", "怎么", "怎样", "how to", "how do", "方法", "步骤" },
        ["why"] = new() { "为什么", "为何", "why", "原因", "理由" },
        ["when"] = new() { "什么时候", "何时", "when", "时间", "时机" },
        ["where"] = new() { "哪里", "在哪", "where", "地点", "位置" },
        ["who"] = new() { "谁", "哪个", "who", "人物", "专家" },
        ["comparison"] = new() { "对比", "比较", "vs", "versus", "区别", "差异", "哪个好" },
        ["list"] = new() { "有哪些", "列表", "top", "best", "推荐", "排行" }
    };

    public QueryFanoutResult AnalyzeQueryFanout(QueryFanoutRequest request)
    {
        var mainQuery = request.MainQuery;
        var clusters = GenerateSubQueryClusters(mainQuery, request.Industry, request.Language);
        
        var totalSubQueries = clusters.Sum(c => c.Queries.Count);
        var coveredQueries = clusters.Sum(c => c.Queries.Count(q => q.IsCovered));
        var coverageScore = totalSubQueries > 0 ? (coveredQueries * 100.0 / totalSubQueries) : 0;

        // +161% 引用率来自 SurferSEO 2026 研究
        var potentialBoost = coverageScore >= 80 ? 161 : coverageScore >= 60 ? 100 : coverageScore >= 40 ? 50 : 20;

        var uncovered = clusters.SelectMany(c => c.Queries.Where(q => !q.IsCovered).Select(q => q.Query)).Take(10).ToList();

        var recommendations = new List<string>();
        if (coverageScore < 50)
            recommendations.Add("子查询覆盖率低于 50%，建议创建更多针对性内容");
        if (clusters.Any(c => c.Intent == "informational" && !c.IsCovered))
            recommendations.Add("信息类查询未覆盖，这是 AI 引用的主要来源");
        if (clusters.Any(c => c.Intent == "comparison" && !c.IsCovered))
            recommendations.Add("对比类查询未覆盖，建议创建对比文章");

        return new QueryFanoutResult
        {
            MainQuery = mainQuery,
            TotalSubQueries = totalSubQueries,
            CoverageScore = Math.Round(coverageScore, 1),
            PotentialCitationBoost = potentialBoost,
            Clusters = clusters,
            UncoveredQueries = uncovered,
            Recommendations = recommendations
        };
    }

    private List<SubQueryCluster> GenerateSubQueryClusters(string mainQuery, string industry, string language)
    {
        var clusters = new List<SubQueryCluster>();

        // What 类查询
        clusters.Add(new SubQueryCluster
        {
            ClusterName = "定义与概念",
            Intent = "informational",
            ImportanceScore = 95,
            IsCovered = false,
            Queries = new List<SubQuery>
            {
                new() { Query = $"什么是{mainQuery}", Type = "what", SearchVolume = 1000, Difficulty = 30, IsCovered = false, SuggestedContentType = "定义文章" },
                new() { Query = $"{mainQuery}是什么意思", Type = "what", SearchVolume = 800, Difficulty = 25, IsCovered = false, SuggestedContentType = "解释文章" },
                new() { Query = $"{mainQuery}的定义", Type = "what", SearchVolume = 500, Difficulty = 20, IsCovered = false, SuggestedContentType = "术语解释" }
            }
        });

        // How 类查询
        clusters.Add(new SubQueryCluster
        {
            ClusterName = "操作与方法",
            Intent = "informational",
            ImportanceScore = 90,
            IsCovered = false,
            Queries = new List<SubQuery>
            {
                new() { Query = $"如何{mainQuery}", Type = "how", SearchVolume = 1200, Difficulty = 40, IsCovered = false, SuggestedContentType = "教程" },
                new() { Query = $"{mainQuery}怎么做", Type = "how", SearchVolume = 900, Difficulty = 35, IsCovered = false, SuggestedContentType = "步骤指南" },
                new() { Query = $"{mainQuery}的方法", Type = "how", SearchVolume = 600, Difficulty = 30, IsCovered = false, SuggestedContentType = "方法论" }
            }
        });

        // Why 类查询
        clusters.Add(new SubQueryCluster
        {
            ClusterName = "原因与解释",
            Intent = "informational",
            ImportanceScore = 85,
            IsCovered = false,
            Queries = new List<SubQuery>
            {
                new() { Query = $"为什么要{mainQuery}", Type = "why", SearchVolume = 700, Difficulty = 35, IsCovered = false, SuggestedContentType = "分析文章" },
                new() { Query = $"{mainQuery}的原因", Type = "why", SearchVolume = 500, Difficulty = 30, IsCovered = false, SuggestedContentType = "原因分析" }
            }
        });

        // Comparison 类查询
        clusters.Add(new SubQueryCluster
        {
            ClusterName = "对比与选择",
            Intent = "commercial",
            ImportanceScore = 80,
            IsCovered = false,
            Queries = new List<SubQuery>
            {
                new() { Query = $"{mainQuery}对比", Type = "comparison", SearchVolume = 600, Difficulty = 45, IsCovered = false, SuggestedContentType = "对比文章" },
                new() { Query = $"{mainQuery}哪个好", Type = "comparison", SearchVolume = 800, Difficulty = 40, IsCovered = false, SuggestedContentType = "评测" },
                new() { Query = $"{mainQuery}推荐", Type = "list", SearchVolume = 1000, Difficulty = 50, IsCovered = false, SuggestedContentType = "推荐列表" }
            }
        });

        // List 类查询
        clusters.Add(new SubQueryCluster
        {
            ClusterName = "列表与推荐",
            Intent = "commercial",
            ImportanceScore = 75,
            IsCovered = false,
            Queries = new List<SubQuery>
            {
                new() { Query = $"最好的{mainQuery}", Type = "list", SearchVolume = 900, Difficulty = 55, IsCovered = false, SuggestedContentType = "Top 10 列表" },
                new() { Query = $"{mainQuery}排行榜", Type = "list", SearchVolume = 700, Difficulty = 50, IsCovered = false, SuggestedContentType = "排行榜" }
            }
        });

        return clusters;
    }

    #endregion

    #region 7.10 Answer Capsules 检测器

    public AnswerCapsuleResult DetectAnswerCapsules(AnswerCapsuleRequest request)
    {
        var content = request.Content;
        var language = request.Language;

        // 按段落分割
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var capsules = new List<DetectedCapsule>();
        var opportunities = new List<CapsuleOpportunity>();

        var position = 0;
        var totalChars = content.Length;

        foreach (var (para, index) in paragraphs.Select((p, i) => (p, i)))
        {
            var charCount = para.Length;
            var positionPercent = (position * 100.0 / totalChars);
            var positionLabel = positionPercent < 30 ? "first_30" : positionPercent > 70 ? "last_30" : "middle";

            var isOptimal = charCount >= 120 && charCount <= 150;
            var isSelfContained = IsSelfContainedParagraph(para, language);
            var hasFactual = HasFactualContent(para, language);

            var score = 0.0;
            if (isOptimal) score += 40;
            else if (charCount >= 100 && charCount <= 180) score += 25;
            if (isSelfContained) score += 30;
            if (hasFactual) score += 30;

            capsules.Add(new DetectedCapsule
            {
                Index = index,
                Text = para.Length > 200 ? para.Substring(0, 200) + "..." : para,
                CharacterCount = charCount,
                IsOptimalLength = isOptimal,
                IsSelfContained = isSelfContained,
                HasFactualContent = hasFactual,
                Score = score,
                Position = positionLabel
            });

            // 识别优化机会
            if (!isOptimal && charCount > 180)
            {
                opportunities.Add(new CapsuleOpportunity
                {
                    OriginalText = para.Length > 100 ? para.Substring(0, 100) + "..." : para,
                    SuggestedCapsule = "将此段落拆分为 120-150 字符的独立答案",
                    Reason = $"当前 {charCount} 字符，超过最优长度",
                    PotentialCharCount = 135
                });
            }
            else if (!isOptimal && charCount < 80 && isSelfContained)
            {
                opportunities.Add(new CapsuleOpportunity
                {
                    OriginalText = para,
                    SuggestedCapsule = "扩展此段落，添加更多细节达到 120-150 字符",
                    Reason = $"当前 {charCount} 字符，低于最优长度",
                    PotentialCharCount = 135
                });
            }

            position += para.Length;
        }

        var optimalCount = capsules.Count(c => c.IsOptimalLength);
        var overallScore = capsules.Count > 0 ? capsules.Average(c => c.Score) : 0;

        // 72.4% 被引用页面使用 Answer Capsules
        var citationPotential = optimalCount >= 3 ? 72.4 : optimalCount >= 1 ? 50 : 20;

        var recommendations = new List<string>();
        if (optimalCount == 0)
            recommendations.Add("没有检测到最优长度（120-150字符）的答案胶囊，建议重构内容");
        if (capsules.Count(c => c.Position == "first_30" && c.Score >= 70) == 0)
            recommendations.Add("前 30% 内容缺少高质量答案胶囊，AI 引用 44% 来自前 30%");
        if (capsules.Count(c => !c.IsSelfContained) > capsules.Count / 2)
            recommendations.Add("多数段落不是自包含的，建议每段都能独立回答一个问题");

        return new AnswerCapsuleResult
        {
            OverallScore = Math.Round(overallScore, 1),
            Grade = GetGrade(overallScore),
            TotalCapsules = capsules.Count,
            OptimalCapsules = optimalCount,
            CitationPotential = citationPotential,
            Capsules = capsules,
            Opportunities = opportunities.Take(5).ToList(),
            Recommendations = recommendations
        };
    }

    private bool IsSelfContainedParagraph(string para, string language)
    {
        // 检查是否有完整的主谓结构
        if (language == "zh")
        {
            return para.Contains("是") || para.Contains("为") || para.Contains("指") || 
                   para.Contains("包括") || para.Contains("可以") || para.Contains("需要");
        }
        return para.Contains(" is ") || para.Contains(" are ") || para.Contains(" means ") ||
               para.Contains(" refers to ") || para.Contains(" includes ");
    }

    private bool HasFactualContent(string para, string language)
    {
        // 检查是否包含数字、百分比、日期等事实性内容
        return Regex.IsMatch(para, @"\d+%|\d{4}年|\d+\s*(万|亿|million|billion)|\d+\.\d+");
    }

    #endregion

    #region 7.11 Google 排名-AI 引用相关性

    public RankingCorrelationResult AnalyzeRankingCorrelation(RankingCorrelationRequest request)
    {
        // 模拟数据 - 基于 Lily Ray 2026 研究: -26.7% 流量 → -22.5% 引用
        var keywordAnalysis = request.Keywords.Select((kw, i) => new KeywordCorrelation
        {
            Keyword = kw,
            GoogleRank = 5 + i * 2,
            RankChange = (i % 3 == 0) ? -2 : (i % 3 == 1) ? 1 : 0,
            AICitationRate = 35 - i * 3,
            CitationChange = (i % 3 == 0) ? -5.5 : (i % 3 == 1) ? 2.3 : -1.2,
            Correlation = 0.84 - i * 0.05
        }).ToList();

        var avgCorrelation = keywordAnalysis.Count > 0 ? keywordAnalysis.Average(k => k.Correlation) : 0;
        var correlationStrength = avgCorrelation >= 0.7 ? "strong" : avgCorrelation >= 0.4 ? "moderate" : "weak";

        var trendAnalysis = new TrendAnalysis
        {
            OverallTrafficChange = -26.7,
            OverallCitationChange = -22.5,
            Trend = "declining",
            HistoricalData = Enumerable.Range(0, 30).Select(i => new DataPoint
            {
                Date = DateTime.Now.AddDays(-29 + i).ToString("yyyy-MM-dd"),
                TrafficIndex = 100 - i * 0.9 + Random.Shared.NextDouble() * 5,
                CitationIndex = 100 - i * 0.75 + Random.Shared.NextDouble() * 5
            }).ToList()
        };

        var insights = new List<string>
        {
            $"Google 排名与 AI 引用相关性系数: {avgCorrelation:F2} ({correlationStrength})",
            "流量下降 26.7% 对应引用下降 22.5%，相关性显著",
            "ChatGPT 对 Google 排名依赖度最高，Perplexity 最低"
        };

        var recommendations = new List<string>
        {
            "保持 Google 排名稳定是 AI 可见度的基础",
            "同时投资 Perplexity 等低依赖平台以分散风险",
            "监测排名变化，及时调整内容策略"
        };

        return new RankingCorrelationResult
        {
            Domain = request.Domain,
            CorrelationCoefficient = Math.Round(avgCorrelation, 2),
            CorrelationStrength = correlationStrength,
            KeywordAnalysis = keywordAnalysis,
            TrendAnalysis = trendAnalysis,
            Insights = insights,
            Recommendations = recommendations
        };
    }

    #endregion

    #region 7.12 平台独立性评估

    private static readonly Dictionary<string, (double dependency, string source)> PlatformDependencies = new()
    {
        ["chatgpt"] = (0.85, "主要依赖 Google 索引和权威来源"),
        ["perplexity"] = (0.35, "实时搜索，多来源聚合"),
        ["gemini"] = (0.75, "Google 生态系统整合"),
        ["claude"] = (0.60, "训练数据为主，实时搜索为辅"),
        ["grok"] = (0.40, "X 平台数据 + 实时搜索"),
        ["copilot"] = (0.70, "Bing 索引 + Microsoft 生态")
    };

    public PlatformIndependenceResult EvaluatePlatformIndependence(PlatformIndependenceRequest request)
    {
        var platforms = request.TargetPlatforms.Count > 0 
            ? request.TargetPlatforms 
            : PlatformDependencies.Keys.ToList();

        var platformAnalysis = platforms.Select(p =>
        {
            var (dep, source) = PlatformDependencies.TryGetValue(p.ToLower(), out var val) 
                ? val 
                : (0.5, "未知来源");

            return new PlatformDependency
            {
                Platform = p,
                DisplayName = GetPlatformDisplayName(p),
                GoogleDependency = dep * 100,
                DependencyLevel = dep >= 0.7 ? "high" : dep >= 0.4 ? "medium" : "low",
                CitationStability = (1 - dep) * 100,
                DataSource = source,
                IndependentFactors = GetIndependentFactors(p)
            };
        }).ToList();

        var avgDependency = platformAnalysis.Average(p => p.GoogleDependency);
        var independenceScore = 100 - avgDependency;
        var riskLevel = avgDependency >= 70 ? "high" : avgDependency >= 50 ? "medium" : "low";

        var strategies = new List<string>
        {
            "增加 Perplexity 和 Grok 的内容布局（低 Google 依赖）",
            "在 Reddit、Medium 等第三方平台建立存在",
            "创建 YouTube 视频内容（Gemini 偏好）",
            "保持 Google 排名的同时分散平台风险"
        };

        var recommendations = new List<string>();
        if (avgDependency > 60)
            recommendations.Add("当前平台组合对 Google 依赖度较高，建议增加低依赖平台");
        recommendations.Add("Perplexity 是最独立的平台，建议优先投入");
        recommendations.Add("定期监测各平台引用变化，及时调整策略");

        return new PlatformIndependenceResult
        {
            Domain = request.Domain,
            OverallIndependenceScore = Math.Round(independenceScore, 1),
            PlatformAnalysis = platformAnalysis,
            RiskLevel = riskLevel,
            DiversificationStrategies = strategies,
            Recommendations = recommendations
        };
    }

    private List<string> GetIndependentFactors(string platform)
    {
        return platform.ToLower() switch
        {
            "perplexity" => new() { "实时搜索", "多来源聚合", "用户生成内容" },
            "grok" => new() { "X 平台数据", "实时信息", "社交信号" },
            "claude" => new() { "训练数据质量", "专业文档" },
            "gemini" => new() { "YouTube 视频", "Google 生态" },
            "chatgpt" => new() { "权威来源", "维基百科" },
            "copilot" => new() { "LinkedIn", "Microsoft 文档" },
            _ => new() { "未知" }
        };
    }

    #endregion

    #region 7.13 多语言 AI 可见度

    private static readonly Dictionary<string, (string name, double potential)> SupportedLanguages = new()
    {
        ["en"] = ("English", 100),
        ["zh"] = ("中文", 85),
        ["es"] = ("Español", 70),
        ["de"] = ("Deutsch", 65),
        ["fr"] = ("Français", 60),
        ["ja"] = ("日本語", 55),
        ["ko"] = ("한국어", 50),
        ["pt"] = ("Português", 45),
        ["it"] = ("Italiano", 40),
        ["nl"] = ("Nederlands", 35),
        ["ar"] = ("العربية", 30),
        ["hi"] = ("हिन्दी", 25)
    };

    public MultiLanguageResult AnalyzeMultiLanguage(MultiLanguageRequest request)
    {
        var targetLanguages = request.TargetLanguages.Count > 0 
            ? request.TargetLanguages 
            : SupportedLanguages.Keys.ToList();

        var languageResults = targetLanguages.Select(lang =>
        {
            var (name, potential) = SupportedLanguages.TryGetValue(lang, out var val) 
                ? val 
                : (lang, 20.0);

            var hasContent = lang == "en" || lang == "zh"; // 模拟
            var visibilityScore = hasContent ? potential * 0.8 : potential * 0.2;

            return new LanguageVisibility
            {
                LanguageCode = lang,
                LanguageName = name,
                VisibilityScore = visibilityScore,
                MarketPotential = potential,
                EstimatedSearchVolume = (int)(potential * 10000),
                TopPlatforms = GetTopPlatformsForLanguage(lang),
                ContentStatus = hasContent ? "available" : "missing"
            };
        }).ToList();

        var opportunities = languageResults
            .Where(l => l.ContentStatus == "missing" && l.MarketPotential >= 40)
            .Select(l => new LanguageOpportunity
            {
                LanguageCode = l.LanguageCode,
                LanguageName = l.LanguageName,
                OpportunityScore = l.MarketPotential,
                Reason = $"市场潜力 {l.MarketPotential}%，但缺少本地化内容",
                SuggestedActions = new() { "创建本地化内容", "与本地 KOL 合作", "投放本地平台" }
            })
            .OrderByDescending(o => o.OpportunityScore)
            .ToList();

        var globalRecommendations = new List<string>
        {
            "英语市场是 AI 可见度的基础，确保英语内容质量",
            "中文市场增长迅速，建议优先投入",
            "西班牙语和德语是欧美市场的重要补充"
        };

        return new MultiLanguageResult
        {
            Brand = request.Brand,
            LanguagesAnalyzed = languageResults.Count,
            LanguageResults = languageResults,
            Opportunities = opportunities,
            GlobalRecommendations = globalRecommendations
        };
    }

    private List<string> GetTopPlatformsForLanguage(string lang)
    {
        return lang switch
        {
            "en" => new() { "ChatGPT", "Perplexity", "Gemini" },
            "zh" => new() { "文心一言", "通义千问", "Kimi" },
            "ja" => new() { "ChatGPT", "Gemini", "Claude" },
            "ko" => new() { "ChatGPT", "Gemini", "Naver" },
            _ => new() { "ChatGPT", "Gemini" }
        };
    }

    #endregion

    #region 4.39-4.41 平台依赖度监测

    public PlatformDependencyMonitorResult MonitorPlatformDependency(PlatformDependencyMonitorRequest request)
    {
        var exposures = new List<PlatformExposure>
        {
            new() { Platform = "chatgpt", DisplayName = "ChatGPT", ExposurePercent = 45, TrendChange = 5, IsOverExposed = false, RiskLevel = "medium" },
            new() { Platform = "perplexity", DisplayName = "Perplexity", ExposurePercent = 25, TrendChange = 10, IsOverExposed = false, RiskLevel = "low" },
            new() { Platform = "gemini", DisplayName = "Gemini", ExposurePercent = 15, TrendChange = -2, IsOverExposed = false, RiskLevel = "low" },
            new() { Platform = "claude", DisplayName = "Claude", ExposurePercent = 10, TrendChange = 3, IsOverExposed = false, RiskLevel = "low" },
            new() { Platform = "grok", DisplayName = "Grok", ExposurePercent = 5, TrendChange = 8, IsOverExposed = false, RiskLevel = "low" }
        };

        var maxExposure = exposures.Max(e => e.ExposurePercent);
        var hasWarning = maxExposure > 50;
        var warningMessage = hasWarning 
            ? $"警告：{exposures.First(e => e.ExposurePercent == maxExposure).DisplayName} 占比超过 50%，存在过度依赖风险"
            : "";

        var alerts = new List<DependencyAlert>();
        if (hasWarning)
        {
            alerts.Add(new DependencyAlert
            {
                AlertType = "over_exposure",
                Platform = exposures.First(e => e.ExposurePercent == maxExposure).Platform,
                Message = warningMessage,
                Severity = "warning",
                SuggestedAction = "增加其他平台的内容布局"
            });
        }

        var diversificationScore = 100 - (maxExposure - 20); // 理想分布是每个平台 20%

        var strategy = new DiversificationStrategy
        {
            CurrentStatus = hasWarning ? "需要分散" : "分布健康",
            ImmediateActions = new() { "增加 Perplexity 内容", "优化 Reddit 存在" },
            MediumTermActions = new() { "建立 YouTube 频道", "创建 Medium 博客" },
            LongTermActions = new() { "多语言内容扩展", "建立品牌社区" },
            TargetDistribution = new() { ["chatgpt"] = 30, ["perplexity"] = 25, ["gemini"] = 20, ["claude"] = 15, ["grok"] = 10 }
        };

        return new PlatformDependencyMonitorResult
        {
            Brand = request.Brand,
            DiversificationScore = Math.Round(diversificationScore, 1),
            HasWarning = hasWarning,
            WarningMessage = warningMessage,
            Exposures = exposures,
            Alerts = alerts,
            Strategy = strategy
        };
    }

    #endregion

    #region 5.21 跨平台调度

    private static readonly Dictionary<string, (string bestDay, string bestTime, double engagement)> PlatformSchedules = new()
    {
        ["linkedin"] = ("Tuesday", "10:00", 0.85),
        ["twitter"] = ("Wednesday", "12:00", 0.78),
        ["instagram"] = ("Thursday", "18:00", 0.82),
        ["tiktok"] = ("Friday", "20:00", 0.90),
        ["youtube"] = ("Saturday", "14:00", 0.88),
        ["reddit"] = ("Monday", "09:00", 0.75),
        ["medium"] = ("Tuesday", "08:00", 0.70)
    };

    public CrossPlatformScheduleResult GenerateSchedule(CrossPlatformScheduleRequest request)
    {
        var startDate = request.PreferredStartDate ?? DateTime.Now.AddDays(1);
        var schedule = new List<ScheduledPost>();

        foreach (var platform in request.TargetPlatforms)
        {
            var (bestDay, bestTime, engagement) = PlatformSchedules.TryGetValue(platform.ToLower(), out var val)
                ? val
                : ("Monday", "10:00", 0.5);

            var scheduledTime = GetNextOccurrence(startDate, bestDay, bestTime);

            schedule.Add(new ScheduledPost
            {
                Platform = platform,
                DisplayName = GetPlatformDisplayName(platform),
                ScheduledTime = scheduledTime,
                DayOfWeek = scheduledTime.DayOfWeek.ToString(),
                TimeSlot = bestTime,
                ExpectedEngagement = engagement * 100,
                ContentVariant = GetContentVariant(platform, request.ContentType),
                Hashtags = GenerateHashtags(platform, request.ContentTitle),
                Status = "pending"
            });
        }

        var orderedSchedule = schedule.OrderBy(s => s.ScheduledTime).ToList();

        var rationale = new List<string>
        {
            "发布顺序基于各平台最佳发布时间优化",
            "LinkedIn 和 Medium 适合工作日早间发布",
            "TikTok 和 Instagram 适合晚间和周末发布",
            "Reddit 适合周一早间，用户活跃度高"
        };

        var warnings = new List<string>();
        if (request.TargetPlatforms.Count > 5)
            warnings.Add("平台数量较多，建议分批发布以确保质量");

        return new CrossPlatformScheduleResult
        {
            ContentId = request.ContentId,
            ContentTitle = request.ContentTitle,
            Schedule = orderedSchedule,
            OptimalSequence = string.Join(" → ", orderedSchedule.Select(s => s.DisplayName)),
            Rationale = rationale,
            Warnings = warnings
        };
    }

    private DateTime GetNextOccurrence(DateTime startDate, string dayOfWeek, string time)
    {
        var targetDay = Enum.Parse<DayOfWeek>(dayOfWeek);
        var timeParts = time.Split(':');
        var hour = int.Parse(timeParts[0]);
        var minute = int.Parse(timeParts[1]);

        var daysUntilTarget = ((int)targetDay - (int)startDate.DayOfWeek + 7) % 7;
        if (daysUntilTarget == 0) daysUntilTarget = 7;

        return startDate.Date.AddDays(daysUntilTarget).AddHours(hour).AddMinutes(minute);
    }

    private string GetContentVariant(string platform, string contentType)
    {
        return platform.ToLower() switch
        {
            "linkedin" => "专业版 - 800-1000字",
            "twitter" => "精简版 - 280字以内",
            "instagram" => "视觉版 - 图文卡片",
            "tiktok" => "短视频版 - 60秒",
            "youtube" => "长视频版 - 10-15分钟",
            "reddit" => "社区版 - 价值优先",
            "medium" => "深度版 - 2000+字",
            _ => "标准版"
        };
    }

    private List<string> GenerateHashtags(string platform, string title)
    {
        var baseHashtags = new List<string> { "#AI", "#GEO", "#ContentMarketing" };
        return platform.ToLower() switch
        {
            "instagram" => baseHashtags.Concat(new[] { "#DigitalMarketing", "#SEO", "#ContentStrategy" }).ToList(),
            "tiktok" => new() { "#AI", "#Marketing", "#Tips", "#Viral" },
            "linkedin" => new() { "#AI", "#B2B", "#Marketing", "#Strategy" },
            _ => baseHashtags
        };
    }

    #endregion

    #region 5.29-5.30 最佳实践/报告

    public BestPracticeResult ExtractBestPractices(BestPracticeRequest request)
    {
        // 模拟高效内容分析
        var topContent = new List<HighPerformingContent>
        {
            new() { ContentId = "c1", Title = "AI 可见度完整指南", Platform = "linkedin", PerformanceScore = 95, Views = 15000, Engagements = 1200, Citations = 45, SuccessFactors = new() { "深度内容", "数据支撑", "清晰结构" } },
            new() { ContentId = "c2", Title = "GEO 优化 10 步法", Platform = "medium", PerformanceScore = 92, Views = 12000, Engagements = 800, Citations = 38, SuccessFactors = new() { "步骤清晰", "案例丰富", "可操作性强" } },
            new() { ContentId = "c3", Title = "ChatGPT 引用秘籍", Platform = "reddit", PerformanceScore = 88, Views = 8000, Engagements = 600, Citations = 25, SuccessFactors = new() { "社区价值", "真实经验", "互动性强" } }
        };

        var patterns = new List<ExtractedPattern>
        {
            new() { PatternName = "答案前置", Description = "在前 100 字内提供核心答案", ImpactScore = 95, OccurrenceCount = 8, Examples = new() { "直接回答问题", "关键数据前置" }, HowToApply = "每篇文章开头用 1-2 句话直接回答核心问题" },
            new() { PatternName = "数据支撑", Description = "使用统计数据增强可信度", ImpactScore = 90, OccurrenceCount = 7, Examples = new() { "72.4% 被引用", "+161% 引用率" }, HowToApply = "每个关键论点配备 1-2 个数据支撑" },
            new() { PatternName = "结构化列表", Description = "使用编号列表提高可读性", ImpactScore = 85, OccurrenceCount = 6, Examples = new() { "Top 10 列表", "步骤指南" }, HowToApply = "将复杂内容拆分为 5-10 个要点" },
            new() { PatternName = "专家引用", Description = "引用权威来源增强 E-E-A-T", ImpactScore = 80, OccurrenceCount = 5, Examples = new() { "研究表明", "专家指出" }, HowToApply = "每篇文章至少引用 2-3 个权威来源" }
        };

        var keyInsights = new List<string>
        {
            "高效内容平均长度 2,500-4,000 词",
            "前 30% 内容质量决定 44% 的引用",
            "120-150 字符的答案胶囊被引用率最高",
            "结构化内容比纯文本引用率高 70%"
        };

        var recommendations = new List<string>
        {
            "优先创建深度指南类内容（67% 引用率）",
            "每篇内容至少包含 3 个答案胶囊",
            "使用 H2/H3 标题结构（3.2x 影响系数）",
            "添加 FAQ Schema 提升 AI 可见度"
        };

        return new BestPracticeResult
        {
            Brand = request.Brand,
            ContentAnalyzed = 50,
            TopContent = topContent,
            Patterns = patterns,
            KeyInsights = keyInsights,
            ActionableRecommendations = recommendations
        };
    }

    public AutomatedReportResult GenerateAutomatedReport(AutomatedReportRequest request)
    {
        var now = DateTime.Now;
        var startDate = request.StartDate ?? (request.ReportType == "weekly" ? now.AddDays(-7) : now.AddMonths(-1));
        var endDate = request.EndDate ?? now;

        var summary = new ExecutiveSummary
        {
            OverallScore = 78.5,
            ScoreChange = "+5.2",
            Trend = "improving",
            Highlight = "AI 引用率提升 15%，Perplexity 表现突出",
            KeyPoints = new() { "总引用数增长 23%", "ChatGPT 引用稳定", "新增 Reddit 渠道效果显著" }
        };

        var metrics = new PerformanceMetrics
        {
            TotalViews = 125000,
            TotalEngagements = 8500,
            TotalCitations = 156,
            EngagementRate = 6.8,
            CitationRate = 0.12,
            ROI = 3.5,
            MetricChanges = new() { ["views"] = 15, ["engagements"] = 22, ["citations"] = 23 }
        };

        var platformBreakdown = new List<PlatformPerformance>
        {
            new() { Platform = "chatgpt", DisplayName = "ChatGPT", Score = 82, Change = 3, ContentCount = 25, TotalEngagements = 3200, Status = "stable" },
            new() { Platform = "perplexity", DisplayName = "Perplexity", Score = 88, Change = 12, ContentCount = 18, TotalEngagements = 2800, Status = "growing" },
            new() { Platform = "gemini", DisplayName = "Gemini", Score = 75, Change = -2, ContentCount = 15, TotalEngagements = 1500, Status = "stable" },
            new() { Platform = "reddit", DisplayName = "Reddit", Score = 70, Change = 25, ContentCount = 10, TotalEngagements = 1000, Status = "growing" }
        };

        var topContent = new List<ContentPerformance>
        {
            new() { ContentId = "c1", Title = "AI 可见度完整指南", Platform = "LinkedIn", PublishedAt = now.AddDays(-5), Views = 15000, Engagements = 1200, EngagementRate = 8.0 },
            new() { ContentId = "c2", Title = "GEO 优化实战", Platform = "Medium", PublishedAt = now.AddDays(-10), Views = 12000, Engagements = 900, EngagementRate = 7.5 }
        };

        return new AutomatedReportResult
        {
            Brand = request.Brand,
            ReportType = request.ReportType,
            Period = $"{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}",
            GeneratedAt = now,
            Summary = summary,
            Metrics = metrics,
            PlatformBreakdown = platformBreakdown,
            TopContent = topContent,
            KeyWins = new() { "Perplexity 引用率提升 35%", "Reddit 账号 Karma 达到 500+", "新增 3 篇高引用内容" },
            AreasForImprovement = new() { "Gemini 引用率下降，需优化视频内容", "Claude 覆盖不足" },
            NextPeriodGoals = new() { "ChatGPT 引用率提升 10%", "新增 YouTube 频道", "多语言内容扩展" }
        };
    }

    #endregion

    #region 辅助方法

    private string GetPlatformDisplayName(string platform)
    {
        return platform.ToLower() switch
        {
            "chatgpt" => "ChatGPT",
            "perplexity" => "Perplexity",
            "gemini" => "Google Gemini",
            "claude" => "Claude",
            "grok" => "Grok",
            "copilot" => "Microsoft Copilot",
            "linkedin" => "LinkedIn",
            "twitter" => "X/Twitter",
            "instagram" => "Instagram",
            "tiktok" => "TikTok",
            "youtube" => "YouTube",
            "reddit" => "Reddit",
            "medium" => "Medium",
            _ => platform
        };
    }

    private string GetGrade(double score)
    {
        return score >= 90 ? "A+" : score >= 80 ? "A" : score >= 70 ? "B" : score >= 60 ? "C" : score >= 50 ? "D" : "F";
    }

    #endregion
}
