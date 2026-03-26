namespace GeoCore.SaaS.Services.AICrawlerAudit;

/// <summary>
/// AI 爬虫审计请求
/// </summary>
public class AICrawlerAuditRequest
{
    /// <summary>
    /// 要审计的网站 URL（会自动获取 robots.txt）
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// 直接提供的 robots.txt 内容（可选，如果提供则不会去获取）
    /// </summary>
    public string? RobotsTxtContent { get; set; }

    /// <summary>
    /// 项目 ID（可选，用于关联项目）
    /// </summary>
    public string? ProjectId { get; set; }
}

/// <summary>
/// AI 爬虫审计报告
/// </summary>
public class AICrawlerAuditReport
{
    /// <summary>
    /// 审计的网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// 审计时间
    /// </summary>
    public DateTime AuditTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// robots.txt 是否存在
    /// </summary>
    public bool RobotsTxtExists { get; set; }

    /// <summary>
    /// robots.txt 原始内容
    /// </summary>
    public string? RobotsTxtContent { get; set; }

    /// <summary>
    /// 整体评分（0-100）
    /// </summary>
    public int OverallScore { get; set; }

    /// <summary>
    /// 评分等级：excellent, good, warning, critical
    /// </summary>
    public string ScoreLevel { get; set; } = "unknown";

    /// <summary>
    /// 各 AI 爬虫的状态
    /// </summary>
    public List<AICrawlerStatus> Crawlers { get; set; } = new();

    /// <summary>
    /// 统计摘要
    /// </summary>
    public AuditSummary Summary { get; set; } = new();

    /// <summary>
    /// 审计问题和建议
    /// </summary>
    public List<AuditIssue> Issues { get; set; } = new();

    /// <summary>
    /// 优化建议
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// AI 爬虫状态
/// </summary>
public class AICrawlerStatus
{
    /// <summary>
    /// 爬虫名称（如 GPTBot）
    /// </summary>
    public string CrawlerName { get; set; } = "";

    /// <summary>
    /// 所属公司/平台
    /// </summary>
    public string Company { get; set; } = "";

    /// <summary>
    /// 平台名称（如 ChatGPT）
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 爬虫用途
    /// </summary>
    public string Purpose { get; set; } = "";

    /// <summary>
    /// 状态：allowed（允许）, blocked（禁止）, partial（部分限制）, not_configured（未配置）
    /// </summary>
    public string Status { get; set; } = "not_configured";

    /// <summary>
    /// 是否在 robots.txt 中明确配置
    /// </summary>
    public bool IsExplicitlyConfigured { get; set; }

    /// <summary>
    /// 相关的 robots.txt 规则
    /// </summary>
    public List<string> Rules { get; set; } = new();

    /// <summary>
    /// 重要性：high, medium, low
    /// </summary>
    public string Importance { get; set; } = "medium";

    /// <summary>
    /// 流量占比估算
    /// </summary>
    public double TrafficShare { get; set; }

    /// <summary>
    /// 建议操作
    /// </summary>
    public string Recommendation { get; set; } = "";
}

/// <summary>
/// 审计摘要
/// </summary>
public class AuditSummary
{
    /// <summary>
    /// 检测的爬虫总数
    /// </summary>
    public int TotalCrawlers { get; set; }

    /// <summary>
    /// 允许的爬虫数
    /// </summary>
    public int AllowedCount { get; set; }

    /// <summary>
    /// 禁止的爬虫数
    /// </summary>
    public int BlockedCount { get; set; }

    /// <summary>
    /// 部分限制的爬虫数
    /// </summary>
    public int PartialCount { get; set; }

    /// <summary>
    /// 未配置的爬虫数
    /// </summary>
    public int NotConfiguredCount { get; set; }

    /// <summary>
    /// 高重要性爬虫中被禁止的数量
    /// </summary>
    public int HighImportanceBlocked { get; set; }

    /// <summary>
    /// 预估的 AI 流量可达性（0-100%）
    /// </summary>
    public double EstimatedAITrafficReach { get; set; }
}

/// <summary>
/// 审计问题
/// </summary>
public class AuditIssue
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
    /// 受影响的爬虫
    /// </summary>
    public List<string> AffectedCrawlers { get; set; } = new();

    /// <summary>
    /// 建议的修复方案
    /// </summary>
    public string SuggestedFix { get; set; } = "";
}

/// <summary>
/// 优化建议
/// </summary>
public class OptimizationSuggestion
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
    /// 示例代码/配置
    /// </summary>
    public string? ExampleCode { get; set; }
}

/// <summary>
/// AI 爬虫定义
/// </summary>
public class AICrawlerDefinition
{
    public string Name { get; set; } = "";
    public string Company { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Purpose { get; set; } = "";
    public string Importance { get; set; } = "medium";
    public double TrafficShare { get; set; }
    public List<string> AlternativeNames { get; set; } = new();
}
