using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using GeoCore.Data.DbContext;
using GeoCore.Data.Repositories;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 问题生成任务状态
/// </summary>
public class QuestionGenerationTask
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public string? Message { get; set; }
    public int Progress { get; set; } = 0; // 0-100
    public string CurrentModel { get; set; } = "";
    public int CompletedModels { get; set; } = 0;
    public int TotalModels { get; set; } = 0;
    public List<GeneratedQuestion>? Questions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // v4.5 新增：项目保存（Phase 1.6）
    public long? ProjectId { get; set; }  // 关联项目 ID
    public long? UserId { get; set; }     // 用户 ID
    public bool SavedToProject { get; set; } = false;  // 是否已保存到项目
}

/// <summary>
/// 蒸馏控制器 - 卖点蒸馏、受众推断、问题生成
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DistillationController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DistillationController> _logger;
    private readonly IConfiguration _configuration;
    private readonly PromptConfigRepository _promptRepo;
    private readonly SystemConfigRepository _systemRepo;
    private readonly ConfigCacheService _configCache;
    
    // 任务队列（内存存储，生产环境应使用 Redis）
    private static readonly ConcurrentDictionary<string, QuestionGenerationTask> _tasks = new();

    public DistillationController(
        IHttpClientFactory httpClientFactory,
        ILogger<DistillationController> logger,
        IConfiguration configuration,
        PromptConfigRepository promptRepo,
        SystemConfigRepository systemRepo,
        ConfigCacheService configCache)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _promptRepo = promptRepo;
        _systemRepo = systemRepo;
        _configCache = configCache;
    }

    #region 配置常量（可通过 Admin 后台覆盖）
    
    // 配置分类
    private const string ConfigCategory = "question_generation";
    
    // 默认值（数据库无配置时使用）
    private const int DefaultQuestionsPerModel = 5;
    private const int DefaultMaxCompetitors = 5;
    private const int DefaultKeywordMaxLength = 30;
    private const int DefaultMinSearchIndex = 0;
    private const int DefaultMinBrandFitIndex = 0;
    private const int DefaultMinScore = 0;
    
    /// <summary>
    /// 获取配置值（优先缓存，fallback 数据库，最终 fallback 默认值）
    /// </summary>
    private async Task<int> GetConfigValueAsync(string key, int defaultValue)
    {
        try
        {
            // 优先从缓存读取
            var cachedValue = _configCache.GetSystemIntValue(ConfigCategory, key, -1);
            if (cachedValue != -1) return cachedValue;

            // 缓存未命中，回退到数据库
            return await _systemRepo.GetIntValueAsync(ConfigCategory, key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取配置 {Key} 失败，使用默认值 {Default}", key, defaultValue);
            return defaultValue;
        }
    }
    
    #endregion

    /// <summary>
    /// 卖点蒸馏 - 从品牌信息中提取核心卖点
    /// </summary>
    [HttpPost("selling-points")]
    public async Task<IActionResult> DistillSellingPoints([FromBody] SellingPointsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { success = false, message = "品牌名称不能为空" });
        }

        try
        {
            var languages = request.Languages ?? new List<string> { "zh-CN" };
            if (languages.Count == 0) languages.Add("zh-CN");

            var countries = request.Countries ?? new List<string> { "CN" };
            if (countries.Count == 0) countries.Add("CN");

            // 构建国家-语言映射（如果前端传入了对应关系）
            // 默认：每个国家使用第一个语言，或根据国家代码推断语言
            var countryLanguageMap = BuildCountryLanguageMap(countries, languages);

            var allSellingPoints = new List<SellingPointItem>();

            // 按 国家×语言 组合并行生成卖点
            var tasks = new List<Task<(string country, string language, List<SellingPointItem> points)>>();
            foreach (var (country, language) in countryLanguageMap)
            {
                var capturedCountry = country;
                var capturedLang = language;
                var displayLang = GetDisplayLanguage(capturedLang);
                var displayMarket = GetDisplayMarket(capturedCountry);
                
                tasks.Add(Task.Run(async () =>
                {
                    _logger.LogInformation("生成卖点: 国家={Country}, 语言={Lang}", capturedCountry, capturedLang);
                    var prompt = BuildSellingPointsPrompt(request, displayLang, displayMarket);
                    var result = await CallAIAsync(prompt);
                    if (result != null)
                    {
                        var points = ParseSellingPointsResponse(result);
                        // 为每个卖点设置国家和语言
                        foreach (var p in points)
                        {
                            p.Country = capturedCountry;
                            p.Language = capturedLang;
                        }
                        return (capturedCountry, capturedLang, points);
                    }
                    return (capturedCountry, capturedLang, new List<SellingPointItem>());
                }));
            }

            var results = await Task.WhenAll(tasks);
            foreach (var (country, language, points) in results)
            {
                _logger.LogInformation("蒸馏结果: Country={Country}, Language={Language}, PointCount={Count}", country, language, points.Count);
                foreach (var p in points)
                {
                    _logger.LogDebug("  卖点: Point={Point}, Country={PointCountry}, Language={PointLang}", p.Point, p.Country, p.Language);
                }
                allSellingPoints.AddRange(points);
            }

            if (allSellingPoints.Count == 0)
            {
                return Ok(new { success = false, message = "AI 服务暂时不可用" });
            }

            // 合并后按权重降序排序
            allSellingPoints = allSellingPoints
                .OrderByDescending(p => p.Weight)
                .ToList();

            return Ok(new { success = true, data = allSellingPoints });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卖点蒸馏失败: {Brand}", request.BrandName);
            return Ok(new { success = false, message = "蒸馏过程中发生错误" });
        }
    }

    /// <summary>
    /// 卖点蒸馏 - 流式进度反馈版本（解决 Nginx 超时问题）
    /// </summary>
    [HttpPost("selling-points/stream")]
    public async Task DistillSellingPointsStream([FromBody] SellingPointsRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
        
        async Task SendEvent(string eventType, object data)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data, jsonOptions);
            await Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n");
            await Response.Body.FlushAsync();
        }

        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            await SendEvent("error", new { message = "品牌名称不能为空" });
            return;
        }

        try
        {
            var languages = request.Languages ?? new List<string> { "zh-CN" };
            if (languages.Count == 0) languages.Add("zh-CN");

            var countries = request.Countries ?? new List<string> { "CN" };
            if (countries.Count == 0) countries.Add("CN");

            var countryLanguageMap = BuildCountryLanguageMap(countries, languages);
            var totalTasks = countryLanguageMap.Count;
            var completedTasks = 0;
            var allSellingPoints = new List<SellingPointItem>();
            var lockObj = new object();

            // 发送开始事件
            await SendEvent("start", new { total = totalTasks, message = $"开始为 {totalTasks} 个国家/语言生成卖点..." });

            // 并行处理，每完成一个就发送进度
            var tasks = countryLanguageMap.Select(async kv =>
            {
                var (country, language) = kv;
                var displayLang = GetDisplayLanguage(language);
                var displayMarket = GetDisplayMarket(country);

                try
                {
                    _logger.LogInformation("生成卖点: 国家={Country}, 语言={Lang}", country, language);
                    var prompt = BuildSellingPointsPrompt(request, displayLang, displayMarket);
                    var result = await CallAIAsync(prompt);
                    
                    var points = new List<SellingPointItem>();
                    if (result != null)
                    {
                        points = ParseSellingPointsResponse(result);
                        foreach (var p in points)
                        {
                            p.Country = country;
                            p.Language = language;
                        }
                    }

                    lock (lockObj)
                    {
                        completedTasks++;
                        allSellingPoints.AddRange(points);
                    }

                    // 发送进度事件
                    await SendEvent("progress", new { 
                        completed = completedTasks, 
                        total = totalTasks, 
                        country, 
                        language,
                        pointCount = points.Count,
                        message = $"[{completedTasks}/{totalTasks}] {displayMarket} ({displayLang}) 完成，生成 {points.Count} 个卖点"
                    });

                    return (country, language, points);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "卖点生成失败: Country={Country}, Language={Language}", country, language);
                    
                    lock (lockObj)
                    {
                        completedTasks++;
                    }

                    await SendEvent("progress", new { 
                        completed = completedTasks, 
                        total = totalTasks, 
                        country, 
                        language,
                        pointCount = 0,
                        error = true,
                        message = $"[{completedTasks}/{totalTasks}] {displayMarket} ({displayLang}) 失败"
                    });

                    return (country, language, new List<SellingPointItem>());
                }
            }).ToList();

            // 等待所有任务完成
            await Task.WhenAll(tasks);

            // 合并后按权重降序排序
            allSellingPoints = allSellingPoints
                .OrderByDescending(p => p.Weight)
                .ToList();

            // 发送完成事件
            await SendEvent("complete", new { 
                success = allSellingPoints.Count > 0, 
                data = allSellingPoints,
                message = allSellingPoints.Count > 0 
                    ? $"卖点蒸馏完成，共生成 {allSellingPoints.Count} 个卖点" 
                    : "AI 服务暂时不可用"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "卖点蒸馏失败: {Brand}", request.BrandName);
            await SendEvent("error", new { message = "蒸馏过程中发生错误" });
        }
    }

    /// <summary>
    /// 目标受众推断
    /// </summary>
    [HttpPost("audience")]
    public async Task<IActionResult> InferAudience([FromBody] AudienceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Industry))
        {
            return BadRequest(new { success = false, message = "行业不能为空" });
        }

        try
        {
            var languages = request.Languages ?? new List<string> { "zh-CN" };
            if (languages.Count == 0) languages.Add("zh-CN");

            var countries = request.Countries ?? new List<string> { "CN" };
            if (countries.Count == 0) countries.Add("CN");

            // 构建国家-语言映射
            var countryLanguageMap = BuildCountryLanguageMap(countries, languages);

            var allResults = new List<AudienceResult>();

            // 按国家/语言并行推断受众
            var tasks = new List<Task<AudienceResult>>();
            foreach (var (country, language) in countryLanguageMap)
            {
                var capturedCountry = country;
                var capturedLang = language;
                var displayLang = GetDisplayLanguage(capturedLang);

                tasks.Add(Task.Run(async () =>
                {
                    _logger.LogInformation("推断受众: 国家={Country}, 语言={Lang}", capturedCountry, capturedLang);
                    var prompt = BuildAudiencePrompt(request, displayLang);
                    var result = await CallAIAsync(prompt);
                    if (result != null)
                    {
                        var audience = ParseAudienceResponse(result);
                        audience.Country = capturedCountry;
                        audience.Language = capturedLang;
                        return audience;
                    }
                    return new AudienceResult { Country = capturedCountry, Language = capturedLang };
                }));
            }

            var results = await Task.WhenAll(tasks);
            allResults.AddRange(results);

            if (allResults.Count == 0)
            {
                return Ok(new { success = false, message = "AI 服务暂时不可用" });
            }

            // 如果只有一个国家/语言，返回单个结果（向后兼容）
            if (allResults.Count == 1)
            {
                return Ok(new { success = true, data = allResults[0] });
            }

            // 多个国家/语言，返回数组
            return Ok(new { success = true, data = allResults });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "受众推断失败: {Industry}", request.Industry);
            return Ok(new { success = false, message = "推断过程中发生错误" });
        }
    }

    /// <summary>
    /// 按需生成软文 - 根据问题和引用源生成 Chain-of-Density 格式软文
    /// </summary>
    [HttpPost("generate-article")]
    public async Task<IActionResult> GenerateArticle([FromBody] GenerateArticleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { success = false, message = "问题不能为空" });
        }

        try
        {
            var prompt = BuildArticlePrompt(request);
            var result = await CallAIAsync(prompt);

            if (result == null)
            {
                return Ok(new { success = false, message = "AI 服务暂时不可用" });
            }

            return Ok(new { success = true, data = new { article = result } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "软文生成失败: {Question}", request.Question);
            return Ok(new { success = false, message = "生成过程中发生错误" });
        }
    }

    /// <summary>
    /// v4.3: 按需获取详细答案 - 轻量级模式下，用户点击后获取单个问题的详细答案
    /// </summary>
    [HttpPost("fetch-answer")]
    public async Task<IActionResult> FetchDetailedAnswer([FromBody] FetchAnswerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { success = false, message = "问题不能为空" });
        }
        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            return BadRequest(new { success = false, message = "模型ID不能为空" });
        }

        _logger.LogInformation("========== 按需获取详细答案 ==========");
        _logger.LogInformation("Question: {Question}, Model: {Model}, Mode: {Mode}", 
            request.Question.Length > 50 ? request.Question[..50] + "..." : request.Question,
            request.ModelId, request.AnswerMode);

        try
        {
            var taskId = $"fetch_{Guid.NewGuid():N}";
            string answer;
            List<string>? sources = null;
            BrandCitationAnalysis? brandAnalysis = null;
            
            // 根据答案模式选择不同的生成方式
            if (request.AnswerMode == AnswerMode.Simulation)
            {
                // AI 模拟模式
                var simPrompt = await LoadSimulationAnswerPromptAsync(
                    new QuestionsRequest
                    {
                        BrandName = request.BrandName,
                        ProductName = request.ProductName,
                        Industry = request.Industry,
                        Language = request.Language,
                        Competitors = request.Competitors
                    },
                    new List<QuestionOnlyItem> { new() { Question = request.Question } },
                    request.ModelId
                );
                
                var simResult = await CallAIAsync(simPrompt, request.ModelId);
                if (string.IsNullOrEmpty(simResult))
                {
                    return Ok(new { success = false, message = "AI 服务暂时不可用" });
                }
                
                var simAnswers = ParseSimulationAnswersResponse(simResult, request.ModelId, 
                    new List<QuestionOnlyItem> { new() { Question = request.Question } },
                    request.BrandName, request.Competitors);
                
                if (simAnswers.Count > 0)
                {
                    answer = simAnswers[0].Answer ?? "";
                    brandAnalysis = simAnswers[0].BrandAnalysis;
                }
                else
                {
                    answer = simResult;
                }
            }
            else
            {
                // 软文模式（Content）
                var contentPrompt = BuildArticlePrompt(new GenerateArticleRequest
                {
                    BrandName = request.BrandName,
                    ProductName = request.ProductName,
                    Industry = request.Industry,
                    Question = request.Question,
                    ModelId = request.ModelId,
                    Sources = request.Sources
                });
                
                answer = await CallAIAsync(contentPrompt, request.ModelId) ?? "";
            }
            
            // 如果需要，使用 Perplexity 获取来源
            if (request.FetchSources)
            {
                var step2Language = request.Language?.ToLower() == "en" ? "English" : "中文";
                var step2Prompt = $"{request.Question}\n\nPlease answer this question thoroughly and objectively. Use {step2Language}.";
                var step2Result = await CallAIAsync(step2Prompt, "perplexity",
                    systemPrompt: "You are a helpful research assistant. Provide comprehensive, well-researched answers based on current web data.");
                
                if (!string.IsNullOrEmpty(step2Result))
                {
                    var citationsMarker = "<!--CITATIONS:";
                    var citationsIdx = step2Result.IndexOf(citationsMarker);
                    if (citationsIdx >= 0)
                    {
                        var endIdx = step2Result.IndexOf("-->", citationsIdx);
                        if (endIdx > citationsIdx)
                        {
                            var citationsJson = step2Result.Substring(citationsIdx + citationsMarker.Length, endIdx - citationsIdx - citationsMarker.Length);
                            try { sources = System.Text.Json.JsonSerializer.Deserialize<List<string>>(citationsJson); } catch { }
                        }
                    }
                }
            }
            
            _logger.LogInformation("按需获取答案完成: ansLen={AnsLen}, srcCnt={SrcCnt}", 
                answer.Length, sources?.Count ?? 0);
            
            return Ok(new { 
                success = true, 
                data = new { 
                    answer, 
                    sources,
                    brandAnalysis,
                    modelId = request.ModelId
                } 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按需获取答案失败: {Question}", request.Question);
            return Ok(new { success = false, message = "获取答案过程中发生错误" });
        }
    }

    /// <summary>
    /// 品牌检查 - 判断品牌是否在 AI 训练数据中存在
    /// </summary>
    [HttpPost("brand-check")]
    public async Task<IActionResult> CheckBrand([FromBody] BrandCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { success = false, message = "品牌名称不能为空" });
        }

        try
        {
            var prompt = BuildBrandCheckPrompt(request);
            var result = await CallAIAsync(prompt);

            if (result == null)
            {
                return Ok(new { success = false, message = "AI 服务暂时不可用" });
            }

            var brandCheck = ParseBrandCheckResponse(result);
            _logger.LogInformation("品牌检查: {Brand}, 已知: {IsKnown}, 置信度: {Confidence}", 
                request.BrandName, brandCheck.IsKnown, brandCheck.Confidence);
            
            return Ok(new { success = true, data = brandCheck });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "品牌检查失败: {Brand}", request.BrandName);
            return Ok(new { success = false, message = "检查过程中发生错误" });
        }
    }

    /// <summary>
    /// 竞品信息提取 - 提取竞品的用户关注点
    /// </summary>
    [HttpPost("competitor-info")]
    public async Task<IActionResult> ExtractCompetitorInfo([FromBody] CompetitorInfoRequest request)
    {
        if (request.Competitors == null || request.Competitors.Count == 0)
        {
            return BadRequest(new { success = false, message = "竞品列表不能为空" });
        }

        try
        {
            var prompt = BuildCompetitorInfoPrompt(request);
            var result = await CallAIAsync(prompt);

            if (result == null)
            {
                return Ok(new { success = false, message = "AI 服务暂时不可用" });
            }

            var competitorInfo = ParseCompetitorInfoResponse(result);
            return Ok(new { success = true, data = competitorInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "竞品信息提取失败");
            return Ok(new { success = false, message = "提取过程中发生错误" });
        }
    }

    /// <summary>
    /// v4.0: 真实问题发现 - 使用 Perplexity 搜索 Reddit/论坛中的真实用户问题
    /// </summary>
    [HttpPost("real-questions")]
    public async Task<IActionResult> DiscoverRealQuestions([FromBody] RealQuestionsRequest request)
    {
        _logger.LogInformation("========== 真实问题发现 ==========");
        _logger.LogInformation("Brand: {Brand}, Industry: {Industry}, Keywords: [{Keywords}]", 
            request.BrandName, request.Industry, string.Join(",", request.Keywords ?? new List<string>()));
        
        if (string.IsNullOrWhiteSpace(request.BrandName) && (request.Keywords == null || request.Keywords.Count == 0))
        {
            return BadRequest(new { success = false, message = "品牌名称或关键词不能为空" });
        }

        try
        {
            var questions = await SearchRealQuestionsAsync(request);
            
            _logger.LogInformation("真实问题发现完成: 找到 {Count} 个问题", questions.Count);
            
            return Ok(new { 
                success = true, 
                data = new { 
                    questions = questions,
                    total = questions.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "真实问题发现失败");
            return Ok(new { success = false, message = "搜索过程中发生错误: " + ex.Message });
        }
    }

    /// <summary>
    /// 使用 Perplexity 搜索真实用户问题
    /// </summary>
    private async Task<List<GeneratedQuestion>> SearchRealQuestionsAsync(RealQuestionsRequest request)
    {
        var questions = new List<GeneratedQuestion>();
        
        // 构建搜索 Prompt
        var searchKeywords = request.Keywords != null && request.Keywords.Count > 0
            ? string.Join(", ", request.Keywords)
            : request.BrandName;
        
        var language = request.Language?.ToLower() == "en" ? "English" : "Chinese";
        var languageInstruction = language == "English" 
            ? "Search and respond in English"
            : "搜索并用中文回复";
        
        var prompt = $@"You are a research assistant specialized in finding real user questions from forums, Reddit, Quora, and other online communities.

## Task
Search for REAL questions that users are asking about: {searchKeywords}
Industry context: {request.Industry}
Brand (if relevant): {request.BrandName}

## Instructions
1. Search Reddit (especially subreddits like r/SEO, r/marketing, r/smallbusiness, r/entrepreneur), Quora, and other forums
2. Find questions that REAL users have asked (not AI-generated)
3. Focus on questions related to the keywords/brand
4. Include the source (e.g., ""r/SEO"", ""Quora"", ""知乎"") for each question
5. {languageInstruction}

## Output Format
Return a JSON object with this exact structure:
{{
  ""questions"": [
    {{
      ""question"": ""The actual question text"",
      ""source"": ""r/SEO"",
      ""sourceUrl"": ""https://reddit.com/r/SEO/..."",
      ""searchIndex"": 75,
      ""brandFitIndex"": 60,
      ""intent"": ""information"",
      ""stage"": ""awareness""
    }}
  ]
}}

## Scoring Guidelines
- searchIndex (0-100): How frequently this type of question is asked (higher = more common)
- brandFitIndex (0-100): How well the brand can answer this question (higher = better fit)

## Requirements
- Find 10-15 real questions
- Each question must have a real source
- Questions should be diverse (different intents, stages)
- Prioritize recent and popular questions";

        _logger.LogInformation("[RealQuestions] ========== 开始搜索真实问题 ==========");
        _logger.LogInformation("[RealQuestions] 参数: Brand={Brand}, Industry={Industry}, Keywords={Keywords}, Language={Lang}", 
            request.BrandName, request.Industry, searchKeywords, language);
        _logger.LogInformation("[RealQuestions] Prompt 长度: {Length}", prompt.Length);
        
        var searchStart = DateTime.UtcNow;
        
        // 调用 Perplexity API
        var result = await CallAIAsync(prompt, "perplexity",
            systemPrompt: "You are a research assistant that searches the web for real user questions. Always cite your sources and return valid JSON.");
        
        _logger.LogInformation("[RealQuestions] Perplexity 调用耗时: {Duration:F1}s", (DateTime.UtcNow - searchStart).TotalSeconds);
        
        if (string.IsNullOrEmpty(result))
        {
            _logger.LogWarning("[RealQuestions] Perplexity 返回空响应");
            return questions;
        }
        
        _logger.LogDebug("[RealQuestions] Perplexity 响应长度: {Length}", result.Length);
        
        // 解析响应
        try
        {
            var cleanJson = CleanJsonResponse(result);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;
            
            if (root.TryGetProperty("questions", out var questionsArray))
            {
                foreach (var q in questionsArray.EnumerateArray())
                {
                    var questionText = q.TryGetProperty("question", out var qProp) ? qProp.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(questionText)) continue;
                    
                    var searchIndex = q.TryGetProperty("searchIndex", out var si) && si.ValueKind == JsonValueKind.Number ? si.GetInt32() : 50;
                    var brandFitIndex = q.TryGetProperty("brandFitIndex", out var bfi) && bfi.ValueKind == JsonValueKind.Number ? bfi.GetInt32() : 50;
                    
                    questions.Add(new GeneratedQuestion
                    {
                        Model = "perplexity",
                        Question = questionText,
                        SearchIndex = searchIndex,
                        BrandFitIndex = brandFitIndex,
                        Score = searchIndex * brandFitIndex / 100,
                        Intent = q.TryGetProperty("intent", out var intent) ? intent.GetString() ?? "information" : "information",
                        Stage = q.TryGetProperty("stage", out var stage) ? stage.GetString() ?? "awareness" : "awareness",
                        Pattern = "real_question",
                        Source = "real",
                        SourceDetail = q.TryGetProperty("source", out var src) ? src.GetString() ?? "" : "",
                        SourceUrl = q.TryGetProperty("sourceUrl", out var url) ? url.GetString() ?? "" : ""
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[RealQuestions] 解析响应失败: {Response}", result.Length > 500 ? result[..500] : result);
        }
        
        _logger.LogInformation("[RealQuestions] ========== 搜索完成 ==========");
        _logger.LogInformation("[RealQuestions] 找到 {Count} 个真实问题", questions.Count);
        
        // 记录来源分布
        var sourceGroups = questions.GroupBy(q => q.SourceDetail ?? "unknown");
        foreach (var g in sourceGroups)
        {
            _logger.LogInformation("[RealQuestions] 来源分布: {Source} = {Count} 个", g.Key, g.Count());
        }
        
        // 按 Score 排序
        return questions.OrderByDescending(q => q.Score).ToList();
    }

    /// <summary>
    /// 问题集生成 - 异步任务模式（绕过 Cloudflare 100秒限制）
    /// 立即返回 taskId，前端轮询获取结果
    /// </summary>
    [HttpPost("questions")]
    public IActionResult GenerateQuestions([FromBody] QuestionsRequest request)
    {
        var taskId = Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogInformation("========== [{TaskId}] 问题生成任务创建 ==========", taskId);
        _logger.LogInformation("[{TaskId}] Brand: {Brand}, Models: [{Models}], Language: {Lang}, ProjectId: {ProjectId}", 
            taskId, request.BrandName, string.Join(",", request.Models), request.Language, request.ProjectId);
        
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { success = false, message = "品牌名称不能为空" });
        }

        // 获取用户 ID（从 Header 或 request）
        long? userId = request.UserId;
        if (!userId.HasValue && Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            if (long.TryParse(userIdHeader.FirstOrDefault(), out var parsedUserId))
            {
                userId = parsedUserId;
            }
        }
        
        // 创建任务
        var task = new QuestionGenerationTask
        {
            TaskId = taskId,
            Status = "processing",
            TotalModels = request.Models.Count,
            Message = "任务已创建，正在处理...",
            ProjectId = request.ProjectId,
            UserId = userId
        };
        _tasks[taskId] = task;

        // 在 HTTP 上下文中捕获 baseUrl（后台线程中无法访问 Request）
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        // 在后台线程执行任务（不阻塞请求）
        _ = Task.Run(async () => await ProcessQuestionGenerationAsync(taskId, request, baseUrl));

        // 立即返回 taskId
        return Ok(new { 
            success = true, 
            taskId = taskId,
            message = "任务已创建，请轮询 /api/distillation/questions/status/{taskId} 获取结果"
        });
    }

    /// <summary>
    /// 查询问题生成任务状态
    /// </summary>
    [HttpGet("questions/status/{taskId}")]
    public IActionResult GetQuestionStatus(string taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            return NotFound(new { success = false, message = "任务不存在" });
        }

        if (task.Status == "completed")
        {
            // 任务完成，返回结果并清理
            var result = new { 
                success = true, 
                status = task.Status,
                progress = task.Progress,
                completedModels = task.CompletedModels,
                totalModels = task.TotalModels,
                data = new { 
                    questions = task.Questions, 
                    total = task.Questions?.Count ?? 0
                }
            };
            
            // 5分钟后清理任务（避免内存泄漏）
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => _tasks.TryRemove(taskId, out QuestionGenerationTask? _));
            
            return Ok(result);
        }

        return Ok(new { 
            success = true, 
            status = task.Status,
            progress = task.Progress,
            completedModels = task.CompletedModels,
            totalModels = task.TotalModels,
            currentModel = task.CurrentModel,
            message = task.Message
        });
    }

    /// <summary>
    /// 后台处理问题生成任务
    /// </summary>
    private async Task ProcessQuestionGenerationAsync(string taskId, QuestionsRequest request, string baseUrl)
    {
        var requestStart = DateTime.UtcNow;
        var task = _tasks[taskId];
        
        // 全局超时保护：从数据库读取配置，默认 10 分钟
        var globalTimeoutMinutes = await GetConfigValueAsync("task_timeout_minutes", 10);
        
        try
        {
            // 收集已完成的结果（线程安全，供超时时提取部分结果）
            var allQuestions = new ConcurrentBag<GeneratedQuestion>();
            var completedCount = 0;
            
            // 获取所有选中的语言
            var languages = request.GetEffectiveLanguages();
            var totalSteps = request.Models.Count * languages.Count;

            _logger.LogInformation("[{TaskId}] 开始处理 {ModelCount} 个模型 x {LangCount} 种语言: [{Models}] x [{Languages}] (超时={Timeout}分钟)", 
                taskId, request.Models.Count, languages.Count, 
                string.Join(",", request.Models), string.Join(",", languages), globalTimeoutMinutes);
            
            // 各语言并行处理，大幅缩短总耗时
            _logger.LogInformation("[{TaskId}] ========== 开始按语言并行处理 ==========", taskId);
            _logger.LogInformation("[{TaskId}] 语言列表: [{Languages}], 国家列表: [{Countries}]", 
                taskId, string.Join(", ", languages), string.Join(", ", request.Countries ?? new List<string> { "CN" }));
            
            var langTasks = languages.Select(langCode =>
            {
                var langName = GetLanguageName(langCode);
                // v6.0: 从绑定信息获取正确的国家代码和卖点
                var countryCode = request.GetCountryForLanguage(langCode);
                var langSellingPoints = request.GetSellingPointsForLanguage(langCode);
                
                _logger.LogInformation("[{TaskId}] 开始处理: 国家={Country}, 语言={Language}, 卖点数={SPCount}", 
                    taskId, countryCode, langName, langSellingPoints.Count);
                
                // v6.1: 获取对应语言的受众
                var langPersonas = request.GetPersonasForLanguage(langCode);
                
                // 创建当前语言的请求副本（完整复制所有字段）
                var langRequest = new QuestionsRequest
                {
                    BrandName = request.BrandName,
                    ProductName = request.ProductName,
                    Industry = request.Industry,
                    SellingPoints = langSellingPoints,  // v6.0: 使用筛选后的卖点
                    Personas = langPersonas,  // v6.1: 使用筛选后的受众
                    Stages = request.Stages,
                    Models = request.Models,
                    Language = langCode,
                    Languages = new List<string> { langCode },
                    Markets = request.Markets,
                    Countries = new List<string> { countryCode },  // v6.0: 使用正确的国家
                    Region = request.Region,
                    Competitors = request.Competitors,
                    EnableGoogleTrends = request.EnableGoogleTrends,
                    EnableRedditSearch = request.EnableRedditSearch,
                    AnswerMode = request.AnswerMode
                };

                _logger.LogInformation("[{TaskId}][{Country}/{Lang}] 请求参数: Brand={Brand}, Industry={Industry}, SellingPoints=[{SP}], Personas=[{P}], EnableGoogleTrends={GT}, EnableRedditSearch={RS}",
                    taskId, countryCode, langCode, request.BrandName, request.Industry, 
                    string.Join(", ", langSellingPoints.Take(3)) + (langSellingPoints.Count > 3 ? "..." : ""), 
                    string.Join(", ", langPersonas.Take(3)) + (langPersonas.Count > 3 ? "..." : ""),
                    request.EnableGoogleTrends, request.EnableRedditSearch);

                return ProcessLanguageModelsAsync(taskId, task, langRequest, langName, baseUrl, totalSteps, () => Interlocked.Increment(ref completedCount));
            }).ToList();

            // 用 WhenAny + Delay 实现全局超时
            var workTask = Task.WhenAll(langTasks);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(globalTimeoutMinutes));
            var completedTask = await Task.WhenAny(workTask, timeoutTask);
            
            var timedOut = completedTask == timeoutTask;
            if (timedOut)
            {
                var totalDuration = (DateTime.UtcNow - requestStart).TotalSeconds;
                _logger.LogWarning("========== [{TaskId}] 问题生成任务超时 ({Timeout}分钟, 实际 {Duration:F1}s)，提取已完成的部分结果 ==========", 
                    taskId, globalTimeoutMinutes, totalDuration);
            }
            
            // 提取已完成的结果（超时时部分 Task 可能已完成）
            var allResults = new List<GeneratedQuestion>();
            foreach (var lt in langTasks)
            {
                try
                {
                    if (lt.IsCompleted && !lt.IsFaulted && !lt.IsCanceled)
                    {
                        allResults.AddRange(lt.Result);
                    }
                    else if (lt.IsFaulted)
                    {
                        _logger.LogWarning("[{TaskId}] 语言任务失败: {Error}", taskId, lt.Exception?.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[{TaskId}] 提取语言任务结果失败: {Error}", taskId, ex.Message);
                }
            }
            
            // 诊断日志：每个模型有多少条目，多少有答案
            var modelStats = allResults.GroupBy(q => q.Model)
                .Select(g => $"{g.Key}: {g.Count()} 条, 有答案 {g.Count(x => !string.IsNullOrEmpty(x.Answer))} 条")
                .ToList();
            _logger.LogInformation("[{TaskId}] 去重前统计: 总 {Total} 条 | {Stats}", 
                taskId, allResults.Count, string.Join(" | ", modelStats));

            // 去重（按问题文本+模型去重，优先保留有答案的条目）
            var deduped = allResults
                .GroupBy(q => new { q.Question, q.Model })
                .Select(g => g.OrderByDescending(x => string.IsNullOrEmpty(x.Answer) ? 0 : 1).First())
                .OrderByDescending(q => q.FreqScore)
                .ToList();

            // 诊断日志：去重后
            var dedupStats = deduped.GroupBy(q => q.Model)
                .Select(g => $"{g.Key}: {g.Count()} 条, 有答案 {g.Count(x => !string.IsNullOrEmpty(x.Answer))} 条")
                .ToList();
            _logger.LogInformation("[{TaskId}] 去重后统计: 总 {Total} 条 | {Stats}", 
                taskId, deduped.Count, string.Join(" | ", dedupStats));
            
            var totalDur = (DateTime.UtcNow - requestStart).TotalSeconds;
            
            // 更新任务状态
            task.Status = "completed";
            task.Progress = 100;
            task.Questions = deduped;
            task.CompletedAt = DateTime.UtcNow;
            
            // v4.5: 如果指定了 ProjectId，自动保存问题到数据库
            _logger.LogInformation("[{TaskId}] 检查是否需要保存问题: ProjectId={ProjectId}, UserId={UserId}, QuestionCount={Count}",
                taskId, task.ProjectId, task.UserId, deduped.Count);
            
            if (task.ProjectId.HasValue && task.UserId.HasValue && deduped.Count > 0)
            {
                _logger.LogInformation("[{TaskId}] 开始保存问题到项目 {ProjectId}", taskId, task.ProjectId.Value);
                try
                {
                    await SaveQuestionsToProjectAsync(taskId, task.ProjectId.Value, task.UserId.Value, deduped);
                    task.SavedToProject = true;
                    _logger.LogInformation("[{TaskId}] ✅ 问题已成功保存到项目 {ProjectId}, 共 {Count} 条", taskId, task.ProjectId.Value, deduped.Count);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "[{TaskId}] ❌ 保存问题到项目失败: {Error}", taskId, saveEx.Message);
                    // 保存失败不影响任务状态
                }
            }
            else
            {
                _logger.LogWarning("[{TaskId}] ⚠️ 跳过保存问题: ProjectId={ProjectId}, UserId={UserId}, QuestionCount={Count}",
                    taskId, task.ProjectId, task.UserId, deduped.Count);
            }
            
            if (timedOut)
            {
                _logger.LogWarning("========== [{TaskId}] 问题生成任务部分完成（超时）: {Total} 个问题, 耗时 {Duration:F1}s ==========", 
                    taskId, deduped.Count, totalDur);
                task.Message = $"部分完成（超时 {globalTimeoutMinutes} 分钟），已生成 {deduped.Count} 个问题";
            }
            else
            {
                _logger.LogInformation("========== [{TaskId}] 问题生成任务完成: 总问题数 {Total}, 总耗时 {Duration:F1}s ==========", 
                    taskId, deduped.Count, totalDur);
                task.Message = $"生成完成，共 {deduped.Count} 个问题";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "========== [{TaskId}] 问题生成任务失败: {Error} ==========", taskId, ex.Message);
            task.Status = "failed";
            task.Message = "生成失败: " + ex.Message;
        }
    }

    /// <summary>
    /// 保存问题到项目数据库 (Phase 1.6) - 优化版：批量插入
    /// </summary>
    private async Task SaveQuestionsToProjectAsync(string taskId, long projectId, long userId, List<GeneratedQuestion> questions)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("[{TaskId}] ========== 开始保存问题到数据库（批量模式）==========", taskId);
        _logger.LogInformation("[{TaskId}] 保存参数: ProjectId={ProjectId}, UserId={UserId}, 问题数={Count}", 
            taskId, projectId, userId, questions.Count);
        
        // 按国家-语言分组统计
        var countryLangGroups = questions.GroupBy(q => $"{q.Country ?? "CN"}/{q.Language ?? "zh-CN"}");
        foreach (var group in countryLangGroups)
        {
            _logger.LogInformation("[{TaskId}] 国家-语言分组: {Group} = {Count} 个问题", taskId, group.Key, group.Count());
        }
        
        var dbContext = new GeoDbContext();
        var questionRepo = new GeoQuestionRepository(dbContext);
        var platformRepo = new SysSourcePlatformRepository(dbContext);

        // 预加载所有平台域名映射（避免N+1查询）
        var allPlatforms = await platformRepo.GetAllPlatformsAsync();
        var domainToPlatform = allPlatforms
            .Where(p => !string.IsNullOrEmpty(p.Domain))
            .ToDictionary(p => p.Domain!.ToLower(), p => p);
        _logger.LogInformation("[{TaskId}] 预加载平台映射: {Count} 个", taskId, domainToPlatform.Count);

        // Step 1: 批量插入问题
        var questionDtos = questions.Select(q => new GeoQuestionDto
        {
            UserId = userId,
            ProjectId = projectId,
            TaskId = taskId,
            Question = q.Question,
            Country = q.Country ?? "CN",
            Language = q.Language ?? "zh-CN",
            Pattern = q.Pattern,
            Intent = q.Intent,
            Stage = q.Stage,
            Persona = q.Persona,
            SellingPoint = q.SellingPoint,
            QuestionSource = q.Source ?? "ai",
            SourceDetail = q.SourceDetail,
            SourceUrl = q.SourceUrl,
            GoogleTrendsHeat = q.GoogleTrendsHeat
        }).ToList();

        var questionIds = await questionRepo.CreateQuestionsAsync(questionDtos);
        _logger.LogInformation("[{TaskId}] Step1 批量插入问题完成: {Count} 条, 耗时 {Ms}ms", 
            taskId, questionIds.Count, sw.ElapsedMilliseconds);

        // Step 2: 批量插入答案
        var answerDtos = new List<GeoQuestionAnswerDto>();
        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var questionId = questionIds[i];
            answerDtos.Add(new GeoQuestionAnswerDto
            {
                UserId = userId,
                QuestionId = questionId,
                Model = q.Model,
                Answer = q.Answer,
                SearchIndex = q.SearchIndex,
                BrandFitIndex = q.BrandFitIndex,
                Score = q.Score,
                BrandAnalysis = q.BrandAnalysis != null ? JsonSerializer.Serialize(q.BrandAnalysis) : null,
                CitationDifficulty = q.CitationDifficulty != null ? JsonSerializer.Serialize(q.CitationDifficulty) : null,
                AnswerMode = q.AnswerModeUsed ?? "simulation"
            });
        }
        
        var answerCount = await questionRepo.BulkInsertAnswersAsync(answerDtos);
        _logger.LogInformation("[{TaskId}] Step2 批量插入答案完成: {Count} 条, 耗时 {Ms}ms", 
            taskId, answerCount, sw.ElapsedMilliseconds);

        // Step 3: 批量插入来源
        var sourceDtos = new List<GeoQuestionSourceDto>();
        for (int i = 0; i < questions.Count; i++)
        {
            var q = questions[i];
            var questionId = questionIds[i];
            if (q.Sources != null && q.Sources.Count > 0)
            {
                for (int j = 0; j < q.Sources.Count; j++)
                {
                    var sourceUrl = q.Sources[j];
                    var domain = ExtractDomain(sourceUrl);
                    
                    var sourceDto = new GeoQuestionSourceDto
                    {
                        UserId = userId,
                        QuestionId = questionId,
                        AnswerId = 0, // 批量模式不关联 AnswerId
                        Model = q.Model,
                        Url = sourceUrl,
                        Domain = domain,
                        AuthorityScore = 50,
                        SortOrder = j
                    };

                    // 从预加载的映射中查找平台
                    if (!string.IsNullOrEmpty(domain) && domainToPlatform.TryGetValue(domain.ToLower(), out var platform))
                    {
                        sourceDto.PlatformId = platform.Id;
                        sourceDto.AuthorityScore = platform.AuthorityBaseScore;
                    }
                    
                    sourceDtos.Add(sourceDto);
                }
            }
        }
        
        var sourceCount = await questionRepo.BulkInsertSourcesAsync(sourceDtos);
        sw.Stop();
        
        _logger.LogInformation("[{TaskId}] Step3 批量插入来源完成: {Count} 条, 耗时 {Ms}ms", 
            taskId, sourceCount, sw.ElapsedMilliseconds);
        _logger.LogInformation("[{TaskId}] ========== 保存完成 ==========", taskId);
        _logger.LogInformation("[{TaskId}] 保存结果: 成功={Saved}, 失败=0, 总计={Total}, ProjectId={ProjectId}, 总耗时={Ms}ms", 
            taskId, questions.Count, questions.Count, projectId, sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// 从 URL 提取域名
    /// </summary>
    private static string? ExtractDomain(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return null;
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 处理单个语言下所有模型（模型间并行，语言间并行）
    /// </summary>
    private async Task<List<GeneratedQuestion>> ProcessLanguageModelsAsync(
        string taskId, QuestionGenerationTask task, QuestionsRequest langRequest,
        string langName, string baseUrl, int totalSteps, Func<int> incrementCompleted)
    {
        var langQuestions = new List<GeneratedQuestion>();

        var modelTasks = langRequest.Models.Select(async modelId =>
        {
            var modelStart = DateTime.UtcNow;
            var modelQuestions = new List<GeneratedQuestion>();

            try
            {
                _logger.LogInformation("[{TaskId}][{Model}][{Lang}] 开始处理", taskId, modelId, langName);

                if (modelId.ToLower() == "perplexity")
                {
                    modelQuestions = await ProcessPerplexityModel(taskId, langRequest);
                }
                else
                {
                    modelQuestions = await ProcessOtherModel(taskId, langRequest, modelId, baseUrl);
                }

                var modelDuration = (DateTime.UtcNow - modelStart).TotalSeconds;
                _logger.LogInformation("[{TaskId}][{Model}][{Lang}] 完成, 生成 {Count} 个问题, 耗时 {Duration:F1}s",
                    taskId, modelId, langName, modelQuestions.Count, modelDuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{TaskId}][{Model}][{Lang}] 处理异常: {Error}", taskId, modelId, langName, ex.Message);
            }

            // 更新进度（线程安全）
            var completed = incrementCompleted();
            task.CompletedModels = completed;
            task.Progress = (int)(completed * 100.0 / totalSteps);
            task.Message = $"已完成 {completed}/{totalSteps} ({langName})";

            return modelQuestions;
        }).ToList();

        var results = await Task.WhenAll(modelTasks);
        foreach (var modelQuestions in results)
        {
            langQuestions.AddRange(modelQuestions);
        }

        return langQuestions;
    }

    /// <summary>
    /// 处理 Perplexity 模型：Step1 生成问题列表 -> 过滤排序 -> Step2 逐个问题调用 Perplexity 获取详细回答
    /// </summary>
    private async Task<List<GeneratedQuestion>> ProcessPerplexityModel(string taskId, QuestionsRequest request)
    {
        var questions = new List<GeneratedQuestion>();
        
        // ===== Step 1: 生成问题列表（不含答案） =====
        _logger.LogInformation("[{TaskId}][perplexity] Step1: 生成问题列表（不含答案）", taskId);
        
        var prompt = await LoadQuestionGenerationPromptAsync(request, "perplexity");
        WriteDiagFile(taskId, "perplexity", "step1_prompt", prompt);
        
        var result = await CallAIAsync(prompt, "perplexity",
            systemPrompt: "You are a helpful research assistant. Provide well-researched, objective answers based on current web data. Always respond with valid JSON.");
        WriteDiagFile(taskId, "perplexity", "step1_response", result ?? "(empty)");
        
        // 重试一次
        if (string.IsNullOrEmpty(result))
        {
            _logger.LogWarning("[{TaskId}][perplexity] Step1 首次调用返回空，等待5秒后重试", taskId);
            await Task.Delay(5000);
            result = await CallAIAsync(prompt, "perplexity",
                systemPrompt: "You are a helpful research assistant. Provide well-researched, objective answers based on current web data. Always respond with valid JSON.");
            WriteDiagFile(taskId, "perplexity", "step1_retry_response", result ?? "(empty)");
        }
        
        if (string.IsNullOrEmpty(result))
        {
            _logger.LogWarning("[{TaskId}][perplexity] Step1 AI返回空响应（含重试），问题生成失败", taskId);
            return questions;
        }
        
        // v5.2: 传入 country/language 确保问题与答案的国家一致
        var country = request.Countries?.FirstOrDefault() ?? "CN";
        var language = request.Language ?? "zh-CN";
        var rawQuestions = ParseQuestionsOnlyResponse(result, "perplexity", country, language);
        _logger.LogInformation("[{TaskId}][perplexity] Step1 完成: 生成 {Count} 个问题 (Country={Country})", taskId, rawQuestions.Count, country);
        foreach (var rq in rawQuestions)
        {
            _logger.LogInformation("[{TaskId}][perplexity] Step1 Q: {Question} | SI={SI} BFI={BFI} Score={Score}",
                taskId, rq.Question.Length > 40 ? rq.Question[..40] + "..." : rq.Question,
                rq.SearchIndex, rq.BrandFitIndex, rq.Score);
        }
        
        if (rawQuestions.Count == 0) return questions;
        
        // ===== 过滤 + 排序，取 Top N =====
        var questionsPerModel = await GetConfigValueAsync("questions_per_model", DefaultQuestionsPerModel);
        var minSearchIndex = await GetConfigValueAsync("min_search_index", DefaultMinSearchIndex);
        var minBrandFitIndex = await GetConfigValueAsync("min_brand_fit_index", DefaultMinBrandFitIndex);
        var minScore = await GetConfigValueAsync("min_score", DefaultMinScore);
        
        var filteredQuestions = rawQuestions
            .Where(q => q.SearchIndex >= minSearchIndex)
            .Where(q => q.BrandFitIndex >= minBrandFitIndex)
            .Where(q => q.Score >= minScore)
            .ToList();
        
        _logger.LogInformation("[{TaskId}][perplexity] 阈值过滤: {Before} -> {After}", 
            taskId, rawQuestions.Count, filteredQuestions.Count);
        
        var questionsToProcess = filteredQuestions
            .OrderByDescending(q => q.Score)
            .Take(questionsPerModel)
            .ToList();
        
        _logger.LogInformation("[{TaskId}][perplexity] 选取 Top{N} 高价值问题", taskId, questionsToProcess.Count);
        
        // ===== Step 2: 逐个问题调用 Perplexity 获取详细回答 =====
        _logger.LogInformation("[{TaskId}][perplexity] Step2: 逐个问题获取详细回答 ({Count} 个)", taskId, questionsToProcess.Count);
        
        var step2Language = language == "en" ? "English" : "中文";
        
        for (int i = 0; i < questionsToProcess.Count; i++)
        {
            var q = questionsToProcess[i];
            _logger.LogInformation("[{TaskId}][perplexity] Step2 [{Index}/{Total}]: {Question}", 
                taskId, i + 1, questionsToProcess.Count, 
                q.Question.Length > 50 ? q.Question[..50] + "..." : q.Question);
            
            var answerPrompt = $"{q.Question}\n\nPlease answer this question thoroughly and objectively, as you would naturally respond to a real user. Include specific product names, features, comparisons, and practical recommendations where relevant. Use {step2Language}.";
            WriteDiagFile(taskId, "perplexity", $"step2_q{i}_prompt", answerPrompt);
            
            var answerResult = await CallAIAsync(answerPrompt, "perplexity",
                systemPrompt: "You are a helpful research assistant. Provide comprehensive, well-researched answers based on current web data.");
            WriteDiagFile(taskId, "perplexity", $"step2_q{i}_response", answerResult ?? "(empty)");
            
            // 提取 citations（CallAIAsync 返回的是 tuple 的 content 部分，citations 在 response JSON 中）
            // answerResult 已经是 content string，citations 需要从全局提取
            var answer = answerResult ?? "";
            List<string>? sources = null;
            
            // 尝试从 answer 中提取 citations marker（如果 CallAIAsync 附加了的话）
            var citationsMarker = "<!--CITATIONS:";
            var citationsIdx = answer.IndexOf(citationsMarker);
            if (citationsIdx >= 0)
            {
                var endIdx = answer.IndexOf("-->", citationsIdx);
                if (endIdx > citationsIdx)
                {
                    var citationsJson = answer.Substring(citationsIdx + citationsMarker.Length, endIdx - citationsIdx - citationsMarker.Length);
                    try { sources = System.Text.Json.JsonSerializer.Deserialize<List<string>>(citationsJson); } catch { }
                    answer = answer.Substring(0, citationsIdx).Trim();
                }
            }
            
            questions.Add(new GeneratedQuestion
            {
                Model = "perplexity",
                Question = q.Question,
                SearchIndex = q.SearchIndex,
                BrandFitIndex = q.BrandFitIndex,
                Score = q.Score,
                Pattern = q.Pattern,
                Intent = q.Intent,
                Stage = q.Stage,
                Persona = q.Persona,
                SellingPoint = q.SellingPoint,
                Answer = answer,
                Sources = sources,
                PerplexityValidated = true
            });
            
            _logger.LogInformation("[{TaskId}][perplexity] Step2 [{Index}/{Total}] 完成: ansLen={AnsLen} srcCnt={SrcCnt}",
                taskId, i + 1, questionsToProcess.Count, answer.Length, sources?.Count ?? 0);
        }
        
        _logger.LogInformation("[{TaskId}][perplexity] 全部完成: {Count} 个问题已获取详细回答", taskId, questions.Count);
        return questions;
    }

    /// <summary>
    /// 处理其他模型：Step1 生成问题 -> Step2 Perplexity验证(客观回答) -> Step3 原模型回答 -> Step4 Google Trends验证
    /// v2.0: 增强 Perplexity 验证为客观回答模拟，添加 Google Trends 热度验证
    /// </summary>
    private async Task<List<GeneratedQuestion>> ProcessOtherModel(string taskId, QuestionsRequest request, string modelId, string baseUrl = "http://localhost:8080")
    {
        var questions = new List<GeneratedQuestion>();
        var stepStart = DateTime.UtcNow;
        
        // v5.0: 获取国家和语言信息
        var country = request.Countries?.FirstOrDefault() ?? "CN";
        var language = request.Language ?? "zh-CN";
        
        _logger.LogInformation("[{TaskId}][{Model}] ========== 开始处理模型 ==========", taskId, modelId);
        _logger.LogInformation("[{TaskId}][{Model}] 基础参数: Country={Country}, Language={Lang}, Brand={Brand}, Industry={Industry}", 
            taskId, modelId, country, language, request.BrandName, request.Industry);
        
        // 详细记录卖点和受众信息
        var sellingPointsStr = request.SellingPoints != null && request.SellingPoints.Count > 0 
            ? string.Join(", ", request.SellingPoints) : "(无)";
        var personasStr = request.Personas != null && request.Personas.Count > 0 
            ? string.Join(", ", request.Personas) : "(无)";
        _logger.LogInformation("[{TaskId}][{Model}] 卖点(SellingPoints): {SellingPoints}", taskId, modelId, sellingPointsStr);
        _logger.LogInformation("[{TaskId}][{Model}] 受众(Personas): {Personas}", taskId, modelId, personasStr);
        _logger.LogInformation("[{TaskId}][{Model}] 开关: EnableRedditSearch={RS}, EnableGoogleTrends={GT}, AnswerMode={AM}", 
            taskId, modelId, request.EnableRedditSearch, request.EnableGoogleTrends, request.AnswerMode);
        
        // v4.2: Step 0 - Reddit/论坛真实问题搜索（必须启用）
        List<GeneratedQuestion>? realQuestions = null;
        if (request.EnableRedditSearch)
        {
            _logger.LogInformation("[{TaskId}][{Model}] Step0: Reddit/论坛真实问题搜索 (Country={Country}, Lang={Lang})", 
                taskId, modelId, country, language);
            try
            {
                var realRequest = new RealQuestionsRequest
                {
                    BrandName = request.BrandName,
                    Industry = request.Industry,
                    Keywords = request.SellingPoints,
                    Language = request.Language
                };
                realQuestions = await SearchRealQuestionsAsync(realRequest);
                _logger.LogInformation("[{TaskId}][{Model}] Step0 完成: 找到 {Count} 个真实问题, 耗时 {Duration:F1}s", 
                    taskId, modelId, realQuestions.Count, (DateTime.UtcNow - stepStart).TotalSeconds);
                
                // 记录真实问题详情
                foreach (var rq in realQuestions.Take(3))
                {
                    _logger.LogDebug("[{TaskId}][{Model}] Step0 真实问题: {Q} (来源: {Source})", 
                        taskId, modelId, rq.Question?.Substring(0, Math.Min(50, rq.Question?.Length ?? 0)), rq.SourceDetail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{TaskId}][{Model}] Step0 Reddit搜索失败: {Error}，继续AI生成", taskId, modelId, ex.Message);
            }
        }
        else
        {
            _logger.LogInformation("[{TaskId}][{Model}] Step0: Reddit搜索已禁用，跳过", taskId, modelId);
        }
        
        // Step 1: 原模型生成问题（含双指标评分）
        _logger.LogInformation("[{TaskId}][{Model}] Step1: 生成问题（含 searchIndex + brandFitIndex）", taskId, modelId);
        var questionPrompt = await LoadQuestionGenerationPromptAsync(request, modelId);
        WriteDiagFile(taskId, modelId, "step1_prompt", questionPrompt);
        var questionResult = await CallAIAsync(questionPrompt, modelId);
        WriteDiagFile(taskId, modelId, "step1_response", questionResult ?? "(empty)");
        
        // 重试一次（并行场景下 API 可能偶发失败）
        if (string.IsNullOrEmpty(questionResult))
        {
            _logger.LogWarning("[{TaskId}][{Model}] Step1 首次调用返回空，等待5秒后重试", taskId, modelId);
            await Task.Delay(5000);
            questionResult = await CallAIAsync(questionPrompt, modelId);
            WriteDiagFile(taskId, modelId, "step1_retry_response", questionResult ?? "(empty)");
        }
        
        if (string.IsNullOrEmpty(questionResult))
        {
            _logger.LogWarning("[{TaskId}][{Model}] Step1 AI返回空响应（含重试），问题生成失败", taskId, modelId);
            return questions;
        }
        
        // v5.2: 传入 country/language 确保问题与答案的国家一致（使用方法开头定义的 country/language）
        var rawQuestions = ParseQuestionsOnlyResponse(questionResult, modelId, country, language);
        
        // v5.3: 详细日志 - AI生成问题列表（按国家，含卖点受众）
        _logger.LogInformation("[{TaskId}][{Model}] ========== AI生成问题汇总 (Country={Country}, Lang={Lang}) ==========", 
            taskId, modelId, country, language);
        _logger.LogInformation("[{TaskId}][{Model}] AI生成问题数量: {Count}", taskId, modelId, rawQuestions.Count);
        for (int idx = 0; idx < rawQuestions.Count; idx++)
        {
            var rq = rawQuestions[idx];
            _logger.LogInformation("[{TaskId}][{Model}] AI问题[{Index}]: {Question}", 
                taskId, modelId, idx + 1, rq.Question);
            _logger.LogInformation("[{TaskId}][{Model}]   -> SI={SI}, BFI={BFI}, Score={Score}, Pattern={Pattern}, Intent={Intent}, Stage={Stage}",
                taskId, modelId, rq.SearchIndex, rq.BrandFitIndex, rq.Score, rq.Pattern, rq.Intent, rq.Stage);
            _logger.LogInformation("[{TaskId}][{Model}]   -> 卖点={SellingPoint}, 受众={Persona}",
                taskId, modelId, rq.SellingPoint ?? "(无)", rq.Persona ?? "(无)");
        }
        _logger.LogInformation("[{TaskId}][{Model}] ========== AI生成问题汇总结束 ==========", taskId, modelId);
        
        // v4.2: 合并 Reddit/论坛真实问题（去重）
        if (realQuestions != null && realQuestions.Count > 0)
        {
            // v5.3: 详细日志 - Reddit问题列表（按国家）
            _logger.LogInformation("[{TaskId}][{Model}] ========== Reddit真实问题汇总 (Country={Country}, Lang={Lang}) ==========", 
                taskId, modelId, country, language);
            _logger.LogInformation("[{TaskId}][{Model}] Reddit问题数量: {Count}", taskId, modelId, realQuestions.Count);
            for (int idx = 0; idx < realQuestions.Count; idx++)
            {
                var rq = realQuestions[idx];
                _logger.LogInformation("[{TaskId}][{Model}] Reddit问题[{Index}]: {Question}", 
                    taskId, modelId, idx + 1, rq.Question);
                _logger.LogInformation("[{TaskId}][{Model}]   -> 来源: {Source}, URL: {Url}",
                    taskId, modelId, rq.SourceDetail ?? "unknown", rq.SourceUrl ?? "N/A");
            }
            _logger.LogInformation("[{TaskId}][{Model}] ========== Reddit真实问题汇总结束 ==========", taskId, modelId);
            
            var existingQuestions = new HashSet<string>(rawQuestions.Select(q => q.Question.ToLower().Trim()));
            var addedCount = 0;
            foreach (var rq in realQuestions)
            {
                var normalizedQ = rq.Question.ToLower().Trim();
                if (!existingQuestions.Contains(normalizedQ))
                {
                    rawQuestions.Add(new QuestionOnlyItem
                    {
                        Question = rq.Question,
                        SearchIndex = rq.SearchIndex > 0 ? rq.SearchIndex : 70,
                        BrandFitIndex = rq.BrandFitIndex > 0 ? rq.BrandFitIndex : 60,
                        Score = rq.Score > 0 ? rq.Score : 4200,
                        Pattern = rq.Pattern ?? "real_question",
                        Intent = rq.Intent ?? "information",
                        Stage = rq.Stage ?? "awareness",
                        Persona = rq.Persona,
                        SellingPoint = rq.SellingPoint,
                        Source = "real",
                        SourceDetail = rq.SourceDetail,
                        SourceUrl = rq.SourceUrl,
                        // v5.2: 设置国家和语言
                        Country = country,
                        Language = language
                    });
                    existingQuestions.Add(normalizedQ);
                    addedCount++;
                }
            }
            _logger.LogInformation("[{TaskId}][{Model}] 合并真实问题: {Added}/{Total} 个（去重后）", taskId, modelId, addedCount, realQuestions.Count);
        }
        
        if (rawQuestions.Count == 0) return questions;
        
        // v2.0: 从数据库获取配置，按 score 排序，取前 N 个高价值问题
        var questionsPerModel = await GetConfigValueAsync("questions_per_model", DefaultQuestionsPerModel);
        var minSearchIndex = await GetConfigValueAsync("min_search_index", DefaultMinSearchIndex);
        var minBrandFitIndex = await GetConfigValueAsync("min_brand_fit_index", DefaultMinBrandFitIndex);
        var minScore = await GetConfigValueAsync("min_score", DefaultMinScore);
        
        // 应用阈值过滤
        var filteredQuestions = rawQuestions
            .Where(q => q.SearchIndex >= minSearchIndex)
            .Where(q => q.BrandFitIndex >= minBrandFitIndex)
            .Where(q => q.Score >= minScore)
            .ToList();
        
        _logger.LogInformation("[{TaskId}][{Model}] 阈值过滤: {Before} -> {After} (minSearchIndex={MinSI}, minBrandFitIndex={MinBFI}, minScore={MinS})", 
            taskId, modelId, rawQuestions.Count, filteredQuestions.Count, minSearchIndex, minBrandFitIndex, minScore);
        
        var questionsToProcess = filteredQuestions
            .OrderByDescending(q => q.Score)
            .Take(questionsPerModel)
            .ToList();
        
        _logger.LogInformation("[{TaskId}][{Model}] 选取 Top{N} 高价值问题（按 score 排序）", taskId, modelId, questionsPerModel);
        
        // v4.3: 轻量级模式 - 只返回问题+元数据，跳过答案生成
        if (request.EnableLightweightMode)
        {
            _logger.LogInformation("[{TaskId}][{Model}] 轻量级模式: 跳过答案生成，只返回问题+元数据", taskId, modelId);
            foreach (var sq in questionsToProcess)
            {
                questions.Add(new GeneratedQuestion
                {
                    Model = modelId,
                    Question = sq.Question,
                    SearchIndex = sq.SearchIndex,
                    BrandFitIndex = sq.BrandFitIndex,
                    Score = sq.Score,
                    Pattern = sq.Pattern,
                    Intent = sq.Intent,
                    Stage = sq.Stage,
                    Persona = sq.Persona,
                    SellingPoint = sq.SellingPoint,
                    Answer = null,  // 轻量级模式不生成答案
                    Sources = null,
                    PerplexityValidated = false,
                    Source = sq.Source,
                    SourceDetail = sq.SourceDetail,
                    SourceUrl = sq.SourceUrl,
                    LightweightMode = true  // 标记为轻量级模式
                });
            }
            _logger.LogInformation("[{TaskId}][{Model}] 轻量级模式完成: {Count} 个问题", taskId, modelId, questions.Count);
            return questions;
        }
        
        // Step 2: Perplexity 逐个问题获取客观回答（每个问题单独调用，确保回答质量）
        // v4.5: 恢复简单问答方式以保留 Perplexity 的真实引用来源
        _logger.LogInformation("[{TaskId}][{Model}] Step2: Perplexity 逐个问题获取客观回答 ({Count} 个)", taskId, modelId, questionsToProcess.Count);
        
        var step2Language = request.Language?.ToLower() == "en" ? "English" : "中文";
        // 收集 Step2 Perplexity 的 sources，供 Step3 原模型结果合并使用
        var perplexitySourcesMap = new Dictionary<int, List<string>>();
        
        for (int i = 0; i < questionsToProcess.Count; i++)
        {
            var sq = questionsToProcess[i];
            _logger.LogInformation("[{TaskId}][{Model}] Step2 [{Index}/{Total}]: {Question}", 
                taskId, modelId, i + 1, questionsToProcess.Count, 
                sq.Question.Length > 50 ? sq.Question[..50] + "..." : sq.Question);
            
            // v4.5: 使用简单问答 prompt 以保留 Perplexity 的真实引用来源
            // Perplexity 的主要价值是提供真实的引用来源，而不是独立评分
            var step2Prompt = $"{sq.Question}\n\nPlease answer this question thoroughly and objectively, as you would naturally respond to a real user. Include specific product names, features, comparisons, and practical recommendations where relevant. Use {step2Language}.";
            WriteDiagFile(taskId, modelId, $"step2_q{i}_prompt", step2Prompt);
            
            var step2Result = await CallAIAsync(step2Prompt, "perplexity",
                systemPrompt: "You are a helpful research assistant. Provide comprehensive, well-researched answers based on current web data.");
            WriteDiagFile(taskId, modelId, $"step2_q{i}_response", step2Result ?? "(empty)");
            
            var step2Answer = step2Result ?? "";
            List<string>? step2Sources = null;
            
            // 提取 Perplexity citations marker
            var citationsMarker = "<!--CITATIONS:";
            var citationsIdx = step2Answer.IndexOf(citationsMarker);
            if (citationsIdx >= 0)
            {
                var endIdx = step2Answer.IndexOf("-->", citationsIdx);
                if (endIdx > citationsIdx)
                {
                    var citationsJson = step2Answer.Substring(citationsIdx + citationsMarker.Length, endIdx - citationsIdx - citationsMarker.Length);
                    try { step2Sources = System.Text.Json.JsonSerializer.Deserialize<List<string>>(citationsJson); } catch { }
                    step2Answer = step2Answer.Substring(0, citationsIdx).Trim();
                }
            }
            
            // v4.5: 只有当 Perplexity 返回有效答案时才创建记录
            if (!string.IsNullOrEmpty(step2Answer))
            {
                questions.Add(new GeneratedQuestion
                {
                    Model = "perplexity",
                    Question = sq.Question,
                    SearchIndex = sq.SearchIndex,
                    BrandFitIndex = sq.BrandFitIndex,
                    Score = sq.Score,
                    Pattern = sq.Pattern,
                    Intent = sq.Intent,
                    Stage = sq.Stage,
                    Persona = sq.Persona,
                    SellingPoint = sq.SellingPoint,
                    Answer = step2Answer,
                    Sources = step2Sources,
                    PerplexityValidated = true,
                    // v4.2: 传递来源标注
                    Source = sq.Source,
                    SourceDetail = sq.SourceDetail,
                    SourceUrl = sq.SourceUrl
                });
            }
            else
            {
                _logger.LogWarning("[{TaskId}][{Model}] Step2 [{Index}] Perplexity 返回空答案，跳过创建记录", taskId, modelId, i + 1);
            }
            
            // 保存 sources 到 map，供 Step3 合并
            if (step2Sources != null && step2Sources.Count > 0)
            {
                perplexitySourcesMap[i] = step2Sources;
            }
            
            _logger.LogInformation("[{TaskId}][{Model}] Step2 [{Index}/{Total}] 完成: ansLen={AnsLen} srcCnt={SrcCnt}",
                taskId, modelId, i + 1, questionsToProcess.Count, step2Answer.Length, step2Sources?.Count ?? 0);
        }
        
        // v5.3: 详细日志 - Perplexity验证结果汇总
        _logger.LogInformation("[{TaskId}][{Model}] ========== Perplexity验证结果汇总 (Country={Country}) ==========", 
            taskId, modelId, country);
        _logger.LogInformation("[{TaskId}][{Model}] Perplexity验证问题数: {Count}", taskId, modelId, questions.Count);
        for (int idx = 0; idx < questions.Count; idx++)
        {
            var pq = questions[idx];
            _logger.LogInformation("[{TaskId}][{Model}] Perplexity[{Index}]: Validated={V}, AnsLen={Len}, Sources={Src}, Q={Question}", 
                taskId, modelId, idx + 1, pq.PerplexityValidated, pq.Answer?.Length ?? 0, pq.Sources?.Count ?? 0,
                pq.Question.Length > 50 ? pq.Question[..50] + "..." : pq.Question);
        }
        _logger.LogInformation("[{TaskId}][{Model}] ========== Perplexity验证结果汇总结束 ==========", taskId, modelId);
        
        // Step 3: 原模型逐个问题生成回答（v3.1 逐个调用，确保回答质量）
        _logger.LogInformation("[{TaskId}][{Model}] Step3: 逐个问题生成答案 (模式: {Mode}, {Count} 个)", taskId, modelId, request.AnswerMode, questionsToProcess.Count);
        
        for (int i = 0; i < questionsToProcess.Count; i++)
        {
            var sq = questionsToProcess[i];
            _logger.LogInformation("[{TaskId}][{Model}] Step3 [{Index}/{Total}]: Country={Country}, Lang={Lang}, Q={Question}", 
                taskId, modelId, i + 1, questionsToProcess.Count, sq.Country, sq.Language,
                sq.Question.Length > 50 ? sq.Question[..50] + "..." : sq.Question);
            
            if (request.AnswerMode == AnswerMode.Simulation)
            {
                // AI 模拟模式：逐个问题模拟 AI 引擎的自然回答
                var singleSimPrompt = await LoadSimulationAnswerPromptAsync(request, new List<QuestionOnlyItem> { sq }, modelId);
                WriteDiagFile(taskId, modelId, $"step3_sim_q{i}_prompt", singleSimPrompt);
                
                var simResult = await CallAIAsync(singleSimPrompt, modelId);
                // 重试一次
                if (string.IsNullOrEmpty(simResult))
                {
                    await Task.Delay(3000);
                    simResult = await CallAIAsync(singleSimPrompt, modelId);
                }
                WriteDiagFile(taskId, modelId, $"step3_sim_q{i}_response", simResult ?? "(empty)");
                
                if (!string.IsNullOrEmpty(simResult))
                {
                    var simAnswers = ParseSimulationAnswersResponse(simResult, modelId, new List<QuestionOnlyItem> { sq }, request.BrandName, request.Competitors);
                    if (simAnswers.Count > 0)
                    {
                        var sa = simAnswers[0];
                        // 合并 Step2 Perplexity 的 sources 到原模型结果
                        if (perplexitySourcesMap.TryGetValue(i, out var pSources))
                        {
                            sa.Sources = (sa.Sources ?? new List<string>()).Concat(pSources).Distinct().ToList();
                        }
                        _logger.LogInformation("[{TaskId}][{Model}] Step3 [{Index}/{Total}] 完成: Country={Country}, ansLen={AnsLen} mentioned={Mentioned} srcCnt={SrcCnt}",
                            taskId, modelId, i + 1, questionsToProcess.Count, sa.Country, sa.Answer?.Length ?? 0, sa.BrandAnalysis?.Mentioned ?? false, sa.Sources?.Count ?? 0);
                        questions.AddRange(simAnswers);
                    }
                    else
                    {
                        questions.Add(CreateEmptyQuestion(sq, modelId, "simulation"));
                    }
                }
                else
                {
                    _logger.LogWarning("[{TaskId}][{Model}] Step3 [{Index}] AI模拟回答失败", taskId, modelId, i + 1);
                    questions.Add(CreateEmptyQuestion(sq, modelId, "simulation"));
                }
            }
            else
            {
                // 软文模式：逐个问题 Chain-of-Density 格式
                var singleContentPrompt = await LoadContentAnswerPromptAsync(request, new List<QuestionOnlyItem> { sq });
                WriteDiagFile(taskId, modelId, $"step3_content_q{i}_prompt", singleContentPrompt);
                
                var contentResult = await CallAIAsync(singleContentPrompt, modelId);
                WriteDiagFile(taskId, modelId, $"step3_content_q{i}_response", contentResult ?? "(empty)");
                
                if (!string.IsNullOrEmpty(contentResult))
                {
                    var contentAnswers = ParseContentAnswersResponse(contentResult, modelId, new List<QuestionOnlyItem> { sq });
                    if (contentAnswers.Count > 0)
                    {
                        var ca = contentAnswers[0];
                        // 合并 Step2 Perplexity 的 sources 到原模型结果
                        if (perplexitySourcesMap.TryGetValue(i, out var pSources))
                        {
                            ca.Sources = (ca.Sources ?? new List<string>()).Concat(pSources).Distinct().ToList();
                        }
                        _logger.LogInformation("[{TaskId}][{Model}] Step3 [{Index}/{Total}] 完成: ansLen={AnsLen} srcCnt={SrcCnt}",
                            taskId, modelId, i + 1, questionsToProcess.Count, ca.Answer?.Length ?? 0, ca.Sources?.Count ?? 0);
                        questions.AddRange(contentAnswers);
                    }
                    else
                    {
                        questions.Add(CreateEmptyQuestion(sq, modelId, "content"));
                    }
                }
                else
                {
                    _logger.LogWarning("[{TaskId}][{Model}] Step3 [{Index}] 软文模式回答失败", taskId, modelId, i + 1);
                    questions.Add(CreateEmptyQuestion(sq, modelId, "content"));
                }
            }
        }
        
        // Step 4: Google Trends 验证搜索热度（v2.0 新增，v5.0 优化 geo 参数）
        if (request.EnableGoogleTrends)
        {
            // v5.0: 从 Countries 获取国家代码，而非根据语言推断
            var geo = request.Countries?.FirstOrDefault() ?? (request.Language?.StartsWith("zh") == true ? "CN" : "US");
            _logger.LogInformation("[{TaskId}][{Model}] Step4: Google Trends 热度验证 (geo={Geo}, 问题数={Count})", 
                taskId, modelId, geo, questions.Count);
            
            var step4Start = DateTime.UtcNow;
            questions = await ValidateWithGoogleTrends(questions, geo, baseUrl);
            
            _logger.LogInformation("[{TaskId}][{Model}] Step4 完成: 耗时 {Duration:F1}s", 
                taskId, modelId, (DateTime.UtcNow - step4Start).TotalSeconds);
            
            // v5.3: 详细日志 - Google Trends验证结果
            _logger.LogInformation("[{TaskId}][{Model}] ========== Google Trends验证结果 (Country={Country}) ==========", 
                taskId, modelId, geo);
            for (int idx = 0; idx < questions.Count; idx++)
            {
                var q = questions[idx];
                _logger.LogInformation("[{TaskId}][{Model}] Trends[{Index}]: Heat={Heat}, Q={Question}", 
                    taskId, modelId, idx + 1, q.GoogleTrendsHeat ?? 0, 
                    q.Question.Length > 50 ? q.Question[..50] + "..." : q.Question);
            }
            
            // 记录热度分布
            var heatGroups = questions.GroupBy(q => q.GoogleTrendsHeat switch
            {
                >= 80 => "高热度(80+)",
                >= 50 => "中热度(50-79)",
                >= 20 => "低热度(20-49)",
                _ => "无热度(<20)"
            });
            foreach (var g in heatGroups)
            {
                _logger.LogInformation("[{TaskId}][{Model}] Step4 热度分布: {Group} = {Count} 个", taskId, modelId, g.Key, g.Count());
            }
            _logger.LogInformation("[{TaskId}][{Model}] ========== Google Trends验证结果结束 ==========", taskId, modelId);
        }
        else
        {
            _logger.LogInformation("[{TaskId}][{Model}] Step4: Google Trends 验证已禁用，跳过", taskId, modelId);
        }
        
        return questions;
    }

    /// <summary>
    /// 保留旧的同步接口（用于本地测试，不经过 Cloudflare）
    /// </summary>
    [HttpPost("questions-sync")]
    public async Task<IActionResult> GenerateQuestionsSync([FromBody] QuestionsRequest request)
    {
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var requestStart = DateTime.UtcNow;
        
        _logger.LogInformation("========== [{RequestId}] 问题生成请求开始 (同步模式) ==========", requestId);
        
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { success = false, message = "品牌名称不能为空" });
        }

        try
        {
            var allQuestions = new List<GeneratedQuestion>();
            var totalGenerated = 0;

            var modelTasks = request.Models.Select(async modelId =>
            {
                var modelStart = DateTime.UtcNow;
                var modelQuestions = new List<GeneratedQuestion>();
                var modelGenerated = 0;
                
                try
                {
                    var questionPrompt = await LoadQuestionGenerationPromptAsync(request, modelId);
                    var questionResult = await CallAIAsync(questionPrompt, modelId);
                    
                    if (string.IsNullOrEmpty(questionResult)) return (modelQuestions, modelGenerated);
                    
                    // v5.2: 传入 country/language 确保问题与答案的国家一致
                    var country = request.Countries?.FirstOrDefault() ?? "CN";
                    var language = request.Language ?? "zh-CN";
                    var rawQuestions = ParseQuestionsOnlyResponse(questionResult, modelId, country, language);
                    modelGenerated = rawQuestions.Count;
                    
                    if (rawQuestions.Count == 0) return (modelQuestions, modelGenerated);
                    
                    // v2.0: 从数据库获取配置
                    var questionsPerModel = await GetConfigValueAsync("questions_per_model", DefaultQuestionsPerModel);
                    var questionsToAnswer = rawQuestions.Take(questionsPerModel * 2).ToList(); // 同步接口取 2 倍数量
                    var answerPrompt = await LoadAnswerPromptAsync(request, questionsToAnswer);
                    var answerResult = await CallAIAsync(answerPrompt, modelId);
                    
                    if (!string.IsNullOrEmpty(answerResult))
                    {
                        modelQuestions.AddRange(ParseAnsweredQuestionsResponse(answerResult, modelId, questionsToAnswer));
                    }
                    else
                    {
                        modelQuestions.AddRange(questionsToAnswer.Select(q => new GeneratedQuestion
                        {
                            Model = modelId, Question = q.Question, FreqScore = q.FreqScore,
                            Pattern = q.Pattern, Intent = q.Intent, Stage = q.Stage,
                            Persona = q.Persona, SellingPoint = q.SellingPoint
                        }));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{RequestId}][{Model}] 异常: {Error}", requestId, modelId, ex.Message);
                }
                
                return (modelQuestions, modelGenerated);
            }).ToList();
            
            var results = await Task.WhenAll(modelTasks);
            foreach (var (questions, generated) in results)
            {
                allQuestions.AddRange(questions);
                totalGenerated += generated;
            }
            
            var deduped = allQuestions.GroupBy(q => new { q.Question, q.Model }).Select(g => g.First()).ToList();
            var sorted = deduped.OrderByDescending(q => q.FreqScore).ToList();
            
            return Ok(new { 
                success = true, 
                data = new { questions = sorted, total = sorted.Count, validation = new { totalGenerated, passedValidation = sorted.Count } } 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "问题生成失败: {Brand}", request.BrandName);
            return Ok(new { success = false, message = "生成过程中发生错误: " + ex.Message });
        }
    }

    /// <summary>
    /// 从模板文件加载问题生成 Prompt（单一模板支持多语言）
    /// v2.0: 优先从数据库读取，文件作为 fallback
    /// </summary>
    private async Task<string> LoadQuestionGenerationPromptAsync(QuestionsRequest request, string modelId)
    {
        var language = request.Language?.ToLower() == "en" ? "English" : "Chinese";
        var sep = request.Language?.ToLower() == "en" ? ", " : "、";
        
        string? template = null;
        
        // v5.3: 只从数据库读取 Prompt，不再使用文件或内嵌模板 fallback
        var configKey = modelId == "perplexity" ? "perplexity" : "general";
        var promptConfig = await _promptRepo.GetByKeyAsync("questions", configKey);
        if (promptConfig == null || string.IsNullOrEmpty(promptConfig.PromptTemplate))
        {
            _logger.LogError("[{Model}] 数据库中未找到 Prompt: questions/{Key}，请检查 prompt_configs 表", modelId, configKey);
            throw new InvalidOperationException($"Prompt 配置缺失: questions/{configKey}，请在数据库 prompt_configs 表中配置");
        }
        template = promptConfig.PromptTemplate;
        _logger.LogInformation("[{Model}] 从数据库加载 Prompt: questions/{Key}", modelId, configKey);
        
        // 替换变量
        var personasText = request.Personas != null && request.Personas.Count > 0
            ? string.Join(sep, request.Personas)
            : (language == "English" ? "individual users, enterprise users" : "个人用户、企业用户");

        var sellingPointsText = request.SellingPoints != null && request.SellingPoints.Count > 0
            ? string.Join(sep, request.SellingPoints)
            : (language == "English" ? "core features" : "核心功能");

        var stagesText = request.Stages != null && request.Stages.Count > 0
            ? string.Join(sep, request.Stages)
            : (language == "English" ? "awareness, consideration, decision" : "认知、考虑、决策");

        // 产品上下文
        var productContext = !string.IsNullOrEmpty(request.ProductName)
            ? $"{request.ProductName} (by {request.BrandName})"
            : request.BrandName;

        // 竞品
        var competitorsText = request.Competitors != null && request.Competitors.Count > 0
            ? string.Join(", ", request.Competitors)
            : "";

        // 目标市场
        var marketsText = request.Markets != null && request.Markets.Count > 0
            ? string.Join(", ", request.Markets)
            : "";
        var regionGuidance = !string.IsNullOrEmpty(marketsText)
            ? $"\n## Region Handling (IMPLICIT)\nTarget markets: {marketsText}\n- Generate questions relevant to these markets but DO NOT explicitly mention region names in questions"
            : "";

        // 强化语言指令
        var languageInstruction = language == "English"
            ? "English (ALL questions and answers MUST be in English)"
            : $"{language} (所有问题和答案必须使用{language}输出，不要使用英文)";

        // v5.0: 获取目标国家
        var country = request.Countries?.FirstOrDefault() ?? "CN";
        var countryName = GetDisplayMarket(country);

        return template
            .Replace("{{brand}}", request.BrandName)
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{productName}}", request.ProductName ?? request.BrandName)
            .Replace("{{productContext}}", productContext)
            .Replace("{{industry}}", request.Industry)
            .Replace("{{selling_points}}", sellingPointsText)
            .Replace("{{sellingPoints}}", sellingPointsText)
            .Replace("{{personas}}", personasText)
            .Replace("{{stages}}", stagesText)
            .Replace("{{competitors}}", competitorsText)
            .Replace("{{country}}", countryName)
            .Replace("{{regionGuidance}}", regionGuidance)
            .Replace("{{language}}", languageInstruction)
            .Replace("{{currentDate}}", DateTime.UtcNow.ToString("yyyy-MM-dd"))
            .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString());
    }

    /// <summary>
    /// v5.3: 从数据库加载回答 Prompt（单一模板支持多语言）
    /// 只从数据库读取，不再使用 fallback
    /// </summary>
    private async Task<string> LoadAnswerPromptAsync(QuestionsRequest request, List<QuestionOnlyItem> questions)
    {
        var language = GetLanguageName(request.Language);
        
        // v5.3: 只从数据库读取 Prompt，不再使用 fallback
        var promptConfig = await _promptRepo.GetByKeyAsync("answers", "general");
        if (promptConfig == null || string.IsNullOrEmpty(promptConfig.PromptTemplate))
        {
            _logger.LogError("数据库中未找到 Prompt: answers/general，请检查 prompt_configs 表");
            throw new InvalidOperationException("Prompt 配置缺失: answers/general，请在数据库 prompt_configs 表中配置");
        }
        var template = promptConfig.PromptTemplate;
        _logger.LogInformation("从数据库加载回答模板: answers/general");
        
        // 构建问题列表
        var questionList = string.Join("\n", questions.Select((q, i) => $"{i + 1}. {q.Question}"));
        
        // v5.2: 获取国家信息
        var country = questions.FirstOrDefault()?.Country ?? request.Countries?.FirstOrDefault() ?? "CN";
        var countryName = GetDisplayMarket(country);
        
        return template
            .Replace("{{brand}}", request.BrandName)
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{productName}}", request.ProductName ?? "")
            .Replace("{{sellingPoints}}", request.SellingPoints != null ? string.Join(", ", request.SellingPoints) : "")
            .Replace("{{industry}}", request.Industry)
            .Replace("{{questions}}", questionList)
            .Replace("{{questionList}}", questionList)
            .Replace("{{language}}", language)
            .Replace("{{country}}", countryName);
    }

    /// <summary>
    /// v5.3: 加载客观回答 Prompt（模拟真实 AI 回答，不带品牌偏向）
    /// 只从数据库读取，不再使用 fallback
    /// </summary>
    private async Task<string> LoadObjectiveAnswerPromptAsync(QuestionsRequest request, List<QuestionOnlyItem> questions)
    {
        var language = GetLanguageName(request.Language);
        var questionList = string.Join("\n", questions.Select((q, i) => $"{i + 1}. {q.Question}"));
        var competitors = request.Competitors != null && request.Competitors.Count > 0 
            ? string.Join(", ", request.Competitors) 
            : "（未提供）";
        
        // v5.3: 只从数据库读取 Prompt，不再使用 fallback
        var promptConfig = await _promptRepo.GetByKeyAsync("answers", "objective");
        if (promptConfig == null || string.IsNullOrEmpty(promptConfig.PromptTemplate))
        {
            _logger.LogError("数据库中未找到 Prompt: answers/objective，请检查 prompt_configs 表");
            throw new InvalidOperationException("Prompt 配置缺失: answers/objective，请在数据库 prompt_configs 表中配置");
        }
        var template = promptConfig.PromptTemplate;
        _logger.LogInformation("从数据库加载客观回答 Prompt: answers/objective");
        
        // v5.2: 获取国家信息
        var country = questions.FirstOrDefault()?.Country ?? request.Countries?.FirstOrDefault() ?? "CN";
        var countryName = GetDisplayMarket(country);
        
        // 替换变量
        return template
            .Replace("{{industry}}", request.Industry)
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{competitors}}", competitors)
            .Replace("{{language}}", language)
            .Replace("{{country}}", countryName)
            .Replace("{{questionList}}", questionList);
    }
    
    /// <summary>
    /// v5.3: 加载软文模式 Prompt（Chain-of-Density 格式，强植入品牌）
    /// 只从数据库读取，不再使用 fallback
    /// </summary>
    private async Task<string> LoadContentAnswerPromptAsync(QuestionsRequest request, List<QuestionOnlyItem> questions)
    {
        var language = GetLanguageName(request.Language);
        var questionList = string.Join("\n", questions.Select((q, i) => $"{i + 1}. {q.Question}"));
        var competitors = request.Competitors != null && request.Competitors.Count > 0 
            ? string.Join(", ", request.Competitors) 
            : "（未提供）";
        var sellingPoints = request.SellingPoints != null && request.SellingPoints.Count > 0
            ? string.Join(", ", request.SellingPoints)
            : "（未提供）";
        var productContext = !string.IsNullOrEmpty(request.ProductName)
            ? $"{request.ProductName} (by {request.BrandName})"
            : request.BrandName;
        
        // v5.3: 只从数据库读取 Prompt，不再使用 fallback
        var promptConfig = await _promptRepo.GetByKeyAsync("answers", "content");
        if (promptConfig == null || string.IsNullOrEmpty(promptConfig.PromptTemplate))
        {
            _logger.LogError("数据库中未找到 Prompt: answers/content，请检查 prompt_configs 表");
            throw new InvalidOperationException("Prompt 配置缺失: answers/content，请在数据库 prompt_configs 表中配置");
        }
        var template = promptConfig.PromptTemplate;
        _logger.LogInformation("从数据库加载软文模式 Prompt: answers/content");
        
        // v5.2: 优先从问题对象获取国家（确保问题与答案的国家一致）
        var country = questions.FirstOrDefault()?.Country ?? request.Countries?.FirstOrDefault() ?? "CN";
        var countryName = GetDisplayMarket(country);
        _logger.LogInformation("[Content] 答案生成使用国家: {Country} ({CountryName})", country, countryName);
        
        return template
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{productName}}", request.ProductName ?? "")
            .Replace("{{productContext}}", productContext)
            .Replace("{{industry}}", request.Industry)
            .Replace("{{sellingPoints}}", sellingPoints)
            .Replace("{{competitors}}", competitors)
            .Replace("{{language}}", language)
            .Replace("{{questions}}", questionList)
            .Replace("{{country}}", countryName);
    }
    
    /// <summary>
    /// v5.3: 加载 AI 模拟模式 Prompt（模拟各 AI 引擎的自然回答）
    /// 只从数据库读取，不再使用 fallback
    /// </summary>
    private async Task<string> LoadSimulationAnswerPromptAsync(QuestionsRequest request, List<QuestionOnlyItem> questions, string aiEngine)
    {
        var language = GetLanguageName(request.Language);
        var questionList = string.Join("\n", questions.Select((q, i) => $"{i + 1}. {q.Question}"));
        var competitors = request.Competitors != null && request.Competitors.Count > 0 
            ? string.Join(", ", request.Competitors) 
            : "（未提供）";
        
        // v5.3: 只从数据库读取 Prompt，不再使用 fallback
        var promptConfig = await _promptRepo.GetByKeyAsync("answers", "simulation");
        if (promptConfig == null || string.IsNullOrEmpty(promptConfig.PromptTemplate))
        {
            _logger.LogError("数据库中未找到 Prompt: answers/simulation，请检查 prompt_configs 表");
            throw new InvalidOperationException("Prompt 配置缺失: answers/simulation，请在数据库 prompt_configs 表中配置");
        }
        var template = promptConfig.PromptTemplate;
        _logger.LogInformation("从数据库加载 AI 模拟模式 Prompt: answers/simulation");
        
        // v5.2: 优先从问题对象获取国家（确保问题与答案的国家一致）
        var country = questions.FirstOrDefault()?.Country ?? request.Countries?.FirstOrDefault() ?? "CN";
        var countryName = GetDisplayMarket(country);
        _logger.LogInformation("[{Engine}] 答案生成使用国家: {Country} ({CountryName})", aiEngine, country, countryName);
        
        return template
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{productName}}", request.ProductName ?? "")
            .Replace("{{industry}}", request.Industry)
            .Replace("{{competitors}}", competitors)
            .Replace("{{language}}", language)
            .Replace("{{aiEngine}}", aiEngine)
            .Replace("{{questions}}", questionList)
            .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString())
            .Replace("{{country}}", countryName);
    }
    
    // v5.3: 已移除所有内嵌模板方法，所有 Prompt 必须从数据库 prompt_configs 表读取
    // 删除的方法: GetEmbeddedContentAnswerTemplate, GetEmbeddedSimulationAnswerTemplate, 
    //            GetEmbeddedQuestionTemplate, GetEmbeddedAnswerTemplate

    /// <summary>
    /// 解析 Step 1 只有问题的响应
    /// v5.2: 添加 country/language 参数，确保问题与答案的国家一致
    /// </summary>
    private List<QuestionOnlyItem> ParseQuestionsOnlyResponse(string response, string modelId, string country = "CN", string language = "zh-CN")
    {
        try
        {
            var cleanJson = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var result = new List<QuestionOnlyItem>();
            
            // 容错：支持多种 JSON 格式
            JsonElement questionsArray;
            if (root.TryGetProperty("questions", out questionsArray) && questionsArray.ValueKind == JsonValueKind.Array)
            {
                // 格式1: { "questions": [...] }
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                // 格式2: 直接是数组 [...]
                questionsArray = root;
            }
            else if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("questions", out questionsArray) && questionsArray.ValueKind == JsonValueKind.Array)
            {
                // 格式3: { "data": { "questions": [...] } }
            }
            else
            {
                // 无法识别的格式，记录详细日志便于诊断
                _logger.LogWarning("[{Model}] 解析问题响应：无法识别的 JSON 格式，顶层类型={Kind}，顶层属性=[{Props}]", 
                    modelId, root.ValueKind, 
                    root.ValueKind == JsonValueKind.Object ? string.Join(",", root.EnumerateObject().Select(p => p.Name).Take(5)) : "N/A");
                return result;
            }

            foreach (var q in questionsArray.EnumerateArray())
            {
                // v2.0: 解析双指标评分
                var searchIndex = q.TryGetProperty("searchIndex", out var si) && si.ValueKind == JsonValueKind.Number ? si.GetInt32() 
                    : (q.TryGetProperty("search_index", out var si2) && si2.ValueKind == JsonValueKind.Number ? si2.GetInt32() 
                    : (q.TryGetProperty("freq_score", out var fs) && fs.ValueKind == JsonValueKind.Number ? fs.GetInt32() 
                    : (q.TryGetProperty("freqScore", out var fs2) && fs2.ValueKind == JsonValueKind.Number ? fs2.GetInt32() : 0)));
                var brandFitIndex = q.TryGetProperty("brandFitIndex", out var bfi) && bfi.ValueKind == JsonValueKind.Number ? bfi.GetInt32() 
                    : (q.TryGetProperty("brand_fit_index", out var bfi2) && bfi2.ValueKind == JsonValueKind.Number ? bfi2.GetInt32() : 50); // 默认 50
                var score = q.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number ? sc.GetInt32() : (searchIndex * brandFitIndex);
                
                var questionText = q.TryGetProperty("question", out var qn) ? qn.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(questionText)) continue; // 跳过空问题
                
                result.Add(new QuestionOnlyItem
                {
                    Question = questionText,
                    SearchIndex = searchIndex,
                    BrandFitIndex = brandFitIndex,
                    Score = score,
                    Pattern = q.TryGetProperty("pattern", out var pt) ? pt.GetString() ?? "" : "",
                    Intent = q.TryGetProperty("intent", out var i) ? i.GetString() ?? "" : "",
                    Stage = q.TryGetProperty("stage", out var s) ? s.GetString() ?? "" : "",
                    Persona = q.TryGetProperty("persona", out var p) ? p.GetString() ?? "" : "",
                    SellingPoint = q.TryGetProperty("selling_point", out var sp) ? sp.GetString() ?? "" : (q.TryGetProperty("sellingPoint", out var sp2) ? sp2.GetString() ?? "" : ""),
                    // v5.2: 设置国家和语言
                    Country = country,
                    Language = language
                });
            }
            
            _logger.LogInformation("[{Model}] 解析问题响应成功: {Count} 个问题 (Country={Country}, Language={Lang})", modelId, result.Count, country, language);
            return result;
        }
        catch (JsonException jsonEx)
        {
            // JSON 解析失败，可能是非 JSON 响应（如拒绝消息）
            var preview = response.Length > 100 ? response[..100] + "..." : response;
            _logger.LogWarning("[{Model}] 解析问题响应失败（非 JSON）: {Preview}", modelId, preview);
            return new List<QuestionOnlyItem>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Model}] 解析问题响应失败", modelId);
            return new List<QuestionOnlyItem>();
        }
    }

    /// <summary>
    /// 解析 Perplexity 直接生成的问题+答案响应
    /// </summary>
    private List<GeneratedQuestion> ParseQuestionsWithAnswersResponse(string response, string modelId)
    {
        try
        {
            // 提取 Perplexity 的 citations（如果有）
            List<string>? perplexityCitations = null;
            var citationsMarker = "<!--CITATIONS:";
            var citationsIdx = response.IndexOf(citationsMarker);
            if (citationsIdx >= 0)
            {
                var endIdx = response.IndexOf("-->", citationsIdx);
                if (endIdx > citationsIdx)
                {
                    var citationsJson = response.Substring(citationsIdx + citationsMarker.Length, endIdx - citationsIdx - citationsMarker.Length);
                    perplexityCitations = System.Text.Json.JsonSerializer.Deserialize<List<string>>(citationsJson);
                    response = response.Substring(0, citationsIdx);
                    _logger.LogInformation("[{Model}] 提取到 {Count} 个 Perplexity citations", modelId, perplexityCitations?.Count ?? 0);
                }
            }
            
            var cleanJson = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var result = new List<GeneratedQuestion>();

            if (root.TryGetProperty("questions", out var questions))
            {
                foreach (var q in questions.EnumerateArray())
                {
                    var sources = ParseSources(q);
                    // 如果问题本身没有 sources，但有 Perplexity citations，则使用 citations
                    if ((sources == null || sources.Count == 0) && perplexityCitations != null && perplexityCitations.Count > 0)
                    {
                        sources = perplexityCitations;
                    }
                    
                    // v2.0: 解析双指标评分
                    var searchIndex = q.TryGetProperty("searchIndex", out var si) && si.ValueKind == JsonValueKind.Number ? si.GetInt32() 
                        : (q.TryGetProperty("search_index", out var si2) && si2.ValueKind == JsonValueKind.Number ? si2.GetInt32() 
                        : (q.TryGetProperty("freq_score", out var fs) && fs.ValueKind == JsonValueKind.Number ? fs.GetInt32() 
                        : (q.TryGetProperty("freq", out var f) && f.ValueKind == JsonValueKind.Number ? f.GetInt32() : 0)));
                    var brandFitIndex = q.TryGetProperty("brandFitIndex", out var bfi) && bfi.ValueKind == JsonValueKind.Number ? bfi.GetInt32() 
                        : (q.TryGetProperty("brand_fit_index", out var bfi2) && bfi2.ValueKind == JsonValueKind.Number ? bfi2.GetInt32() : 50);
                    var score = q.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number ? sc.GetInt32() : (searchIndex * brandFitIndex);
                    
                    result.Add(new GeneratedQuestion
                    {
                        Model = modelId,
                        Question = q.TryGetProperty("question", out var qn) ? qn.GetString() ?? "" : "",
                        SearchIndex = searchIndex,
                        BrandFitIndex = brandFitIndex,
                        Score = score,
                        Pattern = q.TryGetProperty("pattern", out var pt) ? pt.GetString() ?? "" : "",
                        Intent = q.TryGetProperty("intent", out var i) ? i.GetString() ?? "" : "",
                        Stage = q.TryGetProperty("stage", out var s) ? s.GetString() ?? "" : "",
                        Persona = q.TryGetProperty("persona", out var p) ? p.GetString() ?? "" : "",
                        SellingPoint = q.TryGetProperty("selling_point", out var sp) ? sp.GetString() ?? "" : "",
                        Answer = q.TryGetProperty("answer", out var ans) ? ans.GetString() ?? "" : "",
                        Sources = sources
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Model}] 解析问题+答案响应失败", modelId);
            return new List<GeneratedQuestion>();
        }
    }

    /// <summary>
    /// 解析带回答的问题响应
    /// </summary>
    private List<GeneratedQuestion> ParseAnsweredQuestionsResponse(string response, string modelId, List<QuestionOnlyItem> originalQuestions)
    {
        // 提取 Perplexity 的 citations（如果有），必须在 CleanJsonResponse 之前
        // 放在 try 外面，确保 catch 分支也能访问
        List<string>? perplexityCitations = null;
        try
        {
            // citations 提取逻辑
            var citationsMarker = "<!--CITATIONS:";
            var citationsIdx = response.IndexOf(citationsMarker);
            if (citationsIdx >= 0)
            {
                var endIdx = response.IndexOf("-->", citationsIdx);
                if (endIdx > citationsIdx)
                {
                    var citationsJson = response.Substring(citationsIdx + citationsMarker.Length, endIdx - citationsIdx - citationsMarker.Length);
                    perplexityCitations = System.Text.Json.JsonSerializer.Deserialize<List<string>>(citationsJson);
                    response = response.Substring(0, citationsIdx); // 移除 citations 标记
                    _logger.LogInformation("[{Model}] ParseAnsweredQuestions 提取到 {Count} 个 Perplexity citations", modelId, perplexityCitations?.Count ?? 0);
                }
            }

            var cleanJson = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var result = new List<GeneratedQuestion>();
            var answerMap = new Dictionary<int, (string answer, List<string> sources)>();
            var answerList = new List<(string answer, List<string> sources)>();

            if (root.TryGetProperty("answers", out var answers))
            {
                foreach (var a in answers.EnumerateArray())
                {
                    var rawIndex = a.TryGetProperty("index", out var idx) && idx.ValueKind == JsonValueKind.Number ? idx.GetInt32() : 
                                   (a.TryGetProperty("questionIndex", out var idx2) && idx2.ValueKind == JsonValueKind.Number ? idx2.GetInt32() : -1);
                    var answer = a.TryGetProperty("answer", out var ans) ? ans.GetString() ?? "" : "";
                    
                    // 使用 ParseSources 方法统一解析 sources，支持多种格式
                    var sources = ParseSources(a);
                    
                    // 按数组顺序保存（回退用）
                    answerList.Add((answer, sources));
                    
                    // 按 index 保存
                    if (rawIndex >= 0)
                    {
                        answerMap[rawIndex] = (answer, sources);
                    }
                }
                
                _logger.LogInformation("[{Model}] Step2 解析: answers数组={ArrayCount}, indexMap={MapCount}, 有答案={HasAnswer}", 
                    modelId, answerList.Count, answerMap.Count, answerList.Count(x => !string.IsNullOrEmpty(x.answer)));
            }
            else
            {
                _logger.LogWarning("[{Model}] Step2 响应中没有 answers 字段，原始JSON前500字符: {Json}", 
                    modelId, cleanJson.Length > 500 ? cleanJson.Substring(0, 500) : cleanJson);
            }

            // 判断 index 是 0-based 还是 1-based
            var useZeroBased = answerMap.ContainsKey(0);
            if (!useZeroBased && answerMap.Count > 0)
            {
                // 1-based: 将所有 key 减1 转为 0-based
                var adjusted = new Dictionary<int, (string answer, List<string> sources)>();
                foreach (var kv in answerMap)
                    adjusted[kv.Key - 1] = kv.Value;
                answerMap = adjusted;
            }
            
            // 检查 index 匹配率，如果太低则回退到数组顺序
            var mapMatchCount = Enumerable.Range(0, originalQuestions.Count)
                .Count(i => answerMap.ContainsKey(i) && !string.IsNullOrEmpty(answerMap[i].answer));
            var listHasAnswers = answerList.Count(x => !string.IsNullOrEmpty(x.answer));
            var useListFallback = mapMatchCount == 0 && listHasAnswers > 0;
            
            if (useListFallback)
            {
                _logger.LogWarning("[{Model}] Step2 index匹配失败 (matched={MapMatch}), 回退到数组顺序 (answers={ListCount})", 
                    modelId, mapMatchCount, answerList.Count);
            }

            for (int i = 0; i < originalQuestions.Count; i++)
            {
                var q = originalQuestions[i];
                
                (string answer, List<string> sources) entry;
                if (!useListFallback && answerMap.TryGetValue(i, out var val))
                    entry = val;
                else if (i < answerList.Count)
                    entry = answerList[i];
                else
                    entry = ("", new List<string>());
                
                // 如果问题本身没有 sources，但有 Perplexity citations，则使用 citations
                var sources = entry.sources;
                if ((sources == null || sources.Count == 0) && perplexityCitations != null && perplexityCitations.Count > 0)
                {
                    sources = perplexityCitations;
                }

                result.Add(new GeneratedQuestion
                {
                    Model = modelId,
                    Question = q.Question,
                    SearchIndex = q.SearchIndex,
                    BrandFitIndex = q.BrandFitIndex,
                    Score = q.Score > 0 ? q.Score : (q.SearchIndex * q.BrandFitIndex),
                    Pattern = q.Pattern,
                    Intent = q.Intent,
                    Stage = q.Stage,
                    Persona = q.Persona,
                    SellingPoint = q.SellingPoint,
                    Answer = entry.answer,
                    Sources = sources
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Model}] 解析回答响应失败, perplexityCitations={CitCount}, responseLen={RespLen}", 
                modelId, perplexityCitations?.Count ?? 0, response?.Length ?? 0);
            // 即使解析失败，也要尝试回填 Perplexity citations
            return originalQuestions.Select(q => new GeneratedQuestion
            {
                Model = modelId,
                Question = q.Question,
                FreqScore = q.FreqScore,
                Pattern = q.Pattern,
                Intent = q.Intent,
                Stage = q.Stage,
                Persona = q.Persona,
                SellingPoint = q.SellingPoint,
                Sources = perplexityCitations ?? new List<string>()
            }).ToList();
        }
    }

    /// <summary>
    /// v3.0: 解析软文模式答案响应
    /// </summary>
    private List<GeneratedQuestion> ParseContentAnswersResponse(string response, string modelId, List<QuestionOnlyItem> originalQuestions)
    {
        try
        {
            var cleanJson = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;
            
            var result = new List<GeneratedQuestion>();
            var answerMap = new Dictionary<int, (string answer, int brandMentionCount, string keyFeature, List<string> platforms)>();
            var answerList = new List<(string answer, int brandMentionCount, string keyFeature, List<string> platforms)>();
            
            if (root.TryGetProperty("answers", out var answers))
            {
                foreach (var a in answers.EnumerateArray())
                {
                    var rawIndex = a.TryGetProperty("questionIndex", out var idx) && idx.ValueKind == JsonValueKind.Number ? idx.GetInt32() : 
                               (a.TryGetProperty("index", out var idx2) && idx2.ValueKind == JsonValueKind.Number ? idx2.GetInt32() - 1 : -1);
                    var answer = a.TryGetProperty("answer", out var ans) ? ans.GetString() ?? "" : "";
                    var brandMentionCount = a.TryGetProperty("brandMentionCount", out var bmc) && bmc.ValueKind == JsonValueKind.Number ? bmc.GetInt32() : 0;
                    var keyFeature = a.TryGetProperty("keyFeatureHighlighted", out var kf) ? kf.GetString() ?? "" : "";
                    var platforms = new List<string>();
                    if (a.TryGetProperty("platforms", out var plat) && plat.ValueKind == JsonValueKind.Array)
                    {
                        platforms = plat.EnumerateArray().Select(p => p.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
                    }
                    
                    answerList.Add((answer, brandMentionCount, keyFeature, platforms));
                    if (rawIndex >= 0)
                    {
                        answerMap[rawIndex] = (answer, brandMentionCount, keyFeature, platforms);
                    }
                }
            }
            
            // 检查 index 匹配率，回退到数组顺序
            var mapMatchCount = Enumerable.Range(0, originalQuestions.Count)
                .Count(i => answerMap.ContainsKey(i) && !string.IsNullOrEmpty(answerMap[i].answer));
            var listHasAnswers = answerList.Count(x => !string.IsNullOrEmpty(x.answer));
            var useListFallback = mapMatchCount == 0 && listHasAnswers > 0;
            
            if (useListFallback)
            {
                _logger.LogWarning("[{Model}] 软文模式 index匹配失败, 回退到数组顺序 (answers={ListCount})", modelId, answerList.Count);
            }
            
            for (int i = 0; i < originalQuestions.Count; i++)
            {
                var q = originalQuestions[i];
                
                (string answer, int brandMentionCount, string keyFeature, List<string> platforms) entry;
                if (!useListFallback && answerMap.TryGetValue(i, out var val))
                    entry = val;
                else if (i < answerList.Count)
                    entry = answerList[i];
                else
                    entry = ("", 0, "", new List<string>());
                
                var (answer, brandMentionCount, keyFeature, platforms) = entry;
                
                result.Add(new GeneratedQuestion
                {
                    Model = modelId,
                    Question = q.Question,
                    SearchIndex = q.SearchIndex,
                    BrandFitIndex = q.BrandFitIndex,
                    Score = q.Score > 0 ? q.Score : (q.SearchIndex * q.BrandFitIndex),
                    Pattern = q.Pattern,
                    Intent = q.Intent,
                    Stage = q.Stage,
                    Persona = q.Persona,
                    SellingPoint = keyFeature.Length > 0 ? keyFeature : q.SellingPoint,
                    Answer = answer,
                    ContentAnswer = answer,
                    AnswerModeUsed = "content",
                    Sources = platforms,
                    // v4.2: 传递来源标注
                    Source = q.Source,
                    SourceDetail = q.SourceDetail,
                    SourceUrl = q.SourceUrl,
                    // v5.2: 传递国家和语言（确保问题与答案的国家一致）
                    Country = q.Country,
                    Language = q.Language
                });
            }
            
            _logger.LogInformation("[{Model}] 软文模式解析完成: {Count} 个答案", modelId, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Model}] 解析软文模式响应失败", modelId);
            return CreateEmptyQuestions(originalQuestions, modelId, "content");
        }
    }
    
    /// <summary>
    /// v3.0: 解析 AI 模拟模式答案响应（含品牌引用分析）
    /// </summary>
    private List<GeneratedQuestion> ParseSimulationAnswersResponse(string response, string modelId, List<QuestionOnlyItem> originalQuestions, string brandName, List<string>? competitors)
    {
        try
        {
            var cleanJson = CleanJsonResponse(response);
            
            // 诊断日志：打印 AI 返回的 JSON 前 500 字符
            _logger.LogInformation("[{Model}] 模拟模式原始响应 (前500字符): {Response}", 
                modelId, cleanJson.Length > 500 ? cleanJson[..500] : cleanJson);
            
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;
            
            // 诊断日志：打印 JSON 顶层属性名
            var topKeys = root.ValueKind == JsonValueKind.Object 
                ? string.Join(", ", root.EnumerateObject().Select(p => p.Name))
                : root.ValueKind.ToString();
            _logger.LogInformation("[{Model}] 模拟模式 JSON 顶层属性: {Keys}", modelId, topKeys);
            
            var result = new List<GeneratedQuestion>();
            var answerMap = new Dictionary<int, (string answer, BrandCitationAnalysis analysis, List<string> sources, int searchIndex, int brandFitIndex, CitationDifficulty? citationDifficulty)>();
            
            // 同时按数组顺序保存，作为 index 匹配失败的回退
            var answerList = new List<(string answer, BrandCitationAnalysis analysis, List<string> sources, int searchIndex, int brandFitIndex, CitationDifficulty? citationDifficulty)>();
            
            if (root.TryGetProperty("answers", out var answers))
            {
                foreach (var a in answers.EnumerateArray())
                {
                    var rawIndex = a.TryGetProperty("questionIndex", out var idx) && idx.ValueKind == JsonValueKind.Number ? idx.GetInt32() : 
                               (a.TryGetProperty("index", out var idx2) && idx2.ValueKind == JsonValueKind.Number ? idx2.GetInt32() : -1);
                    var answer = a.TryGetProperty("simulatedAnswer", out var ans) ? ans.GetString() ?? "" : 
                                (a.TryGetProperty("answer", out var ans2) ? ans2.GetString() ?? "" : "");
                    
                    // 解析品牌引用分析
                    var analysis = new BrandCitationAnalysis();
                    if (a.TryGetProperty("brandAnalysis", out var ba))
                    {
                        analysis.Mentioned = ba.TryGetProperty("mentioned", out var m) && m.ValueKind == JsonValueKind.True;
                        analysis.MentionCount = ba.TryGetProperty("mentionCount", out var mc) && mc.ValueKind == JsonValueKind.Number ? mc.GetInt32() : 0;
                        analysis.Position = ba.TryGetProperty("position", out var pos) && pos.ValueKind == JsonValueKind.Number ? pos.GetInt32() : 0;
                        analysis.MentionType = ba.TryGetProperty("mentionType", out var mt) ? mt.GetString() ?? "not_mentioned" : "not_mentioned";
                        analysis.MentionContext = ba.TryGetProperty("mentionContext", out var ctx) ? ctx.GetString() : null;
                        analysis.Reason = ba.TryGetProperty("reason", out var r) ? r.GetString() : null;
                        analysis.BrandVisibility = ba.TryGetProperty("brandVisibility", out var bv) ? bv.GetString() ?? "none" : "none";
                        analysis.ImprovementPotential = ba.TryGetProperty("improvementPotential", out var ip) ? ip.GetString() : null;
                    }
                    else
                    {
                        // 如果没有 brandAnalysis，手动分析答案中的品牌引用
                        analysis = AnalyzeBrandCitation(answer, brandName, competitors);
                    }
                    
                    // 解析竞品引用
                    if (a.TryGetProperty("competitorAnalysis", out var ca) && ca.ValueKind == JsonValueKind.Array)
                    {
                        analysis.CompetitorsMentioned = new List<CompetitorMention>();
                        foreach (var comp in ca.EnumerateArray())
                        {
                            analysis.CompetitorsMentioned.Add(new CompetitorMention
                            {
                                Name = comp.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                                Mentioned = comp.TryGetProperty("mentioned", out var cm) && cm.ValueKind == JsonValueKind.True,
                                Position = comp.TryGetProperty("position", out var cp) && cp.ValueKind == JsonValueKind.Number ? cp.GetInt32() : 0,
                                MentionType = comp.TryGetProperty("mentionType", out var cmt) ? cmt.GetString() ?? "not_mentioned" : "not_mentioned"
                            });
                        }
                    }
                    
                    // 解析来源信息（如果 AI 返回了 sources/references/citations）
                    var sources = ParseSources(a);
                    
                    // v4.4: 解析引用难度评估 (Phase 1.5)
                    CitationDifficulty? citationDifficulty = null;
                    if (a.TryGetProperty("citationDifficulty", out var cd))
                    {
                        citationDifficulty = new CitationDifficulty
                        {
                            Score = cd.TryGetProperty("score", out var cdScore) && cdScore.ValueKind == JsonValueKind.Number ? cdScore.GetInt32() : 50,
                            Level = cd.TryGetProperty("level", out var cdLevel) ? cdLevel.GetString() ?? "medium" : "medium",
                            Reasoning = cd.TryGetProperty("reasoning", out var cdReason) ? cdReason.GetString() : null,
                            ActionableInsights = cd.TryGetProperty("actionableInsights", out var cdInsights) && cdInsights.ValueKind == JsonValueKind.Array
                                ? cdInsights.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
                                : null
                        };
                        
                        // 解析影响因素
                        if (cd.TryGetProperty("factors", out var cdFactors))
                        {
                            citationDifficulty.Factors = new CitationDifficultyFactors
                            {
                                CompetitorDominance = cdFactors.TryGetProperty("competitorDominance", out var f1) && f1.ValueKind == JsonValueKind.Number ? f1.GetInt32() : 50,
                                TopicRelevance = cdFactors.TryGetProperty("topicRelevance", out var f2) && f2.ValueKind == JsonValueKind.Number ? f2.GetInt32() : 50,
                                ContentGap = cdFactors.TryGetProperty("contentGap", out var f3) && f3.ValueKind == JsonValueKind.Number ? f3.GetInt32() : 50,
                                AuthorityRequired = cdFactors.TryGetProperty("authorityRequired", out var f4) && f4.ValueKind == JsonValueKind.Number ? f4.GetInt32() : 50
                            };
                        }
                    }
                    
                    // 将 citationDifficulty 临时存储到 analysis 的扩展属性（后续会单独赋值）
                    analysis.ImprovementPotential = analysis.ImprovementPotential ?? citationDifficulty?.Reasoning;
                    
                    // 解析 AI 独立评分（-1 表示 AI 未返回，后续使用原始值）
                    var aiSearchIndex = a.TryGetProperty("searchIndex", out var aiSi) && aiSi.ValueKind == JsonValueKind.Number ? aiSi.GetInt32() :
                                       (a.TryGetProperty("search_index", out var aiSi2) && aiSi2.ValueKind == JsonValueKind.Number ? aiSi2.GetInt32() : -1);
                    var aiBrandFitIndex = a.TryGetProperty("brandFitIndex", out var aiBfi) && aiBfi.ValueKind == JsonValueKind.Number ? aiBfi.GetInt32() :
                                         (a.TryGetProperty("brand_fit_index", out var aiBfi2) && aiBfi2.ValueKind == JsonValueKind.Number ? aiBfi2.GetInt32() : -1);
                    
                    // 保存到有序列表（按数组顺序）
                    answerList.Add((answer, analysis, sources, aiSearchIndex, aiBrandFitIndex, citationDifficulty));
                    
                    // 同时按 index 保存（兼容 0-based 和 1-based）
                    if (rawIndex >= 0)
                    {
                        answerMap[rawIndex] = (answer, analysis, sources, aiSearchIndex, aiBrandFitIndex, citationDifficulty);
                    }
                }
            }
            
            // 判断 AI 返回的 questionIndex 是 0-based 还是 1-based
            // 策略：如果 answerMap 包含 key=0 则认为 0-based，否则尝试 1-based（所有 key 减1）
            var useZeroBased = answerMap.ContainsKey(0);
            if (!useZeroBased && answerMap.Count > 0)
            {
                // 1-based: 将所有 key 减1 转为 0-based
                var adjusted = new Dictionary<int, (string answer, BrandCitationAnalysis analysis, List<string> sources, int searchIndex, int brandFitIndex, CitationDifficulty? citationDifficulty)>();
                foreach (var kv in answerMap)
                {
                    adjusted[kv.Key - 1] = kv.Value;
                }
                answerMap = adjusted;
            }
            
            // 检查 answerMap 匹配率，如果太低则回退到按数组顺序匹配
            var mapMatchCount = Enumerable.Range(0, originalQuestions.Count)
                .Count(i => answerMap.ContainsKey(i) && !string.IsNullOrEmpty(answerMap[i].answer));
            var listMatchCount = Math.Min(answerList.Count, originalQuestions.Count);
            var listHasAnswers = answerList.Count(x => !string.IsNullOrEmpty(x.answer));
            
            var useListFallback = mapMatchCount == 0 && listHasAnswers > 0;
            if (useListFallback)
            {
                _logger.LogWarning("[{Model}] questionIndex 匹配失败 (matched={MapMatch}), 回退到数组顺序匹配 (answers={ListCount})", 
                    modelId, mapMatchCount, answerList.Count);
            }
            
            for (int i = 0; i < originalQuestions.Count; i++)
            {
                var q = originalQuestions[i];
                
                // 优先用 index 匹配，回退到数组顺序
                (string answer, BrandCitationAnalysis analysis, List<string> sources, int searchIndex, int brandFitIndex, CitationDifficulty? citationDifficulty) entry;
                if (!useListFallback && answerMap.TryGetValue(i, out var val))
                {
                    entry = val;
                }
                else if (i < answerList.Count)
                {
                    entry = answerList[i];
                }
                else
                {
                    entry = ("", new BrandCitationAnalysis(), new List<string>(), -1, -1, null);
                }
                
                // AI 返回的独立评分优先，否则使用 Step1 的原始评分
                var finalSearchIndex = entry.searchIndex >= 0 ? entry.searchIndex : q.SearchIndex;
                var finalBrandFitIndex = entry.brandFitIndex >= 0 ? entry.brandFitIndex : q.BrandFitIndex;
                
                result.Add(new GeneratedQuestion
                {
                    Model = modelId,
                    Question = q.Question,
                    SearchIndex = finalSearchIndex,
                    BrandFitIndex = finalBrandFitIndex,
                    Score = finalSearchIndex * finalBrandFitIndex,
                    Pattern = q.Pattern,
                    Intent = q.Intent,
                    Stage = q.Stage,
                    Persona = q.Persona,
                    SellingPoint = q.SellingPoint,
                    Answer = entry.answer,
                    SimulationAnswer = entry.answer,
                    AnswerModeUsed = "simulation",
                    Sources = entry.sources,
                    BrandAnalysis = entry.analysis,
                    // v4.2: 传递来源标注
                    Source = q.Source,
                    SourceDetail = q.SourceDetail,
                    SourceUrl = q.SourceUrl,
                    // v4.4: 引用难度评估 (Phase 1.5)
                    CitationDifficulty = entry.citationDifficulty,
                    // v5.2: 传递国家和语言（确保问题与答案的国家一致）
                    Country = q.Country,
                    Language = q.Language
                });
            }
            
            _logger.LogInformation("[{Model}] AI 模拟模式解析完成: {Count} 个答案", modelId, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{Model}] 解析 AI 模拟模式响应失败", modelId);
            return CreateEmptyQuestions(originalQuestions, modelId, "simulation");
        }
    }
    
    /// <summary>
    /// v3.0: 手动分析答案中的品牌引用情况
    /// </summary>
    private BrandCitationAnalysis AnalyzeBrandCitation(string answer, string brandName, List<string>? competitors)
    {
        var analysis = new BrandCitationAnalysis();
        
        if (string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(brandName))
            return analysis;
        
        // 检查品牌是否被提及
        var brandMentions = System.Text.RegularExpressions.Regex.Matches(
            answer, 
            System.Text.RegularExpressions.Regex.Escape(brandName), 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        analysis.Mentioned = brandMentions.Count > 0;
        analysis.MentionCount = brandMentions.Count;
        
        if (analysis.Mentioned)
        {
            // 找到第一次提及的位置
            var firstMention = brandMentions[0];
            var textBefore = answer.Substring(0, firstMention.Index);
            var otherBrandsBeforeCount = 0;
            
            // 计算在品牌之前提及了多少其他品牌
            if (competitors != null)
            {
                foreach (var comp in competitors)
                {
                    if (!string.IsNullOrEmpty(comp) && textBefore.Contains(comp, StringComparison.OrdinalIgnoreCase))
                    {
                        otherBrandsBeforeCount++;
                    }
                }
            }
            
            analysis.Position = otherBrandsBeforeCount + 1;
            
            // 提取提及的上下文（前后50个字符）
            var start = Math.Max(0, firstMention.Index - 50);
            var length = Math.Min(answer.Length - start, firstMention.Length + 100);
            analysis.MentionContext = answer.Substring(start, length);
            
            // 判断提及类型
            var lowerAnswer = answer.ToLower();
            if (lowerAnswer.Contains("recommend") || lowerAnswer.Contains("推荐") || lowerAnswer.Contains("建议"))
                analysis.MentionType = "recommended";
            else if (lowerAnswer.Contains("compare") || lowerAnswer.Contains("对比") || lowerAnswer.Contains("vs"))
                analysis.MentionType = "compared";
            else if (lowerAnswer.Contains("example") || lowerAnswer.Contains("例如") || lowerAnswer.Contains("比如"))
                analysis.MentionType = "example";
            else
                analysis.MentionType = "listed";
            
            // 判断可见度
            if (analysis.Position == 1 && analysis.MentionCount >= 2)
                analysis.BrandVisibility = "high";
            else if (analysis.Position <= 3)
                analysis.BrandVisibility = "medium";
            else
                analysis.BrandVisibility = "low";
        }
        else
        {
            analysis.BrandVisibility = "none";
            analysis.Reason = "Brand not mentioned in the AI response";
            analysis.ImprovementPotential = "Consider creating more authoritative content about the brand";
        }
        
        // 分析竞品提及
        if (competitors != null && competitors.Count > 0)
        {
            analysis.CompetitorsMentioned = new List<CompetitorMention>();
            var position = 1;
            foreach (var comp in competitors)
            {
                if (string.IsNullOrEmpty(comp)) continue;
                
                var compMentions = System.Text.RegularExpressions.Regex.Matches(
                    answer, 
                    System.Text.RegularExpressions.Regex.Escape(comp), 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (compMentions.Count > 0)
                {
                    analysis.CompetitorsMentioned.Add(new CompetitorMention
                    {
                        Name = comp,
                        Mentioned = true,
                        Position = position++,
                        MentionType = "listed"
                    });
                }
            }
        }
        
        return analysis;
    }
    
    /// <summary>
    /// v3.0: 创建空问题列表（当解析失败时使用）
    /// </summary>
    private List<GeneratedQuestion> CreateEmptyQuestions(List<QuestionOnlyItem> originalQuestions, string modelId, string answerMode)
    {
        return originalQuestions.Select(q => CreateEmptyQuestion(q, modelId, answerMode)).ToList();
    }
    
    /// <summary>
    /// v3.1: 创建单个空问题（逐个调用模式下，单个问题回答失败时使用）
    /// </summary>
    private GeneratedQuestion CreateEmptyQuestion(QuestionOnlyItem q, string modelId, string answerMode)
    {
        return new GeneratedQuestion
        {
            Model = modelId,
            Question = q.Question,
            SearchIndex = q.SearchIndex,
            BrandFitIndex = q.BrandFitIndex,
            Score = q.Score,
            Pattern = q.Pattern,
            Intent = q.Intent,
            Stage = q.Stage,
            Persona = q.Persona,
            SellingPoint = q.SellingPoint,
            AnswerModeUsed = answerMode,
            // v4.2: 传递来源标注
            Source = q.Source,
            SourceDetail = q.SourceDetail,
            SourceUrl = q.SourceUrl,
            // v5.2: 传递国家和语言
            Country = q.Country,
            Language = q.Language
        };
    }

    /// <summary>
    /// v2.0: 使用 Google Trends 验证问题的搜索热度
    /// </summary>
    private async Task<List<GeneratedQuestion>> ValidateWithGoogleTrends(List<GeneratedQuestion> questions, string? geo = "", string baseUrl = "http://localhost:8080")
    {
        // 只验证前 5 个高分问题，避免大量请求导致限流
        const int maxValidations = 5;
        const int perRequestTimeoutSeconds = 20;
        const int maxRetries = 1;
        
        var toValidate = questions.OrderByDescending(q => q.FreqScore).Take(maxValidations).ToList();
        _logger.LogInformation("[GoogleTrends] ========== 开始 Google Trends 验证 ==========");
        _logger.LogInformation("[GoogleTrends] 参数: geo={Geo}, 验证数={Count}/{Total}, 超时={Timeout}s", 
            geo, toValidate.Count, questions.Count, perRequestTimeoutSeconds);
        
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(perRequestTimeoutSeconds);
        var rateLimited = false;
        
        // 并行验证（最多 2 并发，避免触发自身限流）
        var semaphore = new SemaphoreSlim(2);
        var tasks = toValidate.Select(async q =>
        {
            if (rateLimited) return;
            
            await semaphore.WaitAsync();
            try
            {
                var keywords = ExtractKeywords(q.Question);
                if (string.IsNullOrEmpty(keywords)) return;
                
                var url = $"{baseUrl}/api/trends/heat?keyword={Uri.EscapeDataString(keywords)}&geo={geo}";
                _logger.LogDebug("[GoogleTrends] 请求: keyword={Keyword}, geo={Geo}", keywords, geo);
                
                for (int attempt = 0; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Get, url);
                        // 内部调用标识，让 TrendsProxyController 跳过限流检查
                        request.Headers.Add("X-Internal-Call", "distillation");
                        
                        using var response = await client.SendAsync(request);
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("[GoogleTrends] 被限流 (429)，跳过剩余验证");
                            rateLimited = true;
                            return;
                        }
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            
                            if (root.TryGetProperty("success", out var success) && success.GetBoolean())
                            {
                                var avgHeat = root.TryGetProperty("avgHeat", out var ah) ? ah.GetInt32() : 0;
                                var recentHeat = root.TryGetProperty("recentHeat", out var rh) ? rh.GetInt32() : 0;
                                q.GoogleTrendsHeat = Math.Max(avgHeat, recentHeat);
                                
                                // v4.1: 填充趋势方向和强度
                                if (root.TryGetProperty("trendDirection", out var td))
                                {
                                    q.TrendDirection = td.GetString();
                                }
                                if (root.TryGetProperty("trendScore", out var ts))
                                {
                                    q.TrendScore = ts.GetInt32();
                                }
                                
                                _logger.LogInformation("[GoogleTrends] 验证成功: keyword={Keyword}, heat={Heat}, trend={Trend}", 
                                    keywords, q.GoogleTrendsHeat, q.TrendDirection ?? "unknown");
                            }
                            else
                            {
                                _logger.LogWarning("[GoogleTrends] API返回失败: keyword={Keyword}", keywords);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("[GoogleTrends] HTTP错误: {Status}, keyword={Keyword}", response.StatusCode, keywords);
                        }
                        break; // 成功则跳出重试循环
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        _logger.LogWarning("[GoogleTrends] 第{Attempt}次请求失败，重试: {Message}", attempt + 1, ex.Message);
                        await Task.Delay(1000); // 等 1 秒后重试
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("[GoogleTrends] 请求超时: {Question}", q.Question);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[GoogleTrends] 验证失败: {Message}", ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("[GoogleTrends] 验证完成，{Count} 个问题有热度数据{RateLimited}", 
            questions.Count(q => q.GoogleTrendsHeat.HasValue),
            rateLimited ? "（部分跳过：限流）" : "");
        
        return questions;
    }

    /// <summary>
    /// 从问题中提取关键词用于 Google Trends 查询
    /// </summary>
    private static string ExtractKeywords(string question)
    {
        if (string.IsNullOrEmpty(question)) return "";
        
        // 移除常见的疑问词和标点
        var cleaned = question
            .Replace("?", "").Replace("？", "")
            .Replace("什么", "").Replace("哪个", "").Replace("哪些", "")
            .Replace("怎么", "").Replace("如何", "").Replace("为什么", "")
            .Replace("是否", "").Replace("有没有", "").Replace("能不能", "")
            .Replace("what", "").Replace("which", "").Replace("how", "")
            .Replace("why", "").Replace("when", "").Replace("where", "")
            .Replace("best", "").Replace("top", "").Replace("good", "")
            .Trim();
        
        // 取前 N 个字符作为关键词（从数据库获取配置，但这是同步方法，使用默认值）
        // 注意：ExtractKeywords 是静态方法，无法访问数据库，保持默认值
        if (cleaned.Length > DefaultKeywordMaxLength)
            cleaned = cleaned.Substring(0, DefaultKeywordMaxLength);
        
        return cleaned.Trim();
    }

    [HttpPost("competitors")]
    public async Task<IActionResult> SuggestCompetitors([FromBody] CompetitorsRequest r)
    {
        // v2.0: 从数据库获取配置
        var maxCompetitors = await GetConfigValueAsync("max_competitors", DefaultMaxCompetitors);
        
        var desc = r.Description ?? "";
        if (string.IsNullOrWhiteSpace(desc) && !string.IsNullOrWhiteSpace(r.Url))
            desc = await GetDescriptionFromUrlAsync(r.Url);
        if (string.IsNullOrWhiteSpace(desc))
            desc = await GetProductDescriptionAsync(r.BrandName, r.Industry);
        var p = $@"找出与该产品提供相同服务的{maxCompetitors}个直接竞品。

产品:{r.BrandName}
描述:{desc}

要求:
1. 只返回真实存在的公司
2. 竞品必须提供相同或高度相似的服务
3. 如果没有官网,url留空
4. 如果找不到竞品,返回空数组

只输出JSON,不要任何解释:
{{""competitors"":[{{""name"":""品牌名"",""url"":""官网"",""description"":""简介""}}]}}";
        var res = await CallAIAsync(p, "gemini", 0.3);
        if (res == null) return Ok(new { success = false });
        var j = CleanJsonResponse(res);
        try
        {
            using var d = JsonDocument.Parse(j);
            var candidates = d.RootElement.GetProperty("competitors").EnumerateArray()
            .Select(x => new { 
                name = x.GetProperty("name").GetString() ?? "", 
                url = x.GetProperty("url").GetString() ?? "",
                description = x.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : ""
            }).ToList();
        
            var verified = await VerifyCompetitorUrlsAsync(candidates);
            return Ok(new { success = true, data = verified });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "竞品解析失败: {Response}", j);
            return Ok(new { success = false, message = "AI返回格式错误" });
        }
    }

    private Task<List<object>> VerifyCompetitorUrlsAsync(dynamic c)
    {
        var r = new List<object>();
        foreach (var x in c) r.Add(new { name = (string)x.name, url = (string)x.url, description = (string)x.description, verified = true });
        return Task.FromResult(r);
    }

    private async Task<string> GetProductDescriptionAsync(string brandName, string industry)
    {
        var p = $@"请描述'{brandName}'这个产品/公司的核心业务(50字以内)。
如果你不确定这个产品是什么,请明确回复'未知产品'。
只输出描述文本,不要任何格式。";
        var res = await CallAIAsync(p, "gemini", 0.3);
        if (res != null && res.Contains("未知")) return "";
        return res ?? "";
    }

    private async Task<string> GetDescriptionFromUrlAsync(string url)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            var html = await http.GetStringAsync(url);
            var desc = System.Text.RegularExpressions.Regex.Match(html, 
                @"<meta\s+name=""description""\s+content=""([^""]+)""",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return desc.Success ? desc.Groups[1].Value : "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// 构建国家-语言映射
    /// </summary>
    private List<(string country, string language)> BuildCountryLanguageMap(List<string> countries, List<string> languages)
    {
        var result = new List<(string country, string language)>();
        
        // 如果国家和语言数量相同，假设是一一对应的
        if (countries.Count == languages.Count)
        {
            for (int i = 0; i < countries.Count; i++)
            {
                result.Add((countries[i], languages[i]));
            }
        }
        else
        {
            // 否则，为每个国家推断语言
            foreach (var country in countries)
            {
                var lang = InferLanguageFromCountry(country);
                // 如果推断的语言在传入的语言列表中，使用它；否则使用推断的
                if (languages.Contains(lang))
                {
                    result.Add((country, lang));
                }
                else
                {
                    result.Add((country, lang));
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// 根据国家代码推断语言代码
    /// </summary>
    private string InferLanguageFromCountry(string countryCode)
    {
        return countryCode.ToUpper() switch
        {
            "CN" => "zh-CN",
            "TW" => "zh-TW",
            "HK" => "zh-Hant",
            "MO" => "zh-Hant",
            "US" => "en",
            "GB" => "en",
            "AU" => "en",
            "CA" => "en",
            "NZ" => "en",
            "IN" => "en",
            "SG" => "en",
            "JP" => "ja",
            "KR" => "ko",
            "DE" => "de",
            "FR" => "fr",
            "ES" => "es",
            "IT" => "it",
            "PT" => "pt",
            "BR" => "pt",
            "RU" => "ru",
            "AR" => "es",
            "MX" => "es",
            "NL" => "nl",
            "PL" => "pl",
            "TR" => "tr",
            "TH" => "th",
            "VN" => "vi",
            "ID" => "id",
            "MY" => "ms",
            "PH" => "en",
            "SA" => "ar",
            "AE" => "ar",
            "IL" => "he",
            _ => "en"
        };
    }

    /// <summary>
    /// 获取语言的显示名称（用于 Prompt）
    /// </summary>
    private string GetDisplayLanguage(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "zh-cn" => "简体中文",
            "zh-tw" => "繁體中文",
            "zh-hant" => "繁體中文",
            "en" => "English",
            "ja" => "日本語",
            "ko" => "한국어",
            "de" => "Deutsch",
            "fr" => "Français",
            "es" => "Español",
            "it" => "Italiano",
            "pt" => "Português",
            "ru" => "Русский",
            "ar" => "العربية",
            "th" => "ภาษาไทย",
            "vi" => "Tiếng Việt",
            "id" => "Bahasa Indonesia",
            "ms" => "Bahasa Melayu",
            "nl" => "Nederlands",
            "pl" => "Polski",
            "tr" => "Türkçe",
            "he" => "עברית",
            _ => "English"
        };
    }

    /// <summary>
    /// 获取国家的显示名称（用于 Prompt）
    /// </summary>
    private string GetDisplayMarket(string countryCode)
    {
        return countryCode.ToUpper() switch
        {
            "CN" => "中国",
            "TW" => "台湾",
            "HK" => "香港",
            "MO" => "澳门",
            "US" => "United States",
            "GB" => "United Kingdom",
            "AU" => "Australia",
            "CA" => "Canada",
            "NZ" => "New Zealand",
            "IN" => "India",
            "SG" => "Singapore",
            "JP" => "日本",
            "KR" => "한국",
            "DE" => "Deutschland",
            "FR" => "France",
            "ES" => "España",
            "IT" => "Italia",
            "PT" => "Portugal",
            "BR" => "Brasil",
            "RU" => "Россия",
            "AR" => "Argentina",
            "MX" => "México",
            "NL" => "Nederland",
            "PL" => "Polska",
            "TR" => "Türkiye",
            "TH" => "ประเทศไทย",
            "VN" => "Việt Nam",
            "ID" => "Indonesia",
            "MY" => "Malaysia",
            "PH" => "Philippines",
            "SA" => "السعودية",
            "AE" => "الإمارات",
            "IL" => "ישראל",
            _ => countryCode
        };
    }

    private string BuildSellingPointsPrompt(SellingPointsRequest request, string language, string market)
    {
        // 单语言模式：每次只为一种语言生成
        var isEnglish = language == "English";
        
        var outputLanguageHint = isEnglish
            ? $"All selling points and usage MUST be in English. Target market: {market}. Consider what users in {market} would search for."
            : $"卖点和用途均使用{language}表述。目标市场：{market}。请考虑{market}用户的搜索习惯和关注点。";

        // 产品上下文（品牌+产品名）
        var productContext = !string.IsNullOrEmpty(request.ProductName)
            ? $"{request.ProductName}（{request.BrandName} 旗下产品）"
            : request.BrandName;

        // 构建参考信息（品牌已知信息 或 竞品用户关注点）
        var referenceInfo = BuildReferenceInfo(request);

        // 从数据库加载 Prompt 模板
        var template = LoadPromptFromDb("distillation", "selling-points", GetDefaultSellingPointsPrompt());
        
        // 根据语言生成输出格式示例
        var outputFormatExample = isEnglish
            ? @"{
  ""sellingPoints"": [
    { ""point"": ""AI Visibility Monitoring"", ""weight"": 9, ""usage"": ""Best AI search optimization tools"" }
  ]
}"
            : @"{
  ""sellingPoints"": [
    { ""point"": ""卖点关键词"", ""weight"": 9, ""usage"": ""用户搜索场景"" }
  ]
}";

        return template
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{productName}}", request.ProductName ?? "")
            .Replace("{{productContext}}", productContext)
            .Replace("{{industry}}", request.Industry)
            .Replace("{{description}}", request.Description ?? "")
            .Replace("{{markets}}", market)
            .Replace("{{languages}}", language)
            .Replace("{{outputLanguageHint}}", outputLanguageHint)
            .Replace("{{outputFormatExample}}", outputFormatExample)
            .Replace("{{referenceInfo}}", referenceInfo);
    }

    /// <summary>
    /// 构建参考信息（品牌已知信息 或 竞品用户关注点）
    /// </summary>
    private string BuildReferenceInfo(SellingPointsRequest request)
    {
        var sb = new System.Text.StringBuilder();

        // 如果有品牌检查结果且品牌已知
        if (request.BrandCheckResult != null && request.BrandCheckResult.IsKnown && request.BrandCheckResult.Confidence >= 50)
        {
            sb.AppendLine("\n【品牌已知信息】");
            if (!string.IsNullOrEmpty(request.BrandCheckResult.BrandDescription))
                sb.AppendLine($"- 品牌简介：{request.BrandCheckResult.BrandDescription}");
            if (request.BrandCheckResult.UserFocusPoints.Count > 0)
                sb.AppendLine($"- 用户关注点：{string.Join("、", request.BrandCheckResult.UserFocusPoints)}");
            if (!string.IsNullOrEmpty(request.BrandCheckResult.MarketPosition))
                sb.AppendLine($"- 市场定位：{request.BrandCheckResult.MarketPosition}");
        }
        // 否则使用竞品信息
        else if (request.CompetitorFocusPoints != null && request.CompetitorFocusPoints.Count > 0)
        {
            sb.AppendLine("\n【竞品用户关注点参考】");
            sb.AppendLine($"以下是同行业竞品的用户关注点，请参考这些关注点来提炼本产品的卖点：");
            sb.AppendLine($"- {string.Join("、", request.CompetitorFocusPoints)}");
        }
        else if (request.Competitors != null && request.Competitors.Count > 0)
        {
            sb.AppendLine("\n【竞品参考】");
            sb.AppendLine($"同行业竞品：{string.Join("、", request.Competitors)}");
            sb.AppendLine("请参考这些竞品的用户关注点来提炼本产品的卖点。");
        }

        return sb.ToString();
    }

    private string GetDefaultSellingPointsPrompt()
    {
        return @"你是一位跨行业的产品营销专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家，擅长从用户视角提炼产品卖点，帮助品牌在 AI 搜索中获得更高的可见度和在搜索引擎中的排名。

【核心任务】
根据产品信息，结合你对该行业用户搜索行为的了解，提炼出用户最关心的产品卖点关键词。
这些关键词将用于：
- 监控品牌/产品在 AI 搜索中的可见度
- 生成用户向 AI 提问的问题

【产品信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 产品描述：{{description}}
- 目标市场：{{markets}}
- 监控语言：{{languages}}
{{referenceInfo}}

【提炼原则】
1. **用户视角优先**：思考用户在搜索 {{productContext}} 时，最关心哪些特点？
   - 不同行业用户关注点不同（例如：手机用户关心性能/拍照，SaaS用户关心效率/成本）
   - 结合你对该行业的了解，提炼用户真正在意的卖点

2. **高频搜索词优先**：优先提取用户搜索频率最高的关键词
   - 这些词是用户在 AI 搜索中最常使用的表达方式
   - 避免专业术语，使用用户日常用语

3. **产品特点匹配**：从产品描述中找出与用户关注点匹配的特点
   - 产品有什么功能能满足用户需求？
   - 产品有什么优势能解决用户痛点？

4. **搜索关键词化**：将卖点转化为用户可能搜索的关键词
   - 4-8 个字/词，简洁有力
   - 是用户会在 AI 搜索中使用的词汇

5. {{outputLanguageHint}}

【提炼维度】（每个维度 3-4 个卖点，共 18-20 个）
1. **用户痛点**：该行业用户最常遇到的问题是什么？产品如何解决？
2. **核心功能**：产品最重要的功能是什么？用户会怎么描述它？
3. **差异化优势**：与同类产品相比，有什么独特之处？
4. **使用场景**：用户在什么场景下会需要这个产品？
5. **价值结果**：使用产品后能获得什么结果？

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{{outputFormatExample}}

【禁止】
- 禁止空泛词：核心功能、用户体验、品质保障、领先平台、性价比
- 禁止品牌方视角的营销术语，要用用户会搜索的词
- 禁止数量不足：必须生成 18-20 个卖点";
    }

    private string BuildAudiencePrompt(AudienceRequest request, string language = "简体中文")
    {
        var sellingPointsText = request.SellingPoints != null && request.SellingPoints.Count > 0
            ? string.Join("、", request.SellingPoints)
            : "未提供";

        // 产品上下文（品牌+产品名）
        var productContext = !string.IsNullOrEmpty(request.ProductName)
            ? $"{request.ProductName}（{request.BrandName} 旗下产品）"
            : request.BrandName;

        // 根据语言设置输出提示
        var isEnglish = language == "English";
        var audienceLanguageHint = isEnglish
            ? "All output (userTypes, roles, coreNeeds) MUST be in English"
            : $"所有输出（userTypes, roles, coreNeeds）使用{language}表述";

        // 从数据库加载 Prompt 模板
        var template = LoadPromptFromDb("distillation", "audience", GetDefaultAudiencePrompt());
        
        return template
            .Replace("{{brandName}}", request.BrandName ?? "")
            .Replace("{{productName}}", request.ProductName ?? "")
            .Replace("{{productContext}}", productContext)
            .Replace("{{industry}}", request.Industry ?? "")
            .Replace("{{description}}", request.Description ?? "")
            .Replace("{{sellingPoints}}", sellingPointsText)
            .Replace("{{audienceLanguageHint}}", audienceLanguageHint)
            .Replace("{{language}}", language);
    }

    private string GetDefaultAudiencePrompt()
    {
        return @"你是一位跨行业的用户研究专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家，擅长从用户搜索行为推断目标受众，帮助品牌精准定位在 AI 搜索和搜索引擎中的目标用户。

【核心任务】
根据产品信息和核心卖点，推断最可能搜索该产品的目标受众群体。

【产品信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 产品描述：{{description}}
- 核心卖点：{{sellingPoints}}

【推断原则】
1. **搜索行为导向**：思考什么样的用户会搜索 {{productContext}} 相关的问题？
2. **决策链路分析**：谁是信息搜索者？谁是最终决策者？
3. **诉求具体化**：用户搜索时的真实意图是什么？

【推断要求】
1. **用户类型**：从以下选择 1-2 个
   - b2b：企业客户
   - b2c：个人消费者
   - dev：开发者/技术人员

2. **决策角色**：从以下选择 1-3 个
   - tech：技术决策者（CTO、技术负责人）
   - biz：业务决策者（CMO、市场负责人）
   - procurement：采购/财务
   - enduser：终端使用者

3. **核心诉求**：基于产品卖点，推断 3-5 个用户的核心诉求
   - 必须与产品功能直接相关
   - 使用用户搜索时会用的表达方式（例如：""如何提升AI搜索排名""而非""提升排名""）
   - 禁止空泛词：产品价值、服务质量、性价比、用户体验
   - {{audienceLanguageHint}}

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""userTypes"": [""b2b""],
  ""roles"": [""biz"", ""tech""],
  ""coreNeeds"": [""如何监控品牌在AI中的曝光"", ""怎么提升AI搜索排名""]
}";
    }

    private string BuildArticlePrompt(GenerateArticleRequest request)
    {
        var sourcesText = request.Sources != null && request.Sources.Count > 0
            ? string.Join("\n", request.Sources.Select((s, i) => $"  {i + 1}. {s}"))
            : "无特定引用源";

        var productContext = !string.IsNullOrEmpty(request.ProductName)
            ? $"{request.ProductName}（{request.BrandName}）"
            : request.BrandName;

        return $@"你是一位专业的内容营销专家，擅长撰写 SEO 友好、可直接发布的软文。

【任务】
针对以下用户问题，撰写一篇高质量的软文回答，自然植入品牌信息。

【品牌信息】
- 品牌：{request.BrandName}
- 产品：{request.ProductName}
- 行业：{request.Industry}

【用户问题】
{request.Question}

【引用源】
{sourcesText}

【写作要求】
1. 使用 Chain-of-Density 格式：先给出通用回答，逐步加密品牌相关信息
2. 自然植入 {productContext}，不要生硬广告
3. 如果有引用源，根据引用源的类型和风格调整写作口吻：
   - 知乎/论坛类：用问答式、经验分享口吻
   - 博客/媒体类：用专业评测、深度分析口吻
   - 官方文档类：用教程、操作指南口吻
4. 字数 300-500 字
5. 包含 1-2 个自然的品牌提及
6. 结尾可以有软性引导，但不要硬推销

【输出】
直接输出软文内容，不要包含标题或格式标记。";
    }

    private string BuildBrandCheckPrompt(BrandCheckRequest request)
    {
        var languagesText = request.Languages != null && request.Languages.Count > 0
            ? string.Join("、", request.Languages)
            : "中文";

        var template = LoadPromptFromDb("distillation", "brand-check", GetDefaultBrandCheckPrompt());
        
        return template
            .Replace("{{brandName}}", request.BrandName ?? "")
            .Replace("{{productName}}", request.ProductName ?? "未提供")
            .Replace("{{industry}}", request.Industry ?? "")
            .Replace("{{languages}}", languagesText);
    }

    private string GetDefaultBrandCheckPrompt()
    {
        return @"你是一位品牌研究专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家。请判断以下品牌/产品是否在你的知识库中存在。

【品牌信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 语言：{{languages}}

【判断标准】
1. **已知品牌**：你能够描述该品牌的主要产品、市场定位、用户群体、竞争优势等信息
2. **未知品牌**：你对该品牌没有足够的了解，无法提供详细信息

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""isKnown"": true,
  ""confidence"": 85,
  ""knownInfo"": ""该品牌的简要描述（如果已知）"",
  ""suggestedCompetitors"": [""竞品1"", ""竞品2"", ""竞品3""]
}";
    }

    private BrandCheckResult ParseBrandCheckResponse(string response)
    {
        try
        {
            var json = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new BrandCheckResult
            {
                IsKnown = root.TryGetProperty("isKnown", out var isKnown) && isKnown.GetBoolean(),
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetInt32() : 0
            };

            if (root.TryGetProperty("brandInfo", out var brandInfo))
            {
                if (brandInfo.TryGetProperty("description", out var desc))
                    result.BrandDescription = desc.GetString() ?? "";
                
                if (brandInfo.TryGetProperty("userFocusPoints", out var points))
                {
                    result.UserFocusPoints = points.EnumerateArray()
                        .Select(p => p.GetString() ?? "")
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                }

                if (brandInfo.TryGetProperty("marketPosition", out var pos))
                    result.MarketPosition = pos.GetString() ?? "";

                if (brandInfo.TryGetProperty("competitors", out var comps))
                {
                    result.SuggestedCompetitors = comps.EnumerateArray()
                        .Select(c => c.GetString() ?? "")
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析品牌检查响应失败");
            return new BrandCheckResult { IsKnown = false, Confidence = 0 };
        }
    }

    private string BuildCompetitorInfoPrompt(CompetitorInfoRequest request)
    {
        var languagesText = request.Languages != null && request.Languages.Count > 0
            ? string.Join("、", request.Languages)
            : "中文";

        // 根据品牌是否已知，生成不同的提示
        var isUnknownBrand = request.BrandKnown == false;
        var brandKnownStatus = isUnknownBrand ? "未知品牌（AI知识库中可能不存在）" : "已知品牌";
        var brandKnownHint = isUnknownBrand 
            ? $"⚠️ \"{request.BrandName}\" 是一个新品牌或小众品牌，你的知识库中可能没有相关信息。请基于用户提供的【产品描述】和【所属行业】来推断竞品，而不是基于品牌名称。重点关注该行业的头部品牌和主流玩家。"
            : $"\"{request.BrandName}\" 是一个已知品牌，请基于你对该品牌的了解，找出其直接竞品。";
        
        // 根据品牌是否有官网，生成不同的官网要求
        var hasBrandUrl = !string.IsNullOrWhiteSpace(request.BrandUrl);
        var urlRequirement = hasBrandUrl 
            ? $"由于 \"{request.BrandName}\" 有官网，竞品也应尽量提供官网 URL"
            : "竞品有官网则提供，没有则 url 字段留空";

        var template = LoadPromptFromDb("distillation", "competitor-info", GetDefaultCompetitorInfoPrompt());
        
        return template
            .Replace("{{brandName}}", request.BrandName ?? "")
            .Replace("{{productName}}", request.ProductName ?? "")
            .Replace("{{industry}}", request.Industry ?? "")
            .Replace("{{brandUrl}}", request.BrandUrl ?? "")
            .Replace("{{description}}", request.Description ?? "")
            .Replace("{{languages}}", languagesText)
            .Replace("{{brandKnownStatus}}", brandKnownStatus)
            .Replace("{{brandKnownHint}}", brandKnownHint)
            .Replace("{{urlRequirement}}", urlRequirement);
    }

    private string GetDefaultCompetitorInfoPrompt()
    {
        return @"你是一位行业研究专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家。

【核心任务】
为品牌 ""{{brandName}}"" 找出在 {{industry}} 行业中的直接竞品，并分析用户搜索这些竞品时的关注点。

【品牌信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 官网：{{brandUrl}}
- 产品描述：{{description}}
- 目标语言：{{languages}}
- 品牌知名度：{{brandKnownStatus}}

【重要说明】
{{brandKnownHint}}

【竞品发现要求】
1. **行业头部优先**：找出 {{industry}} 行业中市场份额最大、知名度最高的 5-8 个品牌
2. **直接竞品**：这些品牌必须提供与【产品描述】中相同或高度相似的服务
3. **真实性**：只返回真实存在、你确定了解的公司/品牌
4. **官网要求**：{{urlRequirement}}
5. **不确定则跳过**：如果你不确定某个竞品的信息，宁可不列出，也不要编造

【竞品分析维度】
对于每个竞品，分析：
1. **用户关注点**：用户搜索该竞品时最关心的 3-5 个特点（用户视角表达）
2. **核心优势**：该竞品的主要差异化优势
3. **用户痛点**：用户对该竞品的常见不满
4. **典型搜索词**：用户搜索该竞品时常用的关键词（4-8字）

【行业分析】
1. **行业共同关注点**：该行业用户普遍关心的核心问题
2. **行业共同痛点**：该行业用户普遍面临的痛点
3. **高频搜索词**：用户搜索该行业产品时最常用的关键词
4. **差异化机会**：新品牌可以突出的差异化方向

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""competitors"": [
    {
      ""name"": ""竞品名称"",
      ""url"": ""官网URL（如无则留空）"",
      ""description"": ""一句话简介（20字以内）"",
      ""marketPosition"": ""行业领导者/挑战者/细分领先"",
      ""userFocusPoints"": [""关注点1"", ""关注点2"", ""关注点3""],
      ""coreStrengths"": [""优势1"", ""优势2""],
      ""userPainPoints"": [""痛点1"", ""痛点2""],
      ""typicalSearchQueries"": [""搜索词1"", ""搜索词2""]
    }
  ],
  ""industryAnalysis"": {
    ""commonFocusPoints"": [""关注点1"", ""关注点2""],
    ""commonPainPoints"": [""痛点1"", ""痛点2""],
    ""highFrequencyKeywords"": [""关键词1"", ""关键词2"", ""关键词3""],
    ""differentiationOpportunities"": [""机会1"", ""机会2""]
  },
  ""analysisConfidence"": ""high/medium/low"",
  ""confidenceReason"": ""说明为什么给出这个置信度""
}";
    }

    private CompetitorInfoResult ParseCompetitorInfoResponse(string response)
    {
        try
        {
            var json = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new CompetitorInfoResult();

            // 解析竞品列表
            if (root.TryGetProperty("competitors", out var comps))
            {
                foreach (var comp in comps.EnumerateArray())
                {
                    var competitor = new CompetitorFocusPoints
                    {
                        Name = comp.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Url = comp.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "",
                        Description = comp.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        MarketPosition = comp.TryGetProperty("marketPosition", out var mp) ? mp.GetString() ?? "" : ""
                    };
                    
                    if (comp.TryGetProperty("userFocusPoints", out var pts))
                        competitor.UserFocusPoints = ParseStringArray(pts);
                    if (comp.TryGetProperty("coreStrengths", out var cs))
                        competitor.CoreStrengths = ParseStringArray(cs);
                    if (comp.TryGetProperty("userPainPoints", out var upp))
                        competitor.UserPainPoints = ParseStringArray(upp);
                    if (comp.TryGetProperty("typicalSearchQueries", out var tsq))
                        competitor.TypicalSearchQueries = ParseStringArray(tsq);
                    
                    result.Competitors.Add(competitor);
                }
            }

            // 解析行业分析（新格式）
            if (root.TryGetProperty("industryAnalysis", out var ia))
            {
                result.IndustryAnalysis = new IndustryAnalysisResult();
                if (ia.TryGetProperty("commonFocusPoints", out var cfp))
                    result.IndustryAnalysis.CommonFocusPoints = ParseStringArray(cfp);
                if (ia.TryGetProperty("commonPainPoints", out var cpp))
                    result.IndustryAnalysis.CommonPainPoints = ParseStringArray(cpp);
                if (ia.TryGetProperty("highFrequencyKeywords", out var hfk))
                    result.IndustryAnalysis.HighFrequencyKeywords = ParseStringArray(hfk);
                if (ia.TryGetProperty("differentiationOpportunities", out var dop))
                    result.IndustryAnalysis.DifferentiationOpportunities = ParseStringArray(dop);
                
                // 同步到兼容字段
                result.IndustryCommonFocusPoints = result.IndustryAnalysis.CommonFocusPoints;
                result.SearchKeywords = result.IndustryAnalysis.HighFrequencyKeywords;
            }

            // 兼容旧格式
            if (root.TryGetProperty("industryCommonFocusPoints", out var common))
                result.IndustryCommonFocusPoints = ParseStringArray(common);
            if (root.TryGetProperty("searchKeywords", out var keywords))
                result.SearchKeywords = ParseStringArray(keywords);

            // 解析置信度
            if (root.TryGetProperty("analysisConfidence", out var ac))
                result.AnalysisConfidence = ac.GetString() ?? "";
            if (root.TryGetProperty("confidenceReason", out var cr))
                result.ConfidenceReason = cr.GetString() ?? "";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析竞品信息响应失败");
            return new CompetitorInfoResult();
        }
    }

    private List<string> ParseStringArray(JsonElement element)
    {
        return element.EnumerateArray()
            .Select(p => p.GetString() ?? "")
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }

    private string GetLanguageName(string? languageCode)
    {
        return languageCode?.ToLower() switch
        {
            "en" => "English",
            "zh_cn" => "简体中文",
            "zh_tw" => "繁體中文",
            "ja" => "日本語",
            _ => "简体中文"
        };
    }

    private string BuildQuestionsPrompt(QuestionsRequest request, string modelId)
    {
        var language = GetLanguageName(request.Language);
        var sep = ", ";
        
        var personasText = request.Personas != null && request.Personas.Count > 0
            ? string.Join(sep, request.Personas)
            : "general users";

        var sellingPointsText = request.SellingPoints != null && request.SellingPoints.Count > 0
            ? string.Join(sep, request.SellingPoints)
            : "core features";

        // 核心诉求（用户视角的需求）
        var coreNeedsText = request.CoreNeeds != null && request.CoreNeeds.Count > 0
            ? string.Join(sep, request.CoreNeeds)
            : "";

        // 产品名称（如果有则使用，否则使用品牌名）
        var productContext = !string.IsNullOrEmpty(request.ProductName) 
            ? $"Product: {request.ProductName} (by {request.BrandName})" 
            : $"Brand: {request.BrandName}";

        // 区域处理
        var marketsText = request.Markets != null && request.Markets.Count > 0 
            ? string.Join(", ", request.Markets) 
            : "";
        var regionGuidance = !string.IsNullOrEmpty(marketsText)
            ? $"\n## Region Handling (IMPLICIT)\nTarget markets: {marketsText}\n- Generate questions relevant to these markets but DO NOT explicitly mention region names in questions"
            : "";

        // 从外部文件加载 Prompt 模板
        var promptFile = modelId == "perplexity" 
            ? "wwwroot/prompts/questions-perplexity.md" 
            : "wwwroot/prompts/questions-general.md";
        
        var template = LoadPromptTemplate(promptFile);
        
        // 替换占位符
        return template
            .Replace("{{brandName}}", request.BrandName)
            .Replace("{{productContext}}", productContext)
            .Replace("{{industry}}", request.Industry)
            .Replace("{{sellingPoints}}", sellingPointsText)
            .Replace("{{coreNeeds}}", coreNeedsText)
            .Replace("{{personas}}", personasText)
            .Replace("{{language}}", language)
            .Replace("{{regionGuidance}}", regionGuidance)
            .Replace("{{currentDate}}", DateTime.UtcNow.ToString("yyyy-MM-dd"))
            .Replace("{{currentYear}}", DateTime.UtcNow.Year.ToString());
    }

    private string LoadPromptTemplate(string relativePath)
    {
        // 从路径提取 category 和 configKey
        // 例如: wwwroot/prompts/questions-perplexity.md -> category=questions, key=perplexity
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        var parts = fileName.Split('-');
        var category = parts.Length > 0 ? parts[0] : "questions";
        var configKey = parts.Length > 1 ? parts[1] : "general";

        // 1. 优先从数据库读取
        try
        {
            var dbConfig = _promptRepo.GetByKeyAsync(category, configKey).GetAwaiter().GetResult();
            if (dbConfig != null && !string.IsNullOrEmpty(dbConfig.PromptTemplate))
            {
                _logger.LogDebug("从数据库加载 Prompt: {Category}/{Key}", category, configKey);
                return dbConfig.PromptTemplate;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从数据库加载 Prompt 失败: {Category}/{Key}", category, configKey);
        }

        // 2. 从文件读取
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                _logger.LogDebug("从文件加载 Prompt: {Path}", relativePath);
                return System.IO.File.ReadAllText(fullPath);
            }
            _logger.LogWarning("Prompt 模板文件不存在: {Path}，使用默认模板", fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "加载 Prompt 模板失败: {Path}", relativePath);
        }
        
        // 3. 默认模板（兜底）
        return GetDefaultQuestionsPrompt();
    }

    private string GetDefaultQuestionsPrompt()
    {
        return @"You are simulating REAL users searching for {{industry}} solutions.

## Context
{{productContext}}
Industry: {{industry}}
Features/Keywords: {{sellingPoints}}
Target Users: {{personas}}
{{regionGuidance}}

## CRITICAL RULES
1. Questions must be GENERIC industry questions - DO NOT include brand name ""{{brandName}}""
2. Only generate HIGH-FREQUENCY questions (freq >= 70)
3. Answers should naturally mention {{brandName}} as a solution
4. Generate in {{language}}

## Output JSON
{""questions"":[{""question"":""..."",""freq"":90,""stage"":""..."",""intent"":""..."",""pattern"":""..."",""answer"":""..."",""sources"":[""...""]}]}";
    }

    /// <summary>
    /// v5.3: 从数据库加载 Prompt 模板（通用方法）
    /// 只从数据库读取，不再使用 fallback
    /// </summary>
    private string LoadPromptFromDb(string category, string configKey, string defaultTemplate)
    {
        var dbConfig = _promptRepo.GetByKeyAsync(category, configKey).GetAwaiter().GetResult();
        if (dbConfig == null || string.IsNullOrEmpty(dbConfig.PromptTemplate))
        {
            _logger.LogError("数据库中未找到 Prompt: {Category}/{Key}，请检查 prompt_configs 表", category, configKey);
            throw new InvalidOperationException($"Prompt 配置缺失: {category}/{configKey}，请在数据库 prompt_configs 表中配置");
        }
        _logger.LogDebug("从数据库加载 Prompt: {Category}/{Key}", category, configKey);
        return dbConfig.PromptTemplate;
    }

    // Prompt 模板可在 Admin 后台配置
    // 分类: distillation (卖点蒸馏、受众推断), questions (问题生成)
    // 修改 Prompt 只需编辑这些文件，无需改代码

    /// <summary>
    /// Perplexity Step1 JSON Schema：问题+答案列表
    /// </summary>
    private static object GetPerplexityStep1Schema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                questions = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            question = new { type = "string" },
                            searchIndex = new { type = "integer" },
                            brandFitIndex = new { type = "integer" },
                            score = new { type = "integer" },
                            stage = new { type = "string" },
                            intent = new { type = "string" },
                            pattern = new { type = "string" },
                            answer = new { type = "string" },
                            sources = new { type = "array", items = new { type = "string" } }
                        },
                        required = new[] { "question", "searchIndex", "brandFitIndex", "score", "answer", "sources" }
                    }
                }
            },
            required = new[] { "questions" }
        };
    }

    /// <summary>
    /// Perplexity Step2 JSON Schema：答案列表
    /// </summary>
    private static object GetPerplexityStep2Schema(int questionCount)
    {
        return new
        {
            type = "object",
            properties = new
            {
                answers = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            index = new { type = "integer" },
                            answer = new { type = "string" },
                            sources = new { type = "array", items = new { type = "string" } }
                        },
                        required = new[] { "index", "answer", "sources" }
                    }
                }
            },
            required = new[] { "answers" }
        };
    }

    /// <summary>
    /// 流式调用 AI API，支持超时重试
    /// </summary>
    private async Task<string?> CallAIAsync(string prompt, string modelId = "gemini", double temperature = 0.7, 
        string? systemPrompt = null, object? jsonSchema = null)
    {
        var (apiEndpoint, apiKey, model) = GetModelConfig(modelId);

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("[{Model}] API Key 未配置", modelId);
            return null;
        }

        const int maxRetries = 2;
        const int timeoutSeconds = 90; // 单次超时时间
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("[{Model}] API 流式调用 (尝试 {Attempt}/{Max}, timeout={Timeout}s)", 
                    modelId, attempt, maxRetries, timeoutSeconds);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                // 构建 messages：如果有 systemPrompt 则用 system + user 双消息
                var messages = new List<object>();
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }
                messages.Add(new { role = "user", content = prompt });

                // 构建请求体 - 启用流式调用
                var requestBody = new Dictionary<string, object>
                {
                    ["model"] = model,
                    ["messages"] = messages,
                    ["max_tokens"] = 16384,
                    ["temperature"] = temperature,
                    ["stream"] = true,  // 启用流式
                    ["stream_options"] = new { include_usage = true }
                };

                // Perplexity: 添加 response_format (json_schema) 强制 JSON 输出
                if (jsonSchema != null)
                {
                    requestBody["response_format"] = new
                    {
                        type = "json_schema",
                        json_schema = new { schema = jsonSchema }
                    };
                    _logger.LogInformation("[{Model}] 使用 json_schema response_format", modelId);
                }

                var startTime = DateTime.Now;
                
                // 发送请求
                var request = new HttpRequestMessage(HttpMethod.Post, apiEndpoint)
                {
                    Content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(requestBody), 
                        System.Text.Encoding.UTF8, 
                        "application/json")
                };
                
                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("[{Model}] API 调用失败: {Status} {Response}", modelId, response.StatusCode, 
                        errorJson.Length > 200 ? errorJson.Substring(0, 200) : errorJson);
                    if (attempt < maxRetries) 
                    { 
                        _logger.LogInformation("[{Model}] 等待 3 秒后重试...", modelId);
                        await Task.Delay(3000); 
                        continue; 
                    }
                    return null;
                }

                // 流式读取响应
                var contentBuilder = new System.Text.StringBuilder();
                using var stream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new System.IO.StreamReader(stream);
                
                int chunkCount = 0;
                string? citationsJson = null;
                
                while (!streamReader.EndOfStream)
                {
                    var line = await streamReader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;
                    
                    var data = line.Substring(6);
                    if (data == "[DONE]") break;
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(data);
                        var root = doc.RootElement;
                        
                        // 提取 Perplexity citations
                        if (modelId == "perplexity" && root.TryGetProperty("citations", out var citations))
                        {
                            var citationList = citations.EnumerateArray()
                                .Select(c => c.GetString() ?? "")
                                .Where(c => !string.IsNullOrEmpty(c))
                                .ToList();
                            if (citationList.Count > 0)
                            {
                                citationsJson = System.Text.Json.JsonSerializer.Serialize(citationList);
                            }
                        }
                        
                        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var delta = choices[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out var content))
                            {
                                contentBuilder.Append(content.GetString());
                                chunkCount++;
                            }
                        }
                    }
                    catch { /* 忽略单个 chunk 解析错误 */ }
                }
                
                var responseTime = (DateTime.Now - startTime).TotalSeconds;
                var fullContent = contentBuilder.ToString();
                
                _logger.LogInformation("[{Model}] 流式调用成功 (耗时: {Time:F1}s, chunks: {Chunks}, 响应长度: {Len})", 
                    modelId, responseTime, chunkCount, fullContent.Length);
                
                // Perplexity 特殊处理：附加 citations
                if (!string.IsNullOrEmpty(citationsJson))
                {
                    _logger.LogInformation("[perplexity] 获取到引用来源");
                    fullContent = fullContent + "\n<!--CITATIONS:" + citationsJson + "-->";
                }
                
                return fullContent;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("[{Model}] 请求超时 ({Timeout}s)", modelId, timeoutSeconds);
                if (attempt < maxRetries)
                {
                    _logger.LogInformation("[{Model}] 等待 3 秒后重试...", modelId);
                    await Task.Delay(3000);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("[{Model}] 网络异常: {Msg}", modelId, ex.Message);
                if (attempt < maxRetries)
                {
                    _logger.LogInformation("[{Model}] 等待 3 秒后重试...", modelId);
                    await Task.Delay(3000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Model}] API 调用异常", modelId);
                return null;
            }
        }
        return null;
    }

    private List<SellingPointItem> ParseSellingPointsResponse(string response)
    {
        try
        {
            var cleanJson = CleanJsonResponse(response);
            _logger.LogDebug("清理后的 JSON: {Json}", cleanJson.Length > 500 ? cleanJson.Substring(0, 500) + "..." : cleanJson);
            
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var result = new List<SellingPointItem>();

            if (root.TryGetProperty("sellingPoints", out var points))
            {
                foreach (var point in points.EnumerateArray())
                {
                    result.Add(new SellingPointItem
                    {
                        Point = point.GetProperty("point").GetString() ?? "",
                        Weight = point.TryGetProperty("weight", out var w) ? w.GetInt32() : 5,
                        Usage = point.TryGetProperty("usage", out var u) ? u.GetString() ?? "" : ""
                    });
                }
            }

            _logger.LogInformation("成功解析 {Count} 个卖点", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析卖点响应失败，原始响应长度: {Length}, 前200字符: {Preview}", 
                response?.Length ?? 0, 
                response?.Length > 200 ? response.Substring(0, 200) : response);
            return new List<SellingPointItem>();
        }
    }

    private AudienceResult ParseAudienceResponse(string response)
    {
        try
        {
            var cleanJson = CleanJsonResponse(response);
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            return new AudienceResult
            {
                UserTypes = root.TryGetProperty("userTypes", out var ut)
                    ? ut.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : new List<string>(),
                Roles = root.TryGetProperty("roles", out var r)
                    ? r.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : new List<string>(),
                Needs = root.TryGetProperty("coreNeeds", out var cn)
                    ? cn.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                    : (root.TryGetProperty("needs", out var n)
                        ? n.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                        : new List<string>())
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析受众响应失败: {Response}", response);
            return new AudienceResult();
        }
    }

    private List<GeneratedQuestion> ParseQuestionsResponse(string response, string modelId)
    {
        try
        {
            // 提取 Perplexity 的 citations（如果有）
            List<string>? perplexityCitations = null;
            var citationsMarker = "<!--CITATIONS:";
            var citationsIdx = response.IndexOf(citationsMarker);
            if (citationsIdx >= 0)
            {
                var endIdx = response.IndexOf("-->", citationsIdx);
                if (endIdx > citationsIdx)
                {
                    var citationsJson = response.Substring(citationsIdx + citationsMarker.Length, endIdx - citationsIdx - citationsMarker.Length);
                    perplexityCitations = System.Text.Json.JsonSerializer.Deserialize<List<string>>(citationsJson);
                    response = response.Substring(0, citationsIdx); // 移除 citations 标记
                    _logger.LogInformation("[{Model}] 提取到 {Count} 个 Perplexity citations", modelId, perplexityCitations?.Count ?? 0);
                }
            }
            
            var cleanJson = CleanJsonResponse(response);
            _logger.LogInformation("[{Model}] 清理后JSON前200字符: {Json}", modelId, cleanJson.Length > 200 ? cleanJson.Substring(0, 200) : cleanJson);
            
            using var doc = JsonDocument.Parse(cleanJson);
            var root = doc.RootElement;

            var result = new List<GeneratedQuestion>();

            if (root.TryGetProperty("questions", out var questions))
            {
                foreach (var q in questions.EnumerateArray())
                {
                    var sources = ParseSources(q);
                    // 如果问题本身没有 sources，但有 Perplexity citations，则使用 citations
                    if ((sources == null || sources.Count == 0) && perplexityCitations != null && perplexityCitations.Count > 0)
                    {
                        sources = perplexityCitations;
                    }
                    
                    // 对于 perplexity 模型，始终使用 "perplexity" 作为 Model，不使用 target_model
                    // 这样前端可以正确显示 Perplexity 的回答
                    var gq = new GeneratedQuestion
                    {
                        Model = modelId,
                        Persona = q.TryGetProperty("persona", out var p) ? p.GetString() ?? "" : "",
                        SellingPoint = q.TryGetProperty("keyword", out var kw) ? kw.GetString() ?? "" : q.TryGetProperty("selling_point", out var sp) ? sp.GetString() ?? "" : "",
                        Stage = q.TryGetProperty("stage", out var s) ? s.GetString() ?? "" : "",
                        Question = q.TryGetProperty("question", out var qn) ? qn.GetString() ?? "" : "",
                        Intent = q.TryGetProperty("intent", out var i) ? i.GetString() ?? "" : "",
                        Pattern = q.TryGetProperty("pattern", out var pt) ? pt.GetString() ?? "" : "",
                        FreqScore = q.TryGetProperty("freq_score", out var fs) ? fs.GetInt32() : (q.TryGetProperty("freq", out var f) ? f.GetInt32() : 0),
                        Answer = q.TryGetProperty("answer", out var ans) ? ans.GetString() ?? "" : "",
                        Sources = sources ?? new List<string>()
                    };
                    _logger.LogInformation("[{Model}] 解析问题: Q={Question}, Freq={Freq}, Pattern={Pattern}, AnswerLen={Answer}, SourcesCnt={Sources}", 
                        modelId, gq.Question?.Substring(0, Math.Min(30, gq.Question?.Length ?? 0)), gq.FreqScore, gq.Pattern ?? "(null)", gq.Answer?.Length ?? 0, gq.Sources?.Count ?? 0);
                    result.Add(gq);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析问题响应失败: {Response}", response);
            return new List<GeneratedQuestion>();
        }
    }

    private List<string> ParseSources(JsonElement q)
    {
        if (!q.TryGetProperty("sources", out var src))
        {
            // 尝试其他可能的字段名
            if (q.TryGetProperty("source", out src) || q.TryGetProperty("refs", out src) || q.TryGetProperty("references", out src))
            {
                _logger.LogInformation("ParseSources: 使用备选字段名找到 sources");
            }
            else
            {
                _logger.LogInformation("ParseSources: 未找到 sources 字段");
                return new List<string>();
            }
        }

        _logger.LogInformation("ParseSources: 字段类型={Kind}, 原始值={Raw}", src.ValueKind, src.GetRawText());

        // 如果是数组，直接解析
        if (src.ValueKind == JsonValueKind.Array)
        {
            var result = src.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
            _logger.LogDebug("ParseSources: 数组解析结果={Count}项", result.Count);
            return result;
        }

        // 如果是字符串，按空格或逗号分割，提取引用标记
        if (src.ValueKind == JsonValueKind.String)
        {
            var str = src.GetString() ?? "";
            // 匹配 [1], [2] 等格式，或直接按空格分割
            var matches = System.Text.RegularExpressions.Regex.Matches(str, @"\[[\w\d]+\]|[^\s,]+");
            var result = matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).Where(s => !string.IsNullOrEmpty(s)).ToList();
            _logger.LogDebug("ParseSources: 字符串解析结果={Count}项", result.Count);
            return result;
        }

        return new List<string>();
    }

    /// <summary>
    /// 将诊断内容写入 diag 目录（用于调试，失败不影响主流程）
    /// </summary>
    private void WriteDiagFile(string taskId, string modelId, string step, string content)
    {
        try
        {
            var diagDir = Path.Combine(AppContext.BaseDirectory, "diag");
            Directory.CreateDirectory(diagDir);
            var fileName = $"{taskId}_{modelId}_{step}_{DateTime.UtcNow:HHmmss}.txt";
            var diagFile = Path.Combine(diagDir, fileName);
            System.IO.File.WriteAllText(diagFile, content);
            _logger.LogDebug("[{TaskId}][{Model}] 诊断文件已保存: {File} ({Len} 字符)", taskId, modelId, fileName, content.Length);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[{TaskId}][{Model}] 诊断文件写入失败: {Step}", taskId, modelId, step);
        }
    }

    private string CleanJsonResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response)) return "{}";

        var json = response.Trim();

        if (json.StartsWith("```json"))
        {
            json = json.Substring(7);
        }
        else if (json.StartsWith("```"))
        {
            json = json.Substring(3);
        }

        if (json.EndsWith("```"))
        {
            json = json.Substring(0, json.Length - 3);
        }

        var startIndex = json.IndexOf('{');
        var endIndex = json.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            json = json.Substring(startIndex, endIndex - startIndex + 1);
        }
        else if (startIndex >= 0)
        {
            // JSON被截断，尝试修复
            json = json.Substring(startIndex);
            // 计算未闭合的括号
            int braces = 0, brackets = 0;
            foreach (var c in json) { if (c == '{') braces++; else if (c == '}') braces--; else if (c == '[') brackets++; else if (c == ']') brackets--; }
            // 补全括号
            json += new string(']', brackets) + new string('}', braces);
        }

        // v4.4: 修复 JSON 字符串中的非法换行符
        // AI 有时会在字符串值中返回真实换行符，这在 JSON 规范中是不允许的
        json = FixJsonNewlines(json);

        return json.Trim();
    }
    
    /// <summary>
    /// v4.4: 修复 JSON 字符串中的非法换行符
    /// 将字符串值中的真实换行符转换为 \n 转义序列
    /// </summary>
    private string FixJsonNewlines(string json)
    {
        var result = new System.Text.StringBuilder();
        bool inString = false;
        bool escaped = false;
        
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            
            if (escaped)
            {
                result.Append(c);
                escaped = false;
                continue;
            }
            
            if (c == '\\')
            {
                result.Append(c);
                escaped = true;
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                result.Append(c);
                continue;
            }
            
            if (inString)
            {
                // 在字符串内部，将真实换行符转换为转义序列
                if (c == '\n')
                {
                    result.Append("\\n");
                }
                else if (c == '\r')
                {
                    // 跳过 \r，只保留 \n
                }
                else if (c == '\t')
                {
                    result.Append("\\t");
                }
                else
                {
                    result.Append(c);
                }
            }
            else
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }

    private (string endpoint, string apiKey, string model) GetModelConfig(string modelId)
    {
        // 优先从缓存读取（数据库驱动）
        var (endpoint, apiKey, model) = _configCache.GetModelConfig(modelId);
        if (!string.IsNullOrEmpty(apiKey))
        {
            return (endpoint, apiKey, model);
        }

        // 缓存未命中时回退到 config.json（兼容过渡期）
        _logger.LogWarning("[GetModelConfig] 缓存未命中 {ModelId}，回退到 config.json", modelId);
        try
        {
            var cfgPath = Path.Combine(AppContext.BaseDirectory, "prompts", "config.json");
            if (!System.IO.File.Exists(cfgPath))
                cfgPath = @"C:\Users\Administrator\source\GCore\prompts\config.json";
            
            var json = System.IO.File.ReadAllText(cfgPath);
            using var doc = JsonDocument.Parse(json);
            var m = doc.RootElement.GetProperty("models").GetProperty(modelId.ToLower());
            return (m.GetProperty("endpoint").GetString()!, m.GetProperty("api_key").GetString()!, m.GetProperty("model").GetString()!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GetModelConfig] config.json 回退也失败: {ModelId}", modelId);
            return ("", "", "");
        }
    }
}

