using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// 语言配置实体 - 基础语言信息和分词规则
/// </summary>
[SugarTable("language_configs")]
public class LanguageConfigEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 语言代码：zh, en, ja, ko, de, fr, es, etc.
    /// </summary>
    [SugarColumn(Length = 10)]
    public string LanguageCode { get; set; } = "";

    /// <summary>
    /// 语言名称
    /// </summary>
    [SugarColumn(Length = 50)]
    public string LanguageName { get; set; } = "";

    /// <summary>
    /// 语系：cjk (中日韩), latin (拉丁语系), arabic, cyrillic, etc.
    /// </summary>
    [SugarColumn(Length = 20)]
    public string LanguageFamily { get; set; } = "latin";

    /// <summary>
    /// 分词方式：character (按字符), space (按空格), morpheme (形态素)
    /// </summary>
    [SugarColumn(Length = 20)]
    public string TokenizationMethod { get; set; } = "space";

    /// <summary>
    /// 句子分隔符（JSON 数组格式）
    /// 例如：["。", "！", "？"] 或 [".", "!", "?"]
    /// </summary>
    [SugarColumn(Length = 200)]
    public string SentenceDelimiters { get; set; } = "[\".\" , \"!\", \"?\"]";

    /// <summary>
    /// 引号字符对（JSON 数组格式）
    /// 例如：[["\u201c", "\u201d"], ["\"", "\""]]
    /// </summary>
    [SugarColumn(Length = 200)]
    public string QuoteCharPairs { get; set; } = "[[\"\\\"\" , \"\\\"\"]]";

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
/// 提取模式配置实体 - 用于 Claim/Entity 提取的正则模式
/// </summary>
[SugarTable("extraction_patterns")]
public class ExtractionPatternEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 模式分类：claim_number, claim_statistic, claim_citation, claim_fact, entity_person, entity_location, entity_date
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Category { get; set; } = "";

    /// <summary>
    /// 适用范围：global (全局), family (语系), language (特定语言)
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Scope { get; set; } = "global";

    /// <summary>
    /// 适用的语言代码或语系（当 Scope 为 family 或 language 时）
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? ScopeValue { get; set; }

    /// <summary>
    /// 正则表达式模式
    /// </summary>
    [SugarColumn(Length = 500)]
    public string Pattern { get; set; } = "";

    /// <summary>
    /// 模式描述
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序（同类别内）
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
/// 已知实体配置实体 - 品牌、地点、人名等
/// </summary>
[SugarTable("known_entities")]
public class KnownEntityEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 实体类型：brand, location, person, product
    /// </summary>
    [SugarColumn(Length = 30)]
    public string EntityType { get; set; } = "";

    /// <summary>
    /// 适用范围：global (全局), language (特定语言)
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Scope { get; set; } = "global";

    /// <summary>
    /// 适用的语言代码（当 Scope 为 language 时）
    /// </summary>
    [SugarColumn(Length = 10, IsNullable = true)]
    public string? ScopeValue { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string EntityName { get; set; } = "";

    /// <summary>
    /// 别名（JSON 数组格式，用于匹配多种写法）
    /// 例如：["阿里", "Alibaba", "阿里巴巴集团"]
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Aliases { get; set; }

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
/// 情感关键词配置实体 - 用于情感分析
/// </summary>
[SugarTable("sentiment_keywords")]
public class SentimentKeywordEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 情感类型：positive, negative
    /// </summary>
    [SugarColumn(Length = 20)]
    public string SentimentType { get; set; } = "positive";

    /// <summary>
    /// 适用范围：global (全局), language (特定语言)
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Scope { get; set; } = "global";

    /// <summary>
    /// 适用的语言代码（当 Scope 为 language 时）
    /// </summary>
    [SugarColumn(Length = 10, IsNullable = true)]
    public string? ScopeValue { get; set; }

    /// <summary>
    /// 关键词
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Keyword { get; set; } = "";

    /// <summary>
    /// 权重（用于加权计算，默认 1.0）
    /// </summary>
    public double Weight { get; set; } = 1.0;

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
/// 关键词排除词实体 - 用于关键词提取时过滤常见词
/// 支持任意语言，从数据库动态加载
/// </summary>
[SugarTable("keyword_exclusions")]
public class KeywordExclusionEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 排除词
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Word { get; set; } = "";

    /// <summary>
    /// 语言代码：zh, en, ja, ko, de, fr, es, global 等
    /// global 表示适用于所有语言
    /// </summary>
    [SugarColumn(Length = 10)]
    public string LanguageCode { get; set; } = "global";

    /// <summary>
    /// 分类：common (常见词), tech (技术词), time (时间词), stopword (停用词)
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Category { get; set; } = "common";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Prompt 模板实体 - 用于存储可复用的 Prompt 模板
/// </summary>
[SugarTable("prompt_templates")]
public class PromptTemplateEntity
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
    [SugarColumn(Length = 500)]
    public string Description { get; set; } = "";

    /// <summary>
    /// 模板类别：question_generation, content_optimization, citation_analysis, etc.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Category { get; set; } = "general";

    /// <summary>
    /// Prompt 模板内容，支持变量占位符 {{variable}}
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Template { get; set; } = "";

    /// <summary>
    /// 模板变量定义（JSON 格式）
    /// 例如: [{"name": "brand", "type": "string", "required": true, "description": "品牌名称"}]
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Variables { get; set; } = "[]";

    /// <summary>
    /// 当前版本号
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 是否为默认模板
    /// </summary>
    public bool IsDefault { get; set; } = false;

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
/// Prompt 模板版本历史实体
/// </summary>
[SugarTable("prompt_template_versions")]
public class PromptTemplateVersionEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 模板ID
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 该版本的 Prompt 内容
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Template { get; set; } = "";

    /// <summary>
    /// 变更说明
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ChangeNote { get; set; } = "";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 问题库实体 - 保存历史生成的问题，支持复用
/// </summary>
[SugarTable("question_library")]
public class QuestionLibraryEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 关联项目ID（可选）
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? ProjectId { get; set; }

    /// <summary>
    /// 问题内容
    /// </summary>
    [SugarColumn(Length = 1000)]
    public string Question { get; set; } = "";

    /// <summary>
    /// 问题来源：ai_generated, manual, imported
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Source { get; set; } = "ai_generated";

    /// <summary>
    /// 问题类别：informational, transactional, navigational, commercial
    /// </summary>
    [SugarColumn(Length = 30)]
    public string QuestionType { get; set; } = "informational";

    /// <summary>
    /// 关联行业/领域
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Industry { get; set; }

    /// <summary>
    /// 关联关键词（JSON 数组）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Keywords { get; set; }

    /// <summary>
    /// 使用次数
    /// </summary>
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// 是否收藏
    /// </summary>
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 内容时效性记录实体 - 追踪内容更新时间
/// </summary>
[SugarTable("content_freshness")]
public class ContentFreshnessEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 关联项目ID
    /// </summary>
    [SugarColumn(Length = 50)]
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// 内容类型：faq, article, product_info, etc.
    /// </summary>
    [SugarColumn(Length = 30)]
    public string ContentType { get; set; } = "general";

    /// <summary>
    /// 内容标识（如 URL 或内部 ID）
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ContentIdentifier { get; set; } = "";

    /// <summary>
    /// 内容标题
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Title { get; set; } = "";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 建议刷新周期（天）
    /// </summary>
    public int RefreshIntervalDays { get; set; } = 90;

    /// <summary>
    /// 下次刷新提醒时间
    /// </summary>
    public DateTime NextRefreshAt { get; set; }

    /// <summary>
    /// 是否已发送提醒
    /// </summary>
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
