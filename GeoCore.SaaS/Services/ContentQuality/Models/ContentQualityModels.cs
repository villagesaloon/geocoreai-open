namespace GeoCore.SaaS.Services.ContentQuality.Models;

/// <summary>
/// 内容质量评估结果
/// </summary>
public class ContentQualityResult
{
    /// <summary>
    /// 综合可提取性评分 (0-10)
    /// </summary>
    public double ExtractabilityScore { get; set; }
    
    /// <summary>
    /// 评级：优秀/良好/一般/差
    /// </summary>
    public string Grade { get; set; } = "";
    
    /// <summary>
    /// 评级颜色 CSS 类
    /// </summary>
    public string GradeColor { get; set; } = "";
    
    /// <summary>
    /// 内容总词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 句子数量
    /// </summary>
    public int SentenceCount { get; set; }
    
    /// <summary>
    /// 事实密度指标
    /// </summary>
    public ClaimDensityMetric ClaimDensity { get; set; } = new();
    
    /// <summary>
    /// 信息密度指标（基于内容长度）
    /// </summary>
    public InformationDensityMetric InformationDensity { get; set; } = new();
    
    /// <summary>
    /// 答案前置指标
    /// </summary>
    public FrontloadingMetric Frontloading { get; set; } = new();
    
    /// <summary>
    /// 句子长度指标
    /// </summary>
    public SentenceLengthMetric SentenceLength { get; set; } = new();
    
    /// <summary>
    /// 实体密度指标
    /// </summary>
    public EntityDensityMetric EntityDensity { get; set; } = new();
    
    /// <summary>
    /// 优化建议列表
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
    
    #region aiseo-audit 审计指标 (3.13-3.19)
    
    /// <summary>
    /// 答案胶囊检测 (3.13)
    /// </summary>
    public AnswerCapsuleMetric? AnswerCapsule { get; set; }
    
    /// <summary>
    /// 章节长度分析 (3.14)
    /// </summary>
    public SectionLengthMetric? SectionLength { get; set; }
    
    /// <summary>
    /// 答案优先格式 (3.15)
    /// </summary>
    public AnswerFirstMetric? AnswerFirst { get; set; }
    
    /// <summary>
    /// Flesch 可读性 (3.17)
    /// </summary>
    public FleschReadabilityMetric? FleschReadability { get; set; }
    
    /// <summary>
    /// 引语归因检测 (3.19)
    /// </summary>
    public QuotationAttributionMetric? QuotationAttribution { get; set; }
    
    #endregion
    
    #region Listicle 优化指标 (3.20-3.22)
    
    /// <summary>
    /// Listicle 格式检测 (3.20)
    /// </summary>
    public ListicleFormatMetric? ListicleFormat { get; set; }
    
    /// <summary>
    /// 自我推广检测 (3.21)
    /// </summary>
    public SelfPromotionMetric? SelfPromotion { get; set; }
    
    /// <summary>
    /// 第三方引用建议 (3.22)
    /// </summary>
    public ThirdPartyReferenceMetric? ThirdPartyReference { get; set; }
    
    #endregion
    
    #region Schema 完整度指标 (3.18)
    
    /// <summary>
    /// Schema 完整度检测 (3.18)
    /// </summary>
    public SchemaCompletenessMetric? SchemaCompleteness { get; set; }
    
    #endregion
    
    #region 高级内容优化指标 (3.23-3.27)
    
    /// <summary>
    /// 内容类型策略建议 (3.23)
    /// </summary>
    public ContentTypeStrategy? ContentTypeStrategy { get; set; }
    
    /// <summary>
    /// 结构元素影响系数 (3.24)
    /// </summary>
    public StructuralElementsMetric? StructuralElements { get; set; }
    
    /// <summary>
    /// 最佳长度区间检测 (3.25)
    /// </summary>
    public OptimalLengthMetric? OptimalLength { get; set; }
    
    /// <summary>
    /// 可引用性评分 (3.26)
    /// </summary>
    public CitabilityScoreMetric? CitabilityScore { get; set; }
    
    /// <summary>
    /// 内容前 30% 优化 (3.27)
    /// </summary>
    public Front30PercentMetric? Front30Percent { get; set; }
    
    #endregion
    
    #region 补充优化指标 (3.28-3.30)
    
    /// <summary>
    /// 段落长度优化 (3.28)
    /// </summary>
    public ParagraphLengthMetric? ParagraphLength { get; set; }
    
    /// <summary>
    /// 标题策略优化 (3.29)
    /// </summary>
    public TitleStrategyMetric? TitleStrategy { get; set; }
    
    /// <summary>
    /// 实体密度增强 (3.30)
    /// </summary>
    public EnhancedEntityDensityMetric? EnhancedEntityDensity { get; set; }
    
    #endregion
}

