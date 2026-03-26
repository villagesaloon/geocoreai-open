using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 引用监测数据仓储接口
/// </summary>
public interface ICitationMonitoringRepository
{
    Task<CitationMonitoringTaskEntity?> GetTaskAsync(int taskId, CancellationToken cancellationToken = default);
    Task<List<CitationResultEntity>> GetRecentResultsAsync(int taskId, int limit, CancellationToken cancellationToken = default);
    Task<CitationMonitoringTaskEntity?> GetTaskByIdAsync(int taskId);
    Task<List<CitationMonitoringTaskEntity>> GetTasksByProjectIdAsync(string projectId);
    Task<List<CitationMonitoringTaskEntity>> GetPendingTasksAsync();
    Task<int> CreateTaskAsync(CitationMonitoringTaskEntity task);
    Task UpdateTaskAsync(CitationMonitoringTaskEntity task);
    Task DeleteTaskAsync(int taskId);
    Task<CitationMonitoringRunEntity?> GetRunByIdAsync(int runId);
    Task<List<CitationMonitoringRunEntity>> GetRunsByTaskIdAsync(int taskId, int limit = 10);
    Task<CitationMonitoringRunEntity?> GetLatestRunByTaskIdAsync(int taskId);
    Task<int> CreateRunAsync(CitationMonitoringRunEntity run);
    Task UpdateRunAsync(CitationMonitoringRunEntity run);
    Task<List<CitationResultEntity>> GetResultsByRunIdAsync(int runId);
    Task<List<CitationResultEntity>> GetResultsByTaskIdAsync(int taskId, int limit = 100);
    Task CreateResultAsync(CitationResultEntity result);
    Task CreateResultsAsync(List<CitationResultEntity> results);
    Task<List<CitationDailyMetricsEntity>> GetDailyMetricsAsync(int taskId, DateTime startDate, DateTime endDate, string? platform = null);
    Task<CitationDailyMetricsEntity?> GetLatestDailyMetricsAsync(int taskId, string platform = "all");
    Task UpsertDailyMetricsAsync(CitationDailyMetricsEntity metrics);
    Task<(int totalQueries, int citedCount)> GetCitationStatsAsync(int runId);
    Task<Dictionary<string, (int total, int cited)>> GetPlatformStatsAsync(int runId);
}

/// <summary>
/// 引用监测数据仓储
/// </summary>
public class CitationMonitoringRepository : ICitationMonitoringRepository
{
    private readonly GeoDbContext _context;

    public CitationMonitoringRepository(GeoDbContext context)
    {
        _context = context;
    }

    #region Task 操作

