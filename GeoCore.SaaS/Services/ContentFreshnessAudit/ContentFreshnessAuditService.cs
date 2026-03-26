using System.Xml.Linq;

namespace GeoCore.SaaS.Services.ContentFreshnessAudit;

/// <summary>
/// 内容新鲜度审计服务
/// 功能 4.51：60 天未更新页面标记，引用率下降阈值检测
/// 来源：GEO 研究 - 90 天内内容有 2x 引用率，年份更新 +71%
/// </summary>
public class ContentFreshnessAuditService
{
    private readonly ILogger<ContentFreshnessAuditService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ContentFreshnessAuditService(
        ILogger<ContentFreshnessAuditService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 执行内容新鲜度审计
    /// </summary>
    public async Task<ContentFreshnessAuditReport> AuditAsync(ContentFreshnessAuditRequest request)
    {
        _logger.LogInformation("[ContentFreshnessAudit] Starting audit for {Url}", request.WebsiteUrl);

        var report = new ContentFreshnessAuditReport
        {
            WebsiteUrl = request.WebsiteUrl,
            AuditTime = DateTime.UtcNow
        };

        // 收集页面信息
        var pages = new List<PageFreshnessDetail>();

        // 1. 从手动提供的页面列表
        if (request.Pages?.Any() == true)
        {
            pages.AddRange(request.Pages.Select(p => new PageFreshnessDetail
            {
                Url = p.Url,
                Title = p.Title,
                LastModified = p.LastModified,
                PageType = p.PageType ?? InferPageType(p.Url)
            }));
        }

        // 2. 从 sitemap 获取页面
        var sitemapUrl = request.SitemapUrl ?? await DiscoverSitemapUrlAsync(request.WebsiteUrl);
        if (!string.IsNullOrEmpty(sitemapUrl))
        {
            var sitemapPages = await ParseSitemapAsync(sitemapUrl, request.MaxPages);
            foreach (var sp in sitemapPages)
            {
                if (!pages.Any(p => p.Url == sp.Url))
                {
                    pages.Add(sp);
                }
            }
        }

        // 3. 分析每个页面的新鲜度
        foreach (var page in pages)
        {
            AnalyzePageFreshness(page, request);
        }

        // 4. 按更新优先级排序
        pages = pages.OrderByDescending(p => p.UpdatePriority).ThenBy(p => p.AgeDays ?? int.MaxValue).ToList();

        report.Pages = pages;
        report.TotalPages = pages.Count;

        // 5. 计算摘要统计
        report.Summary = CalculateSummary(pages, request);

        // 6. 计算整体评分
        report.OverallScore = CalculateOverallScore(report.Summary);
        report.ScoreLevel = DetermineScoreLevel(report.OverallScore);

        // 7. 按类型分布
        report.DistributionByType = CalculateDistributionByType(pages);

        // 8. 生成问题列表
        report.Issues = GenerateIssues(report, request);

        // 9. 生成优化建议
        report.Suggestions = GenerateSuggestions(report, request);

        _logger.LogInformation("[ContentFreshnessAudit] Audit completed. Score: {Score}, Pages: {Count}",
            report.OverallScore, report.TotalPages);

        return report;
    }

    /// <summary>
    /// 发现 sitemap URL
    /// </summary>
    private async Task<string?> DiscoverSitemapUrlAsync(string websiteUrl)
    {
        try
        {
            var uri = new Uri(websiteUrl);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            var client = _httpClientFactory.CreateClient("WebScraper");

            // 尝试常见位置
            var possibleUrls = new[]
            {
                $"{baseUrl}/sitemap.xml",
                $"{baseUrl}/sitemap_index.xml"
            };

            foreach (var url in possibleUrls)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return url;
                    }
                }
                catch { }
            }

