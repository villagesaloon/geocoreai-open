using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// aiseo-audit 审计分析器
/// 基于 github.com/agencyenterprise/aiseo-audit 的 7 类审计维度
/// 实现 3.13-3.19 功能
/// </summary>
public class AiseoAuditAnalyzer
{
    private readonly ILogger<AiseoAuditAnalyzer> _logger;
    private readonly LanguageConfigProvider _configProvider;

    // 章节长度最佳范围（词数）
    private const int SectionOptimalMin = 120;
    private const int SectionOptimalMax = 180;
    private const int SectionTooShort = 80;
    private const int SectionTooLong = 250;

    // 答案胶囊长度范围（词数）
    private const int CapsuleMinWords = 40;
    private const int CapsuleMaxWords = 100;

    // 答案优先检测范围（词数）
    private const int AnswerFirstWords = 60;

    public AiseoAuditAnalyzer(
        ILogger<AiseoAuditAnalyzer> logger,
        LanguageConfigProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
    }

    #region 3.13 答案胶囊检测

    /// <summary>
    /// 检测答案胶囊 (3.13)
    /// 原理：72.4% 被 AI 引用的内容具有"答案胶囊"特征
    /// </summary>
    public async Task<AnswerCapsuleMetric> DetectAnswerCapsulesAsync(string content, string language = "zh")
    {
        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var paragraphs = SplitParagraphs(content);
        var capsules = new List<AnswerCapsule>();
        var totalWords = CountWords(content, settings);

        for (int i = 0; i < paragraphs.Length; i++)
        {
            var para = paragraphs[i].Trim();
            if (string.IsNullOrWhiteSpace(para)) continue;

            var wordCount = CountWords(para, settings);
            
            // 检查是否符合胶囊长度
            if (wordCount < CapsuleMinWords || wordCount > CapsuleMaxWords)
                continue;

            // 检查胶囊特征
            var hasStatistics = HasStatisticsPattern(para, language);
            var hasDefinition = HasDefinitionPattern(para, language);
            var isSelfContained = IsSelfContainedParagraph(para, language);

            // 至少满足 2 个特征才算胶囊
            var featureCount = (hasStatistics ? 1 : 0) + (hasDefinition ? 1 : 0) + (isSelfContained ? 1 : 0);
            if (featureCount < 2) continue;

            var qualityScore = CalculateCapsuleQuality(wordCount, hasStatistics, hasDefinition, isSelfContained);

            capsules.Add(new AnswerCapsule
            {
                Text = para.Length > 200 ? para[..200] + "..." : para,
                WordCount = wordCount,
                ParagraphIndex = i,
                QualityScore = qualityScore,
                HasStatistics = hasStatistics,
                HasDefinition = hasDefinition,
                IsSelfContained = isSelfContained
            });
        }

        var capsuleWords = capsules.Sum(c => c.WordCount);
        var coverageRate = totalWords > 0 ? (double)capsuleWords / totalWords : 0;

        // 评分：有胶囊且覆盖率 >20% 为满分
        var score = capsules.Count switch
        {
            0 => 0,
            1 => 5 + coverageRate * 10,
            2 => 7 + coverageRate * 6,
            _ => Math.Min(10, 8 + coverageRate * 4)
        };

        _logger.LogDebug("[AiseoAudit] 答案胶囊检测: {Count} 个胶囊, 覆盖率 {Rate:P1}", capsules.Count, coverageRate);

        return new AnswerCapsuleMetric
        {
            HasCapsule = capsules.Count > 0,
            Capsules = capsules,
            Score = Math.Round(score, 1),
            CoverageRate = Math.Round(coverageRate, 3)
        };
    }