    public async Task<CitationMonitoringTaskEntity?> GetTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        return await _context.Client.Queryable<CitationMonitoringTaskEntity>()
            .Where(x => x.Id == taskId)
            .FirstAsync();
    }

    public async Task<CitationMonitoringTaskEntity?> GetTaskByIdAsync(int taskId)
    {
        return await _context.Client.Queryable<CitationMonitoringTaskEntity>()
            .Where(x => x.Id == taskId)
            .FirstAsync();
    }

    public async Task<List<CitationMonitoringTaskEntity>> GetTasksByProjectIdAsync(string projectId)
    {
        return await _context.Client.Queryable<CitationMonitoringTaskEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<CitationMonitoringTaskEntity>> GetPendingTasksAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Client.Queryable<CitationMonitoringTaskEntity>()
            .Where(x => x.Status == "pending" || 
                       (x.NextRunAt != null && x.NextRunAt <= now))
            .ToListAsync();
    }

    public async Task<int> CreateTaskAsync(CitationMonitoringTaskEntity task)
    {
        return await _context.Client.Insertable(task).ExecuteReturnIdentityAsync();
    }

    public async Task UpdateTaskAsync(CitationMonitoringTaskEntity task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        await _context.Client.Updateable(task).ExecuteCommandAsync();
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        await _context.Client.Deleteable<CitationMonitoringTaskEntity>()
            .Where(x => x.Id == taskId)
            .ExecuteCommandAsync();
    }

    #endregion

    #region Run 操作

    public async Task<CitationMonitoringRunEntity?> GetRunByIdAsync(int runId)
    {
        return await _context.Client.Queryable<CitationMonitoringRunEntity>()
            .Where(x => x.Id == runId)
            .FirstAsync();
    }

    public async Task<List<CitationMonitoringRunEntity>> GetRunsByTaskIdAsync(int taskId, int limit = 10)
    {
        return await _context.Client.Queryable<CitationMonitoringRunEntity>()
            .Where(x => x.TaskId == taskId)
            .OrderByDescending(x => x.StartedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<CitationMonitoringRunEntity?> GetLatestRunByTaskIdAsync(int taskId)
    {
        return await _context.Client.Queryable<CitationMonitoringRunEntity>()
            .Where(x => x.TaskId == taskId)
            .OrderByDescending(x => x.StartedAt)
            .FirstAsync();
    }

    public async Task<int> CreateRunAsync(CitationMonitoringRunEntity run)
    {
        return await _context.Client.Insertable(run).ExecuteReturnIdentityAsync();
    }

    public async Task UpdateRunAsync(CitationMonitoringRunEntity run)
    {
        await _context.Client.Updateable(run).ExecuteCommandAsync();
    }

    #endregion

    #region Result 操作

    public async Task<List<CitationResultEntity>> GetResultsByRunIdAsync(int runId)
    {
        return await _context.Client.Queryable<CitationResultEntity>()
            .Where(x => x.RunId == runId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<CitationResultEntity>> GetResultsByTaskIdAsync(int taskId, int limit = 100)
    {
        return await _context.Client.Queryable<CitationResultEntity>()
            .Where(x => x.TaskId == taskId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<CitationResultEntity>> GetRecentResultsAsync(int taskId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.Client.Queryable<CitationResultEntity>()
            .Where(x => x.TaskId == taskId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task CreateResultAsync(CitationResultEntity result)
    {
        await _context.Client.Insertable(result).ExecuteCommandAsync();
    }

    public async Task CreateResultsAsync(List<CitationResultEntity> results)
    {
        await _context.Client.Insertable(results).ExecuteCommandAsync();
    }

    #endregion

    #region Daily Metrics 操作

    public async Task<List<CitationDailyMetricsEntity>> GetDailyMetricsAsync(
        int taskId, 
        DateTime startDate, 
        DateTime endDate,
        string? platform = null)
    {
        var query = _context.Client.Queryable<CitationDailyMetricsEntity>()
            .Where(x => x.TaskId == taskId)
            .Where(x => x.MetricDate >= startDate && x.MetricDate <= endDate);
        
        if (!string.IsNullOrEmpty(platform))
        {
            query = query.Where(x => x.Platform == platform);
        }
        
        return await query.OrderBy(x => x.MetricDate).ToListAsync();
    }

    public async Task<CitationDailyMetricsEntity?> GetLatestDailyMetricsAsync(int taskId, string platform = "all")
    {
        return await _context.Client.Queryable<CitationDailyMetricsEntity>()
            .Where(x => x.TaskId == taskId && x.Platform == platform)
            .OrderByDescending(x => x.MetricDate)
            .FirstAsync();
    }

    public async Task UpsertDailyMetricsAsync(CitationDailyMetricsEntity metrics)
    {
        var existing = await _context.Client.Queryable<CitationDailyMetricsEntity>()
            .Where(x => x.TaskId == metrics.TaskId && 
                       x.MetricDate == metrics.MetricDate && 
                       x.Platform == metrics.Platform)
            .FirstAsync();

        if (existing != null)
        {
            metrics.Id = existing.Id;
            await _context.Client.Updateable(metrics).ExecuteCommandAsync();
        }
        else
        {
            await _context.Client.Insertable(metrics).ExecuteCommandAsync();
        }
    }

    #endregion

    #region 统计查询

    public async Task<(int totalQueries, int citedCount)> GetCitationStatsAsync(int runId)
    {
        var results = await _context.Client.Queryable<CitationResultEntity>()
            .Where(x => x.RunId == runId)
            .ToListAsync();
        
        return (results.Count, results.Count(x => x.IsCited));
    }

    public async Task<Dictionary<string, (int total, int cited)>> GetPlatformStatsAsync(int runId)
    {
        var results = await _context.Client.Queryable<CitationResultEntity>()
            .Where(x => x.RunId == runId)
            .ToListAsync();
        
        return results
            .GroupBy(x => x.Platform)
            .ToDictionary(
                g => g.Key,
                g => (g.Count(), g.Count(x => x.IsCited)));
    }

    #endregion
}
