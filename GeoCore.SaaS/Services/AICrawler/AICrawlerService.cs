using System.Text;
using System.Text.RegularExpressions;
using GeoCore.Data.Entities;

namespace GeoCore.SaaS.Services.AICrawler;

/// <summary>
/// AI 爬虫服务 (4.48-4.53, 4.55-4.58)
/// 数据来源：从 ConfigCacheService 缓存读取（数据库管理）
/// </summary>
public class AICrawlerService
{
    private readonly ILogger<AICrawlerService> _logger;
    private readonly ConfigCacheService _configCache;

    public AICrawlerService(
        ILogger<AICrawlerService> logger,
        ConfigCacheService configCache)
    {
        _logger = logger;
        _configCache = configCache;
    }

    /// <summary>
    /// 从缓存获取 AI 爬虫列表
    /// </summary>
    private List<CrawlerStatus> GetKnownAICrawlersFromCache()
    {
        var entities = _configCache.GetAllAICrawlers();
        return entities.Select(e => new CrawlerStatus
        {
            Name = e.Name,
            UserAgent = e.UserAgentPattern,
            Company = e.Company,
            Importance = e.Importance,
            Purpose = e.Purpose ?? ""
        }).ToList();
    }

    /// <summary>
    /// 从缓存获取 LLM Referrer 列表
    /// </summary>
    private List<LLMReferrer> GetKnownLLMReferrersFromCache()
    {
        var entities = _configCache.GetAllLLMReferrers();
        return entities.Select(e => new LLMReferrer
        {
            Name = e.PlatformName,
            Domain = e.ReferrerPattern,
            Pattern = Regex.Escape(e.ReferrerPattern).Replace("\\.", "\\.")
        }).ToList();
    }

    #region 4.48 AI 爬虫配置审计

    /// <summary>
    /// 审计 AI 爬虫配置
    /// </summary>
    public AICrawlerAuditResult AuditCrawlerConfig(AICrawlerAuditRequest request)
    {
        var result = new AICrawlerAuditResult
        {
            SiteUrl = request.SiteUrl,
            AuditTime = DateTime.UtcNow
        };

        var robotsTxt = request.RobotsTxt ?? "";

        // 从缓存获取 AI 爬虫列表
        var knownAICrawlers = GetKnownAICrawlersFromCache();

        // 分析每个爬虫的状态
        foreach (var crawler in knownAICrawlers)
        {
            var status = new CrawlerStatus
            {
                Name = crawler.Name,
                UserAgent = crawler.UserAgent,
                Company = crawler.Company,
                Importance = crawler.Importance,
                Purpose = crawler.Purpose
            };

            // 检查 robots.txt 中的配置
            status.Status = CheckCrawlerStatus(robotsTxt, crawler.UserAgent);
            status.RecommendedAction = GetRecommendedAction(status);

            result.CrawlerStatuses.Add(status);
        }

        // 统计
        result.AllowedCount = result.CrawlerStatuses.Count(s => s.Status == "allowed");
        result.BlockedCount = result.CrawlerStatuses.Count(s => s.Status == "blocked");
        result.UnconfiguredCount = result.CrawlerStatuses.Count(s => s.Status == "unconfigured");

        // 计算评分
        result.OverallScore = CalculateAuditScore(result);
        result.Grade = GetGrade(result.OverallScore);

        // 生成建议
        result.Recommendations = GenerateAuditRecommendations(result);

        // 生成推荐的 robots.txt
        result.RecommendedRobotsTxt = GenerateRecommendedRobotsTxt();

        return result;
    }

    private string CheckCrawlerStatus(string robotsTxt, string userAgent)
    {
        if (string.IsNullOrEmpty(robotsTxt))
            return "unconfigured";

        // 检查是否有针对该爬虫的规则
        var pattern = $@"User-agent:\s*{Regex.Escape(userAgent)}[\s\S]*?(?=User-agent:|$)";
        var match = Regex.Match(robotsTxt, pattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            // 检查通配符规则
            var wildcardPattern = @"User-agent:\s*\*[\s\S]*?(?=User-agent:|$)";
            var wildcardMatch = Regex.Match(robotsTxt, wildcardPattern, RegexOptions.IgnoreCase);
            if (wildcardMatch.Success)
            {
                return wildcardMatch.Value.Contains("Disallow: /") ? "blocked" : "allowed";
            }
            return "unconfigured";
        }

        var rules = match.Value;
        if (Regex.IsMatch(rules, @"Disallow:\s*/\s*$", RegexOptions.Multiline))
            return "blocked";
        if (Regex.IsMatch(rules, @"Allow:\s*/", RegexOptions.IgnoreCase))
            return "allowed";

        return "unconfigured";
    }