/// <summary>
/// 事实密度指标
/// </summary>
public class ClaimDensityMetric
{
    /// <summary>
    /// 每100词的 claim 数量
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 目标值
    /// </summary>
    public double Target { get; set; } = 4.0;
    
    /// <summary>
    /// 是否达标
    /// </summary>
    public bool IsGood => Value >= Target;
    
    /// <summary>
    /// 提取到的 claims 列表
    /// </summary>
    public List<ExtractedClaim> Claims { get; set; } = new();
}

/// <summary>
/// 提取的事实/声明
/// </summary>
public class ExtractedClaim
{
    /// <summary>
    /// 原文
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 类型：number, statistic, fact, citation
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 在内容中的位置（词数）
    /// </summary>
    public int Position { get; set; }
}

/// <summary>
/// 信息密度指标（基于内容长度）
/// </summary>
public class InformationDensityMetric
{
    /// <summary>
    /// 内容词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 最佳范围下限
    /// </summary>
    public int OptimalMin { get; set; } = 800;
    
    /// <summary>
    /// 最佳范围上限
    /// </summary>
    public int OptimalMax { get; set; } = 1500;
    
    /// <summary>
    /// 是否在最佳范围内
    /// </summary>
    public bool IsOptimal => WordCount >= OptimalMin && WordCount <= OptimalMax;
    
    /// <summary>
    /// 状态描述
    /// </summary>
    public string Status { get; set; } = "";
}

/// <summary>
/// 答案前置指标
/// </summary>
public class FrontloadingMetric
{
    /// <summary>
    /// 前100词中的 claims 数量
    /// </summary>
    public int ClaimsInFirst100 { get; set; }
    
    /// <summary>
    /// 前300词中的 claims 数量
    /// </summary>
    public int ClaimsInFirst300 { get; set; }
    
    /// <summary>
    /// 第一个 claim 的位置（词数）
    /// </summary>
    public int FirstClaimPosition { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 是否达标（前100词至少3个claims）
    /// </summary>
    public bool IsGood => ClaimsInFirst100 >= 3;
}

/// <summary>
/// 句子长度指标
/// </summary>
public class SentenceLengthMetric
{
    /// <summary>
    /// 平均句子长度（词数）
    /// </summary>
    public double AverageLength { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 最佳范围下限
    /// </summary>
    public int OptimalMin { get; set; } = 15;
    
    /// <summary>
    /// 最佳范围上限
    /// </summary>
    public int OptimalMax { get; set; } = 20;
    
    /// <summary>
    /// 是否在最佳范围内
    /// </summary>
    public bool IsOptimal => AverageLength >= OptimalMin && AverageLength <= OptimalMax;
    
    /// <summary>
    /// 过长句子数量（>25词）
    /// </summary>
    public int LongSentenceCount { get; set; }
    
    /// <summary>
    /// 过短句子数量（<10词）
    /// </summary>
    public int ShortSentenceCount { get; set; }
}

/// <summary>
/// 实体密度指标 (3.16 增强版)
/// 原理：实体清晰度指标，最佳范围 2-8/100词
/// </summary>
public class EntityDensityMetric
{
    /// <summary>
    /// 每100词的实体数量
    /// </summary>
    public double Value { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 目标范围下限
    /// </summary>
    public double TargetMin { get; set; } = 2.0;
    
    /// <summary>
    /// 目标范围上限
    /// </summary>
    public double TargetMax { get; set; } = 8.0;
    
    /// <summary>
    /// 目标值（兼容旧版）
    /// </summary>
    public double Target { get; set; } = 2.0;
    
    /// <summary>
    /// 是否在最佳范围内
    /// </summary>
    public bool IsOptimal => Value >= TargetMin && Value <= TargetMax;
    
    /// <summary>
    /// 是否达标（兼容旧版）
    /// </summary>
    public bool IsGood => Value >= Target;
    
    /// <summary>
    /// 提取到的实体列表
    /// </summary>
    public List<ExtractedEntity> Entities { get; set; } = new();
    
    /// <summary>
    /// 按类型分组的实体统计
    /// </summary>
    public Dictionary<string, int> EntityTypeStats { get; set; } = new();
    
    /// <summary>
    /// 实体多样性评分 (0-10)
    /// </summary>
    public double DiversityScore { get; set; }
    
    /// <summary>
    /// 高价值实体数量（品牌、专家、数据）
    /// </summary>
    public int HighValueEntityCount { get; set; }
    
    /// <summary>
    /// 密度状态：low, optimal, high
    /// </summary>
    public string DensityStatus => Value < TargetMin ? "low" : (Value > TargetMax ? "high" : "optimal");
    
    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => DensityStatus switch
    {
        "optimal" => "text-green-600",
        "low" => "text-yellow-600",
        "high" => "text-orange-600",
        _ => "text-gray-600"
    };
}

/// <summary>
/// 提取的实体
/// </summary>
public class ExtractedEntity
{
    /// <summary>
    /// 实体文本
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 类型：brand, person, product, location, number, date
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 出现次数
    /// </summary>
    public int Count { get; set; } = 1;
}

/// <summary>
/// 优化建议
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// 建议类型：claim_density, information_density, frontloading, sentence_length, entity_density
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 优先级：high, medium, low
    /// </summary>
    public string Priority { get; set; } = "";
    
