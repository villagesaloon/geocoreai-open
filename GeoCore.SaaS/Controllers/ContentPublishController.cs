using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentPublish;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 内容发布 API (SaaS 前台)
/// Phase 8.7-8.13: 内容生成、草稿管理、发布
/// </summary>
[ApiController]
[Route("api/content-publish")]
public class ContentPublishController : ControllerBase
{
    private readonly ContentGenerationService _service;
    private readonly ILogger<ContentPublishController> _logger;

    public ContentPublishController(
        ContentGenerationService service,
        ILogger<ContentPublishController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private int GetUserId()
    {
        return 1;
    }

    #region 模板 (8.7)

    /// <summary>
    /// 获取指定平台的模板列表
    /// </summary>
    [HttpGet("templates/{platform}")]
    public async Task<IActionResult> GetTemplates(string platform)
    {
        var templates = await _service.GetTemplatesAsync(platform);
        return Ok(new { success = true, data = templates });
    }

    /// <summary>
    /// 获取所有平台的模板摘要
    /// </summary>
    [HttpGet("templates/summary")]
    public async Task<IActionResult> GetTemplateSummary()
    {
        var summary = await _service.GetTemplateSummaryAsync();
        return Ok(new { success = true, data = summary });
    }

    #endregion

    #region 内容生成 (8.8)

    /// <summary>
    /// 使用模板生成内容
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateContent([FromBody] ContentGenerationRequest request)
    {
        var result = await _service.GenerateContentAsync(request);
        if (!result.Success)
            return BadRequest(new { success = false, message = result.Error });
        return Ok(new { success = true, data = result });
    }

    #endregion

    #region 草稿管理 (8.9)

    /// <summary>
    /// 获取用户草稿列表
    /// </summary>
    [HttpGet("drafts")]
    public async Task<IActionResult> GetDrafts([FromQuery] string? status = null)
    {
        var userId = GetUserId();
        var drafts = await _service.GetDraftsAsync(userId, status);
        return Ok(new { success = true, data = drafts });
    }

    /// <summary>
    /// 获取草稿详情
    /// </summary>
    [HttpGet("drafts/{id}")]
    public async Task<IActionResult> GetDraft(int id)
    {
        var userId = GetUserId();
        var draft = await _service.GetDraftAsync(userId, id);
        if (draft == null)
            return NotFound(new { success = false, message = "草稿不存在" });
        return Ok(new { success = true, data = draft });
    }

    /// <summary>
    /// 保存草稿
    /// </summary>
    [HttpPost("drafts")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftRequest request)
    {
        var userId = GetUserId();
        var id = await _service.SaveDraftAsync(userId, request);
        return Ok(new { success = true, data = new { id } });
    }

    /// <summary>
    /// 更新草稿
    /// </summary>
    [HttpPut("drafts/{id}")]
    public async Task<IActionResult> UpdateDraft(int id, [FromBody] SaveDraftRequest request)
    {
        var userId = GetUserId();
        var result = await _service.UpdateDraftAsync(userId, id, request);
        if (!result)
            return NotFound(new { success = false, message = "草稿不存在或更新失败" });
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除草稿
    /// </summary>
    [HttpDelete("drafts/{id}")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var userId = GetUserId();
        var result = await _service.DeleteDraftAsync(userId, id);
        if (!result)
            return NotFound(new { success = false, message = "草稿不存在" });
        return Ok(new { success = true });
    }

    #endregion

    #region 发布前审核 (8.10)

    /// <summary>
    /// 审核内容
    /// </summary>
    [HttpPost("review")]
    public async Task<IActionResult> ReviewContent([FromBody] ReviewContentRequest request)
    {
        var result = await _service.ReviewContentAsync(request.Platform, request.Content);
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 提交草稿进行审核
    /// </summary>
    [HttpPost("drafts/{id}/submit")]
    public async Task<IActionResult> SubmitForReview(int id)
    {
        var userId = GetUserId();
        var result = await _service.SubmitForReviewAsync(userId, id);
        return Ok(new { success = true, data = result });
    }

    #endregion

    #region 用户平台账号 (8.11)

    /// <summary>
    /// 获取用户绑定的平台账号
    /// </summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var userId = GetUserId();
        var accounts = await _service.GetUserAccountsAsync(userId);
        return Ok(new { success = true, data = accounts });
    }

    /// <summary>
    /// 检查是否已绑定指定平台
    /// </summary>
    [HttpGet("accounts/check/{platform}")]
    public async Task<IActionResult> CheckAccount(string platform)
    {
        var userId = GetUserId();
        var hasBound = await _service.HasPlatformAccountAsync(userId, platform);
        return Ok(new { success = true, data = new { hasBound } });
    }

    #endregion

    #region 发布历史 (8.13)

    /// <summary>
    /// 获取发布历史
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int limit = 50)
    {
        var userId = GetUserId();
        var history = await _service.GetPublishHistoryAsync(userId, limit);
        return Ok(new { success = true, data = history });
    }

    #endregion
}

public class ReviewContentRequest
{
    public string Platform { get; set; } = "";
    public string Content { get; set; } = "";
}

/// <summary>
/// 内容发布 API (SaaS 前台)
/// Phase 8.12: 实际发布
/// </summary>
[ApiController]
[Route("api/publish")]
public class PublishController : ControllerBase
{
    private readonly PublishService _publishService;
    private readonly ILogger<PublishController> _logger;

