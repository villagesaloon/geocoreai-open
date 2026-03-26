using System;
using SqlSugar;

namespace GeoCore.Data.Entities;

#region 检测任务表

/// <summary>
/// GEO 检测任务表
/// 记录每次检测任务的完整信息
/// </summary>
[SugarTable("geo_detection_tasks")]
public class GeoDetectionTaskEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    // 关联信息
    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    // 任务类型和状态
    [SugarColumn(ColumnName = "task_type", Length = 20, IsNullable = false)]
    public string TaskType { get; set; } = "full"; // full/quick/scheduled/website_only

    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "pending"; // pending/queued/running/completed/failed/cancelled

    [SugarColumn(ColumnName = "current_phase", Length = 50, IsNullable = true)]
    public string? CurrentPhase { get; set; } // data_prep/question_gen/ai_detection/website_audit/analysis

    [SugarColumn(ColumnName = "progress", IsNullable = false)]
    public int Progress { get; set; } = 0; // 0-100

    [SugarColumn(ColumnName = "message", ColumnDataType = "text", IsNullable = true)]
    public string? Message { get; set; }

    [SugarColumn(ColumnName = "error_message", ColumnDataType = "text", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    // 队列信息
    [SugarColumn(ColumnName = "queue_name", Length = 100, IsNullable = true)]
    public string? QueueName { get; set; }

    [SugarColumn(ColumnName = "queue_position", IsNullable = true)]
    public int? QueuePosition { get; set; }

    // Phase 1: 数据准备结果
    [SugarColumn(ColumnName = "selling_points_count", IsNullable = false)]
    public int SellingPointsCount { get; set; } = 0;

    [SugarColumn(ColumnName = "personas_count", IsNullable = false)]
    public int PersonasCount { get; set; } = 0;

    [SugarColumn(ColumnName = "stages_count", IsNullable = false)]
    public int StagesCount { get; set; } = 0;

    // Phase 2: 问题生成结果
    [SugarColumn(ColumnName = "questions_count", IsNullable = false)]
    public int QuestionsCount { get; set; } = 0;

    [SugarColumn(ColumnName = "questions_validated", IsNullable = false)]
    public int QuestionsValidated { get; set; } = 0;

    // Phase 3: AI 检测结果
    [SugarColumn(ColumnName = "models_tested", ColumnDataType = "json", IsNullable = true)]
    public string? ModelsTested { get; set; } // JSON array ["gpt","claude",...]

    [SugarColumn(ColumnName = "brand_mention_rate", IsNullable = true)]
    public decimal? BrandMentionRate { get; set; } // 0.0000-1.0000

    [SugarColumn(ColumnName = "avg_mention_position", IsNullable = true)]
    public decimal? AvgMentionPosition { get; set; }

    [SugarColumn(ColumnName = "citation_count", IsNullable = false)]
    public int CitationCount { get; set; } = 0;

    [SugarColumn(ColumnName = "user_site_cited", IsNullable = false)]
    public bool UserSiteCited { get; set; } = false;

    // Phase 4: 网站审计结果
    [SugarColumn(ColumnName = "website_audit_skipped", IsNullable = false)]
    public bool WebsiteAuditSkipped { get; set; } = false;

    [SugarColumn(ColumnName = "website_audit_cached", IsNullable = false)]
    public bool WebsiteAuditCached { get; set; } = false;

    [SugarColumn(ColumnName = "website_overall_score", IsNullable = true)]
    public int? WebsiteOverallScore { get; set; }

    [SugarColumn(ColumnName = "website_technical_score", IsNullable = true)]
    public int? WebsiteTechnicalScore { get; set; }

    [SugarColumn(ColumnName = "website_content_score", IsNullable = true)]
    public int? WebsiteContentScore { get; set; }

    [SugarColumn(ColumnName = "website_eeat_score", IsNullable = true)]
    public int? WebsiteEeatScore { get; set; }

    // Phase 5: 综合结果
    [SugarColumn(ColumnName = "visibility_score", IsNullable = true)]
    public decimal? VisibilityScore { get; set; }

    [SugarColumn(ColumnName = "website_health_score", IsNullable = true)]
    public decimal? WebsiteHealthScore { get; set; }

    [SugarColumn(ColumnName = "issues_count", IsNullable = false)]
    public int IssuesCount { get; set; } = 0;

    [SugarColumn(ColumnName = "recommendations_count", IsNullable = false)]
    public int RecommendationsCount { get; set; } = 0;

    [SugarColumn(ColumnName = "result_summary", ColumnDataType = "json", IsNullable = true)]
    public string? ResultSummary { get; set; }

    // 时间戳
    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "queued_at", IsNullable = true)]
    public DateTime? QueuedAt { get; set; }

    [SugarColumn(ColumnName = "started_at", IsNullable = true)]
    public DateTime? StartedAt { get; set; }

    [SugarColumn(ColumnName = "completed_at", IsNullable = true)]
    public DateTime? CompletedAt { get; set; }
}

