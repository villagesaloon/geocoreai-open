using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentQuality;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 内容质量评估控制器
/// 基于 MIT GEO Paper 和 Dejan AI 研究的可提取性评估
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentQualityController : ControllerBase
{
    private readonly ILogger<ContentQualityController> _logger;
    private readonly ContentQualityAnalyzer _analyzer;

    public ContentQualityController(
        ILogger<ContentQualityController> logger,
        ContentQualityAnalyzer analyzer)
    {
        _logger = logger;
        _analyzer = analyzer;
    }

    /// <summary>
    /// 分析单个内容的质量
    /// </summary>
    /// <param name="request">分析请求</param>
    /// <returns>质量评估结果</returns>
    [HttpPost("analyze")]
    public ActionResult<ContentQualityResult> Analyze([FromBody] ContentQualityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Content is required" });
        }

        _logger.LogInformation("[ContentQuality] 开始分析内容质量, 语言={Language}, 内容长度={Length}",
            request.Language, request.Content.Length);

        var result = _analyzer.Analyze(request.Content, request.Language);

        _logger.LogInformation("[ContentQuality] 分析完成: Score={Score}, Grade={Grade}, Claims={Claims}, Entities={Entities}",
            result.ExtractabilityScore, result.Grade, 
            result.ClaimDensity.Claims.Count, 
            result.EntityDensity.Entities.Count);

        return Ok(result);
    }

    /// <summary>
    /// 批量分析多个问答对的质量
    /// </summary>
    /// <param name="request">批量分析请求</param>
    /// <returns>批量评估结果</returns>
    [HttpPost("batch")]
    public ActionResult<BatchContentQualityResult> AnalyzeBatch([FromBody] BatchContentQualityRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "Items are required" });
        }

        _logger.LogInformation("[ContentQuality] 开始批量分析, 数量={Count}, 语言={Language}",
            request.Items.Count, request.Language);

        var result = _analyzer.AnalyzeBatch(request.Items, request.Language);

        _logger.LogInformation("[ContentQuality] 批量分析完成: AvgScore={Score}, Grade={Grade}",
            result.AverageScore, result.OverallGrade);

        return Ok(result);
    }

    /// <summary>
    /// 获取评估指标说明
    /// </summary>
    [HttpGet("metrics")]
    public ActionResult GetMetricsInfo()
    {
        var metrics = new
        {
            claimDensity = new
            {
                name = "事实密度 (Claim Density)",
                description = "每100词中可提取的事实、统计数据、测量值的数量",
                target = "4+ claims/100词",
                weight = "30%",
                source = "MIT GEO Paper (2024)"
            },
            informationDensity = new
            {
                name = "信息密度 (Information Density)",
                description = "内容长度与 AI 覆盖率的关系",
                target = "800-1500 词最佳",
                weight = "20%",
                source = "Dejan AI Research (2025)"
            },
            frontloading = new
            {
                name = "答案前置 (Answer Frontloading)",
                description = "关键信息在内容中出现的位置",
                target = "前100词至少3个claims",
                weight = "25%",
                source = "GEO Analyzer"
            },
            sentenceLength = new
            {
                name = "句子长度 (Sentence Length)",
                description = "句子的平均词数，匹配 Google 提取块大小",
                target = "15-20 词/句",
                weight = "15%",
                source = "Dejan AI Research (2025)"
            },
            entityDensity = new
            {
                name = "实体密度 (Entity Density)",
                description = "可被 AI 识别和引用的命名实体数量",
                target = "2+ entities/100词",
                weight = "10%",
                source = "GEO Analyzer"
            }
        };

        var grades = new[]
        {
            new { score = "8-10", grade = "优秀", description = "高度可引用" },
            new { score = "6-7.9", grade = "良好", description = "可引用" },
            new { score = "4-5.9", grade = "一般", description = "需要优化" },
            new { score = "< 4", grade = "差", description = "难以被引用" }
        };

        return Ok(new { metrics, grades });
    }
}

/// <summary>
/// GEO 高级分析控制器 (7.27-7.31)
/// Phase 7 高优先级功能
/// </summary>
[ApiController]
[Route("api/geo-advanced")]
public class GEOAdvancedController : ControllerBase
{
    private readonly ILogger<GEOAdvancedController> _logger;
    private readonly GEOAdvancedAnalyzer _analyzer;

    public GEOAdvancedController(
        ILogger<GEOAdvancedController> logger,
        GEOAdvancedAnalyzer analyzer)
    {
        _logger = logger;
        _analyzer = analyzer;
    }

    /// <summary>
    /// 7.27 Listicle 架构审计
    /// 检测内容是否符合 "Top N" 结构（74.2% AI 引用来自此结构）
    /// </summary>
    [HttpPost("listicle-audit")]
    public ActionResult<ListicleArchitectureAudit> AuditListicleArchitecture([FromBody] ListicleAuditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        _logger.LogInformation("[GEOAdvanced] Listicle 架构审计, 内容长度={Length}", request.Content.Length);

        var result = _analyzer.AuditListicleArchitecture(request.Content, request.Title, request.Language ?? "zh");
        return Ok(result);
    }

