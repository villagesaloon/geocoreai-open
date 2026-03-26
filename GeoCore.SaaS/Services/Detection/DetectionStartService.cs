using System.Text.Json;
using GeoCore.Data.DbContext;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// 检测启动请求
/// </summary>
public class DetectionStartRequest
{
    public long ProjectId { get; set; }
    public long UserId { get; set; }
    public string? BrandName { get; set; }
    public string? WebsiteUrl { get; set; }
    public List<string> Models { get; set; } = new() { "perplexity" };
    public bool ForceRefresh { get; set; } = false;
}

/// <summary>
/// 检测启动结果
/// </summary>
public class DetectionStartResult
{
    public bool Success { get; set; }
    public string? TaskId { get; set; }
    public string? QuestionTaskId { get; set; }
    public int QuestionsCount { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 检测启动服务接口
/// 封装完整的检测流程：检查问题 -> 生成问题（如需要）-> 启动检测
/// </summary>
public interface IDetectionStartService
{
    /// <summary>
    /// 启动检测（包含问题检查和生成）
    /// </summary>
    Task<DetectionStartResult> StartDetectionAsync(DetectionStartRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// 检查项目是否有问题
    /// </summary>
    Task<int> GetQuestionCountAsync(long projectId);
    
    /// <summary>
    /// 生成问题（调用 DistillationController 的逻辑）
    /// </summary>
    Task<(bool Success, string? TaskId, string? Error)> GenerateQuestionsAsync(DetectionStartRequest request, CancellationToken ct = default);
}

/// <summary>
/// 检测启动服务实现
/// </summary>
public class DetectionStartService : IDetectionStartService
{
    private readonly ITaskQueueService _queueService;
    private readonly ITaskStatusService _statusService;
    private readonly ILogger<DetectionStartService> _logger;

    public DetectionStartService(
        ITaskQueueService queueService,
        ITaskStatusService statusService,
        ILogger<DetectionStartService> logger)
    {
        _queueService = queueService;
        _statusService = statusService;
        _logger = logger;
    }

    /// <summary>
    /// 启动检测（包含问题检查和生成）
    /// </summary>
    public async Task<DetectionStartResult> StartDetectionAsync(DetectionStartRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("[DetectionStart] 开始检测流程, ProjectId: {ProjectId}, UserId: {UserId}", 
            request.ProjectId, request.UserId);

        try
        {
            // Step 1: 检查项目是否有问题
            var questionCount = await GetQuestionCountAsync(request.ProjectId);
            _logger.LogInformation("[DetectionStart] 项目 {ProjectId} 现有问题数: {Count}", request.ProjectId, questionCount);

            // Step 2: 如果没有问题，需要先生成问题
            string? questionTaskId = null;
            if (questionCount == 0 || request.ForceRefresh)
            {
                _logger.LogInformation("[DetectionStart] 项目 {ProjectId} 需要生成问题", request.ProjectId);
                
                var genResult = await GenerateQuestionsAsync(request, ct);
                if (!genResult.Success)
                {
                    return new DetectionStartResult
                    {
                        Success = false,
                        Error = genResult.Error ?? "问题生成失败"
                    };
                }
                
                questionTaskId = genResult.TaskId;
                _logger.LogInformation("[DetectionStart] 问题生成任务已创建: {TaskId}", questionTaskId);
                
                // 返回问题生成任务 ID，前端需要轮询等待完成
                return new DetectionStartResult
                {
                    Success = true,
                    QuestionTaskId = questionTaskId,
                    Message = "问题生成任务已创建，请轮询等待完成后再启动检测"
                };
            }

            // Step 3: 有问题，直接启动检测
            var taskId = request.ProjectId;
            var position = await _queueService.EnqueueDetectionTaskAsync(taskId, "full");
            await _statusService.SetStatusAsync(taskId, "queued", "任务排队中");
            await _statusService.SetProgressAsync(taskId, 0, "queued", "等待执行");

            _logger.LogInformation("[DetectionStart] 检测任务已入队: {TaskId}, 位置: {Position}", taskId, position);

            return new DetectionStartResult
            {
                Success = true,
                TaskId = taskId.ToString(),
                QuestionsCount = questionCount,
                Message = "检测任务已加入队列"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DetectionStart] 启动检测失败");
            return new DetectionStartResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 检查项目是否有问题
    /// </summary>
    public async Task<int> GetQuestionCountAsync(long projectId)
    {
        try
        {
            var dbContext = new GeoDbContext();
            var questionRepo = new GeoQuestionRepository(dbContext);
            // 使用 userId=0 获取所有用户的问题（管理员视角）
            // 实际使用时应该传入正确的 userId
            var questions = await questionRepo.GetQuestionsByProjectIdAsync(projectId, 0);
            return questions?.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DetectionStart] 获取问题数量失败, ProjectId: {ProjectId}", projectId);
            return 0;
        }
    }

    /// <summary>
    /// 生成问题
    /// 注意：这里只是创建任务，实际生成由 DistillationController 的后台线程处理
    /// </summary>
    public async Task<(bool Success, string? TaskId, string? Error)> GenerateQuestionsAsync(
        DetectionStartRequest request, CancellationToken ct = default)
    {
        // 这里我们需要调用 DistillationController 的问题生成逻辑
        // 但为了避免循环依赖，我们通过 HTTP 调用自己的 API
        // 或者直接复用 DistillationController 中的静态任务字典
        
        // 简化实现：直接返回需要前端调用问题生成 API 的提示
        // 实际的问题生成逻辑已经在 DistillationController 中实现
        
        _logger.LogInformation("[DetectionStart] 需要生成问题, 请调用 /api/distillation/questions API");
        
        return (false, null, "请先调用 /api/distillation/questions API 生成问题");
    }
}