#endregion

#region 网站审计表

/// <summary>
/// 网站审计表
/// 记录网站爬取和审计结果
/// </summary>
[SugarTable("geo_website_audits")]
public class GeoWebsiteAuditEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    // 关联信息
    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "task_id", IsNullable = true)]
    public long? TaskId { get; set; }

    [SugarColumn(ColumnName = "url", Length = 500, IsNullable = false)]
    public string Url { get; set; } = string.Empty;

    // 整体评分
    [SugarColumn(ColumnName = "overall_score", IsNullable = false)]
    public int OverallScore { get; set; } = 0;

    [SugarColumn(ColumnName = "grade", Length = 5, IsNullable = true)]
    public string? Grade { get; set; } // A/B/C/D/F

    // 技术审计
    [SugarColumn(ColumnName = "technical_score", IsNullable = false)]
    public int TechnicalScore { get; set; } = 0;

    [SugarColumn(ColumnName = "robots_txt_exists", IsNullable = false)]
    public bool RobotsTxtExists { get; set; } = false;

    [SugarColumn(ColumnName = "robots_txt_content", ColumnDataType = "text", IsNullable = true)]
    public string? RobotsTxtContent { get; set; }

    [SugarColumn(ColumnName = "ai_crawlers_allowed", IsNullable = false)]
    public bool AiCrawlersAllowed { get; set; } = false;

    [SugarColumn(ColumnName = "blocked_crawlers", ColumnDataType = "json", IsNullable = true)]
    public string? BlockedCrawlers { get; set; }

    [SugarColumn(ColumnName = "sitemap_exists", IsNullable = false)]
    public bool SitemapExists { get; set; } = false;

    [SugarColumn(ColumnName = "sitemap_url_count", IsNullable = false)]
    public int SitemapUrlCount { get; set; } = 0;

    [SugarColumn(ColumnName = "llms_txt_exists", IsNullable = false)]
    public bool LlmsTxtExists { get; set; } = false;

    [SugarColumn(ColumnName = "llms_txt_entry_count", IsNullable = false)]
    public int LlmsTxtEntryCount { get; set; } = 0;

    [SugarColumn(ColumnName = "https_enabled", IsNullable = false)]
    public bool HttpsEnabled { get; set; } = false;

    [SugarColumn(ColumnName = "has_canonical", IsNullable = false)]
    public bool HasCanonical { get; set; } = false;

    [SugarColumn(ColumnName = "js_rendering_issue", IsNullable = false)]
    public bool JsRenderingIssue { get; set; } = false;

    [SugarColumn(ColumnName = "core_web_vitals", ColumnDataType = "json", IsNullable = true)]
    public string? CoreWebVitals { get; set; }

    // 内容审计
    [SugarColumn(ColumnName = "content_score", IsNullable = false)]
    public int ContentScore { get; set; } = 0;

    [SugarColumn(ColumnName = "has_schema", IsNullable = false)]
    public bool HasSchema { get; set; } = false;

    [SugarColumn(ColumnName = "schema_types", ColumnDataType = "json", IsNullable = true)]
    public string? SchemaTypes { get; set; }

    [SugarColumn(ColumnName = "has_article_schema", IsNullable = false)]
    public bool HasArticleSchema { get; set; } = false;

    [SugarColumn(ColumnName = "has_faq_schema", IsNullable = false)]
    public bool HasFaqSchema { get; set; } = false;

    [SugarColumn(ColumnName = "has_answer_capsules", IsNullable = false)]
    public bool HasAnswerCapsules { get; set; } = false;

    [SugarColumn(ColumnName = "answer_capsule_coverage", IsNullable = true)]
    public decimal? AnswerCapsuleCoverage { get; set; }

    [SugarColumn(ColumnName = "heading_structure_ok", IsNullable = false)]
    public bool HeadingStructureOk { get; set; } = false;

    [SugarColumn(ColumnName = "h1_count", IsNullable = false)]
    public int H1Count { get; set; } = 0;

    [SugarColumn(ColumnName = "h2_count", IsNullable = false)]
    public int H2Count { get; set; } = 0;

    [SugarColumn(ColumnName = "meta_title_ok", IsNullable = false)]
    public bool MetaTitleOk { get; set; } = false;

    [SugarColumn(ColumnName = "meta_description_ok", IsNullable = false)]
    public bool MetaDescriptionOk { get; set; } = false;

    [SugarColumn(ColumnName = "has_og_tags", IsNullable = false)]
    public bool HasOgTags { get; set; } = false;

    // E-E-A-T 审计
    [SugarColumn(ColumnName = "eeat_score", IsNullable = false)]
    public int EeatScore { get; set; } = 0;

    [SugarColumn(ColumnName = "has_author_info", IsNullable = false)]
    public bool HasAuthorInfo { get; set; } = false;

    [SugarColumn(ColumnName = "has_publish_date", IsNullable = false)]
    public bool HasPublishDate { get; set; } = false;

    [SugarColumn(ColumnName = "has_update_date", IsNullable = false)]
    public bool HasUpdateDate { get; set; } = false;

    [SugarColumn(ColumnName = "has_citations", IsNullable = false)]
    public bool HasCitations { get; set; } = false;

    [SugarColumn(ColumnName = "external_link_count", IsNullable = false)]
    public int ExternalLinkCount { get; set; } = 0;

    // 问题和建议
    [SugarColumn(ColumnName = "issues", ColumnDataType = "json", IsNullable = true)]
    public string? Issues { get; set; }

    [SugarColumn(ColumnName = "recommendations", ColumnDataType = "json", IsNullable = true)]
    public string? Recommendations { get; set; }

    // 爬虫详情
    [SugarColumn(ColumnName = "pages_crawled", IsNullable = false)]
    public int PagesCrawled { get; set; } = 1;

    [SugarColumn(ColumnName = "crawl_depth", IsNullable = false)]
    public int CrawlDepth { get; set; } = 1;

    [SugarColumn(ColumnName = "crawl_duration_ms", IsNullable = true)]
    public int? CrawlDurationMs { get; set; }

    [SugarColumn(ColumnName = "pages_detail", ColumnDataType = "json", IsNullable = true)]
    public string? PagesDetail { get; set; }

    // 缓存控制
    [SugarColumn(ColumnName = "cache_expires_at", IsNullable = true)]
    public DateTime? CacheExpiresAt { get; set; }

    // 时间戳
    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 检测指标表

