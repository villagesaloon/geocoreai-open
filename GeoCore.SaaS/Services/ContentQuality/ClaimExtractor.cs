using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// Claim（事实/声明）提取器
/// 基于数据库配置的规则提取可验证的事实陈述
/// </summary>
public class ClaimExtractor
{
    private readonly ILogger<ClaimExtractor> _logger;
    private readonly LanguageConfigProvider _configProvider;

    public ClaimExtractor(ILogger<ClaimExtractor> logger, LanguageConfigProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
    }

    /// <summary>
    /// 从内容中提取 claims（异步，从数据库加载配置）
    /// </summary>
    public async Task<List<ExtractedClaim>> ExtractClaimsAsync(string content, string language = "zh")
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<ExtractedClaim>();

        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var claims = new List<ExtractedClaim>();
        var wordCount = CountWords(content, settings);
        
        // 1. 提取数字型 claims
        var numberPatterns = await _configProvider.GetPatternsAsync(language, "claim_number");
        claims.AddRange(ExtractByPatterns(content, numberPatterns, "number", settings));
        
        // 2. 提取统计型 claims
        var statisticPatterns = await _configProvider.GetPatternsAsync(language, "claim_statistic");
        claims.AddRange(ExtractByPatterns(content, statisticPatterns, "statistic", settings));
        
        // 3. 提取引用型 claims
        var citationPatterns = await _configProvider.GetPatternsAsync(language, "claim_citation");
        claims.AddRange(ExtractByPatterns(content, citationPatterns, "citation", settings));
        
        // 4. 提取事实型 claims
        var factPatterns = await _configProvider.GetPatternsAsync(language, "claim_fact");
        claims.AddRange(ExtractByPatterns(content, factPatterns, "fact", settings));

        // 去重（按文本去重）
        var deduplicated = claims
            .GroupBy(c => c.Text.Trim())
            .Select(g => g.First())
            .OrderBy(c => c.Position)
            .ToList();

        _logger.LogDebug("[ClaimExtractor] 从 {WordCount} 词内容中提取到 {ClaimCount} 个 claims", 
            wordCount, deduplicated.Count);

        return deduplicated;
    }

    /// <summary>
    /// 同步版本（用于兼容现有代码，使用默认配置）
    /// </summary>
    public List<ExtractedClaim> ExtractClaims(string content, string language = "zh")
    {
        return ExtractClaimsAsync(content, language).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 使用模式列表提取 claims
    /// </summary>
    private List<ExtractedClaim> ExtractByPatterns(string content, List<string> patterns, string claimType, LanguageSettings settings)
    {
        var claims = new List<ExtractedClaim>();

        foreach (var pattern in patterns)
        {
            try
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var context = claimType == "number" 
                        ? GetClaimContext(content, match.Index, match.Length, settings)
                        : match.Value.Trim();
                    var position = EstimateWordPosition(content, match.Index, settings);
                    
                    claims.Add(new ExtractedClaim
                    {
                        Text = context,
                        Type = claimType,
                        Position = position
                    });
                }
            }
            catch (RegexParseException ex)
            {
                _logger.LogWarning("[ClaimExtractor] 无效的正则表达式: {Pattern}, 错误: {Error}", pattern, ex.Message);
            }
        }

        return claims;
    }

    /// <summary>
    /// 获取 claim 的上下文（包含该 claim 的句子片段）
    /// </summary>
    private string GetClaimContext(string content, int matchIndex, int matchLength, LanguageSettings settings)
    {
        var delimiters = settings.SentenceDelimiters.ToHashSet();
        delimiters.Add('\n');
        
        // 向前找到句子开始（最多50字符）
        var start = matchIndex;
        var searchStart = Math.Max(0, matchIndex - 50);
        for (var i = matchIndex - 1; i >= searchStart; i--)
        {
            if (delimiters.Contains(content[i]))
            {
                start = i + 1;
                break;
            }
            start = i;
        }

        // 向后找到句子结束（最多50字符）
        var end = matchIndex + matchLength;
        var searchEnd = Math.Min(content.Length, matchIndex + matchLength + 50);
        for (var i = matchIndex + matchLength; i < searchEnd; i++)
        {
            if (delimiters.Contains(content[i]))
            {
                end = i + 1;
                break;
            }
            end = i + 1;
        }

        return content[start..end].Trim();
    }

    /// <summary>
    /// 估算字符位置对应的词位置
    /// </summary>
    private int EstimateWordPosition(string content, int charIndex, LanguageSettings settings)
    {
        var textBefore = content[..charIndex];
        
        if (settings.IsCharacterBased)
        {
            return textBefore.Length;
        }
        else
        {
            return textBefore.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }

    /// <summary>
    /// 统计词数
    /// </summary>
    private int CountWords(string content, LanguageSettings settings)
    {
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
}
