namespace GeoCore.SaaS.Services.AdvancedGEO;

#region 7.9 Query Fan-out 分析器

public class QueryFanoutRequest
{
    public string MainQuery { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Language { get; set; } = "zh";
}

public class QueryFanoutResult
{
    public string MainQuery { get; set; } = string.Empty;
    public int TotalSubQueries { get; set; }
    public double CoverageScore { get; set; }
    public double PotentialCitationBoost { get; set; } // +161% 引用率
    public List<SubQueryCluster> Clusters { get; set; } = new();
    public List<string> UncoveredQueries { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class SubQueryCluster
{
    public string ClusterName { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty; // informational, navigational, transactional, commercial
    public List<SubQuery> Queries { get; set; } = new();
    public double ImportanceScore { get; set; }
    public bool IsCovered { get; set; }
}

public class SubQuery
{
    public string Query { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // what, how, why, when, where, who, comparison, list
    public double SearchVolume { get; set; }
    public double Difficulty { get; set; }
    public bool IsCovered { get; set; }
    public string SuggestedContentType { get; set; } = string.Empty;
}

#endregion

#region 7.10 Answer Capsules 检测器

public class AnswerCapsuleRequest
{
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = "zh";
}

public class AnswerCapsuleResult
{
    public double OverallScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int TotalCapsules { get; set; }
    public int OptimalCapsules { get; set; } // 120-150 字符
    public double CitationPotential { get; set; } // 72.4% 被引用
    public List<DetectedCapsule> Capsules { get; set; } = new();
    public List<CapsuleOpportunity> Opportunities { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class DetectedCapsule
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
    public int CharacterCount { get; set; }
    public bool IsOptimalLength { get; set; } // 120-150 字符
    public bool IsSelfContained { get; set; }
    public bool HasFactualContent { get; set; }
    public double Score { get; set; }
    public string Position { get; set; } = string.Empty; // first_30, middle, last_30
}

public class CapsuleOpportunity
{
    public string OriginalText { get; set; } = string.Empty;
    public string SuggestedCapsule { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int PotentialCharCount { get; set; }
}

#endregion

#region 7.11 Google 排名-AI 引用相关性

public class RankingCorrelationRequest
{
    public string Domain { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public string Period { get; set; } = "30d"; // 7d, 30d, 90d
}

public class RankingCorrelationResult
{
    public string Domain { get; set; } = string.Empty;
    public double CorrelationCoefficient { get; set; } // -26.7% 流量 → -22.5% 引用
    public string CorrelationStrength { get; set; } = string.Empty; // strong, moderate, weak
    public List<KeywordCorrelation> KeywordAnalysis { get; set; } = new();
    public TrendAnalysis TrendAnalysis { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class KeywordCorrelation
{
    public string Keyword { get; set; } = string.Empty;
    public int GoogleRank { get; set; }
    public int RankChange { get; set; }
    public double AICitationRate { get; set; }
    public double CitationChange { get; set; }
    public double Correlation { get; set; }
}

public class TrendAnalysis
{
    public double OverallTrafficChange { get; set; }
    public double OverallCitationChange { get; set; }
    public string Trend { get; set; } = string.Empty; // improving, stable, declining
    public List<DataPoint> HistoricalData { get; set; } = new();
}

public class DataPoint
{
    public string Date { get; set; } = string.Empty;
    public double TrafficIndex { get; set; }
    public double CitationIndex { get; set; }
}

#endregion

#region 7.12 平台独立性评估

public class PlatformIndependenceRequest
{
    public string Domain { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
}

public class PlatformIndependenceResult
{
    public string Domain { get; set; } = string.Empty;
    public double OverallIndependenceScore { get; set; }
    public List<PlatformDependency> PlatformAnalysis { get; set; } = new();
    public string RiskLevel { get; set; } = string.Empty; // low, medium, high
    public List<string> DiversificationStrategies { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class PlatformDependency
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double GoogleDependency { get; set; } // ChatGPT 高, Perplexity 低
    public string DependencyLevel { get; set; } = string.Empty; // high, medium, low
    public double CitationStability { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public List<string> IndependentFactors { get; set; } = new();
}

#endregion

#region 7.13 多语言 AI 可见度

public class MultiLanguageRequest
{
    public string Content { get; set; } = string.Empty;
    public List<string> TargetLanguages { get; set; } = new();
    public string Brand { get; set; } = string.Empty;
}

public class MultiLanguageResult
{
    public string Brand { get; set; } = string.Empty;
    public int LanguagesAnalyzed { get; set; }
    public List<LanguageVisibility> LanguageResults { get; set; } = new();
    public List<LanguageOpportunity> Opportunities { get; set; } = new();
    public List<string> GlobalRecommendations { get; set; } = new();
}

public class LanguageVisibility
{
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public double VisibilityScore { get; set; }
    public double MarketPotential { get; set; }
    public int EstimatedSearchVolume { get; set; }
    public List<string> TopPlatforms { get; set; } = new();
    public string ContentStatus { get; set; } = string.Empty; // available, partial, missing
}

public class LanguageOpportunity
{
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public double OpportunityScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> SuggestedActions { get; set; } = new();
}

#endregion

#region 4.39-4.41 平台依赖度监测

public class PlatformDependencyMonitorRequest
{
    public string Brand { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
}

public class PlatformDependencyMonitorResult
{
    public string Brand { get; set; } = string.Empty;
    public double DiversificationScore { get; set; }
    public bool HasWarning { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
    public List<PlatformExposure> Exposures { get; set; } = new();
    public List<DependencyAlert> Alerts { get; set; } = new();
    public DiversificationStrategy Strategy { get; set; } = new();
}

public class PlatformExposure
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double ExposurePercent { get; set; }
    public double TrendChange { get; set; }
    public bool IsOverExposed { get; set; } // >50%
    public string RiskLevel { get; set; } = string.Empty;
}

public class DependencyAlert
{
    public string AlertType { get; set; } = string.Empty; // over_exposure, declining, concentration
    public string Platform { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // critical, warning, info
    public string SuggestedAction { get; set; } = string.Empty;
}

public class DiversificationStrategy
{
    public string CurrentStatus { get; set; } = string.Empty;
    public List<string> ImmediateActions { get; set; } = new();
    public List<string> MediumTermActions { get; set; } = new();
    public List<string> LongTermActions { get; set; } = new();
    public Dictionary<string, double> TargetDistribution { get; set; } = new();
}

#endregion

#region 5.21 跨平台调度

public class CrossPlatformScheduleRequest
{
    public string ContentId { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
    public string Timezone { get; set; } = "Asia/Shanghai";
    public DateTime? PreferredStartDate { get; set; }
}

public class CrossPlatformScheduleResult
{
    public string ContentId { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public List<ScheduledPost> Schedule { get; set; } = new();
    public string OptimalSequence { get; set; } = string.Empty;
    public List<string> Rationale { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ScheduledPost
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string TimeSlot { get; set; } = string.Empty;
    public double ExpectedEngagement { get; set; }
    public string ContentVariant { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public string Status { get; set; } = "pending"; // pending, scheduled, published
}

#endregion

#region 5.29-5.30 最佳实践/报告

public class BestPracticeRequest
{
    public string Brand { get; set; } = string.Empty;
    public int TopN { get; set; } = 10;
    public string Period { get; set; } = "30d";
}

public class BestPracticeResult
{
    public string Brand { get; set; } = string.Empty;
    public int ContentAnalyzed { get; set; }
    public List<HighPerformingContent> TopContent { get; set; } = new();
    public List<ExtractedPattern> Patterns { get; set; } = new();
    public List<string> KeyInsights { get; set; } = new();
    public List<string> ActionableRecommendations { get; set; } = new();
}

public class HighPerformingContent
{
    public string ContentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public double PerformanceScore { get; set; }
    public int Views { get; set; }
    public int Engagements { get; set; }
    public int Citations { get; set; }
    public List<string> SuccessFactors { get; set; } = new();
}

public class ExtractedPattern
{
    public string PatternName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
    public int OccurrenceCount { get; set; }
    public List<string> Examples { get; set; } = new();
    public string HowToApply { get; set; } = string.Empty;
}

public class AutomatedReportRequest
{
    public string Brand { get; set; } = string.Empty;
    public string ReportType { get; set; } = "weekly"; // weekly, monthly
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class AutomatedReportResult
{
    public string Brand { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public ExecutiveSummary Summary { get; set; } = new();
    public PerformanceMetrics Metrics { get; set; } = new();
    public List<PlatformPerformance> PlatformBreakdown { get; set; } = new();
    public List<ContentPerformance> TopContent { get; set; } = new();
    public List<string> KeyWins { get; set; } = new();
    public List<string> AreasForImprovement { get; set; } = new();
    public List<string> NextPeriodGoals { get; set; } = new();
}

public class ExecutiveSummary
{
    public double OverallScore { get; set; }
    public string ScoreChange { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string Highlight { get; set; } = string.Empty;
    public List<string> KeyPoints { get; set; } = new();
}

public class PerformanceMetrics
{
    public int TotalViews { get; set; }
    public int TotalEngagements { get; set; }
    public int TotalCitations { get; set; }
    public double EngagementRate { get; set; }
    public double CitationRate { get; set; }
    public double ROI { get; set; }
    public Dictionary<string, double> MetricChanges { get; set; } = new();
}

public class PlatformPerformance
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double Score { get; set; }
    public double Change { get; set; }
    public int ContentCount { get; set; }
    public int TotalEngagements { get; set; }
    public string Status { get; set; } = string.Empty; // growing, stable, declining
}

public class ContentPerformance
{
    public string ContentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public int Views { get; set; }
    public int Engagements { get; set; }
    public double EngagementRate { get; set; }
}

#endregion
