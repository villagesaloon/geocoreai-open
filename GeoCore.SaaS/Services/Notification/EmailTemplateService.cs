using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Scriban;
using Scriban.Runtime;
using System.Text.Json;

namespace GeoCore.SaaS.Services.Notification;

/// <summary>
/// 邮件模板服务接口
/// </summary>
public interface IEmailTemplateService
{
    Task<GeoEmailTemplateEntity?> GetTemplateAsync(string templateCode);
    Task<(string Subject, string BodyHtml, string? BodyText)> RenderTemplateAsync(string templateCode, object variables);
    Task<List<GeoEmailTemplateEntity>> GetAllTemplatesAsync();
    Task<GeoEmailTemplateEntity> SaveTemplateAsync(GeoEmailTemplateEntity template);
    Task InitializeDefaultTemplatesAsync();
}

/// <summary>
/// 邮件模板服务实现
/// 使用 Scriban 模板引擎
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(
        INotificationRepository repository,
        ILogger<EmailTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GeoEmailTemplateEntity?> GetTemplateAsync(string templateCode)
    {
        return await _repository.GetTemplateByCodeAsync(templateCode);
    }

    public async Task<(string Subject, string BodyHtml, string? BodyText)> RenderTemplateAsync(
        string templateCode, object variables)
    {
        var template = await _repository.GetTemplateByCodeAsync(templateCode);
        if (template == null)
        {
            throw new InvalidOperationException($"邮件模板 '{templateCode}' 不存在");
        }

        try
        {
            // 将变量转换为 ScriptObject
            var scriptObject = new ScriptObject();
            if (variables is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    scriptObject.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                // 使用反射获取属性
                var props = variables.GetType().GetProperties();
                foreach (var prop in props)
                {
                    scriptObject.Add(prop.Name, prop.GetValue(variables));
                }
            }

            var context = new TemplateContext();
            context.PushGlobal(scriptObject);

            // 渲染主题
            var subjectTemplate = Template.Parse(template.Subject);
            var subject = await subjectTemplate.RenderAsync(context);

            // 渲染 HTML 内容
            var htmlTemplate = Template.Parse(template.BodyHtml);
            var bodyHtml = await htmlTemplate.RenderAsync(context);

            // 渲染纯文本内容（如果有）
            string? bodyText = null;
            if (!string.IsNullOrEmpty(template.BodyText))
            {
                var textTemplate = Template.Parse(template.BodyText);
                bodyText = await textTemplate.RenderAsync(context);
            }

            return (subject, bodyHtml, bodyText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "渲染邮件模板 {TemplateCode} 失败", templateCode);
            throw;
        }
    }

    public async Task<List<GeoEmailTemplateEntity>> GetAllTemplatesAsync()
    {
        return await _repository.GetAllTemplatesAsync();
    }

    public async Task<GeoEmailTemplateEntity> SaveTemplateAsync(GeoEmailTemplateEntity template)
    {
        if (template.Id > 0)
        {
            return await _repository.UpdateTemplateAsync(template);
        }
        return await _repository.CreateTemplateAsync(template);
    }

    public async Task InitializeDefaultTemplatesAsync()
    {
        var templates = await _repository.GetAllTemplatesAsync();
        if (templates.Count > 0)
        {
            _logger.LogInformation("邮件模板已存在，跳过初始化");
            return;
        }

        _logger.LogInformation("初始化默认邮件模板...");

        var defaultTemplates = GetDefaultTemplates();
        foreach (var template in defaultTemplates)
        {
            await _repository.CreateTemplateAsync(template);
            _logger.LogInformation("创建邮件模板: {TemplateCode}", template.TemplateCode);
        }
    }

    private List<GeoEmailTemplateEntity> GetDefaultTemplates()
    {
        return new List<GeoEmailTemplateEntity>
        {
            new GeoEmailTemplateEntity
            {
                TemplateCode = "detection_completed",
                Name = "检测完成通知",
                Subject = "✅ GEO 检测完成：{{ project_name }}",
                BodyHtml = GetDetectionCompletedTemplate(),
                Variables = JsonSerializer.Serialize(new[]
                {
                    new { Name = "user_name", Description = "用户名" },
                    new { Name = "project_name", Description = "项目名称" },
                    new { Name = "visibility_score", Description = "AI 可见度评分" },
                    new { Name = "brand_mention_rate", Description = "品牌提及率" },
                    new { Name = "website_health_score", Description = "网站健康度" },
                    new { Name = "result_url", Description = "结果页面链接" }
                })
            },
            new GeoEmailTemplateEntity
            {
                TemplateCode = "detection_failed",
                Name = "检测失败通知",
                Subject = "❌ GEO 检测失败：{{ project_name }}",
                BodyHtml = GetDetectionFailedTemplate(),
                Variables = JsonSerializer.Serialize(new[]
                {
                    new { Name = "user_name", Description = "用户名" },
                    new { Name = "project_name", Description = "项目名称" },
                    new { Name = "error_message", Description = "错误信息" },
                    new { Name = "retry_url", Description = "重试链接" }
                })
            },
            new GeoEmailTemplateEntity
            {
                TemplateCode = "visibility_alert",
                Name = "可见度异常警报",
                Subject = "⚠️ AI 可见度变化警报：{{ project_name }}",
                BodyHtml = GetVisibilityAlertTemplate(),
                Variables = JsonSerializer.Serialize(new[]
                {
                    new { Name = "user_name", Description = "用户名" },
                    new { Name = "project_name", Description = "项目名称" },
                    new { Name = "old_score", Description = "原评分" },
                    new { Name = "new_score", Description = "新评分" },
                    new { Name = "change_percent", Description = "变化百分比" },
                    new { Name = "result_url", Description = "结果页面链接" }
                })
            },
            new GeoEmailTemplateEntity
            {
                TemplateCode = "welcome",
                Name = "欢迎邮件",
                Subject = "🎉 欢迎使用 GeoCore AI",
                BodyHtml = GetWelcomeTemplate(),
                Variables = JsonSerializer.Serialize(new[]
                {
                    new { Name = "user_name", Description = "用户名" },
                    new { Name = "dashboard_url", Description = "控制台链接" }
                })
            }
        };
    }

    private string GetDetectionCompletedTemplate()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; padding: 20px 0; border-bottom: 1px solid #eee; }
        .logo { font-size: 24px; font-weight: bold; color: #6366f1; }
        .content { padding: 30px 0; }
        .title { font-size: 20px; font-weight: 600; margin-bottom: 20px; }
        .metrics { display: flex; justify-content: space-around; margin: 20px 0; }
        .metric { text-align: center; padding: 15px; background: #f9fafb; border-radius: 8px; flex: 1; margin: 0 5px; }
        .metric-value { font-size: 28px; font-weight: bold; color: #6366f1; }
        .metric-label { font-size: 12px; color: #6b7280; margin-top: 5px; }
        .btn { display: inline-block; padding: 12px 24px; background: #6366f1; color: white; text-decoration: none; border-radius: 6px; font-weight: 500; }
        .footer { text-align: center; padding: 20px 0; border-top: 1px solid #eee; color: #9ca3af; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">GeoCore AI</div>
    </div>
    <div class=""content"">
        <div class=""title"">✅ 检测完成：{{ project_name }}</div>
        <p>您好，{{ user_name }}！</p>
        <p>您的项目 <strong>{{ project_name }}</strong> 的 GEO 检测已完成。以下是检测摘要：</p>
        <div class=""metrics"">
            <div class=""metric"">
                <div class=""metric-value"">{{ visibility_score }}</div>
                <div class=""metric-label"">AI 可见度</div>
            </div>
            <div class=""metric"">
                <div class=""metric-value"">{{ brand_mention_rate }}%</div>
                <div class=""metric-label"">品牌提及率</div>
            </div>
            <div class=""metric"">
                <div class=""metric-value"">{{ website_health_score }}</div>
                <div class=""metric-label"">网站健康度</div>
            </div>
        </div>
        <p style=""text-align: center; margin-top: 30px;"">
            <a href=""{{ result_url }}"" class=""btn"">查看完整报告</a>
        </p>
    </div>
    <div class=""footer"">
        <p>此邮件由 GeoCore AI 自动发送，请勿回复。</p>
    </div>
</body>
</html>";
    }

    private string GetDetectionFailedTemplate()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; padding: 20px 0; border-bottom: 1px solid #eee; }
        .logo { font-size: 24px; font-weight: bold; color: #6366f1; }
        .content { padding: 30px 0; }
        .title { font-size: 20px; font-weight: 600; margin-bottom: 20px; color: #dc2626; }
        .error-box { padding: 20px; background: #fef2f2; border: 1px solid #fecaca; border-radius: 8px; margin: 20px 0; }
        .btn { display: inline-block; padding: 12px 24px; background: #6366f1; color: white; text-decoration: none; border-radius: 6px; font-weight: 500; }
        .footer { text-align: center; padding: 20px 0; border-top: 1px solid #eee; color: #9ca3af; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">GeoCore AI</div>
    </div>
    <div class=""content"">
        <div class=""title"">❌ 检测失败：{{ project_name }}</div>
        <p>您好，{{ user_name }}！</p>
        <p>很抱歉，您的项目 <strong>{{ project_name }}</strong> 的 GEO 检测遇到了问题。</p>
        <div class=""error-box"">
            <strong>错误信息：</strong><br>
            {{ error_message }}
        </div>
        <p style=""text-align: center; margin-top: 30px;"">
            <a href=""{{ retry_url }}"" class=""btn"">重新检测</a>
        </p>
    </div>
    <div class=""footer"">
        <p>如需帮助，请联系我们的技术支持。</p>
    </div>
</body>
</html>";
    }

    private string GetVisibilityAlertTemplate()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; padding: 20px 0; border-bottom: 1px solid #eee; }
        .logo { font-size: 24px; font-weight: bold; color: #6366f1; }
        .content { padding: 30px 0; }
        .title { font-size: 20px; font-weight: 600; margin-bottom: 20px; color: #f59e0b; }
        .change-box { display: flex; justify-content: center; align-items: center; gap: 20px; margin: 20px 0; padding: 20px; background: #fffbeb; border-radius: 8px; }
        .score { font-size: 36px; font-weight: bold; }
        .score.old { color: #6b7280; }
        .score.new { color: #dc2626; }
        .arrow { font-size: 24px; color: #dc2626; }
        .btn { display: inline-block; padding: 12px 24px; background: #6366f1; color: white; text-decoration: none; border-radius: 6px; font-weight: 500; }
        .footer { text-align: center; padding: 20px 0; border-top: 1px solid #eee; color: #9ca3af; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">GeoCore AI</div>
    </div>
    <div class=""content"">
        <div class=""title"">⚠️ AI 可见度变化警报</div>
        <p>您好，{{ user_name }}！</p>
        <p>您的项目 <strong>{{ project_name }}</strong> 的 AI 可见度发生了显著变化：</p>
        <div class=""change-box"">
            <span class=""score old"">{{ old_score }}</span>
            <span class=""arrow"">→</span>
            <span class=""score new"">{{ new_score }}</span>
            <span style=""color: #dc2626; font-weight: bold;"">{{ change_percent }}%</span>
        </div>
        <p style=""text-align: center; margin-top: 30px;"">
            <a href=""{{ result_url }}"" class=""btn"">查看详情</a>
        </p>
    </div>
    <div class=""footer"">
        <p>您可以在设置中调整警报阈值或关闭此通知。</p>
    </div>
</body>
</html>";
    }

    private string GetWelcomeTemplate()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { text-align: center; padding: 20px 0; border-bottom: 1px solid #eee; }
        .logo { font-size: 24px; font-weight: bold; color: #6366f1; }
        .content { padding: 30px 0; }
        .title { font-size: 24px; font-weight: 600; margin-bottom: 20px; text-align: center; }
        .features { margin: 20px 0; }
        .feature { display: flex; align-items: center; gap: 10px; margin: 10px 0; }
        .feature-icon { font-size: 20px; }
        .btn { display: inline-block; padding: 12px 24px; background: #6366f1; color: white; text-decoration: none; border-radius: 6px; font-weight: 500; }
        .footer { text-align: center; padding: 20px 0; border-top: 1px solid #eee; color: #9ca3af; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""logo"">GeoCore AI</div>
    </div>
    <div class=""content"">
        <div class=""title"">🎉 欢迎使用 GeoCore AI</div>
        <p>您好，{{ user_name }}！</p>
        <p>感谢您注册 GeoCore AI，这是一个专业的 AI 搜索可见度优化平台。</p>
        <div class=""features"">
            <div class=""feature""><span class=""feature-icon"">🔍</span> AI 搜索可见度检测</div>
            <div class=""feature""><span class=""feature-icon"">📊</span> 多模型品牌提及分析</div>
            <div class=""feature""><span class=""feature-icon"">🌐</span> 网站 GEO 优化审计</div>
            <div class=""feature""><span class=""feature-icon"">💡</span> 智能优化建议</div>
        </div>
        <p style=""text-align: center; margin-top: 30px;"">
            <a href=""{{ dashboard_url }}"" class=""btn"">开始使用</a>
        </p>
    </div>
    <div class=""footer"">
        <p>如有任何问题，请随时联系我们。</p>
    </div>
</body>
</html>";
    }
}
