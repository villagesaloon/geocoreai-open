using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net.Http;
using HtmlAgilityPack;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// URL 分析控制器 - 智能预填充功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UrlAnalyzerController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UrlAnalyzerController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConfigCacheService _configCache;

    public UrlAnalyzerController(IHttpClientFactory httpClientFactory, ILogger<UrlAnalyzerController> logger, IConfiguration configuration, ConfigCacheService configCache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _configCache = configCache;
    }

    /// <summary>
    /// 分析 URL 并提取品牌信息
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeUrl([FromBody] UrlAnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { success = false, message = "URL 不能为空" });
        }

        try
        {
            // 验证 URL 格式
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
            {
                return BadRequest(new { success = false, message = "无效的 URL 格式" });
            }

            // 获取网页内容
            var client = _httpClientFactory.CreateClient("WebScraper");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");

            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                return Ok(new { success = false, message = $"无法访问该网页 (HTTP {(int)response.StatusCode})" });
            }

            var html = await response.Content.ReadAsStringAsync();

            // 使用 HtmlAgilityPack 解析 HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 提取页面信息（规则提取）
            var result = ExtractPageInfo(doc, uri);

            // 规则提取后，如果产品名或行业为空，用 AI 补充
            if (!string.IsNullOrEmpty(result.BrandName) && 
                (string.IsNullOrEmpty(result.ProductName) || string.IsNullOrEmpty(result.Industry)))
            {
                await EnrichWithAI(result);
            }

            _logger.LogInformation("URL 分析成功: {Url}, 品牌: {Brand}, 产品: {Product}, 行业: {Industry}", 
                request.Url, result.BrandName, result.ProductName, result.Industry);

            return Ok(new { success = true, data = result });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "URL 请求失败: {Url}", request.Url);
            return Ok(new { success = false, message = "网络请求失败，请检查 URL 是否正确" });
        }
        catch (TaskCanceledException)
        {
            return Ok(new { success = false, message = "请求超时，请稍后重试" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "URL 分析异常: {Url}", request.Url);
            return Ok(new { success = false, message = "分析过程中发生错误" });
        }
    }

    /// <summary>
    /// 从 HTML 文档中提取页面信息
    /// </summary>
    private UrlAnalyzeResult ExtractPageInfo(HtmlDocument doc, Uri uri)
    {
        var result = new UrlAnalyzeResult
        {
            Url = uri.ToString(),
            Domain = uri.Host
        };

        // 1. 提取 title
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        var pageTitle = titleNode?.InnerText?.Trim() ?? "";

        // 2. 提取 meta description
        var descNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
        result.Description = descNode?.GetAttributeValue("content", "")?.Trim() ?? "";

        // 3. 提取 Open Graph 信息
        var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "");
        var ogDesc = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", "");
        var ogSiteName = doc.DocumentNode.SelectSingleNode("//meta[@property='og:site_name']")?.GetAttributeValue("content", "");

        // 4. 提取 keywords
        var keywordsNode = doc.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
        var keywords = keywordsNode?.GetAttributeValue("content", "")?.Trim() ?? "";

        // 5. 智能推断品牌名称和产品名称
        var (brandName, productName) = InferBrandAndProductName(pageTitle, ogSiteName, ogTitle, uri);
        result.BrandName = brandName;
        result.ProductName = productName;

        // 6. 使用最佳描述
        if (string.IsNullOrEmpty(result.Description) && !string.IsNullOrEmpty(ogDesc))
        {
            result.Description = ogDesc;
        }

        // 7. 尝试推断行业（基于关键词和内容）
        result.Industry = InferIndustry(pageTitle, result.Description, keywords, uri.Host);

        // 8. 提取 H1 标题作为补充
        var h1Node = doc.DocumentNode.SelectSingleNode("//h1");
        result.Headline = h1Node?.InnerText?.Trim() ?? "";

        return result;
    }

    /// <summary>
    /// 智能推断品牌名称和产品名称
    /// 品牌名：公司/品牌（如 Apple、Tesla、Nike）
    /// 产品名：具体产品（如 iPhone 16 Pro、Model 3、Air Max）
    /// </summary>
    private (string brandName, string productName) InferBrandAndProductName(string pageTitle, string? ogSiteName, string? ogTitle, Uri uri)
    {
        string brandName = "";
        string productName = "";

        // 知名品牌映射表（域名 -> 品牌名）
        var knownBrands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["apple"] = "Apple", ["tesla"] = "Tesla", ["nike"] = "Nike",
            ["adidas"] = "Adidas", ["samsung"] = "Samsung", ["huawei"] = "Huawei",
            ["xiaomi"] = "Xiaomi", ["oppo"] = "OPPO", ["vivo"] = "vivo",
            ["google"] = "Google", ["microsoft"] = "Microsoft", ["amazon"] = "Amazon",
            ["meta"] = "Meta", ["openai"] = "OpenAI", ["anthropic"] = "Anthropic",
            ["bmw"] = "BMW", ["mercedes-benz"] = "Mercedes-Benz", ["audi"] = "Audi",
            ["nio"] = "NIO", ["xpeng"] = "XPeng", ["li"] = "Li Auto",
            ["starbucks"] = "Starbucks", ["mcdonald"] = "McDonald's", ["kfc"] = "KFC",
            ["netflix"] = "Netflix", ["spotify"] = "Spotify", ["youtube"] = "YouTube",
            ["alibaba"] = "Alibaba", ["taobao"] = "Taobao", ["jd"] = "JD.com",
            ["geocoreai"] = "GeoCore AI", ["geocore"] = "GeoCore AI",
        };

        // 1. 从域名识别知名品牌
        var host = uri.Host.ToLower();
        host = Regex.Replace(host, @"^www\.", "");
        var domainMain = host.Split('.')[0];
        
        if (knownBrands.TryGetValue(domainMain, out var knownBrand))
        {
            brandName = knownBrand;
        }

        // 2. 如果有 og:site_name，优先作为品牌名
        if (!string.IsNullOrWhiteSpace(ogSiteName))
        {
            var cleaned = ogSiteName.Trim();
            // 清理常见后缀（如 .com、.cn、.com.cn）
            cleaned = Regex.Replace(cleaned, @"\.(com|cn|net|org|io)(\.[a-z]{2})?$", "", RegexOptions.IgnoreCase).Trim();
            brandName = cleaned;
        }

        // 3. 从 title 中提取品牌和产品（常见格式：产品名 - 品牌名 | 品牌名）
        if (!string.IsNullOrWhiteSpace(pageTitle))
        {
            var separators = new[] { " - ", " | ", " – ", " — ", " :: ", " : " };
            foreach (var sep in separators)
            {
                if (pageTitle.Contains(sep))
                {
                    var parts = pageTitle.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        // 通常格式：产品名 - 品牌名
                        var firstPart = parts[0].Trim();
                        var lastPart = parts[^1].Trim();
                        
                        // 如果最后部分是已知品牌或较短，作为品牌名
                        if (lastPart.Length <= 30)
                        {
                            if (string.IsNullOrEmpty(brandName))
                            {
                                brandName = lastPart;
                            }
                            // 第一部分作为产品名（如果不同于品牌名）
                            if (firstPart.Length <= 50 && !firstPart.Equals(brandName, StringComparison.OrdinalIgnoreCase))
                            {
                                productName = firstPart;
                            }
                        }
                        break;
                    }
                }
            }

            // 如果没有分隔符且 title 较短，可能是产品名
            if (string.IsNullOrEmpty(productName) && pageTitle.Length <= 50 && !pageTitle.Equals(brandName, StringComparison.OrdinalIgnoreCase))
            {
                // 检查是否包含产品关键词
                var productKeywords = new[] { "pro", "max", "plus", "ultra", "lite", "mini", "air", "model", "series", "版", "型", "款" };
                if (productKeywords.Any(kw => pageTitle.ToLower().Contains(kw)))
                {
                    productName = pageTitle;
                }
            }
        }

        // 4. 如果品牌名仍为空，从域名推断
        if (string.IsNullOrEmpty(brandName))
        {
            var domainParts = host.Split('.');
            if (domainParts.Length >= 1 && domainParts[0].Length > 0)
            {
                var main = domainParts[0];
                brandName = char.ToUpper(main[0]) + main[1..];
            }
        }

        // 5. 清理产品名
        if (!string.IsNullOrEmpty(productName) && !string.IsNullOrEmpty(brandName))
        {
            // 如果产品名就是品牌名，清空
            if (productName.Equals(brandName, StringComparison.OrdinalIgnoreCase))
            {
                productName = "";
            }
            // 如果产品名看起来像 slogan/广告语而非真实产品名，清空（交给 AI 补充）
            // 判断依据：英文单词数 >= 5，或含句号，或以 the/a/an 开头
            else if (!string.IsNullOrEmpty(productName))
            {
                var wordCount = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var lowerProd = productName.ToLower().Trim();
                if (wordCount >= 5 || 
                    productName.Contains('.') ||
                    lowerProd.StartsWith("the ") || 
                    lowerProd.StartsWith("a ") || 
                    lowerProd.StartsWith("an ") ||
                    lowerProd.StartsWith("one ") ||
                    lowerProd.StartsWith("your ") ||
                    lowerProd.StartsWith("we ") ||
                    lowerProd.StartsWith("welcome"))
                {
                    productName = "";
                }
            }
        }

        return (brandName, productName);
    }

    /// <summary>
    /// 推断行业分类
    /// </summary>
    private string InferIndustry(string title, string description, string keywords, string domain)
    {
        var domainLower = domain.ToLower().Replace("www.", "");
        var text = $"{title} {description} {keywords} {domain}".ToLower();

        // 1. 知名域名直接映射
        var domainIndustryMap = new Dictionary<string, string>
        {
            // 科技互联网
            ["google.com"] = "人工智能",
            ["microsoft.com"] = "企业软件",
            ["amazon.com"] = "电子商务",
            ["facebook.com"] = "社交媒体",
            ["meta.com"] = "社交媒体",
            ["twitter.com"] = "社交媒体",
            ["x.com"] = "社交媒体",
            ["linkedin.com"] = "招聘平台",
            ["github.com"] = "企业软件",
            ["openai.com"] = "人工智能",
            ["anthropic.com"] = "人工智能",
            // 消费电子
            ["apple.com"] = "消费电子",
            ["samsung.com"] = "消费电子",
            ["huawei.com"] = "消费电子",
            ["xiaomi.com"] = "消费电子",
            ["oppo.com"] = "消费电子",
            ["vivo.com"] = "消费电子",
            // 汽车
            ["tesla.com"] = "新能源汽车",
            ["nio.com"] = "新能源汽车",
            ["xpeng.com"] = "新能源汽车",
            ["li.auto"] = "新能源汽车",
            ["bmw.com"] = "传统汽车",
            ["mercedes-benz.com"] = "传统汽车",
            ["audi.com"] = "传统汽车",
            // 电商
            ["taobao.com"] = "电子商务",
            ["jd.com"] = "电子商务",
            ["pinduoduo.com"] = "电子商务",
            ["alibaba.com"] = "电子商务",
            // 运动服饰
            ["nike.com"] = "运动服饰",
            ["adidas.com"] = "运动服饰",
            ["puma.com"] = "运动服饰",
            ["underarmour.com"] = "运动服饰",
            // 餐饮
            ["starbucks.com"] = "咖啡茶饮",
            ["mcdonald.com"] = "快餐",
            ["kfc.com"] = "快餐",
            // 视频平台
            ["youtube.com"] = "视频平台",
            ["netflix.com"] = "视频平台",
            ["bilibili.com"] = "视频平台",
            ["iqiyi.com"] = "视频平台",
            ["youku.com"] = "视频平台",
            // 金融
            ["paypal.com"] = "支付",
            ["stripe.com"] = "支付",
            ["alipay.com"] = "支付",
            // SaaS/企业工具
            ["notion.so"] = "企业软件",
            ["figma.com"] = "企业软件",
            ["canva.com"] = "企业软件",
            ["slack.com"] = "企业软件",
            ["zoom.us"] = "企业软件",
            ["zoom.com"] = "企业软件",
            ["shopify.com"] = "电子商务",
            ["salesforce.com"] = "企业软件",
            ["atlassian.com"] = "企业软件",
            ["dropbox.com"] = "企业软件",
            // 音乐/娱乐
            ["spotify.com"] = "音乐娱乐",
            ["tiktok.com"] = "社交媒体",
            ["douyin.com"] = "社交媒体",
            // 旅游住宿
            ["airbnb.com"] = "旅游住宿",
            ["booking.com"] = "旅游住宿",
            ["trip.com"] = "旅游住宿",
            ["ctrip.com"] = "旅游住宿",
            // 教育
            ["duolingo.com"] = "教育培训",
            ["coursera.org"] = "教育培训",
            // 运动服饰
            ["lululemon.com"] = "运动服饰",
            // 家电
            ["dyson.com"] = "消费电子",
            // AI
            ["geocoreai.com"] = "人工智能",
        };

        // 检查域名直接映射（精确匹配，避免 x.com 匹配到 xiaomi.com）
        foreach (var (knownDomain, industry) in domainIndustryMap)
        {
            // 精确匹配：域名完全相同，或主域名部分完全相同
            if (domainLower == knownDomain || domainLower.EndsWith("." + knownDomain))
            {
                return industry;
            }
            // 子域名匹配：去掉后缀后精确匹配主域名
            var knownMain = knownDomain.Split('.')[0];
            var domainMain = domainLower.Split('.')[0];
            if (knownMain.Length > 1 && domainMain == knownMain)
            {
                return industry;
            }
        }

        // 2. 行业关键词映射
        var industryKeywords = new Dictionary<string, string[]>
        {
            ["消费电子"] = new[] { "iphone", "手机", "电脑", "laptop", "tablet", "耳机", "智能手表", "电子产品", "数码", "smartphone", "phone" },
            ["人工智能"] = new[] { "ai", "artificial intelligence", "机器学习", "深度学习", "大模型", "llm", "chatgpt", "gpt", "人工智能" },
            ["新能源汽车"] = new[] { "tesla", "特斯拉", "电动车", "新能源", "ev", "充电", "model 3", "model y", "蔚来", "小鹏", "理想", "electric vehicle" },
            ["运动服饰"] = new[] { "nike", "adidas", "运动", "跑步", "健身", "球鞋", "运动鞋", "sportswear", "athletic" },
            ["美妆护肤"] = new[] { "护肤", "化妆", "美妆", "skincare", "cosmetic", "面膜", "精华", "口红", "beauty" },
            ["食品饮料"] = new[] { "饮料", "食品", "零食", "咖啡", "茶", "奶茶", "饮品", "food", "beverage" },
            ["企业软件"] = new[] { "saas", "软件", "企业服务", "crm", "erp", "云服务", "b2b", "enterprise", "software" },
            ["电子商务"] = new[] { "电商", "购物", "商城", "淘宝", "京东", "拼多多", "shopping", "ecommerce", "marketplace" },
            ["金融科技"] = new[] { "金融", "银行", "支付", "理财", "投资", "保险", "fintech", "finance", "banking" },
            ["在线教育"] = new[] { "教育", "学习", "课程", "培训", "在线学习", "网课", "education", "learning", "course" },
            ["互联网医疗"] = new[] { "医疗", "健康", "医院", "问诊", "药品", "体检", "health", "medical", "healthcare" },
            ["社交媒体"] = new[] { "社交", "social", "分享", "朋友圈", "动态", "feed" },
            ["视频平台"] = new[] { "视频", "video", "直播", "streaming", "watch" },
            ["游戏"] = new[] { "游戏", "game", "gaming", "电竞", "esports" },
        };

        foreach (var (industry, kws) in industryKeywords)
        {
            foreach (var kw in kws)
            {
                if (text.Contains(kw))
                {
                    return industry;
                }
            }
        }

        return "";
    }

    /// <summary>
    /// 用 AI 补充规则提取缺失的字段（productName、industry）
    /// </summary>
    private async Task EnrichWithAI(UrlAnalyzeResult result)
    {
        try
        {
            // 优先从缓存读取 Gemini 模型配置
            var (cachedEndpoint, cachedKey, cachedModel) = _configCache.GetModelConfig("gemini");
            var apiEndpoint = !string.IsNullOrEmpty(cachedEndpoint) ? cachedEndpoint 
                : (_configuration["AIModels:Gemini:ApiEndpoint"] ?? "https://api.n1n.ai/v1/chat/completions");
            var apiKey = !string.IsNullOrEmpty(cachedKey) ? cachedKey : _configuration["AIModels:Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("AI API Key 未配置，跳过 AI 补充");
                return;
            }

            var systemPrompt = @"You are a JSON extractor. Output ONLY a single-line JSON with these fields:
- productName: the brand's CORE product or service name. Rules:
  * Physical product brand: output the flagship product (e.g. Apple→iPhone, Tesla→Model 3)
  * Platform/service company: output the platform/service name in Chinese (e.g. Netflix→流媒体平台, Spotify→音乐流媒体, Amazon→电商平台, Starbucks→咖啡连锁, ChatGPT→AI对话助手, Airbnb→民宿短租平台, Zoom→视频会议平台)
  * SaaS/tool company: output the product name (e.g. GeoCore AI→GEO优化平台, Notion→协作笔记工具)
  * NEVER return empty string. Every brand has a core product/service.
  * NEVER return the brand name itself as productName.
- industry: one of [人工智能,企业软件,电子商务,社交媒体,消费电子,新能源汽车,传统汽车,运动服饰,咖啡茶饮,快餐,视频平台,音乐娱乐,金融科技,教育培训,医疗健康,游戏娱乐,旅游住宿,其他]
Output ONLY JSON. No explanation. No markdown.";

            var userPrompt = $"Brand: {result.BrandName}\nIndustry: {(string.IsNullOrEmpty(result.Industry) ? "unknown" : result.Industry)}\nDescription: {(string.IsNullOrEmpty(result.Description) ? "none" : result.Description)}\nURL: {result.Url}";

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = !string.IsNullOrEmpty(cachedModel) ? cachedModel : "gemini-2.0-flash",
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.0,
                max_tokens = 200
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI 补充调用失败: HTTP {StatusCode}", response.StatusCode);
                return;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var aiText = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?.Trim() ?? "";

            // 提取 JSON
            var start = aiText.IndexOf('{');
            var end = aiText.LastIndexOf('}');
            if (start < 0 || end <= start) return;
            var jsonStr = aiText[start..(end + 1)];

            using var aiDoc = JsonDocument.Parse(jsonStr);
            var root = aiDoc.RootElement;

            // 只补充空字段，不覆盖已有值
            if (string.IsNullOrEmpty(result.ProductName) && root.TryGetProperty("productName", out var pn))
            {
                var productName = pn.GetString()?.Trim() ?? "";
                // 过滤掉和品牌名重复或包含品牌名的（中英文都检查）
                if (!string.IsNullOrEmpty(productName) && !IsProductNameDuplicate(productName, result.BrandName))
                {
                    result.ProductName = productName;
                }
            }

            if (string.IsNullOrEmpty(result.Industry) && root.TryGetProperty("industry", out var ind))
            {
                result.Industry = ind.GetString()?.Trim() ?? "";
            }

            _logger.LogInformation("AI 补充完成: 品牌={Brand}, 产品={Product}, 行业={Industry}",
                result.BrandName, result.ProductName, result.Industry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI 补充异常");
        }
    }

    /// <summary>
    /// 判断产品名是否和品牌名重复（中英文都检查）
    /// </summary>
    private bool IsProductNameDuplicate(string productName, string brandName)
    {
        if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(brandName))
            return false;

        var pLower = productName.ToLower().Trim();
        var bLower = brandName.ToLower().Trim();

        // 完全相同
        if (pLower == bLower) return true;

        // 产品名就是品牌名（去掉特殊字符后）
        var pClean = Regex.Replace(pLower, @"[®™\s\-_.]", "");
        var bClean = Regex.Replace(bLower, @"[®™\s\-_.]", "");
        if (pClean == bClean) return true;

        // 知名品牌的中英文对照（用于过滤"华为"=huawei等情况）
        var brandAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["apple"] = new[] { "苹果" },
            ["huawei"] = new[] { "华为" },
            ["xiaomi"] = new[] { "小米" },
            ["samsung"] = new[] { "三星" },
            ["nike"] = new[] { "耐克" },
            ["adidas"] = new[] { "阿迪达斯" },
            ["starbucks"] = new[] { "星巴克" },
            ["microsoft"] = new[] { "微软" },
            ["amazon"] = new[] { "亚马逊" },
            ["google"] = new[] { "谷歌" },
            ["tesla"] = new[] { "特斯拉" },
        };

        foreach (var (key, aliases) in brandAliases)
        {
            var allNames = aliases.Append(key).ToArray();
            bool brandMatch = allNames.Any(n => bLower.Contains(n.ToLower()));
            bool productMatch = allNames.Any(n => pLower.Contains(n.ToLower()));
            if (brandMatch && productMatch) return true;
        }

        return false;
    }
}

/// <summary>
/// URL 分析请求
/// </summary>
public class UrlAnalyzeRequest
{
    public string Url { get; set; } = "";
}

/// <summary>
/// URL 分析结果
/// </summary>
public class UrlAnalyzeResult
{
    public string Url { get; set; } = "";
    public string Domain { get; set; } = "";
    public string BrandName { get; set; } = "";      // 品牌名称（公司/品牌）
    public string ProductName { get; set; } = "";    // 产品名称（具体产品，可选）
    public string Description { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Headline { get; set; } = "";
}
