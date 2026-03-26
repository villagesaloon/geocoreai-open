using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GeoCore.SaaS.Services.SiteAudit;

public class SiteAuditService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SiteAuditService> _logger;

    private static readonly List<(string Name, string UserAgent, string Platform)> KnownAICrawlers = new()
    {
        ("GPTBot", "GPTBot", "ChatGPT"),
        ("ChatGPT-User", "ChatGPT-User", "ChatGPT"),
        ("OAI-SearchBot", "OAI-SearchBot", "ChatGPT"),
        ("ClaudeBot", "ClaudeBot", "Claude"),
        ("Claude-Web", "Claude-Web", "Claude"),
        ("PerplexityBot", "PerplexityBot", "Perplexity"),
        ("Amazonbot", "Amazonbot", "Amazon"),
        ("anthropic-ai", "anthropic-ai", "Claude"),
        ("Google-Extended", "Google-Extended", "Gemini"),
        ("Bytespider", "Bytespider", "ByteDance"),
        ("CCBot", "CCBot", "Common Crawl"),
        ("FacebookBot", "FacebookBot", "Meta"),
        ("cohere-ai", "cohere-ai", "Cohere"),
        ("Diffbot", "Diffbot", "Diffbot")
    };

    public SiteAuditService(HttpClient httpClient, ILogger<SiteAuditService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    #region Main Audit

    public async Task<SiteAuditResult> AuditSiteAsync(SiteAuditRequest request)
    {
        var result = new SiteAuditResult
        {
            Url = request.Url,
            AuditTime = DateTime.UtcNow
        };

        try
        {
            var uri = new Uri(request.Url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            // Fetch page content
            var pageContent = await FetchPageContentAsync(request.Url);

            if (request.IncludeTechnical)
            {
                result.Technical = await AuditTechnicalAsync(baseUrl, request.Url, pageContent);
            }

            if (request.IncludeContent)
            {
                result.Content = AuditContent(pageContent);
            }

            if (request.IncludeEEAT)
            {
                result.EEAT = AuditEEAT(pageContent, baseUrl);
            }

            // Calculate overall score
            CalculateOverallScore(result);

            // Collect all issues
            CollectIssues(result);

            // Generate recommendations
            GenerateRecommendations(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Site audit failed for {Url}", request.Url);
            result.Issues.Add(new AuditIssue
            {
                Category = "Technical",
                Severity = "Critical",
                Title = "审计失败",
                Description = $"无法完成网站审计: {ex.Message}",
                ImpactScore = 10
            });
        }

        return result;
    }

    #endregion

    #region Technical Audit (6.5-6.10)

    private async Task<TechnicalAuditResult> AuditTechnicalAsync(string baseUrl, string pageUrl, string pageContent)
    {
        var result = new TechnicalAuditResult();

        // 6.5 robots.txt AI 爬虫检测
        result.RobotsTxt = await AuditRobotsTxtAsync(baseUrl);

        // 6.6 Core Web Vitals (simulated - would need real API)
        result.CoreWebVitals = SimulateCoreWebVitals();

        // 6.7 JS 渲染检测
        result.JSRendering = AuditJSRendering(pageContent);

        // 6.8 HTTPS/Canonical
        result.HttpsCanonical = AuditHttpsCanonical(pageUrl, pageContent);

        // 6.9 Sitemap 检测
        result.Sitemap = await AuditSitemapAsync(baseUrl);

        // 6.10 llms.txt 检测
        result.LlmsTxt = await AuditLlmsTxtAsync(baseUrl);

        // Calculate technical score
        result.Score = CalculateTechnicalScore(result);

        return result;
    }

    private async Task<RobotsTxtAudit> AuditRobotsTxtAsync(string baseUrl)
    {
        var result = new RobotsTxtAudit();

        try
        {
            var robotsUrl = $"{baseUrl}/robots.txt";
            var response = await _httpClient.GetAsync(robotsUrl);

            if (response.IsSuccessStatusCode)
            {
                result.Exists = true;
                result.Content = await response.Content.ReadAsStringAsync();

                // Parse robots.txt for AI crawlers
                foreach (var crawler in KnownAICrawlers)
                {
                    var isAllowed = IsUserAgentAllowed(result.Content, crawler.UserAgent);
                    result.CrawlerAccess.Add(new AICrawlerAccess
                    {
                        CrawlerName = crawler.Name,
                        UserAgent = crawler.UserAgent,
                        Platform = crawler.Platform,
                        IsAllowed = isAllowed
                    });

                    if (!isAllowed)
                    {
                        result.BlockedCrawlers.Add(crawler.Name);
                    }
                }

                result.AllAICrawlersAllowed = result.BlockedCrawlers.Count == 0;

                if (result.BlockedCrawlers.Count > 0)
                {
                    result.Issues.Add($"以下 AI 爬虫被阻止: {string.Join(", ", result.BlockedCrawlers)}");
                }
            }
            else
            {
                result.Exists = false;
                result.Issues.Add("robots.txt 文件不存在");
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add($"无法检测 robots.txt: {ex.Message}");
        }

        return result;
    }

    private bool IsUserAgentAllowed(string robotsTxt, string userAgent)
    {
        var lines = robotsTxt.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentUserAgent = "";
        var isDisallowed = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim().ToLower();

            if (trimmedLine.StartsWith("user-agent:"))
            {
                currentUserAgent = trimmedLine.Replace("user-agent:", "").Trim();
            }
            else if (trimmedLine.StartsWith("disallow:"))
            {
                if (currentUserAgent == "*" || currentUserAgent.Contains(userAgent.ToLower()))
                {
                    var path = trimmedLine.Replace("disallow:", "").Trim();
                    if (path == "/" || path == "/*")
                    {
                        isDisallowed = true;
                    }
                }
            }
            else if (trimmedLine.StartsWith("allow:"))
            {
                if (currentUserAgent == "*" || currentUserAgent.Contains(userAgent.ToLower()))
                {
                    isDisallowed = false;
                }
            }
        }

        return !isDisallowed;
    }

    private CoreWebVitalsAudit SimulateCoreWebVitals()
    {
        // In production, this would call PageSpeed Insights API
        var random = new Random();
        var lcp = 1.5 + random.NextDouble() * 2;
        var inp = 100 + random.NextDouble() * 150;
        var cls = random.NextDouble() * 0.15;

        return new CoreWebVitalsAudit
        {
            LCP = Math.Round(lcp, 2),
            INP = Math.Round(inp, 0),
            CLS = Math.Round(cls, 3),
            LCPStatus = lcp < 2.5 ? "good" : lcp < 4 ? "needs-improvement" : "poor",
            INPStatus = inp < 200 ? "good" : inp < 500 ? "needs-improvement" : "poor",
            CLSStatus = cls < 0.1 ? "good" : cls < 0.25 ? "needs-improvement" : "poor",
            Passed = lcp < 2.5 && inp < 200 && cls < 0.1,
            Issues = new List<string>()
        };
    }

    private JSRenderingAudit AuditJSRendering(string pageContent)
    {
        var result = new JSRenderingAudit();

        // Check for common JS framework indicators
        var jsFrameworkPatterns = new[]
        {
            @"<div\s+id=[""']root[""']\s*>",
            @"<div\s+id=[""']app[""']\s*>",
            @"__NEXT_DATA__",
            @"__NUXT__",
            @"ng-app",
            @"data-reactroot"
        };

        foreach (var pattern in jsFrameworkPatterns)
        {
            if (Regex.IsMatch(pageContent, pattern, RegexOptions.IgnoreCase))
            {
                result.HasJSRenderedContent = true;
                result.JSRenderedElements.Add(pattern);
            }
        }

        // Check if main content is in noscript or if body is mostly empty
        var bodyMatch = Regex.Match(pageContent, @"<body[^>]*>(.*?)</body>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (bodyMatch.Success)
        {
            var bodyContent = bodyMatch.Groups[1].Value;
            var textContent = Regex.Replace(bodyContent, @"<[^>]+>", " ");
            textContent = Regex.Replace(textContent, @"\s+", " ").Trim();

            if (textContent.Length < 500 && result.HasJSRenderedContent)
            {
                result.CriticalContentInJS = true;
                result.Issues.Add("主要内容可能通过 JavaScript 渲染，AI 爬虫可能无法访问");
            }
        }

        if (result.HasJSRenderedContent)
        {
            result.Recommendation = "考虑使用服务端渲染 (SSR) 或预渲染，确保 AI 爬虫可以访问内容";
        }

        return result;
    }

    private HttpsCanonicalAudit AuditHttpsCanonical(string pageUrl, string pageContent)
    {
        var result = new HttpsCanonicalAudit
        {
            IsHttps = pageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        };

        // Check canonical
        var canonicalMatch = Regex.Match(pageContent, @"<link[^>]+rel=[""']canonical[""'][^>]+href=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (!canonicalMatch.Success)
        {
            canonicalMatch = Regex.Match(pageContent, @"<link[^>]+href=[""']([^""']+)[""'][^>]+rel=[""']canonical[""']", RegexOptions.IgnoreCase);
        }

        if (canonicalMatch.Success)
        {
            result.HasCanonical = true;
            result.CanonicalUrl = canonicalMatch.Groups[1].Value;
            result.CanonicalMatchesUrl = result.CanonicalUrl.TrimEnd('/') == pageUrl.TrimEnd('/');

            if (!result.CanonicalMatchesUrl)
            {
                result.Issues.Add($"Canonical URL ({result.CanonicalUrl}) 与当前 URL 不匹配");
            }
        }
        else
        {
            result.HasCanonical = false;
            result.Issues.Add("缺少 canonical 标签");
        }

        if (!result.IsHttps)
        {
            result.Issues.Add("网站未使用 HTTPS");
        }

        return result;
    }

    private async Task<SitemapAudit> AuditSitemapAsync(string baseUrl)
    {
        var result = new SitemapAudit();

        try
        {
            var sitemapUrl = $"{baseUrl}/sitemap.xml";
            var response = await _httpClient.GetAsync(sitemapUrl);

            if (response.IsSuccessStatusCode)
            {
                result.Exists = true;
                result.SitemapUrl = sitemapUrl;

                var content = await response.Content.ReadAsStringAsync();
                try
                {
                    var doc = XDocument.Parse(content);
                    var ns = doc.Root?.GetDefaultNamespace();
                    var urls = doc.Descendants(ns + "url").ToList();
                    result.UrlCount = urls.Count;
                    result.IsValid = true;

                    if (result.UrlCount == 0)
                    {
                        result.Issues.Add("Sitemap 为空，没有 URL");
                    }
                }
                catch
                {
                    result.IsValid = false;
                    result.Issues.Add("Sitemap XML 格式无效");
                }
            }
            else
            {
                result.Exists = false;
                result.Issues.Add("sitemap.xml 不存在");
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add($"无法检测 sitemap: {ex.Message}");
        }

        return result;
    }

    private async Task<LlmsTxtAudit> AuditLlmsTxtAsync(string baseUrl)
    {
        var result = new LlmsTxtAudit();

        try
        {
            var llmsTxtUrl = $"{baseUrl}/llms.txt";
            var response = await _httpClient.GetAsync(llmsTxtUrl);

            if (response.IsSuccessStatusCode)
            {
                result.Exists = true;
                result.Content = await response.Content.ReadAsStringAsync();

                var lines = result.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                result.EntryCount = lines.Count(l => l.StartsWith("http"));
                result.IsValid = result.EntryCount > 0;

                if (!result.IsValid)
                {
                    result.Issues.Add("llms.txt 格式无效或为空");
                }
            }
            else
            {
                result.Exists = false;
                result.Issues.Add("llms.txt 不存在 - 建议创建以帮助 AI 爬虫发现重要内容");
            }
        }
        catch (Exception ex)
        {
            result.Issues.Add($"无法检测 llms.txt: {ex.Message}");
        }

        return result;
    }

    private int CalculateTechnicalScore(TechnicalAuditResult result)
    {
        var score = 100;

        // robots.txt (20 points)
        if (!result.RobotsTxt.Exists) score -= 10;
        if (!result.RobotsTxt.AllAICrawlersAllowed) score -= 10;

        // Core Web Vitals (20 points)
        if (!result.CoreWebVitals.Passed) score -= 20;

        // JS Rendering (15 points)
        if (result.JSRendering.CriticalContentInJS) score -= 15;

        // HTTPS/Canonical (15 points)
        if (!result.HttpsCanonical.IsHttps) score -= 10;
        if (!result.HttpsCanonical.HasCanonical) score -= 5;

        // Sitemap (15 points)
        if (!result.Sitemap.Exists) score -= 10;
        if (!result.Sitemap.IsValid) score -= 5;

        // llms.txt (15 points)
        if (!result.LlmsTxt.Exists) score -= 10;
        if (!result.LlmsTxt.IsValid && result.LlmsTxt.Exists) score -= 5;

        return Math.Max(0, score);
    }

    #endregion

    #region Content Audit (6.11-6.15)

    private ContentAuditResult AuditContent(string pageContent)
    {
        var result = new ContentAuditResult();

        // 6.11 答案胶囊检测
        result.AnswerCapsule = AuditAnswerCapsules(pageContent);

        // 6.12 段落长度分析
        result.ParagraphLength = AuditParagraphLength(pageContent);

        // 6.13 标题层级检测
        result.HeadingStructure = AuditHeadingStructure(pageContent);

        // 6.14 Schema 完整度检测
        result.Schema = AuditSchema(pageContent);

        // 6.15 Meta 标签检测
        result.MetaTags = AuditMetaTags(pageContent);

        // Calculate content score
        result.Score = CalculateContentScore(result);

        return result;
    }

    private AnswerCapsuleAudit AuditAnswerCapsules(string pageContent)
    {
        var result = new AnswerCapsuleAudit();

        // Find H2 headings and check for answer capsules (40-60 words after H2)
        var h2Pattern = @"<h2[^>]*>(.*?)</h2>";
        var h2Matches = Regex.Matches(pageContent, h2Pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in h2Matches)
        {
            var headingText = Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "").Trim();
            var afterH2Index = match.Index + match.Length;

            // Get content after H2 until next heading or 500 chars
            var remainingContent = pageContent.Substring(afterH2Index);
            var nextHeadingMatch = Regex.Match(remainingContent, @"<h[1-6]", RegexOptions.IgnoreCase);
            var contentLength = nextHeadingMatch.Success ? Math.Min(nextHeadingMatch.Index, 1000) : 1000;
            var contentAfterH2 = remainingContent.Substring(0, Math.Min(contentLength, remainingContent.Length));

            // Extract first paragraph
            var firstParagraph = Regex.Match(contentAfterH2, @"<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (firstParagraph.Success)
            {
                var paragraphText = Regex.Replace(firstParagraph.Groups[1].Value, @"<[^>]+>", "").Trim();
                var wordCount = paragraphText.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                var capsule = new AnswerCapsuleInfo
                {
                    HeadingText = headingText,
                    WordCount = wordCount,
                    IsOptimalLength = wordCount >= 40 && wordCount <= 60,
                    FirstSentence = paragraphText.Length > 100 ? paragraphText.Substring(0, 100) + "..." : paragraphText
                };

                result.Capsules.Add(capsule);

                if (capsule.IsOptimalLength)
                {
                    result.CapsuleCount++;
                }
            }
        }

        result.HasAnswerCapsules = result.CapsuleCount > 0;
        result.CoveragePercentage = h2Matches.Count > 0 ? (double)result.CapsuleCount / h2Matches.Count * 100 : 0;

        if (result.CoveragePercentage < 50)
        {
            result.Issues.Add($"仅 {result.CoveragePercentage:F0}% 的 H2 标题后有最佳长度的答案胶囊 (40-60 词)");
        }

        return result;
    }

    private ParagraphLengthAudit AuditParagraphLength(string pageContent)
    {
        var result = new ParagraphLengthAudit();

        var paragraphPattern = @"<p[^>]*>(.*?)</p>";
        var matches = Regex.Matches(pageContent, paragraphPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var totalWords = 0;

        foreach (Match match in matches)
        {
            var text = Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "").Trim();
            var wordCount = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            if (wordCount < 10) continue; // Skip very short paragraphs

            result.TotalParagraphs++;
            totalWords += wordCount;

            if (wordCount >= 120 && wordCount <= 180)
            {
                result.OptimalLengthCount++;
            }
            else if (wordCount < 50)
            {
                result.TooShortCount++;
            }
            else if (wordCount > 250)
            {
                result.TooLongCount++;
            }
        }

        result.AverageLength = result.TotalParagraphs > 0 ? (double)totalWords / result.TotalParagraphs : 0;
        result.OptimalPercentage = result.TotalParagraphs > 0 ? (double)result.OptimalLengthCount / result.TotalParagraphs * 100 : 0;

        if (result.OptimalPercentage < 30)
        {
            result.Issues.Add($"仅 {result.OptimalPercentage:F0}% 的段落在最佳长度范围 (120-180 词)");
        }

        return result;
    }

    private HeadingStructureAudit AuditHeadingStructure(string pageContent)
    {
        var result = new HeadingStructureAudit();
        var order = 0;

        for (int level = 1; level <= 6; level++)
        {
            var pattern = $@"<h{level}[^>]*>(.*?)</h{level}>";
            var matches = Regex.Matches(pageContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var text = Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "").Trim();
                result.Headings.Add(new HeadingInfo
                {
                    Level = $"H{level}",
                    Text = text.Length > 100 ? text.Substring(0, 100) + "..." : text,
                    Order = order++
                });

                switch (level)
                {
                    case 1: result.H1Count++; break;
                    case 2: result.H2Count++; break;
                    case 3: result.H3Count++; break;
                    case 4: result.H4Count++; break;
                }
            }
        }

        result.HasH1 = result.H1Count > 0;

        // Check hierarchy
        result.HasProperHierarchy = result.H1Count == 1 && result.H2Count > 0;

        if (result.H1Count == 0)
        {
            result.Issues.Add("缺少 H1 标题");
        }
        else if (result.H1Count > 1)
        {
            result.Issues.Add($"有 {result.H1Count} 个 H1 标题，应该只有 1 个");
        }

        if (result.H2Count == 0)
        {
            result.Issues.Add("缺少 H2 标题，建议添加以改善内容结构");
        }

        return result;
    }

    private SchemaAudit AuditSchema(string pageContent)
    {
        var result = new SchemaAudit();

        // Find JSON-LD scripts
        var jsonLdPattern = @"<script[^>]+type=[""']application/ld\+json[""'][^>]*>(.*?)</script>";
        var matches = Regex.Matches(pageContent, jsonLdPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            result.HasSchema = true;
            var jsonContent = match.Groups[1].Value.Trim();

            // Simple type detection
            if (jsonContent.Contains("\"@type\""))
            {
                var typeMatch = Regex.Match(jsonContent, @"""@type""\s*:\s*""([^""]+)""");
                if (typeMatch.Success)
                {
                    var schemaType = typeMatch.Groups[1].Value;
                    result.Schemas.Add(new SchemaInfo
                    {
                        Type = schemaType,
                        IsValid = true
                    });

                    if (schemaType.Contains("Article")) result.HasArticleSchema = true;
                    if (schemaType.Contains("FAQ")) result.HasFAQSchema = true;
                    if (schemaType.Contains("Organization")) result.HasOrganizationSchema = true;
                }
            }
        }

        if (!result.HasSchema)
        {
            result.Issues.Add("缺少结构化数据 (JSON-LD)");
        }

        if (!result.HasArticleSchema && !result.HasFAQSchema)
        {
            result.Issues.Add("建议添加 Article 或 FAQ Schema 以提高 AI 引用率");
        }

        return result;
    }

    private MetaTagsAudit AuditMetaTags(string pageContent)
    {
        var result = new MetaTagsAudit();

        // Title
        var titleMatch = Regex.Match(pageContent, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (titleMatch.Success)
        {
            result.HasTitle = true;
            result.Title = titleMatch.Groups[1].Value.Trim();
            result.TitleLength = result.Title.Length;
            result.TitleOptimal = result.TitleLength <= 60;

            if (!result.TitleOptimal)
            {
                result.Issues.Add($"标题过长 ({result.TitleLength} 字符)，建议不超过 60 字符");
            }
        }
        else
        {
            result.Issues.Add("缺少 title 标签");
        }

        // Description
        var descMatch = Regex.Match(pageContent, @"<meta[^>]+name=[""']description[""'][^>]+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
        if (!descMatch.Success)
        {
            descMatch = Regex.Match(pageContent, @"<meta[^>]+content=[""']([^""']+)[""'][^>]+name=[""']description[""']", RegexOptions.IgnoreCase);
        }

        if (descMatch.Success)
        {
            result.HasDescription = true;
            result.Description = descMatch.Groups[1].Value.Trim();
            result.DescriptionLength = result.Description.Length;
            result.DescriptionOptimal = result.DescriptionLength <= 160;

            if (!result.DescriptionOptimal)
            {
                result.Issues.Add($"描述过长 ({result.DescriptionLength} 字符)，建议不超过 160 字符");
            }
        }
        else
        {
            result.Issues.Add("缺少 meta description");
        }

        // OG tags
        result.HasOgTags = Regex.IsMatch(pageContent, @"<meta[^>]+property=[""']og:", RegexOptions.IgnoreCase);
        if (!result.HasOgTags)
        {
            result.Issues.Add("缺少 Open Graph 标签");
        }

        // Twitter cards
        result.HasTwitterCards = Regex.IsMatch(pageContent, @"<meta[^>]+name=[""']twitter:", RegexOptions.IgnoreCase);

        return result;
    }

    private int CalculateContentScore(ContentAuditResult result)
    {
        var score = 100;

        // Answer capsules (25 points)
        if (!result.AnswerCapsule.HasAnswerCapsules) score -= 15;
        if (result.AnswerCapsule.CoveragePercentage < 50) score -= 10;

        // Paragraph length (20 points)
        if (result.ParagraphLength.OptimalPercentage < 30) score -= 20;

        // Heading structure (20 points)
        if (!result.HeadingStructure.HasH1) score -= 10;
        if (!result.HeadingStructure.HasProperHierarchy) score -= 10;

        // Schema (20 points)
        if (!result.Schema.HasSchema) score -= 15;
        if (!result.Schema.HasArticleSchema && !result.Schema.HasFAQSchema) score -= 5;

        // Meta tags (15 points)
        if (!result.MetaTags.HasTitle) score -= 5;
        if (!result.MetaTags.HasDescription) score -= 5;
        if (!result.MetaTags.TitleOptimal) score -= 3;
        if (!result.MetaTags.DescriptionOptimal) score -= 2;

        return Math.Max(0, score);
    }

    #endregion

    #region E-E-A-T Audit (6.16)

    private EEATAuditResult AuditEEAT(string pageContent, string baseUrl)
    {
        var result = new EEATAuditResult();

        // Author info
        var authorPatterns = new[]
        {
            @"<[^>]+class=[""'][^""']*author[^""']*[""'][^>]*>",
            @"<[^>]+rel=[""']author[""'][^>]*>",
            @"written\s+by",
            @"作者[：:]"
        };

        foreach (var pattern in authorPatterns)
        {
            if (Regex.IsMatch(pageContent, pattern, RegexOptions.IgnoreCase))
            {
                result.HasAuthorInfo = true;
                break;
            }
        }

        // Publish/Update dates
        var datePatterns = new[]
        {
            @"<time[^>]+datetime=[""']([^""']+)[""']",
            @"published[^>]*>([^<]+)</",
            @"发布[日时]期[：:]",
            @"更新[日时]期[：:]"
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(pageContent, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.HasPublishDate = true;
                break;
            }
        }

        // Citations and external links
        var linkPattern = @"<a[^>]+href=[""'](https?://[^""']+)[""']";
        var linkMatches = Regex.Matches(pageContent, linkPattern, RegexOptions.IgnoreCase);
        var uri = new Uri(baseUrl);

        foreach (Match match in linkMatches)
        {
            var href = match.Groups[1].Value;
            if (!href.Contains(uri.Host))
            {
                result.ExternalLinkCount++;
            }
        }

        result.HasExternalLinks = result.ExternalLinkCount > 0;
        result.HasCitations = result.ExternalLinkCount >= 3;
        result.CitationCount = result.ExternalLinkCount;

        // Calculate E-E-A-T score
        result.Score = CalculateEEATScore(result);

        // Generate issues
        if (!result.HasAuthorInfo)
        {
            result.Issues.Add("缺少作者信息");
            result.Recommendations.Add("添加作者姓名、简介和资质信息");
        }

        if (!result.HasPublishDate)
        {
            result.Issues.Add("缺少发布日期");
            result.Recommendations.Add("添加发布日期和最后更新日期");
        }

        if (!result.HasCitations)
        {
            result.Issues.Add("外部引用不足");
            result.Recommendations.Add("添加权威来源的引用链接，建议至少 3 个");
        }

        return result;
    }

    private int CalculateEEATScore(EEATAuditResult result)
    {
        var score = 100;

        if (!result.HasAuthorInfo) score -= 25;
        if (!result.HasPublishDate) score -= 20;
        if (!result.HasCitations) score -= 20;
        if (result.ExternalLinkCount < 3) score -= 15;
        if (!result.HasAuthorBio) score -= 10;
        if (!result.HasUpdateDate) score -= 10;

        return Math.Max(0, score);
    }

    #endregion

    #region Score Calculation

    private void CalculateOverallScore(SiteAuditResult result)
    {
        // Weighted average: Technical 40%, Content 40%, E-E-A-T 20%
        result.OverallScore = (int)(
            result.Technical.Score * 0.4 +
            result.Content.Score * 0.4 +
            result.EEAT.Score * 0.2
        );

        result.Grade = result.OverallScore switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };
    }

    private void CollectIssues(SiteAuditResult result)
    {
        // Technical issues
        foreach (var issue in result.Technical.RobotsTxt.Issues)
        {
            result.Issues.Add(CreateIssue("Technical", "High", "robots.txt", issue));
        }
        foreach (var issue in result.Technical.JSRendering.Issues)
        {
            result.Issues.Add(CreateIssue("Technical", "Critical", "JS 渲染", issue));
        }
        foreach (var issue in result.Technical.HttpsCanonical.Issues)
        {
            result.Issues.Add(CreateIssue("Technical", "Medium", "HTTPS/Canonical", issue));
        }
        foreach (var issue in result.Technical.Sitemap.Issues)
        {
            result.Issues.Add(CreateIssue("Technical", "Medium", "Sitemap", issue));
        }
        foreach (var issue in result.Technical.LlmsTxt.Issues)
        {
            result.Issues.Add(CreateIssue("Technical", "Low", "llms.txt", issue));
        }

        // Content issues
        foreach (var issue in result.Content.AnswerCapsule.Issues)
        {
            result.Issues.Add(CreateIssue("Content", "High", "答案胶囊", issue));
        }
        foreach (var issue in result.Content.ParagraphLength.Issues)
        {
            result.Issues.Add(CreateIssue("Content", "Medium", "段落长度", issue));
        }
        foreach (var issue in result.Content.HeadingStructure.Issues)
        {
            result.Issues.Add(CreateIssue("Content", "Medium", "标题结构", issue));
        }
        foreach (var issue in result.Content.Schema.Issues)
        {
            result.Issues.Add(CreateIssue("Content", "High", "Schema", issue));
        }
        foreach (var issue in result.Content.MetaTags.Issues)
        {
            result.Issues.Add(CreateIssue("Content", "Medium", "Meta 标签", issue));
        }

        // E-E-A-T issues
        foreach (var issue in result.EEAT.Issues)
        {
            result.Issues.Add(CreateIssue("EEAT", "High", "E-E-A-T", issue));
        }

        // Sort by severity
        result.Issues = result.Issues.OrderByDescending(i => i.Severity switch
        {
            "Critical" => 4,
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        }).ToList();
    }

    private AuditIssue CreateIssue(string category, string severity, string title, string description)
    {
        return new AuditIssue
        {
            Category = category,
            Severity = severity,
            Title = title,
            Description = description,
            ImpactScore = severity switch
            {
                "Critical" => 10,
                "High" => 7,
                "Medium" => 4,
                "Low" => 2,
                _ => 1
            }
        };
    }

    private void GenerateRecommendations(SiteAuditResult result)
    {
        if (!result.Technical.RobotsTxt.AllAICrawlersAllowed)
        {
            result.Recommendations.Add("更新 robots.txt 允许主要 AI 爬虫访问");
        }

        if (result.Technical.JSRendering.CriticalContentInJS)
        {
            result.Recommendations.Add("使用服务端渲染 (SSR) 确保 AI 爬虫可以访问内容");
        }

        if (!result.Technical.LlmsTxt.Exists)
        {
            result.Recommendations.Add("创建 llms.txt 文件帮助 AI 爬虫发现重要内容");
        }

        if (result.Content.AnswerCapsule.CoveragePercentage < 50)
        {
            result.Recommendations.Add("在每个 H2 标题后添加 40-60 词的直接答案");
        }

        if (result.Content.ParagraphLength.OptimalPercentage < 30)
        {
            result.Recommendations.Add("调整段落长度到 120-180 词以提高 AI 引用率");
        }

        if (!result.Content.Schema.HasSchema)
        {
            result.Recommendations.Add("添加 Article 或 FAQ Schema 结构化数据");
        }

        foreach (var rec in result.EEAT.Recommendations)
        {
            result.Recommendations.Add(rec);
        }
    }

    #endregion

    #region Quick Index (6.17-6.20)

    public async Task<IndexNowResult> SubmitToIndexNowAsync(IndexNowSubmitRequest request)
    {
        var result = new IndexNowResult
        {
            SearchEngines = new List<string> { "Bing", "Yandex", "Naver" }
        };

        var apiKey = request.ApiKey ?? "your-indexnow-key";

        foreach (var url in request.Urls)
        {
            try
            {
                var uri = new Uri(url);
                var indexNowUrl = $"https://api.indexnow.org/indexnow?url={Uri.EscapeDataString(url)}&key={apiKey}";

                var response = await _httpClient.GetAsync(indexNowUrl);
                if (response.IsSuccessStatusCode)
                {
                    result.SubmittedUrls.Add(url);
                }
                else
                {
                    result.FailedUrls.Add(url);
                }
            }
            catch (Exception ex)
            {
                result.FailedUrls.Add(url);
                _logger.LogWarning(ex, "Failed to submit {Url} to IndexNow", url);
            }
        }

        result.Success = result.FailedUrls.Count == 0;
        result.SubmittedCount = result.SubmittedUrls.Count;

        return result;
    }

    public SitemapGenerateResult GenerateSitemap(SitemapGenerateRequest request)
    {
        var result = new SitemapGenerateResult();

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            foreach (var url in request.Urls)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(url)}</loc>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <changefreq>{request.ChangeFreq}</changefreq>");
                sb.AppendLine($"    <priority>{request.Priority:F1}</priority>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");

            result.Success = true;
            result.XmlContent = sb.ToString();
            result.UrlCount = request.Urls.Count;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public RobotsTxtGenerateResult GenerateRobotsTxt(RobotsTxtGenerateRequest request)
    {
        var result = new RobotsTxtGenerateResult();
        var sb = new StringBuilder();

        // Default user-agent
        sb.AppendLine("User-agent: *");
        foreach (var path in request.DisallowPaths)
        {
            sb.AppendLine($"Disallow: {path}");
            result.DisallowedPaths.Add(path);
        }
        foreach (var path in request.AllowPaths)
        {
            sb.AppendLine($"Allow: {path}");
        }
        sb.AppendLine();

        // AI crawlers
        if (request.AllowAllAICrawlers)
        {
            foreach (var crawler in KnownAICrawlers)
            {
                sb.AppendLine($"User-agent: {crawler.UserAgent}");
                sb.AppendLine("Allow: /");
                sb.AppendLine();
                result.AllowedCrawlers.Add(crawler.Name);
            }
        }

        // Sitemap
        sb.AppendLine($"Sitemap: {request.BaseUrl}/sitemap.xml");

        result.Success = true;
        result.Content = sb.ToString();

        return result;
    }

    public LlmsTxtGenerateResult GenerateLlmsTxt(List<string> urls, string baseUrl)
    {
        var result = new LlmsTxtGenerateResult();
        var sb = new StringBuilder();

        sb.AppendLine($"# {baseUrl}");
        sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("# Priority content for LLM crawlers");
        sb.AppendLine();

        foreach (var url in urls)
        {
            sb.AppendLine(url);
        }

        result.Success = true;
        result.Content = sb.ToString();
        result.EntryCount = urls.Count;

        return result;
    }

    #endregion

    #region Helpers

    private async Task<string> FetchPageContentAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; GeoCore-Audit/1.0)");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    #endregion
}
