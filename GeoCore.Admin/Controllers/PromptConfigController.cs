using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// Prompt 配置管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PromptConfigController : ControllerBase
{
    private readonly PromptConfigRepository _promptRepo;
    private readonly SystemConfigRepository _systemRepo;
    private readonly ILogger<PromptConfigController> _logger;

    public PromptConfigController(
        PromptConfigRepository promptRepo,
        SystemConfigRepository systemRepo,
        ILogger<PromptConfigController> logger)
    {
        _promptRepo = promptRepo;
        _systemRepo = systemRepo;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有 Prompt 配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var configs = await _promptRepo.GetAllAsync();
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 按分类获取 Prompt 配置
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var configs = await _promptRepo.GetByCategoryAsync(category);
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 获取单个 Prompt 配置
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var configs = await _promptRepo.GetAllAsync();
        var config = configs.FirstOrDefault(x => x.Id == id);
        if (config == null)
            return NotFound(new { success = false, message = "配置不存在" });
        return Ok(new { success = true, data = config });
    }

    /// <summary>
    /// 创建 Prompt 配置
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PromptConfigEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Category) || string.IsNullOrWhiteSpace(entity.ConfigKey))
            return BadRequest(new { success = false, message = "分类和键名不能为空" });

        var id = await _promptRepo.CreateAsync(entity);
        _logger.LogInformation("创建 Prompt 配置: {Category}/{Key}", entity.Category, entity.ConfigKey);
        return Ok(new { success = true, data = new { id } });
    }

    /// <summary>
    /// 更新 Prompt 配置
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PromptConfigEntity entity)
    {
        entity.Id = id;
        var result = await _promptRepo.UpdateAsync(entity);
        if (!result)
            return NotFound(new { success = false, message = "配置不存在或更新失败" });
        
        _logger.LogInformation("更新 Prompt 配置: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除 Prompt 配置
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _promptRepo.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "配置不存在" });
        
        _logger.LogInformation("删除 Prompt 配置: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 获取所有分类列表
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var configs = await _promptRepo.GetAllAsync();
        var categories = configs.Select(x => x.Category).Distinct().ToList();
        return Ok(new { success = true, data = categories });
    }
}

/// <summary>
/// 系统参数配置管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemConfigController : ControllerBase
{
    private readonly SystemConfigRepository _repo;
    private readonly ILogger<SystemConfigController> _logger;

    public SystemConfigController(SystemConfigRepository repo, ILogger<SystemConfigController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var configs = await _repo.GetAllAsync();
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 按分类获取系统配置
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        var configs = await _repo.GetByCategoryAsync(category);
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SetValue([FromBody] SystemConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.ConfigKey))
            return BadRequest(new { success = false, message = "分类和键名不能为空" });

        await _repo.SetValueAsync(request.Category, request.ConfigKey, request.ConfigValue, request.Name, request.Description);
        _logger.LogInformation("设置系统配置: {Category}/{Key} = {Value}", request.Category, request.ConfigKey, request.ConfigValue);
        return Ok(new { success = true });
    }
}

public class SystemConfigRequest
{
    public string Category { get; set; } = "";
    public string ConfigKey { get; set; } = "";
    public string ConfigValue { get; set; } = "";
    public string? Name { get; set; }
    public string? Description { get; set; }
}
