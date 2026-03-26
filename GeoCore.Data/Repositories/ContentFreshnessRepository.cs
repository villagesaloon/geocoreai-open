using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 内容时效性仓储接口
/// </summary>
public interface IContentFreshnessRepository
{
    Task<List<ContentFreshnessEntity>> GetAllAsync(string projectId);
    Task<List<ContentFreshnessEntity>> GetExpiredAsync(string? projectId = null);
    Task<List<ContentFreshnessEntity>> GetUpcomingRefreshAsync(string? projectId = null, int daysAhead = 7);
    Task<ContentFreshnessEntity?> GetByIdAsync(int id);
    Task<ContentFreshnessEntity?> GetByIdentifierAsync(string projectId, string contentIdentifier);
    Task<int> CreateAsync(ContentFreshnessEntity content);
    Task UpdateAsync(ContentFreshnessEntity content);
    Task MarkRefreshedAsync(int id);
    Task MarkReminderSentAsync(int id);
    Task DeleteAsync(int id);
    Task<ContentFreshnessStats> GetStatsAsync(string projectId);
}

/// <summary>
/// 内容时效性统计
/// </summary>
public class ContentFreshnessStats
{
    public int TotalContent { get; set; }
    public int FreshContent { get; set; }
    public int ExpiredContent { get; set; }
    public int UpcomingRefresh { get; set; }
    public double FreshnessRate => TotalContent > 0 ? (double)FreshContent / TotalContent * 100 : 0;
}

/// <summary>
/// 内容时效性仓储实现
/// </summary>
public class ContentFreshnessRepository : IContentFreshnessRepository
{
    private readonly GeoDbContext _context;

    public ContentFreshnessRepository(GeoDbContext context)
    {
        _context = context;
    }

    public async Task<List<ContentFreshnessEntity>> GetAllAsync(string projectId)
    {
        return await _context.Client.Queryable<ContentFreshnessEntity>()
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.NextRefreshAt)
            .ToListAsync();
    }

    public async Task<List<ContentFreshnessEntity>> GetExpiredAsync(string? projectId = null)
    {
        var query = _context.Client.Queryable<ContentFreshnessEntity>()
            .Where(c => c.NextRefreshAt < DateTime.UtcNow);
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(c => c.ProjectId == projectId);
        }
        
        return await query.OrderBy(c => c.NextRefreshAt).ToListAsync();
    }

    public async Task<List<ContentFreshnessEntity>> GetUpcomingRefreshAsync(string? projectId = null, int daysAhead = 7)
    {
        var deadline = DateTime.UtcNow.AddDays(daysAhead);
        var query = _context.Client.Queryable<ContentFreshnessEntity>()
            .Where(c => c.NextRefreshAt >= DateTime.UtcNow && c.NextRefreshAt <= deadline);
        
        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(c => c.ProjectId == projectId);
        }
        
        return await query.OrderBy(c => c.NextRefreshAt).ToListAsync();
    }

    public async Task<ContentFreshnessEntity?> GetByIdAsync(int id)
    {
        return await _context.Client.Queryable<ContentFreshnessEntity>()
            .Where(c => c.Id == id)
            .FirstAsync();
    }

    public async Task<ContentFreshnessEntity?> GetByIdentifierAsync(string projectId, string contentIdentifier)
    {
        return await _context.Client.Queryable<ContentFreshnessEntity>()
            .Where(c => c.ProjectId == projectId && c.ContentIdentifier == contentIdentifier)
            .FirstAsync();
    }

    public async Task<int> CreateAsync(ContentFreshnessEntity content)
    {
        content.CreatedAt = DateTime.UtcNow;
        content.NextRefreshAt = content.LastUpdatedAt.AddDays(content.RefreshIntervalDays);
        return await _context.Client.Insertable(content).ExecuteReturnIdentityAsync();
    }

    public async Task UpdateAsync(ContentFreshnessEntity content)
    {
        await _context.Client.Updateable(content).ExecuteCommandAsync();
    }

    public async Task MarkRefreshedAsync(int id)
    {
        var content = await GetByIdAsync(id);
        if (content != null)
        {
            content.LastUpdatedAt = DateTime.UtcNow;
            content.NextRefreshAt = DateTime.UtcNow.AddDays(content.RefreshIntervalDays);
            content.ReminderSent = false;
            await UpdateAsync(content);
        }
    }

    public async Task MarkReminderSentAsync(int id)
    {
        await _context.Client.Updateable<ContentFreshnessEntity>()
            .SetColumns(c => new ContentFreshnessEntity { ReminderSent = true })
            .Where(c => c.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await _context.Client.Deleteable<ContentFreshnessEntity>()
            .Where(c => c.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task<ContentFreshnessStats> GetStatsAsync(string projectId)
    {
        var all = await GetAllAsync(projectId);
        var now = DateTime.UtcNow;
        var weekAhead = now.AddDays(7);

        return new ContentFreshnessStats
        {
            TotalContent = all.Count,
            FreshContent = all.Count(c => c.NextRefreshAt > now),
            ExpiredContent = all.Count(c => c.NextRefreshAt <= now),
            UpcomingRefresh = all.Count(c => c.NextRefreshAt > now && c.NextRefreshAt <= weekAhead)
        };
    }
}
