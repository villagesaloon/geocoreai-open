using System.Text.Json;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// 语言配置提供者 - 从数据库加载配置，带缓存
/// </summary>
public class LanguageConfigProvider
{
    private readonly ILogger<LanguageConfigProvider> _logger;
    private readonly LanguageConfigRepository _repository;
    private readonly IMemoryCache _cache;
    
    private const string CacheKeyLanguages = "LanguageConfigs";
    private const string CacheKeyPatterns = "ExtractionPatterns";
    private const string CacheKeyEntities = "KnownEntities";
    private const string CacheKeySentiments = "SentimentKeywords";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public LanguageConfigProvider(
        ILogger<LanguageConfigProvider> logger,
        LanguageConfigRepository repository,
        IMemoryCache cache)
    {
        _logger = logger;
        _repository = repository;
        _cache = cache;
    }

    /// <summary>
    /// 获取语言配置
    /// </summary>
    public virtual async Task<LanguageSettings> GetLanguageSettingsAsync(string languageCode)
    {
        var languages = await GetAllLanguagesAsync();
        var config = languages.FirstOrDefault(l => l.LanguageCode == languageCode);
        
        if (config == null)
        {
            _logger.LogWarning("[LanguageConfigProvider] 未找到语言配置: {Code}，使用默认配置", languageCode);
            return GetDefaultSettings(languageCode);
        }

        return new LanguageSettings
        {
            LanguageCode = config.LanguageCode,
            LanguageFamily = config.LanguageFamily,
            TokenizationMethod = config.TokenizationMethod,
            SentenceDelimiters = ParseJsonArray<char>(config.SentenceDelimiters),
            QuoteCharPairs = ParseQuotePairs(config.QuoteCharPairs)
        };
    }

    /// <summary>
    /// 获取指定语言的提取模式
    /// </summary>
    public virtual async Task<List<string>> GetPatternsAsync(string languageCode, string category)
    {
        var settings = await GetLanguageSettingsAsync(languageCode);
        var allPatterns = await GetAllPatternsAsync();
        
        // 按优先级筛选：特定语言 > 语系 > 全局
        var applicable = allPatterns
            .Where(p => p.Category == category && p.IsEnabled)
            .Where(p => 
                p.Scope == "global" ||
                (p.Scope == "family" && p.ScopeValue == settings.LanguageFamily) ||
                (p.Scope == "language" && p.ScopeValue == languageCode))
            .GroupBy(p => p.Pattern)
            .Select(g => g.OrderByDescending(p => GetScopePriority(p.Scope, p.ScopeValue, languageCode, settings.LanguageFamily)).First())
            .OrderBy(p => p.SortOrder)
            .Select(p => p.Pattern)
            .ToList();

        return applicable;
    }

    /// <summary>
    /// 获取指定语言的已知实体
    /// </summary>
    public virtual async Task<HashSet<string>> GetKnownEntitiesAsync(string languageCode, string entityType)
    {
        var allEntities = await GetAllEntitiesAsync();
        
        var applicable = allEntities
            .Where(e => e.EntityType == entityType && e.IsEnabled)
            .Where(e => e.Scope == "global" || (e.Scope == "language" && e.ScopeValue == languageCode))
            .SelectMany(e => GetEntityNames(e))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return applicable;
    }

    /// <summary>
    /// 获取指定语言的情感关键词
    /// </summary>
    public virtual async Task<HashSet<string>> GetSentimentKeywordsAsync(string languageCode, string sentimentType)
    {
        var allKeywords = await GetAllSentimentKeywordsAsync();
        
        var applicable = allKeywords
            .Where(k => k.SentimentType == sentimentType && k.IsEnabled)
            .Where(k => k.Scope == "global" || (k.Scope == "language" && k.ScopeValue == languageCode))
            .Select(k => k.Keyword)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return applicable;
    }

