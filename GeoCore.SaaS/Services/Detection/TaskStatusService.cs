using System.Text.Json;
using StackExchange.Redis;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// 任务状态服务接口
/// </summary>
public interface ITaskStatusService
{
    Task SetStatusAsync(long taskId, string status, string? message = null);
    Task<string?> GetStatusAsync(long taskId);
    Task SetProgressAsync(long taskId, int progress, string? phase = null, string? message = null);
    Task<TaskProgressInfo?> GetProgressAsync(long taskId);
    Task SetResultAsync(long taskId, object result, TimeSpan? expiry = null);
    Task<T?> GetResultAsync<T>(long taskId) where T : class;
    Task<bool> AcquireLockAsync(string lockKey, TimeSpan expiry);
    Task ReleaseLockAsync(string lockKey);
    Task<int> IncrementDailyLimitAsync(long userId, string limitType);
    Task<int> GetDailyLimitUsageAsync(long userId, string limitType);
}

/// <summary>
/// 任务进度信息
/// </summary>
public class TaskProgressInfo
{
    public int Progress { get; set; }
    public string? Phase { get; set; }
    public string? Message { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 任务状态服务实现
/// </summary>
public class TaskStatusService : ITaskStatusService
{
    private readonly RedisConnectionService _redis;
    private readonly ILogger<TaskStatusService> _logger;

    public TaskStatusService(RedisConnectionService redis, ILogger<TaskStatusService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// 设置任务状态
    /// </summary>
    public async Task SetStatusAsync(long taskId, string status, string? message = null)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TaskStatus(taskId);
        
        var data = JsonSerializer.Serialize(new
        {
            Status = status,
            Message = message,
            UpdatedAt = DateTime.UtcNow
        });

        await db.StringSetAsync(key, data, TimeSpan.FromHours(24));
        
        _logger.LogDebug("[TaskStatus] Task {TaskId} status set to {Status}", taskId, status);
    }

    /// <summary>
    /// 获取任务状态
    /// </summary>
    public async Task<string?> GetStatusAsync(long taskId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TaskStatus(taskId);
        
        var data = await db.StringGetAsync(key);
        if (data.IsNullOrEmpty)
            return null;

        try
        {
            var status = JsonSerializer.Deserialize<JsonElement>(data!);
            return status.GetProperty("Status").GetString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 设置任务进度
    /// </summary>
    public async Task SetProgressAsync(long taskId, int progress, string? phase = null, string? message = null)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TaskProgress(taskId);
        
        var data = JsonSerializer.Serialize(new TaskProgressInfo
        {
            Progress = progress,
            Phase = phase,
            Message = message,
            UpdatedAt = DateTime.UtcNow
        });

        await db.StringSetAsync(key, data, TimeSpan.FromHours(24));
        
        _logger.LogDebug("[TaskStatus] Task {TaskId} progress: {Progress}%, phase: {Phase}", taskId, progress, phase);
    }

    /// <summary>
    /// 获取任务进度
    /// </summary>
    public async Task<TaskProgressInfo?> GetProgressAsync(long taskId)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TaskProgress(taskId);
        
        var data = await db.StringGetAsync(key);
        if (data.IsNullOrEmpty)
            return null;

        try
        {
            return JsonSerializer.Deserialize<TaskProgressInfo>(data!);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 设置任务结果
    /// </summary>
    public async Task SetResultAsync(long taskId, object result, TimeSpan? expiry = null)
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TaskResult(taskId);
        
        var data = JsonSerializer.Serialize(result);
        await db.StringSetAsync(key, data, expiry ?? TimeSpan.FromDays(7));
        
        _logger.LogDebug("[TaskStatus] Task {TaskId} result saved", taskId);
    }

    /// <summary>
    /// 获取任务结果
    /// </summary>
    public async Task<T?> GetResultAsync<T>(long taskId) where T : class
    {
        var db = _redis.GetDatabase();
        var key = RedisKeys.TaskResult(taskId);
        
        var data = await db.StringGetAsync(key);
        if (data.IsNullOrEmpty)
            return null;

        try
        {
            return JsonSerializer.Deserialize<T>(data!);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取分布式锁
    /// </summary>
    public async Task<bool> AcquireLockAsync(string lockKey, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        var acquired = await db.StringSetAsync(lockKey, DateTime.UtcNow.ToString("O"), expiry, When.NotExists);
        
        if (acquired)
            _logger.LogDebug("[Lock] Acquired lock: {LockKey}", lockKey);
        
        return acquired;
    }

    /// <summary>
    /// 释放分布式锁
    /// </summary>
    public async Task ReleaseLockAsync(string lockKey)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(lockKey);
        
        _logger.LogDebug("[Lock] Released lock: {LockKey}", lockKey);
    }

    /// <summary>
    /// 增加每日限制计数
    /// </summary>
    public async Task<int> IncrementDailyLimitAsync(long userId, string limitType)
    {
        var db = _redis.GetDatabase();
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var key = limitType switch
        {
            "detection" => RedisKeys.DetectionDailyLimit(userId, date),
            "audit" => RedisKeys.AuditDailyLimit(userId, date),
            _ => throw new ArgumentException($"Unknown limit type: {limitType}")
        };

        var count = await db.StringIncrementAsync(key);
        
        // 设置过期时间为明天凌晨
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var expiry = tomorrow - DateTime.UtcNow;
        await db.KeyExpireAsync(key, expiry);
        
        return (int)count;
    }

    /// <summary>
    /// 获取每日限制使用量
    /// </summary>
    public async Task<int> GetDailyLimitUsageAsync(long userId, string limitType)
    {
        var db = _redis.GetDatabase();
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var key = limitType switch
        {
            "detection" => RedisKeys.DetectionDailyLimit(userId, date),
            "audit" => RedisKeys.AuditDailyLimit(userId, date),
            _ => throw new ArgumentException($"Unknown limit type: {limitType}")
        };

        var value = await db.StringGetAsync(key);
        if (value.IsNullOrEmpty)
            return 0;

        return int.TryParse(value, out var count) ? count : 0;
    }
}
