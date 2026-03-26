using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 首页快速检测 API - 免费、无需登录
/// 用于首页展示，吸引用户注册
/// </summary>
[ApiController]
[Route("api/quick-detect")]
public class QuickDetectController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<QuickDetectController> _logger;
    private readonly ConfigCacheService _configCache;

    public QuickDetectController(
        IHttpClientFactory httpClientFactory,
        ILogger<QuickDetectController> logger,
        ConfigCacheService configCache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configCache = configCache;
    }

    /// <summary>
    /// 快速 AI 可见度检测 - 首页免费功能
    /// 支持 URL 模式和品牌/产品模式
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuickDetectResult>> Detect([FromBody] QuickDetectRequest request)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("[QuickDetect] 开始检测: Mode={Mode}, Input={Input}", 
            request.Mode, request.Mode == "url" ? request.Url : request.BrandName);

        try
        {
            // 验证输入
            if (request.Mode == "url")
            {
                if (string.IsNullOrWhiteSpace(request.Url))
                    return BadRequest(new { error = "URL 不能为空" });
                
                // 从 URL 提取品牌名
                request.BrandName = ExtractBrandFromUrl(request.Url);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.BrandName))
                    return BadRequest(new { error = "品牌名称不能为空" });
            }

            // 并行执行多个检测任务
            var brandCheckTask = CheckBrandVisibilityAsync(request);
            var competitorTask = FindCompetitorsAsync(request);
            var sampleQuestionsTask = GetSampleQuestionsAsync(request);

            await Task.WhenAll(brandCheckTask, competitorTask, sampleQuestionsTask);

            var brandCheck = await brandCheckTask;
            var competitors = await competitorTask;
            var sampleQuestions = await sampleQuestionsTask;

            // 计算综合评分
            var result = CalculateResult(request, brandCheck, competitors, sampleQuestions);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("[QuickDetect] 检测完成: Brand={Brand}, Score={Score}, Duration={Duration:F1}s", 
                request.BrandName, result.SheepScore, duration);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuickDetect] 检测失败: {Input}", request.BrandName ?? request.Url);
            return StatusCode(500, new { error = "检测过程中发生错误", message = ex.Message });
        }
    }

    /// <summary>
    /// 检测品牌在 AI 中的可见度
    /// </summary>
    private async Task<BrandVisibilityResult> CheckBrandVisibilityAsync(QuickDetectRequest request)
    {
        var prompt = $@"你是一位品牌研究专家和 GEO（生成式引擎优化）专家。请分析以下品牌在 AI 搜索中的可见度。

【品牌信息】
- 品牌名称：{request.BrandName}
- 产品名称：{request.ProductName ?? "未提供"}
- 行业：{request.Industry ?? "未知"}
- 网站：{request.Url ?? "未提供"}

【分析任务】
1. 判断该品牌是否在你的知识库中存在
2. 如果存在，评估其在 AI 回答中被引用的可能性
3. 分析该品牌的内容是否适合被 AI 引用

【输出格式】
必须输出纯 JSON：
{{
  ""isKnown"": true,
  ""confidence"": 85,
  ""description"": ""品牌简要描述"",
  ""citationLikelihood"": 70,
  ""strengths"": [""优势1"", ""优势2""],
  ""weaknesses"": [""不足1"", ""不足2""],
  ""missingElements"": [""缺少结构化数据"", ""缺少权威引用""],
  ""industryCategory"": ""科技/电商/金融/健康/教育/其他""
}}";

        var result = await CallAIAsync(prompt);
        return ParseBrandVisibilityResult(result);
    }

    /// <summary>
    /// 查找竞品并对比
    /// </summary>
    private async Task<List<CompetitorInfo>> FindCompetitorsAsync(QuickDetectRequest request)
    {
        var prompt = $@"你是一位行业研究专家。请为以下品牌找出主要竞品，并评估它们在 AI 搜索中的表现。

【品牌信息】
- 品牌名称：{request.BrandName}
- 产品名称：{request.ProductName ?? "未提供"}
- 行业：{request.Industry ?? "未知"}

【任务】
1. 找出该行业中 3-5 个主要竞品
2. 评估每个竞品在 AI 回答中被引用的可能性
3. 分析竞品的 AI 可见度优势

【输出格式】
必须输出纯 JSON：
{{
  ""competitors"": [
    {{
      ""name"": ""竞品名称"",
      ""citationScore"": 85,
      ""monthlyMentions"": 120,
      ""advantages"": [""内容权威"", ""结构化数据完善""]
    }}
  ],
  ""industryAvgScore"": 65,
  ""monthlyIndustrySearches"": 25000
}}";

        var result = await CallAIAsync(prompt);
        return ParseCompetitorResult(result);
    }

    /// <summary>
    /// 获取示例问题 - 展示 AI 会/不会推荐该品牌的场景
    /// </summary>
    private async Task<SampleQuestionsResult> GetSampleQuestionsAsync(QuickDetectRequest request)
    {
        var prompt = $@"你是一位 GEO 专家。请分析用户可能会问的与 ""{request.BrandName}"" 相关的问题，以及 AI 是否会在回答中提到该品牌。

【品牌信息】
- 品牌名称：{request.BrandName}
- 产品名称：{request.ProductName ?? "未提供"}
- 行业：{request.Industry ?? "未知"}

【任务】
1. 列出 3 个用户可能问的问题
2. 判断 AI 在回答这些问题时是否会提到该品牌
3. 如果不会提到，说明原因

【输出格式】
必须输出纯 JSON：
{{
  ""questions"": [
    {{
      ""question"": ""用户问题"",
      ""willMention"": true,
      ""reason"": ""会/不会提到的原因"",
      ""competitorsMentioned"": [""竞品1"", ""竞品2""]
    }}
  ],
  ""overallMentionRate"": 40
}}";

        var result = await CallAIAsync(prompt);
        return ParseSampleQuestionsResult(result);
    }

    /// <summary>
    /// 计算综合结果
    /// </summary>
    private QuickDetectResult CalculateResult(
        QuickDetectRequest request,
        BrandVisibilityResult brandCheck,
        List<CompetitorInfo> competitors,
        SampleQuestionsResult sampleQuestions)
    {
        // 计算 SHEEP 评分 (0-100)
        // S: Specificity (具体性) - 基于品牌描述的详细程度
        // H: Helpfulness (有用性) - 基于内容是否能解决用户问题
        // E: Expertise (专业性) - 基于行业权威性
        // E: Experience (经验) - 基于用户评价和案例
        // P: Persuasiveness (说服力) - 基于引用和数据支撑

        var sheepScore = brandCheck.IsKnown 
            ? Math.Min(100, brandCheck.Confidence + brandCheck.CitationLikelihood / 2)
            : Math.Max(10, brandCheck.Confidence / 2);

        // 计算 AI 声量占比 (SOV)
        var topCompetitor = competitors.OrderByDescending(c => c.CitationScore).FirstOrDefault();
        var totalScore = competitors.Sum(c => c.CitationScore) + sheepScore;
        var sov = totalScore > 0 ? (int)(sheepScore * 100.0 / totalScore) : 0;

        // 估算被引用次数 - 基于品牌知名度和引用可能性
        // 对于知名品牌，引用次数应该更高
        int estimatedCitations;
        if (!brandCheck.IsKnown)
        {
            estimatedCitations = 0;
        }
        else if (brandCheck.Confidence >= 90)
        {
            // 顶级知名品牌（如小米、苹果等）：数千次引用
            estimatedCitations = (int)(brandCheck.CitationLikelihood * 50 + brandCheck.Confidence * 30);
        }
        else if (brandCheck.Confidence >= 70)
        {
            // 知名品牌：数百次引用
            estimatedCitations = (int)(brandCheck.CitationLikelihood * 10 + brandCheck.Confidence * 5);
        }
        else
        {
            // 一般品牌：几十次引用
            estimatedCitations = (int)(brandCheck.CitationLikelihood * 2 + brandCheck.Confidence * 0.5);
        }

        // 计算排名 - 未被 AI 知道的品牌显示 "未上榜"
        string rankDisplay;
        if (!brandCheck.IsKnown || estimatedCitations == 0)
        {
            rankDisplay = "未上榜";
        }
        else
        {
            var rank = 1;
            foreach (var c in competitors.OrderByDescending(c => c.CitationScore))
            {
                if (c.CitationScore > sheepScore) rank++;
            }
            rankDisplay = $"#{rank}";
        }

        // 计算潜在提升
        var potentialGain = brandCheck.IsKnown 
            ? Math.Min(150, 100 - sheepScore + 50)
            : Math.Min(300, 200);

        // 估算月搜索量
        var monthlySearches = competitors.FirstOrDefault()?.MonthlyMentions * 200 ?? 15000;

        // 构建结果
        var result = new QuickDetectResult
        {
            Brand = request.BrandName ?? "",
            Product = request.ProductName,
            Industry = brandCheck.IndustryCategory ?? request.Industry ?? "未知",
            
            // 核心指标
            SheepScore = sheepScore,
            Sov = $"{sov}%",
            Citations = estimatedCitations,
            Rank = rankDisplay,
            
            // 品牌状态
            IsKnown = brandCheck.IsKnown,
            Confidence = brandCheck.Confidence,
            Description = brandCheck.Description,
            
            // 竞品对比
            TopCompetitor = topCompetitor?.Name ?? "行业领导者",
            TopCompetitorCitations = topCompetitor?.MonthlyMentions ?? 100,
            CitationGap = topCompetitor != null 
                ? $"{(topCompetitor.MonthlyMentions / Math.Max(1, estimatedCitations)):F1}x"
                : "N/A",
            
            // 行业数据
            MonthlySearches = monthlySearches.ToString("N0"),
            IndustryAvgScore = competitors.Any() ? (int)competitors.Average(c => c.CitationScore) : 50,
            
            // 问题示例
            SampleQuestions = sampleQuestions.Questions.Take(3).ToList(),
            OverallMentionRate = sampleQuestions.OverallMentionRate,
            
            // 优化建议
            MissingElements = brandCheck.MissingElements,
            Strengths = brandCheck.Strengths,
            Weaknesses = brandCheck.Weaknesses,
            
            // 潜在提升
            PotentialGain = $"+{potentialGain}%",
            PotentialTraffic = ((int)(monthlySearches * potentialGain / 100 * 0.1)).ToString("N0"),
            
            // 竞品列表
            Competitors = competitors.Take(5).ToList()
        };

        return result;
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

    /// <summary>
    /// 调用 AI API - 从数据库获取模型配置
    /// </summary>
    private async Task<string> CallAIAsync(string prompt, string modelId = "gemini")
    {
        try
        {
            // 从数据库缓存获取模型配置
            var (apiEndpoint, apiKey, modelName) = _configCache.GetModelConfig(modelId);

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("[QuickDetect] 模型 {ModelId} 未配置或 API Key 为空", modelId);
                return "{}";
            }

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new
            {
                model = modelName,
                messages = new[]
                {
                    new { role = "system", content = "你是一位专业的品牌分析师和 GEO 专家。请始终返回有效的 JSON 格式。" },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 4000
            };

            _logger.LogInformation("[QuickDetect] 调用 AI: Model={Model}, Endpoint={Endpoint}", modelId, apiEndpoint);
            var response = await client.PostAsJsonAsync(apiEndpoint, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[QuickDetect] AI API 调用失败: {Status} - {Error}", response.StatusCode, error);
                return "{}";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("[QuickDetect] AI 响应成功, 长度: {Len}", content?.Length ?? 0);
            return content ?? "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuickDetect] AI API 调用异常");
            return "{}";
        }
    }

    /// <summary>
    /// 清理 JSON 响应
    /// </summary>
    private string CleanJsonResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "{}";
        
        // 移除 markdown 代码块标记
        response = response.Trim();
        if (response.StartsWith("```json"))
            response = response[7..];
        else if (response.StartsWith("```"))
            response = response[3..];
        
        if (response.EndsWith("```"))
            response = response[..^3];
        
        return response.Trim();
    }

    private BrandVisibilityResult ParseBrandVisibilityResult(string response)
    {
        try
        {
            var json = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new BrandVisibilityResult
            {
                IsKnown = root.TryGetProperty("isKnown", out var ik) && ik.GetBoolean(),
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetInt32() : 30,
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                CitationLikelihood = root.TryGetProperty("citationLikelihood", out var cl) ? cl.GetInt32() : 30,
                IndustryCategory = root.TryGetProperty("industryCategory", out var ic) ? ic.GetString() ?? "" : "",
                Strengths = root.TryGetProperty("strengths", out var str) 
                    ? str.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                    : new List<string>(),
                Weaknesses = root.TryGetProperty("weaknesses", out var weak)
                    ? weak.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                    : new List<string>(),
                MissingElements = root.TryGetProperty("missingElements", out var miss)
                    ? miss.EnumerateArray().Select(s => s.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                    : new List<string> { "结构化数据", "权威引用来源" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[QuickDetect] 解析品牌可见度结果失败");
            return new BrandVisibilityResult { IsKnown = false, Confidence = 20 };
        }
    }

    private List<CompetitorInfo> ParseCompetitorResult(string response)
    {
        try
        {
            var json = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var competitors = new List<CompetitorInfo>();
            
            if (root.TryGetProperty("competitors", out var comps))
            {
                foreach (var c in comps.EnumerateArray())
                {
                    competitors.Add(new CompetitorInfo
                    {
                        Name = c.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        CitationScore = c.TryGetProperty("citationScore", out var cs) ? cs.GetInt32() : 50,
                        MonthlyMentions = c.TryGetProperty("monthlyMentions", out var mm) ? mm.GetInt32() : 50,
                        Advantages = c.TryGetProperty("advantages", out var adv)
                            ? adv.EnumerateArray().Select(a => a.GetString() ?? "").Where(a => !string.IsNullOrEmpty(a)).ToList()
                            : new List<string>()
                    });
                }
            }

            return competitors;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[QuickDetect] 解析竞品结果失败");
            return new List<CompetitorInfo>();
        }
    }

    private SampleQuestionsResult ParseSampleQuestionsResult(string response)
    {
        try
        {
            var json = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var questions = new List<SampleQuestion>();
            
            if (root.TryGetProperty("questions", out var qs))
            {
                foreach (var q in qs.EnumerateArray())
                {
                    questions.Add(new SampleQuestion
                    {
                        Question = q.TryGetProperty("question", out var qText) ? qText.GetString() ?? "" : "",
                        WillMention = q.TryGetProperty("willMention", out var wm) && wm.GetBoolean(),
                        Reason = q.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "",
                        CompetitorsMentioned = q.TryGetProperty("competitorsMentioned", out var cm)
                            ? cm.EnumerateArray().Select(c => c.GetString() ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList()
                            : new List<string>()
                    });
                }
            }

            var mentionRate = root.TryGetProperty("overallMentionRate", out var mr) ? mr.GetInt32() : 30;

            return new SampleQuestionsResult
            {
                Questions = questions,
                OverallMentionRate = mentionRate
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[QuickDetect] 解析示例问题结果失败");
            return new SampleQuestionsResult { Questions = new List<SampleQuestion>(), OverallMentionRate = 30 };
        }
    }
}

#region DTOs

public class QuickDetectRequest
{
    public string Mode { get; set; } = "url"; // "url" or "brand"
    public string? Url { get; set; }
    public string? BrandName { get; set; }
    public string? ProductName { get; set; }
    public string? Industry { get; set; }
}

public class QuickDetectResult
{
    // 基本信息
    public string Brand { get; set; } = "";
    public string? Product { get; set; }
    public string Industry { get; set; } = "";
    
    // 核心指标
    public int SheepScore { get; set; }
    public string Sov { get; set; } = "0%";
    public int Citations { get; set; }
    public string Rank { get; set; } = "#1";
    
    // 品牌状态
    public bool IsKnown { get; set; }
    public int Confidence { get; set; }
    public string? Description { get; set; }
    
    // 竞品对比
    public string TopCompetitor { get; set; } = "";
    public int TopCompetitorCitations { get; set; }
    public string CitationGap { get; set; } = "";
    
    // 行业数据
    public string MonthlySearches { get; set; } = "0";
    public int IndustryAvgScore { get; set; }
    
    // 问题示例
    public List<SampleQuestion> SampleQuestions { get; set; } = new();
    public int OverallMentionRate { get; set; }
    
    // 优化建议
    public List<string> MissingElements { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    
    // 潜在提升
    public string PotentialGain { get; set; } = "";
    public string PotentialTraffic { get; set; } = "";
    
    // 竞品列表
    public List<CompetitorInfo> Competitors { get; set; } = new();
}

public class BrandVisibilityResult
{
    public bool IsKnown { get; set; }
    public int Confidence { get; set; }
    public string Description { get; set; } = "";
    public int CitationLikelihood { get; set; }
    public string? IndustryCategory { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> MissingElements { get; set; } = new();
}

public class CompetitorInfo
{
    public string Name { get; set; } = "";
    public int CitationScore { get; set; }
    public int MonthlyMentions { get; set; }
    public List<string> Advantages { get; set; } = new();
}

public class SampleQuestionsResult
{
    public List<SampleQuestion> Questions { get; set; } = new();
    public int OverallMentionRate { get; set; }
}

public class SampleQuestion
{
    public string Question { get; set; } = "";
    public bool WillMention { get; set; }
    public string Reason { get; set; } = "";
    public List<string> CompetitorsMentioned { get; set; } = new();
}

#endregion
