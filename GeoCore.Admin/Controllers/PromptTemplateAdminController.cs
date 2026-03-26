using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// Prompt 模板管理控制器 - Admin 后台专用
/// 用于管理 prompt_templates 和 prompt_template_versions 表
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PromptTemplateAdminController : ControllerBase
{
    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<PromptTemplateAdminController> _logger;

    public PromptTemplateAdminController(
        IPromptTemplateRepository repository,
        ILogger<PromptTemplateAdminController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    #region 模板 CRUD

    /// <summary>
    /// 获取所有模板（包括禁用的）
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool enabledOnly = false)
    {
        try
        {
            var templates = await _repository.GetAllAsync(enabledOnly);
            return Ok(new
            {
                success = true,
                data = templates.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Description,
                    t.Category,
                    t.Version,
                    t.IsDefault,
                    t.IsEnabled,
                    t.CreatedAt,
                    t.UpdatedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get templates");
            return StatusCode(500, new { error = "获取模板列表失败" });
        }
    }

    /// <summary>
    /// 按类别获取模板
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var templates = await _repository.GetByCategoryAsync(category, enabledOnly);
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get templates by category {Category}", category);
            return StatusCode(500, new { error = "获取模板列表失败" });
        }
    }

    /// <summary>
    /// 获取模板详情（含完整内容）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var template = await _repository.GetByIdAsync(id);
            if (template == null)
            {
                return NotFound(new { error = "模板不存在" });
            }
            return Ok(new { success = true, data = template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get template {Id}", id);
            return StatusCode(500, new { error = "获取模板失败" });
        }
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "模板名称不能为空" });
            }
            if (string.IsNullOrWhiteSpace(request.Template))
            {
                return BadRequest(new { error = "模板内容不能为空" });
            }

            var entity = new PromptTemplateEntity
            {
                Name = request.Name,
                Description = request.Description ?? "",
                Category = request.Category ?? "general",
                Template = request.Template,
                Variables = request.Variables ?? "[]",
                IsDefault = request.IsDefault,
                IsEnabled = true
            };

            var id = await _repository.CreateAsync(entity);

            // 创建初始版本
            var version = new PromptTemplateVersionEntity
            {
                TemplateId = id,
                Version = 1,
                Template = request.Template,
                ChangeNote = "初始版本"
            };
            await _repository.CreateVersionAsync(version);

            _logger.LogInformation("[Admin] 创建 Prompt 模板: {Name} (id={Id})", request.Name, id);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create template");
            return StatusCode(500, new { error = "创建模板失败" });
        }
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { error = "模板不存在" });
            }

            // 检查模板内容是否变化，如果变化则创建新版本
            bool templateChanged = !string.IsNullOrEmpty(request.Template) && request.Template != existing.Template;

            // 更新字段
            if (!string.IsNullOrEmpty(request.Name)) existing.Name = request.Name;
            if (request.Description != null) existing.Description = request.Description;
            if (!string.IsNullOrEmpty(request.Category)) existing.Category = request.Category;
            if (!string.IsNullOrEmpty(request.Template)) existing.Template = request.Template;
            if (request.Variables != null) existing.Variables = request.Variables;
            if (request.IsDefault.HasValue) existing.IsDefault = request.IsDefault.Value;
            if (request.IsEnabled.HasValue) existing.IsEnabled = request.IsEnabled.Value;

            if (templateChanged)
            {
                existing.Version++;
                // 创建新版本
                var version = new PromptTemplateVersionEntity
                {
                    TemplateId = id,
                    Version = existing.Version,
                    Template = request.Template!,
                    ChangeNote = request.ChangeNote ?? ""
                };
                await _repository.CreateVersionAsync(version);
            }

            await _repository.UpdateAsync(existing);

            _logger.LogInformation("[Admin] 更新 Prompt 模板: {Name} (id={Id}, version={Version})", 
                existing.Name, id, existing.Version);
            return Ok(new { success = true, newVersion = templateChanged ? existing.Version : (int?)null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update template {Id}", id);
            return StatusCode(500, new { error = "更新模板失败" });
        }
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { error = "模板不存在" });
            }

            await _repository.DeleteAsync(id);

            _logger.LogInformation("[Admin] 删除 Prompt 模板: {Name} (id={Id})", existing.Name, id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete template {Id}", id);
            return StatusCode(500, new { error = "删除模板失败" });
        }
    }

    #endregion

    #region 版本管理

    /// <summary>
    /// 获取版本历史
    /// </summary>
    [HttpGet("{id}/versions")]
    public async Task<IActionResult> GetVersions(int id)
    {
        try
        {
            var versions = await _repository.GetVersionsAsync(id);
            return Ok(new { success = true, data = versions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get versions for template {Id}", id);
            return StatusCode(500, new { error = "获取版本历史失败" });
        }
    }

    /// <summary>
    /// 获取特定版本
    /// </summary>
    [HttpGet("{id}/versions/{version}")]
    public async Task<IActionResult> GetVersion(int id, int version)
    {
        try
        {
            var v = await _repository.GetVersionAsync(id, version);
            if (v == null)
            {
                return NotFound(new { error = "版本不存在" });
            }
            return Ok(new { success = true, data = v });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get version {Version} for template {Id}", version, id);
            return StatusCode(500, new { error = "获取版本失败" });
        }
    }

    /// <summary>
    /// 回滚到指定版本
    /// </summary>
    [HttpPost("{id}/rollback/{version}")]
    public async Task<IActionResult> Rollback(int id, int version)
    {
        try
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { error = "模板不存在" });
            }

            var targetVersion = await _repository.GetVersionAsync(id, version);
            if (targetVersion == null)
            {
                return NotFound(new { error = "目标版本不存在" });
            }

            // 创建新版本（回滚版本）
            existing.Version++;
            existing.Template = targetVersion.Template;

            var newVersion = new PromptTemplateVersionEntity
            {
                TemplateId = id,
                Version = existing.Version,
                Template = targetVersion.Template,
                ChangeNote = $"回滚到版本 {version}"
            };
            await _repository.CreateVersionAsync(newVersion);
            await _repository.UpdateAsync(existing);

            _logger.LogInformation("[Admin] 回滚 Prompt 模板: {Name} (id={Id}) 到版本 {TargetVersion}, 新版本 {NewVersion}", 
                existing.Name, id, version, existing.Version);
            return Ok(new { success = true, newVersion = existing.Version });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to rollback template {Id} to version {Version}", id, version);
            return StatusCode(500, new { error = "回滚失败" });
        }
    }

    /// <summary>
    /// 对比两个版本
    /// </summary>
    [HttpGet("{id}/compare")]
    public async Task<IActionResult> CompareVersions(int id, [FromQuery] int v1, [FromQuery] int v2)
    {
        try
        {
            var version1 = await _repository.GetVersionAsync(id, v1);
            var version2 = await _repository.GetVersionAsync(id, v2);

            if (version1 == null || version2 == null)
            {
                return NotFound(new { error = "版本不存在" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    version1 = new { version1.Version, version1.Template, version1.ChangeNote, version1.CreatedAt },
                    version2 = new { version2.Version, version2.Template, version2.ChangeNote, version2.CreatedAt }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to compare versions for template {Id}", id);
            return StatusCode(500, new { error = "版本对比失败" });
        }
    }

    #endregion

    #region 批量操作

    /// <summary>
    /// 批量启用/禁用模板
    /// </summary>
    [HttpPost("batch/toggle")]
    public async Task<IActionResult> BatchToggle([FromBody] BatchToggleRequest request)
    {
        try
        {
            int count = 0;
            foreach (var id in request.Ids)
            {
                var template = await _repository.GetByIdAsync(id);
                if (template != null)
                {
                    template.IsEnabled = request.Enable;
                    await _repository.UpdateAsync(template);
                    count++;
                }
            }

            _logger.LogInformation("[Admin] 批量{Action} {Count} 个 Prompt 模板", 
                request.Enable ? "启用" : "禁用", count);
            return Ok(new { success = true, count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to batch toggle templates");
            return StatusCode(500, new { error = "批量操作失败" });
        }
    }

    /// <summary>
    /// 获取所有类别
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var templates = await _repository.GetAllAsync(false);
            var categories = templates.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
            return Ok(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get categories");
            return StatusCode(500, new { error = "获取类别失败" });
        }
    }

    #endregion
}

#region Request Models

public class CreateTemplateRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string Template { get; set; } = "";
    public string? Variables { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Template { get; set; }
    public string? Variables { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsEnabled { get; set; }
    public string? ChangeNote { get; set; }
}

public class BatchToggleRequest
{
    public List<int> Ids { get; set; } = new();
    public bool Enable { get; set; }
}

#endregion
