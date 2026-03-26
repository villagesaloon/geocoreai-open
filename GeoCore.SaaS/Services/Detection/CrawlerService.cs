using Abot2.Crawler;
using Abot2.Poco;
using AngleSharp.Html.Dom;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// 爬虫服务接口
/// </summary>
public interface ICrawlerService
{
    Task<CrawlResult> CrawlSiteAsync(string url, CrawlOptions? options = null);
}

/// <summary>
/// 爬取选项
/// </summary>
public class CrawlOptions
{
    public int MaxPages { get; set; } = 50;
    public int MaxDepth { get; set; } = 3;
    public int MinDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 3000;
    public int TimeoutSeconds { get; set; } = 30;
    public bool RespectRobotsTxt { get; set; } = true;
}

/// <summary>
/// 爬取结果
/// </summary>
public class CrawlResult
{
    public string RootUrl { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int PagesCrawled { get; set; }
    public int MaxDepthReached { get; set; }
    public long DurationMs { get; set; }
    public List<CrawledPage> Pages { get; set; } = new();
}

/// <summary>
/// 爬取的页面
/// </summary>
public class CrawledPage
{
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int Depth { get; set; }
    public long LoadTimeMs { get; set; }
    public List<string> OutLinks { get; set; } = new();
}

/// <summary>
/// Abot 爬虫服务实现
/// </summary>
public class AbotCrawlerService : ICrawlerService
{
    private readonly ILogger<AbotCrawlerService> _logger;
    private readonly ConfigCacheService _configCache;

    public AbotCrawlerService(ILogger<AbotCrawlerService> logger, ConfigCacheService configCache)
    {
        _logger = logger;
        _configCache = configCache;
    }

    public async Task<CrawlResult> CrawlSiteAsync(string url, CrawlOptions? options = null)
    {
        options ??= GetDefaultOptions();
        var result = new CrawlResult { RootUrl = url };
        var startTime = DateTime.UtcNow;

        try
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = options.MaxPages,
                MaxCrawlDepth = options.MaxDepth,
                MinCrawlDelayPerDomainMilliSeconds = options.MinDelayMs,
                HttpRequestTimeoutInSeconds = options.TimeoutSeconds,
                IsRespectRobotsDotTextEnabled = options.RespectRobotsTxt,
                IsRespectMetaRobotsNoFollowEnabled = true,
                IsRespectHttpXRobotsTagHeaderNoFollowEnabled = true,
                MaxMemoryUsageInMb = 100,
                MaxConcurrentThreads = 1,
                IsSendingCookiesEnabled = false,
                IsHttpRequestAutoRedirectsEnabled = true,
                MaxRetryCount = 2
            };

            var crawler = new PoliteWebCrawler(config);

            crawler.PageCrawlCompleted += (sender, args) =>
            {
                var page = args.CrawledPage;
                var crawledPage = new CrawledPage
                {
                    Url = page.Uri.AbsoluteUri,
                    StatusCode = (int)(page.HttpResponseMessage?.StatusCode ?? 0),
                    Depth = page.CrawlDepth,
                    LoadTimeMs = (long)page.Elapsed
                };

                if (page.AngleSharpHtmlDocument != null)
                {
                    crawledPage.Title = page.AngleSharpHtmlDocument.Title;
                    crawledPage.Content = page.Content.Text;

                    // 提取出站链接
                    var links = page.AngleSharpHtmlDocument.QuerySelectorAll("a[href]");
                    foreach (var link in links)
                    {
                        var href = link.GetAttribute("href");
                        if (!string.IsNullOrEmpty(href) && href.StartsWith("http"))
                        {
                            crawledPage.OutLinks.Add(href);
                        }
                    }
                }

                result.Pages.Add(crawledPage);

                if (page.CrawlDepth > result.MaxDepthReached)
                {
                    result.MaxDepthReached = page.CrawlDepth;
                }

                _logger.LogDebug("[Crawler] Crawled: {Url} (depth={Depth}, status={Status})",
                    page.Uri.AbsoluteUri, page.CrawlDepth, crawledPage.StatusCode);
            };

            _logger.LogInformation("[Crawler] Starting crawl: {Url} (maxPages={MaxPages}, maxDepth={MaxDepth})",
                url, options.MaxPages, options.MaxDepth);

            var crawlResult = await crawler.CrawlAsync(new Uri(url));

            result.Success = !crawlResult.ErrorOccurred;
            result.PagesCrawled = result.Pages.Count;
            result.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            if (crawlResult.ErrorOccurred)
            {
                result.ErrorMessage = crawlResult.ErrorException?.Message ?? "Unknown error";
                _logger.LogWarning("[Crawler] Crawl failed: {Url} - {Error}", url, result.ErrorMessage);
            }
            else
            {
                _logger.LogInformation("[Crawler] Crawl completed: {Url} - {Pages} pages in {Duration}ms",
                    url, result.PagesCrawled, result.DurationMs);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "[Crawler] Crawl exception: {Url}", url);
        }

        return result;
    }

    private CrawlOptions GetDefaultOptions()
    {
        return new CrawlOptions
        {
            MaxPages = _configCache.GetSystemIntValue("crawler", "max_pages_per_site", 50),
            MaxDepth = _configCache.GetSystemIntValue("crawler", "max_crawl_depth", 3),
            MinDelayMs = _configCache.GetSystemIntValue("crawler", "request_interval_min_ms", 1000),
            MaxDelayMs = _configCache.GetSystemIntValue("crawler", "request_interval_max_ms", 3000),
            TimeoutSeconds = 30,
            RespectRobotsTxt = true
        };
    }
}
