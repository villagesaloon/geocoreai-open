using SqlSugar;

namespace GeoCore.Data.Entities;

/// <summary>
/// 定时任务配置实体
/// 通过 Admin 后台管理
/// </summary>
[SugarTable("sys_scheduled_jobs")]
public class ScheduledJobEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 任务代码（唯一标识）
    /// monitoring_scheduler, data_cleanup, daily_report
    /// </summary>
    [SugarColumn(ColumnName = "job_code", Length = 50, IsNullable = false)]
    public string JobCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// Cron 表达式
    /// 例如: "0 0 * * *" 每天 0 点执行
    /// </summary>
    [SugarColumn(ColumnName = "cron_expression", Length = 100, IsNullable = false)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false)]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 上次执行时间
    /// </summary>
    [SugarColumn(ColumnName = "last_run_at", IsNullable = true)]
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    [SugarColumn(ColumnName = "next_run_at", IsNullable = true)]
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// 上次执行状态：success, failed
    /// </summary>
    [SugarColumn(ColumnName = "last_status", Length = 20, IsNullable = true)]
    public string? LastStatus { get; set; }

    /// <summary>
    /// 上次执行耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "last_duration_ms", IsNullable = true)]
    public int? LastDurationMs { get; set; }

    /// <summary>
    /// 上次执行错误信息
    /// </summary>
    [SugarColumn(ColumnName = "last_error", ColumnDataType = "text", IsNullable = true)]
    public string? LastError { get; set; }

    /// <summary>
    /// 任务参数（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "parameters", ColumnDataType = "json", IsNullable = true)]
    public string? Parameters { get; set; }

    [SugarColumn(ColumnName = "created_at", IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 定时任务执行日志
/// </summary>
[SugarTable("sys_scheduled_job_logs")]
public class ScheduledJobLogEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 任务 ID
    /// </summary>
    [SugarColumn(ColumnName = "job_id", IsNullable = false)]
    public long JobId { get; set; }

    /// <summary>
    /// 任务代码
    /// </summary>
    [SugarColumn(ColumnName = "job_code", Length = 50, IsNullable = false)]
    public string JobCode { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnName = "started_at", IsNullable = false)]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnName = "completed_at", IsNullable = true)]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 执行状态：running, success, failed
    /// </summary>
    [SugarColumn(ColumnName = "status", Length = 20, IsNullable = false)]
    public string Status { get; set; } = "running";

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "duration_ms", IsNullable = true)]
    public int? DurationMs { get; set; }

    /// <summary>
    /// 执行结果摘要
    /// </summary>
    [SugarColumn(ColumnName = "result_summary", Length = 500, IsNullable = true)]
    public string? ResultSummary { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnName = "error_message", ColumnDataType = "text", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细日志（JSON）
    /// </summary>
    [SugarColumn(ColumnName = "details", ColumnDataType = "json", IsNullable = true)]
    public string? Details { get; set; }
}
