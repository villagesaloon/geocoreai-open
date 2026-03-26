using System;
using System.Collections.Generic;
using SqlSugar;

namespace GeoCore.Data.Entities;

#region 系统级共享表

/// <summary>
/// 来源平台基础数据（系统级共享）
/// </summary>
[SugarTable("sys_source_platforms")]
public class SysSourcePlatformEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "domain", Length = 200, IsNullable = false)]
    public string Domain { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "platform_type", Length = 50, IsNullable = false)]
    public string PlatformType { get; set; } = "blog"; // forum/news/blog/official/research/social

    [SugarColumn(ColumnName = "language", Length = 20, IsNullable = true)]
    public string? Language { get; set; } = "zh";

    [SugarColumn(ColumnName = "region", Length = 50, IsNullable = true)]
    public string? Region { get; set; } = "global";

    [SugarColumn(ColumnName = "authority_base_score", IsNullable = false)]
    public int AuthorityBaseScore { get; set; } = 50;

    [SugarColumn(ColumnName = "has_login_skill", IsNullable = false)]
    public bool HasLoginSkill { get; set; } = false;

    [SugarColumn(ColumnName = "has_publish_skill", IsNullable = false)]
    public bool HasPublishSkill { get; set; } = false;

    [SugarColumn(ColumnName = "has_comment_skill", IsNullable = false)]
    public bool HasCommentSkill { get; set; } = false;

    [SugarColumn(ColumnName = "has_crawl_skill", IsNullable = false)]
    public bool HasCrawlSkill { get; set; } = false;

    [SugarColumn(ColumnName = "skill_config", ColumnDataType = "json", IsNullable = true)]
    public string? SkillConfig { get; set; }

    [SugarColumn(ColumnName = "notes", ColumnDataType = "text", IsNullable = true)]
    public string? Notes { get; set; }

    [SugarColumn(ColumnName = "is_enabled", IsNullable = false)]
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

#endregion

#region 用户级表 - 项目相关

