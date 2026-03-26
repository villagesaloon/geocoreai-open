using System.Diagnostics;
using System.Text.Json;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking.Adapters;

/// <summary>
/// Claude 平台适配器
/// 权重：10%（专业用户，谨慎引用）
/// </summary>
public class ClaudeAdapter : PlatformAdapterBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public override AIPlatform Platform => AIPlatform.Claude;
    public override double Weight => 0.1; // 10% 权重
    
    private string? ApiKey => _configuration["Anthropic:ApiKey"];
    private string Model => _configuration["Anthropic:Model"] ?? "claude-sonnet-4-20250514";
    private string BaseUrl => _configuration["Anthropic:BaseUrl"] ?? "https://api.anthropic.com/v1";
    
    public override bool IsAvailable => !string.IsNullOrEmpty(ApiKey);

    public ClaudeAdapter(
        ILogger<ClaudeAdapter> logger,
        ICitationAnalyzer citationAnalyzer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, citationAnalyzer)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("Claude");
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
            response.ErrorMessage = "Claude API key not configured";
            return response;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var requestBody = new
            {
                model = Model,
                max_tokens = 2000,
                messages = new[]
                {
                    new { role = "user", content = question }
                },
                tools = new[]
                {
                    new 
                    { 
                        type = "web_search_20250305", 
                        name = "web_search",
                        max_uses = 5
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("x-api-key", ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var httpResponse = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            if (!httpResponse.IsSuccessStatusCode)
            {
                response.Success = false;
                response.ErrorMessage = $"API error: {httpResponse.StatusCode} - {responseContent}";
                _logger.LogWarning("[ClaudeAdapter] API error: {StatusCode}", httpResponse.StatusCode);
                return response;
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            var contentArray = jsonResponse.RootElement.GetProperty("content");
            
            // 遍历 content 数组，提取文本和引用
            var textBuilder = new System.Text.StringBuilder();
            foreach (var contentItem in contentArray.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var type))
                {
                    var typeStr = type.GetString();
                    
                    // 提取文本内容
                    if (typeStr == "text" && contentItem.TryGetProperty("text", out var text))
                    {
                        textBuilder.Append(text.GetString());
                        
                        // 提取 citations（Claude web_search 特有）
                        if (contentItem.TryGetProperty("citations", out var citations))
                        {
                            foreach (var citation in citations.EnumerateArray())
                            {
                                if (citation.TryGetProperty("url", out var url))
                                {
                                    var urlStr = url.GetString();
                                    if (!string.IsNullOrEmpty(urlStr))
                                    {
                                        response.DetectedLinks.Add(urlStr);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            response.Response = textBuilder.ToString();
            response.Success = true;

            // 估算 API 成本（Claude Sonnet with web search: ~$0.05/query）
            response.ApiCost = 0.05;

            _logger.LogDebug(
                "[ClaudeAdapter] Query completed in {Ms}ms, citations: {Count}",
                response.ResponseTimeMs, response.DetectedLinks.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[ClaudeAdapter] Query failed");
        }

        return response;
    }
}