#region Request/Response Models

/// <summary>
/// 答案生成模式
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum AnswerMode
{
    /// <summary>
    /// 软文模式：Chain-of-Density 格式，强植入品牌，可直接发布
    /// </summary>
    Content,
    
    /// <summary>
    /// AI 模拟模式：模拟各 AI 引擎的自然回答，检测品牌引用情况
    /// </summary>
    Simulation
}

public class SellingPointsRequest
{
    public string BrandName { get; set; } = "";      // 品牌名称（公司/品牌）
    public string ProductName { get; set; } = "";    // 产品名称（具体产品，可选）
    public string Industry { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string>? Markets { get; set; }       // 旧版：市场名称（如"中国"、"美国"）
    public List<string>? Languages { get; set; }     // 语言代码（如"zh-CN"、"en"）
    public List<string>? Countries { get; set; }     // 新版：国家代码（如"CN"、"US"）
    
    // 新增：竞品信息（用于增强卖点蒸馏）
    public List<string>? Competitors { get; set; }
    
    // 新增：品牌检查结果（可选，由前端传入或后端重新判断）
    public BrandCheckResult? BrandCheckResult { get; set; }
    
    // 新增：竞品用户关注点（可选，由前端传入）
    public List<string>? CompetitorFocusPoints { get; set; }
}

public class SellingPointItem
{
    public string Point { get; set; } = "";
    public int Weight { get; set; }
    public string Usage { get; set; } = "";
    public string Country { get; set; } = "CN";
    public string Language { get; set; } = "zh-CN";
}

public class AudienceRequest
{
    public string BrandName { get; set; } = "";      // 品牌名称（公司/品牌）
    public string ProductName { get; set; } = "";    // 产品名称（具体产品，可选）
    public string Industry { get; set; } = "";
    public string Description { get; set; } = "";    // 产品描述
    public List<string>? SellingPoints { get; set; }
    public List<string>? Languages { get; set; }     // 监控语言
    public List<string>? Countries { get; set; }     // 目标国家
}

public class AudienceResult
{
    public List<string> UserTypes { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public List<string> Needs { get; set; } = new();
    public string Country { get; set; } = "CN";
    public string Language { get; set; } = "zh-CN";
}

public class GenerateArticleRequest
{
    public string BrandName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Question { get; set; } = "";
    public string ModelId { get; set; } = "";
    public List<string>? Sources { get; set; }
}

/// <summary>
/// v4.3: 按需获取详细答案请求
/// </summary>
public class FetchAnswerRequest
{
    public string Question { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string BrandName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Language { get; set; } = "zh_cn";
    public List<string>? Competitors { get; set; }
    public List<string>? Sources { get; set; }
    public AnswerMode AnswerMode { get; set; } = AnswerMode.Simulation;
    public bool FetchSources { get; set; } = true;  // 是否同时获取 Perplexity 来源
}

/// <summary>
/// v6.0: 带国家-语言绑定信息的卖点
/// </summary>
public class SellingPointWithBinding
{
    public string Point { get; set; } = "";
    public string Country { get; set; } = "CN";
    public string Language { get; set; } = "zh-CN";
    public int Weight { get; set; } = 5;
    public string? Usage { get; set; }
}

/// <summary>
/// v6.1: 带国家-语言绑定信息的受众
/// </summary>
public class PersonaWithBinding
{
    public string Name { get; set; } = "";
    public string Country { get; set; } = "CN";
    public string Language { get; set; } = "zh-CN";
}

public class QuestionsRequest
{
    public string BrandName { get; set; } = "";      // 品牌名称（公司/品牌，用于答案植入）
    public string ProductName { get; set; } = "";    // 产品名称（具体产品，可选）
    public string Industry { get; set; } = "";
    public List<string>? SellingPoints { get; set; }  // 保留，向后兼容
    public List<string>? CoreNeeds { get; set; }      // 核心诉求（用户视角的需求）
    public List<string>? Personas { get; set; }
    public List<string>? Stages { get; set; }
    public List<string> Models { get; set; } = new() { "gpt" };
    public List<string>? Languages { get; set; }  // 多语言支持
    public List<string>? Markets { get; set; }    // 目标市场（隐式区域）
    public List<string>? Countries { get; set; }  // v5.0: 目标国家代码（如 CN、US）
    public string Language { get; set; } = "zh_cn";  // 兼容旧逻辑
    public string Region { get; set; } = "china";
    
