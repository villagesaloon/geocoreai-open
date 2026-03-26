using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// 内容质量分析器
/// 基于 MIT GEO Paper 和 Dejan AI 研究的可提取性评估
/// </summary>
public class ContentQualityAnalyzer
{
    private readonly ILogger<ContentQualityAnalyzer> _logger;
    private readonly ClaimExtractor _claimExtractor;
    private readonly EntityExtractor _entityExtractor;
    private readonly LanguageConfigProvider _configProvider;
    private readonly AiseoAuditAnalyzer _aiseoAuditAnalyzer;
    private readonly ListicleAnalyzer _listicleAnalyzer;
    private readonly SchemaCompletenessAnalyzer _schemaAnalyzer;
    private readonly AdvancedContentAnalyzer _advancedAnalyzer;

    // 权重配置（基于 GEO Analyzer）
    private const double WeightClaimDensity = 0.30;
    private const double WeightInformationDensity = 0.20;
    private const double WeightFrontloading = 0.25;
    private const double WeightSentenceLength = 0.15;
    private const double WeightEntityDensity = 0.10;

    public ContentQualityAnalyzer(
        ILogger<ContentQualityAnalyzer> logger,
        ClaimExtractor claimExtractor,
        EntityExtractor entityExtractor,
        LanguageConfigProvider configProvider,
        AiseoAuditAnalyzer aiseoAuditAnalyzer,
        ListicleAnalyzer listicleAnalyzer,
        SchemaCompletenessAnalyzer schemaAnalyzer,
        AdvancedContentAnalyzer advancedAnalyzer)
    {
        _logger = logger;
        _claimExtractor = claimExtractor;
        _entityExtractor = entityExtractor;
        _configProvider = configProvider;
        _aiseoAuditAnalyzer = aiseoAuditAnalyzer;
        _listicleAnalyzer = listicleAnalyzer;
        _schemaAnalyzer = schemaAnalyzer;
        _advancedAnalyzer = advancedAnalyzer;
    }