    /// <summary>
    /// 优先级颜色
    /// </summary>
    public string PriorityColor => Priority switch
    {
        "high" => "text-red-600",
        "medium" => "text-yellow-600",
        "low" => "text-blue-600",
        _ => "text-gray-600"
    };
    
    /// <summary>
    /// 建议消息
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// 示例
    /// </summary>
    public string? Example { get; set; }
    
    /// <summary>
    /// 图标
    /// </summary>
    public string Icon => Type switch
    {
        "claim_density" => "📊",
        "information_density" => "📏",
        "frontloading" => "⬆️",
        "sentence_length" => "✂️",
        "entity_density" => "🏷️",
        _ => "💡"
    };
}

/// <summary>
/// 内容质量分析请求
/// </summary>
public class ContentQualityRequest
{
    /// <summary>
    /// 要分析的内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 语言：zh, en, ja 等
    /// </summary>
    public string Language { get; set; } = "zh";
    
    /// <summary>
    /// 是否使用 AI 辅助提取（更准确但更慢）
    /// </summary>
    public bool UseAiExtraction { get; set; } = false;
}

/// <summary>
/// 批量分析请求
/// </summary>
public class BatchContentQualityRequest
{
    /// <summary>
    /// 问答对列表
    /// </summary>
    public List<QAPair> Items { get; set; } = new();
    
    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh";
}

/// <summary>
/// 问答对
/// </summary>
public class QAPair
{
    /// <summary>
    /// 问题
    /// </summary>
    public string Question { get; set; } = "";
    
    /// <summary>
    /// 答案
    /// </summary>
    public string Answer { get; set; } = "";
}

/// <summary>
/// 批量分析结果
/// </summary>
public class BatchContentQualityResult
{
    /// <summary>
    /// 各项分析结果
    /// </summary>
    public List<ContentQualityResultWithQuestion> Items { get; set; } = new();
    
    /// <summary>
    /// 平均评分
    /// </summary>
    public double AverageScore { get; set; }
    
    /// <summary>
    /// 总体评级
    /// </summary>
    public string OverallGrade { get; set; } = "";
    
    /// <summary>
    /// 汇总建议
    /// </summary>
    public List<OptimizationSuggestion> TopSuggestions { get; set; } = new();
}

/// <summary>
/// 带问题的分析结果
/// </summary>
public class ContentQualityResultWithQuestion : ContentQualityResult
{
    /// <summary>
    /// 原问题
    /// </summary>
    public string Question { get; set; } = "";
}

#region aiseo-audit 审计模型 (3.13-3.19)

/// <summary>
/// 答案胶囊检测结果 (3.13)
/// 原理：72.4% 被 AI 引用的内容具有"答案胶囊"特征
/// </summary>
public class AnswerCapsuleMetric
{
    /// <summary>
    /// 是否检测到答案胶囊
    /// </summary>
    public bool HasCapsule { get; set; }
    
    /// <summary>
    /// 检测到的胶囊列表
    /// </summary>
    public List<AnswerCapsule> Capsules { get; set; } = new();
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 胶囊覆盖率（胶囊词数/总词数）
    /// </summary>
    public double CoverageRate { get; set; }
}

/// <summary>
/// 单个答案胶囊
/// </summary>
public class AnswerCapsule
{
    /// <summary>
    /// 胶囊文本
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 在内容中的位置（段落索引）
    /// </summary>
    public int ParagraphIndex { get; set; }
    
    /// <summary>
    /// 胶囊质量评分 (0-10)
    /// </summary>
    public double QualityScore { get; set; }
    
    /// <summary>
    /// 是否包含数字/统计
    /// </summary>
    public bool HasStatistics { get; set; }
    
    /// <summary>
    /// 是否包含定义/结论
    /// </summary>
    public bool HasDefinition { get; set; }
    
    /// <summary>
    /// 是否可独立成句
    /// </summary>
    public bool IsSelfContained { get; set; }
}

/// <summary>
/// 章节长度分析结果 (3.14)
/// 原理：120-180 词的章节段落被引用率 +70%
/// </summary>
public class SectionLengthMetric
{
    /// <summary>
    /// 检测到的章节列表
    /// </summary>
    public List<ContentSection> Sections { get; set; } = new();
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 最佳长度章节数量（120-180 词）
    /// </summary>
    public int OptimalSectionCount { get; set; }
    
    /// <summary>
    /// 过短章节数量（<80 词）
    /// </summary>
    public int TooShortCount { get; set; }
    