/// <summary>
/// GEO 项目主表
/// </summary>
[SugarTable("geo_projects")]
public class GeoProjectEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "brand_name", Length = 200, IsNullable = false)]
    public string BrandName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "product_name", Length = 200, IsNullable = true)]
    public string? ProductName { get; set; }

    [SugarColumn(ColumnName = "industry", Length = 100, IsNullable = true)]
    public string? Industry { get; set; }

    [SugarColumn(ColumnName = "description", ColumnDataType = "text", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "monitor_url", Length = 500, IsNullable = true)]
    public string? MonitorUrl { get; set; }

    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "active"; // draft/active/archived

    // 检测相关字段
    [SugarColumn(ColumnName = "detection_status", Length = 20, IsNullable = false)]
    public string DetectionStatus { get; set; } = "none"; // none/pending/running/completed/failed

    [SugarColumn(ColumnName = "last_detection_at", IsNullable = true)]
    public DateTime? LastDetectionAt { get; set; }

    [SugarColumn(ColumnName = "last_detection_task_id", IsNullable = true)]
    public long? LastDetectionTaskId { get; set; }

    [SugarColumn(ColumnName = "last_website_audit_at", IsNullable = true)]
    public DateTime? LastWebsiteAuditAt { get; set; }

    [SugarColumn(ColumnName = "visibility_score", IsNullable = true)]
    public decimal? VisibilityScore { get; set; }

    [SugarColumn(ColumnName = "website_health_score", IsNullable = true)]
    public decimal? WebsiteHealthScore { get; set; }

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 项目配置
/// </summary>
[SugarTable("geo_project_configs")]
public class GeoProjectConfigEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "markets", ColumnDataType = "json", IsNullable = true)]
    public string? Markets { get; set; } // JSON array

    [SugarColumn(ColumnName = "languages", ColumnDataType = "json", IsNullable = true)]
    public string? Languages { get; set; } // JSON array

    [SugarColumn(ColumnName = "models", ColumnDataType = "json", IsNullable = true)]
    public string? Models { get; set; } // JSON array

    [SugarColumn(ColumnName = "answer_mode", Length = 20, IsNullable = false)]
    public string AnswerMode { get; set; } = "simulation"; // content/simulation

    [SugarColumn(ColumnName = "enable_google_trends", IsNullable = false)]
    public bool EnableGoogleTrends { get; set; } = false;

    [SugarColumn(ColumnName = "enable_reddit_search", IsNullable = false)]
    public bool EnableRedditSearch { get; set; } = false;

    [SugarColumn(ColumnName = "enable_lightweight_mode", IsNullable = false)]
    public bool EnableLightweightMode { get; set; } = false;

    [SugarColumn(ColumnName = "questions_per_model", IsNullable = false)]
    public int QuestionsPerModel { get; set; } = 5;

    [SugarColumn(ColumnName = "core_needs", ColumnDataType = "json", IsNullable = true)]
    public string? CoreNeeds { get; set; } // JSON array

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 项目竞品
/// </summary>
[SugarTable("geo_project_competitors")]
public class GeoProjectCompetitorEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "url", Length = 500, IsNullable = true)]
    public string? Url { get; set; }

    [SugarColumn(ColumnName = "focus_points", ColumnDataType = "json", IsNullable = true)]
    public string? FocusPoints { get; set; } // JSON array

    [SugarColumn(ColumnName = "sort_order", IsNullable = false)]
    public int SortOrder { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 项目卖点
/// </summary>
[SugarTable("geo_project_selling_points")]
public class GeoProjectSellingPointEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "country", Length = 10, IsNullable = false)]
    public string Country { get; set; } = "CN";

    [SugarColumn(ColumnName = "language", Length = 20, IsNullable = false)]
    public string Language { get; set; } = "zh-CN";

    [SugarColumn(ColumnName = "point", Length = 200, IsNullable = false)]
    public string Point { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "usage_desc", ColumnDataType = "text", IsNullable = true)]
    public string? UsageDesc { get; set; }

    [SugarColumn(ColumnName = "weight", IsNullable = false)]
    public int Weight { get; set; } = 5; // 1-10

    [SugarColumn(ColumnName = "is_selected", IsNullable = false)]
    public bool IsSelected { get; set; } = true;

    [SugarColumn(ColumnName = "sort_order", IsNullable = false)]
    public int SortOrder { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 项目受众画像
/// </summary>
[SugarTable("geo_project_personas")]
public class GeoProjectPersonaEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "country", Length = 10, IsNullable = false)]
    public string Country { get; set; } = "CN";

    [SugarColumn(ColumnName = "language", Length = 20, IsNullable = false)]
    public string Language { get; set; } = "zh-CN";

    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "description", ColumnDataType = "text", IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(ColumnName = "is_selected", IsNullable = false)]
    public bool IsSelected { get; set; } = true;

    [SugarColumn(ColumnName = "sort_order", IsNullable = false)]
    public int SortOrder { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 项目决策阶段
/// </summary>
[SugarTable("geo_project_stages")]
public class GeoProjectStageEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "stage_key", Length = 50, IsNullable = false)]
    public string StageKey { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "stage_name", Length = 100, IsNullable = false)]
    public string StageName { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "is_selected", IsNullable = false)]
    public bool IsSelected { get; set; } = true;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 用户级表 - 问题相关

