using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using GeoCore.SaaS.Services;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// AI 可见度检测接口 - 独立接口，使用数据库 Prompt
/// 用于首页快速检测，返回真实的 AI 可见度数据
/// </summary>
[ApiController]
[Route("api/visibility-check")]
public class AIVisibilityCheckController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AIVisibilityCheckController> _logger;
    private readonly ConfigCacheService _configCache;
    private readonly PromptConfigRepository _promptRepo;

    private const string PromptCategory = "visibility";
    private const string PromptKey = "ai-visibility-check";

    public AIVisibilityCheckController(
        IHttpClientFactory httpClientFactory,
        ILogger<AIVisibilityCheckController> logger,
        ConfigCacheService configCache,
        PromptConfigRepository promptRepo)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configCache = configCache;
        _promptRepo = promptRepo;
    }

    /// <summary>
    /// AI 可见度检测 - 根据 URL 或品牌/产品查询真实数据
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VisibilityCheckResult>> Check([FromBody] VisibilityCheckRequest request)
    {
        var startTime = DateTime.UtcNow;
        
        // 验证输入
        if (request.Mode == "url")
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest(new { error = "URL 不能为空" });
            
            // 从 URL 提取品牌名
            if (string.IsNullOrWhiteSpace(request.BrandName))
                request.BrandName = ExtractBrandFromUrl(request.Url);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.BrandName))
                return BadRequest(new { error = "品牌名称不能为空" });
        }

        _logger.LogInformation("[VisibilityCheck] 开始检测: Brand={Brand}, URL={URL}", 
            request.BrandName, request.Url);

        try
        {
            // 从数据库加载 Prompt 模板
            var promptTemplate = await LoadPromptTemplateAsync();
            
            // 构建实际 Prompt
            var prompt = BuildPrompt(promptTemplate, request);
            
            // 调用 AI 获取真实数据
            var aiResponse = await CallAIAsync(prompt, request.ModelId ?? "gemini");
            
            // 解析结果
            var result = ParseAIResponse(aiResponse, request);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("[VisibilityCheck] 检测完成: Brand={Brand}, Score={Score}, Duration={Duration:F1}s", 
                request.BrandName, result.SheepScore, duration);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[VisibilityCheck] 检测失败: {Brand}", request.BrandName);
            return StatusCode(500, new { error = "检测过程中发生错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 从数据库加载 Prompt 模板
    /// </summary>
    private async Task<string> LoadPromptTemplateAsync()
    {
        try
        {
            var dbConfig = await _promptRepo.GetByKeyAsync(PromptCategory, PromptKey);
            if (dbConfig != null && !string.IsNullOrEmpty(dbConfig.PromptTemplate))
            {
                _logger.LogDebug("[VisibilityCheck] 从数据库加载 Prompt: {Category}/{Key}", PromptCategory, PromptKey);
                return dbConfig.PromptTemplate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VisibilityCheck] 从数据库加载 Prompt 失败，使用默认模板");
        }
        
        return GetDefaultPromptTemplate();
    }

    /// <summary>
    /// 默认 Prompt 模板
    /// </summary>
    private string GetDefaultPromptTemplate()
    {
        return @"You are a professional AI visibility analyst. Analyze the following brand/website's visibility across major AI platforms (ChatGPT, Claude, Gemini, Perplexity, Grok).

【Input】
- Brand: {{brandName}}
- Product: {{productName}}
- Website: {{url}}
- Industry: {{industry}}

【Analysis Requirements】
1. **Brand Recognition**: Is this brand in your knowledge base? How well do you know it?
2. **AI Citation Estimate**: Based on brand awareness, industry position, and content quality, estimate how often this brand is cited in AI responses
3. **Competitor Comparison**: List main competitors and compare their AI visibility
4. **SHEEP Score**: Rate 0-100 based on five dimensions:
   - S (Specificity): Content specificity
   - H (Helpfulness): Content usefulness
   - E (Expertise): Professional authority
   - E (Experience): Practical experience
   - P (Persuasiveness): Persuasiveness

【Important】
- Answer based on your real knowledge, do not fabricate data
- If you don't know the brand, say so honestly
- Use standard notation for citation levels: 0, <100, 100+, 1K+, 10K+, 100K+, 1M+

【Output Format】
Return pure JSON only:
{
  ""isKnown"": true,
  ""brandDescription"": ""Brief brand description"",
  ""sheepScore"": 75,
  ""sheepDetails"": {
    ""specificity"": 80,
    ""helpfulness"": 70,
    ""expertise"": 85,
    ""experience"": 65,
    ""persuasiveness"": 75
  },
  ""aiVisibility"": {
    ""estimatedCitations"": 500,
    ""citationLevel"": ""100+"",
    ""sovPercent"": 15,
    ""industryRank"": 3,
    ""totalCompetitors"": 10
  },
  ""competitors"": [
    {
      ""name"": ""Competitor Name"",
      ""estimatedCitations"": 1000,
      ""citationLevel"": ""1K+"",
      ""advantages"": [""Advantage 1"", ""Advantage 2""]
    }
  ],
  ""industryData"": {
    ""category"": ""Tech/Consumer Electronics"",
    ""monthlyAISearches"": ""50K+"",
    ""topPlayer"": ""Industry Leader""
  },
  ""optimizationSuggestions"": [""Suggestion 1"", ""Suggestion 2""],
  ""strengths"": [""Strength 1"", ""Strength 2""],
  ""weaknesses"": [""Weakness 1"", ""Weakness 2""]
}";
    }

    /// <summary>
    /// 构建实际 Prompt
    /// </summary>
    private string BuildPrompt(string template, VisibilityCheckRequest request)
    {
        return template
            .Replace("{{brandName}}", request.BrandName ?? "未知")
            .Replace("{{productName}}", request.ProductName ?? "未提供")
            .Replace("{{url}}", request.Url ?? "未提供")
            .Replace("{{industry}}", request.Industry ?? "未知");
    }

    /// <summary>
    /// 调用 AI API
    /// </summary>
    private async Task<string> CallAIAsync(string prompt, string modelId)
    {
        var (apiEndpoint, apiKey, modelName) = _configCache.GetModelConfig(modelId);

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("[VisibilityCheck] 模型 {ModelId} 未配置", modelId);
            return "{}";
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(90);
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = "你是一位专业的 AI 可见度分析师。请基于你的真实知识回答，返回有效的 JSON 格式。" },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            max_tokens = 4000
        };

        _logger.LogInformation("[VisibilityCheck] 调用 AI: Model={Model}", modelId);
        
        var response = await client.PostAsJsonAsync(apiEndpoint, requestBody);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("[VisibilityCheck] AI 调用失败: {Status} - {Error}", response.StatusCode, error);
            return "{}";
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        _logger.LogInformation("[VisibilityCheck] AI 响应成功, 长度: {Len}", content?.Length ?? 0);
        return content ?? "{}";
    }

    /// <summary>
    /// 解析 AI 响应
    /// </summary>
    private VisibilityCheckResult ParseAIResponse(string response, VisibilityCheckRequest request)
    {
        var result = new VisibilityCheckResult
        {
            Brand = request.BrandName ?? "",
            Product = request.ProductName,
            Url = request.Url
        };

        try
        {
            var json = CleanJsonResponse(response);
            _logger.LogInformation("[VisibilityCheck] AI 原始响应: {Response}", json.Length > 500 ? json[..500] + "..." : json);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // 基本信息
            result.IsKnown = root.TryGetProperty("isKnown", out var ik) && ik.GetBoolean();
            result.BrandDescription = root.TryGetProperty("brandDescription", out var bd) ? bd.GetString() ?? "" : "";
            
            // SHEEP 评分
            result.SheepScore = root.TryGetProperty("sheepScore", out var ss) ? ss.GetInt32() : 0;
            
            if (root.TryGetProperty("sheepDetails", out var sd))
            {
                result.SheepDetails = new SheepDetails
                {
                    Specificity = sd.TryGetProperty("specificity", out var s) ? s.GetInt32() : 0,
                    Helpfulness = sd.TryGetProperty("helpfulness", out var h) ? h.GetInt32() : 0,
                    Expertise = sd.TryGetProperty("expertise", out var e1) ? e1.GetInt32() : 0,
                    Experience = sd.TryGetProperty("experience", out var e2) ? e2.GetInt32() : 0,
                    Persuasiveness = sd.TryGetProperty("persuasiveness", out var p) ? p.GetInt32() : 0
                };
            }

            // AI 可见度数据
            // 多模型估算系数：当前只查询1个模型，系统支持5个主流AI平台（GPT、Claude、Gemini、Grok、Perplexity）
            const int MODEL_COUNT = 5;
            
            if (root.TryGetProperty("aiVisibility", out var av))
            {
                var singleModelCitations = av.TryGetProperty("estimatedCitations", out var ec) ? ec.GetInt32() : 0;
                // 全平台估算引用次数 = 单模型引用次数 × 模型数量
                result.EstimatedCitations = singleModelCitations * MODEL_COUNT;
                result.CitationLevel = FormatCitationLevel(result.EstimatedCitations);
                result.SovPercent = av.TryGetProperty("sovPercent", out var sov) ? sov.GetInt32() : 0;
                result.IndustryRank = av.TryGetProperty("industryRank", out var ir) ? ir.GetInt32() : 0;
                result.TotalCompetitors = av.TryGetProperty("totalCompetitors", out var tc) ? tc.GetInt32() : 0;
                
                _logger.LogInformation("[VisibilityCheck] 解析结果: SingleModel={Single}, AllPlatforms={All}, Level={Level}, SOV={SOV}, Rank={Rank}", 
                    singleModelCitations, result.EstimatedCitations, result.CitationLevel, result.SovPercent, result.IndustryRank);
            }
            else
            {
                _logger.LogWarning("[VisibilityCheck] 未找到 aiVisibility 字段");
            }

            // 竞品数据 - 同样应用多模型系数
            if (root.TryGetProperty("competitors", out var comps))
            {
                foreach (var c in comps.EnumerateArray())
                {
                    var compSingleCitations = c.TryGetProperty("estimatedCitations", out var ec) ? ec.GetInt32() : 0;
                    var compAllPlatformsCitations = compSingleCitations * MODEL_COUNT;
                    
                    result.Competitors.Add(new CompetitorData
                    {
                        Name = c.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        EstimatedCitations = compAllPlatformsCitations,
                        CitationLevel = FormatCitationLevel(compAllPlatformsCitations),
                        Advantages = c.TryGetProperty("advantages", out var adv)
                            ? adv.EnumerateArray().Select(a => a.GetString() ?? "").Where(a => !string.IsNullOrEmpty(a)).ToList()
                            : new List<string>()
                    });
                }
            }

            // 行业数据
            if (root.TryGetProperty("industryData", out var ind))
            {
                result.IndustryCategory = ind.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "";
                result.MonthlyAISearches = ind.TryGetProperty("monthlyAISearches", out var mas) ? mas.GetString() ?? "" : "";
                result.TopPlayer = ind.TryGetProperty("topPlayer", out var tp) ? tp.GetString() ?? "" : "";
            }

            // 优化建议
            if (root.TryGetProperty("optimizationSuggestions", out var opts))
            {
                result.OptimizationSuggestions = opts.EnumerateArray()
                    .Select(o => o.GetString() ?? "")
                    .Where(o => !string.IsNullOrEmpty(o))
                    .ToList();
            }

            // 优势和不足
            if (root.TryGetProperty("strengths", out var str))
            {
                result.Strengths = str.EnumerateArray()
                    .Select(s => s.GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            if (root.TryGetProperty("weaknesses", out var weak))
            {
                result.Weaknesses = weak.EnumerateArray()
                    .Select(w => w.GetString() ?? "")
                    .Where(w => !string.IsNullOrEmpty(w))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VisibilityCheck] 解析 AI 响应失败");
            result.IsKnown = false;
            result.BrandDescription = "解析失败，请重试";
        }

        return result;
    }

    /// <summary>
    /// 格式化引用级别为标准格式
    /// </summary>
    private static string FormatCitationLevel(int citations)
    {
        return citations switch
        {
            >= 10000000 => "10M+",
            >= 1000000 => $"{citations / 1000000}M+",
            >= 100000 => "100K+",
            >= 10000 => $"{citations / 1000}K+",
            >= 1000 => "1K+",
            >= 100 => "100+",
            > 0 => "<100",
            _ => "0"
        };
    }

    /// <summary>
    /// 清理 JSON 响应
    /// </summary>
    private string CleanJsonResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "{}";
        
        response = response.Trim();
        if (response.StartsWith("```json"))
            response = response[7..];
        else if (response.StartsWith("```"))
            response = response[3..];
        
        if (response.EndsWith("```"))
            response = response[..^3];
        
        return response.Trim();
    }

    /// <summary>
    /// 从 URL 提取品牌名
    /// </summary>
    private string ExtractBrandFromUrl(string url)
    {
        try
        {
            var domain = url.Replace("https://", "").Replace("http://", "").Replace("www.", "").Split('/')[0];
            var brand = domain.Split('.')[0];
            return char.ToUpper(brand[0]) + brand[1..];
        }
        catch
        {
            return "Unknown";
        }
    }
}

#region DTOs

public class VisibilityCheckRequest
{
    public string Mode { get; set; } = "url";
    public string? Url { get; set; }
    public string? BrandName { get; set; }
    public string? ProductName { get; set; }
    public string? Industry { get; set; }
    public string? ModelId { get; set; }
}

public class VisibilityCheckResult
{
    public string Brand { get; set; } = "";
    public string? Product { get; set; }
    public string? Url { get; set; }
    
    public bool IsKnown { get; set; }
    public string BrandDescription { get; set; } = "";
    
    public int SheepScore { get; set; }
    public SheepDetails? SheepDetails { get; set; }
    
    public int EstimatedCitations { get; set; }
    public string CitationLevel { get; set; } = "";
    public int SovPercent { get; set; }
    public int IndustryRank { get; set; }
    public int TotalCompetitors { get; set; }
    
    public List<CompetitorData> Competitors { get; set; } = new();
    
    public string IndustryCategory { get; set; } = "";
    public string MonthlyAISearches { get; set; } = "";
    public string TopPlayer { get; set; } = "";
    
    public List<string> OptimizationSuggestions { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

public class SheepDetails
{
    public int Specificity { get; set; }
    public int Helpfulness { get; set; }
    public int Expertise { get; set; }
    public int Experience { get; set; }
    public int Persuasiveness { get; set; }
}

public class CompetitorData
{
    public string Name { get; set; } = "";
    public int EstimatedCitations { get; set; }
    public string CitationLevel { get; set; } = "";
    public List<string> Advantages { get; set; } = new();
}

#endregion