    /// <summary>
    /// 过长章节数量（>250 词）
    /// </summary>
    public int TooLongCount { get; set; }
    
    /// <summary>
    /// 最佳长度章节占比
    /// </summary>
    public double OptimalRatio => Sections.Count > 0 
        ? (double)OptimalSectionCount / Sections.Count 
        : 0;
}

/// <summary>
/// 内容章节
/// </summary>
public class ContentSection
{
    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 标题级别（H1=1, H2=2, H3=3, 无标题=0）
    /// </summary>
    public int HeadingLevel { get; set; }
    
    /// <summary>
    /// 章节内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 长度状态：optimal, too_short, too_long
    /// </summary>
    public string LengthStatus { get; set; } = "";
    
    /// <summary>
    /// 长度状态颜色
    /// </summary>
    public string StatusColor => LengthStatus switch
    {
        "optimal" => "text-green-600",
        "too_short" => "text-yellow-600",
        "too_long" => "text-red-600",
        _ => "text-gray-600"
    };
}

/// <summary>
/// 答案优先格式检测结果 (3.15)
/// 原理：前 40-60 词包含核心答案，引用率 +140%
/// </summary>
public class AnswerFirstMetric
{
    /// <summary>
    /// 前 40-60 词是否包含核心答案
    /// </summary>
    public bool HasAnswerFirst { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 前 60 词文本
    /// </summary>
    public string First60Words { get; set; } = "";
    
    /// <summary>
    /// 前 60 词中的 claims 数量
    /// </summary>
    public int ClaimsInFirst60 { get; set; }
    
    /// <summary>
    /// 是否以直接回答开头（是/否/定义）
    /// </summary>
    public bool StartsWithDirectAnswer { get; set; }
    
    /// <summary>
    /// 是否包含关键数字
    /// </summary>
    public bool HasKeyNumbers { get; set; }
    
    /// <summary>
    /// 是否包含核心结论
    /// </summary>
    public bool HasConclusion { get; set; }
    
    /// <summary>
    /// 第一个 claim 的位置（词数）
    /// </summary>
    public int FirstClaimPosition { get; set; }
}

/// <summary>
/// Flesch 可读性指标 (3.17)
/// 原理：60-70 分最佳，便于 AI 压缩摘要
/// </summary>
public class FleschReadabilityMetric
{
    /// <summary>
    /// Flesch Reading Ease 分数 (0-100)
    /// </summary>
    public double FleschScore { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 可读性级别
    /// </summary>
    public string Level { get; set; } = "";
    
    /// <summary>
    /// 级别颜色
    /// </summary>
    public string LevelColor => FleschScore switch
    {
        >= 60 and <= 70 => "text-green-600",
        >= 50 and < 60 or > 70 and <= 80 => "text-yellow-600",
        _ => "text-red-600"
    };
    
    /// <summary>
    /// 平均句子长度（词数）
    /// </summary>
    public double AvgSentenceLength { get; set; }
    
    /// <summary>
    /// 平均音节数/词
    /// </summary>
    public double AvgSyllablesPerWord { get; set; }
}

/// <summary>
/// 引语归因检测结果 (3.19)
/// 原理：有引语归因的内容可见度 +30-40%
/// </summary>
public class QuotationAttributionMetric
{
    /// <summary>
    /// 是否有引语归因
    /// </summary>
    public bool HasAttribution { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 检测到的引语列表
    /// </summary>
    public List<AttributedQuote> Quotes { get; set; } = new();
    
    /// <summary>
    /// 有归因的引语数量
    /// </summary>
    public int AttributedCount { get; set; }
    
    /// <summary>
    /// 无归因的引语数量
    /// </summary>
    public int UnattributedCount { get; set; }
}

/// <summary>
/// 带归因的引语
/// </summary>
public class AttributedQuote
{
    /// <summary>
    /// 引语文本
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 归因来源（人名/机构）
    /// </summary>
    public string? Attribution { get; set; }
    
    /// <summary>
    /// 是否有归因
    /// </summary>
    public bool HasAttribution => !string.IsNullOrEmpty(Attribution);
}

#endregion

#region Listicle 优化模型 (3.20-3.22)

/// <summary>
/// Listicle 格式检测结果 (3.20)
/// 原理：ChatGPT/Perplexity 大量引用 listicles 格式内容
/// </summary>
public class ListicleFormatMetric
{
    /// <summary>
    /// 是否为 Listicle 格式
    /// </summary>
    public bool IsListicle { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// Listicle 类型：numbered, bulleted, mixed, none
    /// </summary>
    public string ListType { get; set; } = "none";
    
    /// <summary>
    /// 列表项数量
    /// </summary>
    public int ItemCount { get; set; }
    
    /// <summary>
    /// 检测到的列表项
    /// </summary>
    public List<ListicleItem> Items { get; set; } = new();
    
    /// <summary>
    /// 列表覆盖率（列表内容占总内容比例）
    /// </summary>
    public double CoverageRate { get; set; }
    
    /// <summary>
    /// 是否有清晰的标题
    /// </summary>
    public bool HasClearTitle { get; set; }
    
    /// <summary>
    /// 是否有总结段落
    /// </summary>
    public bool HasSummary { get; set; }
}

/// <summary>
/// 单个列表项
/// </summary>
public class ListicleItem
{
    /// <summary>
    /// 列表项序号（如果是编号列表）
    /// </summary>
    public int? Number { get; set; }
    
