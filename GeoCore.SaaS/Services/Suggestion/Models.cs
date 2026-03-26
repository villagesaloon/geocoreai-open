namespace GeoCore.SaaS.Services.Suggestion;

/// <summary>
/// 建议规则定义
/// </summary>
public class SuggestionRule
{
    public string RuleId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string Condition { get; set; } = "";
    public string Priority { get; set; } = "medium";
    public int ImpactScore { get; set; }
    public string EffortLevel { get; set; } = "medium";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> ActionItems { get; set; } = new();
    public string? ExampleCode { get; set; }
    public List<string> ReferenceUrls { get; set; } = new();
}

/// <summary>
/// 生成的建议
/// </summary>
public class DetectionSuggestion
{
    public string RuleId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string Priority { get; set; } = "medium";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int ImpactScore { get; set; }
    public string EffortLevel { get; set; } = "medium";
    public List<string> ActionItems { get; set; } = new();
    public string? ExampleCode { get; set; }
    public List<string> ReferenceUrls { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 检测上下文 - 包含所有检测结果数据
/// </summary>
public class DetectionContext
{
    // AI 可见度数据
    public decimal BrandMentionRate { get; set; }
    public decimal AvgMentionPosition { get; set; }
    public bool UserSiteCited { get; set; }
    public int CitationCount { get; set; }
    public bool HasFaqPage { get; set; }

    // 网站审计数据
    public WebsiteAuditContext? WebsiteAudit { get; set; }

    // 品牌信息
    public string BrandName { get; set; } = "";
    public string WebsiteUrl { get; set; } = "";
}

/// <summary>
/// 网站审计上下文
/// </summary>
public class WebsiteAuditContext
{
    // AI 爬虫配置
    public bool AiCrawlersAllowed { get; set; }
    public List<string> BlockedCrawlers { get; set; } = new();
    public bool LlmsTxtExists { get; set; }
    public bool SitemapExists { get; set; }
    public int SitemapUrlCount { get; set; }

    // 页面技术
    public bool HttpsEnabled { get; set; }
    public bool HasCanonical { get; set; }
    public bool JsRenderingIssue { get; set; }

    // 结构化数据
    public bool HasSchema { get; set; }
    public bool HasFaqSchema { get; set; }
    public List<string> SchemaTypes { get; set; } = new();

    // E-E-A-T
    public bool HasAuthorInfo { get; set; }
    public bool HasPublishDate { get; set; }
    public bool HasCitations { get; set; }
    public int ExternalLinkCount { get; set; }

    // 内容结构
    public bool HeadingStructureOk { get; set; }
    public int H1Count { get; set; }
    public int H2Count { get; set; }
    public bool MetaTitleOk { get; set; }
    public bool HasMetaDescription { get; set; }
    public bool HasAnswerCapsules { get; set; }
    public bool HasOgTags { get; set; }

    // 分数
    public int TechnicalScore { get; set; }
    public int ContentScore { get; set; }
    public int EeatScore { get; set; }
    public int OverallScore { get; set; }
    public string Grade { get; set; } = "F";
}

/// <summary>
/// 建议分类
/// </summary>
public static class SuggestionCategory
{
    public const string AiVisibility = "ai_visibility";
    public const string WebsiteTech = "website_tech";
    public const string ContentQuality = "content_quality";
    public const string Seo = "seo";
}

/// <summary>
/// 建议子分类
/// </summary>
public static class SuggestionSubcategory
{
    // AI 可见度
    public const string BrandMention = "brand_mention";
    public const string CitationSource = "citation_source";
    public const string QaContent = "qa_content";

    // 网站技术
    public const string AiCrawler = "ai_crawler";
    public const string PageTech = "page_tech";
    public const string StructuredData = "structured_data";

    // 内容质量
    public const string Eeat = "eeat";
    public const string ContentStructure = "content_structure";
    public const string AnswerCapsule = "answer_capsule";

    // SEO
    public const string Keyword = "keyword";
    public const string Backlink = "backlink";
    public const string PageOptimization = "page_optimization";
}

/// <summary>
/// 优先级
/// </summary>
public static class SuggestionPriority
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";

    public static int GetOrder(string priority) => priority switch
    {
        High => 3,
        Medium => 2,
        Low => 1,
        _ => 0
    };
}

/// <summary>
/// 实施难度
/// </summary>
public static class EffortLevel
{
    public const string Easy = "easy";
    public const string Medium = "medium";
    public const string Hard = "hard";
}
