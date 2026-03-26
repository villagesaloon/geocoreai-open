namespace GeoCore.SaaS.Services.Reddit;

#region 5.22 Reddit 帖子生成

/// <summary>
/// Reddit 帖子生成请求
/// </summary>
public class RedditPostRequest
{
    /// <summary>
    /// 原始内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 目标 Subreddit
    /// </summary>
    public string? Subreddit { get; set; }
    
    /// <summary>
    /// 帖子类型：discussion, question, resource, ama
    /// </summary>
    public string PostType { get; set; } = "discussion";
    
    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "en";
}

/// <summary>
/// Reddit 帖子生成结果
/// </summary>
public class RedditPostResult
{
    /// <summary>
    /// 生成的标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 生成的正文
    /// </summary>
    public string Body { get; set; } = "";
    
    /// <summary>
    /// 帖子类型
    /// </summary>
    public string PostType { get; set; } = "";
    
    /// <summary>
    /// 推荐的 Subreddit
    /// </summary>
    public List<SubredditRecommendation> RecommendedSubreddits { get; set; } = new();
    
    /// <summary>
    /// 帖子质量评分
    /// </summary>
    public double QualityScore { get; set; }
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// Subreddit 推荐
/// </summary>
public class SubredditRecommendation
{
    /// <summary>
    /// Subreddit 名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 匹配度评分
    /// </summary>
    public double MatchScore { get; set; }
    
    /// <summary>
    /// 推荐理由
    /// </summary>
    public string Reason { get; set; } = "";
    
    /// <summary>
    /// 发帖规则摘要
    /// </summary>
    public List<string> Rules { get; set; } = new();
}

#endregion

#region 5.23 Reddit 评论生成

/// <summary>
/// Reddit 评论生成请求
/// </summary>
public class RedditCommentRequest
{
    /// <summary>
    /// 原帖内容
    /// </summary>
    public string PostContent { get; set; } = "";
    
    /// <summary>
    /// 要回复的评论（可选）
    /// </summary>
    public string? ParentComment { get; set; }
    
    /// <summary>
    /// 品牌/产品信息
    /// </summary>
    public string? BrandInfo { get; set; }
    
    /// <summary>
    /// 评论风格：helpful, expert, casual, detailed
    /// </summary>
    public string Style { get; set; } = "helpful";
}

/// <summary>
/// Reddit 评论生成结果
/// </summary>
public class RedditCommentResult
{
    /// <summary>
    /// 生成的评论
    /// </summary>
    public string Comment { get; set; } = "";
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 是否在最佳区间 (120-180 词)
    /// </summary>
    public bool IsOptimalLength { get; set; }
    
    /// <summary>
    /// 价值评分
    /// </summary>
    public double ValueScore { get; set; }
    