    /// <summary>
    /// 清除缓存（Admin 更新配置后调用）
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(CacheKeyLanguages);
        _cache.Remove(CacheKeyPatterns);
        _cache.Remove(CacheKeyEntities);
        _cache.Remove(CacheKeySentiments);
        _logger.LogInformation("[LanguageConfigProvider] 缓存已清除");
    }

    #region 私有方法

    private async Task<List<LanguageConfigEntity>> GetAllLanguagesAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeyLanguages, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var result = await _repository.GetAllEnabledAsync();
            _logger.LogDebug("[LanguageConfigProvider] 加载了 {Count} 个语言配置", result.Count);
            return result;
        }) ?? new List<LanguageConfigEntity>();
    }

    private async Task<List<ExtractionPatternEntity>> GetAllPatternsAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeyPatterns, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var result = await _repository.GetAllPatternsAsync();
            _logger.LogDebug("[LanguageConfigProvider] 加载了 {Count} 个提取模式", result.Count);
            return result;
        }) ?? new List<ExtractionPatternEntity>();
    }

    private async Task<List<KnownEntityEntity>> GetAllEntitiesAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeyEntities, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var result = await _repository.GetAllEntitiesAsync();
            _logger.LogDebug("[LanguageConfigProvider] 加载了 {Count} 个已知实体", result.Count);
            return result;
        }) ?? new List<KnownEntityEntity>();
    }

    private async Task<List<SentimentKeywordEntity>> GetAllSentimentKeywordsAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeySentiments, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var result = await _repository.GetAllSentimentKeywordsAsync();
            _logger.LogDebug("[LanguageConfigProvider] 加载了 {Count} 个情感关键词", result.Count);
            return result;
        }) ?? new List<SentimentKeywordEntity>();
    }

    private static int GetScopePriority(string scope, string? scopeValue, string languageCode, string languageFamily)
    {
        if (scope == "language" && scopeValue == languageCode) return 3;
        if (scope == "family" && scopeValue == languageFamily) return 2;
        if (scope == "global") return 1;
        return 0;
    }

    private static IEnumerable<string> GetEntityNames(KnownEntityEntity entity)
    {
        yield return entity.EntityName;
        
        if (!string.IsNullOrEmpty(entity.Aliases))
        {
            var aliases = ParseJsonArray<string>(entity.Aliases);
            foreach (var alias in aliases)
            {
                yield return alias;
            }
        }
    }

    private static List<T> ParseJsonArray<T>(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static List<(char open, char close)> ParseQuotePairs(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new List<(char, char)> { ('"', '"') };
        try
        {
            var pairs = JsonSerializer.Deserialize<List<List<string>>>(json);
            if (pairs == null) return new List<(char, char)> { ('"', '"') };
            
            return pairs
                .Where(p => p.Count >= 2 && p[0].Length > 0 && p[1].Length > 0)
                .Select(p => (p[0][0], p[1][0]))
                .ToList();
        }
        catch
        {
            return new List<(char, char)> { ('"', '"') };
        }
    }

    private static LanguageSettings GetDefaultSettings(string languageCode)
    {
        // 根据语言代码推断默认设置
        var isCjk = languageCode is "zh" or "zh_tw" or "ja" or "ko";
        
        return new LanguageSettings
        {
            LanguageCode = languageCode,
            LanguageFamily = isCjk ? "cjk" : "latin",
            TokenizationMethod = isCjk ? "character" : "space",
            SentenceDelimiters = isCjk 
                ? new List<char> { '。', '！', '？', '；' }
                : new List<char> { '.', '!', '?' },
            QuoteCharPairs = new List<(char, char)> { ('"', '"') }
        };
    }

    #endregion
}

/// <summary>
/// 语言设置（运行时使用）
/// </summary>
public class LanguageSettings
{
    public string LanguageCode { get; set; } = "";
    public string LanguageFamily { get; set; } = "latin";
    public string TokenizationMethod { get; set; } = "space";
    public List<char> SentenceDelimiters { get; set; } = new() { '.', '!', '?' };
    public List<(char open, char close)> QuoteCharPairs { get; set; } = new() { ('"', '"') };

    /// <summary>
    /// 是否按字符分词（CJK 语系）
    /// </summary>
    public bool IsCharacterBased => TokenizationMethod == "character";
}
