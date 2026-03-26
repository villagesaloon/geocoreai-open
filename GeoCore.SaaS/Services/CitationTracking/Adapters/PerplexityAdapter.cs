using System.Diagnostics;
using System.Text.Json;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking.Adapters;

/// <summary>
/// Perplexity 平台适配器
/// 权重：20%（始终带链接，Reddit 偏好）
/// </summary>
public class PerplexityAdapter : PlatformAdapterBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public override AIPlatform Platform => AIPlatform.Perplexity;
    public override double Weight => 0.2; // 20% 权重
    
    private string? ApiKey => _configuration["Perplexity:ApiKey"];
    private string Model => _configuration["Perplexity:Model"] ?? "llama-3.1-sonar-small-128k-online";
    private string BaseUrl => _configuration["Perplexity:BaseUrl"] ?? "https://api.perplexity.ai";
    
    public override bool IsAvailable => !string.IsNullOrEmpty(ApiKey);

    public PerplexityAdapter(
        ILogger<PerplexityAdapter> logger,
        ICitationAnalyzer citationAnalyzer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, citationAnalyzer)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("Perplexity");
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
            response.ErrorMessage = "Perplexity API key not configured";
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
                return_citations = true
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
                _logger.LogWarning("[PerplexityAdapter] API error: {StatusCode}", httpResponse.StatusCode);
                return response;
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            var content = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            response.Response = content ?? "";
            response.Success = true;

            // 提取 citations（Perplexity 特有）
            if (jsonResponse.RootElement.TryGetProperty("citations", out var citations))
            {
                foreach (var citation in citations.EnumerateArray())
                {
                    var url = citation.GetString();
                    if (!string.IsNullOrEmpty(url))
                    {
                        response.DetectedLinks.Add(url);
                    }
                }
            }

            // 估算 API 成本（Perplexity: ~$0.005/query）
            response.ApiCost = 0.005;

            _logger.LogDebug(
                "[PerplexityAdapter] Query completed in {Ms}ms, citations: {Count}",
                response.ResponseTimeMs, response.DetectedLinks.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[PerplexityAdapter] Query failed");
        }

        return response;
    }
}