    private string GetRecommendedAction(CrawlerStatus status)
    {
        if (status.Status == "allowed")
            return "保持当前配置";

        return status.Importance switch
        {
            "high" => "强烈建议允许此爬虫以获得 AI 搜索可见性",
            "medium" => "建议允许此爬虫以扩大 AI 覆盖范围",
            _ => "可选择性允许"
        };
    }

    private double CalculateAuditScore(AICrawlerAuditResult result)
    {
        double score = 0;
        double maxScore = 0;

        foreach (var status in result.CrawlerStatuses)
        {
            double weight = status.Importance switch
            {
                "high" => 10,
                "medium" => 5,
                _ => 2
            };

            maxScore += weight;
            if (status.Status == "allowed")
                score += weight;
        }

        return maxScore > 0 ? (score / maxScore) * 100 : 0;
    }

    private string GetGrade(double score)
    {
        return score switch
        {
            >= 90 => "A",
            >= 75 => "B",
            >= 60 => "C",
            >= 40 => "D",
            _ => "F"
        };
    }

    private List<AuditRecommendation> GenerateAuditRecommendations(AICrawlerAuditResult result)
    {
        var recommendations = new List<AuditRecommendation>();

        // 高优先级爬虫未配置
        var highPriorityBlocked = result.CrawlerStatuses
            .Where(s => s.Importance == "high" && s.Status != "allowed")
            .ToList();

        foreach (var crawler in highPriorityBlocked)
        {
            recommendations.Add(new AuditRecommendation
            {
                Priority = "high",
                Message = $"允许 {crawler.Name} ({crawler.Company}) 爬虫访问",
                Impact = $"将提升在 {crawler.Company} AI 产品中的可见性"
            });
        }

        if (result.UnconfiguredCount > 5)
        {
            recommendations.Add(new AuditRecommendation
            {
                Priority = "medium",
                Message = "建议明确配置所有 AI 爬虫规则",
                Impact = "避免未来爬虫行为变化带来的不确定性"
            });
        }

        return recommendations;
    }

    private string GenerateRecommendedRobotsTxt()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# AI Crawlers Configuration");
        sb.AppendLine("# Generated by GEO Analyzer");
        sb.AppendLine();

        // 从缓存获取 AI 爬虫列表
        var knownAICrawlers = GetKnownAICrawlersFromCache();

        foreach (var crawler in knownAICrawlers.Where(c => c.Importance == "high"))
        {
            sb.AppendLine($"# {crawler.Company} - {crawler.Purpose}");
            sb.AppendLine($"User-agent: {crawler.UserAgent}");
            sb.AppendLine("Allow: /");
            sb.AppendLine();
        }

        sb.AppendLine("# Medium Priority AI Crawlers");
        foreach (var crawler in knownAICrawlers.Where(c => c.Importance == "medium"))
        {
            sb.AppendLine($"User-agent: {crawler.UserAgent}");
            sb.AppendLine("Allow: /");
        }

