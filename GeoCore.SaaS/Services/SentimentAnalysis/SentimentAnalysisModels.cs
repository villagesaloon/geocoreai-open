namespace GeoCore.SaaS.Services.SentimentAnalysis;

/// <summary>
/// 情感分析报告
/// </summary>
public class SentimentAnalysisReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Brand { get; set; } = "";
    public int TaskId { get; set; }
    
    /// <summary>
    /// 多维情感分析
    /// </summary>
    public MultiDimensionalSentiment Sentiment { get; set; } = new();
    
    /// <summary>
    /// 情感趋势追踪
    /// </summary>
    public SentimentTrendAnalysis Trends { get; set; } = new();
    
    /// <summary>
    /// 竞品情感对比
    /// </summary>
    public List<CompetitorSentimentComparison> CompetitorComparisons { get; set; } = new();
    
    /// <summary>
    /// 情感关键词
    /// </summary>
    public List<SentimentKeyword> Keywords { get; set; } = new();
    
    /// <summary>
    /// 平台情感分布
    /// </summary>
    public List<PlatformSentiment> PlatformBreakdown { get; set; } = new();
    
    /// <summary>
    /// 情感预警
    /// </summary>
    public List<SentimentAlert> Alerts { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<SentimentOptimizationSuggestion> Suggestions { get; set; } = new();
    
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
/// 多维情感分析 (4.33)
/// </summary>
public class MultiDimensionalSentiment
{
    /// <summary>
    /// 整体情感评分 (-1 到 1)
    /// </summary>
    public double OverallScore { get; set; }
    
    /// <summary>
    /// 情感级别：very_positive, positive, neutral, negative, very_negative
    /// </summary>
    public string OverallLevel { get; set; } = "neutral";
    
    /// <summary>
    /// 正面情感分布
    /// </summary>
    public SentimentDimension Positive { get; set; } = new();
    
    /// <summary>
    /// 中性情感分布
    /// </summary>
    public SentimentDimension Neutral { get; set; } = new();
    
    /// <summary>
    /// 负面情感分布
    /// </summary>
    public SentimentDimension Negative { get; set; } = new();
    
    /// <summary>
    /// 情感强度 (0-1)
    /// </summary>
    public double Intensity { get; set; }
    
    /// <summary>
    /// 情感一致性 (0-1)，越高表示各平台情感越一致
    /// </summary>
    public double Consistency { get; set; }
    
    /// <summary>
    /// 情感细分
    /// </summary>
    public List<SentimentCategory> Categories { get; set; } = new();
}

/// <summary>
/// 情感维度
/// </summary>
public class SentimentDimension
{
    /// <summary>
    /// 占比
    /// </summary>
    public double Rate { get; set; }
    
    /// <summary>
    /// 数量
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// 平均强度
    /// </summary>
    public double AverageIntensity { get; set; }
    
    /// <summary>
    /// 代表性表述
    /// </summary>
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// 情感细分类别
/// </summary>
public class SentimentCategory
{
    /// <summary>
    /// 类别名称：trust, satisfaction, recommendation, criticism, concern
    /// </summary>
    public string Category { get; set; } = "";
    
    /// <summary>
    /// 类别标签
    /// </summary>
    public string Label { get; set; } = "";
    
    /// <summary>
    /// 评分 (-1 到 1)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 出现次数
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// 代表性表述
    /// </summary>
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// 情感趋势分析 (4.34)
/// </summary>
public class SentimentTrendAnalysis
{
    /// <summary>
    /// 趋势方向：improving, stable, declining
    /// </summary>
    public string TrendDirection { get; set; } = "stable";
    
    /// <summary>
    /// 变化率
    /// </summary>
    public double ChangeRate { get; set; }
    
    /// <summary>
    /// 趋势数据点
    /// </summary>
    public List<SentimentTrendPoint> DataPoints { get; set; } = new();
    
    /// <summary>
    /// 正面趋势
    /// </summary>
    public TrendLine PositiveTrend { get; set; } = new();
    
    /// <summary>
    /// 负面趋势
    /// </summary>
    public TrendLine NegativeTrend { get; set; } = new();
    
    /// <summary>
    /// 趋势分析说明
    /// </summary>
    public string Analysis { get; set; } = "";
    
    /// <summary>
    /// 预测（未来7天）
    /// </summary>
    public SentimentPrediction Prediction { get; set; } = new();
}

/// <summary>
/// 情感趋势数据点
/// </summary>
public class SentimentTrendPoint
{
    public DateTime Date { get; set; }
    public double AverageScore { get; set; }
    public double PositiveRate { get; set; }
    public double NegativeRate { get; set; }
    public int SampleCount { get; set; }
}

/// <summary>
/// 趋势线
/// </summary>
public class TrendLine
{
    public string Direction { get; set; } = "stable"; // rising, stable, declining
    public double Slope { get; set; }
    public double ChangePercent { get; set; }
}

/// <summary>
/// 情感预测
/// </summary>
public class SentimentPrediction
{
    public double PredictedScore { get; set; }
    public string PredictedLevel { get; set; } = "neutral";
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = "";
}

/// <summary>
/// 竞品情感对比 (4.35)
/// </summary>
public class CompetitorSentimentComparison
{
    /// <summary>
    /// 竞品名称
    /// </summary>
    public string Competitor { get; set; } = "";
    
    /// <summary>
    /// 竞品情感评分
    /// </summary>
    public double CompetitorScore { get; set; }
    
    /// <summary>
    /// 我们的情感评分
    /// </summary>
    public double OurScore { get; set; }
    
    /// <summary>
    /// 差距
    /// </summary>
    public double Gap { get; set; }
    
    /// <summary>
    /// 对比结果：leading, tied, trailing
    /// </summary>
    public string Verdict { get; set; } = "tied";
    
    /// <summary>
    /// 竞品正面率
    /// </summary>
    public double CompetitorPositiveRate { get; set; }
    
    /// <summary>
    /// 我们的正面率
    /// </summary>
    public double OurPositiveRate { get; set; }
    
    /// <summary>
    /// 竞品负面率
    /// </summary>
    public double CompetitorNegativeRate { get; set; }
    
    /// <summary>
    /// 我们的负面率
    /// </summary>
    public double OurNegativeRate { get; set; }
    
    /// <summary>
    /// 竞品优势领域
    /// </summary>
    public List<string> CompetitorStrengths { get; set; } = new();
    
    /// <summary>
    /// 我们的优势领域
    /// </summary>
    public List<string> OurStrengths { get; set; } = new();
}

/// <summary>
/// 情感关键词
/// </summary>
public class SentimentKeyword
{
    public string Keyword { get; set; } = "";
    public string Sentiment { get; set; } = "neutral"; // positive, neutral, negative
    public double Score { get; set; }
    public int Count { get; set; }
    public double Impact { get; set; } // 对整体情感的影响程度
}

/// <summary>
/// 平台情感分布
/// </summary>
public class PlatformSentiment
{
    public string Platform { get; set; } = "";
    public double AverageScore { get; set; }
    public double PositiveRate { get; set; }
    public double NeutralRate { get; set; }
    public double NegativeRate { get; set; }
    public int SampleCount { get; set; }
    public string Verdict { get; set; } = ""; // 该平台的情感倾向描述
}

/// <summary>
/// 情感预警
/// </summary>
public class SentimentAlert
{
    /// <summary>
    /// 预警级别：critical, warning, info
    /// </summary>
    public string Level { get; set; } = "info";
    
    /// <summary>
    /// 预警类型：negative_spike, trend_decline, competitor_gap, consistency_issue
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 预警标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 预警描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 相关数据
    /// </summary>
    public string Data { get; set; } = "";
    
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
/// 情感优化建议
/// </summary>
public class SentimentOptimizationSuggestion
{
    /// <summary>
    /// 建议类型：improve_positive, reduce_negative, address_concern, leverage_strength
    /// </summary>
    public string Type { get; set; } = "";
    
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
/// 情感分析请求
/// </summary>
public class SentimentAnalysisRequest
{
    public int TaskId { get; set; }
    public string Brand { get; set; } = "";
    public List<string>? Competitors { get; set; }
    public bool IncludeTrends { get; set; } = true;
    public bool IncludeKeywords { get; set; } = true;
    public bool IncludeAlerts { get; set; } = true;
}
