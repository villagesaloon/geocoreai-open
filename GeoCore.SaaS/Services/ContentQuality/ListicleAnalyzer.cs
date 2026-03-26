using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// Listicle 格式分析器
/// 实现 3.20-3.22 功能
/// 来源：The Edward Show E897 + Glenn Gabe 研究
/// </summary>
public class ListicleAnalyzer
{
    private readonly ILogger<ListicleAnalyzer> _logger;
    private readonly LanguageConfigProvider _configProvider;

    // 权威来源类型及其权重
    private static readonly Dictionary<string, int> AuthoritySourceWeights = new()
    {
        { "academic", 10 },      // 学术论文、研究机构
        { "government", 9 },     // 政府机构
        { "industry_report", 8 }, // 行业报告（Gartner, McKinsey等）
        { "news", 6 },           // 主流媒体
        { "expert", 7 },         // 专家引用
        { "organization", 5 }    // 行业组织
    };

    public ListicleAnalyzer(
        ILogger<ListicleAnalyzer> logger,
        LanguageConfigProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
    }

    #region 3.20 Listicle 格式检测

    /// <summary>
    /// 检测 Listicle 格式 (3.20)
    /// 原理：ChatGPT/Perplexity 大量引用 listicles 格式内容
    /// </summary>
    public async Task<ListicleFormatMetric> DetectListicleFormatAsync(string content, string language = "zh")
    {
        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var items = new List<ListicleItem>();
        var totalWords = CountWords(content, settings);

        // 检测编号列表（1. 2. 3. 或 一、二、三）
        var numberedItems = ExtractNumberedItems(content, language);
        
        // 检测项目符号列表（- * •）
        var bulletedItems = ExtractBulletedItems(content, language);

        // 合并并确定列表类型
        string listType;
        if (numberedItems.Count > 0 && bulletedItems.Count > 0)
        {
            listType = "mixed";
            items.AddRange(numberedItems);
            items.AddRange(bulletedItems);
        }
        else if (numberedItems.Count > 0)
        {
            listType = "numbered";
            items = numberedItems;
        }
        else if (bulletedItems.Count > 0)
        {
            listType = "bulleted";
            items = bulletedItems;
        }
        else
        {
            listType = "none";
        }

        // 分析每个列表项
        foreach (var item in items)
        {
            item.WordCount = CountWords(item.Content, settings);
            item.HasData = HasDataPattern(item.Content, language);
            item.HasActionable = HasActionablePattern(item.Content, language);
        }

        // 计算覆盖率
        var listWords = items.Sum(i => i.WordCount);
        var coverageRate = totalWords > 0 ? (double)listWords / totalWords : 0;

        // 检测标题和总结
        var hasClearTitle = HasClearTitle(content, language);
        var hasSummary = HasSummaryParagraph(content, language);

        // 评分
        var isListicle = items.Count >= 3;
        var score = CalculateListicleScore(items, coverageRate, hasClearTitle, hasSummary);

        _logger.LogDebug("[ListicleAnalyzer] 格式检测: {Type}, {Count} 项, 覆盖率 {Rate:P1}",
            listType, items.Count, coverageRate);

        return new ListicleFormatMetric
        {
            IsListicle = isListicle,
            Score = Math.Round(score, 1),
            ListType = listType,
            ItemCount = items.Count,
            Items = items.Take(10).ToList(), // 最多返回10项
            CoverageRate = Math.Round(coverageRate, 3),
            HasClearTitle = hasClearTitle,
            HasSummary = hasSummary
        };
    }