    public PublishController(PublishService publishService, ILogger<PublishController> logger)
    {
        _publishService = publishService;
        _logger = logger;
    }

    private int GetUserId() => 1;

    /// <summary>
    /// 发布内容
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request)
    {
        var userId = GetUserId();
        var result = await _publishService.PublishAsync(userId, request);
        
        if (!result.Success)
            return BadRequest(new { success = false, message = result.Error });
            
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 发布草稿
    /// </summary>
    [HttpPost("draft/{draftId}")]
    public async Task<IActionResult> PublishDraft(int draftId)
    {
        var userId = GetUserId();
        var result = await _publishService.PublishDraftAsync(userId, draftId);
        
        if (!result.Success)
            return BadRequest(new { success = false, message = result.Error });
            
        return Ok(new { success = true, data = result });
    }
}

/// <summary>
/// 发布效果关联 API (SaaS 前台)
/// Phase 8.14: 追踪发布内容的 AI 引用效果
/// </summary>
[ApiController]
[Route("api/publish-effect")]
public class PublishEffectController : ControllerBase
{
    private readonly PublishEffectService _effectService;
    private readonly ILogger<PublishEffectController> _logger;

    public PublishEffectController(PublishEffectService effectService, ILogger<PublishEffectController> logger)
    {
        _effectService = effectService;
        _logger = logger;
    }

    private int GetUserId() => 1;

    /// <summary>
    /// 获取发布效果摘要
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] int? publishHistoryId = null)
    {
        var userId = GetUserId();
        var summary = await _effectService.GetEffectSummaryAsync(userId, publishHistoryId);
        return Ok(new { success = true, data = summary });
    }

    /// <summary>
    /// 获取单个发布记录的详细效果
    /// </summary>
    [HttpGet("detail/{publishHistoryId}")]
    public async Task<IActionResult> GetDetail(int publishHistoryId)
    {
        var userId = GetUserId();
        var detail = await _effectService.GetEffectDetailAsync(userId, publishHistoryId);
        
        if (detail == null)
            return NotFound(new { success = false, message = "发布记录不存在" });
            
        return Ok(new { success = true, data = detail });
    }

    /// <summary>
    /// 为发布内容创建引用监测任务
    /// </summary>
    [HttpPost("monitor/{publishHistoryId}")]
    public async Task<IActionResult> CreateMonitoringTask(int publishHistoryId, [FromBody] CreateMonitoringRequest request)
    {
        var userId = GetUserId();
        try
        {
            var taskId = await _effectService.CreateMonitoringTaskAsync(userId, publishHistoryId, request.Questions);
            return Ok(new { success = true, data = new { taskId } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取效果趋势
    /// </summary>
    [HttpGet("trend")]
    public async Task<IActionResult> GetTrend([FromQuery] int days = 30)
    {
        var userId = GetUserId();
        var trend = await _effectService.GetEffectTrendAsync(userId, days);
        return Ok(new { success = true, data = trend });
    }
}

public class CreateMonitoringRequest
{
    public List<string> Questions { get; set; } = new();
}
