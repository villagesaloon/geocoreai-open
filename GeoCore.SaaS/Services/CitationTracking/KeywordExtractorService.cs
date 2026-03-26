using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.SaaS.Services.CitationTracking;

/// <summary>
/// 自动关键词提取服务
/// 来源：github.com/AI2HU/gego - MIT License
/// 原理：从 AI 响应中自动提取品牌/关键词，无需预定义列表
/// 排除词从数据库动态加载，支持任意语言
/// </summary>
public class KeywordExtractorService
{
    private readonly ILogger<KeywordExtractorService> _logger;
    private readonly GeoDbContext _dbContext;
    
    private HashSet<string> _exclusions = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastLoadTime = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

    public KeywordExtractorService(
        ILogger<KeywordExtractorService> logger,
        GeoDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 从数据库加载排除词（带缓存）
    /// </summary>
    public async Task LoadExclusionsAsync(CancellationToken cancellationToken = default)
    {
        if (DateTime.UtcNow - _lastLoadTime < _cacheExpiry && _exclusions.Count > 0)
            return;

        try
        {
            var exclusionEntities = await _dbContext.Client
                .Queryable<KeywordExclusionEntity>()
                .Where(e => e.IsEnabled)
                .ToListAsync();

            _exclusions = new HashSet<string>(
                exclusionEntities.Select(e => e.Word),
                StringComparer.OrdinalIgnoreCase);

            _lastLoadTime = DateTime.UtcNow;
            _logger.LogDebug("[KeywordExtractor] Loaded {Count} exclusion words from database", _exclusions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[KeywordExtractor] Failed to load exclusions from database, using cached data");
        }
    }

    /// <summary>
    /// 添加自定义排除词（运行时）
    /// </summary>
    public void AddCustomExclusions(IEnumerable<string> exclusions)
    {
        foreach (var word in exclusions)
        {
            _exclusions.Add(word);
        }
    }

    /// <summary>
    /// 从文本中提取关键词
    /// </summary>
    public async Task<List<ExtractedKeyword>> ExtractKeywordsAsync(string text, int maxKeywords = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<ExtractedKeyword>();

        // 确保排除词已加载
        await LoadExclusionsAsync(cancellationToken);

        var keywords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 1. 提取大写开头的英文词组（品牌名、产品名等）
        ExtractCapitalizedPhrases(text, keywords);

        // 2. 提取引号内的内容
        ExtractQuotedContent(text, keywords);

        // 3. 提取中文专有名词（连续中文字符）
        ExtractChineseProperNouns(text, keywords);

        // 4. 提取带有特殊格式的词（如 CamelCase）
        ExtractCamelCaseWords(text, keywords);

        // 5. 过滤排除词
        var filtered = keywords
            .Where(kv => !IsExcluded(kv.Key))
            .Where(kv => kv.Key.Length >= 2) // 至少2个字符
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .Take(maxKeywords)
            .Select(kv => new ExtractedKeyword
            {
                Keyword = kv.Key,
                Count = kv.Value,
                Type = DetermineKeywordType(kv.Key)
            })
            .ToList();

        _logger.LogDebug("[KeywordExtractor] Extracted {Count} keywords from text", filtered.Count);

        return filtered;
    }

    /// <summary>
    /// 从多个文本中提取并汇总关键词
    /// </summary>
    public async Task<List<ExtractedKeyword>> ExtractAndAggregateKeywordsAsync(
        IEnumerable<string> texts, 
        int maxKeywords = 50,
        CancellationToken cancellationToken = default)
    {
        // 确保排除词已加载
        await LoadExclusionsAsync(cancellationToken);

        var aggregated = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var text in texts)
        {
            var keywords = await ExtractKeywordsAsync(text, 100, cancellationToken);
            foreach (var kw in keywords)
            {
                if (aggregated.ContainsKey(kw.Keyword))
                    aggregated[kw.Keyword] += kw.Count;
                else
                    aggregated[kw.Keyword] = kw.Count;
            }
        }

        return aggregated
            .OrderByDescending(kv => kv.Value)
            .Take(maxKeywords)
            .Select(kv => new ExtractedKeyword
            {
                Keyword = kv.Key,
                Count = kv.Value,
                Type = DetermineKeywordType(kv.Key)
            })
            .ToList();
    }

    private void ExtractCapitalizedPhrases(string text, Dictionary<string, int> keywords)
    {
        // 匹配大写开头的词或词组（如 "OpenAI", "Google Cloud", "Microsoft Azure"）
        var pattern = @"\b([A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)\b";
        var matches = Regex.Matches(text, pattern);

        foreach (Match match in matches)
        {
            var phrase = match.Value.Trim();
            if (phrase.Length >= 2)
            {
                if (keywords.ContainsKey(phrase))
                    keywords[phrase]++;
                else
                    keywords[phrase] = 1;
            }
        }

        // 匹配全大写缩写词（如 "GPT", "LLM", "NLP"）
        var acronymPattern = @"\b([A-Z]{2,})\b";
        var acronymMatches = Regex.Matches(text, acronymPattern);

        foreach (Match match in acronymMatches)
        {
            var acronym = match.Value;
            if (acronym.Length >= 2 && acronym.Length <= 10)
            {
                if (keywords.ContainsKey(acronym))
                    keywords[acronym]++;
                else
                    keywords[acronym] = 1;
            }
        }
    }

    private void ExtractQuotedContent(string text, Dictionary<string, int> keywords)
    {
        // 匹配双引号内容
        var doubleQuotePattern = @"""([^""]+)""";
        var doubleMatches = Regex.Matches(text, doubleQuotePattern);

        foreach (Match match in doubleMatches)
        {
            var content = match.Groups[1].Value.Trim();
            if (content.Length >= 2 && content.Length <= 50)
            {
                if (keywords.ContainsKey(content))
                    keywords[content]++;
                else
                    keywords[content] = 1;
            }
        }

        // 匹配中文引号内容
        var chineseQuotePattern = @"[\u300c\u201c\u2018]([^\u300c\u201c\u2018\u300d\u201d\u2019]+)[\u300d\u201d\u2019]";
        var chineseMatches = Regex.Matches(text, chineseQuotePattern);

        foreach (Match match in chineseMatches)
        {
            var content = match.Groups[1].Value.Trim();
            if (content.Length >= 2 && content.Length <= 50)
            {
                if (keywords.ContainsKey(content))
                    keywords[content]++;
                else
                    keywords[content] = 1;
            }
        }
    }

    private void ExtractChineseProperNouns(string text, Dictionary<string, int> keywords)
    {
        // 匹配可能是专有名词的中文词组（2-6个汉字）
        var pattern = @"[\u4e00-\u9fff]{2,6}";
        var matches = Regex.Matches(text, pattern);

        foreach (Match match in matches)
        {
            var word = match.Value;
            // 简单启发式：如果词在文本中出现多次，可能是重要关键词
            var count = Regex.Matches(text, Regex.Escape(word)).Count;
            if (count >= 2)
            {
                if (keywords.ContainsKey(word))
                    keywords[word] = Math.Max(keywords[word], count);
                else
                    keywords[word] = count;
            }
        }
    }

    private void ExtractCamelCaseWords(string text, Dictionary<string, int> keywords)
    {
        // 匹配 CamelCase 或 camelCase 格式的词
        var pattern = @"\b([a-z]+[A-Z][a-zA-Z]*|[A-Z][a-z]+[A-Z][a-zA-Z]*)\b";
        var matches = Regex.Matches(text, pattern);

        foreach (Match match in matches)
        {
            var word = match.Value;
            if (word.Length >= 4)
            {
                if (keywords.ContainsKey(word))
                    keywords[word]++;
                else
                    keywords[word] = 1;
            }
        }
    }

    private bool IsExcluded(string keyword)
    {
        return _exclusions.Contains(keyword);
    }

    private KeywordType DetermineKeywordType(string keyword)
    {
        // 全大写 -> 缩写
        if (Regex.IsMatch(keyword, @"^[A-Z]{2,}$"))
            return KeywordType.Acronym;

        // 包含中文 -> 中文关键词
        if (Regex.IsMatch(keyword, @"[\u4e00-\u9fff]"))
            return KeywordType.Chinese;

        // CamelCase -> 技术术语
        if (Regex.IsMatch(keyword, @"[a-z][A-Z]"))
            return KeywordType.Technical;

        // 大写开头 -> 品牌/专有名词
        if (Regex.IsMatch(keyword, @"^[A-Z]"))
            return KeywordType.Brand;

        return KeywordType.General;
    }
}

/// <summary>
/// 提取的关键词
/// </summary>
public class ExtractedKeyword
{
    public string Keyword { get; set; } = "";
    public int Count { get; set; }
    public KeywordType Type { get; set; }
}

/// <summary>
/// 关键词类型
/// </summary>
public enum KeywordType
{
    General,
    Brand,
    Acronym,
    Technical,
    Chinese
}