        return sb.ToString();
    }

    #endregion

    #region 4.49 llms.txt 生成器

    /// <summary>
    /// 生成 llms.txt
    /// </summary>
    public LlmsTxtResult GenerateLlmsTxt(LlmsTxtRequest request)
    {
        var result = new LlmsTxtResult
        {
            PageCount = request.Pages.Count
        };

        var sb = new StringBuilder();

        // 标题
        sb.AppendLine($"# {request.SiteName}");
        sb.AppendLine();

        // 描述
        if (!string.IsNullOrEmpty(request.SiteDescription))
        {
            sb.AppendLine($"> {request.SiteDescription}");
            sb.AppendLine();
        }

        // 按优先级排序的页面
        var sortedPages = request.Pages.OrderByDescending(p => p.Priority).ToList();

        foreach (var page in sortedPages)
        {
            sb.AppendLine($"- [{page.Title}]({page.Url})");
            if (request.IncludeOptionalFields && !string.IsNullOrEmpty(page.Description))
            {
                sb.AppendLine($"  {page.Description}");
            }
        }

        result.Content = sb.ToString();

        // 部署说明
        result.DeploymentInstructions = new List<string>
        {
            $"1. 将生成的内容保存为 llms.txt",
            $"2. 上传到网站根目录: {request.SiteUrl}/llms.txt",
            "3. 确保文件可公开访问",
            "4. 在 robots.txt 中添加: Sitemap: /llms.txt"
        };

        // 验证
        result.Validation = ValidateLlmsTxt(result.Content, request);

        return result;
    }

    private LlmsTxtValidation ValidateLlmsTxt(string content, LlmsTxtRequest request)
    {
        var validation = new LlmsTxtValidation { IsValid = true };

        if (string.IsNullOrEmpty(request.SiteName))
        {
            validation.Errors.Add("缺少网站名称");
            validation.IsValid = false;
        }

        if (request.Pages.Count == 0)
        {
            validation.Errors.Add("没有页面内容");
            validation.IsValid = false;
        }

        if (request.Pages.Count > 100)
        {
            validation.Warnings.Add("页面数量较多，建议精选最重要的页面");
        }

        if (request.Pages.Any(p => string.IsNullOrEmpty(p.Title)))
        {
            validation.Warnings.Add("部分页面缺少标题");
        }

        return validation;
    }

    #endregion

    #region 4.50 GA4 AI 流量追踪

    /// <summary>
    /// 获取 GA4 AI 流量追踪配置
    /// </summary>
    public GA4AITrackingConfig GetGA4TrackingConfig()
    {
        // 从缓存获取数据
        var knownAICrawlers = GetKnownAICrawlersFromCache();
        var knownLLMReferrers = GetKnownLLMReferrersFromCache();

        return new GA4AITrackingConfig
        {
            LLMReferrers = knownLLMReferrers,
            AIBotUserAgents = knownAICrawlers.Select(c => c.UserAgent).ToList(),
            GTMCode = GenerateGTMCode(),
            CustomDimensions = new List<CustomDimension>
            {
                new CustomDimension { Name = "ai_referrer", Scope = "session", Description = "AI 来源平台" },
                new CustomDimension { Name = "ai_bot_visit", Scope = "event", Description = "AI 爬虫访问" },
                new CustomDimension { Name = "llm_source", Scope = "session", Description = "LLM 来源详情" }
            }
        };
    }

    private string GenerateGTMCode()
    {
        // 从缓存获取 LLM Referrer 列表
        var knownLLMReferrers = GetKnownLLMReferrersFromCache();
        var referrerPatterns = string.Join("|", knownLLMReferrers.Select(r => r.Pattern));

        return $@"
// GA4 AI Traffic Tracking
(function() {{
  var referrer = document.referrer;
  var aiReferrerPattern = /{referrerPatterns}/i;
  
  if (aiReferrerPattern.test(referrer)) {{
    gtag('set', 'user_properties', {{
      'ai_referrer': referrer.match(aiReferrerPattern)[0]
    }});
    gtag('event', 'ai_traffic', {{
      'event_category': 'AI',
      'event_label': referrer
    }});
  }}
}})();
";
    }

    #endregion

    #region 4.52 双平台优化策略

    /// <summary>
    /// 分析双平台优化
    /// 原理：AIO vs AI Mode 仅 13.7% 重叠
    /// </summary>
    public DualPlatformResult AnalyzeDualPlatform(DualPlatformRequest request)
    {
        var result = new DualPlatformResult();

        // AIO 优化分析
        result.AIOOptimization = AnalyzeForAIO(request.Content);

        // AI Mode 优化分析
        result.AIModeOptimization = AnalyzeForAIMode(request.Content);

        // 重叠度分析
        result.Overlap = AnalyzeOverlap(result.AIOOptimization, result.AIModeOptimization);

        // 综合建议
        result.Recommendations = GenerateDualPlatformRecommendations(result);

        return result;
    }

    private PlatformOptimization AnalyzeForAIO(string content)
    {
        var opt = new PlatformOptimization
        {
            Platform = "AI Overview (AIO)",
            CurrentScore = 0
        };

        // AIO 偏好：FAQ 格式、直接答案、结构化内容
        double score = 50;

        // 检查 FAQ 格式
        if (Regex.IsMatch(content, @"(Q:|问:|FAQ|常见问题)", RegexOptions.IgnoreCase))
        {
            score += 15;
            opt.Strengths.Add("包含 FAQ 格式内容");
        }
        else
        {
            opt.Improvements.Add("添加 FAQ 格式内容以提升 AIO 可见性");
        }

        // 检查直接答案
        if (Regex.IsMatch(content, @"^[^。.]{10,100}[。.]", RegexOptions.Multiline))
        {
            score += 10;
            opt.Strengths.Add("包含简洁直接的答案");
        }

        // 检查列表结构
        if (Regex.IsMatch(content, @"(\d+\.|[-•])\s+\w"))
        {
            score += 10;
            opt.Strengths.Add("使用列表结构");
        }

        opt.CurrentScore = Math.Min(100, score);
        opt.ContentTips = new List<string>
        {
            "使用问答格式",
            "提供简洁直接的答案",
            "使用编号列表",
            "添加 Schema 标记"
        };

        return opt;
    }

    private PlatformOptimization AnalyzeForAIMode(string content)
    {
        var opt = new PlatformOptimization
        {
            Platform = "AI Mode",
            CurrentScore = 0
        };

        double score = 50;

        // AI Mode 偏好：深度内容、专家引用、数据支持
        if (content.Length > 2000)
        {
            score += 15;
            opt.Strengths.Add("内容深度足够");
        }
        else
        {
            opt.Improvements.Add("增加内容深度和详细程度");
        }

        // 检查数据/统计
        if (Regex.IsMatch(content, @"\d+%|\d+\.\d+|统计|数据|研究"))
        {
            score += 10;
            opt.Strengths.Add("包含数据支持");
        }

        // 检查专家引用
        if (Regex.IsMatch(content, @"(专家|研究员|教授|博士|according to|根据)", RegexOptions.IgnoreCase))
        {
            score += 10;
            opt.Strengths.Add("包含专家引用");
        }

        opt.CurrentScore = Math.Min(100, score);
        opt.ContentTips = new List<string>
        {
            "提供深度分析",
            "引用专家观点",
            "包含数据和统计",
            "展示专业知识"
        };

        return opt;
    }

    private OverlapAnalysis AnalyzeOverlap(PlatformOptimization aio, PlatformOptimization aiMode)
    {
        return new OverlapAnalysis
        {
            OverlapPercentage = 13.7,
            CommonOptimizations = new List<string>
            {
                "高质量原创内容",
                "清晰的内容结构",
                "准确的事实信息"
            },
            AIOOnlyOptimizations = new List<string>
            {
                "FAQ 格式",
                "简洁直接的答案",
                "Schema 标记",
                "列表结构"
            },
            AIModeOnlyOptimizations = new List<string>
            {
                "深度长文内容",
                "专家引用和观点",
                "数据和研究支持",
                "详细的分析论证"
            }
        };
    }

    private List<string> GenerateDualPlatformRecommendations(DualPlatformResult result)
    {
        var recommendations = new List<string>
        {
            "AIO 和 AI Mode 仅有 13.7% 的优化重叠，需要针对性优化",
            "为 AIO 创建 FAQ 页面和简洁答案",
            "为 AI Mode 创建深度分析文章"
        };

        if (result.AIOOptimization.CurrentScore < 60)
            recommendations.Add("优先提升 AIO 优化：添加 FAQ 和结构化内容");

        if (result.AIModeOptimization.CurrentScore < 60)
            recommendations.Add("优先提升 AI Mode 优化：增加内容深度和专家引用");

        return recommendations;
    }

    #endregion

    #region 4.53 JS 渲染检测

    /// <summary>
    /// 检测 JS 渲染依赖
    /// </summary>
    public JSRenderingResult DetectJSRendering(JSRenderingRequest request)
    {
        var result = new JSRenderingResult
        {
            Url = request.Url
        };

        var html = request.HtmlContent ?? "";
        var issues = new List<JSRenderingIssue>();

        // 检测客户端渲染框架
        if (Regex.IsMatch(html, @"<div\s+id=[""'](?:root|app|__next)[""']>\s*</div>", RegexOptions.IgnoreCase))
        {
            issues.Add(new JSRenderingIssue
            {
                Type = "SPA Framework",
                Description = "检测到单页应用框架（React/Vue/Angular），内容可能依赖 JS 渲染",
                Severity = "high",
                Solution = "使用 SSR（服务端渲染）或 SSG（静态生成）"
            });
        }

        // 检测动态内容加载
        if (Regex.IsMatch(html, @"fetch\(|axios\.|\.ajax\(|XMLHttpRequest", RegexOptions.IgnoreCase))
        {
            issues.Add(new JSRenderingIssue
            {
                Type = "Dynamic Content",
                Description = "检测到动态内容加载，AI 爬虫可能无法获取",
                Severity = "medium",
                Solution = "将关键内容预渲染到 HTML 中"
            });
        }

        // 检测延迟加载
        if (Regex.IsMatch(html, @"loading=[""']lazy[""']|data-src=", RegexOptions.IgnoreCase))
        {
            issues.Add(new JSRenderingIssue
            {
                Type = "Lazy Loading",
                Description = "检测到延迟加载，部分内容可能对 AI 爬虫不可见",
                Severity = "low",
                Solution = "确保关键内容不使用延迟加载"
            });
        }

        result.Issues = issues;
        result.RequiresJSRendering = issues.Any(i => i.Severity == "high");
        result.RiskLevel = issues.Any(i => i.Severity == "high") ? "high" :
                          issues.Any(i => i.Severity == "medium") ? "medium" : "low";

        // 估算可见内容比例
        result.VisibleContentRatio = result.RequiresJSRendering ? 0.3 : 0.9;

        // 生成建议
        result.Recommendations = GenerateJSRenderingRecommendations(result);

        return result;
    }

    private List<string> GenerateJSRenderingRecommendations(JSRenderingResult result)
    {
        var recommendations = new List<string>();

        if (result.RequiresJSRendering)
        {
            recommendations.Add("AI 爬虫不执行 JavaScript，当前页面大部分内容可能不可见");
            recommendations.Add("建议使用 Next.js/Nuxt.js 等框架的 SSR 或 SSG 功能");
            recommendations.Add("或使用预渲染服务如 Prerender.io");
        }

        if (result.RiskLevel == "medium")
        {
            recommendations.Add("部分动态内容可能对 AI 爬虫不可见");
            recommendations.Add("确保核心内容在初始 HTML 中可用");
        }

        recommendations.Add("使用 Google Search Console 的 URL 检查工具验证渲染结果");

        return recommendations;
    }

    #endregion

    #region 4.55-4.58 竞品分析

    /// <summary>
    /// 分析竞品引用
    /// </summary>
    public CompetitorAnalysisResult AnalyzeCompetitors(CompetitorAnalysisRequest request)
    {
        var result = new CompetitorAnalysisResult
        {
            Topic = request.Topic
        };

        // 模拟 SOV 数据（实际需要 API 数据）
        var allDomains = new List<string> { request.OwnDomain };
        allDomains.AddRange(request.CompetitorDomains);

        result.SOVBreakdown = allDomains.Select((domain, index) => new SOVData
        {
            Domain = domain,
            SharePercentage = domain == request.OwnDomain ? 15 : 20 - index * 3,
            Trend = index % 2 == 0 ? "up" : "stable",
            TrendChange = index % 2 == 0 ? 2.5 : 0
        }).ToList();

        // 引用来源分类
        result.SourceBreakdown = new CitationSourceBreakdown
        {
            OwnedPercentage = 25,
            SocialPercentage = 20,
            CompetitorPercentage = 35,
            ThirdPartyPercentage = 20
        };

        // 趋势分析
        result.Trends = new TrendAnalysis
        {
            OverallTrend = "competitive",
            RisingCompetitors = request.CompetitorDomains.Take(2).ToList(),
            DecliningCompetitors = new List<string>()
        };

        // 机会和风险
        result.OpportunityRisk = new OpportunityRiskAnalysis
        {
            Opportunities = new List<string>
            {
                $"在 {request.Topic} 领域增加内容深度",
                "创建更多 FAQ 内容以提升 AIO 可见性",
                "增加专家引用和数据支持"
            },
            Risks = new List<string>
            {
                "竞品在 AI 搜索中的可见度正在上升",
                "需要持续监测和优化"
            },
            ActionItems = new List<string>
            {
                "每周监测 AI 搜索中的品牌提及",
                "针对高价值关键词创建优化内容",
                "建立 Reddit 等平台的正面声誉"
            }
        };

        return result;
    }

    /// <summary>
    /// 获取所有已知 AI 爬虫（从缓存读取）
    /// </summary>
    public List<CrawlerStatus> GetKnownAICrawlers()
    {
        return GetKnownAICrawlersFromCache();
    }

    /// <summary>
    /// 获取所有 LLM Referrers（从缓存读取）
    /// </summary>
    public List<LLMReferrer> GetLLMReferrers()
    {
        return GetKnownLLMReferrersFromCache();
    }

    #endregion
}
