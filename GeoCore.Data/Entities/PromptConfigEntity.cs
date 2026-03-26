using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// Prompt 配置实体 - 用于 Admin 后台管理 Prompt 模板
/// </summary>
[SugarTable("prompt_configs")]
public class PromptConfigEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 配置分类：questions, answers, analysis, etc.
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Category { get; set; } = "";

    /// <summary>
    /// 配置键名：general, perplexity, gpt, claude, etc.
    /// </summary>
    [SugarColumn(Length = 100)]
    public string ConfigKey { get; set; } = "";

    /// <summary>
    /// 配置名称（显示用）
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Name { get; set; } = "";

    /// <summary>
    /// 配置描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// Prompt 模板内容
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string PromptTemplate { get; set; } = "";

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

    /// <summary>
    /// 创建人
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 系统参数配置实体 - 用于存储阈值等可配置参数
/// </summary>
[SugarTable("system_configs")]
public class SystemConfigEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 配置分类
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Category { get; set; } = "";

    /// <summary>
    /// 配置键名
    /// </summary>
    [SugarColumn(Length = 100)]
    public string ConfigKey { get; set; } = "";

    /// <summary>
    /// 配置值
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ConfigValue { get; set; } = "";

    /// <summary>
    /// 配置名称（显示用）
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Name { get; set; } = "";

    /// <summary>
    /// 配置描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 值类型：string, int, bool, json
    /// </summary>
    [SugarColumn(Length = 20)]
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 大模型配置实体 - 用于存储 AI 模型的 API 配置
/// </summary>
[SugarTable("model_configs")]
public class ModelConfigEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 模型标识：gpt, claude, gemini, grok, perplexity 等
    /// </summary>
    [SugarColumn(Length = 50)]
    public string ModelId { get; set; } = "";

    /// <summary>
    /// 显示名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// API 端点
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ApiEndpoint { get; set; } = "";

    /// <summary>
    /// API Key（加密存储）
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// 模型名称（如 gpt-4o, claude-3-opus 等）
    /// </summary>
    [SugarColumn(Length = 200)]
    public string ModelName { get; set; } = "";

    /// <summary>
    /// 默认温度参数
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int MaxTokens { get; set; } = 16384;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 备注描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 输入价格（每百万 Token），单位由 PriceCurrency 决定
    /// </summary>
    [SugarColumn(DecimalDigits = 4, IsNullable = true)]
    public decimal? InputPricePerMToken { get; set; }

    /// <summary>
    /// 输出价格（每百万 Token），单位由 PriceCurrency 决定
    /// </summary>
    [SugarColumn(DecimalDigits = 4, IsNullable = true)]
    public decimal? OutputPricePerMToken { get; set; }

    /// <summary>
    /// 价格货币：USD / CNY
    /// </summary>
    [SugarColumn(Length = 10, IsNullable = true)]
    public string? PriceCurrency { get; set; } = "USD";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
