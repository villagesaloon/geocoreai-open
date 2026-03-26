using GeoCore.SaaS.Services.LLMCitationOptimization;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.SaaS.Controllers;

[ApiController]
[Route("api/llm-citation")]
public class LLMCitationController : ControllerBase
{
    private readonly LLMCitationOptimizationService _citationService;
    private readonly ILogger<LLMCitationController> _logger;

    public LLMCitationController(LLMCitationOptimizationService citationService, ILogger<LLMCitationController> logger)
    {
        _citationService = citationService;
        _logger = logger;
    }

    /// <summary>
    /// 7.1 获取所有 LLM 平台偏好来源
    /// </summary>
    [HttpGet("platforms")]
    public ActionResult<List<LLMPlatformPreferences>> GetAllPlatforms()
    {
        var result = _citationService.GetAllPlatformPreferences();
        return Ok(result);
    }

    /// <summary>
    /// 7.1 获取特定平台偏好来源
    /// </summary>
    [HttpGet("platforms/{platform}")]
    public ActionResult<LLMPlatformPreferences> GetPlatform(string platform)
    {
        var result = _citationService.GetPlatformPreferences(platform);
        if (result == null)
        {
            return NotFound($"Platform '{platform}' not found");
        }
        return Ok(result);
    }

    /// <summary>
    /// 7.2 生成平台特定优化策略
    /// </summary>
    [HttpPost("optimize")]
    public ActionResult<PlatformOptimizationResult> GenerateOptimization([FromBody] PlatformOptimizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest("Brand name is required");
        }

        if (request.TargetPlatforms == null || request.TargetPlatforms.Count == 0)
        {
            request.TargetPlatforms = new List<string> { "chatgpt", "perplexity", "gemini", "claude", "grok" };
        }

        var result = _citationService.GenerateOptimizationStrategy(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.3 评估引用源可操作性
    /// </summary>
    [HttpGet("actionability/{platform}")]
    public ActionResult<List<CitationSource>> EvaluateActionability(string platform)
    {
        var result = _citationService.EvaluateActionability(platform);
        if (result.Count == 0)
        {
            return NotFound($"Platform '{platform}' not found");
        }
        return Ok(result);
    }

    /// <summary>
    /// 7.4 跨平台分析
    /// </summary>
    [HttpPost("cross-platform")]
    public ActionResult<CrossPlatformAnalysis> AnalyzeCrossPlatform([FromBody] List<string> platforms)
    {
        if (platforms == null || platforms.Count < 2)
        {
            platforms = new List<string> { "chatgpt", "perplexity", "gemini", "claude", "grok" };
        }

        var result = _citationService.AnalyzeCrossPlatform(platforms);
        return Ok(result);
    }

    /// <summary>
    /// 7.5 获取内容模板
    /// </summary>
    [HttpPost("template")]
    public ActionResult<ContentTemplateResult> GetContentTemplate([FromBody] ContentTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Platform))
        {
            return BadRequest("Platform is required");
        }

        var result = _citationService.GetContentTemplate(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.6 生成 30 天路线图
    /// </summary>
    [HttpPost("roadmap")]
    public ActionResult<OptimizationRoadmap> GenerateRoadmap([FromBody] PlatformOptimizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest("Brand name is required");
        }

        var result = _citationService.Generate30DayRoadmap(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.7 获取效果预期时间线
    /// </summary>
    [HttpGet("timelines")]
    public ActionResult<List<EffectTimeline>> GetEffectTimelines()
    {
        var result = _citationService.GetEffectTimelines();
        return Ok(result);
    }

    /// <summary>
    /// 7.8 计算平台优先级
    /// </summary>
    [HttpPost("priority")]
    public ActionResult<PlatformPriorityResult> CalculatePriority([FromBody] PlatformPriorityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Industry))
        {
            return BadRequest("Industry is required");
        }

        var result = _citationService.CalculatePlatformPriority(request);
        return Ok(result);
    }

    #region Phase 7 新功能 (7.14-7.18)

    /// <summary>
    /// 7.14 Wikipedia 风格内容策略分析
    /// 原理：Wikipedia 占 ChatGPT 47.9% 引用
    /// </summary>
    [HttpPost("wikipedia-style")]
    public ActionResult<WikipediaStyleResult> AnalyzeWikipediaStyle([FromBody] WikipediaStyleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest("Topic is required");
        }

        _logger.LogInformation("[LLMCitation] Analyzing Wikipedia style for topic: {Topic}", request.Topic);
        var result = _citationService.AnalyzeWikipediaStyle(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.16 YouTube 引用优化策略
    /// 原理：YouTube 超越 Reddit 成为 #1 社交引用源（16% vs 10%）
    /// </summary>
    [HttpPost("youtube-strategy")]
    public ActionResult<YouTubeCitationResult> GenerateYouTubeStrategy([FromBody] YouTubeCitationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Industry))
        {
            return BadRequest("Industry is required");
        }