    /// <summary>
    /// 列表项标题/要点
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 列表项内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 是否包含具体数据/统计
    /// </summary>
    public bool HasData { get; set; }
    
    /// <summary>
    /// 是否包含可操作建议
    /// </summary>
    public bool HasActionable { get; set; }
}

/// <summary>
/// 自我推广检测结果 (3.21)
/// 原理：自我推广型 listicle 会被 AI 惩罚
/// </summary>
public class SelfPromotionMetric
{
    /// <summary>
    /// 是否检测到自我推广
    /// </summary>
    public bool HasSelfPromotion { get; set; }
    
    /// <summary>
    /// 评分 (0-10)，越高越好（无自我推广）
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 自我推广强度 (0-100)
    /// </summary>
    public double PromotionIntensity { get; set; }
    
    /// <summary>
    /// 检测到的推广信号
    /// </summary>
    public List<PromotionSignal> Signals { get; set; } = new();
    
    /// <summary>
    /// 品牌提及次数
    /// </summary>
    public int BrandMentionCount { get; set; }
    
    /// <summary>
    /// 产品提及次数
    /// </summary>
    public int ProductMentionCount { get; set; }
    
    /// <summary>
    /// CTA（行动号召）次数
    /// </summary>
    public int CtaCount { get; set; }
    
    /// <summary>
    /// 风险等级：low, medium, high
    /// </summary>
    public string RiskLevel { get; set; } = "low";
    
    /// <summary>
    /// 风险颜色
    /// </summary>
    public string RiskColor => RiskLevel switch
    {
        "low" => "text-green-600",
        "medium" => "text-yellow-600",
        "high" => "text-red-600",
        _ => "text-gray-600"
    };
}

/// <summary>
/// 推广信号
/// </summary>
public class PromotionSignal
{
    /// <summary>
    /// 信号类型：brand_mention, product_push, cta, superlative, exclusive_claim
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 信号文本
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 在内容中的位置
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// 严重程度 (1-5)
    /// </summary>
    public int Severity { get; set; }
}

/// <summary>
/// 第三方引用建议结果 (3.22)
/// 原理：让其他可信来源引用你，提升权威性
/// </summary>
public class ThirdPartyReferenceMetric
{
    /// <summary>
    /// 是否有第三方引用
    /// </summary>
    public bool HasThirdPartyReferences { get; set; }
    
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 检测到的第三方引用
    /// </summary>
    public List<ThirdPartyReference> References { get; set; } = new();
    
    /// <summary>
    /// 引用来源多样性评分 (0-10)
    /// </summary>
    public double DiversityScore { get; set; }
    
    /// <summary>
    /// 权威来源数量
    /// </summary>
    public int AuthoritativeSourceCount { get; set; }
    
    /// <summary>
    /// 建议添加的引用类型
    /// </summary>
    public List<string> SuggestedReferenceTypes { get; set; } = new();
}

/// <summary>
/// 第三方引用
/// </summary>
public class ThirdPartyReference
{
    /// <summary>
    /// 引用来源名称
    /// </summary>
    public string SourceName { get; set; } = "";
    
    /// <summary>
    /// 来源类型：academic, industry_report, news, expert, government, organization
    /// </summary>
    public string SourceType { get; set; } = "";
    
    /// <summary>
    /// 引用文本
    /// </summary>
    public string Text { get; set; } = "";
    
    /// <summary>
    /// 权威性评分 (1-10)
    /// </summary>
    public int AuthorityScore { get; set; }
    
    /// <summary>
    /// 是否有具体数据
    /// </summary>
    public bool HasSpecificData { get; set; }
}

#endregion

#region Schema 完整度模型 (3.18)

/// <summary>
/// Schema 完整度检测结果 (3.18)
/// 原理：Schema 标记增强权威信号，提升 AI 引用率
/// </summary>
public class SchemaCompletenessMetric
{
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 是否有 Schema 标记
    /// </summary>
    public bool HasSchema { get; set; }
    
    /// <summary>
    /// 检测到的 Schema 类型
    /// </summary>
    public List<DetectedSchema> DetectedSchemas { get; set; } = new();
    
    /// <summary>
    /// 建议添加的 Schema 类型
    /// </summary>
    public List<SchemaSuggestion> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 完整度百分比 (0-100)
    /// </summary>
    public double CompletenessPercent { get; set; }
    
