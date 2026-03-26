namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// Redis 键名常量
/// 检测系统使用的所有 Redis 键
/// </summary>
public static class RedisKeys
{
    /// <summary>
    /// 键前缀
    /// </summary>
    private const string Prefix = "geo:";

    #region 队列相关

    /// <summary>
    /// 检测任务队列
    /// </summary>
    public const string DetectionQueue = Prefix + "queue:detection";

    /// <summary>
    /// 爬虫任务队列
    /// </summary>
    public const string CrawlerQueue = Prefix + "queue:crawler";

    /// <summary>
    /// 通知任务队列
    /// </summary>
    public const string NotificationQueue = Prefix + "queue:notification";

    /// <summary>
    /// 处理中的任务集合（用于防止重复处理）
    /// </summary>
    public const string ProcessingTasks = Prefix + "processing:tasks";

    #endregion

    #region 任务状态相关

    /// <summary>
    /// 获取任务状态键
    /// </summary>
    public static string TaskStatus(long taskId) => $"{Prefix}task:{taskId}:status";

    /// <summary>
    /// 获取任务进度键
    /// </summary>
    public static string TaskProgress(long taskId) => $"{Prefix}task:{taskId}:progress";

    /// <summary>
    /// 获取任务结果键
    /// </summary>
    public static string TaskResult(long taskId) => $"{Prefix}task:{taskId}:result";

    #endregion

    #region 缓存相关

    /// <summary>
    /// 获取网站审计缓存键
    /// </summary>
    public static string WebsiteAuditCache(long projectId) => $"{Prefix}cache:audit:{projectId}";

    /// <summary>
    /// 获取项目检测限制键（每日限制）
    /// </summary>
    public static string DetectionDailyLimit(long userId, string date) => $"{Prefix}limit:detection:{userId}:{date}";

    /// <summary>
    /// 获取网站审计限制键（每日限制）
    /// </summary>
    public static string AuditDailyLimit(long userId, string date) => $"{Prefix}limit:audit:{userId}:{date}";

    #endregion

    #region 锁相关

    /// <summary>
    /// 获取项目检测锁键（防止同一项目并发检测）
    /// </summary>
    public static string ProjectDetectionLock(long projectId) => $"{Prefix}lock:detection:{projectId}";

    /// <summary>
    /// 获取网站爬取锁键（防止同一网站并发爬取）
    /// </summary>
    public static string WebsiteCrawlLock(string domain) => $"{Prefix}lock:crawl:{domain}";

    #endregion
}
