namespace GeoCore.SaaS.Services.SiteAudit;

#region Request Models

public class SiteAuditRequest
{
    public string Url { get; set; } = string.Empty;
    public bool IncludeTechnical { get; set; } = true;
    public bool IncludeContent { get; set; } = true;
    public bool IncludeEEAT { get; set; } = true;
}

public class IndexNowSubmitRequest
{
    public List<string> Urls { get; set; } = new();
    public string? ApiKey { get; set; }
}

public class SitemapGenerateRequest
{
    public List<string> Urls { get; set; } = new();
    public string BaseUrl { get; set; } = string.Empty;
    public string ChangeFreq { get; set; } = "weekly";
    public double Priority { get; set; } = 0.8;
}

public class RobotsTxtGenerateRequest
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool AllowAllAICrawlers { get; set; } = true;
    public List<string> DisallowPaths { get; set; } = new();
    public List<string> AllowPaths { get; set; } = new();
}

#endregion

#region Result Models

public class SiteAuditResult
{
    public string Url { get; set; } = string.Empty;
    public DateTime AuditTime { get; set; } = DateTime.UtcNow;
    public int OverallScore { get; set; }
    public string Grade { get; set; } = string.Empty; // A, B, C, D, F
    
    public TechnicalAuditResult Technical { get; set; } = new();
    public ContentAuditResult Content { get; set; } = new();
    public EEATAuditResult EEAT { get; set; } = new();
    
