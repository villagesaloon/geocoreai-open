using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// 实体提取器
/// 基于数据库配置提取品牌、人物、产品、地点、日期等命名实体
/// </summary>
public class EntityExtractor
{
    private readonly ILogger<EntityExtractor> _logger;
    private readonly LanguageConfigProvider _configProvider;

    // 常见产品名模式（全局通用，不需要配置）
    private static readonly string[] ProductPatterns = new[]
    {
        @"[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\s+(?:Pro|Plus|Max|Ultra|Mini|Air|Studio|Enterprise|Premium)",
        @"(?:iPhone|iPad|MacBook|Surface|Galaxy|Pixel)\s*\d*\s*(?:Pro|Plus|Max|Ultra|Mini)?",
    };

    public EntityExtractor(ILogger<EntityExtractor> logger, LanguageConfigProvider configProvider)
    {
        _logger = logger;
        _configProvider = configProvider;
    }

    /// <summary>
    /// 从内容中提取实体（异步，从数据库加载配置）
    /// </summary>
    public async Task<List<ExtractedEntity>> ExtractEntitiesAsync(string content, string language = "zh")
    {
        if (string.IsNullOrWhiteSpace(content))
            return new List<ExtractedEntity>();

        var settings = await _configProvider.GetLanguageSettingsAsync(language);
        var entities = new List<ExtractedEntity>();

        // 1. 提取品牌/公司（从数据库加载）
        var knownBrands = await _configProvider.GetKnownEntitiesAsync(language, "brand");
        entities.AddRange(ExtractByKnownList(content, knownBrands, "brand"));

        // 2. 提取产品名（使用固定模式）
        entities.AddRange(ExtractProducts(content));

        // 3. 提取人名（从数据库加载）
        var knownPersons = await _configProvider.GetKnownEntitiesAsync(language, "person");
        entities.AddRange(ExtractByKnownList(content, knownPersons, "person"));
        
        // 人名模式提取
        var personPatterns = await _configProvider.GetPatternsAsync(language, "entity_person");
        entities.AddRange(ExtractByPatterns(content, personPatterns, "person"));

        // 4. 提取地点（从数据库加载）
        var knownLocations = await _configProvider.GetKnownEntitiesAsync(language, "location");
        entities.AddRange(ExtractByKnownList(content, knownLocations, "location"));

        // 5. 提取日期（从数据库加载模式）
        var datePatterns = await _configProvider.GetPatternsAsync(language, "entity_date");
        entities.AddRange(ExtractByPatterns(content, datePatterns, "date", minLength: 4));

        // 6. 提取大写开头的词（英文，可能是专有名词）
        if (!settings.IsCharacterBased)
        {
            entities.AddRange(ExtractCapitalizedWords(content));
        }

        // 合并相同实体，统计出现次数
        var merged = entities
            .GroupBy(e => e.Text.ToLower())
            .Select(g => new ExtractedEntity
            {
                Text = g.First().Text,
                Type = g.First().Type,
                Count = g.Sum(x => x.Count > 0 ? x.Count : 1)
            })
            .OrderByDescending(e => e.Count)
            .ToList();

        _logger.LogDebug("[EntityExtractor] 提取到 {EntityCount} 个实体", merged.Count);

        return merged;
    }

    /// <summary>
    /// 同步版本（用于兼容现有代码）
    /// </summary>
    public List<ExtractedEntity> ExtractEntities(string content, string language = "zh")
    {
        return ExtractEntitiesAsync(content, language).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 使用已知列表提取实体
    /// </summary>
    private List<ExtractedEntity> ExtractByKnownList(string content, HashSet<string> knownList, string entityType)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var item in knownList)
        {
            if (content.Contains(item, StringComparison.OrdinalIgnoreCase))
            {
                entities.Add(new ExtractedEntity
                {
                    Text = item,
                    Type = entityType,
                    Count = Regex.Matches(content, Regex.Escape(item), RegexOptions.IgnoreCase).Count
                });
            }
        }

        return entities;
    }

    /// <summary>
    /// 使用模式列表提取实体
    /// </summary>
    private List<ExtractedEntity> ExtractByPatterns(string content, List<string> patterns, string entityType, int minLength = 0)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var pattern in patterns)
        {
            try
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var text = match.Groups.Count > 1 ? match.Groups[1].Value.Trim() : match.Value.Trim();
                    if (text.Length >= minLength)
                    {
                        entities.Add(new ExtractedEntity
                        {
                            Text = text,
                            Type = entityType
                        });
                    }
                }
            }
            catch (RegexParseException ex)
            {
                _logger.LogWarning("[EntityExtractor] 无效的正则表达式: {Pattern}, 错误: {Error}", pattern, ex.Message);
            }
        }

        return entities;
    }

    /// <summary>
    /// 提取产品名
    /// </summary>
    private List<ExtractedEntity> ExtractProducts(string content)
    {
        var entities = new List<ExtractedEntity>();

        foreach (var pattern in ProductPatterns)
        {
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                entities.Add(new ExtractedEntity
                {
                    Text = match.Value,
                    Type = "product"
                });
            }
        }

        return entities;
    }

    /// <summary>
    /// 提取大写开头的词（可能是专有名词）
    /// </summary>
    private List<ExtractedEntity> ExtractCapitalizedWords(string content)
    {
        var entities = new List<ExtractedEntity>();

        // 匹配连续的大写开头词（2-4个词）
        var pattern = @"\b([A-Z][a-z]+(?:\s+[A-Z][a-z]+){0,3})\b";
        var matches = Regex.Matches(content, pattern);

        // 排除常见词
        var excludeWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "The", "This", "That", "These", "Those", "What", "When", "Where", "Why", "How",
            "And", "But", "Or", "If", "Then", "Because", "However", "Therefore", "Moreover",
            "First", "Second", "Third", "Finally", "Additionally", "Furthermore",
            "According", "Based", "Using", "Including", "Following"
        };

        foreach (Match match in matches)
        {
            var word = match.Value;
            if (!excludeWords.Contains(word.Split(' ')[0]) && word.Length > 3)
            {
                entities.Add(new ExtractedEntity
                {
                    Text = word,
                    Type = "proper_noun"
                });
            }
        }

        return entities;
    }
}
