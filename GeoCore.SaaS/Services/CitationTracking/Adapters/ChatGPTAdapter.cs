using System.Diagnostics;
using System.Text.Json;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking.Adapters;

/// <summary>
/// ChatGPT 平台适配器
/// 权重：40%（驱动 87.4% AI 引荐流量）
/// </summary>
public class ChatGPTAdapter : PlatformAdapterBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public override AIPlatform Platform => AIPlatform.ChatGPT;
    public override double Weight => 0.4; // 40% 权重
    
    private string? ApiKey => _configuration["OpenAI:ApiKey"];
    private string Model => _configuration["OpenAI:Model"] ?? "gpt-4o-search-preview";
    private string BaseUrl => _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
    
    public override bool IsAvailable => !string.IsNullOrEmpty(ApiKey);

    public ChatGPTAdapter(
        ILogger<ChatGPTAdapter> logger,
        ICitationAnalyzer citationAnalyzer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, citationAnalyzer)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("ChatGPT");
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
            response.ErrorMessage = "ChatGPT API key not configured";
            return response;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = question }
                },
                max_tokens = 2000,
                temperature = 0.7,
                web_search_options = new { }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");

            var httpResponse = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            if (!httpResponse.IsSuccessStatusCode)
            {
                response.Success = false;
                response.ErrorMessage = $"API error: {httpResponse.StatusCode} - {responseContent}";
                _logger.LogWarning("[ChatGPTAdapter] API error: {StatusCode}", httpResponse.StatusCode);
                return response;
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            var message = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");
            
            var content = message.GetProperty("content").GetString();
            response.Response = content ?? "";
            response.Success = true;

            // 提取 annotations 中的 url_citation（gpt-4o-search-preview 特有）
            if (message.TryGetProperty("annotations", out var annotations))
            {
                foreach (var annotation in annotations.EnumerateArray())
                {
                    if (annotation.TryGetProperty("type", out var type) &&
                        type.GetString() == "url_citation" &&
                        annotation.TryGetProperty("url", out var url))
                    {
                        var urlStr = url.GetString();
                        if (!string.IsNullOrEmpty(urlStr))
                        {
                            response.DetectedLinks.Add(urlStr);
                        }
                    }
                }
            }

            // 估算 API 成本（GPT-4o-search-preview: ~$0.03/query with search）
            response.ApiCost = 0.03;

            _logger.LogDebug(
                "[ChatGPTAdapter] Query completed in {Ms}ms, citations: {Count}",
                response.ResponseTimeMs, response.DetectedLinks.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[ChatGPTAdapter] Query failed");
        }

        return response;
    }
}
