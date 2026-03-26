using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.CitationTracking.Models;
using GeoCore.SaaS.Services.ContentQuality;

namespace GeoCore.SaaS.Services.CitationTracking;

/// <summary>
/// 引用分析器实现
/// </summary>
public class CitationAnalyzer : ICitationAnalyzer
{
    private readonly ILogger<CitationAnalyzer> _logger;
    private readonly LanguageConfigProvider _configProvider;

    public CitationAnalyzer(
        ILogger<CitationAnalyzer> logger,
        LanguageConfigProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
    }

    public async Task<CitationAnalysisResult> AnalyzeAsync(
        string response,
        string brandName,
        List<string> brandAliases,
        List<string> competitors,
        List<string> detectedLinks,
        CancellationToken cancellationToken = default)
    {
        var result = new CitationAnalysisResult();
        
        if (string.IsNullOrWhiteSpace(response))
        {
            return result;
        }

        // 1. 检测品牌是否被引用
        var allBrandNames = new List<string> { brandName };
        allBrandNames.AddRange(brandAliases);
        
        var (isCited, firstMentionIndex) = DetectBrandMention(response, allBrandNames);
        result.IsCited = isCited;
        
        if (!isCited)
        {
            // 检测竞品引用
            result.CompetitorCitations = DetectCompetitorCitations(response, competitors);
            return result;
        }

        // 2. 计算引用位置
        result.PositionRatio = (double)firstMentionIndex / response.Length;
        result.Position = result.PositionRatio switch
        {
            < 0.2 => CitationPosition.First,
            < 0.7 => CitationPosition.Middle,
            _ => CitationPosition.Last
        };

        // 3. 检测链接
        var (hasLink, detectedLink) = DetectBrandLink(response, brandName, brandAliases, detectedLinks);
        result.HasLink = hasLink;
        result.DetectedLink = detectedLink;

        // 4. 提取引用上下文
        result.CitationContext = ExtractContext(response, firstMentionIndex, 150);

        // 5. 情感分析（从数据库加载关键词）
        var (sentiment, sentimentScore) = await AnalyzeSentimentAsync(result.CitationContext ?? response);
        result.Sentiment = sentiment;
        result.SentimentScore = sentimentScore;

        // 6. 计算 Word Count 指标
        var (wordCountRatio, citationWordCount, totalWordCount) = CalculateWordCountMetrics(response, result.CitationContext);
        result.WordCountRatio = wordCountRatio;
        result.CitationWordCount = citationWordCount;
        result.TotalWordCount = totalWordCount;

        // 7. 计算 Position-Adjusted Score
        result.PositionAdjustedScore = CalculatePositionAdjustedScore(result.PositionRatio, citationWordCount, totalWordCount);

        // 8. 计算可见度评分 (BVS)
        result.VisibilityScore = CalculateVisibilityScore(result);

        // 9. 检测竞品引用
        result.CompetitorCitations = DetectCompetitorCitations(response, competitors);

        _logger.LogDebug(
            "[CitationAnalyzer] Brand: {Brand}, Cited: {Cited}, Position: {Position}, HasLink: {HasLink}, Sentiment: {Sentiment}, BVS: {BVS}, WordCount: {WC}%, PAS: {PAS}",
            brandName, result.IsCited, result.Position, result.HasLink, result.Sentiment, result.VisibilityScore, 
            (wordCountRatio * 100).ToString("F1"), result.PositionAdjustedScore.ToString("F2"));

        return result;
    }

    private (bool isCited, int firstIndex) DetectBrandMention(string response, List<string> brandNames)
    {
        var firstIndex = int.MaxValue;
        var found = false;

        foreach (var name in brandNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            
            // 使用正则表达式进行词边界匹配
            var pattern = $@"\b{Regex.Escape(name)}\b";
            var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
            
            if (match.Success && match.Index < firstIndex)
            {
                firstIndex = match.Index;
                found = true;
            }
        }

        return (found, found ? firstIndex : -1);
    }

    private (bool hasLink, string? link) DetectBrandLink(
        string response, 
        string brandName, 
        List<string> brandAliases,
        List<string> detectedLinks)
    {
        // 从已检测的链接中查找品牌相关链接
        foreach (var link in detectedLinks)
        {
            if (link.Contains(brandName, StringComparison.OrdinalIgnoreCase))
            {
                return (true, link);
            }
            
            foreach (var alias in brandAliases)
            {
                if (!string.IsNullOrWhiteSpace(alias) && 
                    link.Contains(alias, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, link);
                }
            }
        }

        // 在响应文本中查找 URL
        var urlPattern = @"https?://[^\s\]\)]+";
        var matches = Regex.Matches(response, urlPattern);
        
        foreach (Match match in matches)
        {
            var url = match.Value;
            if (url.Contains(brandName, StringComparison.OrdinalIgnoreCase))
            {
                return (true, url);
            }
            
            foreach (var alias in brandAliases)
            {
                if (!string.IsNullOrWhiteSpace(alias) && 
                    url.Contains(alias, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, url);
                }
            }
        }

        return (false, null);
    }

    private string ExtractContext(string response, int mentionIndex, int contextLength)
    {
        var start = Math.Max(0, mentionIndex - contextLength / 2);
        var end = Math.Min(response.Length, mentionIndex + contextLength / 2);
        
        var context = response.Substring(start, end - start);
        
        // 添加省略号
        if (start > 0) context = "..." + context;
        if (end < response.Length) context += "...";
        
        return context;
    }

