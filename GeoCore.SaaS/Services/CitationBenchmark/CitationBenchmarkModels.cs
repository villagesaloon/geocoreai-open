namespace GeoCore.SaaS.Services.CitationBenchmark;

#region 4.42 分平台引用基准

public class PlatformBenchmark
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double AverageCitationRate { get; set; }
    public int TypicalCitationCount { get; set; }
    public List<string> PreferredSourceTypes { get; set; } = new();
    public List<string> ContentPreferences { get; set; } = new();
    public ContentStructureBenchmark StructureBenchmark { get; set; } = new();
    public string UpdateFrequencyPreference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class ContentStructureBenchmark
{
    public int OptimalParagraphLength { get; set; }
    public int OptimalSectionCount { get; set; }
    public bool PrefersBulletPoints { get; set; }
    public bool PrefersNumberedLists { get; set; }
    public bool PrefersTables { get; set; }
    public int OptimalHeadingDepth { get; set; }
}

#endregion

#region 4.43 内容结构评分

public class ContentStructureRequest
{
    public string Content { get; set; } = string.Empty;
    public string TargetPlatform { get; set; } = string.Empty;
}

public class ContentStructureResult
{
    public double OverallScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public ParagraphAnalysis ParagraphAnalysis { get; set; } = new();
    public HeadingAnalysis HeadingAnalysis { get; set; } = new();
    public ListAnalysis ListAnalysis { get; set; } = new();
    public List<StructureIssue> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class ParagraphAnalysis
{
    public int TotalParagraphs { get; set; }
    public double AverageLength { get; set; }
    public int OptimalParagraphs { get; set; } // 120-180 词
    public int TooShortParagraphs { get; set; }
    public int TooLongParagraphs { get; set; }
    public double Score { get; set; }
}

public class HeadingAnalysis
{
    public int H1Count { get; set; }
    public int H2Count { get; set; }
    public int H3Count { get; set; }
    public bool HasProperHierarchy { get; set; }
    public double Score { get; set; }
}

public class ListAnalysis
{
    public int BulletListCount { get; set; }
    public int NumberedListCount { get; set; }
    public int TableCount { get; set; }
    public double Score { get; set; }
}

public class StructureIssue
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // critical, warning, info
    public string Description { get; set; } = string.Empty;
    public string Fix { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
}

#endregion

#region 4.44 多模态内容检测

public class MultimodalAnalysisRequest
{
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class MultimodalAnalysisResult
{
    public double MultimodalScore { get; set; }
    public ImageAnalysis ImageAnalysis { get; set; } = new();
    public VideoAnalysis VideoAnalysis { get; set; } = new();
    public TableAnalysis TableAnalysis { get; set; } = new();
    public ChartAnalysis ChartAnalysis { get; set; } = new();
    public double GoogleAICorrelation { get; set; } // r=0.92
    public List<string> Recommendations { get; set; } = new();
}

public class ImageAnalysis
{
    public int ImageCount { get; set; }
    public int ImagesWithAlt { get; set; }
    public int ImagesWithCaption { get; set; }
    public double Score { get; set; }
    public List<string> Issues { get; set; } = new();
}

public class VideoAnalysis
{
    public int VideoCount { get; set; }
    public int VideosWithTranscript { get; set; }
    public double Score { get; set; }
}

public class TableAnalysis
{
    public int TableCount { get; set; }
    public int TablesWithHeaders { get; set; }
    public double Score { get; set; }
}

public class ChartAnalysis
{
    public int ChartCount { get; set; }
    public int ChartsWithDescription { get; set; }
    public double Score { get; set; }
}

#endregion

#region 4.45 实体密度分析

public class EntityDensityRequest
{
    public string Content { get; set; } = string.Empty;
}

public class EntityDensityResult
{
    public int TotalEntities { get; set; }
    public double EntityDensity { get; set; } // 实体/100词
    public double SelectionMultiplier { get; set; } // 15+ 实体 → 4.8x
    public EntityBreakdown Breakdown { get; set; } = new();
    public List<ExtractedEntity> TopEntities { get; set; } = new();
    public double Score { get; set; }
    public string Grade { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
}

public class EntityBreakdown
{
    public int PersonCount { get; set; }
    public int OrganizationCount { get; set; }
    public int LocationCount { get; set; }
    public int ProductCount { get; set; }
    public int DateCount { get; set; }
    public int NumberCount { get; set; }
    public int OtherCount { get; set; }
}

public class ExtractedEntity
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public double Relevance { get; set; }
}

#endregion

#region 综合基准分析

public class ComprehensiveBenchmarkRequest
{
    public string Content { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
}

public class ComprehensiveBenchmarkResult
{
    public double OverallScore { get; set; }
    public string Grade { get; set; } = string.Empty;
    public ContentStructureResult StructureAnalysis { get; set; } = new();
    public MultimodalAnalysisResult MultimodalAnalysis { get; set; } = new();
    public EntityDensityResult EntityAnalysis { get; set; } = new();
    public List<PlatformFitScore> PlatformFitScores { get; set; } = new();
    public List<string> TopRecommendations { get; set; } = new();
}

public class PlatformFitScore
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double FitScore { get; set; }
    public string FitLevel { get; set; } = string.Empty; // excellent, good, fair, poor
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

#endregion

#region 4.42 分平台引用基准（扩展）

public class PlatformBenchmarkDetailRequest
{
    public string Platform { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
}

public class PlatformBenchmarkDetailResult
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PlatformBenchmark Benchmark { get; set; } = new();
    public PlatformCitationMetrics CitationMetrics { get; set; } = new();
    public List<PlatformTopSource> TopSources { get; set; } = new();
    public PlatformContentGuidelines ContentGuidelines { get; set; } = new();
    public List<string> OptimizationTips { get; set; } = new();
}

public class PlatformCitationMetrics
{
    public double AverageCitationsPerResponse { get; set; }
    public double CitationDiversity { get; set; }
    public double FreshnessWeight { get; set; }
    public double AuthorityWeight { get; set; }
    public double RelevanceWeight { get; set; }
    public string UpdateFrequency { get; set; } = string.Empty;
    public int TypicalSourceCount { get; set; }
}

public class PlatformTopSource
{
    public int Rank { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double CitationShare { get; set; }
    public string Actionability { get; set; } = string.Empty;
    public List<string> Examples { get; set; } = new();
}

public class PlatformContentGuidelines
{
    public int OptimalWordCount { get; set; }
    public int OptimalParagraphLength { get; set; }
    public string TonePreference { get; set; } = string.Empty;
    public List<string> MustHaveElements { get; set; } = new();
    public List<string> AvoidElements { get; set; } = new();
    public string StructurePreference { get; set; } = string.Empty;
}

public class AllPlatformBenchmarksResult
{
    public List<PlatformBenchmarkSummary> Platforms { get; set; } = new();
    public PlatformComparisonMatrix ComparisonMatrix { get; set; } = new();
    public List<string> CrossPlatformTips { get; set; } = new();
}

public class PlatformBenchmarkSummary
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public double CitationRate { get; set; }
    public int TypicalCitations { get; set; }
    public string TopSourceType { get; set; } = string.Empty;
    public string FreshnessPreference { get; set; } = string.Empty;
    public string ContentStyle { get; set; } = string.Empty;
}

public class PlatformComparisonMatrix
{
    public List<string> Platforms { get; set; } = new();
    public Dictionary<string, Dictionary<string, double>> OverlapRates { get; set; } = new();
    public string Insight { get; set; } = string.Empty;
}

#endregion

#region 4.47 平台偏好差异化

public class PlatformPreferenceDiffRequest
{
    public string Content { get; set; } = string.Empty;
    public List<string> TargetPlatforms { get; set; } = new();
    public string Industry { get; set; } = string.Empty;
}

public class PlatformPreferenceDiffResult
{
    public List<PlatformSpecificStrategy> PlatformStrategies { get; set; } = new();
    public PlatformDifferentiationMatrix DifferentiationMatrix { get; set; } = new();
    public List<ContentVariation> ContentVariations { get; set; } = new();
    public List<string> UnifiedRecommendations { get; set; } = new();
}

public class PlatformSpecificStrategy
{
    public string Platform { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CorePreference { get; set; } = string.Empty;
    public double CurrentFitScore { get; set; }
    public List<string> StrengthsForPlatform { get; set; } = new();
    public List<string> WeaknessesForPlatform { get; set; } = new();
    public List<PlatformOptimizationAction> OptimizationActions { get; set; } = new();
    public string ExpectedImpact { get; set; } = string.Empty;
}

public class PlatformOptimizationAction
{
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
}

public class PlatformDifferentiationMatrix
{
    public List<DifferentiationDimension> Dimensions { get; set; } = new();
    public string KeyInsight { get; set; } = string.Empty;
}

public class DifferentiationDimension
{
    public string Dimension { get; set; } = string.Empty;
    public Dictionary<string, string> PlatformValues { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

public class ContentVariation
{
    public string Platform { get; set; } = string.Empty;
    public string VariationType { get; set; } = string.Empty;
    public string OriginalApproach { get; set; } = string.Empty;
    public string OptimizedApproach { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
}

#endregion
