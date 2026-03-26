using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeoCore.SaaS.Services.Notification;

/// <summary>
/// 邮件发送结果
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? ResendId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 邮件发送服务接口
/// </summary>
public interface IEmailSendService
{
    Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null);
    Task<EmailSendResult> SendTemplateEmailAsync(string to, string templateCode, object variables);
}

/// <summary>
/// Resend 邮件发送服务
/// https://resend.com/docs/api-reference/emails/send-email
/// 配置从数据库 sys_configs 表读取（Admin 后台管理）
/// </summary>
public class ResendEmailService : IEmailSendService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailTemplateService _templateService;
    private readonly INotificationRepository _notificationRepository;
    private readonly ISysConfigRepository _configRepository;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IHttpClientFactory httpClientFactory,
        IEmailTemplateService templateService,
        INotificationRepository notificationRepository,
        ISysConfigRepository configRepository,
        ILogger<ResendEmailService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _templateService = templateService;
        _notificationRepository = notificationRepository;
        _configRepository = configRepository;
        _logger = logger;
    }

    private async Task<(string ApiKey, string FromEmail, string FromName)> GetConfigAsync()
    {
        var apiKey = await _configRepository.GetValueAsync("resend", "api_key") ?? "";
        var fromEmail = await _configRepository.GetValueAsync("resend", "from_email") ?? "noreply@geocoreai.com";
        var fromName = await _configRepository.GetValueAsync("resend", "from_name") ?? "GeoCore AI";
        return (apiKey, fromEmail, fromName);
    }

    public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        try
        {
            var (apiKey, fromEmail, fromName) = await GetConfigAsync();
            
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Resend API Key 未配置");
                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = "邮件服务未配置"
                };
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var request = new ResendEmailRequest
            {
                From = $"{fromName} <{fromEmail}>",
                To = new[] { to },
                Subject = subject,
                Html = htmlBody,
                Text = textBody
            };

            var response = await httpClient.PostAsJsonAsync("https://api.resend.com/emails", request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ResendEmailResponse>(responseContent);
                _logger.LogInformation("邮件发送成功: {ResendId} -> {To}", result?.Id, to);

                // 记录发送日志
                await _notificationRepository.CreateSendLogAsync(new GeoEmailSendLogEntity
                {
                    ResendId = result?.Id,
                    RecipientEmail = to,
                    Subject = subject,
                    Status = "sent",
                    Response = responseContent
                });

                return new EmailSendResult
                {
                    Success = true,
                    ResendId = result?.Id
                };
            }
            else
            {
                var error = JsonSerializer.Deserialize<ResendErrorResponse>(responseContent);
                _logger.LogError("邮件发送失败: {StatusCode} - {Error}", response.StatusCode, error?.Message);

                // 记录失败日志
                await _notificationRepository.CreateSendLogAsync(new GeoEmailSendLogEntity
                {
                    RecipientEmail = to,
                    Subject = subject,
                    Status = "failed",
                    Response = responseContent
                });

                return new EmailSendResult
                {
                    Success = false,
                    ErrorMessage = error?.Message ?? $"HTTP {response.StatusCode}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邮件发送异常: {To}", to);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<EmailSendResult> SendTemplateEmailAsync(string to, string templateCode, object variables)
    {
        try
        {
            var (subject, htmlBody, textBody) = await _templateService.RenderTemplateAsync(templateCode, variables);
            return await SendEmailAsync(to, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模板邮件发送失败: {TemplateCode} -> {To}", templateCode, to);
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #region Resend API Models

    private class ResendEmailRequest
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string[] To { get; set; } = Array.Empty<string>();

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("html")]
        public string? Html { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class ResendEmailResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private class ResendErrorResponse
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    #endregion
}
