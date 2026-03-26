namespace GeoCore.SaaS.Services.LLMCitationOptimization;

#region Platform Preference Data (7.1)

public class LLMPlatformPreferences
{
    public string Platform { get; set; } = string.Empty; // ChatGPT, Perplexity, Gemini, Claude, Grok
    public string DisplayName { get; set; } = string.Empty;
    public List<CitationSource> TopSources { get; set; } = new();
    public PlatformCharacteristics Characteristics { get; set; } = new();
    public string LastUpdated { get; set; } = string.Empty;
}

public class CitationSource
{
    public int Rank { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // news, encyclopedia, forum, social, official
    public double CitationShare { get; set; } // 百分比
    public string Actionability { get; set; } = string.Empty; // high, medium, low
    public string ActionabilityReason { get; set; } = string.Empty;
    public List<string> OptimizationTips { get; set; } = new();
}

public class PlatformCharacteristics
{
    public string ContentPreference { get; set; } = string.Empty; // fresh, authoritative, comprehensive
    public string TypicalCitationCount { get; set; } = string.Empty;
    public string PreferredFormat { get; set; } = string.Empty;
    public List<string> KeyFactors { get; set; } = new();
    public string EffectiveTimeframe { get; set; } = string.Empty; // 1 week, 2-4 weeks, 1-2 months
}

#endregion

#region Optimization Suggestions (7.2)

public class PlatformOptimizationRequest
{
    public string BrandName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
    public string CurrentWebsiteUrl { get; set; } = string.Empty;
    public List<string> ExistingPlatformPresence { get; set; } = new(); // reddit, medium, linkedin, etc.
}

public class PlatformOptimizationResult
{
    public string BrandName { get; set; } = string.Empty;
    public List<PlatformStrategy> Strategies { get; set; } = new();
    public List<QuickWin> QuickWins { get; set; } = new();
    public CrossPlatformAnalysis CrossPlatformAnalysis { get; set; } = new();
    public OptimizationRoadmap Roadmap { get; set; } = new();
}

public class PlatformStrategy
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int PriorityScore { get; set; } // 1-100
    public string PriorityLevel { get; set; } = string.Empty; // P0, P1, P2
    public List<ActionItem> ActionItems { get; set; } = new();
    public List<ContentRecommendation> ContentRecommendations { get; set; } = new();
    public string ExpectedTimeframe { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
}

public class ActionItem
{
    public int Order { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Effort { get; set; } = string.Empty; // low, medium, high
    public string Impact { get; set; } = string.Empty; // low, medium, high
    public string Timeline { get; set; } = string.Empty;
}

public class ContentRecommendation
{
    public string TargetPlatform { get; set; } = string.Empty; // reddit, medium, linkedin, youtube
    public string ContentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public List<string> BestPractices { get; set; } = new();
}

public class QuickWin
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Effort { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public int DaysToImplement { get; set; }
}

#endregion

#region Cross-Platform Analysis (7.4)

public class CrossPlatformAnalysis
{
    public double OverlapPercentage { get; set; } // 研究显示仅 11% 重叠
    public List<PlatformOverlap> Overlaps { get; set; } = new();
    public List<UniqueOpportunity> UniqueOpportunities { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class PlatformOverlap
{
    public string Platform1 { get; set; } = string.Empty;
    public string Platform2 { get; set; } = string.Empty;
    public double OverlapPercentage { get; set; }
    public List<string> SharedSources { get; set; } = new();
}

public class UniqueOpportunity
{
    public string Platform { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Actionability { get; set; } = string.Empty;
}

#endregion

#region 30-Day Roadmap (7.6)

public class OptimizationRoadmap
{
    public List<RoadmapPhase> Phases { get; set; } = new();
    public List<Milestone> Milestones { get; set; } = new();
    public Dictionary<string, string> ExpectedTimelines { get; set; } = new();
    public string TotalDuration { get; set; } = "30 天";
}

public class RoadmapPhase
{
    public int PhaseNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    public List<PhaseTask> Tasks { get; set; } = new();
    public string ExpectedOutcome { get; set; } = string.Empty;
}

public class PhaseTask
{
    public string Task { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty; // technical, content, marketing
    public bool IsCompleted { get; set; }
}

public class Milestone
{
    public int Day { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Deliverables { get; set; } = new();
}

#endregion

#region Platform Priority (7.8)

public class PlatformPriorityRequest
{
    public string Industry { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // B2B, B2C, technical, general
    public List<string> Goals { get; set; } = new(); // brand_awareness, lead_generation, thought_leadership
    public int Budget { get; set; } // 1-5 scale
    public int TeamSize { get; set; } // 1-5 scale
}

public class PlatformPriorityResult
{
    public List<PlatformRanking> Rankings { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
}

public class PlatformRanking
{
    public int Rank { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Score { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public List<string> KeyActions { get; set; } = new();
    public string ROIEstimate { get; set; } = string.Empty;
}

#endregion

#region Content Templates (7.5)

public class ContentTemplateRequest
{
    public string Platform { get; set; } = string.Empty; // reddit, medium, linkedin, youtube
    public string ContentType { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
}

public class ContentTemplateResult
{
    public string Platform { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public List<string> Guidelines { get; set; } = new();
    public List<string> DoList { get; set; } = new();
    public List<string> DontList { get; set; } = new();
    public string OptimalLength { get; set; } = string.Empty;
    public string BestPostingTime { get; set; } = string.Empty;
}

#endregion

#region Effect Timeline (7.7)

public class EffectTimeline
{
    public string Platform { get; set; } = string.Empty;
    public string InitialEffectTime { get; set; } = string.Empty;
    public string FullEffectTime { get; set; } = string.Empty;
    public List<TimelineStage> Stages { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
}

public class TimelineStage
{
    public string Stage { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
}

#endregion

#region Wikipedia Style Content Strategy (7.14)

/// <summary>
/// Wikipedia 风格内容策略请求
/// 原理：Wikipedia 占 ChatGPT 47.9% 引用，生成 Wikipedia 风格内容指南
/// </summary>
public class WikipediaStyleRequest
{
    public string Topic { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ExistingContent { get; set; } = string.Empty;
}

public class WikipediaStyleResult
{
    public string Topic { get; set; } = string.Empty;
    public WikipediaStyleAnalysis Analysis { get; set; } = new();
    public WikipediaStyleGuide StyleGuide { get; set; } = new();
    public List<WikipediaStyleRecommendation> Recommendations { get; set; } = new();
    public WikipediaStyleTemplate Template { get; set; } = new();
    public int OverallScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class WikipediaStyleAnalysis
{
    public int NeutralityScore { get; set; }
    public int CitationDensityScore { get; set; }
    public int StructureScore { get; set; }
    public int FactualDensityScore { get; set; }
    public int VerifiabilityScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
}

public class WikipediaStyleGuide
{
    public List<WikipediaStylePrinciple> Principles { get; set; } = new();
    public List<string> ToneGuidelines { get; set; } = new();
    public List<string> StructureGuidelines { get; set; } = new();
    public List<string> CitationGuidelines { get; set; } = new();
    public List<string> AvoidList { get; set; } = new();
}

public class WikipediaStylePrinciple
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
    public string CounterExample { get; set; } = string.Empty;
}

public class WikipediaStyleRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;
}

public class WikipediaStyleTemplate
{
    public string IntroductionTemplate { get; set; } = string.Empty;
    public List<string> SuggestedSections { get; set; } = new();
    public string CitationFormat { get; set; } = string.Empty;
    public string ConclusionTemplate { get; set; } = string.Empty;
}

#endregion

#region YouTube Citation Optimization (7.16)

/// <summary>
/// YouTube 引用优化请求
/// 原理：YouTube 超越 Reddit 成为 #1 社交引用源（16% vs 10%）
/// </summary>
public class YouTubeCitationRequest
{
    public string BrandName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ChannelUrl { get; set; } = string.Empty;
    public List<string> TopicAreas { get; set; } = new();
    public string TargetAudience { get; set; } = string.Empty;
}

public class YouTubeCitationResult
{
    public YouTubeCitationAnalysis Analysis { get; set; } = new();
    public List<YouTubeContentStrategy> ContentStrategies { get; set; } = new();
    public YouTubeOptimizationGuide OptimizationGuide { get; set; } = new();
    public List<YouTubeActionItem> ActionItems { get; set; } = new();
    public YouTubeBenchmarks Benchmarks { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class YouTubeCitationAnalysis
{
    public double YouTubeShareOfCitations { get; set; }
    public double RedditShareOfCitations { get; set; }
    public string WhyYouTubeMatters { get; set; } = string.Empty;
    public List<string> KeyInsights { get; set; } = new();
    public List<string> PlatformsThatCiteYouTube { get; set; } = new();
}

public class YouTubeContentStrategy
{
    public string ContentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CitationPotential { get; set; }
    public List<string> BestPractices { get; set; } = new();
    public string OptimalLength { get; set; } = string.Empty;
    public string ExampleTopic { get; set; } = string.Empty;
}

public class YouTubeOptimizationGuide
{
    public List<string> TitleOptimization { get; set; } = new();
    public List<string> DescriptionOptimization { get; set; } = new();
    public List<string> TranscriptOptimization { get; set; } = new();
    public List<string> ChapterOptimization { get; set; } = new();
    public List<string> ThumbnailTips { get; set; } = new();
}

public class YouTubeActionItem
{
    public int Priority { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Effort { get; set; } = string.Empty;
}

public class YouTubeBenchmarks
{
    public string AverageCitationRate { get; set; } = string.Empty;
    public string TopPerformingCategories { get; set; } = string.Empty;
    public string OptimalVideoLength { get; set; } = string.Empty;
    public string BestPostingFrequency { get; set; } = string.Empty;
}

#endregion

#region AI Traffic Conversion Tracking (7.17)

/// <summary>
/// AI 流量转化追踪请求
/// 原理：AI 流量转化率 14.2% vs Google 2.8%，13,770 域名大规模验证
/// </summary>
public class AITrafficConversionRequest
{
    public string WebsiteUrl { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ConversionGoal { get; set; } = string.Empty;
    public bool HasGA4Setup { get; set; }
}

public class AITrafficConversionResult
{
    public AITrafficBenchmarks Benchmarks { get; set; } = new();
    public AITrafficSetupGuide SetupGuide { get; set; } = new();
    public List<AITrafficMetric> MetricsToTrack { get; set; } = new();
    public List<AITrafficOptimization> Optimizations { get; set; } = new();
    public AITrafficROIProjection ROIProjection { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class AITrafficBenchmarks
{
    public double AIConversionRate { get; set; }
    public double GoogleConversionRate { get; set; }
    public double ConversionRateMultiplier { get; set; }
    public int SampleSize { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public List<IndustryBenchmark> IndustryBenchmarks { get; set; } = new();
}

public class IndustryBenchmark
{
    public string Industry { get; set; } = string.Empty;
    public double AIConversionRate { get; set; }
    public double GoogleConversionRate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class AITrafficSetupGuide
{
    public List<TrackingSetupStep> GA4Steps { get; set; } = new();
    public List<TrackingSetupStep> GTMSteps { get; set; } = new();
    public string CustomDimensionCode { get; set; } = string.Empty;
    public string EventTrackingCode { get; set; } = string.Empty;
    public List<string> ReferrerPatterns { get; set; } = new();
}

public class TrackingSetupStep
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = new();
}

public class AITrafficMetric
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Benchmark { get; set; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
}

public class AITrafficOptimization
{
    public string Area { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
}

public class AITrafficROIProjection
{
    public string CurrentTrafficEstimate { get; set; } = string.Empty;
    public string ProjectedAITrafficGrowth { get; set; } = string.Empty;
    public string ProjectedConversions { get; set; } = string.Empty;
    public string ProjectedRevenue { get; set; } = string.Empty;
    public List<string> Assumptions { get; set; } = new();
}

#endregion

#region Influenceable Domain Strategy (7.18)

/// <summary>
/// 可影响域名策略请求
/// 原理：74% 高引用域名可被营销影响，50 域名分析
/// </summary>
public class InfluenceableDomainRequest
{
    public string BrandName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
    public string Budget { get; set; } = string.Empty;
}

public class InfluenceableDomainResult
{
    public InfluenceabilityAnalysis Analysis { get; set; } = new();
    public List<InfluenceableDomain> InfluenceableDomains { get; set; } = new();
    public List<DomainInfluenceStrategy> Strategies { get; set; } = new();
    public InfluenceabilityRoadmap Roadmap { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class InfluenceabilityAnalysis
{
    public double InfluenceablePercentage { get; set; }
    public int TotalDomainsAnalyzed { get; set; }
    public string DataSource { get; set; } = string.Empty;
    public List<string> KeyFindings { get; set; } = new();
    public List<DomainCategory> Categories { get; set; } = new();
}

public class DomainCategory
{
    public string Category { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public string Influenceability { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
}

public class InfluenceableDomain
{
    public string Domain { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string InfluenceLevel { get; set; } = string.Empty;
    public List<string> InfluenceMethods { get; set; } = new();
    public string Effort { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
    public int PriorityScore { get; set; }
}

public class DomainInfluenceStrategy
{
    public string DomainType { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public string Timeline { get; set; } = string.Empty;
    public string Budget { get; set; } = string.Empty;
    public string ExpectedROI { get; set; } = string.Empty;
}

public class InfluenceabilityRoadmap
{
    public List<InfluencePhase> Phases { get; set; } = new();
    public string TotalDuration { get; set; } = string.Empty;
    public List<string> QuickWins { get; set; } = new();
    public List<string> LongTermGoals { get; set; } = new();
}

public class InfluencePhase
{
    public int PhaseNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public List<string> TargetDomains { get; set; } = new();
    public string ExpectedOutcome { get; set; } = string.Empty;
}

#endregion

#region 7.19 Query Fan-out 年份检测

public class QueryYearDetectionRequest
{
    public string Query { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TargetYear { get; set; }
}

public class QueryYearDetectionResult
{
    public string Query { get; set; } = string.Empty;
    public bool HasYearInQuery { get; set; }
    public int? DetectedYear { get; set; }
    public bool ContentHasYearMarker { get; set; }
    public List<int> YearsFoundInContent { get; set; } = new();
    public bool IsYearRelevantQuery { get; set; }
    public double YearRelevanceScore { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public QueryYearAnalysis Analysis { get; set; } = new();
}

public class QueryYearAnalysis
{
    public string QueryType { get; set; } = string.Empty;
    public bool LikelyToAddYear { get; set; }
    public double YearAdditionProbability { get; set; }
    public List<string> YearSensitiveKeywords { get; set; } = new();
    public string RecommendedYearStrategy { get; set; } = string.Empty;
}

#endregion

#region 7.20 AutoGEO 内容重写建议

public class AutoGEORewriteRequest
{
    public string Content { get; set; } = string.Empty;
    public string TargetPlatform { get; set; } = "all";
    public string Industry { get; set; } = string.Empty;
    public List<string> TargetKeywords { get; set; } = new();
}

public class AutoGEORewriteResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public double CurrentGEOScore { get; set; }
    public List<GEORewriteSuggestion> Suggestions { get; set; } = new();
    public List<string> ExtractedRules { get; set; } = new();
    public string RewrittenContent { get; set; } = string.Empty;
    public double PredictedGEOScore { get; set; }
    public double ImprovementPercentage { get; set; }
}

public class GEORewriteSuggestion
{
    public string Type { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string SuggestedText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
    public string Priority { get; set; } = string.Empty;
}

#endregion

#region 7.21 平台特定引用策略

public class PlatformCitationStrategyRequest
{
    public string TargetPlatform { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public List<string> CurrentChannels { get; set; } = new();
}

public class PlatformCitationStrategyResult
{
    public string Platform { get; set; } = string.Empty;
    public string PlatformDisplayName { get; set; } = string.Empty;
    public PlatformCitationProfile Profile { get; set; } = new();
    public List<CitationChannelStrategy> ChannelStrategies { get; set; } = new();
    public List<string> QuickWins { get; set; } = new();
    public PlatformActionPlan ActionPlan { get; set; } = new();
}

public class PlatformCitationProfile
{
    public string TopCitationSource { get; set; } = string.Empty;
    public double TopSourceMultiplier { get; set; }
    public List<string> PreferredContentTypes { get; set; } = new();
    public string CitationStyle { get; set; } = string.Empty;
    public string UpdateFrequencyPreference { get; set; } = string.Empty;
}

public class CitationChannelStrategy
{
    public string Channel { get; set; } = string.Empty;
    public double CitationMultiplier { get; set; }
    public string Strategy { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public string ExpectedImpact { get; set; } = string.Empty;
}

public class PlatformActionPlan
{
    public List<PlatformActionPhase> Phases { get; set; } = new();
    public string TotalDuration { get; set; } = string.Empty;
    public List<string> KPIs { get; set; } = new();
}

public class PlatformActionPhase
{
    public int Week { get; set; }
    public string Focus { get; set; } = string.Empty;
    public List<string> Tasks { get; set; } = new();
    public string ExpectedOutcome { get; set; } = string.Empty;
}

#endregion

#region 7.22 LinkedIn B2B 引用优化

public class LinkedInB2BRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public List<string> TargetAudience { get; set; } = new();
    public string ContentType { get; set; } = "thought_leadership";
}

public class LinkedInB2BResult
{
    public string CompanyName { get; set; } = string.Empty;
    public LinkedInCitationAnalysis Analysis { get; set; } = new();
    public List<LinkedInContentStrategy> ContentStrategies { get; set; } = new();
    public LinkedInOptimizationGuide OptimizationGuide { get; set; } = new();
    public List<string> B2BSpecificTips { get; set; } = new();
}

public class LinkedInCitationAnalysis
{
    public double LinkedInCitationShare { get; set; }
    public string ComparisonToYouTube { get; set; } = string.Empty;
    public List<string> TopPerformingContentTypes { get; set; } = new();
    public string B2BAdvantage { get; set; } = string.Empty;
}

public class LinkedInContentStrategy
{
    public string ContentType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> BestPractices { get; set; } = new();
    public string PostingFrequency { get; set; } = string.Empty;
    public string ExpectedEngagement { get; set; } = string.Empty;
}

public class LinkedInOptimizationGuide
{
    public List<string> ProfileOptimization { get; set; } = new();
    public List<string> ContentOptimization { get; set; } = new();
    public List<string> EngagementTactics { get; set; } = new();
    public List<string> HashtagStrategy { get; set; } = new();
}

#endregion

#region 7.24 llms.txt 模型定制

public class LlmsTxtModelCustomRequest
{
    public string TargetModel { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public List<string> KeyPages { get; set; } = new();
}

public class LlmsTxtModelCustomResult
{
    public string TargetModel { get; set; } = string.Empty;
    public string ModelDisplayName { get; set; } = string.Empty;
    public ModelLlmsTxtProfile Profile { get; set; } = new();
    public string GeneratedLlmsTxt { get; set; } = string.Empty;
    public List<LlmsTxtSection> Sections { get; set; } = new();
    public List<string> ModelSpecificTips { get; set; } = new();
}

public class ModelLlmsTxtProfile
{
    public string PreferredContentType { get; set; } = string.Empty;
    public List<string> PriorityPages { get; set; } = new();
    public string StructurePreference { get; set; } = string.Empty;
    public string DetailLevel { get; set; } = string.Empty;
}

public class LlmsTxtSection
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsModelSpecific { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

#endregion

#region 7.25 引用表面积分析

public class CitationSurfaceRequest
{
    public string BrandName { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public List<string> CompetitorBrands { get; set; } = new();
}

public class CitationSurfaceResult
{
    public string BrandName { get; set; } = string.Empty;
    public CitationSurfaceAnalysis Analysis { get; set; } = new();
    public List<CitationSurfaceChannel> Channels { get; set; } = new();
    public CitationSurfaceStrategy Strategy { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class CitationSurfaceAnalysis
{
    public double BrandMentionScore { get; set; }
    public double BacklinkScore { get; set; }
    public double CombinedScore { get; set; }
    public string MentionVsBacklinkRatio { get; set; } = string.Empty;
    public string Insight { get; set; } = string.Empty;
}

public class CitationSurfaceChannel
{
    public string Channel { get; set; } = string.Empty;
    public double MentionPotential { get; set; }
    public double BacklinkPotential { get; set; }
    public string RecommendedFocus { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
}

public class CitationSurfaceStrategy
{
    public List<string> MentionBuildingTactics { get; set; } = new();
    public List<string> BacklinkBuildingTactics { get; set; } = new();
    public List<string> CombinedApproach { get; set; } = new();
    public string ExpectedImpact { get; set; } = string.Empty;
}

#endregion

#region 7.26 高权威平台快速索引

public class RapidIndexRequest
{
    public string ContentUrl { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public string ContentSummary { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
}

public class RapidIndexResult
{
    public string ContentUrl { get; set; } = string.Empty;
    public List<RapidIndexPlatform> Platforms { get; set; } = new();
    public RapidIndexStrategy Strategy { get; set; } = new();
    public List<string> QuickActions { get; set; } = new();
    public string EstimatedIndexTime { get; set; } = string.Empty;
}

public class RapidIndexPlatform
{
    public string Platform { get; set; } = string.Empty;
    public int DomainRating { get; set; }
    public string EstimatedIndexTime { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public string ContentFormat { get; set; } = string.Empty;
    public List<string> BestPractices { get; set; } = new();
}

public class RapidIndexStrategy
{
    public List<RapidIndexPhase> Phases { get; set; } = new();
    public string TotalDuration { get; set; } = string.Empty;
    public List<string> PlatformPriority { get; set; } = new();
}

public class RapidIndexPhase
{
    public int Hour { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ExpectedOutcome { get; set; } = string.Empty;
}

#endregion
