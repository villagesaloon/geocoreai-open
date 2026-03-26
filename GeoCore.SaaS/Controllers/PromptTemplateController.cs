using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Prompt 模板控制器 - SaaS 前台专用
/// 仅提供只读和渲染功能，模板管理请使用 Admin 后台
/// </summary>
[ApiController]
[Route("api/prompt-templates")]
public class PromptTemplateController : ControllerBase
{
    private readonly ILogger<PromptTemplateController> _logger;
    private readonly PromptTemplateService _templateService;

    public PromptTemplateController(
        ILogger<PromptTemplateController> logger,
        PromptTemplateService templateService)
    {
        _logger = logger;
        _templateService = templateService;
    }

    #region 只读 API

    /// <summary>
    /// 获取所有已启用的模板
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            // SaaS 前台只能获取已启用的模板
            var templates = await _templateService.GetAllTemplatesAsync(enabledOnly: true);
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
                    t.IsDefault
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PromptTemplate] Failed to get templates");
            return StatusCode(500, new { error = "获取模板列表失败" });
        }
    }

    /// <summary>
    /// 按类别获取已启用的模板
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        try
        {
            // SaaS 前台只能获取已启用的模板
            var templates = await _templateService.GetTemplatesByCategoryAsync(category, enabledOnly: true);
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PromptTemplate] Failed to get templates by category {Category}", category);
            return StatusCode(500, new { error = "获取模板列表失败" });
        }
    }

    /// <summary>
    /// 获取模板详情（不含完整模板内容，仅元数据）
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var template = await _templateService.GetTemplateAsync(id);
            if (template == null || !template.IsEnabled)
            {
                return NotFound(new { error = "模板不存在" });
            }
            // 返回元数据，不暴露完整模板内容
            return Ok(new
            {
                success = true,
                data = new
                {
                    template.Id,
                    template.Name,
                    template.Description,
                    template.Category,
                    template.Version,
                    template.IsDefault,
                    template.Variables
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PromptTemplate] Failed to get template {Id}", id);
            return StatusCode(500, new { error = "获取模板失败" });
        }
    }

    #endregion

    #region 渲染 API

    /// <summary>
    /// 渲染模板（用户使用模板的核心功能）
    /// </summary>
    [HttpPost("{id}/render")]
    public async Task<IActionResult> Render(int id, [FromBody] Dictionary<string, string> variables)
    {
        try
        {
            var rendered = await _templateService.RenderTemplateAsync(id, variables);
            return Ok(new { success = true, data = new { rendered } });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PromptTemplate] Failed to render template {Id}", id);
            return StatusCode(500, new { error = "渲染模板失败" });
        }
    }

    /// <summary>
    /// 获取默认模板并渲染
    /// </summary>
    [HttpPost("category/{category}/render")]
    public async Task<IActionResult> RenderDefault(string category, [FromBody] Dictionary<string, string> variables)
    {
        try
        {
            var rendered = await _templateService.RenderDefaultTemplateAsync(category, variables);
            return Ok(new { success = true, data = new { rendered } });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PromptTemplate] Failed to render default template for category {Category}", category);
            return StatusCode(500, new { error = "渲染模板失败" });
        }
    }

    #endregion
}