    /// <summary>
    /// 是否有 FAQPage Schema
    /// </summary>
    public bool HasFAQSchema { get; set; }
    
    /// <summary>
    /// 是否有 HowTo Schema
    /// </summary>
    public bool HasHowToSchema { get; set; }
    
    /// <summary>
    /// 是否有 Article Schema
    /// </summary>
    public bool HasArticleSchema { get; set; }
    
    /// <summary>
    /// 是否有 Organization Schema
    /// </summary>
    public bool HasOrganizationSchema { get; set; }
}

/// <summary>
/// 检测到的 Schema
/// </summary>
public class DetectedSchema
{
    /// <summary>
    /// Schema 类型：FAQPage, HowTo, Article, Organization, Product, etc.
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 完整度评分 (0-10)
    /// </summary>
    public double CompletenessScore { get; set; }
    
    /// <summary>
    /// 缺失的必需字段
    /// </summary>
    public List<string> MissingRequiredFields { get; set; } = new();
    
    /// <summary>
    /// 缺失的推荐字段
    /// </summary>
    public List<string> MissingRecommendedFields { get; set; } = new();
    
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }
}

/// <summary>
/// Schema 建议
/// </summary>
public class SchemaSuggestion
{
    /// <summary>
    /// 建议的 Schema 类型
    /// </summary>
    public string SchemaType { get; set; } = "";
    
    /// <summary>
    /// 建议原因
    /// </summary>
    public string Reason { get; set; } = "";
    
    /// <summary>
    /// 优先级：high, medium, low
    /// </summary>
    public string Priority { get; set; } = "medium";
    
    /// <summary>
    /// 预期收益
    /// </summary>
    public string ExpectedBenefit { get; set; } = "";
}

#endregion

#region 内容类型引用率基准 (3.23)

/// <summary>
/// 内容类型引用率基准 (3.23)
/// 原理：综合指南 67% > 对比 61% > FAQ 58% > 操作 54% > 观点 18%
/// </summary>
public class ContentTypeBenchmark
{
    /// <summary>
    /// 内容类型：guide, comparison, faq, howto, opinion, listicle, review
    /// </summary>
    public string ContentType { get; set; } = "";
    
    /// <summary>
    /// 内容类型中文名
    /// </summary>
    public string ContentTypeName { get; set; } = "";
    
    /// <summary>
    /// 基准引用率 (0-100%)
    /// </summary>
    public double BenchmarkCitationRate { get; set; }
    
    /// <summary>
    /// 推荐优先级 (1-5，1最高)
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// 适用场景
    /// </summary>
    public List<string> UseCases { get; set; } = new();
    
    /// <summary>
    /// 最佳实践建议
    /// </summary>
    public List<string> BestPractices { get; set; } = new();
}

/// <summary>
/// 内容类型策略建议
/// </summary>
public class ContentTypeStrategy
{
    /// <summary>
    /// 推荐的内容类型（按引用率排序）
    /// </summary>
    public List<ContentTypeBenchmark> RecommendedTypes { get; set; } = new();
    
    /// <summary>
    /// 当前内容检测到的类型
    /// </summary>
    public string DetectedType { get; set; } = "";
    
    /// <summary>
    /// 当前类型的基准引用率
    /// </summary>
    public double CurrentBenchmark { get; set; }
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

#endregion

#region 结构元素影响系数 (3.24)

/// <summary>
/// 结构元素影响系数 (3.24)
/// 原理：H2/H3 3.2x，表格 2.8x，FAQ 10+ +156%
/// </summary>
public class StructuralElementsMetric
{
    /// <summary>
    /// 综合评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// H2 标题数量
    /// </summary>
    public int H2Count { get; set; }
    
    /// <summary>
    /// H3 标题数量
    /// </summary>
    public int H3Count { get; set; }
    
    /// <summary>
    /// 表格数量
    /// </summary>
    public int TableCount { get; set; }
    
    /// <summary>
    /// 列表数量
    /// </summary>
    public int ListCount { get; set; }
    
    /// <summary>
    /// FAQ 数量
    /// </summary>
    public int FaqCount { get; set; }
    
    /// <summary>
    /// 代码块数量
    /// </summary>
    public int CodeBlockCount { get; set; }
    
    /// <summary>
    /// 引用块数量
    /// </summary>
    public int BlockquoteCount { get; set; }
    
    /// <summary>
    /// 结构元素影响系数（加权）
    /// </summary>
    public double ImpactMultiplier { get; set; }
    
