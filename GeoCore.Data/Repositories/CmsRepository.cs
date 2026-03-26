using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// CMS 文章 Repository
/// Phase 10: 轻量 CMS
/// </summary>
public class CmsArticleRepository
{
    private readonly GeoDbContext _db;

    public CmsArticleRepository(GeoDbContext db)
    {
        _db = db;
    }

    #region 文章 CRUD

    public async Task<List<CmsArticleEntity>> GetAllAsync(string? articleType = null, bool? isPublished = null)
    {
        var query = _db.Client.Queryable<CmsArticleEntity>();
        
        if (!string.IsNullOrEmpty(articleType))
            query = query.Where(x => x.ArticleType == articleType);
        
        if (isPublished.HasValue)
            query = query.Where(x => x.IsPublished == isPublished.Value);
        
        return await query.OrderByDescending(x => x.IsFeatured)
                          .OrderBy(x => x.SortOrder)
                          .OrderByDescending(x => x.PublishedAt)
                          .ToListAsync();
    }

    public async Task<CmsArticleEntity?> GetByIdAsync(int id)
    {
        return await _db.Client.Queryable<CmsArticleEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<CmsArticleEntity?> GetBySlugAsync(string slug)
    {
        return await _db.Client.Queryable<CmsArticleEntity>()
            .Where(x => x.Slug == slug && x.IsPublished)
            .FirstAsync();
    }

    public async Task<List<CmsArticleEntity>> GetByCategoryAsync(string category, bool publishedOnly = true)
    {
        var query = _db.Client.Queryable<CmsArticleEntity>()
            .Where(x => x.Category == category);
        
        if (publishedOnly)
            query = query.Where(x => x.IsPublished);
        
        return await query.OrderByDescending(x => x.IsFeatured)
                          .OrderBy(x => x.SortOrder)
                          .OrderByDescending(x => x.PublishedAt)
                          .ToListAsync();
    }

    public async Task<int> CreateAsync(CmsArticleEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.IsPublished && !entity.PublishedAt.HasValue)
            entity.PublishedAt = DateTime.UtcNow;
        
        return await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    public async Task<bool> UpdateAsync(CmsArticleEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.IsPublished && !entity.PublishedAt.HasValue)
            entity.PublishedAt = DateTime.UtcNow;
        
        return await _db.Client.Updateable(entity).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Client.Deleteable<CmsArticleEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }

    public async Task<bool> IncrementViewCountAsync(int id)
    {
        return await _db.Client.Updateable<CmsArticleEntity>()
            .SetColumns(x => x.ViewCount == x.ViewCount + 1)
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }

    #endregion
}

/// <summary>
/// CMS 分类 Repository
/// Phase 10: 轻量 CMS
/// </summary>
public class CmsCategoryRepository
{
    private readonly GeoDbContext _db;

    public CmsCategoryRepository(GeoDbContext db)
    {
        _db = db;
    }

    public async Task<List<CmsCategoryEntity>> GetAllAsync(string? articleType = null)
    {
        var query = _db.Client.Queryable<CmsCategoryEntity>()
            .Where(x => x.IsActive);
        
        if (!string.IsNullOrEmpty(articleType))
            query = query.Where(x => x.ArticleType == articleType);
        
        return await query.OrderBy(x => x.SortOrder).ToListAsync();
    }

    public async Task<CmsCategoryEntity?> GetByIdAsync(int id)
    {
        return await _db.Client.Queryable<CmsCategoryEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<CmsCategoryEntity?> GetBySlugAsync(string slug, string articleType)
    {
        return await _db.Client.Queryable<CmsCategoryEntity>()
            .Where(x => x.Slug == slug && x.ArticleType == articleType && x.IsActive)
            .FirstAsync();
    }

    public async Task<int> CreateAsync(CmsCategoryEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        return await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    public async Task<bool> UpdateAsync(CmsCategoryEntity entity)
    {
        return await _db.Client.Updateable(entity).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Client.Deleteable<CmsCategoryEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandAsync() > 0;
    }
}
