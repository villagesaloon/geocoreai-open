using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// 监测任务实体
/// </summary>
[SugarTable("citation_monitoring_tasks")]
public class CitationMonitoringTaskEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 项目ID（关联项目）
    /// </summary>
    [SugarColumn(Length = 64)]
    public string ProjectId { get; set; } = "";

    /// <summary>
    /// 品牌名称
    /// </summary>
    [SugarColumn(Length = 200)]
    public string BrandName { get; set; } = "";

    /// <summary>
    /// 品牌别名（JSON 数组）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? BrandAliases { get; set; }

    /// <summary>
    /// 竞品列表（JSON 数组）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Competitors { get; set; }

    /// <summary>
    /// 监测问题列表（JSON 数组）
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Questions { get; set; } = "[]";

    /// <summary>
    /// 目标平台（JSON 数组：chatgpt, perplexity, claude, gemini, grok, google_ai）
    /// </summary>
    [SugarColumn(Length = 500)]
    public string TargetPlatforms { get; set; } = "[\"chatgpt\",\"perplexity\",\"claude\",\"gemini\"]";

    /// <summary>
    /// 任务状态：pending, running, completed, failed
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 监测频率：daily, weekly, monthly
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Frequency { get; set; } = "weekly";

    /// <summary>
    /// 上次执行时间
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    public DateTime? NextRunAt { get; set; }

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
/// 监测执行记录实体
/// </summary>
[SugarTable("citation_monitoring_runs")]
public class CitationMonitoringRunEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 关联任务ID
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// 执行开始时间
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 执行结束时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 执行状态：running, completed, failed
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Status { get; set; } = "running";

    /// <summary>
    /// 总问题数
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// 已完成问题数
    /// </summary>
    public int CompletedQuestions { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 汇总指标（JSON）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? SummaryMetrics { get; set; }
}

/// <summary>
/// 引用检测结果实体
/// </summary>
[SugarTable("citation_results")]
public class CitationResultEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 关联执行记录ID
    /// </summary>
    public int RunId { get; set; }

    /// <summary>
    /// 关联任务ID
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// 平台名称：chatgpt, perplexity, claude, gemini, grok, google_ai
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 查询的问题
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Question { get; set; } = "";

    /// <summary>
    /// AI 回答内容
    /// </summary>
    [SugarColumn(ColumnDataType = "mediumtext")]
    public string Response { get; set; } = "";

    /// <summary>
    /// 品牌是否被引用
    /// </summary>
    public bool IsCited { get; set; }

    /// <summary>
    /// 引用位置：first, middle, last, none
    /// </summary>
    [SugarColumn(Length = 20)]
    public string CitationPosition { get; set; } = "none";

    /// <summary>
    /// 引用位置比例（0-1）
    /// </summary>
    public double PositionRatio { get; set; }

    /// <summary>
    /// 是否带有品牌链接
    /// </summary>
    public bool HasLink { get; set; }

    /// <summary>
    /// 检测到的链接URL
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? DetectedLink { get; set; }

    /// <summary>
    /// 引用上下文（品牌提及前后的文本）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? CitationContext { get; set; }

    /// <summary>
    /// 情感分析：positive, neutral, negative
    /// </summary>
    [SugarColumn(Length = 20)]
    public string Sentiment { get; set; } = "neutral";

    /// <summary>
    /// 情感评分（-1 到 1）
    /// </summary>
    public double SentimentScore { get; set; }

    /// <summary>
    /// 品牌可见度评分（BVS）
    /// </summary>
    public double VisibilityScore { get; set; }

    /// <summary>
    /// Word Count 指标：引用句子词数占总词数的比例
    /// </summary>
    public double WordCountRatio { get; set; }

    /// <summary>
    /// 引用句子的词数
    /// </summary>
    public int CitationWordCount { get; set; }

    /// <summary>
    /// 响应总词数
    /// </summary>
    public int TotalWordCount { get; set; }

    /// <summary>
    /// Position-Adjusted Score：位置加权可见度
    /// </summary>
    public double PositionAdjustedScore { get; set; }

    /// <summary>
    /// 竞品引用情况（JSON：{competitor: cited/not_cited}）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? CompetitorCitations { get; set; }

    /// <summary>
    /// API 调用耗时（毫秒）
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// API 调用成本（美元）
    /// </summary>
    public double ApiCost { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 每日汇总指标实体（用于趋势分析）
/// </summary>
[SugarTable("citation_daily_metrics")]
public class CitationDailyMetricsEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 关联任务ID
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// 统计日期
    /// </summary>
    [SugarColumn(ColumnDataType = "date")]
    public DateTime MetricDate { get; set; }

    /// <summary>
    /// 平台名称（all 表示全平台汇总）
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "all";

    /// <summary>
    /// 引用频率（Citation Frequency）
    /// </summary>
    public double CitationFrequency { get; set; }

    /// <summary>
    /// 品牌可见度评分（Brand Visibility Score）
    /// </summary>
    public double BrandVisibilityScore { get; set; }

    /// <summary>
    /// AI 声量份额（Share of Voice）
    /// </summary>
    public double ShareOfVoice { get; set; }

    /// <summary>
    /// 正面情感占比
    /// </summary>
    public double PositiveSentimentRatio { get; set; }

    /// <summary>
    /// 带链接引用占比
    /// </summary>
    public double LinkedCitationRatio { get; set; }

    /// <summary>
    /// 首位引用占比
    /// </summary>
    public double FirstPositionRatio { get; set; }

    /// <summary>
    /// 总查询数
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// 被引用次数
    /// </summary>
    public int CitedCount { get; set; }

    /// <summary>
    /// 竞品引用次数（JSON）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? CompetitorMetrics { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