    /// <summary>
    /// 各元素详情
    /// </summary>
    public List<StructuralElement> Elements { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 结构元素详情
/// </summary>
public class StructuralElement
{
    /// <summary>
    /// 元素类型：h2, h3, table, list, faq, code, blockquote
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 数量
    /// </summary>
    public int Count { get; set; }
    
    /// <summary>
    /// 影响系数
    /// </summary>
    public double ImpactFactor { get; set; }
    
    /// <summary>
    /// 是否达到最佳数量
    /// </summary>
    public bool IsOptimal { get; set; }
    
    /// <summary>
    /// 建议数量
    /// </summary>
    public string RecommendedRange { get; set; } = "";
}

#endregion

#region 最佳长度区间检测 (3.25)

/// <summary>
/// 最佳长度区间检测 (3.25)
/// 原理：2,500-4,000 词最佳，密度 > 长度
/// </summary>
public class OptimalLengthMetric
{
    /// <summary>
    /// 评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 当前词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 最佳范围下限
    /// </summary>
    public int OptimalMin { get; set; } = 2500;
    
    /// <summary>
    /// 最佳范围上限
    /// </summary>
    public int OptimalMax { get; set; } = 4000;
    
    /// <summary>
    /// 是否在最佳范围内
    /// </summary>
    public bool IsOptimal => WordCount >= OptimalMin && WordCount <= OptimalMax;
    
    /// <summary>
    /// 长度状态：short, optimal, long
    /// </summary>
    public string LengthStatus => WordCount < OptimalMin ? "short" : (WordCount > OptimalMax ? "long" : "optimal");
    
    /// <summary>
    /// 状态颜色
    /// </summary>
    public string StatusColor => LengthStatus switch
    {
        "optimal" => "text-green-600",
        "short" => "text-yellow-600",
        "long" => "text-orange-600",
        _ => "text-gray-600"
    };
    
    /// <summary>
    /// 内容密度评分（密度 > 长度）
    /// </summary>
    public double DensityScore { get; set; }
    
    /// <summary>
    /// 建议
    /// </summary>
    public string Suggestion { get; set; } = "";
}

#endregion

#region 可引用性评分 (3.26)

/// <summary>
/// 可引用性评分 (3.26)
/// 原理：134-167 词最佳，自包含、事实密集、直接回答问题
/// </summary>
public class CitabilityScoreMetric
{
    /// <summary>
    /// 综合可引用性评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 可引用段落数量
    /// </summary>
    public int CitableParagraphCount { get; set; }
    
    /// <summary>
    /// 总段落数量
    /// </summary>
    public int TotalParagraphCount { get; set; }
    
    /// <summary>
    /// 可引用段落比例
    /// </summary>
    public double CitableRatio => TotalParagraphCount > 0 ? (double)CitableParagraphCount / TotalParagraphCount : 0;
    
    /// <summary>
    /// 最佳段落（可被 AI 引用的段落）
    /// </summary>
    public List<CitableParagraph> TopCitableParagraphs { get; set; } = new();
    
    /// <summary>
    /// 需要优化的段落
    /// </summary>
    public List<ParagraphOptimization> ParagraphsToOptimize { get; set; } = new();
}

/// <summary>
/// 可引用段落
/// </summary>
public class CitableParagraph
{
    /// <summary>
    /// 段落索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 段落内容（截取前200字符）
    /// </summary>
    public string Preview { get; set; } = "";
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 可引用性评分 (0-10)
    /// </summary>
    public double CitabilityScore { get; set; }
    
    /// <summary>
    /// 是否自包含
    /// </summary>
    public bool IsSelfContained { get; set; }
    
    /// <summary>
    /// 事实密度
    /// </summary>
    public double FactDensity { get; set; }
    
    /// <summary>
    /// 是否直接回答问题
    /// </summary>
    public bool IsDirectAnswer { get; set; }
}

/// <summary>
/// 段落优化建议
/// </summary>
public class ParagraphOptimization
{
    /// <summary>
    /// 段落索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 段落预览
    /// </summary>
    public string Preview { get; set; } = "";
    
    /// <summary>
    /// 当前问题
    /// </summary>
    public List<string> Issues { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

#endregion

#region 内容前 30% 优化 (3.27)

/// <summary>
/// 内容前 30% 优化 (3.27)
/// 原理：44% LLM 引用来自前 30% 内容
/// </summary>
public class Front30PercentMetric
{
    /// <summary>
    /// 前 30% 内容评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 前 30% 词数
    /// </summary>
    public int Front30WordCount { get; set; }
    
    /// <summary>
    /// 总词数
    /// </summary>
    public int TotalWordCount { get; set; }
    
    /// <summary>
    /// 前 30% 事实密度
    /// </summary>
    public double Front30ClaimDensity { get; set; }
    
    /// <summary>
    /// 后 70% 事实密度
    /// </summary>
    public double Back70ClaimDensity { get; set; }
    
    /// <summary>
    /// 前 30% 是否优于后 70%
    /// </summary>
    public bool IsFrontLoaded => Front30ClaimDensity >= Back70ClaimDensity;
    
    /// <summary>
    /// 前 30% 包含的关键元素
    /// </summary>
    public Front30Elements Elements { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 前 30% 关键元素
/// </summary>
public class Front30Elements
{
    /// <summary>
    /// 是否有答案胶囊
    /// </summary>
    public bool HasAnswerCapsule { get; set; }
    
    /// <summary>
    /// 是否有核心定义
    /// </summary>
    public bool HasCoreDefinition { get; set; }
    
    /// <summary>
    /// 是否有关键数据
    /// </summary>
    public bool HasKeyStatistics { get; set; }
    
    /// <summary>
    /// 是否有专家引语
    /// </summary>
    public bool HasExpertQuote { get; set; }
    
    /// <summary>
    /// 事实数量
    /// </summary>
    public int ClaimCount { get; set; }
    
    /// <summary>
    /// 实体数量
    /// </summary>
    public int EntityCount { get; set; }
}

#endregion

#region 段落长度优化 (3.28)

/// <summary>
/// 段落长度优化 (3.28)
/// 原理：120-180 词段落 +70% 引用率
/// </summary>
public class ParagraphLengthMetric
{
    /// <summary>
    /// 综合评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 总段落数
    /// </summary>
    public int TotalParagraphs { get; set; }
    
    /// <summary>
    /// 最佳长度段落数 (120-180词)
    /// </summary>
    public int OptimalParagraphs { get; set; }
    
    /// <summary>
    /// 过短段落数 (<120词)
    /// </summary>
    public int ShortParagraphs { get; set; }
    
    /// <summary>
    /// 过长段落数 (>180词)
    /// </summary>
    public int LongParagraphs { get; set; }
    
    /// <summary>
    /// 最佳长度比例
    /// </summary>
    public double OptimalRatio => TotalParagraphs > 0 ? (double)OptimalParagraphs / TotalParagraphs : 0;
    
    /// <summary>
    /// 平均段落长度
    /// </summary>
    public double AverageLength { get; set; }
    
    /// <summary>
    /// 段落详情（前5个需要优化的）
    /// </summary>
    public List<ParagraphDetail> ParagraphsToOptimize { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// 段落详情
/// </summary>
public class ParagraphDetail
{
    /// <summary>
    /// 段落索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// 状态：short, optimal, long
    /// </summary>
    public string Status { get; set; } = "";
    
    /// <summary>
    /// 预览
    /// </summary>
    public string Preview { get; set; } = "";
}

#endregion

#region 标题策略优化 (3.29)

/// <summary>
/// 标题策略优化 (3.29)
/// 原理：问号标题 -0.9 引用，直陈式更佳
/// </summary>
public class TitleStrategyMetric
{
    /// <summary>
    /// 综合评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 检测到的标题
    /// </summary>
    public string DetectedTitle { get; set; } = "";
    
    /// <summary>
    /// 标题类型：question, statement, howto, listicle
    /// </summary>
    public string TitleType { get; set; } = "";
    
    /// <summary>
    /// 是否为问号标题
    /// </summary>
    public bool IsQuestionTitle { get; set; }
    
    /// <summary>
    /// 标题长度
    /// </summary>
    public int TitleLength { get; set; }
    
    /// <summary>
    /// 是否包含数字
    /// </summary>
    public bool HasNumber { get; set; }
    
    /// <summary>
    /// 是否包含年份
    /// </summary>
    public bool HasYear { get; set; }
    
    /// <summary>
    /// 引用率影响系数
    /// </summary>
    public double CitationImpact { get; set; }
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 推荐的标题变体
    /// </summary>
    public List<string> RecommendedVariants { get; set; } = new();
}

#endregion

#region 实体密度增强 (3.30)

/// <summary>
/// 实体密度增强 (3.30)
/// 原理：实体密度 15+ = 4.8x 引用率
/// </summary>
public class EnhancedEntityDensityMetric
{
    /// <summary>
    /// 综合评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 总实体数
    /// </summary>
    public int TotalEntities { get; set; }
    
    /// <summary>
    /// 每100词实体数
    /// </summary>
    public double DensityPer100Words { get; set; }
    
    /// <summary>
    /// 是否达到高引用阈值 (15+)
    /// </summary>
    public bool IsHighDensity => DensityPer100Words >= 15;
    
    /// <summary>
    /// 引用率倍数
    /// </summary>
    public double CitationMultiplier => DensityPer100Words >= 15 ? 4.8 : (DensityPer100Words >= 10 ? 2.5 : 1.0);
    
    /// <summary>
    /// 高价值实体数（专有名词、数据、专家名）
    /// </summary>
    public int HighValueEntities { get; set; }
    
    /// <summary>
    /// 实体类型分布
    /// </summary>
    public Dictionary<string, int> EntityTypeDistribution { get; set; } = new();
    
    /// <summary>
    /// 缺失的实体类型
    /// </summary>
    public List<string> MissingEntityTypes { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

#endregion
