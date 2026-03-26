using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using GeoCore.SaaS.Services.Notification;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.SaaS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly IEmailSendService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly INotificationService _notificationService;
    private readonly ISysConfigRepository _configRepository;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        IEmailSendService emailService,
        IEmailTemplateService templateService,
        INotificationService notificationService,
        ISysConfigRepository configRepository,
        ILogger<NotificationController> logger)
    {
        _emailService = emailService;
        _templateService = templateService;
        _notificationService = notificationService;
        _configRepository = configRepository;
        _logger = logger;
    }

    /// <summary>
    /// 初始化 Resend 配置（仅首次使用）
    /// </summary>
    [HttpPost("init-config")]
    public async Task<IActionResult> InitConfig()
    {
        var existing = await _configRepository.GetValueAsync("resend", "api_key");
        if (!string.IsNullOrEmpty(existing))
        {
            return Ok(new { message = "配置已存在", exists = true });
        }

        var configs = new List<SysConfigEntity>
        {
            new()
            {
                ConfigGroup = "resend",
                ConfigKey = "api_key",
                ConfigValue = "re_hWT1vEKG_LjfMqWmce2GUCbqmWWnJcsjg",
                Name = "Resend API Key",
                Description = "Resend 邮件服务的 API 密钥",
                ValueType = "string",
                IsSensitive = true,
                SortOrder = 1
            },
            new()
            {
                ConfigGroup = "resend",
                ConfigKey = "from_email",
                ConfigValue = "noreply@geocoreai.com",
                Name = "发件人邮箱",
                Description = "邮件发送时使用的发件人邮箱地址",
                ValueType = "string",
                IsSensitive = false,
                SortOrder = 2
            },
            new()
            {
                ConfigGroup = "resend",
                ConfigKey = "from_name",
                ConfigValue = "GeoCore AI",
                Name = "发件人名称",
                Description = "邮件发送时显示的发件人名称",
                ValueType = "string",
                IsSensitive = false,
                SortOrder = 3
            }
        };

        foreach (var config in configs)
        {
            await _configRepository.CreateOrUpdateAsync(config);
        }

        return Ok(new { message = "配置初始化成功", count = configs.Count });
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    [HttpPost("update-config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateConfigRequest request)
    {
        await _configRepository.SetValueAsync(request.ConfigGroup, request.ConfigKey, request.ConfigValue);
        return Ok(new { message = "配置更新成功" });
    }

    /// <summary>
    /// 发送测试邮件（直接发送，不经过队列）
    /// </summary>
    [HttpPost("test-send")]
    public async Task<IActionResult> TestSendEmail([FromBody] TestEmailRequest request)
    {
        try
        {
            var result = await _emailService.SendTemplateEmailAsync(
                request.Email,
                request.TemplateCode,
                request.Variables ?? new Dictionary<string, object>());

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = "邮件发送成功",
                    resendId = result.ResendId
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "邮件发送失败",
                    error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试邮件发送失败");
            return StatusCode(500, new
            {
                success = false,
                message = "邮件发送异常",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// 获取所有邮件模板
    /// </summary>
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        return Ok(templates.Select(t => new
        {
            t.Id,
            t.TemplateCode,
            t.Name,
            t.Subject,
            t.IsActive,
            t.Variables
        }));
    }
}

public class TestEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "welcome";
    public Dictionary<string, object>? Variables { get; set; }
}

public class UpdateConfigRequest
{
    public string ConfigGroup { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public string? ConfigValue { get; set; }
}
