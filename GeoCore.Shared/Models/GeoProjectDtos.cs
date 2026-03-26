using System;
using System.Collections.Generic;

namespace GeoCore.Shared.Models;

#region 项目相关 DTO

/// <summary>
/// GEO 项目 DTO
/// </summary>
public class GeoProjectDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? MonitorUrl { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // 关联数据（可选加载）
    public GeoProjectConfigDto? Config { get; set; }
    public List<GeoCompetitorDto>? Competitors { get; set; }
    public List<GeoSellingPointDto>? SellingPoints { get; set; }
    public List<GeoPersonaDto>? Personas { get; set; }
    public List<GeoStageDto>? Stages { get; set; }
}

/// <summary>
/// 项目配置 DTO
/// </summary>
public class GeoProjectConfigDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    
    /// <summary>
    /// 目标国家代码列表（新版，如 CN, US, JP）
    /// </summary>
    public List<string>? Countries { get; set; }
    
    /// <summary>
    /// 目标市场列表（旧版，向后兼容）
    /// 如果 Countries 为空，则使用 Markets
    /// </summary>
    public List<string>? Markets { get; set; }
    
    /// <summary>
    /// 获取有效的目标区域（优先使用 Countries，否则使用 Markets）
    /// </summary>
    public List<string>? EffectiveCountries => Countries?.Count > 0 ? Countries : Markets;
    
    public List<string>? Languages { get; set; }
    public List<string>? Models { get; set; }
    public string AnswerMode { get; set; } = "simulation";
    public bool EnableGoogleTrends { get; set; }
    public bool EnableRedditSearch { get; set; }
    public bool EnableLightweightMode { get; set; }
    public int QuestionsPerModel { get; set; } = 5;
    
    /// <summary>
    /// 核心诉求列表（用户视角的需求，如"效率提升"、"成本控制"）
    /// </summary>
    public List<string>? CoreNeeds { get; set; }
}

/// <summary>
/// 竞品 DTO
/// </summary>
public class GeoCompetitorDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public List<string>? FocusPoints { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// 卖点 DTO
/// </summary>
public class GeoSellingPointDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Country { get; set; } = "CN";
    public string Language { get; set; } = "zh-CN";
    public string Point { get; set; } = string.Empty;
    public string? UsageDesc { get; set; }
    public int Weight { get; set; } = 5;
    public bool IsSelected { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// 受众画像 DTO
/// </summary>
public class GeoPersonaDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Country { get; set; } = "CN";
    public string Language { get; set; } = "zh-CN";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSelected { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// 决策阶段 DTO
/// </summary>
public class GeoStageDto
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string StageKey { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public bool IsSelected { get; set; } = true;
}

#endregion

#region 问题相关 DTO

/// <summary>
/// GEO 问题 DTO
/// </summary>
public class GeoQuestionDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long ProjectId { get; set; }
    public string Country { get; set; } = "CN";
    public string? TaskId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Language { get; set; } = "zh-CN";
    public string? Pattern { get; set; }
    public string? Intent { get; set; }
    public string? Stage { get; set; }
    public string? Persona { get; set; }
    public string? SellingPoint { get; set; }
    public string QuestionSource { get; set; } = "ai";
    public string? SourceDetail { get; set; }
    public string? SourceUrl { get; set; }
    public int? GoogleTrendsHeat { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // 关联数据（可选加载）
    public List<GeoQuestionAnswerDto>? Answers { get; set; }
    public List<GeoQuestionSourceDto>? Sources { get; set; }
}

/// <summary>
/// 问题回答 DTO
/// </summary>
public class GeoQuestionAnswerDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long QuestionId { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public int SearchIndex { get; set; }
    public int BrandFitIndex { get; set; }
    public int Score { get; set; }
    public string? BrandAnalysis { get; set; } // JSON string
    public string? CitationDifficulty { get; set; } // JSON string
    public string AnswerMode { get; set; } = "simulation";
    public DateTime CreatedAt { get; set; }
    
    // 关联数据（可选加载）
    public List<GeoQuestionSourceDto>? Sources { get; set; }
}

/// <summary>
/// 问题来源 DTO
/// </summary>
public class GeoQuestionSourceDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long QuestionId { get; set; }
    public long? AnswerId { get; set; }
    public long? PlatformId { get; set; }
    public string? Model { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Title { get; set; }
    public string? Snippet { get; set; }
    public string? SourceType { get; set; }
    public int AuthorityScore { get; set; } = 50;
    public bool IsTargetForContent { get; set; }
    public string ContentStatus { get; set; } = "none";
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // 关联数据（可选加载）
    public SysSourcePlatformDto? Platform { get; set; }
}

#endregion

#region 系统级 DTO

/// <summary>
/// 来源平台 DTO
/// </summary>
public class SysSourcePlatformDto
{
    public long Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PlatformType { get; set; } = "blog";
    public string? Language { get; set; }
    public string? Region { get; set; }
    public int AuthorityBaseScore { get; set; } = 50;
    public bool HasLoginSkill { get; set; }
    public bool HasPublishSkill { get; set; }
    public bool HasCommentSkill { get; set; }
    public bool HasCrawlSkill { get; set; }
    public string? SkillConfig { get; set; }
    public string? Notes { get; set; }
    public bool IsEnabled { get; set; } = true;
}

#endregion
