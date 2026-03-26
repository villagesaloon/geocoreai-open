namespace GeoCore.SaaS.Services.ContentBrief;

/// <summary>
/// 内容简报 - 数据驱动的内容创作指南
/// </summary>
public class ContentBriefReport
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Brand { get; set; } = "";
    public string TargetTopic { get; set; } = "";
    
    /// <summary>
    /// AI 对齐主题推荐
    /// </summary>
    public List<AlignedTopic> RecommendedTopics { get; set; } = new();
    
    /// <summary>
    /// 标题结构建议
    /// </summary>
    public HeadingStructure SuggestedStructure { get; set; } = new();
    
    /// <summary>
    /// 引用事实建议
    /// </summary>
    public List<CitableFact> CitableFacts { get; set; } = new();
    
    /// <summary>
    /// 关键词建议
    /// </summary>
    public List<KeywordSuggestion> KeywordSuggestions { get; set; } = new();
    
    /// <summary>
    /// 竞品内容分析
    /// </summary>
    public List<CompetitorContentAnalysis> CompetitorAnalysis { get; set; } = new();
    
    /// <summary>
    /// 内容优化清单
    /// </summary>
    public List<OptimizationChecklist> Checklist { get; set; } = new();
    
    /// <summary>
    /// 简报摘要
    /// </summary>
    public string Summary { get; set; } = "";
    
    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// AI 对齐主题 - AI 容易引用的主题
/// </summary>
public class AlignedTopic
{
    /// <summary>
    /// 主题名称
    /// </summary>
    public string Topic { get; set; } = "";
    
    /// <summary>
    /// 主题描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// AI 引用频率（基于历史数据）
    /// </summary>
    public double CitationFrequency { get; set; }
    
    /// <summary>
    /// 相关问题示例
    /// </summary>
    public List<string> RelatedQuestions { get; set; } = new();
    
    /// <summary>
    /// 推荐原因
    /// </summary>
    public string Reason { get; set; } = "";
    
    /// <summary>
    /// 优先级 (1-10)
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// 内容类型建议
    /// </summary>
    public string SuggestedContentType { get; set; } = ""; // article, faq, guide, comparison
}

/// <summary>
/// 标题结构建议
/// </summary>
public class HeadingStructure
{
    /// <summary>
    /// 建议的 H1 标题
    /// </summary>
    public string SuggestedH1 { get; set; } = "";
    
    /// <summary>
    /// 建议的 H2 标题列表
    /// </summary>
    public List<HeadingItem> H2Sections { get; set; } = new();
    
    /// <summary>
    /// 结构说明
    /// </summary>
    public string StructureRationale { get; set; } = "";
    
    /// <summary>
    /// 预估字数
    /// </summary>
    public int EstimatedWordCount { get; set; }
}

/// <summary>
/// 标题项
/// </summary>
public class HeadingItem
{
    public string Heading { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> H3Subsections { get; set; } = new();
    public List<string> KeyPoints { get; set; } = new();
    public int EstimatedWordCount { get; set; }
}

/// <summary>
/// 可引用事实 - AI 已引用的统计/事实
/// </summary>
public class CitableFact
{
    /// <summary>
    /// 事实内容
    /// </summary>
    public string Fact { get; set; } = "";
    
    /// <summary>
    /// 事实类型：statistic, quote, research, case_study
    /// </summary>
    public string FactType { get; set; } = "statistic";
    
    /// <summary>
    /// 来源
    /// </summary>
    public string Source { get; set; } = "";
    
    /// <summary>
    /// AI 引用次数
    /// </summary>
    public int CitationCount { get; set; }
    
    /// <summary>
    /// 引用该事实的 AI 平台
    /// </summary>
    public List<string> CitedByPlatforms { get; set; } = new();
    
    /// <summary>
    /// 使用建议
    /// </summary>
    public string UsageSuggestion { get; set; } = "";
    
    /// <summary>
    /// 可信度评分 (0-1)
    /// </summary>
    public double CredibilityScore { get; set; }
}

/// <summary>
/// 关键词建议
/// </summary>
public class KeywordSuggestion
{
    public string Keyword { get; set; } = "";
    public string KeywordType { get; set; } = ""; // primary, secondary, long_tail
    public int SearchVolume { get; set; }
    public double AICitationRate { get; set; }
    public string UsageContext { get; set; } = "";
    public int Priority { get; set; }
}

/// <summary>
/// 竞品内容分析
/// </summary>
public class CompetitorContentAnalysis
{
    public string Competitor { get; set; } = "";
    public double MentionRate { get; set; }
    public List<string> StrengthTopics { get; set; } = new();
    public List<string> WeaknessTopics { get; set; } = new();
    public string ContentStrategy { get; set; } = "";
    public List<string> Opportunities { get; set; } = new();
}

/// <summary>
/// 优化清单项
/// </summary>
public class OptimizationChecklist
{
    public string Category { get; set; } = ""; // structure, content, seo, ai_optimization
    public string Item { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsRequired { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// 内容简报导出格式
/// </summary>
public class ContentBriefExport
{
    public string Format { get; set; } = "markdown"; // markdown, html, json, pdf
    public string Content { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 主题分析请求
/// </summary>
public class TopicAnalysisRequest
{
    public int TaskId { get; set; }
    public string Brand { get; set; } = "";
    public string TargetTopic { get; set; } = "";
    public List<string>? Competitors { get; set; }
    public string ContentType { get; set; } = "article"; // article, faq, guide, comparison
    public int TargetWordCount { get; set; } = 1500;
}
