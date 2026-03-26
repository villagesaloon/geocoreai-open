using System.Diagnostics;
using System.Text.Json;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking.Adapters;

/// <summary>
/// Gemini 平台适配器
/// 权重：5%（Google 生态集成）
/// </summary>
public class GeminiAdapter : PlatformAdapterBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public override AIPlatform Platform => AIPlatform.Gemini;
    public override double Weight => 0.05; // 5% 权重
    
    private string? ApiKey => _configuration["Google:ApiKey"];
    private string Model => _configuration["Google:Model"] ?? "gemini-2.5-flash";
    private string BaseUrl => _configuration["Google:BaseUrl"] ?? "https://generativelanguage.googleapis.com/v1beta";
    
    public override bool IsAvailable => !string.IsNullOrEmpty(ApiKey);

    public GeminiAdapter(
        ILogger<GeminiAdapter> logger,
        ICitationAnalyzer citationAnalyzer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, citationAnalyzer)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("Gemini");
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
            response.ErrorMessage = "Gemini API key not configured";
            return response;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = question }
                        }
                    }
                },
                tools = new[]
                {
                    new { googleSearch = new { } }
                },
                generationConfig = new
                {
                    maxOutputTokens = 2000,
                    temperature = 0.7
                }
            };

            var url = $"{BaseUrl}/models/{Model}:generateContent?key={ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            var httpResponse = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            if (!httpResponse.IsSuccessStatusCode)
            {
                response.Success = false;
                response.ErrorMessage = $"API error: {httpResponse.StatusCode} - {responseContent}";
                _logger.LogWarning("[GeminiAdapter] API error: {StatusCode}", httpResponse.StatusCode);
                return response;
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            var candidate = jsonResponse.RootElement.GetProperty("candidates")[0];
            
            var content = candidate
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            response.Response = content ?? "";
            response.Success = true;

            // 提取 groundingMetadata 中的引用链接（Gemini 特有）
            if (candidate.TryGetProperty("groundingMetadata", out var groundingMetadata))
            {
                if (groundingMetadata.TryGetProperty("groundingChunks", out var chunks))
                {
                    foreach (var chunk in chunks.EnumerateArray())
                    {
                        if (chunk.TryGetProperty("web", out var web) &&
                            web.TryGetProperty("uri", out var uri))
                        {
                            var citationUrl = uri.GetString();
                            if (!string.IsNullOrEmpty(citationUrl))
                            {
                                response.DetectedLinks.Add(citationUrl);
                            }
                        }
                    }
                }
            }

            // 估算 API 成本（Gemini Flash with grounding: ~$0.035/query）
            response.ApiCost = 0.035;

            _logger.LogDebug(
                "[GeminiAdapter] Query completed in {Ms}ms, citations: {Count}",
                response.ResponseTimeMs, response.DetectedLinks.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[GeminiAdapter] Query failed");
        }

        return response;
    }
}