    public List<AuditIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class TechnicalAuditResult
{
    public int Score { get; set; }
    
    // 6.5 robots.txt AI 爬虫检测
    public RobotsTxtAudit RobotsTxt { get; set; } = new();
    
    // 6.6 Core Web Vitals
    public CoreWebVitalsAudit CoreWebVitals { get; set; } = new();
    
    // 6.7 JS 渲染检测
    public JSRenderingAudit JSRendering { get; set; } = new();
    
    // 6.8 HTTPS/Canonical
    public HttpsCanonicalAudit HttpsCanonical { get; set; } = new();
    
    // 6.9 Sitemap 检测
    public SitemapAudit Sitemap { get; set; } = new();
    
    // 6.10 llms.txt 检测
    public LlmsTxtAudit LlmsTxt { get; set; } = new();
}

public class RobotsTxtAudit
{
    public bool Exists { get; set; }
    public string? Content { get; set; }
    public List<AICrawlerAccess> CrawlerAccess { get; set; } = new();
    public bool AllAICrawlersAllowed { get; set; }
    public List<string> BlockedCrawlers { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}

public class AICrawlerAccess
{
    public string CrawlerName { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
    public string Platform { get; set; } = string.Empty; // ChatGPT, Claude, Perplexity, etc.
}

public class CoreWebVitalsAudit
{
    public bool Passed { get; set; }
    public double LCP { get; set; } // Largest Contentful Paint (seconds)
    public double INP { get; set; } // Interaction to Next Paint (ms)
    public double CLS { get; set; } // Cumulative Layout Shift
    public string LCPStatus { get; set; } = string.Empty; // good, needs-improvement, poor
    public string INPStatus { get; set; } = string.Empty;
    public string CLSStatus { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}

public class JSRenderingAudit
{
    public bool HasJSRenderedContent { get; set; }
    public bool CriticalContentInJS { get; set; }
    public double JSContentPercentage { get; set; }
    public List<string> JSRenderedElements { get; set; } = new();
    public List<string> Issues { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

public class HttpsCanonicalAudit
{
    public bool IsHttps { get; set; }
    public bool HasCanonical { get; set; }
    public string? CanonicalUrl { get; set; }
    public bool CanonicalMatchesUrl { get; set; }
    public bool HasHsts { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class SitemapAudit
{
    public bool Exists { get; set; }
    public string? SitemapUrl { get; set; }
    public bool IsValid { get; set; }
    public int UrlCount { get; set; }
    public DateTime? LastModified { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class LlmsTxtAudit
{
    public bool Exists { get; set; }
    public string? Content { get; set; }
    public bool IsValid { get; set; }
    public int EntryCount { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class ContentAuditResult
{
    public int Score { get; set; }
    
    // 6.11 答案胶囊检测
    public AnswerCapsuleAudit AnswerCapsule { get; set; } = new();
    
    // 6.12 段落长度分析
    public ParagraphLengthAudit ParagraphLength { get; set; } = new();
    
    // 6.13 标题层级检测
    public HeadingStructureAudit HeadingStructure { get; set; } = new();
    
    // 6.14 Schema 完整度检测
    public SchemaAudit Schema { get; set; } = new();
    
    // 6.15 Meta 标签检测
    public MetaTagsAudit MetaTags { get; set; } = new();
}

public class AnswerCapsuleAudit
{
    public bool HasAnswerCapsules { get; set; }
    public int CapsuleCount { get; set; }
    public List<AnswerCapsuleInfo> Capsules { get; set; } = new();
    public double CoveragePercentage { get; set; } // H2 后有答案胶囊的比例
    public List<string> Issues { get; set; } = new();
}

public class AnswerCapsuleInfo
{
    public string HeadingText { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public bool IsOptimalLength { get; set; } // 40-60 词
    public string FirstSentence { get; set; } = string.Empty;
}

public class ParagraphLengthAudit
{
    public int TotalParagraphs { get; set; }
    public int OptimalLengthCount { get; set; } // 120-180 词
    public int TooShortCount { get; set; } // < 50 词
    public int TooLongCount { get; set; } // > 250 词
    public double OptimalPercentage { get; set; }
    public double AverageLength { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class HeadingStructureAudit
{
    public bool HasH1 { get; set; }
    public int H1Count { get; set; }
    public int H2Count { get; set; }
    public int H3Count { get; set; }
    public int H4Count { get; set; }
    public bool HasProperHierarchy { get; set; }
    public List<HeadingInfo> Headings { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}

public class HeadingInfo
{
    public string Level { get; set; } = string.Empty; // H1, H2, etc.
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class SchemaAudit
{
    public bool HasSchema { get; set; }
    public List<SchemaInfo> Schemas { get; set; } = new();
    public bool HasArticleSchema { get; set; }
    public bool HasFAQSchema { get; set; }
    public bool HasOrganizationSchema { get; set; }
    public List<string> MissingProperties { get; set; } = new();
    public List<string> Issues { get; set; } = new();
}

public class SchemaInfo
{
    public string Type { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> Properties { get; set; } = new();
    public List<string> MissingRequired { get; set; } = new();
}

public class MetaTagsAudit
{
    public bool HasTitle { get; set; }
    public string? Title { get; set; }
    public int TitleLength { get; set; }
    public bool TitleOptimal { get; set; } // < 60 chars
    
    public bool HasDescription { get; set; }
    public string? Description { get; set; }
    public int DescriptionLength { get; set; }
    public bool DescriptionOptimal { get; set; } // < 160 chars
    
    public bool HasOgTags { get; set; }
    public bool HasTwitterCards { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class EEATAuditResult
{
    public int Score { get; set; }
    
    // 6.16 E-E-A-T 信号检测
    public bool HasAuthorInfo { get; set; }
    public string? AuthorName { get; set; }
    public bool HasAuthorBio { get; set; }
    public bool HasAuthorCredentials { get; set; }
    
    public bool HasPublishDate { get; set; }
    public DateTime? PublishDate { get; set; }
    public bool HasUpdateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    
    public bool HasCitations { get; set; }
    public int CitationCount { get; set; }
    public bool HasExternalLinks { get; set; }
    public int ExternalLinkCount { get; set; }
    
    public bool HasAboutPage { get; set; }
    public bool HasContactInfo { get; set; }
    public bool HasPrivacyPolicy { get; set; }
    
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class AuditIssue
{
    public string Category { get; set; } = string.Empty; // Technical, Content, EEAT
    public string Severity { get; set; } = string.Empty; // Critical, High, Medium, Low
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HowToFix { get; set; }
    public int ImpactScore { get; set; } // 1-10
}

#endregion

#region Quick Index Models

public class IndexNowResult
{
    public bool Success { get; set; }
    public int SubmittedCount { get; set; }
    public List<string> SubmittedUrls { get; set; } = new();
    public List<string> FailedUrls { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public List<string> SearchEngines { get; set; } = new(); // Bing, Yandex, Naver
}

public class SitemapGenerateResult
{
    public bool Success { get; set; }
    public string XmlContent { get; set; } = string.Empty;
    public int UrlCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RobotsTxtGenerateResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> AllowedCrawlers { get; set; } = new();
    public List<string> DisallowedPaths { get; set; } = new();
}

public class LlmsTxtGenerateResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

#region Audit History Models

public class AuditHistoryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Url { get; set; } = string.Empty;
    public DateTime AuditTime { get; set; }
    public int OverallScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int TechnicalScore { get; set; }
    public int ContentScore { get; set; }
    public int EEATScore { get; set; }
    public int IssueCount { get; set; }
    public int CriticalIssueCount { get; set; }
}

public class AuditComparisonResult
{
    public AuditHistoryEntry Previous { get; set; } = new();
    public AuditHistoryEntry Current { get; set; } = new();
    public int ScoreChange { get; set; }
    public string Trend { get; set; } = string.Empty; // improved, declined, stable
    public List<string> Improvements { get; set; } = new();
    public List<string> Regressions { get; set; } = new();
}

#endregion
