using GeoCore.Data.Entities;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 定时任务仓储接口
/// </summary>
public interface ISchedulerRepository
{
    Task<List<ScheduledJobEntity>> GetEnabledJobsAsync();
    Task<ScheduledJobEntity?> GetJobByCodeAsync(string jobCode);
    Task<ScheduledJobEntity?> GetJobByIdAsync(long jobId);
    Task UpdateJobAsync(ScheduledJobEntity job);
    Task<ScheduledJobEntity> CreateJobAsync(ScheduledJobEntity job);
    Task<ScheduledJobLogEntity> CreateLogAsync(ScheduledJobLogEntity log);
    Task UpdateLogAsync(ScheduledJobLogEntity log);
    Task<List<ScheduledJobLogEntity>> GetRecentLogsAsync(long jobId, int limit = 10);
}

/// <summary>
/// 定时任务仓储实现
/// </summary>
public class SchedulerRepository : ISchedulerRepository
{
    private readonly ISqlSugarClient _db;

    public SchedulerRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<List<ScheduledJobEntity>> GetEnabledJobsAsync()
    {
        return await _db.Queryable<ScheduledJobEntity>()
            .Where(j => j.IsEnabled)
            .ToListAsync();
    }

    public async Task<ScheduledJobEntity?> GetJobByCodeAsync(string jobCode)
    {
        return await _db.Queryable<ScheduledJobEntity>()
            .Where(j => j.JobCode == jobCode)
            .FirstAsync();
    }

    public async Task<ScheduledJobEntity?> GetJobByIdAsync(long jobId)
    {
        return await _db.Queryable<ScheduledJobEntity>()
            .Where(j => j.Id == jobId)
            .FirstAsync();
    }

    public async Task UpdateJobAsync(ScheduledJobEntity job)
    {
        job.UpdatedAt = DateTime.UtcNow;
        await _db.Updateable(job).ExecuteCommandAsync();
    }

    public async Task<ScheduledJobEntity> CreateJobAsync(ScheduledJobEntity job)
    {
        job.CreatedAt = DateTime.UtcNow;
        job.Id = await _db.Insertable(job).ExecuteReturnIdentityAsync();
        return job;
    }

    public async Task<ScheduledJobLogEntity> CreateLogAsync(ScheduledJobLogEntity log)
    {
        log.Id = await _db.Insertable(log).ExecuteReturnIdentityAsync();
        return log;
    }

    public async Task UpdateLogAsync(ScheduledJobLogEntity log)
    {
        await _db.Updateable(log).ExecuteCommandAsync();
    }

    public async Task<List<ScheduledJobLogEntity>> GetRecentLogsAsync(long jobId, int limit = 10)
    {
        return await _db.Queryable<ScheduledJobLogEntity>()
            .Where(l => l.JobId == jobId)
            .OrderByDescending(l => l.StartedAt)
            .Take(limit)
            .ToListAsync();
    }
}

/// <summary>
/// 定时任务初始化器
/// </summary>
public class SchedulerInitializer
{
    private readonly GeoCore.Data.DbContext.GeoDbContext _db;

    public SchedulerInitializer(GeoCore.Data.DbContext.GeoDbContext db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        var repo = new SchedulerRepository(_db.Client);
        
        // 检查是否已初始化
        var existing = await repo.GetJobByCodeAsync("monitoring_scheduler");
        if (existing != null)
        {
            return;
        }

        // 创建默认定时任务
        var defaultJobs = new List<ScheduledJobEntity>
        {
            new()
            {
                JobCode = "monitoring_scheduler",
                Name = "引用监测调度",
                Description = "根据监测任务的频率设置，调度待执行的引用监测任务",
                CronExpression = "0 * * * *", // 每小时执行
                IsEnabled = true
            },
            new()
            {
                JobCode = "data_cleanup",
                Name = "数据清理",
                Description = "清理过期的邮件日志、通知任务、引用检测结果等",
                CronExpression = "0 3 * * *", // 每天凌晨 3 点执行
                IsEnabled = true
            },
            new()
            {
                JobCode = "daily_metrics",
                Name = "每日指标汇总",
                Description = "汇总每日引用监测指标，用于趋势分析",
                CronExpression = "0 1 * * *", // 每天凌晨 1 点执行
                IsEnabled = true
            }
        };

        foreach (var job in defaultJobs)
        {
            await repo.CreateJobAsync(job);
        }
    }
}
