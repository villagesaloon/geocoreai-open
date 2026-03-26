using GeoCore.Data.Entities;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 检测任务 Repository 接口
/// </summary>
public interface IDetectionTaskRepository
{
    Task<GeoDetectionTaskEntity?> GetByIdAsync(long id);
    Task<GeoDetectionTaskEntity?> GetLatestByProjectAsync(long projectId);
    Task<List<GeoDetectionTaskEntity>> GetByProjectAsync(long projectId, int limit = 10);
    Task<List<GeoDetectionTaskEntity>> GetByUserAsync(long userId, int limit = 20);
    Task<List<GeoDetectionTaskEntity>> GetPendingTasksAsync(int limit = 100);
    Task<long> CreateAsync(GeoDetectionTaskEntity entity);
    Task<bool> UpdateAsync(GeoDetectionTaskEntity entity);
    Task<bool> UpdateStatusAsync(long id, string status, string? message = null, string? errorMessage = null);
    Task<bool> UpdateProgressAsync(long id, int progress, string? currentPhase = null, string? message = null);
}

/// <summary>
/// 检测任务 Repository 实现
/// </summary>
public class DetectionTaskRepository : IDetectionTaskRepository
{
    private readonly ISqlSugarClient _db;

    public DetectionTaskRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GeoDetectionTaskEntity?> GetByIdAsync(long id)
    {
        return await _db.Queryable<GeoDetectionTaskEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<GeoDetectionTaskEntity?> GetLatestByProjectAsync(long projectId)
    {
        return await _db.Queryable<GeoDetectionTaskEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
    }

    public async Task<List<GeoDetectionTaskEntity>> GetByProjectAsync(long projectId, int limit = 10)
    {
        return await _db.Queryable<GeoDetectionTaskEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<GeoDetectionTaskEntity>> GetByUserAsync(long userId, int limit = 20)
    {
        return await _db.Queryable<GeoDetectionTaskEntity>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<GeoDetectionTaskEntity>> GetPendingTasksAsync(int limit = 100)
    {
        return await _db.Queryable<GeoDetectionTaskEntity>()
            .Where(x => x.Status == "pending" || x.Status == "queued")
            .OrderBy(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<long> CreateAsync(GeoDetectionTaskEntity entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<bool> UpdateAsync(GeoDetectionTaskEntity entity)
    {
        return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
    }

    public async Task<bool> UpdateStatusAsync(long id, string status, string? message = null, string? errorMessage = null)
    {
        var updateDict = new Dictionary<string, object>
        {
            { "status", status }
        };

        if (message != null)
            updateDict["message"] = message;

        if (errorMessage != null)
            updateDict["error_message"] = errorMessage;

        if (status == "running")
            updateDict["started_at"] = DateTime.UtcNow;
        else if (status == "completed" || status == "failed" || status == "cancelled")
            updateDict["completed_at"] = DateTime.UtcNow;
        else if (status == "queued")
            updateDict["queued_at"] = DateTime.UtcNow;

        return await _db.Updateable<GeoDetectionTaskEntity>()
            .SetColumns(it => new GeoDetectionTaskEntity
            {
                Status = status,
                Message = message,
                ErrorMessage = errorMessage
            })
            .Where(it => it.Id == id)
            .ExecuteCommandHasChangeAsync();
    }

    public async Task<bool> UpdateProgressAsync(long id, int progress, string? currentPhase = null, string? message = null)
    {
        return await _db.Updateable<GeoDetectionTaskEntity>()
            .SetColumns(it => new GeoDetectionTaskEntity
            {
                Progress = progress,
                CurrentPhase = currentPhase,
                Message = message
            })
            .Where(it => it.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 网站审计 Repository 接口
/// </summary>
public interface IWebsiteAuditRepository
{
    Task<GeoWebsiteAuditEntity?> GetByIdAsync(long id);
    Task<GeoWebsiteAuditEntity?> GetLatestByProjectAsync(long projectId);
    Task<GeoWebsiteAuditEntity?> GetCachedByProjectAsync(long projectId);
    Task<List<GeoWebsiteAuditEntity>> GetByProjectAsync(long projectId, int limit = 10);
    Task<long> CreateAsync(GeoWebsiteAuditEntity entity);
    Task<bool> UpdateAsync(GeoWebsiteAuditEntity entity);
}

/// <summary>
/// 网站审计 Repository 实现
/// </summary>
public class WebsiteAuditRepository : IWebsiteAuditRepository
{
    private readonly ISqlSugarClient _db;

    public WebsiteAuditRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GeoWebsiteAuditEntity?> GetByIdAsync(long id)
    {
        return await _db.Queryable<GeoWebsiteAuditEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<GeoWebsiteAuditEntity?> GetLatestByProjectAsync(long projectId)
    {
        return await _db.Queryable<GeoWebsiteAuditEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
    }

    public async Task<GeoWebsiteAuditEntity?> GetCachedByProjectAsync(long projectId)
    {
        return await _db.Queryable<GeoWebsiteAuditEntity>()
            .Where(x => x.ProjectId == projectId)
            .Where(x => x.CacheExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
    }

    public async Task<List<GeoWebsiteAuditEntity>> GetByProjectAsync(long projectId, int limit = 10)
    {
        return await _db.Queryable<GeoWebsiteAuditEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<long> CreateAsync(GeoWebsiteAuditEntity entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<bool> UpdateAsync(GeoWebsiteAuditEntity entity)
    {
        return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 检测指标 Repository 接口
/// </summary>
public interface IDetectionMetricRepository
{
    Task<GeoDetectionMetricEntity?> GetByIdAsync(long id);
    Task<List<GeoDetectionMetricEntity>> GetByTaskAsync(long taskId);
    Task<GeoDetectionMetricEntity?> GetByTaskAndCountryAsync(long taskId, string countryCode);
    Task<List<GeoDetectionMetricEntity>> GetByProjectAsync(long projectId, int limit = 50);
    Task<long> CreateAsync(GeoDetectionMetricEntity entity);
    Task<bool> CreateBatchAsync(List<GeoDetectionMetricEntity> entities);
}

/// <summary>
/// 检测指标 Repository 实现
/// </summary>
public class DetectionMetricRepository : IDetectionMetricRepository
{
    private readonly ISqlSugarClient _db;

    public DetectionMetricRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GeoDetectionMetricEntity?> GetByIdAsync(long id)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<List<GeoDetectionMetricEntity>> GetByTaskAsync(long taskId)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.TaskId == taskId)
            .ToListAsync();
    }

    public async Task<GeoDetectionMetricEntity?> GetByTaskAndCountryAsync(long taskId, string countryCode)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.TaskId == taskId && x.CountryCode == countryCode)
            .FirstAsync();
    }

    public async Task<List<GeoDetectionMetricEntity>> GetByProjectAsync(long projectId, int limit = 50)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<long> CreateAsync(GeoDetectionMetricEntity entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<bool> CreateBatchAsync(List<GeoDetectionMetricEntity> entities)
    {
        return await _db.Insertable(entities).ExecuteCommandAsync() > 0;
    }
}

/// <summary>
/// 检测建议 Repository 接口
/// </summary>
public interface IDetectionSuggestionRepository
{
    Task<GeoDetectionSuggestionEntity?> GetByIdAsync(long id);
    Task<List<GeoDetectionSuggestionEntity>> GetByTaskAsync(long taskId);
    Task<List<GeoDetectionSuggestionEntity>> GetByProjectAsync(long projectId, string? category = null, string? status = null);
    Task<long> CreateAsync(GeoDetectionSuggestionEntity entity);
    Task<bool> CreateBatchAsync(List<GeoDetectionSuggestionEntity> entities);
    Task<bool> UpdateStatusAsync(long id, string status);
}

/// <summary>
/// 检测建议 Repository 实现
/// </summary>
public class DetectionSuggestionRepository : IDetectionSuggestionRepository
{
    private readonly ISqlSugarClient _db;

    public DetectionSuggestionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GeoDetectionSuggestionEntity?> GetByIdAsync(long id)
    {
        return await _db.Queryable<GeoDetectionSuggestionEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    public async Task<List<GeoDetectionSuggestionEntity>> GetByTaskAsync(long taskId)
    {
        return await _db.Queryable<GeoDetectionSuggestionEntity>()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.Priority)
            .ToListAsync();
    }

    public async Task<List<GeoDetectionSuggestionEntity>> GetByProjectAsync(long projectId, string? category = null, string? status = null)
    {
        var query = _db.Queryable<GeoDetectionSuggestionEntity>()
            .Where(x => x.ProjectId == projectId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(x => x.Category == category);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        return await query
            .OrderBy(x => x.Priority)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<long> CreateAsync(GeoDetectionSuggestionEntity entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<bool> CreateBatchAsync(List<GeoDetectionSuggestionEntity> entities)
    {
        return await _db.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> UpdateStatusAsync(long id, string status)
    {
        var completedAt = (status == "completed") ? DateTime.UtcNow : (DateTime?)null;

        return await _db.Updateable<GeoDetectionSuggestionEntity>()
            .SetColumns(it => new GeoDetectionSuggestionEntity
            {
                Status = status,
                CompletedAt = completedAt
            })
            .Where(it => it.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 通知设置 Repository 接口
/// </summary>
public interface INotificationSettingRepository
{
    Task<GeoNotificationSettingEntity?> GetByUserAsync(long userId);
    Task<long> CreateAsync(GeoNotificationSettingEntity entity);
    Task<bool> UpdateAsync(GeoNotificationSettingEntity entity);
    Task<bool> UpsertAsync(GeoNotificationSettingEntity entity);
}

/// <summary>
/// 通知设置 Repository 实现
/// </summary>
public class NotificationSettingRepository : INotificationSettingRepository
{
    private readonly ISqlSugarClient _db;

    public NotificationSettingRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<GeoNotificationSettingEntity?> GetByUserAsync(long userId)
    {
        return await _db.Queryable<GeoNotificationSettingEntity>()
            .Where(x => x.UserId == userId)
            .FirstAsync();
    }

    public async Task<long> CreateAsync(GeoNotificationSettingEntity entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<bool> UpdateAsync(GeoNotificationSettingEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
    }

    public async Task<bool> UpsertAsync(GeoNotificationSettingEntity entity)
    {
        var existing = await GetByUserAsync(entity.UserId);
        if (existing != null)
        {
            entity.Id = existing.Id;
            entity.UpdatedAt = DateTime.UtcNow;
            return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
        }
        else
        {
            return await _db.Insertable(entity).ExecuteCommandAsync() > 0;
        }
    }
}

/// <summary>
/// 检测指标 Repository
/// </summary>
public class GeoDetectionMetricRepository
{
    private readonly ISqlSugarClient _db;

    public GeoDetectionMetricRepository(GeoCore.Data.DbContext.GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    public async Task<long> CreateAsync(GeoDetectionMetricEntity entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<List<GeoDetectionMetricEntity>> GetByTaskIdAsync(long taskId)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.TaskId == taskId)
            .ToListAsync();
    }

    public async Task<List<GeoDetectionMetricEntity>> GetByProjectIdAsync(long projectId)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<GeoDetectionMetricEntity?> GetLatestByProjectAsync(long projectId)
    {
        return await _db.Queryable<GeoDetectionMetricEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstAsync();
    }
}
