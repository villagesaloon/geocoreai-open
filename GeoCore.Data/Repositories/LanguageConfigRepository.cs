using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 语言配置 Repository
/// </summary>
public class LanguageConfigRepository
{
    private readonly GeoDbContext _context;

    public LanguageConfigRepository(GeoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有启用的语言配置
    /// </summary>
    public async Task<List<LanguageConfigEntity>> GetAllEnabledAsync()
    {
        return await _context.Client.Queryable<LanguageConfigEntity>()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 根据语言代码获取配置
    /// </summary>
    public async Task<LanguageConfigEntity?> GetByCodeAsync(string languageCode)
    {
        return await _context.Client.Queryable<LanguageConfigEntity>()
            .Where(x => x.LanguageCode == languageCode && x.IsEnabled)
            .FirstAsync();
    }

    /// <summary>
    /// 根据语系获取配置
    /// </summary>
    public async Task<List<LanguageConfigEntity>> GetByFamilyAsync(string family)
    {
        return await _context.Client.Queryable<LanguageConfigEntity>()
            .Where(x => x.LanguageFamily == family && x.IsEnabled)
            .ToListAsync();
    }

    /// <summary>
    /// 获取适用于指定语言的提取模式
    /// 按优先级：特定语言 > 语系 > 全局
    /// </summary>
    public async Task<List<ExtractionPatternEntity>> GetPatternsForLanguageAsync(string languageCode, string languageFamily, string category)
    {
        var patterns = await _context.Client.Queryable<ExtractionPatternEntity>()
            .Where(x => x.IsEnabled && x.Category == category)
            .Where(x => 
                x.Scope == "global" ||
                (x.Scope == "family" && x.ScopeValue == languageFamily) ||
                (x.Scope == "language" && x.ScopeValue == languageCode))
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        // 按优先级排序：特定语言 > 语系 > 全局
        return patterns.OrderByDescending(x => x.Scope == "language" ? 3 : x.Scope == "family" ? 2 : 1)
                       .ThenBy(x => x.SortOrder)
                       .ToList();
    }

    /// <summary>
    /// 获取适用于指定语言的已知实体
    /// </summary>
    public async Task<List<KnownEntityEntity>> GetEntitiesForLanguageAsync(string languageCode, string entityType)
    {
        return await _context.Client.Queryable<KnownEntityEntity>()
            .Where(x => x.IsEnabled && x.EntityType == entityType)
            .Where(x => x.Scope == "global" || (x.Scope == "language" && x.ScopeValue == languageCode))
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有提取模式（Admin 用）
    /// </summary>
    public async Task<List<ExtractionPatternEntity>> GetAllPatternsAsync()
    {
        var patterns = await _context.Client.Queryable<ExtractionPatternEntity>()
            .OrderBy(x => x.Category)
            .ToListAsync();
        return patterns.OrderBy(x => x.Category).ThenBy(x => x.SortOrder).ToList();
    }

    /// <summary>
    /// 获取所有已知实体（Admin 用）
    /// </summary>
    public async Task<List<KnownEntityEntity>> GetAllEntitiesAsync()
    {
        var entities = await _context.Client.Queryable<KnownEntityEntity>()
            .OrderBy(x => x.EntityType)
            .ToListAsync();
        return entities.OrderBy(x => x.EntityType).ThenBy(x => x.EntityName).ToList();
    }

    /// <summary>
    /// 保存语言配置
    /// </summary>
    public async Task<int> SaveLanguageConfigAsync(LanguageConfigEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.Id == 0)
        {
            entity.CreatedAt = DateTime.UtcNow;
            return await _context.Client.Insertable(entity).ExecuteReturnIdentityAsync();
        }
        else
        {
            await _context.Client.Updateable(entity).ExecuteCommandAsync();
            return entity.Id;
        }
    }

    /// <summary>
    /// 保存提取模式
    /// </summary>
    public async Task<int> SavePatternAsync(ExtractionPatternEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.Id == 0)
        {
            entity.CreatedAt = DateTime.UtcNow;
            return await _context.Client.Insertable(entity).ExecuteReturnIdentityAsync();
        }
        else
        {
            await _context.Client.Updateable(entity).ExecuteCommandAsync();
            return entity.Id;
        }
    }

    /// <summary>
    /// 保存已知实体
    /// </summary>
    public async Task<int> SaveEntityAsync(KnownEntityEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.Id == 0)
        {
            entity.CreatedAt = DateTime.UtcNow;
            return await _context.Client.Insertable(entity).ExecuteReturnIdentityAsync();
        }
        else
        {
            await _context.Client.Updateable(entity).ExecuteCommandAsync();
            return entity.Id;
        }
    }

    /// <summary>
    /// 批量保存提取模式
    /// </summary>
    public async Task BulkInsertPatternsAsync(List<ExtractionPatternEntity> patterns)
    {
        if (patterns.Count == 0) return;
        foreach (var p in patterns)
        {
            p.CreatedAt = DateTime.UtcNow;
            p.UpdatedAt = DateTime.UtcNow;
        }
        await _context.Client.Insertable(patterns).ExecuteCommandAsync();
    }

    /// <summary>
    /// 批量保存已知实体
    /// </summary>
    public async Task BulkInsertEntitiesAsync(List<KnownEntityEntity> entities)
    {
        if (entities.Count == 0) return;
        foreach (var e in entities)
        {
            e.CreatedAt = DateTime.UtcNow;
            e.UpdatedAt = DateTime.UtcNow;
        }
        await _context.Client.Insertable(entities).ExecuteCommandAsync();
    }

    #region 情感关键词

    /// <summary>
    /// 获取指定语言的情感关键词
    /// </summary>
    public async Task<List<SentimentKeywordEntity>> GetSentimentKeywordsAsync(string languageCode, string sentimentType)
    {
        var keywords = await _context.Client.Queryable<SentimentKeywordEntity>()
            .Where(x => x.IsEnabled && x.SentimentType == sentimentType)
            .Where(x => x.Scope == "global" || (x.Scope == "language" && x.ScopeValue == languageCode))
            .ToListAsync();
        
        return keywords;
    }

    /// <summary>
    /// 获取所有情感关键词（Admin 用）
    /// </summary>
    public async Task<List<SentimentKeywordEntity>> GetAllSentimentKeywordsAsync()
    {
        return await _context.Client.Queryable<SentimentKeywordEntity>()
            .OrderBy(x => x.SentimentType)
            .OrderBy(x => x.Scope)
            .ToListAsync();
    }

    /// <summary>
    /// 保存情感关键词
    /// </summary>
    public async Task<int> SaveSentimentKeywordAsync(SentimentKeywordEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.Id == 0)
        {
            entity.CreatedAt = DateTime.UtcNow;
            return await _context.Client.Insertable(entity).ExecuteReturnIdentityAsync();
        }
        else
        {
            await _context.Client.Updateable(entity).ExecuteCommandAsync();
            return entity.Id;
        }
    }

    /// <summary>
    /// 批量保存情感关键词
    /// </summary>
    public async Task BulkInsertSentimentKeywordsAsync(List<SentimentKeywordEntity> keywords)
    {
        if (keywords.Count == 0) return;
        foreach (var k in keywords)
        {
            k.CreatedAt = DateTime.UtcNow;
            k.UpdatedAt = DateTime.UtcNow;
        }
        await _context.Client.Insertable(keywords).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除情感关键词
    /// </summary>
    public async Task DeleteSentimentKeywordAsync(int id)
    {
        await _context.Client.Deleteable<SentimentKeywordEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();
    }

    #endregion
}
