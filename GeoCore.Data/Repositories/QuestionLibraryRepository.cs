using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 问题库仓储接口
/// </summary>
public interface IQuestionLibraryRepository
{
    Task<List<QuestionLibraryEntity>> GetAllAsync(string? projectId = null, bool enabledOnly = true);
    Task<List<QuestionLibraryEntity>> SearchAsync(string keyword, string? projectId = null, int limit = 50);
    Task<List<QuestionLibraryEntity>> GetByTypeAsync(string questionType, string? projectId = null);
    Task<List<QuestionLibraryEntity>> GetFavoritesAsync(string? projectId = null);
    Task<List<QuestionLibraryEntity>> GetRecentlyUsedAsync(string? projectId = null, int limit = 20);
    Task<QuestionLibraryEntity?> GetByIdAsync(int id);
    Task<int> CreateAsync(QuestionLibraryEntity question);
    Task CreateBatchAsync(List<QuestionLibraryEntity> questions);
    Task UpdateAsync(QuestionLibraryEntity question);
    Task IncrementUsageAsync(int id);
    Task ToggleFavoriteAsync(int id);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(string question, string? projectId = null);
}

/// <summary>
/// 问题库仓储实现
/// </summary>
public class QuestionLibraryRepository : IQuestionLibraryRepository
{
    private readonly GeoDbContext _context;

    public QuestionLibraryRepository(GeoDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuestionLibraryEntity>> GetAllAsync(string? projectId = null, bool enabledOnly = true)
    {
        var query = _context.Client.Queryable<QuestionLibraryEntity>();
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(q => q.ProjectId == projectId);
        }
        
        if (enabledOnly)
        {
            query = query.Where(q => q.IsEnabled);
        }
        
        return await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
    }

    public async Task<List<QuestionLibraryEntity>> SearchAsync(string keyword, string? projectId = null, int limit = 50)
    {
        var query = _context.Client.Queryable<QuestionLibraryEntity>()
            .Where(q => q.IsEnabled && q.Question.Contains(keyword));
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(q => q.ProjectId == projectId);
        }
        
        return await query
            .OrderByDescending(q => q.UsageCount)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<QuestionLibraryEntity>> GetByTypeAsync(string questionType, string? projectId = null)
    {
        var query = _context.Client.Queryable<QuestionLibraryEntity>()
            .Where(q => q.IsEnabled && q.QuestionType == questionType);
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(q => q.ProjectId == projectId);
        }
        
        return await query.OrderByDescending(q => q.UsageCount).ToListAsync();
    }

    public async Task<List<QuestionLibraryEntity>> GetFavoritesAsync(string? projectId = null)
    {
        var query = _context.Client.Queryable<QuestionLibraryEntity>()
            .Where(q => q.IsEnabled && q.IsFavorite);
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(q => q.ProjectId == projectId);
        }
        
        return await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
    }

    public async Task<List<QuestionLibraryEntity>> GetRecentlyUsedAsync(string? projectId = null, int limit = 20)
    {
        var query = _context.Client.Queryable<QuestionLibraryEntity>()
            .Where(q => q.IsEnabled && q.LastUsedAt != null);
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(q => q.ProjectId == projectId);
        }
        
        return await query
            .OrderByDescending(q => q.LastUsedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<QuestionLibraryEntity?> GetByIdAsync(int id)
    {
        return await _context.Client.Queryable<QuestionLibraryEntity>()
            .Where(q => q.Id == id)
            .FirstAsync();
    }

    public async Task<int> CreateAsync(QuestionLibraryEntity question)
    {
        question.CreatedAt = DateTime.UtcNow;
        return await _context.Client.Insertable(question).ExecuteReturnIdentityAsync();
    }

    public async Task CreateBatchAsync(List<QuestionLibraryEntity> questions)
    {
        foreach (var q in questions)
        {
            q.CreatedAt = DateTime.UtcNow;
        }
        await _context.Client.Insertable(questions).ExecuteCommandAsync();
    }

    public async Task UpdateAsync(QuestionLibraryEntity question)
    {
        await _context.Client.Updateable(question).ExecuteCommandAsync();
    }

    public async Task IncrementUsageAsync(int id)
    {
        await _context.Client.Updateable<QuestionLibraryEntity>()
            .SetColumns(q => new QuestionLibraryEntity 
            { 
                UsageCount = q.UsageCount + 1,
                LastUsedAt = DateTime.UtcNow
            })
            .Where(q => q.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task ToggleFavoriteAsync(int id)
    {
        var question = await GetByIdAsync(id);
        if (question != null)
        {
            question.IsFavorite = !question.IsFavorite;
            await UpdateAsync(question);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await _context.Client.Deleteable<QuestionLibraryEntity>()
            .Where(q => q.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task<bool> ExistsAsync(string question, string? projectId = null)
    {
        var query = _context.Client.Queryable<QuestionLibraryEntity>()
            .Where(q => q.Question == question);
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(q => q.ProjectId == projectId);
        }
        
        return await query.AnyAsync();
    }
}