    // v6.0 新增：带绑定信息的卖点（优先使用）
    public List<SellingPointWithBinding>? SellingPointsWithBinding { get; set; }
    
    // v6.1 新增：带绑定信息的受众（优先使用）
    public List<PersonaWithBinding>? PersonasWithBinding { get; set; }
    
    // v2.0 新增：竞品信息和验证选项
    public List<string>? Competitors { get; set; }   // 竞品列表
    public bool EnableGoogleTrends { get; set; } = true;  // Google Trends 验证（必须启用）
    
    // v4.2 新增：Reddit/论坛真实问题搜索
    public bool EnableRedditSearch { get; set; } = true;  // Reddit/论坛真实问题搜索（必须启用）
    
    // v4.3 新增：轻量级模式（只生成问题+元数据，不生成详细答案）
    public bool EnableLightweightMode { get; set; } = false;  // 是否启用轻量级模式
    
    // v3.0 新增：答案生成模式
    public AnswerMode AnswerMode { get; set; } = AnswerMode.Content;  // 默认软文模式
    
    // v4.5 新增：项目保存（Phase 1.6）
    public long? ProjectId { get; set; }  // 关联项目 ID（可选，如果提供则自动保存问题到项目）
    public long? UserId { get; set; }     // 用户 ID（从 Header 获取或前端传递）
    
