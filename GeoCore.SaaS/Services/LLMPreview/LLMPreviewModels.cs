namespace GeoCore.SaaS.Services.LLMPreview;

#region 3.31 LLM 预览模拟

/// <summary>
/// LLM 预览请求
/// </summary>
public class LLMPreviewRequest
{
    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// 目标 LLM 平台
    /// </summary>
    public List<string> TargetPlatforms { get; set; } = new() { "chatgpt", "perplexity", "claude", "gemini" };
    
    /// <summary>
    /// 模拟的用户问题
    /// </summary>
    public string? SimulatedQuery { get; set; }
}

/// <summary>
/// LLM 预览结果
/// </summary>
public class LLMPreviewResult
{
    /// <summary>
    /// 各平台预览
    /// </summary>
    public List<PlatformPreview> Previews { get; set; } = new();
    
    /// <summary>
    /// 综合可引用性评分
    /// </summary>
    public double OverallCitabilityScore { get; set; }
    
    /// <summary>
    /// 预测的引用片段
    /// </summary>
    public List<PredictedCitation> PredictedCitations { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<PreviewSuggestion> Suggestions { get; set; } = new();
}

/// <summary>
/// 平台预览
/// </summary>
public class PlatformPreview
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
    /// 模拟的回答
    /// </summary>
    public string SimulatedResponse { get; set; } = "";
    
    /// <summary>
    /// 是否可能被引用
    /// </summary>
    public bool LikelyCited { get; set; }
    
    /// <summary>
    /// 引用概率 (0-100)
    /// </summary>
    public double CitationProbability { get; set; }
    
    /// <summary>
    /// 预测的引用位置
    /// </summary>
    public string CitationPosition { get; set; } = "";
    
    /// <summary>
    /// 平台特定建议
    /// </summary>
    public List<string> PlatformTips { get; set; } = new();
}

/// <summary>
/// 预测的引用片段
/// </summary>
public class PredictedCitation
{
    /// <summary>
    /// 引用文本
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 在原文中的位置
    /// </summary>
    public int StartPosition { get; set; }
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 引用概率
    /// </summary>
    public double Probability { get; set; }
    
    /// <summary>
    /// 引用原因
    /// </summary>
    public string Reason { get; set; } = "";
}

/// <summary>
/// 预览建议
/// </summary>
public class PreviewSuggestion
{
    /// <summary>
    /// 优先级
    /// </summary>
    public string Priority { get; set; } = "medium";
    
    /// <summary>
    /// 建议内容
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// 预期影响
    /// </summary>
    public string Impact { get; set; } = "";
    
    /// <summary>
    /// 相关平台
    /// </summary>
    public List<string> AffectedPlatforms { get; set; } = new();
}

#endregion

#region 4.54 持续优化循环

/// <summary>
/// 优化循环配置
/// </summary>
public class OptimizationLoopConfig
{
    /// <summary>
    /// 项目 ID
    /// </summary>
    public string ProjectId { get; set; } = "";
    
    /// <summary>
    /// 监测的 URL 列表
    /// </summary>
    public List<string> MonitoredUrls { get; set; } = new();
    
    /// <summary>
    /// 监测的关键词
    /// </summary>
    public List<string> Keywords { get; set; } = new();
    
    /// <summary>
    /// 检测频率（小时）
    /// </summary>
    public int CheckIntervalHours { get; set; } = 24;
    
    /// <summary>
    /// 是否启用自动修复建议
    /// </summary>
    public bool EnableAutoFix { get; set; } = false;
}

/// <summary>
/// 优化循环状态
/// </summary>
public class OptimizationLoopStatus
{
    /// <summary>
    /// 项目 ID
    /// </summary>
    public string ProjectId { get; set; } = "";
    
    /// <summary>
    /// 当前阶段：detect, fix, verify
    /// </summary>
    public string CurrentStage { get; set; } = "detect";
    
    /// <summary>
    /// 上次检测时间
    /// </summary>
    public DateTime? LastDetectionTime { get; set; }
    
    /// <summary>
    /// 检测到的问题
    /// </summary>
    public List<DetectedIssue> DetectedIssues { get; set; } = new();
    
    /// <summary>
    /// 待验证的修复
    /// </summary>
    public List<PendingFix> PendingFixes { get; set; } = new();
    
    /// <summary>
    /// 已验证的改进
    /// </summary>
    public List<VerifiedImprovement> VerifiedImprovements { get; set; } = new();
    
    /// <summary>
    /// 下一步行动
    /// </summary>
    public List<string> NextActions { get; set; } = new();
}

/// <summary>
/// 检测到的问题
/// </summary>
public class DetectedIssue
{
    /// <summary>
    /// 问题 ID
    /// </summary>
    public string IssueId { get; set; } = "";
    
    /// <summary>
    /// 问题类型
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 问题描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 严重程度
    /// </summary>
    public string Severity { get; set; } = "medium";
    
    /// <summary>
    /// 影响的 URL
    /// </summary>
    public string AffectedUrl { get; set; } = "";
    
    /// <summary>
    /// 建议的修复
    /// </summary>
    public string SuggestedFix { get; set; } = "";
    
    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime DetectedAt { get; set; }
}

/// <summary>
/// 待验证的修复
/// </summary>
public class PendingFix
{
    /// <summary>
    /// 修复 ID
    /// </summary>
    public string FixId { get; set; } = "";
    
    /// <summary>
    /// 关联的问题 ID
    /// </summary>
    public string IssueId { get; set; } = "";
    
    /// <summary>
    /// 修复描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 应用时间
    /// </summary>
    public DateTime AppliedAt { get; set; }
    
    /// <summary>
    /// 预期验证时间
    /// </summary>
    public DateTime ExpectedVerificationTime { get; set; }
}

/// <summary>
/// 已验证的改进
/// </summary>
public class VerifiedImprovement
{
    /// <summary>
    /// 改进 ID
    /// </summary>
    public string ImprovementId { get; set; } = "";
    
    /// <summary>
    /// 关联的修复 ID
    /// </summary>
    public string FixId { get; set; } = "";
    
    /// <summary>
    /// 改进描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 改进前指标
    /// </summary>
    public double BeforeMetric { get; set; }
    
    /// <summary>
    /// 改进后指标
    /// </summary>
    public double AfterMetric { get; set; }
    
    /// <summary>
    /// 改进百分比
    /// </summary>
    public double ImprovementPercentage { get; set; }
    
    /// <summary>
    /// 验证时间
    /// </summary>
    public DateTime VerifiedAt { get; set; }
}

#endregion
