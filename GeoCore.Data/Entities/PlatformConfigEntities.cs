using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// 系统配置实体 - 存储系统级配置（如 API Key、邮件设置等）
/// 通过 Admin 后台管理
/// </summary>
[SugarTable("sys_configs")]
public class SysConfigEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 配置分组，如 resend, brightdata, openai
    /// </summary>
    [SugarColumn(ColumnName = "config_group", Length = 50, IsNullable = false)]
    public string ConfigGroup { get; set; } = string.Empty;

    /// <summary>
    /// 配置键
    /// </summary>
    [SugarColumn(ColumnName = "config_key", Length = 100, IsNullable = false)]
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    [SugarColumn(ColumnName = "config_value", ColumnDataType = "text", IsNullable = true)]
    public string? ConfigValue { get; set; }

    /// <summary>
    /// 配置名称（显示用）
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 配置描述
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 值类型：string, int, bool, json
    /// </summary>
    [SugarColumn(ColumnName = "value_type", Length = 20, IsNullable = false)]
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 是否敏感信息（如 API Key，显示时需要遮掩）
    /// </summary>
    [SugarColumn(ColumnName = "is_sensitive", IsNullable = false)]
    public bool IsSensitive { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false)]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    [SugarColumn(ColumnName = "sort_order", IsNullable = false)]
    public int SortOrder { get; set; } = 0;

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// AI 爬虫实体 - 存储已知的 AI 爬虫信息
/// </summary>
[SugarTable("ai_crawlers")]
public class AICrawlerEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 爬虫名称，如 GPTBot, ClaudeBot, PerplexityBot
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Name { get; set; } = "";

    /// <summary>
    /// User-Agent 字符串模式
    /// </summary>
    [SugarColumn(Length = 500)]
    public string UserAgentPattern { get; set; } = "";

    /// <summary>
    /// 所属公司/平台
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Company { get; set; } = "";

    /// <summary>
    /// 关联的 AI 平台名称，如 ChatGPT, Claude, Perplexity
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Platform { get; set; }

    /// <summary>
    /// 爬虫用途说明
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Purpose { get; set; }

    /// <summary>
    /// 重要性级别：high, medium, low
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Importance { get; set; } = "medium";

    /// <summary>
    /// 流量占比（百分比）
    /// </summary>
    public double TrafficShare { get; set; } = 0;

    /// <summary>
    /// 别名列表（JSON 数组格式）
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? AlternativeNames { get; set; }

    /// <summary>
    /// 官方文档链接
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否支持 robots.txt
    /// </summary>
    public bool RespectsRobotsTxt { get; set; } = true;

    /// <summary>
    /// 爬虫类型：training（训练爬虫）, retrieval（检索爬虫）, hybrid（混合）
    /// 7.23 AI 爬虫分类策略
    /// </summary>
    [SugarColumn(Length = 20)]
    public string CrawlerType { get; set; } = "retrieval";

    /// <summary>
    /// 推荐的 robots.txt 策略：allow（允许）, disallow（禁止）, conditional（条件允许）
    /// </summary>
    [SugarColumn(Length = 20)]
    public string RecommendedRobotsPolicy { get; set; } = "allow";

    /// <summary>
    /// 策略说明
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    public string? PolicyRationale { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LLM Referrer 实体 - 存储 LLM 平台的 Referrer 信息
/// </summary>
[SugarTable("llm_referrers")]
public class LLMReferrerEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 平台名称，如 ChatGPT, Perplexity, Claude
    /// </summary>
    [SugarColumn(Length = 100)]
    public string PlatformName { get; set; } = "";

    /// <summary>
    /// 所属公司/平台
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Company { get; set; }

    /// <summary>
    /// Referrer 域名模式
    /// </summary>
    [SugarColumn(Length = 200)]
    public string ReferrerPattern { get; set; } = "";

    /// <summary>
    /// 关联的 User-Agent 模式（JSON 数组格式）
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? UserAgentPatterns { get; set; }

    /// <summary>
    /// 流量类型：referral, organic, direct
    /// </summary>
    [SugarColumn(Length = 50)]
    public string TrafficType { get; set; } = "referral";

    /// <summary>
    /// 预估流量占比（百分比）
    /// </summary>
    public double EstimatedShare { get; set; } = 0;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Notes { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LLM 平台偏好实体 - 存储各 LLM 平台的偏好来源数据
/// </summary>
[SugarTable("llm_platform_preferences")]
public class LLMPlatformPreferenceEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 平台名称，如 ChatGPT, Perplexity, Claude, Gemini
    /// </summary>
    [SugarColumn(Length = 100)]
    public string PlatformName { get; set; } = "";

    /// <summary>
    /// 偏好类别，如 source_type, content_format, domain_authority
    /// </summary>
    [SugarColumn(Length = 50)]
    public string PreferenceCategory { get; set; } = "";

    /// <summary>
    /// 偏好项名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string PreferenceName { get; set; } = "";

    /// <summary>
    /// 偏好值/权重 (0-100)
    /// </summary>
    public int PreferenceValue { get; set; } = 50;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? DataSource { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 引用基准实体 - 存储各平台的引用基准数据
/// </summary>
[SugarTable("citation_benchmarks")]
public class CitationBenchmarkEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 平台名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string PlatformName { get; set; } = "";

    /// <summary>
    /// 指标名称，如 avg_citations, source_diversity, response_time
    /// </summary>
    [SugarColumn(Length = 100)]
    public string MetricName { get; set; } = "";

    /// <summary>
    /// 指标值
    /// </summary>
    public double MetricValue { get; set; } = 0;

    /// <summary>
    /// 指标单位
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? MetricUnit { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? DataSource { get; set; }

    /// <summary>
    /// 数据采集时间
    /// </summary>
    public DateTime? DataCollectedAt { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Persona 模板实体 - 存储买家角色模板
/// </summary>
[SugarTable("persona_templates")]
public class PersonaTemplateEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Name { get; set; } = "";

    /// <summary>
    /// 模板描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 行业/领域
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Industry { get; set; }

    /// <summary>
    /// 角色类型，如 decision_maker, influencer, end_user
    /// </summary>
    [SugarColumn(Length = 50)]
    public string RoleType { get; set; } = "end_user";

    /// <summary>
    /// 模板内容 (JSON 格式)
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string TemplateContent { get; set; } = "{}";

    /// <summary>
    /// 是否为默认模板
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