    private bool HasStatisticsPattern(string text, string language)
    {
        // 检测数字、百分比、统计数据
        var patterns = language == "zh"
            ? new[] { @"\d+%", @"\d+\s*(万|亿|千|百)", @"增长\s*\d+", @"提升\s*\d+", @"超过\s*\d+", @"约\s*\d+" }
            : new[] { @"\d+%", @"\d+\s*(million|billion|thousand)", @"increased?\s+by\s+\d+", @"over\s+\d+", @"approximately\s+\d+" };

        return patterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase));
    }

    private bool HasDefinitionPattern(string text, string language)
    {
        // 检测定义、结论性语句
        var patterns = language == "zh"
            ? new[] { @"^[^，。]+是[^，。]+[。]", @"指的是", @"定义为", @"简单来说", @"总之", @"因此", @"所以", @"结论是" }
            : new[] { @"^[^,.]+\s+is\s+", @"refers to", @"defined as", @"in short", @"therefore", @"thus", @"in conclusion" };

        return patterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase));
    }

    private bool IsSelfContainedParagraph(string text, string language)
    {
        // 检测是否可独立成句（不以连接词开头，不以省略结尾）
        var badStarts = language == "zh"
            ? new[] { "但是", "然而", "不过", "此外", "另外", "同时", "而且", "并且", "因为", "由于", "如果", "虽然" }
            : new[] { "but", "however", "moreover", "furthermore", "additionally", "also", "because", "since", "if", "although", "while" };

        var badEnds = language == "zh"
            ? new[] { "……", "...", "等等", "以及" }
            : new[] { "...", "etc.", "and so on" };

        var startsWithBad = badStarts.Any(s => text.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        var endsWithBad = badEnds.Any(e => text.TrimEnd().EndsWith(e, StringComparison.OrdinalIgnoreCase));

        return !startsWithBad && !endsWithBad;
    }

    private double CalculateCapsuleQuality(int wordCount, bool hasStats, bool hasDef, bool isSelfContained)
    {
        var score = 5.0;
        
        // 长度评分：60-80 词最佳
        if (wordCount >= 60 && wordCount <= 80) score += 2;
        else if (wordCount >= 50 && wordCount <= 90) score += 1;

        // 特征评分
        if (hasStats) score += 1.5;
        if (hasDef) score += 1;
        if (isSelfContained) score += 0.5;

        return Math.Min(10, score);
    }

    #endregion

    #region 3.14 章节长度分析

    /// <summary>
    /// 分析章节长度 (3.14)
    /// 原理：120-180 词的章节段落被引用率 +70%
    /// </summary>
    public async Task<SectionLengthMetric> AnalyzeSectionLengthAsync(string content, string language = "zh")
    {
        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var sections = ExtractSections(content, settings);

        var optimalCount = 0;
        var tooShortCount = 0;
        var tooLongCount = 0;

        foreach (var section in sections)
        {
            if (section.WordCount >= SectionOptimalMin && section.WordCount <= SectionOptimalMax)
            {
                section.LengthStatus = "optimal";
                optimalCount++;
            }
            else if (section.WordCount < SectionTooShort)
            {
                section.LengthStatus = "too_short";
                tooShortCount++;
            }
            else if (section.WordCount > SectionTooLong)
            {
                section.LengthStatus = "too_long";
                tooLongCount++;
            }
            else
            {
                section.LengthStatus = "acceptable";
            }
        }

        // 评分：最佳章节占比越高越好
        var optimalRatio = sections.Count > 0 ? (double)optimalCount / sections.Count : 0;
        var score = optimalRatio * 10;

        // 惩罚过短/过长章节
        if (tooShortCount > 0) score -= tooShortCount * 0.5;
        if (tooLongCount > 0) score -= tooLongCount * 1.0;
        score = Math.Max(0, Math.Min(10, score));

        _logger.LogDebug("[AiseoAudit] 章节长度分析: {Total} 章节, {Optimal} 最佳, {Short} 过短, {Long} 过长",
            sections.Count, optimalCount, tooShortCount, tooLongCount);

        return new SectionLengthMetric
        {
            Sections = sections,
            Score = Math.Round(score, 1),
            OptimalSectionCount = optimalCount,
            TooShortCount = tooShortCount,
            TooLongCount = tooLongCount
        };
    }

    private List<ContentSection> ExtractSections(string content, LanguageSettings settings)
    {
        var sections = new List<ContentSection>();
        
        // 按标题分割（支持 Markdown 和 HTML 标题）
        var headingPattern = @"(?:^|\n)(#{1,3}\s+.+|<h[1-3][^>]*>.+?</h[1-3]>)";
        var matches = Regex.Matches(content, headingPattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        if (matches.Count == 0)
        {
            // 无标题，按段落分割
            var paragraphs = SplitParagraphs(content);
            for (int i = 0; i < paragraphs.Length; i++)
            {
                var para = paragraphs[i].Trim();
                if (string.IsNullOrWhiteSpace(para)) continue;

                sections.Add(new ContentSection
                {
                    Title = $"段落 {i + 1}",
                    HeadingLevel = 0,
                    Content = para.Length > 100 ? para[..100] + "..." : para,
                    WordCount = CountWords(para, settings)
                });
            }
        }
        else
        {
            // 有标题，按标题分割
            var positions = matches.Select(m => m.Index).ToList();
            positions.Add(content.Length);

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var title = ExtractHeadingText(match.Value);
                var level = GetHeadingLevel(match.Value);
                
                var start = match.Index + match.Length;
                var end = positions[i + 1];
                var sectionContent = content[start..end].Trim();

                sections.Add(new ContentSection
                {
                    Title = title,
                    HeadingLevel = level,
                    Content = sectionContent.Length > 100 ? sectionContent[..100] + "..." : sectionContent,
                    WordCount = CountWords(sectionContent, settings)
                });
            }
        }

        return sections;
    }

    private string ExtractHeadingText(string heading)
    {
        // 移除 Markdown # 或 HTML 标签
        var text = Regex.Replace(heading, @"^#+\s*", "");
        text = Regex.Replace(text, @"</?h[1-3][^>]*>", "", RegexOptions.IgnoreCase);
        return text.Trim();
    }

    private int GetHeadingLevel(string heading)
    {
        if (heading.StartsWith("###") || Regex.IsMatch(heading, @"<h3", RegexOptions.IgnoreCase)) return 3;
        if (heading.StartsWith("##") || Regex.IsMatch(heading, @"<h2", RegexOptions.IgnoreCase)) return 2;
        if (heading.StartsWith("#") || Regex.IsMatch(heading, @"<h1", RegexOptions.IgnoreCase)) return 1;
        return 0;
    }

    #endregion

    #region 3.15 答案优先格式检测

    /// <summary>
    /// 检测答案优先格式 (3.15)
    /// 原理：前 40-60 词包含核心答案，引用率 +140%
    /// </summary>
    public async Task<AnswerFirstMetric> DetectAnswerFirstAsync(
        string content, 
        List<ExtractedClaim> claims,
        string language = "zh")
    {
        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var first60Words = GetFirstNWords(content, AnswerFirstWords, settings);
        
        // 统计前 60 词中的 claims
        var claimsIn60 = claims.Count(c => c.Position <= AnswerFirstWords);
        var firstClaimPos = claims.Any() ? claims.Min(c => c.Position) : int.MaxValue;

        // 检测是否以直接回答开头
        var startsWithAnswer = DetectDirectAnswerStart(content, language);
        
        // 检测是否包含关键数字
        var hasNumbers = HasStatisticsPattern(first60Words, language);
        
        // 检测是否包含结论
        var hasConclusion = HasDefinitionPattern(first60Words, language);

        // 评分
        var score = 0.0;
        
        // 前 60 词有 2+ claims：+4 分
        if (claimsIn60 >= 2) score += 4;
        else if (claimsIn60 == 1) score += 2;

        // 第一个 claim 在前 20 词：+3 分
        if (firstClaimPos <= 20) score += 3;
        else if (firstClaimPos <= 40) score += 2;
        else if (firstClaimPos <= 60) score += 1;

        // 以直接回答开头：+1.5 分
        if (startsWithAnswer) score += 1.5;

        // 包含数字：+1 分
        if (hasNumbers) score += 1;

        // 包含结论：+0.5 分
        if (hasConclusion) score += 0.5;

        score = Math.Min(10, score);

        var hasAnswerFirst = claimsIn60 >= 1 && firstClaimPos <= 40;

        _logger.LogDebug("[AiseoAudit] 答案优先检测: {Claims} claims in first 60, first at {Pos}, direct={Direct}",
            claimsIn60, firstClaimPos, startsWithAnswer);

        return new AnswerFirstMetric
        {
            HasAnswerFirst = hasAnswerFirst,
            Score = Math.Round(score, 1),
            First60Words = first60Words,
            ClaimsInFirst60 = claimsIn60,
            StartsWithDirectAnswer = startsWithAnswer,
            HasKeyNumbers = hasNumbers,
            HasConclusion = hasConclusion,
            FirstClaimPosition = firstClaimPos == int.MaxValue ? 0 : firstClaimPos
        };
    }

    private bool DetectDirectAnswerStart(string content, string language)
    {
        // 检测是否以直接回答开头（是/否/定义）
        var patterns = language == "zh"
            ? new[] { @"^是的[，,]", @"^不[，,]", @"^[^，。]{2,10}是[^，。]+[。，]", @"^简单来说", @"^总的来说", @"^答案是" }
            : new[] { @"^yes[,.]", @"^no[,.]", @"^[^,.]+\s+is\s+", @"^in short", @"^the answer is", @"^simply put" };

        return patterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase));
    }

    #endregion

    #region 3.17 Flesch 可读性

    /// <summary>
    /// 计算 Flesch 可读性 (3.17)
    /// 原理：60-70 分最佳，便于 AI 压缩摘要
    /// 注意：此公式主要适用于英文，中文使用简化版本
    /// </summary>
    public async Task<FleschReadabilityMetric> CalculateFleschReadabilityAsync(string content, string language = "zh")
    {
        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var sentences = SplitSentences(content, settings);
        var wordCount = CountWords(content, settings);

        if (sentences.Length == 0 || wordCount == 0)
        {
            return new FleschReadabilityMetric
            {
                FleschScore = 0,
                Score = 0,
                Level = "无法计算"
            };
        }

        var avgSentenceLength = (double)wordCount / sentences.Length;
        double fleschScore;
        double avgSyllables;

        if (language == "zh")
        {
            // 中文简化版：基于句子长度和标点密度
            var punctCount = content.Count(c => "，。！？；：".Contains(c));
            var punctDensity = (double)punctCount / wordCount * 100;
            
            // 中文可读性公式（简化）：短句+多标点=高可读性
            fleschScore = 100 - (avgSentenceLength * 2) + (punctDensity * 5);
            fleschScore = Math.Max(0, Math.Min(100, fleschScore));
            avgSyllables = 1.5; // 中文平均音节数约 1.5
        }
        else
        {
            // 英文 Flesch Reading Ease 公式
            var syllables = CountSyllables(content);
            avgSyllables = (double)syllables / wordCount;
            fleschScore = 206.835 - (1.015 * avgSentenceLength) - (84.6 * avgSyllables);
            fleschScore = Math.Max(0, Math.Min(100, fleschScore));
        }

        // 评分：60-70 为满分
        var score = fleschScore switch
        {
            >= 60 and <= 70 => 10.0,
            >= 55 and < 60 or > 70 and <= 75 => 8.0,
            >= 50 and < 55 or > 75 and <= 80 => 6.0,
            >= 40 and < 50 or > 80 and <= 90 => 4.0,
            _ => 2.0
        };

        var level = fleschScore switch
        {
            >= 90 => "非常简单",
            >= 80 => "简单",
            >= 70 => "较简单",
            >= 60 => "标准",
            >= 50 => "较难",
            >= 30 => "难",
            _ => "非常难"
        };

        return new FleschReadabilityMetric
        {
            FleschScore = Math.Round(fleschScore, 1),
            Score = score,
            Level = level,
            AvgSentenceLength = Math.Round(avgSentenceLength, 1),
            AvgSyllablesPerWord = Math.Round(avgSyllables, 2)
        };
    }

    private int CountSyllables(string text)
    {
        // 英文音节计数（简化版）
        var words = text.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var total = 0;

        foreach (var word in words)
        {
            var cleaned = Regex.Replace(word.ToLower(), @"[^a-z]", "");
            if (string.IsNullOrEmpty(cleaned)) continue;

            // 简化规则：元音数量 - 连续元音 - 结尾 e
            var vowels = Regex.Matches(cleaned, @"[aeiouy]").Count;
            var doubleVowels = Regex.Matches(cleaned, @"[aeiouy]{2}").Count;
            var endE = cleaned.EndsWith("e") && cleaned.Length > 2 ? 1 : 0;

            var syllables = Math.Max(1, vowels - doubleVowels - endE);
            total += syllables;
        }

        return total;
    }

    #endregion

    #region 3.19 引语归因检测

    /// <summary>
    /// 检测引语归因 (3.19)
    /// 原理：有引语归因的内容可见度 +30-40%
    /// </summary>
    public Task<QuotationAttributionMetric> DetectQuotationAttributionAsync(string content, string language = "zh")
    {
        var quotes = new List<AttributedQuote>();

        // 检测引号内容
        var quotePatterns = language == "zh"
            ? new[] { "\u201C([^\u201D]+)\u201D", "\u300C([^\u300D]+)\u300D", "\u300E([^\u300F]+)\u300F" }
            : new[] { "\"([^\"]+)\"", "'([^']+)'" };

        foreach (var pattern in quotePatterns)
        {
            var matches = Regex.Matches(content, pattern);
            foreach (Match match in matches)
            {
                var quoteText = match.Groups[1].Value;
                if (quoteText.Length < 10) continue; // 忽略太短的引用

                // 查找归因（引号后的来源）
                var afterQuote = content[(match.Index + match.Length)..];
                var attribution = ExtractAttribution(afterQuote, language);

                quotes.Add(new AttributedQuote
                {
                    Text = quoteText.Length > 100 ? quoteText[..100] + "..." : quoteText,
                    Attribution = attribution
                });
            }
        }

        // 检测"据...称"、"...表示"等归因模式
        var attributionPatterns = language == "zh"
            ? new[] { @"据([^，。]+)称", @"([^，。]+)表示", @"([^，。]+)指出", @"([^，。]+)认为" }
            : new[] { @"according to ([^,.]+)", @"([^,.]+) said", @"([^,.]+) stated", @"([^,.]+) noted" };

        foreach (var pattern in attributionPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var source = match.Groups[1].Value.Trim();
                if (source.Length < 2 || source.Length > 30) continue;

                // 检查是否已存在
                if (!quotes.Any(q => q.Attribution == source))
                {
                    quotes.Add(new AttributedQuote
                    {
                        Text = match.Value,
                        Attribution = source
                    });
                }
            }
        }

        var attributedCount = quotes.Count(q => q.HasAttribution);
        var unattributedCount = quotes.Count(q => !q.HasAttribution);

        // 评分：有归因的引语越多越好
        var score = attributedCount switch
        {
            0 => quotes.Count > 0 ? 3.0 : 5.0, // 有引语但无归因=3分，无引语=5分（中性）
            1 => 7.0,
            2 => 8.5,
            _ => 10.0
        };

        // 惩罚无归因引语
        if (unattributedCount > 0) score -= unattributedCount * 0.5;
        score = Math.Max(0, Math.Min(10, score));

        _logger.LogDebug("[AiseoAudit] 引语归因检测: {Total} 引语, {Attributed} 有归因, {Unattributed} 无归因",
            quotes.Count, attributedCount, unattributedCount);

        return Task.FromResult(new QuotationAttributionMetric
        {
            HasAttribution = attributedCount > 0,
            Score = Math.Round(score, 1),
            Quotes = quotes,
            AttributedCount = attributedCount,
            UnattributedCount = unattributedCount
        });
    }

    private string? ExtractAttribution(string textAfterQuote, string language)
    {
        // 在引号后查找归因来源
        var patterns = language == "zh"
            ? new[] { @"^[，,\s]*([^，。\s]{2,15})(说|表示|指出|认为|称)" }
            : new[] { @"^[,\s]*([A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\s+(?:said|stated|noted|added)" };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(textAfterQuote, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    #endregion

    #region 辅助方法

    private string[] SplitParagraphs(string content)
    {
        return content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

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

    private string GetFirstNWords(string content, int n, LanguageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "";

        if (settings.IsCharacterBased)
        {
            var chars = content.Where(c => !char.IsWhiteSpace(c)).Take(n);
            return string.Concat(chars);
        }
        else
        {
            var words = content.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                              .Take(n);
            return string.Join(" ", words);
        }
    }

    #endregion
}
