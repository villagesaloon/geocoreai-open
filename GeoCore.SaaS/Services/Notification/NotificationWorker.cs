using GeoCore.Data.Repositories;
using System.Text.Json;

namespace GeoCore.SaaS.Services.Notification;

/// <summary>
/// 通知 Worker 后台服务
/// 从数据库队列中获取待发送的通知任务并处理
/// </summary>
public class NotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private readonly int _batchSize = 10;
    private readonly int _maxRetries = 3;

    public NotificationWorker(
        IServiceProvider serviceProvider,
        ILogger<NotificationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationWorker 启动");

        // 初始化默认邮件模板
        await InitializeTemplatesAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NotificationWorker 处理异常");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("NotificationWorker 停止");
    }

    private async Task InitializeTemplatesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
            await templateService.InitializeDefaultTemplatesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化邮件模板失败");
        }
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailSendService>();

        var pendingTasks = await repository.GetPendingTasksAsync(_batchSize);

        if (pendingTasks.Count == 0)
        {
            return;
        }

        _logger.LogDebug("处理 {Count} 个待发送通知", pendingTasks.Count);

        foreach (var task in pendingTasks)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                // 更新状态为处理中
                await repository.UpdateTaskStatusAsync(task.Id, "processing");

                // 解析变量
                var variables = string.IsNullOrEmpty(task.Variables)
                    ? new Dictionary<string, object>()
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(task.Variables)
                      ?? new Dictionary<string, object>();

                // 发送邮件
                var result = await emailService.SendTemplateEmailAsync(
                    task.RecipientEmail,
                    task.TemplateCode,
                    variables);

                if (result.Success)
                {
                    await repository.UpdateTaskStatusAsync(task.Id, "sent", result.ResendId);
                    _logger.LogInformation("通知发送成功: {TaskId} -> {Email}", task.Id, task.RecipientEmail);
                }
                else
                {
                    // 检查是否需要重试
                    if (task.RetryCount + 1 >= _maxRetries)
                    {
                        await repository.UpdateTaskStatusAsync(task.Id, "failed", null, result.ErrorMessage);
                        _logger.LogWarning("通知发送失败（已达最大重试次数）: {TaskId}, 错误: {Error}",
                            task.Id, result.ErrorMessage);
                    }
                    else
                    {
                        // 重置为 pending 状态，等待下次重试
                        await repository.UpdateTaskStatusAsync(task.Id, "pending", null, result.ErrorMessage);
                        _logger.LogWarning("通知发送失败（将重试）: {TaskId}, 重试次数: {Retry}, 错误: {Error}",
                            task.Id, task.RetryCount + 1, result.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理通知任务异常: {TaskId}", task.Id);
                await repository.UpdateTaskStatusAsync(task.Id, "pending", null, ex.Message);
            }
        }
    }
}