    /// <summary>
    /// 获取有效的语言列表（优先使用 Languages，否则使用 Language）
    /// </summary>
    public List<string> GetEffectiveLanguages()
    {
        if (Languages != null && Languages.Count > 0)
            return Languages;
        return new List<string> { Language ?? "zh_cn" };
    }
    
    /// <summary>
    /// v6.0: 获取指定语言的卖点（优先从 SellingPointsWithBinding 筛选）
    /// </summary>
    public List<string> GetSellingPointsForLanguage(string language)
    {
        if (SellingPointsWithBinding != null && SellingPointsWithBinding.Count > 0)
        {
            var filtered = SellingPointsWithBinding
                .Where(p => p.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Point)
                .ToList();
            if (filtered.Count > 0) return filtered;
        }
        // 回退到旧字段
        return SellingPoints ?? new List<string>();
    }
    
    /// <summary>
    /// v6.0: 获取语言对应的国家代码
    /// </summary>
    public string GetCountryForLanguage(string language)
    {
        if (SellingPointsWithBinding != null && SellingPointsWithBinding.Count > 0)
        {
            var match = SellingPointsWithBinding
                .FirstOrDefault(p => p.Language.Equals(language, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match.Country;
        }
        // 回退到旧逻辑
        return Countries?.FirstOrDefault() ?? "CN";
    }
    
    /// <summary>
    /// v6.1: 获取指定语言的受众（优先从 PersonasWithBinding 筛选）
    /// </summary>
    public List<string> GetPersonasForLanguage(string language)
    {
        if (PersonasWithBinding != null && PersonasWithBinding.Count > 0)
        {
            var filtered = PersonasWithBinding
                .Where(p => p.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();
            if (filtered.Count > 0) return filtered;
        }
        // 回退到旧字段
        return Personas ?? new List<string>();
    }
}

public class GeneratedQuestion
{
    [System.Text.Json.Serialization.JsonPropertyName("model")]
    public string Model { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("question")]
    public string Question { get; set; } = "";
    
    // v2.0 新增：双指标评分（由 AI 评估）
    [System.Text.Json.Serialization.JsonPropertyName("searchIndex")]
    public int SearchIndex { get; set; } = 0;      // 搜索热度指数 (0-100)
    [System.Text.Json.Serialization.JsonPropertyName("brandFitIndex")]
    public int BrandFitIndex { get; set; } = 0;    // 品牌植入指数 (0-100)
    [System.Text.Json.Serialization.JsonPropertyName("score")]
    public int Score { get; set; } = 0;            // SearchIndex × BrandFitIndex
    
    // v2.0 新增：外部验证数据（可选）
    [System.Text.Json.Serialization.JsonPropertyName("googleTrendsHeat")]
    public int? GoogleTrendsHeat { get; set; }     // Google Trends 热度
    [System.Text.Json.Serialization.JsonPropertyName("baiduIndex")]
    public int? BaiduIndex { get; set; }           // 百度指数（5118）
    [System.Text.Json.Serialization.JsonPropertyName("perplexityValidated")]
    public bool? PerplexityValidated { get; set; } // Perplexity 验证通过
    
    [System.Text.Json.Serialization.JsonPropertyName("stage")]
    public string Stage { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("intent")]
    public string Intent { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("pattern")]
    public string Pattern { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("answer")]
    public string Answer { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("sources")]
    public List<string> Sources { get; set; } = new();
    
    // 保留旧字段（兼容性）
    [System.Text.Json.Serialization.JsonPropertyName("freqScore")]
    public int FreqScore { get => SearchIndex; set => SearchIndex = value; }  // 兼容旧版本，映射到 SearchIndex
    [System.Text.Json.Serialization.JsonPropertyName("persona")]
    public string? Persona { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("sellingPoint")]
    public string? SellingPoint { get; set; }
    
    // v3.0 新增：答案模式和品牌引用分析
    [System.Text.Json.Serialization.JsonPropertyName("answerMode")]
    public string? AnswerModeUsed { get; set; }  // "content" 或 "simulation"
    
    // v4.0 新增：问题来源标注
    [System.Text.Json.Serialization.JsonPropertyName("source")]
    public string Source { get; set; } = "ai";  // "ai" = AI生成, "real" = 真实问题发现
    
    [System.Text.Json.Serialization.JsonPropertyName("sourceDetail")]
    public string? SourceDetail { get; set; }  // 来源详情，如 "r/SEO", "Quora", "知乎" 等
    
    [System.Text.Json.Serialization.JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; set; }  // 原始来源 URL
    
    // v4.1 新增：上升趋势指标
    [System.Text.Json.Serialization.JsonPropertyName("trendDirection")]
    public string? TrendDirection { get; set; }  // "rising" = 上升, "stable" = 稳定, "declining" = 下降
    
    [System.Text.Json.Serialization.JsonPropertyName("trendScore")]
    public int? TrendScore { get; set; }  // 趋势强度 (0-100)，越高表示上升趋势越明显
    
    [System.Text.Json.Serialization.JsonPropertyName("contentAnswer")]
    public string? ContentAnswer { get; set; }  // 软文模式答案
    
    [System.Text.Json.Serialization.JsonPropertyName("simulationAnswer")]
    public string? SimulationAnswer { get; set; }  // AI 模拟模式答案
    
    [System.Text.Json.Serialization.JsonPropertyName("brandAnalysis")]
    public BrandCitationAnalysis? BrandAnalysis { get; set; }  // 品牌引用分析
    
    // v4.3 新增：轻量级模式标记
    [System.Text.Json.Serialization.JsonPropertyName("lightweightMode")]
    public bool LightweightMode { get; set; } = false;  // 是否为轻量级模式（无详细答案）
    
    // v4.4 新增：引用难度评估 (Phase 1.5)
    [System.Text.Json.Serialization.JsonPropertyName("citationDifficulty")]
    public CitationDifficulty? CitationDifficulty { get; set; }  // 引用难度评估
    
    // v5.0 新增：国家和语言（按国家-语言维度生成问题）
    [System.Text.Json.Serialization.JsonPropertyName("country")]
    public string Country { get; set; } = "CN";  // 国家代码
    
    [System.Text.Json.Serialization.JsonPropertyName("language")]
    public string Language { get; set; } = "zh-CN";  // 语言代码
}

/// <summary>
/// 引用难度评估 (Phase 1.5)
/// </summary>
public class CitationDifficulty
{
    [System.Text.Json.Serialization.JsonPropertyName("score")]
    public int Score { get; set; } = 50;  // 难度分数 (0-100)，越高越难
    
    [System.Text.Json.Serialization.JsonPropertyName("level")]
    public string Level { get; set; } = "medium";  // easy/medium/hard
    
    [System.Text.Json.Serialization.JsonPropertyName("factors")]
    public CitationDifficultyFactors? Factors { get; set; }  // 影响因素
    
    [System.Text.Json.Serialization.JsonPropertyName("reasoning")]
    public string? Reasoning { get; set; }  // 难度评估理由
    
    [System.Text.Json.Serialization.JsonPropertyName("actionableInsights")]
    public List<string>? ActionableInsights { get; set; }  // 可执行的改进建议
}

/// <summary>
/// 引用难度影响因素
/// </summary>
public class CitationDifficultyFactors
{
    [System.Text.Json.Serialization.JsonPropertyName("competitorDominance")]
    public int CompetitorDominance { get; set; } = 50;  // 竞品主导程度 (0-100)
    
    [System.Text.Json.Serialization.JsonPropertyName("topicRelevance")]
    public int TopicRelevance { get; set; } = 50;  // 话题相关性 (0-100)
    
    [System.Text.Json.Serialization.JsonPropertyName("contentGap")]
    public int ContentGap { get; set; } = 50;  // 内容缺口 (0-100)，越高表示机会越大
    
    [System.Text.Json.Serialization.JsonPropertyName("authorityRequired")]
    public int AuthorityRequired { get; set; } = 50;  // 所需权威度 (0-100)
}

/// <summary>
/// 品牌引用分析结果
/// </summary>
public class BrandCitationAnalysis
{
    [System.Text.Json.Serialization.JsonPropertyName("mentioned")]
    public bool Mentioned { get; set; } = false;  // 是否被提及
    
    [System.Text.Json.Serialization.JsonPropertyName("mentionCount")]
    public int MentionCount { get; set; } = 0;  // 提及次数
    
    [System.Text.Json.Serialization.JsonPropertyName("position")]
    public int Position { get; set; } = 0;  // 在答案中的位置（1=第一个提及，0=未提及）
    
    [System.Text.Json.Serialization.JsonPropertyName("mentionType")]
    public string MentionType { get; set; } = "not_mentioned";  // recommended/listed/compared/example/not_mentioned
    
    [System.Text.Json.Serialization.JsonPropertyName("mentionContext")]
    public string? MentionContext { get; set; }  // 提及的具体句子
    
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; set; }  // AI 为什么会/不会提及该品牌
    
    [System.Text.Json.Serialization.JsonPropertyName("brandVisibility")]
    public string BrandVisibility { get; set; } = "none";  // high/medium/low/none
    
    [System.Text.Json.Serialization.JsonPropertyName("improvementPotential")]
    public string? ImprovementPotential { get; set; }  // 改进建议
    
    [System.Text.Json.Serialization.JsonPropertyName("competitorsMentioned")]
    public List<CompetitorMention>? CompetitorsMentioned { get; set; }  // 竞品提及情况
}

/// <summary>
/// 竞品提及情况
/// </summary>
public class CompetitorMention
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [System.Text.Json.Serialization.JsonPropertyName("mentioned")]
    public bool Mentioned { get; set; } = false;
    
    [System.Text.Json.Serialization.JsonPropertyName("position")]
    public int Position { get; set; } = 0;
    
    [System.Text.Json.Serialization.JsonPropertyName("mentionType")]
    public string MentionType { get; set; } = "not_mentioned";
}

public class CompetitorsRequest
{
    public string BrandName { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
}

public class QuestionOnlyItem
{
    public string Question { get; set; } = "";
    
