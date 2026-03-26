using System.Text.Json;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using GeoCore.SaaS.Controllers;
using GeoCore.SaaS.Services.CitationTracking;
using GeoCore.SaaS.Services.SiteAudit;
using GeoCore.Shared.Models;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// 检测任务后台 Worker
/// 从队列中取出任务并执行
/// </summary>
public class DetectionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DetectionWorker> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public DetectionWorker(IServiceProvider serviceProvider, ILogger<DetectionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[DetectionWorker] Starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextTaskAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DetectionWorker] Error processing task");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("[DetectionWorker] Stopped");
    }

    private async Task ProcessNextTaskAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<ITaskQueueService>();
        var statusService = scope.ServiceProvider.GetRequiredService<ITaskStatusService>();
        var taskRepository = scope.ServiceProvider.GetRequiredService<IDetectionTaskRepository>();

        // 从队列中取出任务
        var taskId = await queueService.DequeueDetectionTaskAsync();
        if (taskId == null)
            return;

        _logger.LogInformation("[DetectionWorker] Processing task {TaskId}", taskId);

        try
        {
            // 更新任务状态为运行中
            await taskRepository.UpdateStatusAsync(taskId.Value, "running", "开始执行检测任务");
            await statusService.SetStatusAsync(taskId.Value, "running", "开始执行检测任务");
            await statusService.SetProgressAsync(taskId.Value, 0, "init", "初始化检测任务");

            // 获取任务详情
            var task = await taskRepository.GetByIdAsync(taskId.Value);
            if (task == null)
            {
                _logger.LogWarning("[DetectionWorker] Task {TaskId} not found", taskId);
                return;
            }

            // 执行检测流程
            await ExecuteDetectionAsync(task, statusService, taskRepository, stoppingToken);

            // 更新任务状态为完成
            await taskRepository.UpdateStatusAsync(taskId.Value, "completed", "检测任务完成");
            await statusService.SetStatusAsync(taskId.Value, "completed", "检测任务完成");
            await statusService.SetProgressAsync(taskId.Value, 100, "completed", "检测任务完成");

            _logger.LogInformation("[DetectionWorker] Task {TaskId} completed", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DetectionWorker] Task {TaskId} failed", taskId);

            // 更新任务状态为失败
            await taskRepository.UpdateStatusAsync(taskId.Value, "failed", null, ex.Message);
            await statusService.SetStatusAsync(taskId.Value, "failed", ex.Message);
        }
    }

    private async Task ExecuteDetectionAsync(
        GeoDetectionTaskEntity task,
        ITaskStatusService statusService,
        IDetectionTaskRepository taskRepository,
        CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = new GeoDbContext();
        var projectRepo = new GeoProjectRepository(dbContext);
        var questionRepo = new GeoQuestionRepository(dbContext);
        
        _logger.LogInformation("[DetectionWorker] ========== 开始执行检测任务 {TaskId} ==========", task.Id);
        _logger.LogInformation("[DetectionWorker] 项目ID: {ProjectId}, 用户ID: {UserId}", task.ProjectId, task.UserId);
        
        // Phase 1: 数据准备 (0-20%)
        await statusService.SetProgressAsync(task.Id, 5, "data_prep", "正在准备项目数据...");
        
        var project = await projectRepo.GetProjectByIdAsync(task.ProjectId, task.UserId);
        if (project == null)
        {
            throw new InvalidOperationException($"项目 {task.ProjectId} 不存在");
        }
        
        _logger.LogInformation("[DetectionWorker] 项目: {BrandName}, 行业: {Industry}", project.BrandName, project.Industry);
        
        // 获取项目问题
        var questions = await questionRepo.GetQuestionsByProjectIdAsync(task.ProjectId, task.UserId);
        _logger.LogInformation("[DetectionWorker] 项目问题数: {Count}", questions.Count);
        
        if (questions.Count == 0)
        {
            _logger.LogWarning("[DetectionWorker] 项目没有问题，跳过检测");
            await statusService.SetProgressAsync(task.Id, 100, "completed", "项目没有问题，检测完成");
            return;
        }
        
        await statusService.SetProgressAsync(task.Id, 20, "data_prep", $"数据准备完成，共 {questions.Count} 个问题");

        // Phase 2: AI 可见度检测 (20-70%)
        await statusService.SetProgressAsync(task.Id, 25, "ai_detection", "正在执行 AI 可见度检测...");
        
        var citationTracker = scope.ServiceProvider.GetService<CitationTrackerService>();
        var detectionMetrics = new List<GeoDetectionMetricEntity>();
        var totalQuestions = questions.Count;
        var processedCount = 0;
        
        // 按国家-语言分组处理
        var countryLangGroups = questions.GroupBy(q => new { q.Country, q.Language });
        
        foreach (var group in countryLangGroups)
        {
            var country = group.Key.Country ?? "CN";
            var language = group.Key.Language ?? "zh-CN";
            var groupQuestions = group.ToList();
            
            _logger.LogInformation("[DetectionWorker] 处理国家-语言组: {Country}/{Lang}, 问题数: {Count}", 
                country, language, groupQuestions.Count);
            
            foreach (var question in groupQuestions)
            {
                if (stoppingToken.IsCancellationRequested) break;
                
                try
                {
                    // 获取问题的回答
                    var answers = await questionRepo.GetAnswersByQuestionIdAsync(question.Id);
                    
                    // 计算该问题的可见度分数
                    var visibilityScore = CalculateVisibilityScore(answers);
                    
                    // 累计到分组指标
                    processedCount++;
                    
                    // 更新进度 (20-70%)
                    var progress = 20 + (int)(50.0 * processedCount / totalQuestions);
                    await statusService.SetProgressAsync(task.Id, progress, "ai_detection", 
                        $"AI 检测进度: {processedCount}/{totalQuestions}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[DetectionWorker] 处理问题 {QuestionId} 失败", question.Id);
                }
            }
        }
        
        _logger.LogInformation("[DetectionWorker] AI 检测完成，处理 {Count} 个问题", processedCount);
        await statusService.SetProgressAsync(task.Id, 70, "ai_detection", $"AI 检测完成，共处理 {processedCount} 个问题");

        // Phase 3: 网站审计 (70-85%)
        GeoWebsiteAuditEntity? auditResult = null;
        if (!task.WebsiteAuditSkipped && !string.IsNullOrEmpty(project.MonitorUrl))
        {
            await statusService.SetProgressAsync(task.Id, 75, "website_audit", "正在执行网站审计...");
            
            try
            {
                var auditService = scope.ServiceProvider.GetService<IWebsiteAuditIntegrationService>();
                if (auditService != null)
                {
                    auditResult = await auditService.AuditAndSaveAsync(task.ProjectId, project.MonitorUrl, task.Id);
                    _logger.LogInformation("[DetectionWorker] 网站审计完成: Score={Score}, Grade={Grade}", 
                        auditResult.OverallScore, auditResult.Grade);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DetectionWorker] 网站审计失败: {Error}", ex.Message);
            }
            
            await statusService.SetProgressAsync(task.Id, 85, "website_audit", "网站审计完成");
        }
        else
        {
            _logger.LogInformation("[DetectionWorker] 跳过网站审计");
            await statusService.SetProgressAsync(task.Id, 85, "website_audit", "跳过网站审计");
        }

        // Phase 4: 结果分析与保存 (85-100%)
        await statusService.SetProgressAsync(task.Id, 90, "analysis", "正在分析检测结果...");
        
        // 创建或更新检测指标记录
        var metricRepo = new GeoDetectionMetricRepository(dbContext);
        
        // 创建汇总指标实体
        var summaryMetric = new GeoDetectionMetricEntity
        {
            TaskId = task.Id,
            ProjectId = task.ProjectId,
            CountryCode = "ALL",
            Language = "ALL",
            VisibilityScore = processedCount > 0 ? 50 : 0, // 基础分数
            MentionCount = processedCount,
            CitationPageCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        
        // 如果有网站审计结果，更新权威分数
        if (auditResult != null)
        {
            summaryMetric.AuthorityScore = auditResult.OverallScore;
        }
        
        try
        {
            await metricRepo.CreateAsync(summaryMetric);
            _logger.LogInformation("[DetectionWorker] 保存检测指标成功");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DetectionWorker] 保存检测指标失败: {Error}", ex.Message);
        }
        
        await statusService.SetProgressAsync(task.Id, 95, "analysis", "正在生成优化建议...");
        
        // TODO: 生成优化建议（可以调用 AI 生成）
        
        _logger.LogInformation("[DetectionWorker] ========== 检测任务 {TaskId} 完成 ==========", task.Id);
        await statusService.SetProgressAsync(task.Id, 100, "completed", "检测任务完成");
    }
    
    /// <summary>
    /// 计算可见度分数
    /// </summary>
    private decimal CalculateVisibilityScore(List<GeoQuestionAnswerDto> answers)
    {
        if (answers == null || answers.Count == 0) return 0;
        
        // 基于 SearchIndex 和 BrandFitIndex 计算可见度分数
        var avgSearchIndex = answers.Average(a => a.SearchIndex);
        var avgBrandFitIndex = answers.Average(a => a.BrandFitIndex);
        
        // 可见度分数 = (SearchIndex * 0.4 + BrandFitIndex * 0.6)
        return (decimal)(avgSearchIndex * 0.4 + avgBrandFitIndex * 0.6);
    }
}