    /// <summary>
    /// 分析内容质量（异步）
    /// </summary>
    public async Task<ContentQualityResult> AnalyzeAsync(string content, string language = "zh")
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ContentQualityResult
            {
                ExtractabilityScore = 0,
                Grade = "差",
                GradeColor = "text-red-600",
                Suggestions = new List<OptimizationSuggestion>
                {
                    new() { Type = "content", Priority = "high", Message = "内容为空" }
                }
            };
        }

        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var wordCount = CountWords(content, settings);
        var sentences = SplitSentences(content, settings);
        var claims = await _claimExtractor.ExtractClaimsAsync(content, language);
        var entities = await _entityExtractor.ExtractEntitiesAsync(content, language);

        // 计算各项指标
        var claimDensity = CalculateClaimDensity(claims, wordCount);
        var informationDensity = CalculateInformationDensity(wordCount);
        var frontloading = CalculateFrontloading(content, claims, settings);
        var sentenceLength = CalculateSentenceLength(sentences, settings);
        var entityDensity = CalculateEntityDensity(entities, wordCount);

        // 计算综合评分
        var extractabilityScore = 
            claimDensity.Score * WeightClaimDensity +
            informationDensity.Score * WeightInformationDensity +
            frontloading.Score * WeightFrontloading +
            sentenceLength.Score * WeightSentenceLength +
            entityDensity.Score * WeightEntityDensity;

        // 确定评级
        var (grade, gradeColor) = GetGrade(extractabilityScore);

        // 生成优化建议
        var suggestions = GenerateSuggestions(
            claimDensity, informationDensity, frontloading, sentenceLength, entityDensity, language);

        // aiseo-audit 审计分析 (3.13-3.19)
        var answerCapsule = await _aiseoAuditAnalyzer.DetectAnswerCapsulesAsync(content, language);
        var sectionLengthMetric = await _aiseoAuditAnalyzer.AnalyzeSectionLengthAsync(content, language);
        var answerFirst = await _aiseoAuditAnalyzer.DetectAnswerFirstAsync(content, claims, language);
        var fleschReadability = await _aiseoAuditAnalyzer.CalculateFleschReadabilityAsync(content, language);
        var quotationAttribution = await _aiseoAuditAnalyzer.DetectQuotationAttributionAsync(content, language);

        // Listicle 优化分析 (3.20-3.22)
        var listicleFormat = await _listicleAnalyzer.DetectListicleFormatAsync(content, language);
        var selfPromotion = await _listicleAnalyzer.DetectSelfPromotionAsync(content, null, null, language);
        var thirdPartyReference = await _listicleAnalyzer.AnalyzeThirdPartyReferencesAsync(content, language);

        // Schema 完整度分析 (3.18)
        var schemaCompleteness = await _schemaAnalyzer.AnalyzeContentForSchemaAsync(content, language);

        // 高级内容分析 (3.23-3.27)
        var contentTypeStrategy = _advancedAnalyzer.GetContentTypeStrategy(content, language);
        var structuralElements = _advancedAnalyzer.AnalyzeStructuralElements(content, language);
        var optimalLength = _advancedAnalyzer.AnalyzeOptimalLength(content, claimDensity.Value, language);
        var citabilityScore = await _advancedAnalyzer.AnalyzeCitabilityAsync(content, language);
        var front30Percent = await _advancedAnalyzer.AnalyzeFront30PercentAsync(content, language);

        // 补充优化分析 (3.28-3.30)
        var paragraphLength = _advancedAnalyzer.AnalyzeParagraphLength(content, language);
        var titleStrategy = _advancedAnalyzer.AnalyzeTitleStrategy(content);
        var enhancedEntityDensity = await _advancedAnalyzer.AnalyzeEnhancedEntityDensityAsync(content, language);

        var result = new ContentQualityResult
        {
            ExtractabilityScore = Math.Round(extractabilityScore, 1),
            Grade = grade,
            GradeColor = gradeColor,
            WordCount = wordCount,
            SentenceCount = sentences.Length,
            ClaimDensity = claimDensity,
            InformationDensity = informationDensity,
            Frontloading = frontloading,
            SentenceLength = sentenceLength,
            EntityDensity = entityDensity,
            Suggestions = suggestions,
            // aiseo-audit 审计指标
            AnswerCapsule = answerCapsule,
            SectionLength = sectionLengthMetric,
            AnswerFirst = answerFirst,
            FleschReadability = fleschReadability,
            QuotationAttribution = quotationAttribution,
            // Listicle 优化指标
            ListicleFormat = listicleFormat,
            SelfPromotion = selfPromotion,
            ThirdPartyReference = thirdPartyReference,
            // Schema 完整度指标
            SchemaCompleteness = schemaCompleteness,
            // 高级内容优化指标 (3.23-3.27)
            ContentTypeStrategy = contentTypeStrategy,
            StructuralElements = structuralElements,
            OptimalLength = optimalLength,
            CitabilityScore = citabilityScore,
            Front30Percent = front30Percent,
            // 补充优化指标 (3.28-3.30)
            ParagraphLength = paragraphLength,
            TitleStrategy = titleStrategy,
            EnhancedEntityDensity = enhancedEntityDensity
        };

        _logger.LogDebug("[ContentQualityAnalyzer] 分析完成: Score={Score}, Grade={Grade}, WordCount={WordCount}, Claims={Claims}, Listicle={IsListicle}",
            result.ExtractabilityScore, result.Grade, wordCount, claims.Count, listicleFormat.IsListicle);

        return result;
    }

    /// <summary>
    /// 同步版本（用于兼容现有代码）
    /// </summary>
    public ContentQualityResult Analyze(string content, string language = "zh")
    {
        return AnalyzeAsync(content, language).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 批量分析（异步）
    /// </summary>
    public async Task<BatchContentQualityResult> AnalyzeBatchAsync(List<QAPair> items, string language = "zh")
    {
        var results = new List<ContentQualityResultWithQuestion>();
        foreach (var item in items)
        {
            var result = await AnalyzeAsync(item.Answer, language);
            results.Add(new ContentQualityResultWithQuestion
            {
                Question = item.Question,
                ExtractabilityScore = result.ExtractabilityScore,
                Grade = result.Grade,
                GradeColor = result.GradeColor,
                WordCount = result.WordCount,
                SentenceCount = result.SentenceCount,
                ClaimDensity = result.ClaimDensity,
                InformationDensity = result.InformationDensity,
                Frontloading = result.Frontloading,
                SentenceLength = result.SentenceLength,
                EntityDensity = result.EntityDensity,
                Suggestions = result.Suggestions
            });
        }

        var avgScore = results.Any() ? results.Average(r => r.ExtractabilityScore) : 0;
        var (overallGrade, _) = GetGrade(avgScore);

        // 汇总最常见的建议
        var topSuggestions = results
            .SelectMany(r => r.Suggestions)
            .GroupBy(s => s.Type)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.First())
            .ToList();

        return new BatchContentQualityResult
        {
            Items = results,
            AverageScore = Math.Round(avgScore, 1),
            OverallGrade = overallGrade,
            TopSuggestions = topSuggestions
        };
    }

    /// <summary>
    /// 同步版本批量分析
    /// </summary>
    public BatchContentQualityResult AnalyzeBatch(List<QAPair> items, string language = "zh")
    {
        return AnalyzeBatchAsync(items, language).GetAwaiter().GetResult();
    }

    #region 指标计算

    /// <summary>
    /// 计算事实密度
    /// </summary>
    private ClaimDensityMetric CalculateClaimDensity(List<ExtractedClaim> claims, int wordCount)
    {
        if (wordCount == 0)
            return new ClaimDensityMetric { Value = 0, Score = 0 };

        var density = claims.Count / (wordCount / 100.0);
        var score = NormalizeScore(density, target: 4, max: 8);

        return new ClaimDensityMetric
        {
            Value = Math.Round(density, 1),
            Score = score,
            Claims = claims
        };
    }

    /// <summary>
    /// 计算信息密度（基于内容长度）
    /// </summary>
    private InformationDensityMetric CalculateInformationDensity(int wordCount)
    {
        // 基于 Dejan AI 研究：800-1500 词最佳
        var score = wordCount switch
        {
            < 200 => 3.0,
            < 500 => 5.0,
            < 800 => 7.0,
            < 1500 => 10.0,  // 最佳范围
            < 2000 => 8.0,
            < 3000 => 6.0,
            _ => 4.0
        };

        var status = wordCount switch
        {
            < 200 => "内容过短",
            < 500 => "内容偏短",
            < 800 => "接近最佳",
            < 1500 => "最佳范围",
            < 2000 => "略长",
            < 3000 => "偏长",
            _ => "过长"
        };

        return new InformationDensityMetric
        {
            WordCount = wordCount,
            Score = score,
            Status = status
        };
    }

    /// <summary>
    /// 计算答案前置评分
    /// </summary>
    private FrontloadingMetric CalculateFrontloading(string content, List<ExtractedClaim> allClaims, LanguageSettings settings)
    {
        var first100Words = GetFirstNWords(content, 100, settings);
        var first300Words = GetFirstNWords(content, 300, settings);

        var claimsIn100 = allClaims.Count(c => c.Position <= 100);
        var claimsIn300 = allClaims.Count(c => c.Position <= 300);
        var firstClaimPosition = allClaims.Any() ? allClaims.Min(c => c.Position) : int.MaxValue;

        // 评分：前100词3+个claims为满分
        var score100 = NormalizeScore(claimsIn100, target: 3, max: 5);
        
        // 第一个claim位置：前20词为满分
        var positionScore = firstClaimPosition switch
        {
            <= 20 => 10.0,
            <= 50 => 8.0,
            <= 100 => 6.0,
            <= 200 => 4.0,
            _ => 2.0
        };

        // 综合评分
        var score = (score100 * 0.7 + positionScore * 0.3);

        return new FrontloadingMetric
        {
            ClaimsInFirst100 = claimsIn100,
            ClaimsInFirst300 = claimsIn300,
            FirstClaimPosition = firstClaimPosition == int.MaxValue ? 0 : firstClaimPosition,
            Score = Math.Round(score, 1)
        };
    }

    /// <summary>
    /// 计算句子长度评分
    /// </summary>
    private SentenceLengthMetric CalculateSentenceLength(string[] sentences, LanguageSettings settings)
    {
        if (sentences.Length == 0)
            return new SentenceLengthMetric { AverageLength = 0, Score = 0 };

        var sentenceLengths = sentences.Select(s => CountWords(s, settings)).ToList();
        var avgLength = sentenceLengths.Average();

        // 基于 Dejan AI 研究：15-20 词最佳（匹配 Google 15.5 词提取块）
        var score = avgLength switch
        {
            < 8 => 4.0,
            < 12 => 6.0,
            < 15 => 8.0,
            < 20 => 10.0,  // 最佳
            < 25 => 7.0,
            < 30 => 5.0,
            _ => 3.0
        };

        return new SentenceLengthMetric
        {
            AverageLength = Math.Round(avgLength, 1),
            Score = score,
            LongSentenceCount = sentenceLengths.Count(l => l > 25),
            ShortSentenceCount = sentenceLengths.Count(l => l < 10)
        };
    }

    /// <summary>
    /// 计算实体密度 (3.16 增强版)
    /// 原理：实体清晰度指标，最佳范围 2-8/100词
    /// </summary>
    private EntityDensityMetric CalculateEntityDensity(List<ExtractedEntity> entities, int wordCount)
    {
        if (wordCount == 0)
            return new EntityDensityMetric { Value = 0, Score = 0 };

        var totalEntityCount = entities.Sum(e => e.Count);
        var density = totalEntityCount / (wordCount / 100.0);
        
        // 按类型统计实体
        var typeStats = entities
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Count));
        
        // 计算多样性评分（类型越多越好）
        var typeCount = typeStats.Count;
        var diversityScore = Math.Min(10, typeCount * 2.0);
        
        // 高价值实体：品牌、专家、数据相关
        var highValueTypes = new[] { "brand", "person", "number", "statistic" };
        var highValueCount = entities
            .Where(e => highValueTypes.Contains(e.Type.ToLower()))
            .Sum(e => e.Count);
        
        // 评分：最佳范围 2-8/100词
        double score;
        if (density < 2)
        {
            score = density / 2 * 5; // 0-2 映射到 0-5
        }
        else if (density <= 8)
        {
            score = 5 + (density - 2) / 6 * 5; // 2-8 映射到 5-10
            score = Math.Min(10, score);
        }
        else
        {
            score = Math.Max(5, 10 - (density - 8) * 0.5); // >8 逐渐降分
        }

        return new EntityDensityMetric
        {
            Value = Math.Round(density, 1),
            Score = Math.Round(score, 1),
            TargetMin = 2.0,
            TargetMax = 8.0,
            Target = 2.0,
            Entities = entities,
            EntityTypeStats = typeStats,
            DiversityScore = Math.Round(diversityScore, 1),
            HighValueEntityCount = highValueCount
        };
    }

    #endregion

    #region 优化建议生成

    /// <summary>
    /// 生成优化建议
    /// </summary>
    private List<OptimizationSuggestion> GenerateSuggestions(
        ClaimDensityMetric claimDensity,
        InformationDensityMetric informationDensity,
        FrontloadingMetric frontloading,
        SentenceLengthMetric sentenceLength,
        EntityDensityMetric entityDensity,
        string language)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var isZh = language == "zh";

        // 1. 事实密度建议
        if (claimDensity.Score < 6)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = "claim_density",
                Priority = claimDensity.Score < 4 ? "high" : "medium",
                Message = isZh 
                    ? $"事实密度偏低（{claimDensity.Value}/100词），目标 4+/100词。建议添加更多具体数据和统计信息。"
                    : $"Claim density is low ({claimDensity.Value}/100 words), target 4+. Add more specific data and statistics.",
                Example = isZh 
                    ? "例如：'提升 40% 效率'、'根据 2025 年研究...'、'超过 100 万用户'"
                    : "e.g., 'improves efficiency by 40%', 'according to 2025 research...', 'over 1 million users'"
            });
        }

        // 2. 信息密度建议
        if (informationDensity.Score < 7)
        {
            var msg = informationDensity.WordCount < 500
                ? (isZh ? "内容过短，建议扩展到 800-1500 词以获得最佳 AI 覆盖率。" : "Content too short. Expand to 800-1500 words for optimal AI coverage.")
                : (isZh ? "内容过长，AI 只会引用部分内容。考虑拆分为多篇或精简。" : "Content too long. AI will only cite portions. Consider splitting or condensing.");

            suggestions.Add(new OptimizationSuggestion
            {
                Type = "information_density",
                Priority = informationDensity.Score < 5 ? "high" : "medium",
                Message = msg,
                Example = isZh 
                    ? $"当前 {informationDensity.WordCount} 词，最佳范围 800-1500 词"
                    : $"Current: {informationDensity.WordCount} words, optimal: 800-1500 words"
            });
        }

        // 3. 答案前置建议
        if (frontloading.Score < 6)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = "frontloading",
                Priority = frontloading.Score < 4 ? "high" : "medium",
                Message = isZh 
                    ? $"关键信息出现太晚（前100词仅 {frontloading.ClaimsInFirst100} 个事实）。将核心答案和数据移到第一段。"
                    : $"Key information appears too late ({frontloading.ClaimsInFirst100} claims in first 100 words). Move core answer to first paragraph.",
                Example = isZh 
                    ? "采用倒金字塔结构：第一段直接回答问题并给出关键数据"
                    : "Use inverted pyramid: answer the question directly with key data in the first paragraph"
            });
        }

        // 4. 句子长度建议
        if (sentenceLength.Score < 7)
        {
            var msg = sentenceLength.AverageLength < 12
                ? (isZh ? "句子过短，可能显得碎片化。合并相关短句。" : "Sentences too short, may appear fragmented. Combine related short sentences.")
                : (isZh ? $"句子过长（平均 {sentenceLength.AverageLength} 词），目标 15-20 词。拆分长句以提高可提取性。" 
                        : $"Sentences too long (avg {sentenceLength.AverageLength} words), target 15-20. Split long sentences for better extractability.");

            suggestions.Add(new OptimizationSuggestion
            {
                Type = "sentence_length",
                Priority = sentenceLength.LongSentenceCount > 3 ? "high" : "medium",
                Message = msg,
                Example = sentenceLength.LongSentenceCount > 0 
                    ? (isZh ? $"有 {sentenceLength.LongSentenceCount} 个超长句子（>25词）需要拆分" 
                            : $"{sentenceLength.LongSentenceCount} sentences exceed 25 words and need splitting")
                    : null
            });
        }

        // 5. 实体密度建议
        if (entityDensity.Score < 6)
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Type = "entity_density",
                Priority = "low",
                Message = isZh 
                    ? $"实体密度偏低（{entityDensity.Value}/100词）。添加更多品牌、产品、人名等可引用实体。"
                    : $"Entity density is low ({entityDensity.Value}/100 words). Add more brands, products, names that AI can reference.",
                Example = isZh 
                    ? "例如：明确提及公司名、产品名、专家姓名、具体地点"
                    : "e.g., explicitly mention company names, product names, expert names, specific locations"
            });
        }

        return suggestions.OrderByDescending(s => s.Priority switch
        {
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        }).ToList();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 统计词数
    /// </summary>
    private int CountWords(string content, LanguageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        if (settings.IsCharacterBased)
        {
            // CJK：统计非空白非标点字符数
            return content.Count(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c));
        }
        else
        {
            // 拉丁语系：按空格分词
            return content.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                         .Count(w => w.Any(char.IsLetterOrDigit));
        }
    }

    /// <summary>
    /// 分句
    /// </summary>
    private string[] SplitSentences(string content, LanguageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<string>();

        var delimiters = settings.SentenceDelimiters.Concat(new[] { '\n' }).ToArray();

        return content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => s.Trim())
                     .Where(s => s.Length > 0)
                     .ToArray();
    }

    /// <summary>
    /// 获取前N个词
    /// </summary>
    private string GetFirstNWords(string content, int n, LanguageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "";

        if (settings.IsCharacterBased)
        {
            // CJK：取前N个字符
            var chars = content.Where(c => !char.IsWhiteSpace(c)).Take(n);
            return string.Concat(chars);
        }
        else
        {
            // 拉丁语系：取前N个词
            var words = content.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                              .Take(n);
            return string.Join(" ", words);
        }
    }

    /// <summary>
    /// 归一化评分到 0-10
    /// </summary>
    private double NormalizeScore(double value, double target, double max)
    {
        if (value >= max) return 10.0;
        if (value >= target) return 7.0 + (value - target) / (max - target) * 3.0;
        if (value > 0) return value / target * 7.0;
        return 0;
    }

    /// <summary>
    /// 获取评级
    /// </summary>
    private (string grade, string color) GetGrade(double score)
    {
        return score switch
        {
            >= 8 => ("优秀", "text-green-600"),
            >= 6 => ("良好", "text-blue-600"),
            >= 4 => ("一般", "text-yellow-600"),
            _ => ("差", "text-red-600")
        };
    }

    #endregion
}