    // v2.0 新增：双指标评分
    public int SearchIndex { get; set; } = 0;      // 搜索热度指数 (0-100)
    public int BrandFitIndex { get; set; } = 0;    // 品牌植入指数 (0-100)
    public int Score { get; set; } = 0;            // SearchIndex × BrandFitIndex
    
    public string Pattern { get; set; } = "";
    public string Intent { get; set; } = "";
    public string Stage { get; set; } = "";
    
    // 保留旧字段（兼容性）
    public int FreqScore { get => SearchIndex; set => SearchIndex = value; }
    public string? Persona { get; set; }
    public string? SellingPoint { get; set; }
    
    // v4.2 新增：问题来源标注
    public string Source { get; set; } = "ai";     // "ai" = AI生成, "real" = 真实问题
    public string? SourceDetail { get; set; }      // 来源详情，如 "r/SEO", "Quora"
    public string? SourceUrl { get; set; }         // 来源 URL
    
    // v5.2 新增：国家和语言（确保问题与答案的国家一致）
    public string Country { get; set; } = "CN";    // 国家代码
    public string Language { get; set; } = "zh-CN"; // 语言代码
}

/// <summary>
/// 品牌检查请求
/// </summary>
public class BrandCheckRequest
{
    public string BrandName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Industry { get; set; } = "";
    public List<string>? Languages { get; set; }
}

/// <summary>
/// 品牌检查结果
/// </summary>
public class BrandCheckResult
{
    public bool IsKnown { get; set; } = false;
    public int Confidence { get; set; } = 0;
    public string BrandDescription { get; set; } = "";
    public List<string> UserFocusPoints { get; set; } = new();
    public string MarketPosition { get; set; } = "";
    public List<string> SuggestedCompetitors { get; set; } = new();
}

/// <summary>
/// 竞品信息提取请求
/// </summary>
public class CompetitorInfoRequest
{
    public string BrandName { get; set; } = "";           // 品牌名称
    public string ProductName { get; set; } = "";         // 产品名称
    public string Industry { get; set; } = "";            // 所属行业
    public string BrandUrl { get; set; } = "";            // 品牌官网
    public string Description { get; set; } = "";         // 产品描述
    public List<string>? Languages { get; set; }          // 目标语言
    public bool? BrandKnown { get; set; }                 // 品牌是否已知（来自 brand-check 结果）
    public List<string> Competitors { get; set; } = new(); // 已知竞品列表（可选，用于补充分析）
}

/// <summary>
/// 竞品信息提取结果
/// </summary>
public class CompetitorInfoResult
{
    public List<CompetitorFocusPoints> Competitors { get; set; } = new();
    public IndustryAnalysisResult? IndustryAnalysis { get; set; }
    public string AnalysisConfidence { get; set; } = "";      // high/medium/low
    public string ConfidenceReason { get; set; } = "";
    
