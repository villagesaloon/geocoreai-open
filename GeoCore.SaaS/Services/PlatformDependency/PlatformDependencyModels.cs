namespace GeoCore.SaaS.Services.PlatformDependency;

/// <summary>
/// 平台依赖度分析请求
/// </summary>
public class PlatformDependencyRequest
{
    /// <summary>
    /// 监测任务 ID
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// 品牌名称
    /// </summary>
    public string Brand { get; set; } = "";

    /// <summary>
    /// 是否包含历史趋势
    /// </summary>
    public bool IncludeTrends { get; set; } = true;

    /// <summary>
    /// 是否包含分散策略建议
    /// </summary>
    public bool IncludeStrategies { get; set; } = true;

    /// <summary>
    /// 依赖度警告阈值（默认 50%）
    /// </summary>
    public double DependencyThreshold { get; set; } = 0.5;
}

/// <summary>
/// 平台依赖度分析报告
/// </summary>
public class PlatformDependencyReport
{
    /// <summary>
    /// 报告 ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 任务 ID
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// 品牌名称
    /// </summary>
    public string Brand { get; set; } = "";

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 整体依赖度评分（0-100，越高越分散）
    /// </summary>
    public double DiversificationScore { get; set; }

    /// <summary>
    /// 依赖度等级
    /// </summary>
    public string DependencyLevel { get; set; } = "balanced";

    /// <summary>
    /// 各平台曝光占比
    /// </summary>
    public List<PlatformExposure> Platforms { get; set; } = new();

    /// <summary>
    /// 依赖度警告
    /// </summary>
    public List<DependencyAlert> Alerts { get; set; } = new();

    /// <summary>
    /// 分散策略建议
    /// </summary>
    public List<DiversificationStrategy> Strategies { get; set; } = new();

    /// <summary>
    /// 历史趋势
    /// </summary>
    public DependencyTrend Trend { get; set; } = new();

    /// <summary>
    /// 报告摘要
    /// </summary>
    public string Summary { get; set; } = "";
}

/// <summary>
/// 平台曝光数据
/// </summary>
public class PlatformExposure
{
    /// <summary>
    /// 平台名称
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 平台显示名称
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// 引用次数
    /// </summary>
    public int CitationCount { get; set; }

    /// <summary>
    /// 曝光占比（0-1）
    /// </summary>
    public double ExposureRate { get; set; }

    /// <summary>
    /// 是否过度依赖
    /// </summary>
    public bool IsOverDependent { get; set; }

    /// <summary>
    /// 平台健康度评分（基于平台生命周期）
    /// </summary>
    public double HealthScore { get; set; }

    /// <summary>
    /// 平台生命周期阶段
    /// </summary>
    public string LifecycleStage { get; set; } = "mature";

    /// <summary>
    /// 平台风险等级
    /// </summary>
    public string RiskLevel { get; set; } = "low";

    /// <summary>
    /// 趋势方向
    /// </summary>
    public string TrendDirection { get; set; } = "stable";

    /// <summary>
    /// 与上期对比变化
    /// </summary>
    public double ChangeFromPrevious { get; set; }
}

/// <summary>
/// 依赖度警告
/// </summary>
public class DependencyAlert
{
    /// <summary>
    /// 警告类型
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 警告级别
    /// </summary>
    public string Level { get; set; } = "warning";

    /// <summary>
    /// 警告标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 警告描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 相关平台
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 当前占比
    /// </summary>
    public double CurrentRate { get; set; }

    /// <summary>
    /// 建议操作
    /// </summary>
    public string SuggestedAction { get; set; } = "";
}

/// <summary>
/// 分散策略建议
/// </summary>
public class DiversificationStrategy
{
    /// <summary>
    /// 策略类型
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 策略标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 策略描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 目标平台
    /// </summary>
    public string TargetPlatform { get; set; } = "";

    /// <summary>
    /// 优先级（1-10）
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 预期效果
    /// </summary>
    public string ExpectedOutcome { get; set; } = "";

    /// <summary>
    /// 实施难度
    /// </summary>
    public string Effort { get; set; } = "medium";

    /// <summary>
    /// 具体行动步骤
    /// </summary>
    public List<string> ActionSteps { get; set; } = new();

    /// <summary>
    /// 建议时间线
    /// </summary>
    public string SuggestedTimeline { get; set; } = "";
}

/// <summary>
/// 依赖度趋势
/// </summary>
public class DependencyTrend
{
    /// <summary>
    /// 趋势方向
    /// </summary>
    public string Direction { get; set; } = "stable";

    /// <summary>
    /// 分散度变化
    /// </summary>
    public double DiversificationChange { get; set; }

    /// <summary>
    /// 趋势描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 数据点
    /// </summary>
    public List<DependencyDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// 依赖度数据点
/// </summary>
public class DependencyDataPoint
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 分散度评分
    /// </summary>
    public double DiversificationScore { get; set; }

    /// <summary>
    /// 主导平台
    /// </summary>
    public string DominantPlatform { get; set; } = "";

    /// <summary>
    /// 主导平台占比
    /// </summary>
    public double DominantRate { get; set; }
}

/// <summary>
/// 平台生命周期配置
/// </summary>
public class PlatformLifecycleConfig
{
    /// <summary>
    /// 平台名称
    /// </summary>
    public string Platform { get; set; } = "";

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// 生命周期阶段
    /// </summary>
    public string Stage { get; set; } = "mature";

    /// <summary>
    /// 健康度评分（0-100）
    /// </summary>
    public double HealthScore { get; set; } = 80;

    /// <summary>
    /// 风险等级
    /// </summary>
    public string RiskLevel { get; set; } = "low";

    /// <summary>
    /// 趋势方向
    /// </summary>
    public string TrendDirection { get; set; } = "stable";

    /// <summary>
    /// 备注说明
    /// </summary>
    public string Notes { get; set; } = "";
}
