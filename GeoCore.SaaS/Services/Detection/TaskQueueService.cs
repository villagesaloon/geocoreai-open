using System.Text.Json;
using StackExchange.Redis;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// 任务队列服务接口
/// </summary>
public interface ITaskQueueService
{
    Task<long> EnqueueDetectionTaskAsync(long taskId, string taskType = "full");
    Task<long?> DequeueDetectionTaskAsync();
    Task<long> GetQueueLengthAsync(string queueName);
    Task<bool> IsTaskInQueueAsync(long taskId);
    Task<bool> RemoveFromQueueAsync(long taskId);
}

/// <summary>
/// 任务队列服务实现
/// 基于 Redis List 实现 FIFO 队列
/// </summary>
public class TaskQueueService : ITaskQueueService
{
    private readonly RedisConnectionService _redis;
    private readonly ILogger<TaskQueueService> _logger;

    public TaskQueueService(RedisConnectionService redis, ILogger<TaskQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// 将检测任务加入队列
    /// </summary>
    public async Task<long> EnqueueDetectionTaskAsync(long taskId, string taskType = "full")
    {
        var db = _redis.GetDatabase();
        
        var taskData = JsonSerializer.Serialize(new
        {
            TaskId = taskId,
            TaskType = taskType,
            EnqueuedAt = DateTime.UtcNow
        });

        var position = await db.ListRightPushAsync(RedisKeys.DetectionQueue, taskData);
        
        _logger.LogInformation("[Queue] Task {TaskId} enqueued, position: {Position}", taskId, position);
        
        return position;
    }

    /// <summary>
    /// 从队列中取出一个检测任务
    /// </summary>
    public async Task<long?> DequeueDetectionTaskAsync()
    {
        var db = _redis.GetDatabase();
        
        var taskData = await db.ListLeftPopAsync(RedisKeys.DetectionQueue);
        
        if (taskData.IsNullOrEmpty)
            return null;

        try
        {
            var task = JsonSerializer.Deserialize<QueuedTask>(taskData!);
            if (task != null)
            {
                _logger.LogInformation("[Queue] Task {TaskId} dequeued", task.TaskId);
                return task.TaskId;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "[Queue] Failed to deserialize task data: {Data}", taskData);
        }

        return null;
    }

    /// <summary>
    /// 获取队列长度
    /// </summary>
    public async Task<long> GetQueueLengthAsync(string queueName)
    {
        var db = _redis.GetDatabase();
        return await db.ListLengthAsync(queueName);
    }

    /// <summary>
    /// 检查任务是否在队列中
    /// </summary>
    public async Task<bool> IsTaskInQueueAsync(long taskId)
    {
        var db = _redis.GetDatabase();
        var items = await db.ListRangeAsync(RedisKeys.DetectionQueue);
        
        foreach (var item in items)
        {
            try
            {
                var task = JsonSerializer.Deserialize<QueuedTask>(item!);
                if (task?.TaskId == taskId)
                    return true;
            }
            catch
            {
                // 忽略解析错误
            }
        }
        
        return false;
    }

    /// <summary>
    /// 从队列中移除任务（取消任务时使用）
    /// </summary>
    public async Task<bool> RemoveFromQueueAsync(long taskId)
    {
        var db = _redis.GetDatabase();
        var items = await db.ListRangeAsync(RedisKeys.DetectionQueue);
        
        foreach (var item in items)
        {
            try
            {
                var task = JsonSerializer.Deserialize<QueuedTask>(item!);
                if (task?.TaskId == taskId)
                {
                    var removed = await db.ListRemoveAsync(RedisKeys.DetectionQueue, item);
                    if (removed > 0)
                    {
                        _logger.LogInformation("[Queue] Task {TaskId} removed from queue", taskId);
                        return true;
                    }
                }
            }
            catch
            {
                // 忽略解析错误
            }
        }
        
        return false;
    }

    private class QueuedTask
    {
        public long TaskId { get; set; }
        public string TaskType { get; set; } = "full";
        public DateTime EnqueuedAt { get; set; }
    }
}