    /// <summary>
    /// 自我推广检测
    /// </summary>
    public SelfPromotionAnalysis SelfPromotion { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 自我推广分析
/// </summary>
public class SelfPromotionAnalysis
{
    /// <summary>
    /// 是否包含自我推广
    /// </summary>
    public bool HasSelfPromotion { get; set; }
    
    /// <summary>
    /// 推广强度 (0-10)
    /// </summary>
    public double Intensity { get; set; }
    
    /// <summary>
    /// 检测到的推广元素
    /// </summary>
    public List<string> DetectedElements { get; set; } = new();
    
    /// <summary>
    /// 风险等级：low, medium, high
    /// </summary>
    public string RiskLevel { get; set; } = "low";
}

#endregion

#region 5.29 Reddit 暗漏斗监测

/// <summary>
/// 暗漏斗监测配置
/// </summary>
public class DarkFunnelConfig
{
    /// <summary>
    /// 品牌关键词
    /// </summary>
    public List<string> BrandKeywords { get; set; } = new();
    
    /// <summary>
    /// 监测的 Subreddit
    /// </summary>
    public List<string> Subreddits { get; set; } = new();
    
    /// <summary>
    /// 监测周期（天）
    /// </summary>
    public int MonitoringPeriodDays { get; set; } = 90;
    
    /// <summary>
    /// 是否启用 AI 搜索关联
    /// </summary>
    public bool EnableAISearchCorrelation { get; set; } = true;
}

/// <summary>
/// 暗漏斗监测结果
/// </summary>
public class DarkFunnelResult
{
    /// <summary>
    /// 监测周期
    /// </summary>
    public DateRange MonitoringPeriod { get; set; } = new();
    
    /// <summary>
    /// 品牌提及统计
    /// </summary>
    public BrandMentionStats MentionStats { get; set; } = new();
    
    /// <summary>
    /// 情感分析
    /// </summary>
    public SentimentSummary Sentiment { get; set; } = new();
    
    /// <summary>
    /// AI 搜索关联度
    /// </summary>
    public double AISearchCorrelation { get; set; }
    
    /// <summary>
    /// 营销时机建议
    /// </summary>
    public MarketingTimingAdvice TimingAdvice { get; set; } = new();
    
    /// <summary>
    /// 热门讨论
    /// </summary>
    public List<RedditMention> TopMentions { get; set; } = new();
}

/// <summary>
/// 日期范围
/// </summary>
public class DateRange
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

/// <summary>
/// 品牌提及统计
/// </summary>
public class BrandMentionStats
{
    /// <summary>
    /// 总提及数
    /// </summary>
    public int TotalMentions { get; set; }
    
    /// <summary>
    /// 帖子提及数
    /// </summary>
    public int PostMentions { get; set; }
    
    /// <summary>
    /// 评论提及数
    /// </summary>
    public int CommentMentions { get; set; }
    
    /// <summary>
    /// 周趋势
    /// </summary>
    public List<WeeklyTrend> WeeklyTrends { get; set; } = new();
}

/// <summary>
/// 周趋势
/// </summary>
public class WeeklyTrend
{
    public DateTime WeekStart { get; set; }
    public int Mentions { get; set; }
    public double SentimentScore { get; set; }
}

/// <summary>
/// 情感摘要
/// </summary>
public class SentimentSummary
{
    public double OverallScore { get; set; }
    public int PositiveCount { get; set; }
    public int NeutralCount { get; set; }
    public int NegativeCount { get; set; }
}

/// <summary>
/// 营销时机建议
/// </summary>
public class MarketingTimingAdvice
{
    /// <summary>
    /// 是否适合营销
    /// </summary>
    public bool IsReadyForMarketing { get; set; }
    
    /// <summary>
    /// 建议等待天数
    /// </summary>
    public int SuggestedWaitDays { get; set; }
    
    /// <summary>
    /// 当前阶段
    /// </summary>
    public string CurrentStage { get; set; } = "";
    
    /// <summary>
    /// 建议
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Reddit 提及
/// </summary>
public class RedditMention
{
    public string Type { get; set; } = "";
    public string Subreddit { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime Date { get; set; }
    public int Score { get; set; }
    public double SentimentScore { get; set; }
}

#endregion

#region 5.30 自报告归因字段

/// <summary>
/// 自报告归因配置
/// </summary>
public class SelfReportAttributionConfig
{
    /// <summary>
    /// 字段标签
    /// </summary>
    public string FieldLabel { get; set; } = "您是如何知道我们的？";
    
    /// <summary>
    /// 预设选项
    /// </summary>
    public List<AttributionOption> Options { get; set; } = new();
    
    /// <summary>
    /// 是否允许自定义输入
    /// </summary>
    public bool AllowCustomInput { get; set; } = true;
}

/// <summary>
/// 归因选项
/// </summary>
public class AttributionOption
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Category { get; set; } = "";
    public bool IsDarkFunnel { get; set; }
}

/// <summary>
/// 归因统计结果
/// </summary>
public class AttributionStats
{
    /// <summary>
    /// 总响应数
    /// </summary>
    public int TotalResponses { get; set; }
    
    /// <summary>
    /// 按来源分类
    /// </summary>
    public Dictionary<string, int> BySource { get; set; } = new();
    
    /// <summary>
    /// 暗漏斗占比
    /// </summary>
    public double DarkFunnelPercentage { get; set; }
    
    /// <summary>
    /// AI 搜索占比
    /// </summary>
    public double AISearchPercentage { get; set; }
    
    /// <summary>
    /// Reddit 占比
    /// </summary>
    public double RedditPercentage { get; set; }
}

#endregion

#region 5.33 Reddit 参与度评估

/// <summary>
/// Reddit 参与度评估请求
/// </summary>
public class EngagementAssessmentRequest
{
    /// <summary>
    /// Reddit 用户名
    /// </summary>
    public string? Username { get; set; }
    
    /// <summary>
    /// 品牌名称
    /// </summary>
    public string BrandName { get; set; } = "";
    
    /// <summary>
    /// 目标 Subreddit
    /// </summary>
    public List<string> TargetSubreddits { get; set; } = new();
}

/// <summary>
/// Reddit 参与度评估结果
/// </summary>
public class EngagementAssessmentResult
{
    /// <summary>
    /// 当前阶段 (1-5)
    /// </summary>
    public int CurrentStage { get; set; }
    
    /// <summary>
    /// 阶段名称
    /// </summary>
    public string StageName { get; set; } = "";
    
    /// <summary>
    /// 阶段描述
    /// </summary>
    public string StageDescription { get; set; } = "";
    
    /// <summary>
    /// 阶段进度 (0-100)
    /// </summary>
    public double StageProgress { get; set; }
    
    /// <summary>
    /// 各阶段详情
    /// </summary>
    public List<EngagementStage> Stages { get; set; } = new();
    
    /// <summary>
    /// 下一步行动建议
    /// </summary>
    public List<string> NextActions { get; set; } = new();
    
    /// <summary>
    /// 风险提示
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 参与度阶段
/// </summary>
public class EngagementStage
{
    /// <summary>
    /// 阶段编号
    /// </summary>
    public int StageNumber { get; set; }
    
    /// <summary>
    /// 阶段名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 阶段描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>
    /// 是否当前阶段
    /// </summary>
    public bool IsCurrent { get; set; }
    
    /// <summary>
    /// 完成标准
    /// </summary>
    public List<string> Criteria { get; set; } = new();
    
    /// <summary>
    /// 建议行动
    /// </summary>
    public List<string> Actions { get; set; } = new();
}

#endregion
