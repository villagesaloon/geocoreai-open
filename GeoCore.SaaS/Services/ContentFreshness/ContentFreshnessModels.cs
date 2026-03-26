namespace GeoCore.SaaS.Services.ContentFreshness;

/// <summary>
/// 内容新鲜度分析报告
/// </summary>
public class ContentFreshnessReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Brand { get; set; } = "";
    public int TaskId { get; set; }
    
    /// <summary>
    /// 整体新鲜度评分 (0-100)
    /// </summary>
    public double OverallScore { get; set; }
    
    /// <summary>
    /// 新鲜度级别：fresh, good, stale, outdated
    /// </summary>
    public string FreshnessLevel { get; set; } = "good";
    
    /// <summary>
    /// 内容新鲜度详情
    /// </summary>
    public List<ContentFreshnessItem> Items { get; set; } = new();
    
    /// <summary>
    /// 新鲜度分布统计
    /// </summary>
    public FreshnessDistribution Distribution { get; set; } = new();
    
    /// <summary>
    /// 新鲜度告警
    /// </summary>
    public List<FreshnessAlert> Alerts { get; set; } = new();
    
    /// <summary>
    /// 更新建议
    /// </summary>
    public List<UpdateSuggestion> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 趋势分析
    /// </summary>
    public FreshnessTrend Trend { get; set; } = new();
    
    /// <summary>
    /// 报告摘要
    /// </summary>
    public string Summary { get; set; } = "";
    
    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 内容新鲜度项
/// </summary>
public class ContentFreshnessItem
{
    /// <summary>
    /// 内容 URL 或标识
    /// </summary>
    public string ContentUrl { get; set; } = "";
    
    /// <summary>
    /// 内容标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 内容类型
    /// </summary>
    public string ContentType { get; set; } = "article";
    
    /// <summary>
    /// 发布日期
    /// </summary>
    public DateTime? PublishedDate { get; set; }
    
    /// <summary>
    /// 最后更新日期
    /// </summary>
    public DateTime? LastUpdated { get; set; }
    
    /// <summary>
    /// 内容年龄（天）
    /// </summary>
    public int AgeDays { get; set; }
    
    /// <summary>
    /// 新鲜度评分 (0-100)
    /// </summary>
    public double FreshnessScore { get; set; }
    
    /// <summary>
    /// 新鲜度级别
    /// </summary>
    public string FreshnessLevel { get; set; } = "good";
    
    /// <summary>
    /// 被 AI 引用次数
    /// </summary>
    public int CitationCount { get; set; }
    
    /// <summary>
    /// 引用该内容的 AI 平台
    /// </summary>
    public List<string> CitedByPlatforms { get; set; } = new();
    
    /// <summary>
    /// 是否需要更新
    /// </summary>
    public bool NeedsUpdate { get; set; }
    
    /// <summary>
    /// 更新优先级 (1-10)
    /// </summary>
    public int UpdatePriority { get; set; }
    
    /// <summary>
    /// 更新建议
    /// </summary>
    public string UpdateRecommendation { get; set; } = "";
}

/// <summary>
/// 新鲜度分布统计
/// </summary>
public class FreshnessDistribution
{
    /// <summary>
    /// 新鲜内容（6个月内）占比
    /// </summary>
    public double FreshRate { get; set; }
    
    /// <summary>
    /// 良好内容（6-12个月）占比
    /// </summary>
    public double GoodRate { get; set; }
    
    /// <summary>
    /// 陈旧内容（12-24个月）占比
    /// </summary>
    public double StaleRate { get; set; }
    
    /// <summary>
    /// 过时内容（24个月以上）占比
    /// </summary>
    public double OutdatedRate { get; set; }
    
    /// <summary>
    /// 各级别数量
    /// </summary>
    public int FreshCount { get; set; }
    public int GoodCount { get; set; }
    public int StaleCount { get; set; }
    public int OutdatedCount { get; set; }
    
    /// <summary>
    /// 平均内容年龄（天）
    /// </summary>
    public double AverageAgeDays { get; set; }
    
    /// <summary>
    /// 中位数内容年龄（天）
    /// </summary>
    public double MedianAgeDays { get; set; }
}

/// <summary>
/// 新鲜度告警
/// </summary>
public class FreshnessAlert
{
    /// <summary>
    /// 告警级别：critical, warning, info
    /// </summary>
    public string Level { get; set; } = "info";
    
    /// <summary>
    /// 告警类型：outdated_content, stale_content, no_recent_updates, high_citation_stale
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 告警标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 告警描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 受影响的内容数量
    /// </summary>
    public int AffectedCount { get; set; }
    
    /// <summary>
    /// 建议行动
    /// </summary>
    public string SuggestedAction { get; set; } = "";
    
    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 更新建议
/// </summary>
public class UpdateSuggestion
{
    /// <summary>
    /// 建议类型：refresh, rewrite, archive, merge
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 目标内容
    /// </summary>
    public string TargetContent { get; set; } = "";
    
    /// <summary>
    /// 建议标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 建议描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 具体行动步骤
    /// </summary>
    public List<string> ActionSteps { get; set; } = new();
    
    /// <summary>
    /// 优先级 (1-10)
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// 预期影响
    /// </summary>
    public string ExpectedImpact { get; set; } = "";
    
    /// <summary>
    /// 预计工作量：low, medium, high
    /// </summary>
    public string Effort { get; set; } = "medium";
    
    /// <summary>
    /// 建议更新时间
    /// </summary>
    public string SuggestedTimeline { get; set; } = "";
}

/// <summary>
/// 新鲜度趋势
/// </summary>
public class FreshnessTrend
{
    /// <summary>
    /// 趋势方向：improving, stable, declining
    /// </summary>
    public string Direction { get; set; } = "stable";
    
    /// <summary>
    /// 趋势数据点
    /// </summary>
    public List<FreshnessTrendPoint> DataPoints { get; set; } = new();
    
    /// <summary>
    /// 趋势分析说明
    /// </summary>
    public string Analysis { get; set; } = "";
}

/// <summary>
/// 新鲜度趋势数据点
/// </summary>
public class FreshnessTrendPoint
{
    public DateTime Date { get; set; }
    public double AverageScore { get; set; }
    public int TotalContent { get; set; }
    public int FreshContent { get; set; }
    public int StaleContent { get; set; }
}

/// <summary>
/// 新鲜度阈值配置
/// </summary>
public class FreshnessThresholds
{
    /// <summary>
    /// 新鲜内容阈值（天）- 默认 180 天（6个月）
    /// </summary>
    public int FreshDays { get; set; } = 180;
    
    /// <summary>
    /// 良好内容阈值（天）- 默认 365 天（12个月）
    /// </summary>
    public int GoodDays { get; set; } = 365;
    
    /// <summary>
    /// 陈旧内容阈值（天）- 默认 730 天（24个月）
    /// </summary>
    public int StaleDays { get; set; } = 730;
    
    /// <summary>
    /// ChatGPT 偏好的更新周期（天）- 393-458 天
    /// </summary>
    public int ChatGptPreferredMinDays { get; set; } = 393;
    public int ChatGptPreferredMaxDays { get; set; } = 458;
}

/// <summary>
/// 内容新鲜度分析请求
/// </summary>
public class ContentFreshnessRequest
{
    public int TaskId { get; set; }
    public string Brand { get; set; } = "";
    public FreshnessThresholds? Thresholds { get; set; }
    public bool IncludeTrends { get; set; } = true;
    public bool IncludeAlerts { get; set; } = true;
}
