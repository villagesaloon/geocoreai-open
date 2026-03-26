using System.Text;
using System.Xml.Linq;

namespace GeoCore.SaaS.Services.LlmsTxt;

/// <summary>
/// llms.txt 生成服务
/// 功能 4.49：从 sitemap 生成 llms.txt，按优先级排序
/// 参考：https://llmstxt.org/
/// </summary>
public class LlmsTxtService
{
    private readonly ILogger<LlmsTxtService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public LlmsTxtService(
        ILogger<LlmsTxtService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 生成 llms.txt 文件内容
    /// </summary>
    public async Task<LlmsTxtGenerateResult> GenerateAsync(LlmsTxtGenerateRequest request)
    {
        _logger.LogInformation("[LlmsTxt] Generating llms.txt for {Url}", request.WebsiteUrl);

        var result = new LlmsTxtGenerateResult
        {
            WebsiteUrl = request.WebsiteUrl,
            GeneratedAt = DateTime.UtcNow
        };

        var pages = new List<LlmsTxtPage>();

        // 1. 从 sitemap 获取页面
        var sitemapUrl = request.SitemapUrl ?? await DiscoverSitemapUrlAsync(request.WebsiteUrl);
        if (!string.IsNullOrEmpty(sitemapUrl))
        {
            var sitemapResult = await ParseSitemapAsync(sitemapUrl);
            if (sitemapResult.Success)
            {
                result.Stats.SitemapPagesFound = sitemapResult.Entries.Count;
                pages.AddRange(ConvertSitemapEntriesToPages(sitemapResult.Entries));
            }
        }

        // 2. 添加手动指定的页面
        if (request.ImportantPages?.Any() == true)
        {
            result.Stats.ManualPagesAdded = request.ImportantPages.Count;
            foreach (var page in request.ImportantPages)
            {
                var existing = pages.FirstOrDefault(p => p.Url == page.Url);
                if (existing != null)
                {
                    // 更新已存在的页面信息
                    existing.Title = page.Title ?? existing.Title;
                    existing.Description = page.Description ?? existing.Description;
                    existing.Priority = Math.Max(existing.Priority, page.Priority);
                    existing.PageType = page.PageType;
                    existing.Keywords = page.Keywords;
                }
                else
                {
                    pages.Add(page);
                }
            }
        }

        // 3. 推断页面类型和优先级
        pages = InferPageMetadata(pages, request.WebsiteUrl);

        // 4. 按优先级排序并限制数量
        pages = pages
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Url)
            .Take(request.MaxPages)
            .ToList();

        result.Pages = pages;
        result.PageCount = pages.Count;
        result.Stats.FinalPageCount = pages.Count;

        // 5. 计算页面类型分布
        result.Stats.PageTypeDistribution = pages
            .GroupBy(p => p.PageType)
            .ToDictionary(g => g.Key, g => g.Count());

        // 6. 生成 llms.txt 内容
        result.Content = GenerateLlmsTxtContent(request, pages);
        result.Stats.FileSizeBytes = Encoding.UTF8.GetByteCount(result.Content);

        // 7. 生成部署说明
        result.DeploymentInstructions = GenerateDeploymentInstructions(request.WebsiteUrl);

        _logger.LogInformation("[LlmsTxt] Generated llms.txt with {PageCount} pages, {Size} bytes",
            result.PageCount, result.Stats.FileSizeBytes);

        return result;
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

            // 常见的 sitemap 位置
            var possibleUrls = new[]
            {
                $"{baseUrl}/sitemap.xml",
                $"{baseUrl}/sitemap_index.xml",
                $"{baseUrl}/sitemap/sitemap.xml"
            };

            var client = _httpClientFactory.CreateClient("WebScraper");

            foreach (var url in possibleUrls)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[LlmsTxt] Found sitemap at {Url}", url);
                        return url;
                    }
                }
                catch
                {
                    // 继续尝试下一个
                }
            }

            // 尝试从 robots.txt 获取
            try
            {
                var robotsUrl = $"{baseUrl}/robots.txt";
                var robotsResponse = await client.GetAsync(robotsUrl);
                if (robotsResponse.IsSuccessStatusCode)
                {
                    var content = await robotsResponse.Content.ReadAsStringAsync();
                    var lines = content.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                        {
                            var sitemapUrl = line.Substring(8).Trim();
                            _logger.LogInformation("[LlmsTxt] Found sitemap in robots.txt: {Url}", sitemapUrl);
                            return sitemapUrl;
                        }
                    }
                }
            }
            catch
            {
                // 忽略
            }

            _logger.LogWarning("[LlmsTxt] No sitemap found for {Url}", websiteUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LlmsTxt] Error discovering sitemap for {Url}", websiteUrl);
            return null;
        }
    }

    /// <summary>
    /// 解析 sitemap
    /// </summary>
    private async Task<SitemapParseResult> ParseSitemapAsync(string sitemapUrl)
    {
        var result = new SitemapParseResult();

        try
        {
            var client = _httpClientFactory.CreateClient("WebScraper");
            var response = await client.GetAsync(sitemapUrl);

            if (!response.IsSuccessStatusCode)
            {
                result.Error = $"Failed to fetch sitemap: {response.StatusCode}";
                return result;
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(content);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // 检查是否是 sitemap index
            var sitemapIndexElements = doc.Descendants(ns + "sitemap").ToList();
            if (sitemapIndexElements.Any())
            {
                // 这是一个 sitemap index，递归解析子 sitemap
                foreach (var sitemapElement in sitemapIndexElements.Take(5)) // 限制子 sitemap 数量
                {
                    var loc = sitemapElement.Element(ns + "loc")?.Value;
                    if (!string.IsNullOrEmpty(loc))
                    {
                        var subResult = await ParseSitemapAsync(loc);
                        if (subResult.Success)
                        {
                            result.Entries.AddRange(subResult.Entries);
                        }
                    }
                }
            }
            else
            {
                // 这是一个普通 sitemap
                var urlElements = doc.Descendants(ns + "url").ToList();
                foreach (var urlElement in urlElements)
                {
                    var entry = new SitemapEntry
                    {
                        Url = urlElement.Element(ns + "loc")?.Value ?? ""
                    };

                    var lastmod = urlElement.Element(ns + "lastmod")?.Value;
                    if (DateTime.TryParse(lastmod, out var lastModDate))
                    {
                        entry.LastModified = lastModDate;
                    }

                    entry.ChangeFrequency = urlElement.Element(ns + "changefreq")?.Value;

                    var priority = urlElement.Element(ns + "priority")?.Value;
                    if (double.TryParse(priority, out var priorityValue))
                    {
                        entry.Priority = priorityValue;
                    }

                    if (!string.IsNullOrEmpty(entry.Url))
                    {
                        result.Entries.Add(entry);
                    }
                }
            }

            result.Success = true;
            _logger.LogInformation("[LlmsTxt] Parsed sitemap with {Count} entries", result.Entries.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LlmsTxt] Error parsing sitemap {Url}", sitemapUrl);
            result.Error = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 将 sitemap 条目转换为 llms.txt 页面
    /// </summary>
    private List<LlmsTxtPage> ConvertSitemapEntriesToPages(List<SitemapEntry> entries)
    {
        return entries.Select(e => new LlmsTxtPage
        {
            Url = e.Url,
            LastModified = e.LastModified,
            Priority = e.Priority.HasValue ? (int)(e.Priority.Value * 10) : 5
        }).ToList();
    }

    /// <summary>
    /// 推断页面元数据
    /// </summary>
    private List<LlmsTxtPage> InferPageMetadata(List<LlmsTxtPage> pages, string websiteUrl)
    {
        var uri = new Uri(websiteUrl);
        var baseHost = uri.Host;

        foreach (var page in pages)
        {
            try
            {
                var pageUri = new Uri(page.Url);
                var path = pageUri.AbsolutePath.ToLower();

                // 推断页面类型
                page.PageType = InferPageType(path);

                // 根据页面类型调整优先级
                page.Priority = AdjustPriorityByType(page.Priority, page.PageType, path);

                // 从 URL 推断标题（如果没有）
                if (string.IsNullOrEmpty(page.Title))
                {
                    page.Title = InferTitleFromUrl(path);
                }
            }
            catch
            {
                // 保持原样
            }
        }

        return pages;
    }

    /// <summary>
    /// 推断页面类型
    /// </summary>
    private string InferPageType(string path)
    {
        if (path == "/" || path == "/index.html" || path == "/index.htm")
            return "homepage";

        if (path.Contains("/product") || path.Contains("/shop") || path.Contains("/item"))
            return "product";

        if (path.Contains("/blog") || path.Contains("/article") || path.Contains("/post") || path.Contains("/news"))
            return "blog";

        if (path.Contains("/doc") || path.Contains("/guide") || path.Contains("/tutorial") || path.Contains("/help"))
            return "documentation";

        if (path.Contains("/faq") || path.Contains("/question"))
            return "faq";

        if (path.Contains("/about") || path.Contains("/team") || path.Contains("/company"))
            return "about";

        if (path.Contains("/contact") || path.Contains("/support"))
            return "contact";

        if (path.Contains("/pricing") || path.Contains("/plan"))
            return "pricing";

        if (path.Contains("/feature") || path.Contains("/service"))
            return "features";

        return "other";
    }

    /// <summary>
    /// 根据页面类型调整优先级
    /// </summary>
    private int AdjustPriorityByType(int basePriority, string pageType, string path)
    {
        var adjustment = pageType switch
        {
            "homepage" => 5,
            "features" => 3,
            "pricing" => 3,
            "documentation" => 2,
            "faq" => 2,
            "product" => 1,
            "blog" => 0,
            "about" => 0,
            "contact" => -1,
            _ => 0
        };

        // 首页始终最高优先级
        if (path == "/" || path == "/index.html")
        {
            return 10;
        }

        return Math.Max(1, Math.Min(10, basePriority + adjustment));
    }

    /// <summary>
    /// 从 URL 推断标题
    /// </summary>
    private string InferTitleFromUrl(string path)
    {
        if (path == "/" || path == "/index.html")
            return "Homepage";

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return "Page";

        var lastSegment = segments.Last();
        // 移除文件扩展名
        lastSegment = System.IO.Path.GetFileNameWithoutExtension(lastSegment);
        // 转换连字符和下划线为空格
        lastSegment = lastSegment.Replace("-", " ").Replace("_", " ");
        // 首字母大写
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lastSegment);
    }

    /// <summary>
    /// 生成 llms.txt 内容
    /// </summary>
    private string GenerateLlmsTxtContent(LlmsTxtGenerateRequest request, List<LlmsTxtPage> pages)
    {
        var sb = new StringBuilder();

        // 头部信息
        sb.AppendLine("# llms.txt");
        sb.AppendLine($"# Generated by GeoCore AI - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"# Website: {request.WebsiteUrl}");
        sb.AppendLine();

        // 网站信息
        if (!string.IsNullOrEmpty(request.SiteTitle))
        {
            sb.AppendLine($"# {request.SiteTitle}");
        }
        if (!string.IsNullOrEmpty(request.SiteDescription))
        {
            sb.AppendLine($"> {request.SiteDescription}");
            sb.AppendLine();
        }

        // 按类型分组输出
        var groupedPages = pages.GroupBy(p => p.PageType).OrderBy(g => GetTypeOrder(g.Key));

        foreach (var group in groupedPages)
        {
            sb.AppendLine($"## {GetTypeName(group.Key)}");
            sb.AppendLine();

            foreach (var page in group.OrderByDescending(p => p.Priority))
            {
                if (request.Format == "extended")
                {
                    // 扩展格式：包含更多信息
                    sb.AppendLine($"- [{page.Title ?? InferTitleFromUrl(new Uri(page.Url).AbsolutePath)}]({page.Url})");
                    if (!string.IsNullOrEmpty(page.Description))
                    {
                        sb.AppendLine($"  > {page.Description}");
                    }
                    if (page.Keywords?.Any() == true)
                    {
                        sb.AppendLine($"  Keywords: {string.Join(", ", page.Keywords)}");
                    }
                }
                else
                {
                    // 标准格式：简洁
                    var title = page.Title ?? InferTitleFromUrl(new Uri(page.Url).AbsolutePath);
                    sb.AppendLine($"- [{title}]({page.Url})");
                }
            }

            sb.AppendLine();
        }

        // 页脚
        sb.AppendLine("---");
        sb.AppendLine($"# Total pages: {pages.Count}");
        sb.AppendLine("# For more information about llms.txt, visit: https://llmstxt.org/");

        return sb.ToString();
    }

    /// <summary>
    /// 获取类型排序顺序
    /// </summary>
    private int GetTypeOrder(string pageType)
    {
        return pageType switch
        {
            "homepage" => 0,
            "features" => 1,
            "pricing" => 2,
            "product" => 3,
            "documentation" => 4,
            "faq" => 5,
            "blog" => 6,
            "about" => 7,
            "contact" => 8,
            _ => 9
        };
    }

    /// <summary>
    /// 获取类型显示名称
    /// </summary>
    private string GetTypeName(string pageType)
    {
        return pageType switch
        {
            "homepage" => "Homepage",
            "features" => "Features & Services",
            "pricing" => "Pricing",
            "product" => "Products",
            "documentation" => "Documentation & Guides",
            "faq" => "FAQ",
            "blog" => "Blog & Articles",
            "about" => "About",
            "contact" => "Contact & Support",
            _ => "Other Pages"
        };
    }

    /// <summary>
    /// 生成部署说明
    /// </summary>
    private List<string> GenerateDeploymentInstructions(string websiteUrl)
    {
        var uri = new Uri(websiteUrl);
        return new List<string>
        {
            $"1. 将 llms.txt 文件保存到网站根目录：{uri.Scheme}://{uri.Host}/llms.txt",
            "2. 确保文件可以通过 HTTP 访问（返回 200 状态码）",
            "3. 在 robots.txt 中添加引用（可选）：",
            "   # llms.txt",
            $"   # See: {uri.Scheme}://{uri.Host}/llms.txt",
            "4. 定期更新 llms.txt 以反映网站结构变化",
            "5. 考虑在 sitemap.xml 中包含 llms.txt 的引用"
        };
    }

    #region 7.15 llms.txt 行业模板库

    private static readonly Dictionary<string, LlmsTxtIndustryTemplate> IndustryTemplates = new()
    {
        ["saas"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "saas",
            IndustryName = "SaaS / 软件服务",
            RequiredSections = new() { "Company Overview", "Products", "Pricing", "Documentation", "API Reference" },
            RecommendedSections = new() { "Use Cases", "Integrations", "Security", "Changelog", "Status Page" },
            Examples = new() { "Stripe", "Notion", "Figma" }
        },
        ["ecommerce"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "ecommerce",
            IndustryName = "电子商务",
            RequiredSections = new() { "Company Overview", "Product Categories", "Shipping Info", "Return Policy", "Customer Service" },
            RecommendedSections = new() { "Size Guide", "Gift Cards", "Loyalty Program", "Store Locator" },
            Examples = new() { "Amazon", "Shopify stores" }
        },
        ["content"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "content",
            IndustryName = "内容/媒体",
            RequiredSections = new() { "About", "Content Categories", "Editorial Guidelines", "Contact" },
            RecommendedSections = new() { "Authors", "Newsletter", "Podcast", "Video Content" },
            Examples = new() { "Medium", "Substack" }
        },
        ["b2b"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "b2b",
            IndustryName = "B2B 企业服务",
            RequiredSections = new() { "Company Overview", "Solutions", "Industries Served", "Case Studies", "Contact Sales" },
            RecommendedSections = new() { "Partners", "Resources", "Webinars", "White Papers" },
            Examples = new() { "Salesforce", "HubSpot" }
        },
        ["healthcare"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "healthcare",
            IndustryName = "医疗健康",
            RequiredSections = new() { "About", "Services", "Providers", "Patient Resources", "Contact" },
            RecommendedSections = new() { "Insurance", "Appointments", "Health Library", "Telehealth" },
            Examples = new() { "Mayo Clinic", "WebMD" }
        },
        ["finance"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "finance",
            IndustryName = "金融服务",
            RequiredSections = new() { "Company Overview", "Products", "Security", "Compliance", "Contact" },
            RecommendedSections = new() { "Rates", "Calculator Tools", "Educational Resources", "Mobile App" },
            Examples = new() { "Stripe", "Square" }
        },
        ["education"] = new LlmsTxtIndustryTemplate
        {
            IndustryCode = "education",
            IndustryName = "教育",
            RequiredSections = new() { "About", "Courses", "Instructors", "Pricing", "Support" },
            RecommendedSections = new() { "Certifications", "Community", "Blog", "Free Resources" },
            Examples = new() { "Coursera", "Udemy" }
        }
    };

    /// <summary>
    /// 获取所有支持的行业模板
    /// </summary>
    public List<LlmsTxtIndustryTemplate> GetAllIndustryTemplates()
    {
        return IndustryTemplates.Values.ToList();
    }

    /// <summary>
    /// 根据行业生成 llms.txt 模板
    /// </summary>
    public LlmsTxtIndustryTemplateResult GenerateIndustryTemplate(LlmsTxtIndustryTemplateRequest request)
    {
        _logger.LogInformation("[LlmsTxt] Generating industry template for {Industry}", request.Industry);

        var industryCode = request.Industry.ToLower();
        if (!IndustryTemplates.TryGetValue(industryCode, out var template))
        {
            template = IndustryTemplates["saas"]; // 默认使用 SaaS 模板
            industryCode = "saas";
        }

        var result = new LlmsTxtIndustryTemplateResult
        {
            Industry = industryCode,
            IndustryDisplayName = template.IndustryName,
            Sections = GenerateTemplateSections(template, request),
            BestPractices = GenerateBestPractices(industryCode),
            IndustrySpecificTips = GenerateIndustryTips(industryCode)
        };

        result.GeneratedLlmsTxt = BuildLlmsTxtFromSections(request, result.Sections);
        result.Validation = ValidateLlmsTxt(result.GeneratedLlmsTxt, template);

        return result;
    }

    private List<LlmsTxtTemplateSection> GenerateTemplateSections(LlmsTxtIndustryTemplate template, LlmsTxtIndustryTemplateRequest request)
    {
        var sections = new List<LlmsTxtTemplateSection>();
        var priority = 10;

        // 必需章节
        foreach (var sectionName in template.RequiredSections)
        {
            sections.Add(new LlmsTxtTemplateSection
            {
                Name = sectionName,
                Description = GetSectionDescription(sectionName),
                Content = GenerateSectionContent(sectionName, request),
                IsRequired = true,
                Priority = priority--
            });
        }

        // 推荐章节
        foreach (var sectionName in template.RecommendedSections)
        {
            sections.Add(new LlmsTxtTemplateSection
            {
                Name = sectionName,
                Description = GetSectionDescription(sectionName),
                Content = GenerateSectionContent(sectionName, request),
                IsRequired = false,
                Priority = priority--
            });
        }

        return sections;
    }

    private string GetSectionDescription(string sectionName)
    {
        return sectionName switch
        {
            "Company Overview" => "公司简介和核心价值主张",
            "Products" => "产品和服务列表",
            "Pricing" => "定价信息和套餐",
            "Documentation" => "产品文档和使用指南",
            "API Reference" => "API 文档和开发者资源",
            "Use Cases" => "使用场景和解决方案",
            "Integrations" => "第三方集成",
            "Security" => "安全和合规信息",
            "Product Categories" => "产品分类",
            "Shipping Info" => "配送信息",
            "Return Policy" => "退换货政策",
            "Customer Service" => "客户服务",
            "Solutions" => "解决方案",
            "Industries Served" => "服务行业",
            "Case Studies" => "客户案例",
            "Contact Sales" => "销售联系",
            _ => sectionName
        };
    }

    private string GenerateSectionContent(string sectionName, LlmsTxtIndustryTemplateRequest request)
    {
        var baseUrl = request.WebsiteUrl.TrimEnd('/');
        var slug = sectionName.ToLower().Replace(" ", "-").Replace("&", "and");

        return sectionName switch
        {
            "Company Overview" => $"- [{request.CompanyName}]({baseUrl}/): {request.CompanyDescription}",
            "Products" => string.Join("\n", request.KeyProducts.Select(p => $"- [{p}]({baseUrl}/products/{p.ToLower().Replace(" ", "-")})")),
            "Pricing" => $"- [Pricing]({baseUrl}/pricing): View our pricing plans",
            "Documentation" => $"- [Documentation]({baseUrl}/docs): Product documentation and guides",
            "API Reference" => $"- [API Reference]({baseUrl}/api): Developer API documentation",
            "Contact Sales" => $"- [Contact Sales]({baseUrl}/contact): Get in touch with our sales team",
            _ => $"- [{sectionName}]({baseUrl}/{slug})"
        };
    }

    private string BuildLlmsTxtFromSections(LlmsTxtIndustryTemplateRequest request, List<LlmsTxtTemplateSection> sections)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# {request.CompanyName}");
        sb.AppendLine();
        sb.AppendLine($"> {request.CompanyDescription}");
        sb.AppendLine();

        // Sections
        foreach (var section in sections.OrderByDescending(s => s.Priority))
        {
            sb.AppendLine($"## {section.Name}");
            sb.AppendLine();
            sb.AppendLine(section.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private LlmsTxtValidationResult ValidateLlmsTxt(string content, LlmsTxtIndustryTemplate template)
    {
        var result = new LlmsTxtValidationResult
        {
            RequiredElements = template.RequiredSections,
            MissingElements = new List<string>(),
            Warnings = new List<string>()
        };

        // 检查必需章节
        foreach (var section in template.RequiredSections)
        {
            if (!content.Contains($"## {section}"))
            {
                result.MissingElements.Add(section);
            }
        }

        // 检查基本结构
        if (!content.StartsWith("#"))
        {
            result.Warnings.Add("llms.txt 应以标题开头");
        }

        if (!content.Contains(">"))
        {
            result.Warnings.Add("建议添加公司描述（使用 > 引用格式）");
        }

        // 计算完整度
        var totalRequired = template.RequiredSections.Count;
        var presentRequired = totalRequired - result.MissingElements.Count;
        result.CompletenessScore = totalRequired > 0 ? (presentRequired * 100 / totalRequired) : 100;
        result.IsValid = result.MissingElements.Count == 0 && result.Warnings.Count == 0;

        return result;
    }

    private List<string> GenerateBestPractices(string industry)
    {
        var common = new List<string>
        {
            "保持 llms.txt 文件简洁，通常不超过 500 行",
            "使用 Markdown 格式，便于 AI 解析",
            "定期更新以反映网站变化",
            "将最重要的页面放在前面",
            "使用清晰的章节标题"
        };

        var industrySpecific = industry switch
        {
            "saas" => new List<string> { "包含 API 文档链接", "添加 changelog 链接", "包含状态页面链接" },
            "ecommerce" => new List<string> { "包含产品分类结构", "添加退换货政策", "包含配送信息" },
            "b2b" => new List<string> { "包含案例研究", "添加行业解决方案", "包含白皮书链接" },
            _ => new List<string>()
        };

        return common.Concat(industrySpecific).ToList();
    }

    private List<string> GenerateIndustryTips(string industry)
    {
        return industry switch
        {
            "saas" => new List<string>
            {
                "SaaS 产品应重点展示 API 文档和集成能力",
                "包含定价页面有助于 AI 回答价格相关问题",
                "技术文档是 AI 引用的重要来源"
            },
            "ecommerce" => new List<string>
            {
                "产品分类结构有助于 AI 理解商品组织",
                "退换货政策是常见的 AI 查询主题",
                "包含尺码指南等实用信息"
            },
            "b2b" => new List<string>
            {
                "案例研究是 B2B 决策的重要参考",
                "行业解决方案页面有助于精准匹配",
                "白皮书和研究报告增加权威性"
            },
            "healthcare" => new List<string>
            {
                "确保医疗信息准确且有来源",
                "包含预约和联系方式",
                "健康教育内容是高价值引用来源"
            },
            "finance" => new List<string>
            {
                "安全和合规信息是必需的",
                "包含计算器等实用工具链接",
                "费率信息应保持更新"
            },
            "education" => new List<string>
            {
                "课程目录是核心内容",
                "包含认证和证书信息",
                "免费资源有助于吸引 AI 引用"
            },
            _ => new List<string>
            {
                "根据行业特点定制内容",
                "关注用户常见问题",
                "保持信息准确和更新"
            }
        };
    }

    #endregion
}
