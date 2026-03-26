namespace GeoCore.SaaS.Services.ContentFreshnessAudit;

/// <summary>
/// 内容新鲜度审计请求
/// </summary>
public class ContentFreshnessAuditRequest
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// Sitemap URL（可选）
    /// </summary>
    public string? SitemapUrl { get; set; }

    /// <summary>
    /// 手动提供的页面列表（可选）
    /// </summary>
    public List<PageInfo>? Pages { get; set; }

    /// <summary>
    /// 新鲜度阈值（天数，默认 60）
    /// </summary>
    public int FreshnessThresholdDays { get; set; } = 60;

    /// <summary>
    /// 警告阈值（天数，默认 90）
    /// </summary>
    public int WarningThresholdDays { get; set; } = 90;

    /// <summary>
    /// 严重阈值（天数，默认 180）
    /// </summary>
    public int CriticalThresholdDays { get; set; } = 180;

    /// <summary>
    /// 项目 ID（可选）
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// 是否检查页面内容中的日期
    /// </summary>
    public bool CheckContentDates { get; set; } = false;

    /// <summary>
    /// 最大检查页面数
    /// </summary>
    public int MaxPages { get; set; } = 100;
}

/// <summary>
/// 页面信息
/// </summary>
public class PageInfo
{
    /// <summary>
    /// 页面 URL
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// 页面标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// 页面类型
    /// </summary>
    public string? PageType { get; set; }
}

/// <summary>
/// 内容新鲜度审计报告
/// </summary>
public class ContentFreshnessAuditReport
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// 审计时间
    /// </summary>
    public DateTime AuditTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 整体新鲜度评分（0-100）
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// 评分等级：excellent, good, warning, critical
    /// </summary>
    public string ScoreLevel { get; set; } = "unknown";

    /// <summary>
    /// 审计的页面总数
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// 统计摘要
    /// </summary>
    public FreshnessAuditSummary Summary { get; set; } = new();

    /// <summary>
    /// 页面详情列表
    /// </summary>
    public List<PageFreshnessDetail> Pages { get; set; } = new();

    /// <summary>
    /// 问题列表
    /// </summary>
    public List<FreshnessIssue> Issues { get; set; } = new();

    /// <summary>
    /// 优化建议
    /// </summary>
    public List<FreshnessSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// 按类型分布
    /// </summary>
    public Dictionary<string, FreshnessDistributionItem> DistributionByType { get; set; } = new();
}

/// <summary>
/// 新鲜度审计摘要
/// </summary>
public class FreshnessAuditSummary
{
    /// <summary>
    /// 新鲜页面数（< 60 天）
    /// </summary>
    public int FreshCount { get; set; }

    /// <summary>
    /// 需要关注的页面数（60-90 天）
    /// </summary>
    public int AttentionCount { get; set; }

    /// <summary>
    /// 警告页面数（90-180 天）
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// 严重过时页面数（> 180 天）
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// 未知更新时间的页面数
    /// </summary>
    public int UnknownCount { get; set; }

    /// <summary>
    /// 新鲜页面占比
    /// </summary>
    public double FreshRate { get; set; }

    /// <summary>
    /// 平均页面年龄（天）
    /// </summary>
    public double AverageAgeDays { get; set; }

    /// <summary>
    /// 最老页面年龄（天）
    /// </summary>
    public int OldestAgeDays { get; set; }

    /// <summary>
    /// 最新页面年龄（天）
    /// </summary>
    public int NewestAgeDays { get; set; }

    /// <summary>
    /// 预估的引用率影响
    /// </summary>
    public string CitationImpactEstimate { get; set; } = "";
}

/// <summary>
/// 页面新鲜度详情
/// </summary>
public class PageFreshnessDetail
{
    /// <summary>
    /// 页面 URL
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// 页面标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 页面类型
    /// </summary>
    public string PageType { get; set; } = "other";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// 页面年龄（天）
    /// </summary>
    public int? AgeDays { get; set; }

    /// <summary>
    /// 新鲜度状态：fresh, attention, warning, critical, unknown
    /// </summary>
    public string FreshnessStatus { get; set; } = "unknown";

    /// <summary>
    /// 新鲜度评分（0-100）
    /// </summary>
    public int FreshnessScore { get; set; }

    /// <summary>
    /// 是否需要更新
    /// </summary>
    public bool NeedsUpdate { get; set; }

    /// <summary>
    /// 更新优先级（1-10，10 最高）
    /// </summary>
    public int UpdatePriority { get; set; }

    /// <summary>
    /// 建议的操作
    /// </summary>
    public string RecommendedAction { get; set; } = "";

    /// <summary>
    /// 预估的引用率影响
    /// </summary>
    public string CitationImpact { get; set; } = "";
}

/// <summary>
/// 新鲜度问题
/// </summary>
public class FreshnessIssue
{
    /// <summary>
    /// 问题级别：critical, warning, info
    /// </summary>
    public string Level { get; set; } = "info";

    /// <summary>
    /// 问题类型
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 问题标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 问题描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 受影响的页面数
    /// </summary>
    public int AffectedPages { get; set; }

    /// <summary>
    /// 受影响的页面 URL 列表（最多 5 个）
    /// </summary>
    public List<string> AffectedUrls { get; set; } = new();

    /// <summary>
    /// 建议的修复方案
    /// </summary>
    public string SuggestedFix { get; set; } = "";
}

/// <summary>
/// 新鲜度优化建议
/// </summary>
public class FreshnessSuggestion
{
    /// <summary>
    /// 优先级：high, medium, low
    /// </summary>
    public string Priority { get; set; } = "medium";

    /// <summary>
    /// 建议标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 建议描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 预期影响
    /// </summary>
    public string ExpectedImpact { get; set; } = "";

    /// <summary>
    /// 实施步骤
    /// </summary>
    public List<string> Steps { get; set; } = new();

    /// <summary>
    /// 预估工作量
    /// </summary>
    public string EstimatedEffort { get; set; } = "";
}

/// <summary>
/// 新鲜度分布项
/// </summary>
public class FreshnessDistributionItem
{
    /// <summary>
    /// 总数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 新鲜数
    /// </summary>
    public int Fresh { get; set; }

    /// <summary>
    /// 需要关注数
    /// </summary>
    public int Attention { get; set; }

    /// <summary>
    /// 警告数
    /// </summary>
    public int Warning { get; set; }

    /// <summary>
    /// 严重数
    /// </summary>
    public int Critical { get; set; }

    /// <summary>
    /// 新鲜率
    /// </summary>
    public double FreshRate { get; set; }
}