    private async Task<(SentimentType sentiment, double score)> AnalyzeSentimentAsync(string context, string language = "en")
    {
        // 从数据库加载情感关键词（支持多语言）
        var positiveKeywords = await _configProvider.GetSentimentKeywordsAsync(language, "positive");
        var negativeKeywords = await _configProvider.GetSentimentKeywordsAsync(language, "negative");

        var positiveCount = 0;
        var negativeCount = 0;

        // 统计正面关键词
        foreach (var keyword in positiveKeywords)
        {
            if (context.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                positiveCount++;
            }
        }

        // 统计负面关键词
        foreach (var keyword in negativeKeywords)
        {
            if (context.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                negativeCount++;
            }
        }

        // 计算情感分数 (-1 到 1)
        var total = positiveCount + negativeCount;
        double score;
        SentimentType sentiment;

        if (total == 0)
        {
            score = 0;
            sentiment = SentimentType.Neutral;
        }
        else
        {
            score = (double)(positiveCount - negativeCount) / total;
            sentiment = score switch
            {
                > 0.2 => SentimentType.Positive,
                < -0.2 => SentimentType.Negative,
                _ => SentimentType.Neutral
            };
        }

        return (sentiment, score);
    }

    private double CalculateVisibilityScore(CitationAnalysisResult result)
    {
        // BVS 计算公式（参考 GEO-METRICS-PRINCIPLES.md）
        // 基础分 + 位置权重 + 链接权重 + 情感权重
        
        double score = 0;

        // 被引用基础分
        if (result.IsCited)
        {
            score += 1.0;
        }

        // 位置权重
        score += result.Position switch
        {
            CitationPosition.First => 3.0,  // 首位提及 +3
            CitationPosition.Middle => 1.5, // 中间提及 +1.5
            CitationPosition.Last => 0.5,   // 末尾提及 +0.5
            _ => 0
        };

        // 链接权重
        if (result.HasLink)
        {
            score += 2.0; // 带链接 +2
        }

        // 情感权重
        score += result.Sentiment switch
        {
            SentimentType.Positive => 1.0,  // 正面 +1
            SentimentType.Neutral => 0,     // 中性 0
            SentimentType.Negative => -1.0, // 负面 -1
            _ => 0
        };

        // 归一化到 0-10 分
        return Math.Max(0, Math.Min(10, score));
    }

    private Dictionary<string, bool> DetectCompetitorCitations(string response, List<string> competitors)
    {
        var result = new Dictionary<string, bool>();
        
        foreach (var competitor in competitors)
        {
            if (string.IsNullOrWhiteSpace(competitor)) continue;
            
            var pattern = $@"\b{Regex.Escape(competitor)}\b";
            var isCited = Regex.IsMatch(response, pattern, RegexOptions.IgnoreCase);
            result[competitor] = isCited;
        }

        return result;
    }

    /// <summary>
    /// 计算 Word Count 指标
    /// 来源：GEO 论文 arxiv.org/abs/2311.09735
    /// </summary>
    private (double ratio, int citationWords, int totalWords) CalculateWordCountMetrics(string response, string? citationContext)
    {
        if (string.IsNullOrWhiteSpace(response))
            return (0, 0, 0);

        // 计算总词数（支持中英文混合）
        var totalWords = CountWords(response);
        
        if (totalWords == 0)
            return (0, 0, 0);

        // 计算引用上下文词数
        var citationWords = string.IsNullOrWhiteSpace(citationContext) ? 0 : CountWords(citationContext);
        
        // 计算比例
        var ratio = (double)citationWords / totalWords;
        
        return (ratio, citationWords, totalWords);
    }

    /// <summary>
    /// 计算 Position-Adjusted Score
    /// 公式：词数 × e^(-位置)，位置越靠前权重越高
    /// </summary>
    private double CalculatePositionAdjustedScore(double positionRatio, int citationWords, int totalWords)
    {
        if (totalWords == 0 || citationWords == 0)
            return 0;

        // 位置衰减因子：e^(-position)，position 在 0-1 之间
        // 位置 0（开头）权重最高 = 1.0
        // 位置 0.5（中间）权重 ≈ 0.6
        // 位置 1（末尾）权重 ≈ 0.37
        var positionWeight = Math.Exp(-positionRatio);
        
        // 词数占比
        var wordRatio = (double)citationWords / totalWords;
        
        // Position-Adjusted Score = 词数占比 × 位置权重 × 100（归一化到 0-100）
        var score = wordRatio * positionWeight * 100;
        
        return Math.Round(score, 2);
    }

    /// <summary>
    /// 统计词数（支持中英文混合）
    /// </summary>
    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // 英文单词：按空格分割
        var englishWords = Regex.Matches(text, @"[a-zA-Z]+").Count;
        
        // 中文字符：每个汉字算一个词
        var chineseChars = Regex.Matches(text, @"[\u4e00-\u9fff]").Count;
        
        // 日文假名
        var japaneseChars = Regex.Matches(text, @"[\u3040-\u309f\u30a0-\u30ff]").Count;
        
        // 韩文
        var koreanChars = Regex.Matches(text, @"[\uac00-\ud7af]").Count;
        
        return englishWords + chineseChars + japaneseChars + koreanChars;
    }
}
