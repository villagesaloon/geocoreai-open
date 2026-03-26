namespace GeoCore.SaaS.Services.CitationTracking.Models;

/// <summary>
/// 支持的 AI 平台
/// </summary>
public enum AIPlatform
{
    ChatGPT,
    Perplexity,
    Claude,
    Gemini,
    Grok,
    GoogleAI
}

/// <summary>
/// 引用位置
/// </summary>
public enum CitationPosition
{
    None,
    First,   // 前 20%
    Middle,  // 20%-70%
    Last     // 后 30%
}

/// <summary>
/// 情感类型
/// </summary>
public enum SentimentType
{
    Positive,
    Neutral,
    Negative
}

/// <summary>
/// 平台查询响应
/// </summary>
public class PlatformResponse
{
    public AIPlatform Platform { get; set; }
    public string Question { get; set; } = "";
    public string Response { get; set; } = "";
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResponseTimeMs { get; set; }
    public double ApiCost { get; set; }
    public List<string> DetectedLinks { get; set; } = new();
}

/// <summary>
/// 引用分析结果
/// </summary>
public class CitationAnalysisResult
{
    public bool IsCited { get; set; }
    public CitationPosition Position { get; set; } = CitationPosition.None;
    public double PositionRatio { get; set; }
    public bool HasLink { get; set; }
    public string? DetectedLink { get; set; }
    public string? CitationContext { get; set; }
    public SentimentType Sentiment { get; set; } = SentimentType.Neutral;
    public double SentimentScore { get; set; }
    public double VisibilityScore { get; set; }
    public Dictionary<string, bool> CompetitorCitations { get; set; } = new();
    
    /// <summary>
    /// Word Count 指标：引用句子词数占总词数的比例
    /// 来源：GEO 论文 arxiv.org/abs/2311.09735
    /// </summary>
    public double WordCountRatio { get; set; }
    
    /// <summary>
    /// 引用句子的词数
    /// </summary>
    public int CitationWordCount { get; set; }
    
    /// <summary>
    /// 响应总词数
    /// </summary>
    public int TotalWordCount { get; set; }
    
    /// <summary>
    /// Position-Adjusted Score：位置加权可见度
    /// 公式：Σ(词数 × e^(-位置))，前面权重更高
    /// </summary>
    public double PositionAdjustedScore { get; set; }
}

/// <summary>
/// 监测任务请求
/// </summary>
public class MonitoringTaskRequest
{
    public string BrandName { get; set; } = "";
    public List<string> BrandAliases { get; set; } = new();
    public List<string> Competitors { get; set; } = new();
    public List<string> Questions { get; set; } = new();
    public List<AIPlatform> TargetPlatforms { get; set; } = new()
    {
        AIPlatform.ChatGPT,
        AIPlatform.Perplexity,
        AIPlatform.Claude,
        AIPlatform.Gemini,
        AIPlatform.Grok
    };
    public string? ProjectId { get; set; }
}

/// <summary>
/// 监测结果汇总
/// </summary>
public class MonitoringSummary
{
    public int TaskId { get; set; }
    public int RunId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalQueries { get; set; }
    public int CompletedQueries { get; set; }
    
    // 核心指标
    public double CitationFrequency { get; set; }
    public double BrandVisibilityScore { get; set; }
    public double ShareOfVoice { get; set; }
    public double PositiveSentimentRatio { get; set; }
    public double LinkedCitationRatio { get; set; }
    public double FirstPositionRatio { get; set; }
    
    // 分平台指标
    public Dictionary<AIPlatform, PlatformMetrics> PlatformMetrics { get; set; } = new();
    
    // 竞品对比
    public Dictionary<string, double> CompetitorShareOfVoice { get; set; } = new();
    
    // 详细结果
    public List<CitationResultDetail> Results { get; set; } = new();
}

/// <summary>
/// 分平台指标
/// </summary>
public class PlatformMetrics
{
    public AIPlatform Platform { get; set; }
    public int TotalQueries { get; set; }
    public int CitedCount { get; set; }
    public double CitationFrequency { get; set; }
    public double AverageVisibilityScore { get; set; }
    public double PositiveSentimentRatio { get; set; }
    public double LinkedCitationRatio { get; set; }
    public string Trend { get; set; } = "→"; // ↑ ↓ →
}

/// <summary>
/// 单条引用结果详情
/// </summary>
public class CitationResultDetail
{
    public int Id { get; set; }
    public AIPlatform Platform { get; set; }
    public string Question { get; set; } = "";
    public string ResponsePreview { get; set; } = "";
    public bool IsCited { get; set; }
    public CitationPosition Position { get; set; }
    public bool HasLink { get; set; }
    public SentimentType Sentiment { get; set; }
    public double VisibilityScore { get; set; }
    public Dictionary<string, bool> CompetitorCitations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 趋势数据点
/// </summary>
public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public double CitationFrequency { get; set; }
    public double BrandVisibilityScore { get; set; }
    public double ShareOfVoice { get; set; }
}

/// <summary>
/// Dashboard 数据
/// </summary>
public class CitationDashboard
{
    // 当前指标
    public double CitationFrequency { get; set; }
    public double BrandVisibilityScore { get; set; }
    public double ShareOfVoice { get; set; }
    public double PositiveSentimentRatio { get; set; }
    public double LinkedCitationRatio { get; set; }
    
    /// <summary>
    /// 平均 Word Count 比例
    /// </summary>
    public double AverageWordCountRatio { get; set; }
    
    /// <summary>
    /// 平均 Position-Adjusted Score
    /// </summary>
    public double AveragePositionAdjustedScore { get; set; }
    
    // 目标值
    public double CitationFrequencyTarget { get; set; } = 0.3; // 30%
    public double ShareOfVoiceTarget { get; set; } = 0.25; // 25%
    public double PositiveSentimentTarget { get; set; } = 0.7; // 70%
    public double LinkedCitationTarget { get; set; } = 0.5; // 50%
    
    // 趋势
    public string CitationFrequencyTrend { get; set; } = "→";
    public string BrandVisibilityTrend { get; set; } = "→";
    public string ShareOfVoiceTrend { get; set; } = "→";
    
    // 分平台数据
    public List<PlatformMetrics> PlatformData { get; set; } = new();
    
    // 历史趋势
    public List<TrendDataPoint> TrendHistory { get; set; } = new();
    
    // 竞品对比
    public Dictionary<string, double> CompetitorComparison { get; set; } = new();
}
