using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Services;

/// <summary>
/// Prompt 模板服务
/// </summary>
public class PromptTemplateService
{
    private readonly ILogger<PromptTemplateService> _logger;
    private readonly IPromptTemplateRepository _repository;

    public PromptTemplateService(
        ILogger<PromptTemplateService> logger,
        IPromptTemplateRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    public async Task<List<PromptTemplateEntity>> GetAllTemplatesAsync(bool enabledOnly = true)
    {
        return await _repository.GetAllAsync(enabledOnly);
    }

    /// <summary>
    /// 按类别获取模板
    /// </summary>
    public async Task<List<PromptTemplateEntity>> GetTemplatesByCategoryAsync(string category, bool enabledOnly = true)
    {
        return await _repository.GetByCategoryAsync(category, enabledOnly);
    }

    /// <summary>
    /// 获取模板详情
    /// </summary>
    public async Task<PromptTemplateEntity?> GetTemplateAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取类别的默认模板
    /// </summary>
    public async Task<PromptTemplateEntity?> GetDefaultTemplateAsync(string category)
    {
        return await _repository.GetDefaultByCategoryAsync(category);
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    public async Task<int> CreateTemplateAsync(CreateTemplateRequest request)
    {
        var template = new PromptTemplateEntity
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Template = request.Template,
            Variables = JsonSerializer.Serialize(request.Variables ?? new List<TemplateVariable>()),
            IsDefault = request.IsDefault,
            IsEnabled = true,
            Version = 1
        };

        var id = await _repository.CreateAsync(template);

        // 创建初始版本记录
        await _repository.CreateVersionAsync(new PromptTemplateVersionEntity
        {
            TemplateId = id,
            Version = 1,
            Template = request.Template,
            ChangeNote = "初始版本"
        });

        _logger.LogInformation("[PromptTemplate] Created template {Id}: {Name}", id, request.Name);
        return id;
    }

    /// <summary>
    /// 更新模板（创建新版本）
    /// </summary>
    public async Task UpdateTemplateAsync(int id, UpdateTemplateRequest request)
    {
        var template = await _repository.GetByIdAsync(id);
        if (template == null)
        {
            throw new ArgumentException($"Template {id} not found");
        }

        var templateChanged = template.Template != request.Template;

        template.Name = request.Name ?? template.Name;
        template.Description = request.Description ?? template.Description;
        template.Category = request.Category ?? template.Category;
        template.IsDefault = request.IsDefault ?? template.IsDefault;
        template.IsEnabled = request.IsEnabled ?? template.IsEnabled;

        if (templateChanged && !string.IsNullOrEmpty(request.Template))
        {
            template.Template = request.Template;
            template.Version++;

            // 创建新版本记录
            await _repository.CreateVersionAsync(new PromptTemplateVersionEntity
            {
                TemplateId = id,
                Version = template.Version,
                Template = request.Template,
                ChangeNote = request.ChangeNote ?? $"版本 {template.Version}"
            });
        }

        if (request.Variables != null)
        {
            template.Variables = JsonSerializer.Serialize(request.Variables);
        }

        await _repository.UpdateAsync(template);
        _logger.LogInformation("[PromptTemplate] Updated template {Id} to version {Version}", id, template.Version);
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    public async Task DeleteTemplateAsync(int id)
    {
        await _repository.DeleteAsync(id);
        _logger.LogInformation("[PromptTemplate] Deleted template {Id}", id);
    }

    /// <summary>
    /// 获取模板版本历史
    /// </summary>
    public async Task<List<PromptTemplateVersionEntity>> GetVersionHistoryAsync(int templateId)
    {
        return await _repository.GetVersionsAsync(templateId);
    }

    /// <summary>
    /// 获取特定版本
    /// </summary>
    public async Task<PromptTemplateVersionEntity?> GetVersionAsync(int templateId, int version)
    {
        return await _repository.GetVersionAsync(templateId, version);
    }

    /// <summary>
    /// 回滚到指定版本
    /// </summary>
    public async Task RollbackToVersionAsync(int templateId, int version)
    {
        var template = await _repository.GetByIdAsync(templateId);
        if (template == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        var targetVersion = await _repository.GetVersionAsync(templateId, version);
        if (targetVersion == null)
        {
            throw new ArgumentException($"Version {version} not found for template {templateId}");
        }

        template.Template = targetVersion.Template;
        template.Version++;

        // 创建回滚版本记录
        await _repository.CreateVersionAsync(new PromptTemplateVersionEntity
        {
            TemplateId = templateId,
            Version = template.Version,
            Template = targetVersion.Template,
            ChangeNote = $"回滚到版本 {version}"
        });

        await _repository.UpdateAsync(template);
        _logger.LogInformation("[PromptTemplate] Rolled back template {Id} to version {Version}", templateId, version);
    }

    /// <summary>
    /// 渲染模板（替换变量）
    /// </summary>
    public string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }

    /// <summary>
    /// 渲染模板（使用模板ID）
    /// </summary>
    public async Task<string> RenderTemplateAsync(int templateId, Dictionary<string, string> variables)
    {
        var template = await _repository.GetByIdAsync(templateId);
        if (template == null || !template.IsEnabled)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        return RenderTemplate(template.Template, variables);
    }

    /// <summary>
    /// 渲染类别的默认模板
    /// </summary>
    public async Task<string> RenderDefaultTemplateAsync(string category, Dictionary<string, string> variables)
    {
        var template = await _repository.GetDefaultByCategoryAsync(category);
        if (template == null)
        {
            throw new ArgumentException($"No default template found for category '{category}'");
        }

        return RenderTemplate(template.Template, variables);
    }

    /// <summary>
    /// 提取模板中的变量
    /// </summary>
    public List<string> ExtractVariables(string template)
    {
        var pattern = @"\{\{(\w+)\}\}";
        var matches = Regex.Matches(template, pattern);
        return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    /// <summary>
    /// 验证变量是否完整
    /// </summary>
    public (bool IsValid, List<string> MissingVariables) ValidateVariables(
        string template, 
        Dictionary<string, string> variables)
    {
        var required = ExtractVariables(template);
        var missing = required.Where(v => !variables.ContainsKey(v)).ToList();
        return (missing.Count == 0, missing);
    }
}

/// <summary>
/// 创建模板请求
/// </summary>
public class CreateTemplateRequest
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "general";
    public string Template { get; set; } = "";
    public List<TemplateVariable>? Variables { get; set; }
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// 更新模板请求
/// </summary>
public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Template { get; set; }
    public List<TemplateVariable>? Variables { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsEnabled { get; set; }
    public string? ChangeNote { get; set; }
}

/// <summary>
/// 模板变量定义
/// </summary>
public class TemplateVariable
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "string";
    public bool Required { get; set; } = true;
    public string Description { get; set; } = "";
    public string? DefaultValue { get; set; }
}