            // 尝试从 robots.txt 获取
            try
            {
                var robotsResponse = await client.GetAsync($"{baseUrl}/robots.txt");
                if (robotsResponse.IsSuccessStatusCode)
                {
                    var content = await robotsResponse.Content.ReadAsStringAsync();
                    foreach (var line in content.Split('\n'))
                    {
                        if (line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                        {
                            return line.Substring(8).Trim();
                        }
                    }
                }
            }
            catch { }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshnessAudit] Error discovering sitemap");
            return null;
        }
    }

    /// <summary>
    /// 解析 sitemap
    /// </summary>
    private async Task<List<PageFreshnessDetail>> ParseSitemapAsync(string sitemapUrl, int maxPages)
    {
        var pages = new List<PageFreshnessDetail>();

        try
        {
            var client = _httpClientFactory.CreateClient("WebScraper");
            var response = await client.GetAsync(sitemapUrl);

            if (!response.IsSuccessStatusCode)
            {
                return pages;
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(content);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // 检查是否是 sitemap index
            var sitemapIndexElements = doc.Descendants(ns + "sitemap").ToList();
            if (sitemapIndexElements.Any())
            {
                foreach (var sitemapElement in sitemapIndexElements.Take(3))
                {
                    var loc = sitemapElement.Element(ns + "loc")?.Value;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        var subPages = await ParseSitemapAsync(loc, maxPages - pages.Count);
                        pages.AddRange(subPages);
                        if (pages.Count >= maxPages) break;
                    }
                }
            }
            else
            {
                var urlElements = doc.Descendants(ns + "url").Take(maxPages - pages.Count);
                foreach (var urlElement in urlElements)
                {
                    var url = urlElement.Element(ns + "loc")?.Value;
                    if (string.IsNullOrEmpty(url)) continue;

                    var page = new PageFreshnessDetail
                    {
                        Url = url,
                        PageType = InferPageType(url)
                    };

                    var lastmod = urlElement.Element(ns + "lastmod")?.Value;
                    if (DateTime.TryParse(lastmod, out var lastModDate))
                    {
                        page.LastModified = lastModDate;
                    }

                    pages.Add(page);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentFreshnessAudit] Error parsing sitemap {Url}", sitemapUrl);
        }

        return pages;
    }

    /// <summary>
    /// 分析页面新鲜度
    /// </summary>
    private void AnalyzePageFreshness(PageFreshnessDetail page, ContentFreshnessAuditRequest request)
    {
        if (page.LastModified.HasValue)
        {
            page.AgeDays = (int)(DateTime.UtcNow - page.LastModified.Value).TotalDays;

            // 确定新鲜度状态
            if (page.AgeDays <= request.FreshnessThresholdDays)
            {
                page.FreshnessStatus = "fresh";
                page.FreshnessScore = 100 - (int)(page.AgeDays.Value * 100.0 / request.FreshnessThresholdDays * 0.2);
                page.NeedsUpdate = false;
                page.UpdatePriority = 1;
                page.RecommendedAction = "保持监测";
                page.CitationImpact = "最佳引用率（90 天内 2x 引用率）";
            }
            else if (page.AgeDays <= request.WarningThresholdDays)
            {
                page.FreshnessStatus = "attention";
                page.FreshnessScore = 80 - (int)((page.AgeDays.Value - request.FreshnessThresholdDays) * 20.0 /
                    (request.WarningThresholdDays - request.FreshnessThresholdDays));
                page.NeedsUpdate = true;
                page.UpdatePriority = 5;
                page.RecommendedAction = "计划更新";
                page.CitationImpact = "引用率开始下降";
            }
            else if (page.AgeDays <= request.CriticalThresholdDays)
            {
                page.FreshnessStatus = "warning";
                page.FreshnessScore = 60 - (int)((page.AgeDays.Value - request.WarningThresholdDays) * 30.0 /
                    (request.CriticalThresholdDays - request.WarningThresholdDays));
                page.NeedsUpdate = true;
                page.UpdatePriority = 7;
                page.RecommendedAction = "尽快更新";
                page.CitationImpact = "引用率显著下降";
            }
            else
            {
                page.FreshnessStatus = "critical";
                page.FreshnessScore = Math.Max(0, 30 - (int)((page.AgeDays.Value - request.CriticalThresholdDays) * 30.0 / 180));
                page.NeedsUpdate = true;
                page.UpdatePriority = 10;
                page.RecommendedAction = "紧急更新或归档";
                page.CitationImpact = "引用率极低，可能被 AI 忽略";
            }
        }
        else
        {
            page.FreshnessStatus = "unknown";
            page.FreshnessScore = 50;
            page.NeedsUpdate = true;
            page.UpdatePriority = 3;
            page.RecommendedAction = "检查并添加更新时间";
            page.CitationImpact = "无法评估";
        }

        // 根据页面类型调整优先级
        page.UpdatePriority = AdjustPriorityByPageType(page.UpdatePriority, page.PageType);
    }

    /// <summary>
    /// 根据页面类型调整优先级
    /// </summary>
    private int AdjustPriorityByPageType(int basePriority, string pageType)
    {
        var adjustment = pageType switch
        {
            "homepage" => 2,
            "product" => 1,
            "documentation" => 1,
            "blog" => 0,
            "faq" => 0,
            "about" => -1,
            "contact" => -2,
            _ => 0
        };

        return Math.Max(1, Math.Min(10, basePriority + adjustment));
    }

    /// <summary>
    /// 推断页面类型
    /// </summary>
    private string InferPageType(string url)
    {
        try
        {
            var path = new Uri(url).AbsolutePath.ToLower();

            if (path == "/" || path == "/index.html")
                return "homepage";
            if (path.Contains("/product") || path.Contains("/shop"))
                return "product";
            if (path.Contains("/blog") || path.Contains("/article") || path.Contains("/post"))
                return "blog";
            if (path.Contains("/doc") || path.Contains("/guide") || path.Contains("/help"))
                return "documentation";
            if (path.Contains("/faq"))
                return "faq";
            if (path.Contains("/about"))
                return "about";
            if (path.Contains("/contact"))
                return "contact";

            return "other";
        }
        catch
        {
            return "other";
        }
    }

    /// <summary>
    /// 计算摘要统计
    /// </summary>
    private FreshnessAuditSummary CalculateSummary(List<PageFreshnessDetail> pages, ContentFreshnessAuditRequest request)
    {
        var summary = new FreshnessAuditSummary
        {
            FreshCount = pages.Count(p => p.FreshnessStatus == "fresh"),
            AttentionCount = pages.Count(p => p.FreshnessStatus == "attention"),
            WarningCount = pages.Count(p => p.FreshnessStatus == "warning"),
            CriticalCount = pages.Count(p => p.FreshnessStatus == "critical"),
            UnknownCount = pages.Count(p => p.FreshnessStatus == "unknown")
        };

        var total = pages.Count;
        if (total > 0)
        {
            summary.FreshRate = (double)summary.FreshCount / total;
        }

        var pagesWithAge = pages.Where(p => p.AgeDays.HasValue).ToList();
        if (pagesWithAge.Any())
        {
            summary.AverageAgeDays = pagesWithAge.Average(p => p.AgeDays!.Value);
            summary.OldestAgeDays = pagesWithAge.Max(p => p.AgeDays!.Value);
            summary.NewestAgeDays = pagesWithAge.Min(p => p.AgeDays!.Value);
        }

        // 预估引用率影响
        if (summary.FreshRate >= 0.8)
        {
            summary.CitationImpactEstimate = "优秀：大部分内容保持新鲜，预计有最佳引用率";
        }
        else if (summary.FreshRate >= 0.5)
        {
            summary.CitationImpactEstimate = "良好：半数内容新鲜，引用率可能下降 20-30%";
        }
        else if (summary.FreshRate >= 0.3)
        {
            summary.CitationImpactEstimate = "警告：大部分内容过时，引用率可能下降 50%+";
        }
        else
        {
            summary.CitationImpactEstimate = "严重：内容严重过时，AI 可能不再引用";
        }

        return summary;
    }

    /// <summary>
    /// 计算整体评分
    /// </summary>
    private int CalculateOverallScore(FreshnessAuditSummary summary)
    {
        var total = summary.FreshCount + summary.AttentionCount + summary.WarningCount +
                   summary.CriticalCount + summary.UnknownCount;

        if (total == 0) return 0;

        // 加权计算
        var weightedScore = (summary.FreshCount * 100 +
                            summary.AttentionCount * 70 +
                            summary.WarningCount * 40 +
                            summary.CriticalCount * 10 +
                            summary.UnknownCount * 50) / total;

        return (int)weightedScore;
    }

    /// <summary>
    /// 确定评分等级
    /// </summary>
    private string DetermineScoreLevel(int score)
    {
        return score switch
        {
            >= 80 => "excellent",
            >= 60 => "good",
            >= 40 => "warning",
            _ => "critical"
        };
    }

    /// <summary>
    /// 按类型计算分布
    /// </summary>
    private Dictionary<string, FreshnessDistributionItem> CalculateDistributionByType(List<PageFreshnessDetail> pages)
    {
        return pages
            .GroupBy(p => p.PageType)
            .ToDictionary(
                g => g.Key,
                g => new FreshnessDistributionItem
                {
                    Total = g.Count(),
                    Fresh = g.Count(p => p.FreshnessStatus == "fresh"),
                    Attention = g.Count(p => p.FreshnessStatus == "attention"),
                    Warning = g.Count(p => p.FreshnessStatus == "warning"),
                    Critical = g.Count(p => p.FreshnessStatus == "critical"),
                    FreshRate = g.Count() > 0 ? (double)g.Count(p => p.FreshnessStatus == "fresh") / g.Count() : 0
                });
    }

    /// <summary>
    /// 生成问题列表
    /// </summary>
    private List<FreshnessIssue> GenerateIssues(ContentFreshnessAuditReport report, ContentFreshnessAuditRequest request)
    {
        var issues = new List<FreshnessIssue>();

        // 严重过时内容
        if (report.Summary.CriticalCount > 0)
        {
            var criticalPages = report.Pages.Where(p => p.FreshnessStatus == "critical").Take(5).ToList();
            issues.Add(new FreshnessIssue
            {
                Level = "critical",
                Type = "critical_outdated",
                Title = $"{report.Summary.CriticalCount} 个页面严重过时",
                Description = $"这些页面超过 {request.CriticalThresholdDays} 天未更新，AI 平台可能不再引用",
                AffectedPages = report.Summary.CriticalCount,
                AffectedUrls = criticalPages.Select(p => p.Url).ToList(),
                SuggestedFix = "立即更新这些页面，或考虑归档/重定向"
            });
        }

        // 警告级别内容
        if (report.Summary.WarningCount > 0)
        {
            var warningPages = report.Pages.Where(p => p.FreshnessStatus == "warning").Take(5).ToList();
            issues.Add(new FreshnessIssue
            {
                Level = "warning",
                Type = "warning_outdated",
                Title = $"{report.Summary.WarningCount} 个页面需要更新",
                Description = $"这些页面已超过 {request.WarningThresholdDays} 天未更新，引用率正在下降",
                AffectedPages = report.Summary.WarningCount,
                AffectedUrls = warningPages.Select(p => p.Url).ToList(),
                SuggestedFix = "在未来 2-4 周内安排更新"
            });
        }

        // 首页过时
        var homepage = report.Pages.FirstOrDefault(p => p.PageType == "homepage");
        if (homepage != null && (homepage.FreshnessStatus == "warning" || homepage.FreshnessStatus == "critical"))
        {
            issues.Add(new FreshnessIssue
            {
                Level = "critical",
                Type = "homepage_outdated",
                Title = "首页内容过时",
                Description = $"首页已 {homepage.AgeDays} 天未更新，这是最重要的页面",
                AffectedPages = 1,
                AffectedUrls = new List<string> { homepage.Url },
                SuggestedFix = "优先更新首页内容"
            });
        }

        // 未知更新时间
        if (report.Summary.UnknownCount > 0)
        {
            issues.Add(new FreshnessIssue
            {
                Level = "info",
                Type = "unknown_lastmod",
                Title = $"{report.Summary.UnknownCount} 个页面缺少更新时间",
                Description = "这些页面在 sitemap 中没有 lastmod 信息，无法评估新鲜度",
                AffectedPages = report.Summary.UnknownCount,
                SuggestedFix = "在 sitemap 中添加 lastmod 标签"
            });
        }

        // 整体新鲜度低
        if (report.Summary.FreshRate < 0.5)
        {
            issues.Add(new FreshnessIssue
            {
                Level = "warning",
                Type = "low_freshness_rate",
                Title = "整体内容新鲜度偏低",
                Description = $"只有 {report.Summary.FreshRate:P0} 的内容保持新鲜，这会影响整体 AI 可见度",
                AffectedPages = report.TotalPages,
                SuggestedFix = "建立定期内容更新机制"
            });
        }

        return issues.OrderBy(i => i.Level == "critical" ? 0 : i.Level == "warning" ? 1 : 2).ToList();
    }

    /// <summary>
    /// 生成优化建议
    /// </summary>
    private List<FreshnessSuggestion> GenerateSuggestions(ContentFreshnessAuditReport report, ContentFreshnessAuditRequest request)
    {
        var suggestions = new List<FreshnessSuggestion>();

        // 建议 1：更新严重过时内容
        if (report.Summary.CriticalCount > 0)
        {
            suggestions.Add(new FreshnessSuggestion
            {
                Priority = "high",
                Title = "紧急更新严重过时内容",
                Description = $"有 {report.Summary.CriticalCount} 个页面超过 {request.CriticalThresholdDays} 天未更新",
                ExpectedImpact = "恢复 AI 引用率，研究显示年份更新可提升 71% 引用率",
                Steps = new List<string>
                {
                    "审查严重过时页面列表",
                    "更新过时的数据和统计信息",
                    "添加最新的行业趋势和见解",
                    "更新发布日期和年份引用",
                    "重新提交到搜索引擎索引"
                },
                EstimatedEffort = $"每页约 1-2 小时，共 {report.Summary.CriticalCount} 页"
            });
        }

        // 建议 2：建立内容更新日历
        if (report.Summary.FreshRate < 0.7)
        {
            suggestions.Add(new FreshnessSuggestion
            {
                Priority = "high",
                Title = "建立内容更新日历",
                Description = "系统性地管理内容更新，确保内容保持新鲜",
                ExpectedImpact = "持续保持高引用率（90 天内内容有 2x 引用率）",
                Steps = new List<string>
                {
                    "创建内容清单，标记每个页面的更新周期",
                    "设置 60 天更新提醒",
                    "优先更新高流量和高转化页面",
                    "建立内容更新 SOP",
                    "定期审查更新效果"
                },
                EstimatedEffort = "初始设置 2-4 小时，持续维护每周 1-2 小时"
            });
        }

        // 建议 3：优化 sitemap lastmod
        if (report.Summary.UnknownCount > 0)
        {
            suggestions.Add(new FreshnessSuggestion
            {
                Priority = "medium",
                Title = "完善 Sitemap lastmod 信息",
                Description = $"有 {report.Summary.UnknownCount} 个页面缺少更新时间信息",
                ExpectedImpact = "帮助搜索引擎和 AI 爬虫了解内容新鲜度",
                Steps = new List<string>
                {
                    "检查 CMS 或静态站点生成器的 sitemap 配置",
                    "确保所有页面都有 lastmod 标签",
                    "使用实际的最后修改时间，而非生成时间",
                    "验证 sitemap 格式正确"
                },
                EstimatedEffort = "1-2 小时"
            });
        }

        // 建议 4：常青内容策略
        suggestions.Add(new FreshnessSuggestion
        {
            Priority = "medium",
            Title = "采用常青内容策略",
            Description = "创建不易过时的内容，减少更新频率需求",
            ExpectedImpact = "降低维护成本，保持长期引用率",
            Steps = new List<string>
            {
                "识别可以转化为常青内容的页面",
                "移除时效性强的数据，使用相对时间",
                "添加 '最后更新' 日期显示",
                "定期进行小幅更新而非大改"
            },
            EstimatedEffort = "每页 30 分钟评估和调整"
        });

        // 建议 5：自动化监测
        suggestions.Add(new FreshnessSuggestion
        {
            Priority = "low",
            Title = "设置自动化新鲜度监测",
            Description = "自动追踪内容新鲜度，及时发现过时内容",
            ExpectedImpact = "主动管理内容生命周期",
            Steps = new List<string>
            {
                "使用 GeoCore AI 的内容新鲜度追踪功能",
                "设置 60 天未更新告警",
                "配置每周新鲜度报告",
                "集成到内容管理工作流"
            },
            EstimatedEffort = "初始设置 1 小时"
        });

        return suggestions;
    }
}