    private List<ListicleItem> ExtractNumberedItems(string content, string language)
    {
        var items = new List<ListicleItem>();
        
        // 匹配编号列表：1. 2. 3. 或 一、二、三、
        var patterns = language == "zh"
            ? new[] { @"(?:^|\n)\s*(\d+)[\.、]\s*(.+?)(?=\n\s*\d+[\.、]|\n\n|$)", @"(?:^|\n)\s*([一二三四五六七八九十]+)[、\.]\s*(.+?)(?=\n\s*[一二三四五六七八九十]+[、\.]|\n\n|$)" }
            : new[] { @"(?:^|\n)\s*(\d+)\.\s*(.+?)(?=\n\s*\d+\.|\n\n|$)" };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                var numberStr = match.Groups[1].Value;
                int? number = int.TryParse(numberStr, out var n) ? n : null;
                
                var text = match.Groups[2].Value.Trim();
                var (title, contentPart) = SplitTitleAndContent(text);

                items.Add(new ListicleItem
                {
                    Number = number,
                    Title = title,
                    Content = contentPart
                });
            }
        }

        return items;
    }

    private List<ListicleItem> ExtractBulletedItems(string content, string language)
    {
        var items = new List<ListicleItem>();
        
        // 匹配项目符号列表：- * • ·
        var pattern = @"(?:^|\n)\s*[-*•·]\s*(.+?)(?=\n\s*[-*•·]|\n\n|$)";
        var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var text = match.Groups[1].Value.Trim();
            if (text.Length < 5) continue; // 忽略太短的项

            var (title, contentPart) = SplitTitleAndContent(text);

            items.Add(new ListicleItem
            {
                Number = null,
                Title = title,
                Content = contentPart
            });
        }

        return items;
    }

    private (string title, string content) SplitTitleAndContent(string text)
    {
        // 尝试分割标题和内容（通过冒号或换行）
        var colonMatch = Regex.Match(text, @"^([^：:]+)[：:]\s*(.*)$", RegexOptions.Singleline);
        if (colonMatch.Success)
        {
            return (colonMatch.Groups[1].Value.Trim(), colonMatch.Groups[2].Value.Trim());
        }

        var newlineMatch = Regex.Match(text, @"^([^\n]+)\n(.*)$", RegexOptions.Singleline);
        if (newlineMatch.Success)
        {
            return (newlineMatch.Groups[1].Value.Trim(), newlineMatch.Groups[2].Value.Trim());
        }

        // 无法分割，整体作为内容
        return (text.Length > 50 ? text[..50] + "..." : text, text);
    }

    private bool HasDataPattern(string text, string language)
    {
        var patterns = language == "zh"
            ? new[] { @"\d+%", @"\d+\s*(万|亿|千|百|倍)", @"增长|提升|下降|减少", @"约\s*\d+", @"超过\s*\d+" }
            : new[] { @"\d+%", @"\d+x", @"\d+\s*(million|billion|thousand)", @"increase|decrease|growth" };

        return patterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase));
    }

    private bool HasActionablePattern(string text, string language)
    {
        var patterns = language == "zh"
            ? new[] { @"^(首先|然后|接下来|最后|步骤)", @"(可以|应该|需要|建议|尝试)", @"(如何|怎样|方法)" }
            : new[] { @"^(first|then|next|finally|step)", @"(can|should|need|try|consider)", @"(how to|ways to)" };

        return patterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase));
    }

    private bool HasClearTitle(string content, string language)
    {
        // 检测是否有清晰的标题（H1/H2 或数字开头的标题）
        var patterns = new[] { @"^#\s+.+", @"^##\s+.+", @"^\d+\s*(个|种|条|大).+" };
        return patterns.Any(p => Regex.IsMatch(content, p, RegexOptions.Multiline));
    }

    private bool HasSummaryParagraph(string content, string language)
    {
        // 检测是否有总结段落
        var patterns = language == "zh"
            ? new[] { @"(总结|总之|综上|结论|小结)", @"(以上|这些).*(方法|技巧|建议|要点)" }
            : new[] { @"(summary|conclusion|in summary|to sum up)", @"(these|above).*(tips|methods|ways)" };

        var lastPart = content.Length > 500 ? content[^500..] : content;
        return patterns.Any(p => Regex.IsMatch(lastPart, p, RegexOptions.IgnoreCase));
    }

    private double CalculateListicleScore(List<ListicleItem> items, double coverageRate, bool hasTitle, bool hasSummary)
    {
        var score = 0.0;

        // 列表项数量评分
        score += items.Count switch
        {
            >= 7 => 3.0,
            >= 5 => 2.5,
            >= 3 => 2.0,
            >= 1 => 1.0,
            _ => 0
        };

        // 覆盖率评分
        score += coverageRate switch
        {
            >= 0.5 => 2.5,
            >= 0.3 => 2.0,
            >= 0.2 => 1.5,
            >= 0.1 => 1.0,
            _ => 0
        };

        // 数据丰富度
        var dataRatio = items.Count > 0 ? (double)items.Count(i => i.HasData) / items.Count : 0;
        score += dataRatio * 2;

        // 可操作性
        var actionableRatio = items.Count > 0 ? (double)items.Count(i => i.HasActionable) / items.Count : 0;
        score += actionableRatio * 1.5;

        // 结构完整性
        if (hasTitle) score += 0.5;
        if (hasSummary) score += 0.5;

        return Math.Min(10, score);
    }

    #endregion

    #region 3.21 自我推广检测

    /// <summary>
    /// 检测自我推广 (3.21)
    /// 原理：自我推广型 listicle 会被 AI 惩罚
    /// </summary>
    public Task<SelfPromotionMetric> DetectSelfPromotionAsync(
        string content, 
        string? brandName = null,
        string? productName = null,
        string language = "zh")
    {
        var signals = new List<PromotionSignal>();
        var position = 0;

        // 1. 品牌提及检测
        var brandMentions = 0;
        if (!string.IsNullOrEmpty(brandName))
        {
            var brandMatches = Regex.Matches(content, Regex.Escape(brandName), RegexOptions.IgnoreCase);
            brandMentions = brandMatches.Count;
            
            if (brandMentions > 3)
            {
                signals.Add(new PromotionSignal
                {
                    Type = "brand_mention",
                    Text = $"品牌 '{brandName}' 被提及 {brandMentions} 次",
                    Position = 0,
                    Severity = brandMentions > 5 ? 4 : 3
                });
            }
        }

        // 2. 产品推广检测
        var productMentions = 0;
        if (!string.IsNullOrEmpty(productName))
        {
            var productMatches = Regex.Matches(content, Regex.Escape(productName), RegexOptions.IgnoreCase);
            productMentions = productMatches.Count;
            
            if (productMentions > 2)
            {
                signals.Add(new PromotionSignal
                {
                    Type = "product_push",
                    Text = $"产品 '{productName}' 被提及 {productMentions} 次",
                    Position = 0,
                    Severity = productMentions > 4 ? 4 : 3
                });
            }
        }

        // 3. CTA（行动号召）检测
        var ctaPatterns = language == "zh"
            ? new[] { @"(立即|马上|现在)(购买|注册|下载|体验|试用)", @"(点击|扫码|关注|订阅)", @"(免费|限时|优惠|折扣)", @"(联系我们|咨询|预约)" }
            : new[] { @"(buy now|sign up|download|try)", @"(click|subscribe|follow)", @"(free|limited|discount|offer)", @"(contact us|book|schedule)" };

        var ctaCount = 0;
        foreach (var pattern in ctaPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                ctaCount++;
                if (ctaCount <= 3) // 只记录前3个
                {
                    signals.Add(new PromotionSignal
                    {
                        Type = "cta",
                        Text = match.Value,
                        Position = match.Index,
                        Severity = 3
                    });
                }
            }
        }

        // 4. 夸大词检测
        var superlativePatterns = language == "zh"
            ? new[] { @"(最好|最佳|第一|唯一|独家|领先|顶级|首选)", @"(无与伦比|无可比拟|绝对|完美)" }
            : new[] { @"(best|#1|only|exclusive|leading|top|premier)", @"(unmatched|unparalleled|absolute|perfect)" };

        foreach (var pattern in superlativePatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                signals.Add(new PromotionSignal
                {
                    Type = "superlative",
                    Text = match.Value,
                    Position = match.Index,
                    Severity = 2
                });
            }
        }

        // 5. 独家声明检测
        var exclusivePatterns = language == "zh"
            ? new[] { @"(只有我们|我们独有|我们的优势)", @"(比竞争对手|比其他)" }
            : new[] { @"(only we|our unique|our advantage)", @"(than competitors|than others)" };

        foreach (var pattern in exclusivePatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                signals.Add(new PromotionSignal
                {
                    Type = "exclusive_claim",
                    Text = match.Value,
                    Position = match.Index,
                    Severity = 4
                });
            }
        }

        // 计算推广强度
        var totalSeverity = signals.Sum(s => s.Severity);
        var promotionIntensity = Math.Min(100, totalSeverity * 5);

        // 确定风险等级
        var riskLevel = promotionIntensity switch
        {
            >= 60 => "high",
            >= 30 => "medium",
            _ => "low"
        };

        // 评分（越高越好，无推广=10分）
        var score = Math.Max(0, 10 - promotionIntensity / 10);

        _logger.LogDebug("[ListicleAnalyzer] 自我推广检测: {Signals} 信号, 强度 {Intensity}, 风险 {Risk}",
            signals.Count, promotionIntensity, riskLevel);

        return Task.FromResult(new SelfPromotionMetric
        {
            HasSelfPromotion = signals.Count > 0,
            Score = Math.Round((double)score, 1),
            PromotionIntensity = promotionIntensity,
            Signals = signals.Take(10).ToList(),
            BrandMentionCount = brandMentions,
            ProductMentionCount = productMentions,
            CtaCount = ctaCount,
            RiskLevel = riskLevel
        });
    }

    #endregion

    #region 3.22 第三方引用建议

    /// <summary>
    /// 分析第三方引用 (3.22)
    /// 原理：让其他可信来源引用你，提升权威性
    /// </summary>
    public Task<ThirdPartyReferenceMetric> AnalyzeThirdPartyReferencesAsync(string content, string language = "zh")
    {
        var references = new List<ThirdPartyReference>();

        // 1. 学术来源检测
        var academicPatterns = language == "zh"
            ? new[] { @"(研究|论文|学者|教授|博士|大学|学院|研究院)([^，。]+)(表示|指出|发现|证明)", @"(根据|据).*(研究|调查|报告)" }
            : new[] { @"(study|research|paper|professor|dr\.|university)([^,.]+)(found|shows|indicates)", @"(according to).*(study|research|report)" };

        foreach (var pattern in academicPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                references.Add(new ThirdPartyReference
                {
                    SourceName = ExtractSourceName(match.Value, language),
                    SourceType = "academic",
                    Text = match.Value.Length > 100 ? match.Value[..100] + "..." : match.Value,
                    AuthorityScore = AuthoritySourceWeights["academic"],
                    HasSpecificData = HasDataPattern(match.Value, language)
                });
            }
        }

        // 2. 行业报告检测
        var reportPatterns = language == "zh"
            ? new[] { @"(Gartner|麦肯锡|McKinsey|IDC|Forrester|艾瑞|易观|36氪)", @"(行业报告|市场报告|白皮书|蓝皮书)" }
            : new[] { @"(Gartner|McKinsey|IDC|Forrester|Deloitte|PwC)", @"(industry report|market report|whitepaper)" };

        foreach (var pattern in reportPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (!references.Any(r => r.Text.Contains(match.Value)))
                {
                    references.Add(new ThirdPartyReference
                    {
                        SourceName = match.Value,
                        SourceType = "industry_report",
                        Text = GetSurroundingText(content, match.Index, 100),
                        AuthorityScore = AuthoritySourceWeights["industry_report"],
                        HasSpecificData = true
                    });
                }
            }
        }

        // 3. 政府/官方来源检测
        var govPatterns = language == "zh"
            ? new[] { @"(国务院|工信部|发改委|统计局|央行|证监会)", @"(政府|官方|部门).*(数据|报告|公告)" }
            : new[] { @"(government|federal|ministry|department|bureau)", @"(official).*(data|report|announcement)" };

        foreach (var pattern in govPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                references.Add(new ThirdPartyReference
                {
                    SourceName = match.Value,
                    SourceType = "government",
                    Text = GetSurroundingText(content, match.Index, 100),
                    AuthorityScore = AuthoritySourceWeights["government"],
                    HasSpecificData = HasDataPattern(match.Value, language)
                });
            }
        }

        // 4. 专家引用检测
        var expertPatterns = language == "zh"
            ? new[] { @"([^，。]{2,10})(专家|分析师|顾问|创始人|CEO|CTO)(表示|指出|认为)" }
            : new[] { @"([A-Z][a-z]+\s+[A-Z][a-z]+),?\s+(expert|analyst|consultant|founder|CEO|CTO)" };

        foreach (var pattern in expertPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                references.Add(new ThirdPartyReference
                {
                    SourceName = match.Groups[1].Value.Trim(),
                    SourceType = "expert",
                    Text = match.Value,
                    AuthorityScore = AuthoritySourceWeights["expert"],
                    HasSpecificData = false
                });
            }
        }

        // 去重
        references = references
            .GroupBy(r => r.SourceName)
            .Select(g => g.First())
            .ToList();

        // 计算多样性评分
        var sourceTypes = references.Select(r => r.SourceType).Distinct().Count();
        var diversityScore = Math.Min(10, sourceTypes * 2.5);

        // 权威来源数量
        var authoritativeCount = references.Count(r => r.AuthorityScore >= 7);

        // 生成建议
        var suggestions = GenerateReferenceSuggestions(references, language);

        // 总评分
        var score = CalculateReferenceScore(references, diversityScore);

        _logger.LogDebug("[ListicleAnalyzer] 第三方引用: {Count} 个来源, 多样性 {Diversity}, 权威 {Auth}",
            references.Count, diversityScore, authoritativeCount);

        return Task.FromResult(new ThirdPartyReferenceMetric
        {
            HasThirdPartyReferences = references.Count > 0,
            Score = Math.Round(score, 1),
            References = references.Take(10).ToList(),
            DiversityScore = Math.Round(diversityScore, 1),
            AuthoritativeSourceCount = authoritativeCount,
            SuggestedReferenceTypes = suggestions
        });
    }

    private string ExtractSourceName(string text, string language)
    {
        // 尝试提取来源名称
        var patterns = language == "zh"
            ? new[] { @"([\u4e00-\u9fa5]+大学)", @"([\u4e00-\u9fa5]+研究院)", @"([\u4e00-\u9fa5]+教授)" }
            : new[] { @"([A-Z][a-z]+\s+University)", @"(Dr\.\s+[A-Z][a-z]+)", @"(Professor\s+[A-Z][a-z]+)" };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return text.Length > 30 ? text[..30] + "..." : text;
    }

    private string GetSurroundingText(string content, int position, int length)
    {
        var start = Math.Max(0, position - length / 2);
        var end = Math.Min(content.Length, position + length / 2);
        return content[start..end];
    }

    private List<string> GenerateReferenceSuggestions(List<ThirdPartyReference> references, string language)
    {
        var suggestions = new List<string>();
        var existingTypes = references.Select(r => r.SourceType).ToHashSet();

        if (!existingTypes.Contains("academic"))
        {
            suggestions.Add(language == "zh" ? "建议添加学术研究或论文引用" : "Consider adding academic research citations");
        }

        if (!existingTypes.Contains("industry_report"))
        {
            suggestions.Add(language == "zh" ? "建议引用行业报告（如 Gartner、McKinsey）" : "Consider citing industry reports (e.g., Gartner, McKinsey)");
        }

        if (!existingTypes.Contains("expert"))
        {
            suggestions.Add(language == "zh" ? "建议添加专家观点引用" : "Consider adding expert opinions");
        }

        if (references.Count(r => r.HasSpecificData) < 2)
        {
            suggestions.Add(language == "zh" ? "建议增加带具体数据的引用" : "Consider adding citations with specific data");
        }

        return suggestions;
    }

    private double CalculateReferenceScore(List<ThirdPartyReference> references, double diversityScore)
    {
        if (references.Count == 0) return 3.0; // 无引用给基础分

        var score = 0.0;

        // 引用数量
        score += Math.Min(3, references.Count * 0.5);

        // 权威性
        var avgAuthority = references.Average(r => r.AuthorityScore);
        score += avgAuthority / 10 * 3;

        // 多样性
        score += diversityScore / 10 * 2;

        // 数据丰富度
        var dataRatio = (double)references.Count(r => r.HasSpecificData) / references.Count;
        score += dataRatio * 2;

        return Math.Min(10, score);
    }

    #endregion

    #region 辅助方法

    private int CountWords(string content, LanguageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        if (settings.IsCharacterBased)
        {
            return content.Count(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c));
        }
        else
        {
            return content.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                         .Count(w => w.Any(char.IsLetterOrDigit));
        }
    }

    #endregion
}
