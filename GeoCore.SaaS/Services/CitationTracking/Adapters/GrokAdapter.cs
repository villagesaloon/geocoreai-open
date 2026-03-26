using System.Diagnostics;
using System.Text.Json;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking.Adapters;

/// <summary>
/// Grok 平台适配器
/// 权重：5%（X/Twitter 生态，实时数据）
/// </summary>
public class GrokAdapter : PlatformAdapterBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    public override AIPlatform Platform => AIPlatform.Grok;
    public override double Weight => 0.05; // 5% 权重
    
    private string? ApiKey => _configuration["Grok:ApiKey"];
    private string Model => _configuration["Grok:Model"] ?? "grok-4";
    private string BaseUrl => _configuration["Grok:BaseUrl"] ?? "https://api.x.ai/v1";
    
    public override bool IsAvailable => !string.IsNullOrEmpty(ApiKey);

    public GrokAdapter(
        ILogger<GrokAdapter> logger,
        ICitationAnalyzer citationAnalyzer,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
        : base(logger, citationAnalyzer)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("Grok");
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
            response.ErrorMessage = "Grok API key not configured";
            return response;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Grok API 使用 OpenAI 兼容格式，添加 web_search 和 x_search 工具
            var requestBody = new
            {
                model = Model,
                messages = new[]
                {
                    new { role = "user", content = question }
                },
                tools = new object[]
                {
                    new { type = "web_search" },
                    new { type = "x_search" }
                },
                tool_choice = "auto",
                max_tokens = 2000,
                temperature = 0.7
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
                _logger.LogWarning("[GrokAdapter] API error: {StatusCode}", httpResponse.StatusCode);
                return response;
            }

            var jsonResponse = JsonDocument.Parse(responseContent);
            var message = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");
            
            var content = message.GetProperty("content").GetString();
            response.Response = content ?? "";
            response.Success = true;

            // 提取 tool_calls 中的搜索结果引用（Grok web_search/x_search 特有）
            if (message.TryGetProperty("tool_calls", out var toolCalls))
            {
                foreach (var toolCall in toolCalls.EnumerateArray())
                {
                    if (toolCall.TryGetProperty("function", out var function) &&
                        function.TryGetProperty("arguments", out var arguments))
                    {
                        // 尝试解析 arguments 中的 URL
                        try
                        {
                            var argsJson = JsonDocument.Parse(arguments.GetString() ?? "{}");
                            if (argsJson.RootElement.TryGetProperty("results", out var results))
                            {
                                foreach (var result in results.EnumerateArray())
                                {
                                    if (result.TryGetProperty("url", out var url))
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
                        catch { /* 忽略解析错误 */ }
                    }
                }
            }

            // 估算 API 成本（Grok with search: ~$0.05/query）
            response.ApiCost = 0.05;

            _logger.LogDebug(
                "[GrokAdapter] Query completed in {Ms}ms, citations: {Count}",
                response.ResponseTimeMs, response.DetectedLinks.Count);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[GrokAdapter] Query failed");
        }

        return response;
    }
}