    /// <summary>
    /// 7.28 生成 Triple JSON-LD Stack
    /// Article + ItemList + FAQPage 三层 Schema = 1.8x 引用
    /// </summary>
    [HttpPost("triple-jsonld")]
    public ActionResult<TripleJsonLdResult> GenerateTripleJsonLd([FromBody] TripleJsonLdRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title is required" });

        _logger.LogInformation("[GEOAdvanced] 生成 Triple JSON-LD: {Title}", request.Title);

        var result = _analyzer.GenerateTripleJsonLd(
            request.Title,
            request.Description ?? "",
            request.Url ?? "",
            request.AuthorName ?? "Unknown",
            request.PublishDate ?? DateTime.UtcNow,
            request.ListItems ?? new List<string>(),
            request.Faqs ?? new List<(string, string)>(),
            request.ImageUrl
        );
        return Ok(result);
    }

    /// <summary>
    /// 7.29 ChatGPT 无源响应过滤
    /// 53.6% ChatGPT 响应无 web 源，从指标中排除
    /// </summary>
    [HttpPost("filter-sourceless")]
    public ActionResult<SourcelessResponseFilter> FilterSourcelessResponses([FromBody] FilterSourcelessRequest request)
    {
        if (request.Responses == null || !request.Responses.Any())
            return BadRequest(new { error = "Responses are required" });

        _logger.LogInformation("[GEOAdvanced] 过滤无源响应, 数量={Count}", request.Responses.Count);

        var result = _analyzer.FilterSourcelessResponses(request.Responses);
        return Ok(result);
    }

    /// <summary>
    /// 7.30 AI Overview 7 因素评分
    /// 语义完整性、多模态、事实验证等 7 因素量化评分
    /// </summary>
    [HttpPost("ai-overview-score")]
    public ActionResult<AIOverviewScoreResult> CalculateAIOverviewScore([FromBody] AIOverviewScoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        _logger.LogInformation("[GEOAdvanced] AI Overview 评分, 内容长度={Length}", request.Content.Length);

        var result = _analyzer.CalculateAIOverviewScore(request.Content, request.Title, request.Language ?? "zh");
        return Ok(result);
    }

    /// <summary>
    /// 7.31 最优段落长度检测
    /// 127-156 词是最优 AI 提取长度
    /// </summary>
    [HttpPost("paragraph-length")]
    public ActionResult<ParagraphLengthAnalysis> AnalyzeParagraphLengths([FromBody] ParagraphLengthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        _logger.LogInformation("[GEOAdvanced] 段落长度分析, 内容长度={Length}", request.Content.Length);

        var result = _analyzer.AnalyzeParagraphLengths(request.Content, request.Language ?? "zh");
        return Ok(result);
    }

    /// <summary>
    /// 综合分析 - 一次调用所有 7.27-7.31 功能
    /// </summary>
    [HttpPost("comprehensive")]
    public ActionResult ComprehensiveAnalysis([FromBody] ComprehensiveAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required" });

        _logger.LogInformation("[GEOAdvanced] 综合分析, 内容长度={Length}", request.Content.Length);

        var language = request.Language ?? "zh";

        var listicleAudit = _analyzer.AuditListicleArchitecture(request.Content, request.Title, language);
        var aiOverviewScore = _analyzer.CalculateAIOverviewScore(request.Content, request.Title, language);
        var paragraphAnalysis = _analyzer.AnalyzeParagraphLengths(request.Content, language);

        return Ok(new
        {
            listicleAudit,
            aiOverviewScore,
            paragraphAnalysis,
            overallScore = Math.Round((listicleAudit.ListicleScore + aiOverviewScore.TotalScore + paragraphAnalysis.Score) / 3, 1),
            summary = new
            {
                isListicleOptimized = listicleAudit.ListicleScore >= 6,
                isAIOverviewReady = aiOverviewScore.TotalScore >= 6,
                hasParagraphOptimization = paragraphAnalysis.OptimalRate >= 0.3,
                topIssues = listicleAudit.Issues.Take(2)
                    .Concat(aiOverviewScore.Suggestions.Take(2))
                    .Concat(paragraphAnalysis.Suggestions.Take(2))
                    .ToList()
            }
        });
    }
}

#region Request Models

public class ListicleAuditRequest
{
    public string Content { get; set; } = "";
    public string? Title { get; set; }
    public string? Language { get; set; }
}

public class TripleJsonLdRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Url { get; set; }
    public string? AuthorName { get; set; }
    public DateTime? PublishDate { get; set; }
    public List<string>? ListItems { get; set; }
    public List<(string question, string answer)>? Faqs { get; set; }
    public string? ImageUrl { get; set; }
}

public class FilterSourcelessRequest
{
    public List<AIResponse> Responses { get; set; } = new();
}

public class AIOverviewScoreRequest
{
    public string Content { get; set; } = "";
    public string? Title { get; set; }
    public string? Language { get; set; }
}

public class ParagraphLengthRequest
{
    public string Content { get; set; } = "";
    public string? Language { get; set; }
}

public class ComprehensiveAnalysisRequest
{
    public string Content { get; set; } = "";
    public string? Title { get; set; }
    public string? Language { get; set; }
}

#endregion
