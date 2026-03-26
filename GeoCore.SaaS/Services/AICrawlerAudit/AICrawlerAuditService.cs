using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCore.Data.Entities;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Services.AICrawlerAudit;

/// <summary>
/// AI 爬虫配置审计服务
/// 功能 4.48：检测 14+ AI 爬虫的 robots.txt 配置状态
/// 数据来源：从 ConfigCacheService 缓存读取（数据库管理）
/// </summary>
public class AICrawlerAuditService
{
    private readonly ILogger<AICrawlerAuditService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConfigCacheService _configCache;

    public AICrawlerAuditService(
        ILogger<AICrawlerAuditService> logger,
        IHttpClientFactory httpClientFactory,
        ConfigCacheService configCache)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configCache = configCache;
    }

    /// <summary>
    /// 从缓存获取 AI 爬虫列表并转换为 AICrawlerDefinition
    /// </summary>
    private List<AICrawlerDefinition> GetAICrawlersFromCache()
    {
        var entities = _configCache.GetAllAICrawlers();
        return entities.Select(e => new AICrawlerDefinition
        {
            Name = e.Name,
            Company = e.Company,
            Platform = e.Platform ?? "",
            Purpose = e.Purpose ?? "",
            Importance = e.Importance,
            TrafficShare = e.TrafficShare,
            AlternativeNames = ParseAlternativeNames(e.AlternativeNames)
        }).ToList();
    }

    /// <summary>
    /// 解析 AlternativeNames JSON 数组
    /// </summary>
    private List<string> ParseAlternativeNames(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// 执行 AI 爬虫配置审计
    /// </summary>
    public async Task<AICrawlerAuditReport> AuditAsync(AICrawlerAuditRequest request)
    {
        _logger.LogInformation("[AICrawlerAudit] Starting audit for {Url}", request.WebsiteUrl);

        var report = new AICrawlerAuditReport
        {
            WebsiteUrl = request.WebsiteUrl,
            AuditTime = DateTime.UtcNow
        };

        // 获取 robots.txt 内容
        string? robotsTxt = request.RobotsTxtContent;
        if (string.IsNullOrEmpty(robotsTxt))
        {
            robotsTxt = await FetchRobotsTxtAsync(request.WebsiteUrl);
        }

        report.RobotsTxtExists = !string.IsNullOrEmpty(robotsTxt);
        report.RobotsTxtContent = robotsTxt;

        // 从缓存获取 AI 爬虫列表
        var aiCrawlers = GetAICrawlersFromCache();

        // 分析每个 AI 爬虫的状态
        foreach (var crawler in aiCrawlers)
        {
            var status = AnalyzeCrawlerStatus(robotsTxt, crawler);
            report.Crawlers.Add(status);
        }

        // 计算摘要统计
        report.Summary = CalculateSummary(report.Crawlers);

        // 计算整体评分
        report.OverallScore = CalculateOverallScore(report);
        report.ScoreLevel = DetermineScoreLevel(report.OverallScore);

        // 生成问题列表
        report.Issues = GenerateIssues(report);

        // 生成优化建议
        report.Suggestions = GenerateSuggestions(report);

        _logger.LogInformation("[AICrawlerAudit] Audit completed. Score: {Score}, Level: {Level}",
            report.OverallScore, report.ScoreLevel);

        return report;
    }

    /// <summary>
    /// 获取 robots.txt 内容
    /// </summary>
    private async Task<string?> FetchRobotsTxtAsync(string websiteUrl)
    {
        try
        {
            var uri = new Uri(websiteUrl);
            var robotsUrl = $"{uri.Scheme}://{uri.Host}/robots.txt";

            var client = _httpClientFactory.CreateClient("WebScraper");
            var response = await client.GetAsync(robotsUrl);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            _logger.LogWarning("[AICrawlerAudit] robots.txt not found at {Url}", robotsUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AICrawlerAudit] Failed to fetch robots.txt for {Url}", websiteUrl);
            return null;
        }
    }

    /// <summary>
    /// 分析单个爬虫的状态
    /// </summary>
    private AICrawlerStatus AnalyzeCrawlerStatus(string? robotsTxt, AICrawlerDefinition crawler)
    {
        var status = new AICrawlerStatus
        {
            CrawlerName = crawler.Name,
            Company = crawler.Company,
            Platform = crawler.Platform,
            Purpose = crawler.Purpose,
            Importance = crawler.Importance,
            TrafficShare = crawler.TrafficShare
        };

        if (string.IsNullOrEmpty(robotsTxt))
        {
            status.Status = "allowed";
            status.IsExplicitlyConfigured = false;
            status.Recommendation = "robots.txt 不存在，默认允许所有爬虫";
            return status;
        }

        // 查找与此爬虫相关的规则
        var allNames = new List<string> { crawler.Name };
        allNames.AddRange(crawler.AlternativeNames);

        var rules = new List<string>();
        var isBlocked = false;
        var isPartial = false;
        var isExplicit = false;

        foreach (var name in allNames)
        {
            var crawlerRules = ExtractRulesForCrawler(robotsTxt, name);
            if (crawlerRules.Any())
            {
                isExplicit = true;
                rules.AddRange(crawlerRules);

                // 检查是否有 Disallow: / 规则
                if (crawlerRules.Any(r => r.Contains("Disallow: /") && !r.Contains("Disallow: /?")))
                {
                    if (crawlerRules.Any(r => r.StartsWith("Allow:")))
                    {
                        isPartial = true;
                    }
                    else
                    {
                        isBlocked = true;
                    }
                }
                else if (crawlerRules.Any(r => r.StartsWith("Disallow:") && r.Length > 10))
                {
                    isPartial = true;
                }
            }
        }

        // 检查通配符规则（User-agent: *）
        var wildcardRules = ExtractRulesForCrawler(robotsTxt, "*");
        if (wildcardRules.Any() && !isExplicit)
        {
            rules.AddRange(wildcardRules.Select(r => $"[*] {r}"));
            if (wildcardRules.Any(r => r.Contains("Disallow: /")))
            {
                isBlocked = true;
            }
        }

        status.Rules = rules.Distinct().ToList();
        status.IsExplicitlyConfigured = isExplicit;

        if (isBlocked)
        {
            status.Status = "blocked";
            status.Recommendation = crawler.Importance == "high"
                ? "⚠️ 高重要性爬虫被禁止，可能影响 AI 可见度"
                : "爬虫被禁止";
        }
        else if (isPartial)
        {
            status.Status = "partial";
            status.Recommendation = "部分路径被限制，检查是否符合预期";
        }
        else
        {
            status.Status = "allowed";
            status.Recommendation = isExplicit
                ? "已明确配置允许"
                : "未明确配置，默认允许";
        }

        return status;
    }

    /// <summary>
    /// 提取特定爬虫的规则
    /// </summary>
    private List<string> ExtractRulesForCrawler(string robotsTxt, string crawlerName)
    {
        var rules = new List<string>();
        var lines = robotsTxt.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var inCrawlerSection = false;
        var pattern = new Regex($@"User-agent:\s*{Regex.Escape(crawlerName)}\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                inCrawlerSection = pattern.IsMatch(trimmedLine) ||
                    trimmedLine.Equals($"User-agent: {crawlerName}", StringComparison.OrdinalIgnoreCase);
            }
            else if (inCrawlerSection)
            {
                if (trimmedLine.StartsWith("Allow:", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
                {
                    rules.Add(trimmedLine);
                }
                else if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // 空行可能结束当前 section
                }
            }
        }

        return rules;
    }

    /// <summary>
    /// 计算摘要统计
    /// </summary>
    private AuditSummary CalculateSummary(List<AICrawlerStatus> crawlers)
    {
        var summary = new AuditSummary
        {
            TotalCrawlers = crawlers.Count,
            AllowedCount = crawlers.Count(c => c.Status == "allowed"),
            BlockedCount = crawlers.Count(c => c.Status == "blocked"),
            PartialCount = crawlers.Count(c => c.Status == "partial"),
            NotConfiguredCount = crawlers.Count(c => !c.IsExplicitlyConfigured),
            HighImportanceBlocked = crawlers.Count(c => c.Status == "blocked" && c.Importance == "high")
        };

        // 计算预估 AI 流量可达性
        var allowedTraffic = crawlers
            .Where(c => c.Status == "allowed" || c.Status == "partial")
            .Sum(c => c.TrafficShare);

        summary.EstimatedAITrafficReach = allowedTraffic;

        return summary;
    }

    /// <summary>
    /// 计算整体评分
    /// </summary>
    private int CalculateOverallScore(AICrawlerAuditReport report)
    {
        var score = 100;

        // 高重要性爬虫被禁止：每个 -20 分
        score -= report.Summary.HighImportanceBlocked * 20;

        // 中重要性爬虫被禁止：每个 -5 分
        var mediumBlocked = report.Crawlers.Count(c => c.Status == "blocked" && c.Importance == "medium");
        score -= mediumBlocked * 5;

        // robots.txt 不存在：-10 分（虽然默认允许，但最好明确配置）
        if (!report.RobotsTxtExists)
        {
            score -= 10;
        }

        // 没有明确配置任何 AI 爬虫：-15 分
        if (!report.Crawlers.Any(c => c.IsExplicitlyConfigured))
        {
            score -= 15;
        }

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// 确定评分等级
    /// </summary>
    private string DetermineScoreLevel(int score)
    {
        return score switch
        {
            >= 90 => "excellent",
            >= 70 => "good",
            >= 50 => "warning",
            _ => "critical"
        };
    }

    /// <summary>
    /// 生成问题列表
    /// </summary>
    private List<AuditIssue> GenerateIssues(AICrawlerAuditReport report)
    {
        var issues = new List<AuditIssue>();

        // 检查 robots.txt 是否存在
        if (!report.RobotsTxtExists)
        {
            issues.Add(new AuditIssue
            {
                Level = "warning",
                Type = "missing_robots_txt",
                Title = "robots.txt 文件不存在",
                Description = "网站没有 robots.txt 文件，虽然默认允许所有爬虫，但建议创建并明确配置 AI 爬虫规则",
                SuggestedFix = "创建 robots.txt 文件并添加 AI 爬虫配置"
            });
        }

        // 检查高重要性爬虫被禁止
        var highBlockedCrawlers = report.Crawlers
            .Where(c => c.Status == "blocked" && c.Importance == "high")
            .ToList();

        if (highBlockedCrawlers.Any())
        {
            issues.Add(new AuditIssue
            {
                Level = "critical",
                Type = "high_importance_blocked",
                Title = "高重要性 AI 爬虫被禁止",
                Description = $"以下高重要性 AI 爬虫被禁止访问，这将严重影响您在主流 AI 平台的可见度",
                AffectedCrawlers = highBlockedCrawlers.Select(c => c.CrawlerName).ToList(),
                SuggestedFix = "在 robots.txt 中移除对这些爬虫的 Disallow 规则，或添加 Allow: / 规则"
            });
        }

        // 检查 GPTBot 状态（最重要的爬虫）
        var gptBot = report.Crawlers.FirstOrDefault(c => c.CrawlerName == "GPTBot");
        if (gptBot?.Status == "blocked")
        {
            issues.Add(new AuditIssue
            {
                Level = "critical",
                Type = "gptbot_blocked",
                Title = "GPTBot 被禁止",
                Description = "GPTBot 驱动 87.4% 的 AI 引荐流量，禁止它将严重影响您在 ChatGPT 中的可见度",
                AffectedCrawlers = new List<string> { "GPTBot" },
                SuggestedFix = "添加 'User-agent: GPTBot\\nAllow: /' 到 robots.txt"
            });
        }

        // 检查没有明确配置任何 AI 爬虫
        if (!report.Crawlers.Any(c => c.IsExplicitlyConfigured))
        {
            issues.Add(new AuditIssue
            {
                Level = "info",
                Type = "no_explicit_config",
                Title = "未明确配置 AI 爬虫",
                Description = "robots.txt 中没有针对 AI 爬虫的明确配置，建议添加明确规则以更好地控制 AI 访问",
                SuggestedFix = "在 robots.txt 中添加针对主要 AI 爬虫的明确规则"
            });
        }

        return issues.OrderBy(i => i.Level == "critical" ? 0 : i.Level == "warning" ? 1 : 2).ToList();
    }

    /// <summary>
    /// 生成优化建议
    /// </summary>
    private List<OptimizationSuggestion> GenerateSuggestions(AICrawlerAuditReport report)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // 建议 1：创建或优化 robots.txt
        if (!report.RobotsTxtExists || !report.Crawlers.Any(c => c.IsExplicitlyConfigured))
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = "high",
                Title = "创建 AI 友好的 robots.txt 配置",
                Description = "明确配置主要 AI 爬虫的访问权限，确保您的内容能被 AI 平台索引",
                ExpectedImpact = "提升在 ChatGPT、Claude、Perplexity 等平台的可见度",
                Steps = new List<string>
                {
                    "创建或编辑 robots.txt 文件",
                    "添加针对主要 AI 爬虫的 Allow 规则",
                    "测试配置是否生效"
                },
                ExampleCode = GenerateRecommendedRobotsTxt()
            });
        }

        // 建议 2：解除高重要性爬虫的限制
        var blockedHighCrawlers = report.Crawlers
            .Where(c => c.Status == "blocked" && c.Importance == "high")
            .ToList();

        if (blockedHighCrawlers.Any())
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Priority = "high",
                Title = "解除高重要性 AI 爬虫的限制",
                Description = $"当前有 {blockedHighCrawlers.Count} 个高重要性爬虫被禁止，这会显著降低 AI 可见度",
                ExpectedImpact = $"预计可提升约 {blockedHighCrawlers.Sum(c => c.TrafficShare):F1}% 的 AI 流量可达性",
                Steps = new List<string>
                {
                    "编辑 robots.txt 文件",
                    "移除或修改针对这些爬虫的 Disallow 规则",
                    "添加 Allow: / 规则以明确允许"
                }
            });
        }

        // 建议 3：创建 llms.txt 文件
        suggestions.Add(new OptimizationSuggestion
        {
            Priority = "medium",
            Title = "创建 llms.txt 文件",
            Description = "llms.txt 是新兴的 AI 友好标准，帮助 LLM 更好地理解您的网站结构",
            ExpectedImpact = "提升 AI 对网站内容的理解和引用质量",
            Steps = new List<string>
            {
                "使用 llms.txt 生成器创建文件",
                "将文件放置在网站根目录",
                "在 robots.txt 中添加 llms.txt 的引用"
            },
            ExampleCode = "# llms.txt\n# 网站标题和描述\n# 重要页面列表\n# 参见 llms.txt 生成器功能"
        });

        // 建议 4：配置 GA4 AI 流量追踪
        suggestions.Add(new OptimizationSuggestion
        {
            Priority = "medium",
            Title = "配置 GA4 AI 流量追踪",
            Description = "在 Google Analytics 4 中配置 AI 流量来源识别，追踪来自 AI 平台的流量",
            ExpectedImpact = "了解 AI 平台带来的实际流量和转化",
            Steps = new List<string>
            {
                "在 GA4 中创建自定义渠道分组",
                "添加 AI 平台的 referrer 识别规则",
                "设置 AI 流量报告和告警"
            }
        });

        return suggestions;
    }

    /// <summary>
    /// 生成推荐的 robots.txt 配置
    /// </summary>
    private string GenerateRecommendedRobotsTxt()
    {
        return @"# AI 爬虫配置 - 推荐配置
# 允许主要 AI 爬虫访问

# OpenAI (ChatGPT)
User-agent: GPTBot
Allow: /

User-agent: ChatGPT-User
Allow: /

User-agent: OAI-SearchBot
Allow: /

# Anthropic (Claude)
User-agent: ClaudeBot
Allow: /

User-agent: Claude-Web
Allow: /

# Perplexity
User-agent: PerplexityBot
Allow: /

# Google (Gemini)
User-agent: Google-Extended
Allow: /

# Microsoft (Copilot)
User-agent: Bingbot
Allow: /

# 通用规则
User-agent: *
Allow: /

# Sitemap
Sitemap: https://example.com/sitemap.xml

# llms.txt (可选)
# 参见: https://example.com/llms.txt";
    }

    /// <summary>
    /// 获取所有支持的 AI 爬虫列表（从缓存读取）
    /// </summary>
    public List<AICrawlerDefinition> GetSupportedCrawlers()
    {
        return GetAICrawlersFromCache();
    }
}