/// <summary>
/// GEO 问题
/// </summary>
[SugarTable("geo_questions")]
public class GeoQuestionEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "project_id", IsNullable = false)]
    public long ProjectId { get; set; }

    [SugarColumn(ColumnName = "country", Length = 10, IsNullable = false)]
    public string Country { get; set; } = "CN";

    [SugarColumn(ColumnName = "task_id", Length = 50, IsNullable = true)]
    public string? TaskId { get; set; }

    [SugarColumn(ColumnName = "question", ColumnDataType = "text", IsNullable = false)]
    public string Question { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "language", Length = 20, IsNullable = false)]
    public string Language { get; set; } = "zh-CN";

    [SugarColumn(ColumnName = "pattern", Length = 50, IsNullable = true)]
    public string? Pattern { get; set; }

    [SugarColumn(ColumnName = "intent", Length = 50, IsNullable = true)]
    public string? Intent { get; set; }

    [SugarColumn(ColumnName = "stage", Length = 50, IsNullable = true)]
    public string? Stage { get; set; }

    [SugarColumn(ColumnName = "persona", Length = 100, IsNullable = true)]
    public string? Persona { get; set; }

    [SugarColumn(ColumnName = "selling_point", Length = 200, IsNullable = true)]
    public string? SellingPoint { get; set; }

    [SugarColumn(ColumnName = "question_source", Length = 20, IsNullable = false)]
    public string QuestionSource { get; set; } = "ai"; // ai/real/trends

    [SugarColumn(ColumnName = "source_detail", Length = 100, IsNullable = true)]
    public string? SourceDetail { get; set; } // r/SEO, Quora, 知乎

    [SugarColumn(ColumnName = "source_url", Length = 500, IsNullable = true)]
    public string? SourceUrl { get; set; }

    [SugarColumn(ColumnName = "google_trends_heat", IsNullable = true)]
    public int? GoogleTrendsHeat { get; set; }

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 问题模型回答
/// </summary>
[SugarTable("geo_question_answers")]
public class GeoQuestionAnswerEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "question_id", IsNullable = false)]
    public long QuestionId { get; set; }

    [SugarColumn(ColumnName = "model", Length = 50, IsNullable = false)]
    public string Model { get; set; } = string.Empty; // gpt/claude/gemini/grok/perplexity

    [SugarColumn(ColumnName = "answer", ColumnDataType = "text", IsNullable = true)]
    public string? Answer { get; set; }

    [SugarColumn(ColumnName = "search_index", IsNullable = false)]
    public int SearchIndex { get; set; } = 0; // 0-100

    [SugarColumn(ColumnName = "brand_fit_index", IsNullable = false)]
    public int BrandFitIndex { get; set; } = 0; // 0-100

    [SugarColumn(ColumnName = "score", IsNullable = false)]
    public int Score { get; set; } = 0;

    [SugarColumn(ColumnName = "brand_analysis", ColumnDataType = "json", IsNullable = true)]
    public string? BrandAnalysis { get; set; } // JSON

    [SugarColumn(ColumnName = "citation_difficulty", ColumnDataType = "json", IsNullable = true)]
    public string? CitationDifficulty { get; set; } // JSON

    [SugarColumn(ColumnName = "answer_mode", Length = 20, IsNullable = false)]
    public string AnswerMode { get; set; } = "simulation"; // content/simulation

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 问题来源信息
/// </summary>
[SugarTable("geo_question_sources")]
public class GeoQuestionSourceEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnName = "user_id", IsNullable = false)]
    public long UserId { get; set; }

    [SugarColumn(ColumnName = "question_id", IsNullable = false)]
    public long QuestionId { get; set; }

    [SugarColumn(ColumnName = "answer_id", IsNullable = true)]
    public long? AnswerId { get; set; }

    [SugarColumn(ColumnName = "platform_id", IsNullable = true)]
    public long? PlatformId { get; set; }

    [SugarColumn(ColumnName = "model", Length = 50, IsNullable = true)]
    public string? Model { get; set; }

    [SugarColumn(ColumnName = "url", Length = 1000, IsNullable = false)]
    public string Url { get; set; } = string.Empty;

    [SugarColumn(ColumnName = "domain", Length = 200, IsNullable = true)]
    public string? Domain { get; set; }

    [SugarColumn(ColumnName = "title", Length = 500, IsNullable = true)]
    public string? Title { get; set; }

    [SugarColumn(ColumnName = "snippet", ColumnDataType = "text", IsNullable = true)]
    public string? Snippet { get; set; }

    [SugarColumn(ColumnName = "source_type", Length = 50, IsNullable = true)]
    public string? SourceType { get; set; } // article/forum/news/official/research

    [SugarColumn(ColumnName = "authority_score", IsNullable = false)]
    public int AuthorityScore { get; set; } = 50; // 0-100

    [SugarColumn(ColumnName = "is_target_for_content", IsNullable = false)]
    public bool IsTargetForContent { get; set; } = false;

    [SugarColumn(ColumnName = "content_status", Length = 20, IsNullable = false)]
    public string ContentStatus { get; set; } = "none"; // none/pending/drafting/published

    [SugarColumn(ColumnName = "sort_order", IsNullable = false)]
    public int SortOrder { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion
