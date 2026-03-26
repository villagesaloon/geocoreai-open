namespace GeoCore.SaaS.Services.ContentReuse;

#region 5.18 内容复用工作流

/// <summary>
/// 内容复用请求
/// </summary>
public class ContentReuseRequest
{
    /// <summary>
    /// 原始长文内容
    /// </summary>
    public string OriginalContent { get; set; } = "";
    
    /// <summary>
    /// 内容标题
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// 目标平台列表
    /// </summary>
    public List<string> TargetPlatforms { get; set; } = new();
    
    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh";
    
    /// <summary>
    /// 品牌信息（用于适度提及）
    /// </summary>
    public string? BrandInfo { get; set; }
}

/// <summary>
/// 内容复用结果
/// </summary>
public class ContentReuseResult
{
    /// <summary>
    /// 原始内容摘要
    /// </summary>
    public ContentSummary OriginalSummary { get; set; } = new();
    
    /// <summary>
    /// 各平台转换结果
    /// </summary>
    public List<PlatformContent> PlatformContents { get; set; } = new();
    
    /// <summary>
    /// 复用建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 内容摘要
/// </summary>
public class ContentSummary
{
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 检测到的内容类型
    /// </summary>
    public string ContentType { get; set; } = "";
    
    /// <summary>
    /// 核心要点
    /// </summary>
    public List<string> KeyPoints { get; set; } = new();
    
    /// <summary>
    /// 提取的实体
    /// </summary>
    public List<string> Entities { get; set; } = new();
}

/// <summary>
/// 平台内容
/// </summary>
public class PlatformContent
{
    /// <summary>
    /// 平台名称
    /// </summary>
    public string Platform { get; set; } = "";
    
    /// <summary>
    /// 平台显示名称
    /// </summary>
    public string PlatformDisplayName { get; set; } = "";
    
    /// <summary>
    /// 转换后的标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 转换后的内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 是否符合平台最佳实践
    /// </summary>
    public bool MeetsBestPractices { get; set; }
    
    /// <summary>
    /// 平台特定建议
    /// </summary>
    public List<string> PlatformTips { get; set; } = new();
    
    /// <summary>
    /// 推荐的标签/话题
    /// </summary>
    public List<string> SuggestedTags { get; set; } = new();
}

#endregion

#region 5.19 平台选择建议

/// <summary>
/// 平台选择请求
/// </summary>
public class PlatformSelectionRequest
{
    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 内容类型（可选，自动检测）
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// 目标受众
    /// </summary>
    public string? TargetAudience { get; set; }
    
    /// <summary>
    /// 营销目标：awareness, engagement, conversion
    /// </summary>
    public string? MarketingGoal { get; set; }
}

/// <summary>
/// 平台选择结果
/// </summary>
public class PlatformSelectionResult
{
    /// <summary>
    /// 检测到的内容类型
    /// </summary>
    public string DetectedContentType { get; set; } = "";
    
    /// <summary>
    /// 推荐的平台列表
    /// </summary>
    public List<PlatformRecommendation> Recommendations { get; set; } = new();
    
    /// <summary>
    /// 不推荐的平台
    /// </summary>
    public List<PlatformWarning> NotRecommended { get; set; } = new();
}

/// <summary>
/// 平台推荐
/// </summary>
public class PlatformRecommendation
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
    /// 匹配度评分 (0-100)
    /// </summary>
    public double MatchScore { get; set; }
    
    /// <summary>
    /// 推荐理由
    /// </summary>
    public List<string> Reasons { get; set; } = new();
    
    /// <summary>
    /// 最佳实践
    /// </summary>
    public List<string> BestPractices { get; set; } = new();
    
    /// <summary>
    /// 预期效果
    /// </summary>
    public string ExpectedOutcome { get; set; } = "";
    
    /// <summary>
    /// 优先级：high, medium, low
    /// </summary>
    public string Priority { get; set; } = "medium";
}

/// <summary>
/// 平台警告
/// </summary>
public class PlatformWarning
{
    /// <summary>
    /// 平台名称
    /// </summary>
    public string Platform { get; set; } = "";
    
    /// <summary>
    /// 不推荐原因
    /// </summary>
    public string Reason { get; set; } = "";
}

/// <summary>
/// 平台配置
/// </summary>
public class PlatformConfig
{
    /// <summary>
    /// 平台 ID
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// 平台名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 最佳内容长度范围
    /// </summary>
    public (int Min, int Max) OptimalLength { get; set; }
    
    /// <summary>
    /// 适合的内容类型
    /// </summary>
    public List<string> SuitableContentTypes { get; set; } = new();
    
    /// <summary>
    /// 平台特点
    /// </summary>
    public List<string> Characteristics { get; set; } = new();
    
    /// <summary>
    /// 最佳实践
    /// </summary>
    public List<string> BestPractices { get; set; } = new();
    
    /// <summary>
    /// AI 引用潜力
    /// </summary>
    public double AICitationPotential { get; set; }
}

#endregion