    // 兼容旧版本
    public List<string> IndustryCommonFocusPoints { get; set; } = new();
    public List<string> SearchKeywords { get; set; } = new();
}

/// <summary>
/// 行业分析结果
/// </summary>
public class IndustryAnalysisResult
{
    public List<string> CommonFocusPoints { get; set; } = new();
    public List<string> CommonPainPoints { get; set; } = new();
    public List<string> HighFrequencyKeywords { get; set; } = new();
    public List<string> DifferentiationOpportunities { get; set; } = new();
}

/// <summary>
/// 单个竞品的详细信息
/// </summary>
public class CompetitorFocusPoints
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public string MarketPosition { get; set; } = "";
    public List<string> UserFocusPoints { get; set; } = new();
    public List<string> CoreStrengths { get; set; } = new();
    public List<string> UserPainPoints { get; set; } = new();
    public List<string> TypicalSearchQueries { get; set; } = new();
}

/// <summary>
/// v4.0: 真实问题发现请求
/// </summary>
public class RealQuestionsRequest
{
    public string BrandName { get; set; } = "";
    public string Industry { get; set; } = "";
    public List<string>? Keywords { get; set; }  // 搜索关键词（卖点）
    public string Language { get; set; } = "zh_cn";
    public List<string>? Competitors { get; set; }  // 竞品列表
}

#endregion