        _logger.LogInformation("[LLMCitation] Generating YouTube strategy for industry: {Industry}", request.Industry);
        var result = _citationService.GenerateYouTubeStrategy(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.17 AI 流量转化追踪配置
    /// 原理：AI 流量转化率 14.2% vs Google 2.8%
    /// </summary>
    [HttpPost("ai-traffic-conversion")]
    public ActionResult<AITrafficConversionResult> GenerateAITrafficConversionGuide([FromBody] AITrafficConversionRequest request)
    {
        _logger.LogInformation("[LLMCitation] Generating AI traffic conversion guide for: {Url}", request.WebsiteUrl);
        var result = _citationService.GenerateAITrafficConversionGuide(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.18 可影响域名策略
    /// 原理：74% 高引用域名可被营销影响
    /// </summary>
    [HttpPost("influenceable-domains")]
    public ActionResult<InfluenceableDomainResult> GenerateInfluenceableDomainStrategy([FromBody] InfluenceableDomainRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Industry))
        {
            return BadRequest("Industry is required");
        }

        _logger.LogInformation("[LLMCitation] Generating influenceable domain strategy for: {Industry}", request.Industry);
        var result = _citationService.GenerateInfluenceableDomainStrategy(request);
        return Ok(result);
    }

    #endregion

    #region Phase 7.19-7.26 API Endpoints

    /// <summary>
    /// 7.19 Query Fan-out 年份检测
    /// 原理：28.1% 子查询自动添加当前年份
    /// </summary>
    [HttpPost("query-year-detection")]
    public ActionResult<QueryYearDetectionResult> DetectQueryYearRelevance([FromBody] QueryYearDetectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query is required");
        }

        _logger.LogInformation("[LLMCitation] Detecting query year relevance for: {Query}", request.Query);
        var result = _citationService.DetectQueryYearRelevance(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.20 AutoGEO 内容重写建议
    /// 原理：CMU ICLR 2026 论文，自动提取 GE 偏好规则
    /// </summary>
    [HttpPost("autogeo-rewrite")]
    public ActionResult<AutoGEORewriteResult> GenerateAutoGEORewriteSuggestions([FromBody] AutoGEORewriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Content is required");
        }

        _logger.LogInformation("[LLMCitation] Generating AutoGEO rewrite suggestions");
        var result = _citationService.GenerateAutoGEORewriteSuggestions(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.21 平台特定引用策略
    /// 原理：Perplexity→Reddit 6.1x，Grok→Reddit 2.3x
    /// </summary>
    [HttpPost("platform-citation-strategy")]
    public ActionResult<PlatformCitationStrategyResult> GeneratePlatformCitationStrategy([FromBody] PlatformCitationStrategyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetPlatform))
        {
            return BadRequest("TargetPlatform is required");
        }

        _logger.LogInformation("[LLMCitation] Generating platform citation strategy for: {Platform}", request.TargetPlatform);
        var result = _citationService.GeneratePlatformCitationStrategy(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.22 LinkedIn B2B 引用优化
    /// 原理：LinkedIn 引用量接近 YouTube，B2B 专属优化
    /// </summary>
    [HttpPost("linkedin-b2b")]
    public ActionResult<LinkedInB2BResult> GenerateLinkedInB2BStrategy([FromBody] LinkedInB2BRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return BadRequest("CompanyName is required");
        }

        _logger.LogInformation("[LLMCitation] Generating LinkedIn B2B strategy for: {Company}", request.CompanyName);
        var result = _citationService.GenerateLinkedInB2BStrategy(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.24 llms.txt 模型定制
    /// 原理：Claude→证据页、ChatGPT→规范页、Perplexity→FAQ
    /// </summary>
    [HttpPost("llms-txt-model-custom")]
    public ActionResult<LlmsTxtModelCustomResult> GenerateModelCustomLlmsTxt([FromBody] LlmsTxtModelCustomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetModel))
        {
            return BadRequest("TargetModel is required");
        }

        _logger.LogInformation("[LLMCitation] Generating model-custom llms.txt for: {Model}", request.TargetModel);
        var result = _citationService.GenerateModelCustomLlmsTxt(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.25 引用表面积分析
    /// 原理：品牌提及 3x 比外链更预测 AI 可见度
    /// </summary>
    [HttpPost("citation-surface")]
    public ActionResult<CitationSurfaceResult> AnalyzeCitationSurface([FromBody] CitationSurfaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest("BrandName is required");
        }

        _logger.LogInformation("[LLMCitation] Analyzing citation surface for: {Brand}", request.BrandName);
        var result = _citationService.AnalyzeCitationSurface(request);
        return Ok(result);
    }

    /// <summary>
    /// 7.26 高权威平台快速索引
    /// 原理：LinkedIn(DR90+) 3 小时索引
    /// </summary>
    [HttpPost("rapid-index")]
    public ActionResult<RapidIndexResult> GenerateRapidIndexStrategy([FromBody] RapidIndexRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ContentUrl))
        {
            return BadRequest("ContentUrl is required");
        }

        _logger.LogInformation("[LLMCitation] Generating rapid index strategy for: {Url}", request.ContentUrl);
        var result = _citationService.GenerateRapidIndexStrategy(request);
        return Ok(result);
    }

    #endregion
}
