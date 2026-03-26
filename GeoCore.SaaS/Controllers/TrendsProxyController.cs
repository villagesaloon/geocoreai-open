using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Google Trends 代理控制器
/// 通过 Bright Data SERP API 访问 Google Trends（解决国内服务器无法直连问题）
/// 
/// 安全措施：
/// 1. 只允许请求 trends.google.com 域名
/// 2. 限制请求频率（每个用户每分钟最多 10 次）
/// 3. 记录请求日志用于监控
/// </summary>
[ApiController]
[Route("api/trends")]
public class TrendsProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConfigCacheService _configCache;
    private readonly ILogger<TrendsProxyController> _logger;
    
    // SERP API 配置（从数据库读取）
    private string SerpApiKey => _configCache.GetSystemValue("BrightData", "ApiKey") ?? "";
    private string SerpZone => _configCache.GetSystemValue("BrightData", "Zone") ?? "serp_api1";
    private string SerpBaseUrl => _configCache.GetSystemValue("BrightData", "BaseUrl") ?? "https://api.brightdata.com/request";
    private int SerpTimeout => _configCache.GetSystemIntValue("BrightData", "TimeoutSeconds", 60);
    
    // 简单的内存限流（生产环境应使用 Redis）
    private static readonly Dictionary<string, List<DateTime>> _requestLog = new();
    private static readonly object _lockObj = new();
    private const int MaxRequestsPerMinute = 10;

    public TrendsProxyController(
        IHttpClientFactory httpClientFactory,
        ConfigCacheService configCache,
        ILogger<TrendsProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configCache = configCache;
        _logger = logger;
    }

    /// <summary>
    /// 代理请求 Google Trends API
    /// </summary>
    [HttpPost("proxy")]
    public async Task<IActionResult> Proxy([FromBody] ProxyRequest request)
    {
        // 验证 URL
        if (string.IsNullOrEmpty(request.Url))
        {
            return BadRequest(new { error = "URL is required" });
        }

        // 安全检查：只允许 trends.google.com
        if (!IsAllowedUrl(request.Url))
        {
            _logger.LogWarning("Blocked proxy request to: {Url}", request.Url);
            return BadRequest(new { error = "Only trends.google.com is allowed" });
        }

        // 限流检查
        var clientIp = GetClientIp();
        if (!CheckRateLimit(clientIp))
        {
            _logger.LogWarning("Rate limit exceeded for IP: {Ip}", clientIp);
            return StatusCode(429, new { error = "Rate limit exceeded. Please wait a moment." });
        }

        try
        {
            // 使用 SERP API 代理请求
            var response = await CallSerpApiAsync(request.Url);
            
            if (response == null)
            {
                return StatusCode(502, new { error = "SERP API request failed" });
            }
            
            _logger.LogDebug("Proxy request successful via SERP API: {Url}", request.Url);
            
            return Content(response, "application/json");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Proxy request failed: {Url}", request.Url);
            return StatusCode(502, new { error = $"Failed to fetch: {ex.Message}" });
        }
    }

    /// <summary>
    /// 直接获取关键词热度（简化接口）
    /// </summary>
    [HttpGet("heat")]
    public async Task<IActionResult> GetKeywordHeat([FromQuery] string keyword, [FromQuery] string? geo = "", [FromQuery] string? time = "today 12-m")
    {
        if (string.IsNullOrEmpty(keyword))
        {
            return BadRequest(new { error = "Keyword is required" });
        }

        // 内部调用（来自 DistillationController）跳过限流
        var isInternalCall = Request.Headers.ContainsKey("X-Internal-Call");
        if (!isInternalCall)
        {
            var clientIp = GetClientIp();
            if (!CheckRateLimit(clientIp))
            {
                _logger.LogWarning("[GoogleTrends] 限流触发: IP={Ip}, keyword={Keyword}", clientIp, keyword);
                return StatusCode(429, new { error = "Rate limit exceeded" });
            }
        }

        _logger.LogDebug("[GoogleTrends] 开始查询: keyword={Keyword}, geo={Geo}, time={Time}, internal={Internal}", 
            keyword, geo, time, isInternalCall);

        try
        {
            // 使用 SERP API 支持的 Google Trends explore 页面 URL
            // 添加 brd_trends 和 brd_json 参数获取结构化数据
            _logger.LogDebug("[GoogleTrends] 通过 SERP API 获取趋势数据");
            
            var geoParam = string.IsNullOrEmpty(geo) ? "" : $"&geo={geo}";
            var dateParam = string.IsNullOrEmpty(time) ? "" : $"&date={Uri.EscapeDataString(time)}";
            var trendsUrl = $"https://trends.google.com/trends/explore?q={Uri.EscapeDataString(keyword)}{geoParam}{dateParam}&brd_trends=timeseries,geo_map&brd_json=1";
            
            var trendsResponse = await CallSerpApiAsync(trendsUrl);
            
            if (string.IsNullOrEmpty(trendsResponse))
            {
                return Ok(new
                {
                    keyword,
                    avgHeat = 0,
                    recentHeat = 0,
                    trendDirection = "unknown",
                    trendScore = 0,
                    relatedQueries = Array.Empty<string>(),
                    relatedTopics = Array.Empty<object>(),
                    success = false,
                    error = "SERP API request failed"
                });
            }

            // 解析 SERP API 返回的 JSON（brd_json=1 格式）
            _logger.LogInformation("[GoogleTrends] SERP API 响应 ({Length} chars): {Preview}", 
                trendsResponse.Length, 
                trendsResponse.Length > 500 ? trendsResponse[..500] + "..." : trendsResponse);
            
            int avgHeat = 0, recentHeat = 0;
            string trendDirection = "stable";
            int trendScore = 50;
            var relatedQueries = new List<string>();
            var relatedTopics = new List<object>();

            using var doc = JsonDocument.Parse(trendsResponse);
            var root = doc.RootElement;

            // SERP API 返回的是 widgets 数组，数据在 widgets[].data.default.timelineData 中
            if (root.TryGetProperty("widgets", out var widgets))
            {
                foreach (var widget in widgets.EnumerateArray())
                {
                    // 解析时间序列数据
                    if (widget.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("default", out var defaultData) &&
                        defaultData.TryGetProperty("timelineData", out var timelineData))
                    {
                        var values = new List<int>();
                        foreach (var point in timelineData.EnumerateArray())
                        {
                            if (point.TryGetProperty("value", out var valArr) && valArr.GetArrayLength() > 0)
                            {
                                values.Add(valArr[0].GetInt32());
                            }
                        }
                        
                        if (values.Count > 0)
                        {
                            avgHeat = (int)values.Average();
                            recentHeat = values.Last();
                            
                            // 计算趋势方向
                            if (values.Count >= 6)
                            {
                                var recentAvg = values.Skip(values.Count - 3).Take(3).Average();
                                var earlierAvg = values.Skip(values.Count - 6).Take(3).Average();
                                
                                if (earlierAvg > 0)
                                {
                                    var ratio = recentAvg / earlierAvg;
                                    if (ratio >= 1.15)
                                    {
                                        trendDirection = "rising";
                                        trendScore = Math.Min(100, 50 + (int)((ratio - 1) * 100));
                                    }
                                    else if (ratio <= 0.85)
                                    {
                                        trendDirection = "declining";
                                        trendScore = Math.Max(0, 50 - (int)((1 - ratio) * 100));
                                    }
                                }
                            }
                        }
                    }
                    
                    // 解析地理分布数据
                    if (widget.TryGetProperty("data", out var geoData) &&
                        geoData.TryGetProperty("default", out var geoDefault) &&
                        geoDefault.TryGetProperty("geoMapData", out var geoMapData))
                    {
                        foreach (var region in geoMapData.EnumerateArray().Take(10))
                        {
                            if (region.TryGetProperty("geoName", out var geoName))
                            {
                                relatedTopics.Add(new { 
                                    title = geoName.GetString() ?? "", 
                                    type = "region",
                                    value = region.TryGetProperty("value", out var v) && v.GetArrayLength() > 0 
                                        ? v[0].GetInt32().ToString() : "0"
                                });
                            }
                        }
                    }
                    
                    // 解析相关查询
                    if (widget.TryGetProperty("data", out var relData) &&
                        relData.TryGetProperty("default", out var relDefault) &&
                        relDefault.TryGetProperty("rankedList", out var rankedList))
                    {
                        foreach (var list in rankedList.EnumerateArray())
                        {
                            if (list.TryGetProperty("rankedKeyword", out var keywords))
                            {
                                foreach (var kw in keywords.EnumerateArray().Take(10))
                                {
                                    if (kw.TryGetProperty("query", out var query))
                                    {
                                        relatedQueries.Add(query.GetString() ?? "");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogDebug("[GoogleTrends] 查询完成: keyword={Keyword}, avgHeat={AvgHeat}, recentHeat={RecentHeat}, trend={TrendDirection}({TrendScore})",
                keyword, avgHeat, recentHeat, trendDirection, trendScore);

            return Ok(new
            {
                keyword,
                avgHeat,
                recentHeat,
                trendDirection,
                trendScore,
                relatedQueries,
                relatedTopics,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GoogleTrends] 查询失败: keyword={Keyword}", keyword);
            return Ok(new
            {
                keyword,
                avgHeat = 0,
                recentHeat = 0,
                trendDirection = "unknown",
                trendScore = 0,
                relatedQueries = Array.Empty<string>(),
                relatedTopics = Array.Empty<object>(),  // v4.2
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 检查 URL 是否允许代理
    /// </summary>
    private static bool IsAllowedUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.EndsWith("google.com") && uri.Host.Contains("trends");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查限流
    /// </summary>
    private bool CheckRateLimit(string clientIp)
    {
        lock (_lockObj)
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);

            if (!_requestLog.ContainsKey(clientIp))
            {
                _requestLog[clientIp] = new List<DateTime>();
            }

            // 清理过期记录
            _requestLog[clientIp] = _requestLog[clientIp]
                .Where(t => t > oneMinuteAgo)
                .ToList();

            // 检查是否超过限制
            if (_requestLog[clientIp].Count >= MaxRequestsPerMinute)
            {
                return false;
            }

            // 记录本次请求
            _requestLog[clientIp].Add(now);
            return true;
        }
    }

    /// <summary>
    /// 获取客户端 IP
    /// </summary>
    private string GetClientIp()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // 检查 X-Forwarded-For 头（用于反向代理场景）
        if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        
        return ip ?? "unknown";
    }

    /// <summary>
    /// 通过 Bright Data SERP API 请求 Google Trends（REST API + Bearer Token）
    /// </summary>
    private async Task<string?> CallSerpApiAsync(string url)
    {
        if (string.IsNullOrEmpty(SerpApiKey))
        {
            _logger.LogWarning("[SERP API] API Key not configured");
            return null;
        }
        
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(SerpTimeout);
            
            var request = new
            {
                zone = SerpZone,
                url = url,
                format = "raw"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SerpApiKey);

            _logger.LogDebug("[SERP API] Calling: {Url}", url.Length > 80 ? url[..80] + "..." : url);

            var response = await client.PostAsync(SerpBaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("[SERP API] Error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("[SERP API] Response ({Length} chars)", responseBody.Length);

            return responseBody;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("[SERP API] Request timed out for URL: {Url}", url.Length > 80 ? url[..80] + "..." : url);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SERP API] Request failed for URL: {Url}", url.Length > 80 ? url[..80] + "..." : url);
            return null;
        }
    }

    /// <summary>
    /// 移除 Google API 响应的前缀
    /// </summary>
    private static string RemoveGooglePrefix(string response)
    {
        return Regex.Replace(response, @"^\)\]\}',?\s*", "");
    }
}

/// <summary>
/// 代理请求模型
/// </summary>
public class ProxyRequest
{
    public string Url { get; set; } = "";
}