/// <summary>
/// 检测指标表（分国家）
/// 记录每次检测的分国家/分模型指标
/// </summary>
[SugarTable("geo_detection_metrics")]
public class GeoDetectionMetricEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    // 关联信息
    [SugarColumn(ColumnName = "task_id", IsNullable = false)]
    public long TaskId { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "country_code", Length = 5, IsNullable = false)]
    public string CountryCode { get; set; } = "ALL"; // US/CN/GB/ALL(全球汇总)

    [SugarColumn(ColumnName = "language", Length = 20, IsNullable = false)]
    public string Language { get; set; } = "zh-CN"; // 语言代码

    // AI 可见度指标
    [SugarColumn(ColumnName = "visibility_score", IsNullable = false)]
    public int VisibilityScore { get; set; } = 0;

    [SugarColumn(ColumnName = "mention_count", IsNullable = false)]
    public int MentionCount { get; set; } = 0;

    [SugarColumn(ColumnName = "citation_page_count", IsNullable = false)]
    public int CitationPageCount { get; set; } = 0;

    [SugarColumn(ColumnName = "brand_mention_rate", IsNullable = true)]
    public decimal? BrandMentionRate { get; set; }

    [SugarColumn(ColumnName = "avg_mention_position", IsNullable = true)]
    public decimal? AvgMentionPosition { get; set; }

    // 情感分析
    [SugarColumn(ColumnName = "sentiment_positive", IsNullable = true)]
    public decimal? SentimentPositive { get; set; }

    [SugarColumn(ColumnName = "sentiment_neutral", IsNullable = true)]
    public decimal? SentimentNeutral { get; set; }

    [SugarColumn(ColumnName = "sentiment_negative", IsNullable = true)]
    public decimal? SentimentNegative { get; set; }

    // ChatGPT
    [SugarColumn(ColumnName = "chatgpt_mentions", IsNullable = false)]
    public int ChatgptMentions { get; set; } = 0;

    [SugarColumn(ColumnName = "chatgpt_citations", IsNullable = false)]
    public int ChatgptCitations { get; set; } = 0;

    [SugarColumn(ColumnName = "chatgpt_visibility", IsNullable = true)]
    public int? ChatgptVisibility { get; set; }

    // Claude
    [SugarColumn(ColumnName = "claude_mentions", IsNullable = false)]
    public int ClaudeMentions { get; set; } = 0;

    [SugarColumn(ColumnName = "claude_citations", IsNullable = false)]
    public int ClaudeCitations { get; set; } = 0;

    [SugarColumn(ColumnName = "claude_visibility", IsNullable = true)]
    public int? ClaudeVisibility { get; set; }

    // Gemini
    [SugarColumn(ColumnName = "gemini_mentions", IsNullable = false)]
    public int GeminiMentions { get; set; } = 0;

    [SugarColumn(ColumnName = "gemini_citations", IsNullable = false)]
    public int GeminiCitations { get; set; } = 0;

    [SugarColumn(ColumnName = "gemini_visibility", IsNullable = true)]
    public int? GeminiVisibility { get; set; }

    // Perplexity
    [SugarColumn(ColumnName = "perplexity_mentions", IsNullable = false)]
    public int PerplexityMentions { get; set; } = 0;

    [SugarColumn(ColumnName = "perplexity_citations", IsNullable = false)]
    public int PerplexityCitations { get; set; } = 0;

    [SugarColumn(ColumnName = "perplexity_visibility", IsNullable = true)]
    public int? PerplexityVisibility { get; set; }

    // Grok
    [SugarColumn(ColumnName = "grok_mentions", IsNullable = false)]
    public int GrokMentions { get; set; } = 0;

    [SugarColumn(ColumnName = "grok_citations", IsNullable = false)]
    public int GrokCitations { get; set; } = 0;

    [SugarColumn(ColumnName = "grok_visibility", IsNullable = true)]
    public int? GrokVisibility { get; set; }

    // SEO 指标（可选）
    [SugarColumn(ColumnName = "authority_score", IsNullable = true)]
    public int? AuthorityScore { get; set; }

    [SugarColumn(ColumnName = "organic_traffic", IsNullable = true)]
    public int? OrganicTraffic { get; set; }

    [SugarColumn(ColumnName = "traffic_change_percent", IsNullable = true)]
    public decimal? TrafficChangePercent { get; set; }

    // 时间戳
    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 优化建议表

