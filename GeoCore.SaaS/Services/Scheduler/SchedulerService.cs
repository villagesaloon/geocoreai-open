using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Diagnostics;

namespace GeoCore.SaaS.Services.Scheduler;

/// <summary>
/// 定时任务调度服务
/// 基于数据库配置的定时任务，支持 Admin 后台管理
/// </summary>
public class SchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SchedulerService> _logger;
    
    // 调度间隔（每分钟检查一次）
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public SchedulerService(
        IServiceProvider serviceProvider,
        ILogger<SchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SchedulerService 启动");

        // 初始化定时任务配置
        await InitializeJobsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExecuteJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SchedulerService 执行异常");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("SchedulerService 停止");
    }

    private async Task InitializeJobsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GeoCore.Data.DbContext.GeoDbContext>();
            var initializer = new SchedulerInitializer(db);
            await initializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化定时任务配置失败");
        }
    }

    private async Task CheckAndExecuteJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var schedulerRepo = scope.ServiceProvider.GetRequiredService<ISchedulerRepository>();

        var jobs = await schedulerRepo.GetEnabledJobsAsync();
        var now = DateTime.UtcNow;

        foreach (var job in jobs)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            // 检查是否需要执行
            if (ShouldExecute(job, now))
            {
                await ExecuteJobAsync(job, stoppingToken);
            }
        }
    }

    private bool ShouldExecute(ScheduledJobEntity job, DateTime now)
    {
        // 如果没有设置下次执行时间，根据 Cron 表达式计算
        if (job.NextRunAt == null)
        {
            return true; // 首次执行
        }

        return job.NextRunAt <= now;
    }

    private async Task ExecuteJobAsync(ScheduledJobEntity job, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var schedulerRepo = scope.ServiceProvider.GetRequiredService<ISchedulerRepository>();

        // 创建执行日志
        var log = new ScheduledJobLogEntity
        {
            JobId = job.Id,
            JobCode = job.JobCode,
            StartedAt = DateTime.UtcNow,
            Status = "running"
        };
        await schedulerRepo.CreateLogAsync(log);

        var sw = Stopwatch.StartNew();
        string? error = null;
        string? summary = null;

        try
        {
            _logger.LogInformation("执行定时任务: {JobCode} - {JobName}", job.JobCode, job.Name);

            // 根据任务代码执行对应逻辑
            summary = job.JobCode switch
            {
                "monitoring_scheduler" => await ExecuteMonitoringSchedulerAsync(scope, stoppingToken),
                "data_cleanup" => await ExecuteDataCleanupAsync(scope, stoppingToken),
                "daily_metrics" => await ExecuteDailyMetricsAsync(scope, stoppingToken),
                _ => $"未知任务类型: {job.JobCode}"
            };

            log.Status = "success";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时任务执行失败: {JobCode}", job.JobCode);
            error = ex.Message;
            log.Status = "failed";
            log.ErrorMessage = ex.ToString();
        }
        finally
        {
            sw.Stop();
            log.CompletedAt = DateTime.UtcNow;
            log.DurationMs = (int)sw.ElapsedMilliseconds;
            log.ResultSummary = summary;
            await schedulerRepo.UpdateLogAsync(log);

            // 更新任务状态
            job.LastRunAt = DateTime.UtcNow;
            job.LastStatus = log.Status;
            job.LastDurationMs = log.DurationMs;
            job.LastError = error;
            job.NextRunAt = CalculateNextRunTime(job.CronExpression);
            await schedulerRepo.UpdateJobAsync(job);

            _logger.LogInformation("定时任务完成: {JobCode}, 耗时: {Duration}ms, 状态: {Status}",
                job.JobCode, sw.ElapsedMilliseconds, log.Status);
        }
    }

    private DateTime CalculateNextRunTime(string cronExpression)
    {
        // 简化的 Cron 解析（仅支持常用格式）
        // 格式: 分 时 日 月 周
        var parts = cronExpression.Split(' ');
        if (parts.Length < 5) return DateTime.UtcNow.AddHours(1);

        var now = DateTime.UtcNow;
        
        // 解析小时
        if (parts[1] == "*")
        {
            // 每小时执行
            return now.AddHours(1);
        }
        else if (int.TryParse(parts[1], out int hour))
        {
            // 每天固定时间执行
            var next = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0, DateTimeKind.Utc);
            if (next <= now)
            {
                next = next.AddDays(1);
            }
            return next;
        }

        return now.AddHours(1);
    }

    #region 具体任务执行方法

    /// <summary>
    /// 执行监测调度任务
    /// </summary>
    private async Task<string> ExecuteMonitoringSchedulerAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        var monitoringRepo = scope.ServiceProvider.GetRequiredService<ICitationMonitoringRepository>();
        var pendingTasks = await monitoringRepo.GetPendingTasksAsync();
        var scheduledCount = 0;

        foreach (var task in pendingTasks)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            if (task.NextRunAt == null || task.NextRunAt <= DateTime.UtcNow)
            {
                _logger.LogInformation("调度监测任务: {TaskId}, 品牌: {BrandName}", task.Id, task.BrandName);

                task.Status = "running";
                task.LastRunAt = DateTime.UtcNow;
                task.NextRunAt = CalculateFrequencyNextRunTime(task.Frequency);
                await monitoringRepo.UpdateTaskAsync(task);
                scheduledCount++;
            }
        }

        return $"调度了 {scheduledCount} 个监测任务";
    }

    /// <summary>
    /// 执行数据清理任务
    /// </summary>
    private async Task<string> ExecuteDataCleanupAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<SqlSugar.ISqlSugarClient>();
        var totalDeleted = 0;

        // 清理 30 天前的邮件发送日志
        var emailLogCutoff = DateTime.UtcNow.AddDays(-30);
        var emailLogsDeleted = await db.Deleteable<GeoEmailSendLogEntity>()
            .Where(x => x.CreatedAt < emailLogCutoff)
            .ExecuteCommandAsync();
        totalDeleted += emailLogsDeleted;

        // 清理 7 天前已完成/失败的通知任务
        var notificationCutoff = DateTime.UtcNow.AddDays(-7);
        var notificationsDeleted = await db.Deleteable<GeoNotificationTaskEntity>()
            .Where(x => x.CreatedAt < notificationCutoff && 
                       (x.Status == "sent" || x.Status == "failed"))
            .ExecuteCommandAsync();
        totalDeleted += notificationsDeleted;

        // 清理 90 天前的引用检测结果
        var citationCutoff = DateTime.UtcNow.AddDays(-90);
        var citationsDeleted = await db.Deleteable<CitationResultEntity>()
            .Where(x => x.CreatedAt < citationCutoff)
            .ExecuteCommandAsync();
        totalDeleted += citationsDeleted;

        // 清理 30 天前的定时任务日志
        var jobLogCutoff = DateTime.UtcNow.AddDays(-30);
        var jobLogsDeleted = await db.Deleteable<ScheduledJobLogEntity>()
            .Where(x => x.StartedAt < jobLogCutoff)
            .ExecuteCommandAsync();
        totalDeleted += jobLogsDeleted;

        return $"清理了 {totalDeleted} 条过期数据（邮件日志: {emailLogsDeleted}, 通知: {notificationsDeleted}, 引用结果: {citationsDeleted}, 任务日志: {jobLogsDeleted}）";
    }

    /// <summary>
    /// 执行每日指标汇总任务
    /// </summary>
    private async Task<string> ExecuteDailyMetricsAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        var db = scope.ServiceProvider.GetRequiredService<SqlSugar.ISqlSugarClient>();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);

        // 获取所有活跃的监测任务
        var tasks = await db.Queryable<CitationMonitoringTaskEntity>()
            .Where(t => t.Status != "deleted")
            .ToListAsync();

        var metricsCreated = 0;

        foreach (var task in tasks)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            // 检查是否已有该日期的汇总
            var existingMetric = await db.Queryable<CitationDailyMetricsEntity>()
                .Where(m => m.TaskId == task.Id && m.MetricDate == yesterday && m.Platform == "all")
                .FirstAsync();

            if (existingMetric != null)
                continue;

            // 获取昨天的检测结果
            var results = await db.Queryable<CitationResultEntity>()
                .Where(r => r.TaskId == task.Id && r.CreatedAt >= yesterday && r.CreatedAt < yesterday.AddDays(1))
                .ToListAsync();

            if (results.Count == 0)
                continue;

            // 计算汇总指标
            var metric = new CitationDailyMetricsEntity
            {
                TaskId = task.Id,
                MetricDate = yesterday,
                Platform = "all",
                TotalQueries = results.Count,
                CitedCount = results.Count(r => r.IsCited),
                CitationFrequency = results.Count > 0 ? (double)results.Count(r => r.IsCited) / results.Count : 0,
                BrandVisibilityScore = results.Count > 0 ? results.Average(r => r.VisibilityScore) : 0,
                PositiveSentimentRatio = results.Count > 0 ? (double)results.Count(r => r.Sentiment == "positive") / results.Count : 0,
                LinkedCitationRatio = results.Count > 0 ? (double)results.Count(r => r.HasLink) / results.Count : 0,
                FirstPositionRatio = results.Count > 0 ? (double)results.Count(r => r.CitationPosition == "first") / results.Count : 0
            };

            await db.Insertable(metric).ExecuteCommandAsync();
            metricsCreated++;
        }

        return $"创建了 {metricsCreated} 个每日指标汇总";
    }

    #endregion

    /// <summary>
    /// 根据频率计算下次执行时间
    /// </summary>
    private DateTime CalculateFrequencyNextRunTime(string frequency)
    {
        var now = DateTime.UtcNow;
        return frequency.ToLower() switch
        {
            "daily" => now.AddDays(1),
            "weekly" => now.AddDays(7),
            "monthly" => now.AddMonths(1),
            _ => now.AddDays(7)
        };
    }
}
