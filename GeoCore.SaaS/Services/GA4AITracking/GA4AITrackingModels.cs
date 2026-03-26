namespace GeoCore.SaaS.Services.GA4AITracking;

/// <summary>
/// GA4 AI 流量追踪配置请求
/// </summary>
public class GA4AITrackingConfigRequest
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// GA4 Measurement ID（如 G-XXXXXXXXXX）
    /// </summary>
    public string? MeasurementId { get; set; }

    /// <summary>
    /// 项目 ID（可选）
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// 是否包含自定义事件代码
    /// </summary>
    public bool IncludeCustomEvents { get; set; } = true;

    /// <summary>
    /// 是否包含 GTM 配置
    /// </summary>
    public bool IncludeGTMConfig { get; set; } = true;
}

/// <summary>
/// GA4 AI 流量追踪配置结果
/// </summary>
public class GA4AITrackingConfigResult
{
    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// AI 流量来源定义
    /// </summary>
    public List<AITrafficSource> TrafficSources { get; set; } = new();

    /// <summary>
    /// GA4 自定义渠道分组配置
    /// </summary>
    public GA4ChannelGroupConfig ChannelGroupConfig { get; set; } = new();

    /// <summary>
    /// GA4 自定义事件代码
    /// </summary>
    public string? CustomEventCode { get; set; }

    /// <summary>
    /// GTM 配置 JSON
    /// </summary>
    public string? GTMConfigJson { get; set; }

    /// <summary>
    /// 实施步骤
    /// </summary>
    public List<ImplementationStep> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// 推荐的报告和仪表板
    /// </summary>
    public List<RecommendedReport> RecommendedReports { get; set; } = new();
}

/// <summary>
/// AI 流量来源定义
/// </summary>
public class AITrafficSource
{
    /// <summary>
    /// 来源名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 平台
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// Referrer 匹配模式（正则表达式）
    /// </summary>
    public List<string> ReferrerPatterns { get; set; } = new();

    /// <summary>
    /// User-Agent 匹配模式
    /// </summary>
    public List<string> UserAgentPatterns { get; set; } = new();

    /// <summary>
    /// 流量类型：direct, referral, organic
    /// </summary>
    public string TrafficType { get; set; } = "referral";

    /// <summary>
    /// 预估流量占比
    /// </summary>
    public double EstimatedShare { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// GA4 自定义渠道分组配置
/// </summary>
public class GA4ChannelGroupConfig
{
    /// <summary>
    /// 渠道分组名称
    /// </summary>
    public string GroupName { get; set; } = "AI Traffic";

    /// <summary>
    /// 渠道规则
    /// </summary>
    public List<ChannelRule> Rules { get; set; } = new();

    /// <summary>
    /// GA4 Admin 配置说明
    /// </summary>
    public string ConfigurationInstructions { get; set; } = "";
}

/// <summary>
/// 渠道规则
/// </summary>
public class ChannelRule
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 条件类型：source, medium, campaign, referrer
    /// </summary>
    public string ConditionType { get; set; } = "source";

    /// <summary>
    /// 匹配操作：contains, exactly_matches, regex_matches
    /// </summary>
    public string MatchType { get; set; } = "contains";

    /// <summary>
    /// 匹配值
    /// </summary>
    public string MatchValue { get; set; } = "";

    /// <summary>
    /// 优先级（数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// 实施步骤
/// </summary>
public class ImplementationStep
{
    /// <summary>
    /// 步骤编号
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// 步骤标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 步骤描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 详细操作
    /// </summary>
    public List<string> Actions { get; set; } = new();

    /// <summary>
    /// 代码片段（如果有）
    /// </summary>
    public string? CodeSnippet { get; set; }

    /// <summary>
    /// 预估时间（分钟）
    /// </summary>
    public int EstimatedMinutes { get; set; }
}

/// <summary>
/// 推荐的报告
/// </summary>
public class RecommendedReport
{
    /// <summary>
    /// 报告名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 报告描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 报告类型：exploration, standard, funnel
    /// </summary>
    public string ReportType { get; set; } = "exploration";

    /// <summary>
    /// 维度
    /// </summary>
    public List<string> Dimensions { get; set; } = new();

    /// <summary>
    /// 指标
    /// </summary>
    public List<string> Metrics { get; set; } = new();

    /// <summary>
    /// 创建说明
    /// </summary>
    public string CreationInstructions { get; set; } = "";
}