/// <summary>
/// 优化建议表
/// 记录检测生成的优化建议
/// </summary>
[SugarTable("geo_detection_suggestions")]
public class GeoDetectionSuggestionEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    // 关联信息
    [SugarColumn(ColumnName = "task_id", IsNullable = false)]
    public long TaskId { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    // 建议分类
    [SugarColumn(ColumnName = "category", Length = 50, IsNullable = false)]
    public string Category { get; set; } = string.Empty; // ai_visibility/website_tech/content_quality/seo

    [SugarColumn(ColumnName = "subcategory", Length = 50, IsNullable = true)]
    public string? Subcategory { get; set; } // brand_mention/citation_source/robots_txt等

    // 建议内容
    [SugarColumn(ColumnName = "priority", Length = 10, IsNullable = false)]
    public string Priority { get; set; } = "medium"; // high/medium/low

    [SugarColumn(ColumnName = "title", Length = 200, IsNullable = false)]
    public string Title { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", ColumnDataType = "text", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "impact_score", IsNullable = true)]
    public int? ImpactScore { get; set; } // 1-10

    [SugarColumn(ColumnName = "effort_level", Length = 10, IsNullable = true)]
    public string? EffortLevel { get; set; } // easy/medium/hard

    // 具体行动
    [SugarColumn(ColumnName = "action_items", ColumnDataType = "json", IsNullable = true)]
    public string? ActionItems { get; set; }

    [SugarColumn(ColumnName = "example_code", ColumnDataType = "text", IsNullable = true)]
    public string? ExampleCode { get; set; }

    [SugarColumn(ColumnName = "reference_urls", ColumnDataType = "json", IsNullable = true)]
    public string? ReferenceUrls { get; set; }

    // 状态跟踪
    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "pending"; // pending/in_progress/completed/dismissed

    [SugarColumn(ColumnName = "completed_at", IsNullable = true)]
    public DateTime? CompletedAt { get; set; }

    // 时间戳
    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 通知设置表

/// <summary>
/// 通知设置表
/// 记录用户的通知偏好
/// </summary>
[SugarTable("geo_notification_settings")]
public class GeoNotificationSettingEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    // 关联信息
    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    // 邮件通知设置
    [SugarColumn(ColumnName = "email_on_detection_complete", IsNullable = false)]
    public bool EmailOnDetectionComplete { get; set; } = true;

    [SugarColumn(ColumnName = "email_on_detection_failed", IsNullable = false)]
    public bool EmailOnDetectionFailed { get; set; } = true;

    [SugarColumn(ColumnName = "email_on_weekly_report", IsNullable = false)]
    public bool EmailOnWeeklyReport { get; set; } = false;

    [SugarColumn(ColumnName = "email_on_visibility_change", IsNullable = false)]
    public bool EmailOnVisibilityChange { get; set; } = false;

    // 通知阈值
    [SugarColumn(ColumnName = "visibility_change_threshold", IsNullable = false)]
    public int VisibilityChangeThreshold { get; set; } = 10;

    // 时间戳
    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region 邮件模板表

/// <summary>
/// 邮件模板表
/// 后台可编辑的邮件模板
/// </summary>
[SugarTable("geo_email_templates")]
public class GeoEmailTemplateEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 模板代码（唯一标识）
    /// detection_completed, detection_failed, visibility_alert, weekly_report, welcome
    /// </summary>
    [SugarColumn(ColumnName = "template_code", Length = 50, IsNullable = false)]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称（显示用）
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 邮件主题（支持 Scriban 模板变量）
    /// </summary>
    [SugarColumn(ColumnName = "subject", Length = 200, IsNullable = false)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML 内容（支持 Scriban 模板变量）
    /// </summary>
    [SugarColumn(ColumnName = "body_html", ColumnDataType = "mediumtext", IsNullable = false)]
    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>
    /// 纯文本内容（备用）
    /// </summary>
    [SugarColumn(ColumnName = "body_text", ColumnDataType = "text", IsNullable = true)]
    public string? BodyText { get; set; }

    /// <summary>
    /// 可用变量说明（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "variables", ColumnDataType = "json", IsNullable = true)]
    public string? Variables { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_active", IsNullable = false)]
    public bool IsActive { get; set; } = true;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region 通知任务表

/// <summary>
/// 通知任务表
/// 异步通知队列
/// </summary>
[SugarTable("geo_notification_tasks")]
public class GeoNotificationTaskEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 模板代码
    /// </summary>
    [SugarColumn(ColumnName = "template_code", Length = 50, IsNullable = false)]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 收件人邮箱
    /// </summary>
    [SugarColumn(ColumnName = "recipient_email", Length = 255, IsNullable = false)]
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// 模板变量值（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "variables", ColumnDataType = "json", IsNullable = true)]
    public string? Variables { get; set; }

    /// <summary>
    /// 状态：pending/processing/sent/failed
    /// </summary>
    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnName = "error_message", ColumnDataType = "text", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    [SugarColumn(ColumnName = "retry_count", IsNullable = false)]
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Resend 返回的邮件 ID
    /// </summary>
    [SugarColumn(ColumnName = "resend_id", Length = 100, IsNullable = true)]
    public string? ResendId { get; set; }

    [SugarColumn(ColumnName = "sent_at", IsNullable = true)]
    public DateTime? SentAt { get; set; }

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 邮件发送日志表

/// <summary>
/// 邮件发送日志表
/// 审计和追踪
/// </summary>
[SugarTable("geo_email_send_logs")]
public class GeoEmailSendLogEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "notification_task_id", IsNullable = true)]
    public long? NotificationTaskId { get; set; }

    [SugarColumn(ColumnName = "resend_id", Length = 100, IsNullable = true)]
    public string? ResendId { get; set; }

    [SugarColumn(ColumnName = "recipient_email", Length = 255, IsNullable = false)]
    public string RecipientEmail { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "subject", Length = 200, IsNullable = false)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 状态：sent/failed/bounced/complained
    /// </summary>
    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Resend API 响应（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "response", ColumnDataType = "json", IsNullable = true)]
    public string? Response { get; set; }

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion
