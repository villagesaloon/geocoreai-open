namespace GeoCore.SaaS.Services.InsightAnalyzer;

/// <summary>
/// 四信号洞察分析结果
/// </summary>
public class FourSignalInsight
{
    /// <summary>
    /// 引用集中度信号 - 品牌在哪些问题/场景中被提及最多
    /// </summary>
    public CitationConcentrationSignal CitationConcentration { get; set; } = new();

    /// <summary>
    /// 当前位置信号 - 品牌在 AI 回答中的位置（首位/前三/提及）
    /// </summary>
    public CurrentPositionSignal CurrentPosition { get; set; } = new();

    /// <summary>
    /// 品牌力信号 - 品牌被推荐的强度和语气
    /// </summary>
    public BrandStrengthSignal BrandStrength { get; set; } = new();

    /// <summary>
    /// 品类信号 - 品牌在哪些品类/领域被关联
    /// </summary>
    public CategorySignal Category { get; set; } = new();

    /// <summary>
    /// 综合评分 (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// 洞察摘要
    /// </summary>
    public string Summary { get; set; } = "";

    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 引用集中度信号
/// </summary>
public class CitationConcentrationSignal
{
    /// <summary>
    /// 信号强度 (0-100)
    /// </summary>
    public double Strength { get; set; }

    /// <summary>
    /// 高集中度问题（品牌经常被提及的问题类型）
    /// </summary>
    public List<ConcentrationCluster> HighConcentrationClusters { get; set; } = new();

    /// <summary>
    /// 低集中度问题（品牌很少被提及的问题类型）
    /// </summary>
    public List<ConcentrationCluster> LowConcentrationClusters { get; set; } = new();

    /// <summary>
    /// 集中度分布（按问题类型）
    /// </summary>
    public Dictionary<string, double> DistributionByQuestionType { get; set; } = new();
}

/// <summary>
/// 集中度聚类
/// </summary>
public class ConcentrationCluster
{
    public string QuestionType { get; set; } = "";
    public string Description { get; set; } = "";
    public int MentionCount { get; set; }
    public double MentionRate { get; set; }
    public List<string> SampleQuestions { get; set; } = new();
}

/// <summary>
/// 当前位置信号
/// </summary>
public class CurrentPositionSignal
{
    /// <summary>
    /// 信号强度 (0-100)
    /// </summary>
    public double Strength { get; set; }

    /// <summary>
    /// 首位提及率
    /// </summary>
    public double FirstPositionRate { get; set; }

    /// <summary>
    /// 前三位提及率
    /// </summary>
    public double TopThreeRate { get; set; }

    /// <summary>
    /// 总提及率
    /// </summary>
    public double OverallMentionRate { get; set; }

    /// <summary>
    /// 平均位置
    /// </summary>
    public double AveragePosition { get; set; }

    /// <summary>
    /// 各平台位置分布
    /// </summary>
    public Dictionary<string, PositionStats> ByPlatform { get; set; } = new();
}

/// <summary>
/// 位置统计
/// </summary>
public class PositionStats
{
    public double FirstPositionRate { get; set; }
    public double TopThreeRate { get; set; }
    public double MentionRate { get; set; }
    public double AveragePosition { get; set; }
    public int TotalQueries { get; set; }
}

/// <summary>
/// 品牌力信号
/// </summary>
public class BrandStrengthSignal
{
    /// <summary>
    /// 信号强度 (0-100)
    /// </summary>
    public double Strength { get; set; }

    /// <summary>
    /// 推荐强度分布
    /// </summary>
    public RecommendationStrength RecommendationStrength { get; set; } = new();

    /// <summary>
    /// 情感倾向
    /// </summary>
    public SentimentBreakdown Sentiment { get; set; } = new();

    /// <summary>
    /// 常见描述词
    /// </summary>
    public List<DescriptorFrequency> CommonDescriptors { get; set; } = new();

    /// <summary>
    /// 与竞品对比
    /// </summary>
    public List<CompetitorComparison> CompetitorComparisons { get; set; } = new();
}

/// <summary>
/// 推荐强度分布
/// </summary>
public class RecommendationStrength
{
    /// <summary>
    /// 强烈推荐率（"最佳"、"首选"、"强烈推荐"）
    /// </summary>
    public double StrongRecommendationRate { get; set; }

    /// <summary>
    /// 一般推荐率（"不错"、"可以考虑"）
    /// </summary>
    public double ModerateRecommendationRate { get; set; }

    /// <summary>
    /// 仅提及率（无明确推荐语气）
    /// </summary>
    public double MentionOnlyRate { get; set; }

    /// <summary>
    /// 负面提及率
    /// </summary>
    public double NegativeMentionRate { get; set; }
}

/// <summary>
/// 情感分布
/// </summary>
public class SentimentBreakdown
{
    public double PositiveRate { get; set; }
    public double NeutralRate { get; set; }
    public double NegativeRate { get; set; }
    public double AverageScore { get; set; } // -1 to 1
}

/// <summary>
/// 描述词频率
/// </summary>
public class DescriptorFrequency
{
    public string Descriptor { get; set; } = "";
    public int Count { get; set; }
    public string Sentiment { get; set; } = "neutral"; // positive, neutral, negative
}

/// <summary>
/// 竞品对比
/// </summary>
public class CompetitorComparison
{
    public string Competitor { get; set; } = "";
    public double YourMentionRate { get; set; }
    public double CompetitorMentionRate { get; set; }
    public double YourAveragePosition { get; set; }
    public double CompetitorAveragePosition { get; set; }
    public string Verdict { get; set; } = ""; // "领先", "持平", "落后"
}

/// <summary>
/// 品类信号
/// </summary>
public class CategorySignal
{
    /// <summary>
    /// 信号强度 (0-100)
    /// </summary>
    public double Strength { get; set; }

    /// <summary>
    /// 主要关联品类
    /// </summary>
    public List<CategoryAssociation> PrimaryCategories { get; set; } = new();

    /// <summary>
    /// 次要关联品类
    /// </summary>
    public List<CategoryAssociation> SecondaryCategories { get; set; } = new();

    /// <summary>
    /// 品类覆盖度
    /// </summary>
    public double CategoryCoverage { get; set; }

    /// <summary>
    /// 品类机会（品牌未被关联但有潜力的品类）
    /// </summary>
    public List<CategoryOpportunity> CategoryOpportunities { get; set; } = new();
}

/// <summary>
/// 品类关联
/// </summary>
public class CategoryAssociation
{
    public string Category { get; set; } = "";
    public int MentionCount { get; set; }
    public double AssociationStrength { get; set; }
    public List<string> RelatedKeywords { get; set; } = new();
}

/// <summary>
/// 品类机会
/// </summary>
public class CategoryOpportunity
{
    public string Category { get; set; } = "";
    public string Reason { get; set; } = "";
    public double PotentialScore { get; set; }
    public List<string> SuggestedActions { get; set; } = new();
}

/// <summary>
/// 三路径行动建议
/// </summary>
public class ThreePathRecommendation
{
    /// <summary>
    /// 路径1：创建新内容
    /// </summary>
    public ContentCreationPath CreateContent { get; set; } = new();

    /// <summary>
    /// 路径2：改进现有内容
    /// </summary>
    public ContentImprovementPath ImproveContent { get; set; } = new();

    /// <summary>
    /// 路径3：获取曝光
    /// </summary>
    public ExposurePath GetExposure { get; set; } = new();

    /// <summary>
    /// 推荐优先级
    /// </summary>
    public string RecommendedPriority { get; set; } = ""; // "create", "improve", "exposure"

    /// <summary>
    /// 优先级理由
    /// </summary>
    public string PriorityReason { get; set; } = "";
}

/// <summary>
/// 路径1：创建新内容
/// </summary>
public class ContentCreationPath
{
    /// <summary>
    /// 优先级 (1-10)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 建议创建的内容类型
    /// </summary>
    public List<ContentSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// 目标问题/场景
    /// </summary>
    public List<string> TargetScenarios { get; set; } = new();

    /// <summary>
    /// 预期影响
    /// </summary>
    public string ExpectedImpact { get; set; } = "";
}

/// <summary>
/// 内容建议
/// </summary>
public class ContentSuggestion
{
    public string ContentType { get; set; } = ""; // "faq", "article", "guide", "comparison"
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> KeyPoints { get; set; } = new();
    public string TargetKeyword { get; set; } = "";
    public int EstimatedEffort { get; set; } // 1-5
    public int ExpectedImpact { get; set; } // 1-5
}

/// <summary>
/// 路径2：改进现有内容
/// </summary>
public class ContentImprovementPath
{
    /// <summary>
    /// 优先级 (1-10)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 需要改进的内容
    /// </summary>
    public List<ImprovementSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// 预期影响
    /// </summary>
    public string ExpectedImpact { get; set; } = "";
}

/// <summary>
/// 改进建议
/// </summary>
public class ImprovementSuggestion
{
    public string ContentIdentifier { get; set; } = "";
    public string CurrentIssue { get; set; } = "";
    public string SuggestedImprovement { get; set; } = "";
    public List<string> SpecificActions { get; set; } = new();
    public int EstimatedEffort { get; set; }
    public int ExpectedImpact { get; set; }
}

/// <summary>
/// 路径3：获取曝光
/// </summary>
public class ExposurePath
{
    /// <summary>
    /// 优先级 (1-10)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 曝光机会
    /// </summary>
    public List<ExposureOpportunity> Opportunities { get; set; } = new();

    /// <summary>
    /// 预期影响
    /// </summary>
    public string ExpectedImpact { get; set; } = "";
}

/// <summary>
/// 曝光机会
/// </summary>
public class ExposureOpportunity
{
    public string Channel { get; set; } = ""; // "reddit", "quora", "wikipedia", "industry_forum"
    public string SpecificTarget { get; set; } = ""; // 具体的 subreddit、问题等
    public string ActionDescription { get; set; } = "";
    public List<string> Steps { get; set; } = new();
    public int EstimatedEffort { get; set; }
    public int ExpectedImpact { get; set; }
}

/// <summary>
/// 可执行任务
/// </summary>
public class ActionableTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = ""; // "content_creation", "content_improvement", "exposure"
    public int Priority { get; set; } // 1-10
    public int EstimatedEffort { get; set; } // 1-5 (小时)
    public int ExpectedImpact { get; set; } // 1-5
    public List<string> Steps { get; set; } = new();
    public List<string> Resources { get; set; } = new();
    public string TargetMetric { get; set; } = "";
    public string SuccessCriteria { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "pending"; // pending, in_progress, completed, skipped
}

/// <summary>
/// 完整的洞察报告
/// </summary>
public class InsightReport
{
    public string ProjectId { get; set; } = "";
    public string Brand { get; set; } = "";
    public FourSignalInsight Signals { get; set; } = new();
    public ThreePathRecommendation Recommendations { get; set; } = new();
    public List<ActionableTask> Tasks { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string ReportSummary { get; set; } = "";
}
