using GeoCore.Data.Entities;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 通知仓储接口
/// </summary>
public interface INotificationRepository
{
    // 邮件模板
    Task<GeoEmailTemplateEntity?> GetTemplateByCodeAsync(string templateCode);
    Task<List<GeoEmailTemplateEntity>> GetAllTemplatesAsync();
    Task<GeoEmailTemplateEntity> CreateTemplateAsync(GeoEmailTemplateEntity template);
    Task<GeoEmailTemplateEntity> UpdateTemplateAsync(GeoEmailTemplateEntity template);

    // 通知任务
    Task<GeoNotificationTaskEntity> CreateNotificationTaskAsync(GeoNotificationTaskEntity task);
    Task<List<GeoNotificationTaskEntity>> GetPendingTasksAsync(int limit = 10);
    Task UpdateTaskStatusAsync(long taskId, string status, string? resendId = null, string? errorMessage = null);
    Task<GeoNotificationTaskEntity?> GetTaskByIdAsync(long taskId);

    // 发送日志
    Task<GeoEmailSendLogEntity> CreateSendLogAsync(GeoEmailSendLogEntity log);

    // 通知设置
    Task<GeoNotificationSettingEntity?> GetSettingsByUserIdAsync(long userId);
    Task<GeoNotificationSettingEntity> CreateOrUpdateSettingsAsync(GeoNotificationSettingEntity settings);
}

/// <summary>
/// 通知仓储实现
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly ISqlSugarClient _db;

    public NotificationRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    #region 邮件模板

    public async Task<GeoEmailTemplateEntity?> GetTemplateByCodeAsync(string templateCode)
    {
        return await _db.Queryable<GeoEmailTemplateEntity>()
            .Where(t => t.TemplateCode == templateCode && t.IsActive)
            .FirstAsync();
    }

    public async Task<List<GeoEmailTemplateEntity>> GetAllTemplatesAsync()
    {
        return await _db.Queryable<GeoEmailTemplateEntity>()
            .OrderBy(t => t.TemplateCode)
            .ToListAsync();
    }

    public async Task<GeoEmailTemplateEntity> CreateTemplateAsync(GeoEmailTemplateEntity template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.Id = await _db.Insertable(template).ExecuteReturnIdentityAsync();
        return template;
    }

    public async Task<GeoEmailTemplateEntity> UpdateTemplateAsync(GeoEmailTemplateEntity template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await _db.Updateable(template).ExecuteCommandAsync();
        return template;
    }

    #endregion

    #region 通知任务

    public async Task<GeoNotificationTaskEntity> CreateNotificationTaskAsync(GeoNotificationTaskEntity task)
    {
        task.CreatedAt = DateTime.UtcNow;
        task.Id = await _db.Insertable(task).ExecuteReturnIdentityAsync();
        return task;
    }

    public async Task<List<GeoNotificationTaskEntity>> GetPendingTasksAsync(int limit = 10)
    {
        return await _db.Queryable<GeoNotificationTaskEntity>()
            .Where(t => t.Status == "pending" && t.RetryCount < 3)
            .OrderBy(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task UpdateTaskStatusAsync(long taskId, string status, string? resendId = null, string? errorMessage = null)
    {
        var update = _db.Updateable<GeoNotificationTaskEntity>()
            .SetColumns(t => t.Status == status)
            .Where(t => t.Id == taskId);

        if (resendId != null)
        {
            update = update.SetColumns(t => t.ResendId == resendId);
        }

        if (errorMessage != null)
        {
            update = update.SetColumns(t => t.ErrorMessage == errorMessage)
                          .SetColumns(t => t.RetryCount == t.RetryCount + 1);
        }

        if (status == "sent")
        {
            update = update.SetColumns(t => t.SentAt == DateTime.UtcNow);
        }

        await update.ExecuteCommandAsync();
    }

    public async Task<GeoNotificationTaskEntity?> GetTaskByIdAsync(long taskId)
    {
        return await _db.Queryable<GeoNotificationTaskEntity>()
            .Where(t => t.Id == taskId)
            .FirstAsync();
    }

    #endregion

    #region 发送日志

    public async Task<GeoEmailSendLogEntity> CreateSendLogAsync(GeoEmailSendLogEntity log)
    {
        log.CreatedAt = DateTime.UtcNow;
        log.Id = await _db.Insertable(log).ExecuteReturnIdentityAsync();
        return log;
    }

    #endregion

    #region 通知设置

    public async Task<GeoNotificationSettingEntity?> GetSettingsByUserIdAsync(long userId)
    {
        return await _db.Queryable<GeoNotificationSettingEntity>()
            .Where(s => s.UserId == userId)
            .FirstAsync();
    }

    public async Task<GeoNotificationSettingEntity> CreateOrUpdateSettingsAsync(GeoNotificationSettingEntity settings)
    {
        var existing = await GetSettingsByUserIdAsync(settings.UserId);
        if (existing != null)
        {
            settings.Id = existing.Id;
            settings.UpdatedAt = DateTime.UtcNow;
            await _db.Updateable(settings).ExecuteCommandAsync();
        }
        else
        {
            settings.CreatedAt = DateTime.UtcNow;
            settings.Id = await _db.Insertable(settings).ExecuteReturnIdentityAsync();
        }
        return settings;
    }

    #endregion
}
