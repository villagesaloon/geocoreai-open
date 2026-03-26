using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace GeoCore.SaaS.Services.GoogleTrends;

/// <summary>
/// Google Trends 服务 - 通过 Bright Data SERP API 获取趋势数据
/// 配置从 system_configs 表读取（Category = "BrightData"）
/// </summary>
public class GoogleTrendsService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigCacheService _configCache;
    private readonly ILogger<GoogleTrendsService> _logger;
    private readonly IMemoryCache _cache;

    // 从 system_configs 表读取配置（Category = "BrightData"）
    private string ApiKey => _configCache.GetSystemValue("BrightData", "ApiKey") 
        ?? throw new InvalidOperationException("BrightData:ApiKey not configured in system_configs");
    private string Zone => _configCache.GetSystemValue("BrightData", "Zone") ?? "serp_api1";
    private string BaseUrl => _configCache.GetSystemValue("BrightData", "BaseUrl") ?? "https://api.brightdata.com/request";
    private int TimeoutSeconds => _configCache.GetSystemIntValue("BrightData", "TimeoutSeconds", 60);
    
    // 从 system_configs 表读取配置（Category = "GoogleTrends"）
    private int CacheMinutes => _configCache.GetSystemIntValue("GoogleTrends", "CacheExpirationMinutes", 60);
    private string DefaultGeo => _configCache.GetSystemValue("GoogleTrends", "DefaultGeo") ?? "CN";
    private string DefaultDateRange => _configCache.GetSystemValue("GoogleTrends", "DefaultDateRange") ?? "today 12-m";

    public GoogleTrendsService(
        HttpClient httpClient,
        ConfigCacheService configCache,
        ILogger<GoogleTrendsService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configCache = configCache;
        _logger = logger;
        _cache = cache;

        _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    }

    /// <summary>
    /// 获取关键词趋势数据（Interest Over Time）
    /// </summary>
    public async Task<TrendsInterestResponse> GetInterestOverTimeAsync(
        string[] keywords,
        string? geo = null,
        string? dateRange = null,
        CancellationToken cancellationToken = default)
    {
        geo ??= DefaultGeo;
        dateRange ??= DefaultDateRange;

        var cacheKey = $"trends_interest_{string.Join("_", keywords)}_{geo}_{dateRange}";
        
        if (_cache.TryGetValue(cacheKey, out TrendsInterestResponse? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for trends interest: {Keywords}", string.Join(", ", keywords));
            return cached;
        }

        var query = string.Join(",", keywords);
        var trendsUrl = $"https://trends.google.com/trends/explore?q={Uri.EscapeDataString(query)}&geo={geo}&date={Uri.EscapeDataString(dateRange)}&brd_trends=timeseries,geo_map&brd_json=1";

        _logger.LogInformation("Fetching Google Trends interest for: {Keywords}, geo={Geo}, date={Date}", 
            string.Join(", ", keywords), geo, dateRange);

        var rawJson = await CallSerpApiRawAsync(trendsUrl, cancellationToken);
        
        if (string.IsNullOrEmpty(rawJson))
        {
            return new TrendsInterestResponse { Success = false, Error = "No data returned from SERP API" };
        }

        var result = ParseInterestResponse(rawJson);
        
        if (result.Success)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheMinutes));
        }

        return result;
    }

    /// <summary>
    /// 获取相关搜索词
    /// </summary>
    public async Task<TrendsRelatedResponse> GetRelatedQueriesAsync(
        string keyword,
        string? geo = null,
        CancellationToken cancellationToken = default)
    {
        geo ??= DefaultGeo;

        var cacheKey = $"trends_related_{keyword}_{geo}";
        
        if (_cache.TryGetValue(cacheKey, out TrendsRelatedResponse? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for related queries: {Keyword}", keyword);
            return cached;
        }

        var trendsUrl = $"https://trends.google.com/trends/explore?q={Uri.EscapeDataString(keyword)}&geo={geo}&brd_trends=related_queries&brd_json=1";

        _logger.LogInformation("Fetching related queries for: {Keyword}, geo={Geo}", keyword, geo);

        var rawJson = await CallSerpApiRawAsync(trendsUrl, cancellationToken);
        
        if (string.IsNullOrEmpty(rawJson))
        {
            return new TrendsRelatedResponse { Success = false, Error = "No data returned from SERP API" };
        }

        var result = ParseRelatedResponse(rawJson);
        
        if (result.Success)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheMinutes * 6));
        }

        return result;
    }

    /// <summary>
    /// 获取每日热搜（通过 realtime trends 页面）
    /// 注意：SERP API 不支持 /trendingsearches/daily 端点，使用 realtime 替代
    /// </summary>
    public async Task<TrendsDailyResponse> GetDailyTrendsAsync(
        string? geo = null,
        CancellationToken cancellationToken = default)
    {
        geo ??= DefaultGeo;

        var cacheKey = $"trends_daily_{geo}_{DateTime.UtcNow:yyyyMMddHH}";
        
        if (_cache.TryGetValue(cacheKey, out TrendsDailyResponse? cached) && cached != null)
        {
            _logger.LogDebug("Cache hit for daily trends: {Geo}", geo);
            return cached;
        }

        // SERP API 支持的 realtime trends URL
        var trendsUrl = $"https://trends.google.com/trending?geo={geo}&hours=24&brd_json=1";

        _logger.LogInformation("Fetching daily trends for geo={Geo}", geo);

        var rawJson = await CallSerpApiRawAsync(trendsUrl, cancellationToken);
        
        if (string.IsNullOrEmpty(rawJson))
        {
            return new TrendsDailyResponse { Success = false, Error = "No data returned from SERP API" };
        }

        // 检查是否返回了不支持的端点错误
        if (rawJson.Contains("this endpoint is not supported"))
        {
            _logger.LogWarning("Daily trends endpoint not supported by SERP API, returning empty result");
            return new TrendsDailyResponse 
            { 
                Success = true, 
                Geo = geo,
                Trends = new List<TrendingSearch>(),
                Message = "Daily trends endpoint not supported by SERP API"
            };
        }

        // 记录响应前 1000 字符用于调试
        _logger.LogInformation("Daily trends response preview: {Preview}", 
            rawJson.Length > 1000 ? rawJson[..1000] : rawJson);

        var result = ParseDailyResponse(rawJson, geo);
        
        if (result.Success)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheMinutes));
        }

        return result;
    }

    /// <summary>
    /// 品牌 vs 竞品趋势对比
    /// </summary>
    public async Task<TrendsComparisonResponse> CompareBrandsAsync(
        string brand,
        List<string> competitors,
        string? geo = null,
        string? dateRange = null,
        CancellationToken cancellationToken = default)
    {
        var allKeywords = new List<string> { brand };
        allKeywords.AddRange(competitors.Take(4)); // Google Trends 最多比较 5 个关键词

        var interestData = await GetInterestOverTimeAsync(allKeywords.ToArray(), geo, dateRange, cancellationToken);

        var response = new TrendsComparisonResponse
        {
            Success = interestData.Success,
            Brand = brand,
            Competitors = competitors,
            Geo = geo ?? DefaultGeo,
            DateRange = dateRange ?? DefaultDateRange
        };

        if (interestData.Success && interestData.Timeline != null)
        {
            response.Timeline = interestData.Timeline;
            response.Summary = CalculateSummary(allKeywords, interestData.Timeline);
        }

        return response;
    }

    /// <summary>
    /// 验证问题热度（批量）
    /// </summary>
    public async Task<List<QuestionTrendResult>> ValidateQuestionTrendsAsync(
        List<string> questions,
        string? geo = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<QuestionTrendResult>();

        foreach (var question in questions)
        {
            // 从问题中提取关键词（简单实现：取前 3 个词）
            var keyword = ExtractKeyword(question);
            
            try
            {
                var interest = await GetInterestOverTimeAsync(new[] { keyword }, geo, "today 3-m", cancellationToken);
                
                var result = new QuestionTrendResult
                {
                    Question = question,
                    ExtractedKeyword = keyword,
                    TrendScore = CalculateTrendScore(interest),
                    TrendDirection = DetermineTrendDirection(interest),
                    GoogleTrendsHeat = CalculateHeat(interest)
                };
                
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate trend for question: {Question}", question);
                results.Add(new QuestionTrendResult
                {
                    Question = question,
                    ExtractedKeyword = keyword,
                    TrendScore = 0,
                    TrendDirection = null,
                    GoogleTrendsHeat = 0
                });
            }

            // 避免请求过快
            await Task.Delay(500, cancellationToken);
        }

        return results;
    }

    /// <summary>
    /// 调用 Bright Data SERP API（使用 REST API + Bearer Token）
    /// </summary>
    private async Task<string?> CallSerpApiRawAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var request = new
            {
                zone = Zone,
                url = url,
                format = "raw"  // 返回原始 HTML/JSON
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            _logger.LogInformation("Calling SERP API for URL: {Url}", url.Length > 100 ? url[..100] + "..." : url);

            var response = await _httpClient.PostAsync(BaseUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("SERP API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("SERP API response ({Length} chars)", responseBody.Length);

            return responseBody;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("SERP API request timed out for URL: {Url}", url.Length > 100 ? url[..100] + "..." : url);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SERP API request failed for URL: {Url}", url.Length > 100 ? url[..100] + "..." : url);
            return null;
        }
    }

    /// <summary>
    /// 解析 Google Trends Interest Over Time 响应
    /// </summary>
    private TrendsInterestResponse ParseInterestResponse(string json)
    {
        var response = new TrendsInterestResponse { Success = true };
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 查找 widgets 数组中的 timeseries 数据
            if (root.TryGetProperty("widgets", out var widgets))
            {
                foreach (var widget in widgets.EnumerateArray())
                {
                    if (widget.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("default", out var defaultData) &&
                        defaultData.TryGetProperty("timelineData", out var timelineData))
                    {
                        response.Timeline = new List<TrendsTimelineItem>();
                        
                        foreach (var item in timelineData.EnumerateArray())
                        {
                            var timelineItem = new TrendsTimelineItem
                            {
                                Date = item.TryGetProperty("formattedTime", out var ft) ? ft.GetString() : null,
                                Values = new List<int>()
                            };

                            if (item.TryGetProperty("value", out var values))
                            {
                                foreach (var v in values.EnumerateArray())
                                {
                                    timelineItem.Values.Add(v.GetInt32());
                                }
                            }

                            response.Timeline.Add(timelineItem);
                        }
                        
                        break; // 只取第一个 timeseries widget
                    }
                }
            }

            // 查找 geo_map 数据
            if (root.TryGetProperty("widgets", out var widgets2))
            {
                foreach (var widget in widgets2.EnumerateArray())
                {
                    if (widget.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("default", out var defaultData) &&
                        defaultData.TryGetProperty("geoMapData", out var geoMapData))
                    {
                        response.GeoMap = new TrendsGeoMap { Regions = new List<TrendsGeoRegion>() };
                        
                        foreach (var item in geoMapData.EnumerateArray())
                        {
                            var region = new TrendsGeoRegion
                            {
                                Name = item.TryGetProperty("geoName", out var gn) ? gn.GetString() : null,
                                Value = item.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Array 
                                    ? (v.GetArrayLength() > 0 ? v[0].GetInt32() : 0) 
                                    : 0
                            };
                            response.GeoMap.Regions.Add(region);
                        }
                        
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Google Trends response");
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    /// <summary>
    /// 解析 Google Trends Related Queries 响应
    /// </summary>
    private TrendsRelatedResponse ParseRelatedResponse(string json)
    {
        var response = new TrendsRelatedResponse { Success = true };
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("widgets", out var widgets))
            {
                foreach (var widget in widgets.EnumerateArray())
                {
                    if (widget.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("default", out var defaultData) &&
                        defaultData.TryGetProperty("rankedList", out var rankedList))
                    {
                        foreach (var list in rankedList.EnumerateArray())
                        {
                            if (list.TryGetProperty("rankedKeyword", out var keywords))
                            {
                                var queries = new List<TrendsRelatedQuery>();
                                foreach (var kw in keywords.EnumerateArray())
                                {
                                    queries.Add(new TrendsRelatedQuery
                                    {
                                        Query = kw.TryGetProperty("query", out var q) ? q.GetString() : null,
                                        Value = kw.TryGetProperty("formattedValue", out var v) ? v.GetString() : 
                                               (kw.TryGetProperty("value", out var v2) ? v2.ToString() : null)
                                    });
                                }
                                
                                // 第一个列表是 Top，第二个是 Rising
                                if (response.Top == null)
                                    response.Top = queries;
                                else if (response.Rising == null)
                                    response.Rising = queries;
                            }
                        }
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse related queries response");
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    /// <summary>
    /// 解析 Google Trends Daily Trends 响应
    /// </summary>
    private TrendsDailyResponse ParseDailyResponse(string json, string? geo)
    {
        var response = new TrendsDailyResponse 
        { 
            Success = true, 
            Geo = geo,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Keywords = new List<TrendsDailyKeyword>(),
            Trends = new List<TrendingSearch>()
        };
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // /trending 页面返回的是数组格式
            // 格式: [{"query":"xxx","volume":500000,"volume_change":"1000%","related_queries":["a","b"]}]
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    // 获取标题：优先 query，其次 title
                    var title = item.TryGetProperty("query", out var q) ? q.GetString() :
                               (item.TryGetProperty("title", out var t) ? t.GetString() : null);
                    
                    // 获取流量：优先 volume，其次 searchVolume，最后 formattedTraffic
                    string? traffic = null;
                    if (item.TryGetProperty("volume", out var vol))
                        traffic = vol.ToString();
                    else if (item.TryGetProperty("searchVolume", out var sv))
                        traffic = sv.ToString();
                    else if (item.TryGetProperty("formattedTraffic", out var ft))
                        traffic = ft.GetString();
                    
                    // 获取变化率
                    var volumeChange = item.TryGetProperty("volume_change", out var vc) ? vc.GetString() : null;
                    
                    var trend = new TrendingSearch
                    {
                        Title = title,
                        Traffic = traffic,
                        Description = volumeChange != null ? $"变化: {volumeChange}" : null
                    };
                    
                    // 同时填充 Keywords 以保持兼容
                    var keyword = new TrendsDailyKeyword
                    {
                        Keyword = title,
                        Traffic = traffic,
                        RelatedQueries = new List<string>()
                    };
                    
                    // 解析相关查询（字符串数组格式）
                    if (item.TryGetProperty("related_queries", out var related) && related.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var rq in related.EnumerateArray())
                        {
                            var relQuery = rq.ValueKind == JsonValueKind.String ? rq.GetString() :
                                          (rq.TryGetProperty("query", out var rqq) ? rqq.GetString() : null);
                            if (!string.IsNullOrEmpty(relQuery))
                                keyword.RelatedQueries.Add(relQuery);
                        }
                    }
                    
                    if (item.TryGetProperty("articles", out var articles) && articles.ValueKind == JsonValueKind.Array && articles.GetArrayLength() > 0)
                    {
                        keyword.NewsUrl = articles[0].TryGetProperty("url", out var url) ? url.GetString() : null;
                    }
                    
                    response.Trends.Add(trend);
                    response.Keywords.Add(keyword);
                }
                
                return response;
            }

            // 旧格式: { default: { trendingSearchesDays: [...] } }
            if (root.TryGetProperty("default", out var defaultData) &&
                defaultData.TryGetProperty("trendingSearchesDays", out var days))
            {
                foreach (var day in days.EnumerateArray())
                {
                    if (day.TryGetProperty("trendingSearches", out var searches))
                    {
                        foreach (var search in searches.EnumerateArray())
                        {
                            var keyword = new TrendsDailyKeyword
                            {
                                Keyword = search.TryGetProperty("title", out var t2) && t2.TryGetProperty("query", out var q) 
                                    ? q.GetString() : null,
                                Traffic = search.TryGetProperty("formattedTraffic", out var ft2) ? ft2.GetString() : null,
                                RelatedQueries = new List<string>()
                            };

                            if (search.TryGetProperty("relatedQueries", out var related))
                            {
                                foreach (var rq in related.EnumerateArray())
                                {
                                    if (rq.TryGetProperty("query", out var rqQuery))
                                    {
                                        keyword.RelatedQueries.Add(rqQuery.GetString() ?? "");
                                    }
                                }
                            }

                            if (search.TryGetProperty("articles", out var articles) && articles.GetArrayLength() > 0)
                            {
                                var firstArticle = articles[0];
                                keyword.NewsUrl = firstArticle.TryGetProperty("url", out var url) ? url.GetString() : null;
                            }

                            response.Keywords.Add(keyword);
                            response.Trends.Add(new TrendingSearch
                            {
                                Title = keyword.Keyword,
                                Traffic = keyword.Traffic
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse daily trends response");
            response.Success = false;
            response.Error = ex.Message;
        }

        return response;
    }

    private string ExtractKeyword(string question)
    {
        // 简单实现：去除常见问句词，取核心词
        var stopWords = new[] { "如何", "怎么", "什么", "为什么", "哪些", "哪个", "是否", "能否", "可以", "应该", "how", "what", "why", "which", "can", "should" };
        var words = question.Split(new[] { ' ', '，', '。', '？', '?', '、' }, StringSplitOptions.RemoveEmptyEntries);
        var filtered = words.Where(w => !stopWords.Contains(w.ToLower()) && w.Length > 1).Take(3);
        return string.Join(" ", filtered);
    }

    private int CalculateTrendScore(TrendsInterestResponse interest)
    {
        if (interest?.Timeline == null || interest.Timeline.Count == 0) return 0;
        
        var values = interest.Timeline.SelectMany(t => t.Values).ToList();
        if (values.Count == 0) return 0;
        
        return (int)values.Average();
    }

    private string? DetermineTrendDirection(TrendsInterestResponse interest)
    {
        if (interest?.Timeline == null || interest.Timeline.Count < 2) return null;

        var recent = interest.Timeline.TakeLast(3).SelectMany(t => t.Values).ToList();
        var older = interest.Timeline.Take(3).SelectMany(t => t.Values).ToList();

        if (recent.Count == 0 || older.Count == 0) return null;

        var recentAvg = recent.Average();
        var olderAvg = older.Average();

        if (recentAvg > olderAvg * 1.1) return "rising";
        if (recentAvg < olderAvg * 0.9) return "declining";
        return "stable";
    }

    private int CalculateHeat(TrendsInterestResponse interest)
    {
        if (interest?.Timeline == null || interest.Timeline.Count == 0) return 0;
        
        var recentValues = interest.Timeline.TakeLast(4).SelectMany(t => t.Values).ToList();
        if (recentValues.Count == 0) return 0;
        
        return (int)recentValues.Average();
    }

    private List<TrendsSummaryItem> CalculateSummary(List<string> keywords, List<TrendsTimelineItem> timeline)
    {
        var summary = new List<TrendsSummaryItem>();
        
        for (int i = 0; i < keywords.Count; i++)
        {
            var values = timeline.Select(t => t.Values.Count > i ? t.Values[i] : 0).ToList();
            var avgInterest = values.Count > 0 ? (int)values.Average() : 0;
            
            var recent = values.TakeLast(3).ToList();
            var older = values.Take(3).ToList();
            var recentAvg = recent.Count > 0 ? recent.Average() : 0;
            var olderAvg = older.Count > 0 ? older.Average() : 0;

            string trend;
            string change;
            if (olderAvg > 0)
            {
                var changePercent = (recentAvg - olderAvg) / olderAvg * 100;
                change = changePercent >= 0 ? $"+{changePercent:F0}%" : $"{changePercent:F0}%";
                trend = changePercent > 10 ? "rising" : (changePercent < -10 ? "declining" : "stable");
            }
            else
            {
                change = "N/A";
                trend = "stable";
            }

            var peakIndex = values.IndexOf(values.Max());
            var peakDate = timeline.Count > peakIndex ? timeline[peakIndex].Date : null;

            summary.Add(new TrendsSummaryItem
            {
                Keyword = keywords[i],
                AvgInterest = avgInterest,
                Trend = trend,
                Change = change,
                PeakDate = peakDate
            });
        }

        return summary;
    }
}

#region Response Models

public class TrendsInterestResponse
{
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public List<TrendsTimelineItem>? Timeline { get; set; }
    public TrendsGeoMap? GeoMap { get; set; }
}

public class TrendsTimelineItem
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }
    
    [JsonPropertyName("values")]
    public List<int> Values { get; set; } = new();
}

public class TrendsGeoMap
{
    [JsonPropertyName("regions")]
    public List<TrendsGeoRegion>? Regions { get; set; }
}

public class TrendsGeoRegion
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class TrendsRelatedResponse
{
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public List<TrendsRelatedQuery>? Rising { get; set; }
    public List<TrendsRelatedQuery>? Top { get; set; }
}

public class TrendsRelatedQuery
{
    [JsonPropertyName("query")]
    public string? Query { get; set; }
    
    [JsonPropertyName("value")]
    public string? Value { get; set; } // 可能是 "100" 或 "+500%"
}

public class TrendsDailyResponse
{
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public string? Message { get; set; }
    public string? Date { get; set; }
    public string? Geo { get; set; }
    public List<TrendsDailyKeyword>? Keywords { get; set; }
    public List<TrendingSearch>? Trends { get; set; }
}

public class TrendingSearch
{
    public string? Title { get; set; }
    public string? Traffic { get; set; }
    public string? Description { get; set; }
}

public class TrendsDailyKeyword
{
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }
    
    [JsonPropertyName("traffic")]
    public string? Traffic { get; set; }
    
    [JsonPropertyName("relatedQueries")]
    public List<string>? RelatedQueries { get; set; }
    
    [JsonPropertyName("newsUrl")]
    public string? NewsUrl { get; set; }
}

public class TrendsComparisonResponse
{
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public string? Brand { get; set; }
    public List<string>? Competitors { get; set; }
    public string? Geo { get; set; }
    public string? DateRange { get; set; }
    public List<TrendsTimelineItem>? Timeline { get; set; }
    public List<TrendsSummaryItem>? Summary { get; set; }
    public string? Insights { get; set; }
}

public class TrendsSummaryItem
{
    public string? Keyword { get; set; }
    public int AvgInterest { get; set; }
    public string? Trend { get; set; } // rising, stable, declining
    public string? Change { get; set; } // +15%, -10%
    public string? PeakDate { get; set; }
}

public class QuestionTrendResult
{
    public string? Question { get; set; }
    public string? ExtractedKeyword { get; set; }
    public int TrendScore { get; set; }
    public string? TrendDirection { get; set; }
    public int GoogleTrendsHeat { get; set; }
}

#endregion
