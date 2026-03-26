namespace GeoCore.SaaS.Services.CitationSource;

/// <summary>
/// 引用来源分析报告
/// </summary>
public class CitationSourceReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Brand { get; set; } = "";
    public int TaskId { get; set; }
    
    /// <summary>
    /// 引用来源统计
    /// </summary>
    public List<SourceStatistics> SourceStats { get; set; } = new();
    
    /// <summary>
    /// 引用集中度分析
    /// </summary>
    public ConcentrationAnalysis Concentration { get; set; } = new();
    
    /// <summary>
    /// 竞品引用来源对比
    /// </summary>
    public List<CompetitorSourceComparison> CompetitorSources { get; set; } = new();
    
    /// <summary>
    /// Reddit 活跃度分析
    /// </summary>
    public RedditActivityAnalysis RedditActivity { get; set; } = new();
    
    /// <summary>
    /// 来源趋势
    /// </summary>
    public List<SourceTrend> SourceTrends { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<SourceOptimizationSuggestion> Suggestions { get; set; } = new();
    
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
/// 来源统计
/// </summary>
public class SourceStatistics
{
    /// <summary>
    /// 来源域名
    /// </summary>
    public string Domain { get; set; } = "";
    
    /// <summary>
    /// 来源类型：reddit, quora, wikipedia, news, blog, official, forum, social
    /// </summary>
    public string SourceType { get; set; } = "";
    
    /// <summary>
    /// 引用次数
    /// </summary>
    public int CitationCount { get; set; }
    
    /// <summary>
    /// 引用占比
    /// </summary>
    public double CitationRate { get; set; }
    
    /// <summary>
    /// 引用该来源的 AI 平台
    /// </summary>
    public List<string> CitedByPlatforms { get; set; } = new();
    
    /// <summary>
    /// 平均情感评分
    /// </summary>
    public double AverageSentiment { get; set; }
    
    /// <summary>
    /// 示例 URL
    /// </summary>
    public List<string> ExampleUrls { get; set; } = new();
    
    /// <summary>
    /// 信任度评分 (0-100)
    /// </summary>
    public double TrustScore { get; set; }
}

/// <summary>
/// 引用集中度分析
/// </summary>
public class ConcentrationAnalysis
{
    /// <summary>
    /// 集中度指数 (0-1)，越高表示来源越集中
    /// </summary>
    public double ConcentrationIndex { get; set; }
    
    /// <summary>
    /// 集中度级别：high, medium, low
    /// </summary>
    public string ConcentrationLevel { get; set; } = "medium";
    
    /// <summary>
    /// 主要来源（占比 > 20%）
    /// </summary>
    public List<SourceStatistics> PrimarySources { get; set; } = new();
    
    /// <summary>
    /// 次要来源（占比 5-20%）
    /// </summary>
    public List<SourceStatistics> SecondarySources { get; set; } = new();
    
    /// <summary>
    /// 长尾来源（占比 < 5%）
    /// </summary>
    public List<SourceStatistics> LongTailSources { get; set; } = new();
    
    /// <summary>
    /// 来源多样性评分 (0-100)
    /// </summary>
    public double DiversityScore { get; set; }
    
    /// <summary>
    /// 分析说明
    /// </summary>
    public string Analysis { get; set; } = "";
}

/// <summary>
/// 竞品引用来源对比
/// </summary>
public class CompetitorSourceComparison
{
    /// <summary>
    /// 竞品名称
    /// </summary>
    public string Competitor { get; set; } = "";
    
    /// <summary>
    /// 竞品主要来源
    /// </summary>
    public List<SourceStatistics> TopSources { get; set; } = new();
    
    /// <summary>
    /// 共同来源
    /// </summary>
    public List<string> SharedSources { get; set; } = new();
    
    /// <summary>
    /// 竞品独有来源（我们未被引用的）
    /// </summary>
    public List<string> CompetitorOnlySources { get; set; } = new();
    
    /// <summary>
    /// 我们独有来源（竞品未被引用的）
    /// </summary>
    public List<string> OurOnlySources { get; set; } = new();
    
    /// <summary>
    /// 来源优势对比
    /// </summary>
    public string Verdict { get; set; } = ""; // "领先", "持平", "落后"
}

/// <summary>
/// Reddit 活跃度分析
/// </summary>
public class RedditActivityAnalysis
{
    /// <summary>
    /// Reddit 引用占比
    /// </summary>
    public double RedditCitationRate { get; set; }
    
    /// <summary>
    /// 活跃的 Subreddit
    /// </summary>
    public List<SubredditActivity> ActiveSubreddits { get; set; } = new();
    
    /// <summary>
    /// Reddit 情感分布
    /// </summary>
    public SentimentDistribution Sentiment { get; set; } = new();
    
    /// <summary>
    /// 讨论热度趋势
    /// </summary>
    public string TrendDirection { get; set; } = "stable"; // rising, stable, declining
    
    /// <summary>
    /// 建议参与的 Subreddit
    /// </summary>
    public List<SubredditRecommendation> RecommendedSubreddits { get; set; } = new();
    
    /// <summary>
    /// 分析说明
    /// </summary>
    public string Analysis { get; set; } = "";
}

/// <summary>
/// Subreddit 活跃度
/// </summary>
public class SubredditActivity
{
    /// <summary>
    /// Subreddit 名称
    /// </summary>
    public string Subreddit { get; set; } = "";
    
    /// <summary>
    /// 提及次数
    /// </summary>
    public int MentionCount { get; set; }
    
    /// <summary>
    /// 情感评分
    /// </summary>
    public double SentimentScore { get; set; }
    
    /// <summary>
    /// 最近活跃时间
    /// </summary>
    public DateTime? LastActivity { get; set; }
    
    /// <summary>
    /// 相关问题
    /// </summary>
    public List<string> RelatedQuestions { get; set; } = new();
}

/// <summary>
/// Subreddit 推荐
/// </summary>
public class SubredditRecommendation
{
    public string Subreddit { get; set; } = "";
    public string Reason { get; set; } = "";
    public int EstimatedReach { get; set; }
    public string SuggestedAction { get; set; } = "";
}

/// <summary>
/// 情感分布
/// </summary>
public class SentimentDistribution
{
    public double PositiveRate { get; set; }
    public double NeutralRate { get; set; }
    public double NegativeRate { get; set; }
    public double AverageScore { get; set; }
}

/// <summary>
/// 来源趋势
/// </summary>
public class SourceTrend
{
    public string Domain { get; set; } = "";
    public string TrendDirection { get; set; } = "stable"; // rising, stable, declining
    public double ChangeRate { get; set; } // 变化率
    public List<TrendDataPoint> DataPoints { get; set; } = new();
}

/// <summary>
/// 趋势数据点
/// </summary>
public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public int CitationCount { get; set; }
    public double CitationRate { get; set; }
}

/// <summary>
/// 来源优化建议
/// </summary>
public class SourceOptimizationSuggestion
{
    /// <summary>
    /// 建议类型：increase_presence, improve_content, new_source, monitor
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 目标来源
    /// </summary>
    public string TargetSource { get; set; } = "";
    
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
    /// 难度：easy, medium, hard
    /// </summary>
    public string Difficulty { get; set; } = "medium";
}

/// <summary>
/// 引用来源分析请求
/// </summary>
public class CitationSourceRequest
{
    public int TaskId { get; set; }
    public string Brand { get; set; } = "";
    public List<string>? Competitors { get; set; }
    public bool IncludeRedditAnalysis { get; set; } = true;
    public bool IncludeTrends { get; set; } = true;
}
