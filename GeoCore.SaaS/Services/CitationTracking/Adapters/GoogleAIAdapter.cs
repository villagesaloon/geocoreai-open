using System.Diagnostics;
using System.Text.Json;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking.Adapters;

/// <summary>
/// Google AI Overviews 平台适配器
/// 权重：20%（52% 搜索出现 AI Overview）
/// 注意：无直接 API，需通过搜索 API 或爬取
/// </summary>
public class GoogleAIAdapter : PlatformAdapterBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public override AIPlatform Platform => AIPlatform.GoogleAI;
    public override double Weight => 0.2; // 20% 权重
    
    private string? ApiKey => _configuration["GoogleSearch:ApiKey"];
    private string? SearchEngineId => _configuration["GoogleSearch:SearchEngineId"];
    private string BaseUrl => "https://www.googleapis.com/customsearch/v1";
    
    public override bool IsAvailable => !string.IsNullOrEmpty(ApiKey) && !string.IsNullOrEmpty(SearchEngineId);

    public GoogleAIAdapter(
        ILogger<GoogleAIAdapter> logger,
        ICitationAnalyzer citationAnalyzer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, citationAnalyzer)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("GoogleAI");
    }

    public override async Task<PlatformResponse> QueryAsync(string question, CancellationToken cancellationToken = default)
    {
        var response = new PlatformResponse
        {
            Platform = Platform,
            Question = question
        };

        if (!IsAvailable)
        {
            response.Success = false;
            response.ErrorMessage = "Google Search API not configured";
            return response;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 使用 Google Custom Search API
            // 注意：这不能直接获取 AI Overview，但可以获取搜索结果摘要
            var url = $"{BaseUrl}?key={ApiKey}&cx={SearchEngineId}&q={Uri.EscapeDataString(question)}";
            
            var httpResponse = await _httpClient.GetAsync(url, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            if (!httpResponse.IsSuccessStatusCode)
            {
                response.Success = false;
                response.ErrorMessage = $"API error: {httpResponse.StatusCode} - {responseContent}";
                _logger.LogWarning("[GoogleAIAdapter] API error: {StatusCode}", httpResponse.StatusCode);
                return response;
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            
            // 构建响应：合并搜索结果的 snippets
            var snippets = new List<string>();
            if (jsonResponse.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray().Take(5))
                {
                    if (item.TryGetProperty("snippet", out var snippet))
                    {
                        snippets.Add(snippet.GetString() ?? "");
                    }
                    
                    // 提取链接
                    if (item.TryGetProperty("link", out var link))
                    {
                        var linkUrl = link.GetString();
                        if (!string.IsNullOrEmpty(linkUrl))
                        {
                            response.DetectedLinks.Add(linkUrl);
                        }
                    }
                }
            }

            response.Response = string.Join("\n\n", snippets);
            response.Success = true;

            // 估算 API 成本（Google Custom Search: $5/1000 queries = $0.005/query）
            response.ApiCost = 0.005;

            _logger.LogDebug(
                "[GoogleAIAdapter] Query completed in {Ms}ms, results: {Count}",
                response.ResponseTimeMs, snippets.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[GoogleAIAdapter] Query failed");
        }

        return response;
    }
}
