using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Text.Json;

namespace GeoCore.SaaS.Services.Notification;

/// <summary>
/// 通知服务接口
/// 用于创建通知任务（异步发送）
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 创建通知任务（加入队列异步发送）
    /// </summary>
    Task<long> EnqueueNotificationAsync(long userId, string email, string templateCode, object variables);

    /// <summary>
    /// 发送检测完成通知
    /// </summary>
    Task SendDetectionCompletedAsync(long userId, string email, string userName, string projectName,
        int visibilityScore, decimal brandMentionRate, int websiteHealthScore, string resultUrl);

    /// <summary>
    /// 发送检测失败通知
    /// </summary>
    Task SendDetectionFailedAsync(long userId, string email, string userName, string projectName,
        string errorMessage, string retryUrl);

    /// <summary>
    /// 发送可见度警报
    /// </summary>
    Task SendVisibilityAlertAsync(long userId, string email, string userName, string projectName,
        int oldScore, int newScore, string resultUrl);

    /// <summary>
    /// 发送欢迎邮件
    /// </summary>
    Task SendWelcomeEmailAsync(long userId, string email, string userName, string dashboardUrl);

    /// <summary>
    /// 检查用户是否启用了指定类型的通知
    /// </summary>
    Task<bool> IsNotificationEnabledAsync(long userId, string notificationType);
}

/// <summary>
/// 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository repository,
        ILogger<NotificationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<long> EnqueueNotificationAsync(long userId, string email, string templateCode, object variables)
    {
        var task = new GeoNotificationTaskEntity
        {
            UserId = userId,
            TemplateCode = templateCode,
            RecipientEmail = email,
            Variables = JsonSerializer.Serialize(variables),
            Status = "pending"
        };

        var created = await _repository.CreateNotificationTaskAsync(task);
        _logger.LogInformation("通知任务已创建: {TaskId}, 模板: {TemplateCode}, 收件人: {Email}",
            created.Id, templateCode, email);

        return created.Id;
    }

    public async Task SendDetectionCompletedAsync(long userId, string email, string userName, string projectName,
        int visibilityScore, decimal brandMentionRate, int websiteHealthScore, string resultUrl)
    {
        // 检查用户是否启用了此通知
        if (!await IsNotificationEnabledAsync(userId, "detection_complete"))
        {
            _logger.LogDebug("用户 {UserId} 已禁用检测完成通知", userId);
            return;
        }

        await EnqueueNotificationAsync(userId, email, "detection_completed", new
        {
            user_name = userName,
            project_name = projectName,
            visibility_score = visibilityScore,
            brand_mention_rate = Math.Round(brandMentionRate * 100, 1),
            website_health_score = websiteHealthScore,
            result_url = resultUrl
        });
    }

    public async Task SendDetectionFailedAsync(long userId, string email, string userName, string projectName,
        string errorMessage, string retryUrl)
    {
        // 检查用户是否启用了此通知
        if (!await IsNotificationEnabledAsync(userId, "detection_failed"))
        {
            _logger.LogDebug("用户 {UserId} 已禁用检测失败通知", userId);
            return;
        }

        await EnqueueNotificationAsync(userId, email, "detection_failed", new
        {
            user_name = userName,
            project_name = projectName,
            error_message = errorMessage,
            retry_url = retryUrl
        });
    }

    public async Task SendVisibilityAlertAsync(long userId, string email, string userName, string projectName,
        int oldScore, int newScore, string resultUrl)
    {
        // 检查用户是否启用了此通知
        if (!await IsNotificationEnabledAsync(userId, "visibility_change"))
        {
            _logger.LogDebug("用户 {UserId} 已禁用可见度变化通知", userId);
            return;
        }

        // 检查变化是否超过阈值
        var settings = await _repository.GetSettingsByUserIdAsync(userId);
        var threshold = settings?.VisibilityChangeThreshold ?? 10;
        var changePercent = Math.Abs(newScore - oldScore);

        if (changePercent < threshold)
        {
            _logger.LogDebug("可见度变化 {Change}% 未达到阈值 {Threshold}%", changePercent, threshold);
            return;
        }

        await EnqueueNotificationAsync(userId, email, "visibility_alert", new
        {
            user_name = userName,
            project_name = projectName,
            old_score = oldScore,
            new_score = newScore,
            change_percent = newScore > oldScore ? $"+{changePercent}" : $"-{changePercent}",
            result_url = resultUrl
        });
    }

    public async Task SendWelcomeEmailAsync(long userId, string email, string userName, string dashboardUrl)
    {
        await EnqueueNotificationAsync(userId, email, "welcome", new
        {
            user_name = userName,
            dashboard_url = dashboardUrl
        });
    }

    public async Task<bool> IsNotificationEnabledAsync(long userId, string notificationType)
    {
        var settings = await _repository.GetSettingsByUserIdAsync(userId);
        if (settings == null)
        {
            // 默认启用检测完成和失败通知
            return notificationType == "detection_complete" || notificationType == "detection_failed";
        }

        return notificationType switch
        {
            "detection_complete" => settings.EmailOnDetectionComplete,
            "detection_failed" => settings.EmailOnDetectionFailed,
            "weekly_report" => settings.EmailOnWeeklyReport,
            "visibility_change" => settings.EmailOnVisibilityChange,
            _ => false
        };
    }
}
