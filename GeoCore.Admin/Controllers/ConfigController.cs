using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// 配置管理控制器 - 大模型配置 + 全局参数 CRUD
/// 修改时同时更新数据库并通知 SaaS 前台刷新缓存
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly ModelConfigRepository _modelRepo;
    private readonly SystemConfigRepository _systemRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigController> _logger;

    // SaaS 前台缓存刷新地址
    private const string SaaSCacheRefreshUrl = "http://localhost:8080/api/cache/refresh";
    private const string SaaSModelCacheRefreshUrl = "http://localhost:8080/api/cache/refresh/models";
    private const string SaaSSystemCacheRefreshUrl = "http://localhost:8080/api/cache/refresh/system";

    public ConfigController(
        ModelConfigRepository modelRepo,
        SystemConfigRepository systemRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigController> logger)
    {
        _modelRepo = modelRepo;
        _systemRepo = systemRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    #region 大模型配置 API

    /// <summary>
    /// 获取所有模型配置
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetAllModels()
    {
        var models = await _modelRepo.GetAllAsync();
        return Ok(new
        {
            success = true,
            data = models.Select(m => new
            {
                m.Id,
                m.ModelId,
                m.DisplayName,
                m.ApiEndpoint,
                apiKey = MaskApiKey(m.ApiKey),
                m.ModelName,
                m.Temperature,
                m.MaxTokens,
                m.IsEnabled,
                m.SortOrder,
                m.Description,
                m.InputPricePerMToken,
                m.OutputPricePerMToken,
                m.PriceCurrency,
                m.CreatedAt,
                m.UpdatedAt
            })
        });
    }

    /// <summary>
    /// 获取单个模型配置（含完整 API Key）
    /// </summary>
    [HttpGet("models/{id}")]
    public async Task<IActionResult> GetModel(int id)
    {
        var model = await _modelRepo.GetByIdAsync(id);
        if (model == null)
            return NotFound(new { success = false, message = "模型配置不存在" });

        return Ok(new { success = true, data = model });
    }

    /// <summary>
    /// 创建模型配置
    /// </summary>
    [HttpPost("models")]
    public async Task<IActionResult> CreateModel([FromBody] ModelConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ModelId))
            return BadRequest(new { success = false, message = "模型标识不能为空" });

        // 检查是否已存在
        if (await _modelRepo.ExistsByModelIdAsync(request.ModelId))
            return Conflict(new { success = false, message = $"模型标识 '{request.ModelId}' 已存在" });

        var entity = new ModelConfigEntity
        {
            ModelId = request.ModelId.ToLower(),
            DisplayName = request.DisplayName ?? request.ModelId,
            ApiEndpoint = request.ApiEndpoint ?? "",
            ApiKey = request.ApiKey ?? "",
            ModelName = request.ModelName ?? "",
            Temperature = request.Temperature ?? 0.7,
            MaxTokens = request.MaxTokens ?? 16384,
            IsEnabled = request.IsEnabled ?? true,
            SortOrder = request.SortOrder ?? 0,
            Description = request.Description,
            InputPricePerMToken = request.InputPricePerMToken,
            OutputPricePerMToken = request.OutputPricePerMToken,
            PriceCurrency = request.PriceCurrency ?? "USD"
        };

        var id = await _modelRepo.CreateAsync(entity);
        await NotifySaaSCacheRefreshAsync("models");

        _logger.LogInformation("[Admin] 创建模型配置: {ModelId} (id={Id})", entity.ModelId, id);
        return Ok(new { success = true, message = "创建成功", data = new { id } });
    }

    /// <summary>
    /// 更新模型配置
    /// </summary>
    [HttpPut("models/{id}")]
    public async Task<IActionResult> UpdateModel(int id, [FromBody] ModelConfigRequest request)
    {
        var existing = await _modelRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { success = false, message = "模型配置不存在" });

        // 更新字段（仅更新非空值）
        if (!string.IsNullOrEmpty(request.DisplayName)) existing.DisplayName = request.DisplayName;
        if (!string.IsNullOrEmpty(request.ApiEndpoint)) existing.ApiEndpoint = request.ApiEndpoint;
        if (!string.IsNullOrEmpty(request.ApiKey)) existing.ApiKey = request.ApiKey;
        if (!string.IsNullOrEmpty(request.ModelName)) existing.ModelName = request.ModelName;
        if (request.Temperature.HasValue) existing.Temperature = request.Temperature.Value;
        if (request.MaxTokens.HasValue) existing.MaxTokens = request.MaxTokens.Value;
        if (request.IsEnabled.HasValue) existing.IsEnabled = request.IsEnabled.Value;
        if (request.SortOrder.HasValue) existing.SortOrder = request.SortOrder.Value;
        if (request.Description != null) existing.Description = request.Description;
        if (request.InputPricePerMToken.HasValue) existing.InputPricePerMToken = request.InputPricePerMToken;
        if (request.OutputPricePerMToken.HasValue) existing.OutputPricePerMToken = request.OutputPricePerMToken;
        if (request.PriceCurrency != null) existing.PriceCurrency = request.PriceCurrency;

        await _modelRepo.UpdateAsync(existing);
        await NotifySaaSCacheRefreshAsync("models");

        _logger.LogInformation("[Admin] 更新模型配置: {ModelId} (id={Id})", existing.ModelId, id);
        return Ok(new { success = true, message = "更新成功" });
    }

    /// <summary>
    /// 删除模型配置
    /// </summary>
    [HttpDelete("models/{id}")]
    public async Task<IActionResult> DeleteModel(int id)
    {
        var existing = await _modelRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { success = false, message = "模型配置不存在" });

        await _modelRepo.DeleteAsync(id);
        await NotifySaaSCacheRefreshAsync("models");

        _logger.LogInformation("[Admin] 删除模型配置: {ModelId} (id={Id})", existing.ModelId, id);
        return Ok(new { success = true, message = "删除成功" });
    }

    #endregion

    #region 全局参数（system_configs）API

    /// <summary>
    /// 获取所有系统参数
    /// </summary>
    [HttpGet("system")]
    public async Task<IActionResult> GetAllSystemConfigs()
    {
        var configs = await _systemRepo.GetAllAsync();
        return Ok(new
        {
            success = true,
            data = configs.Select(c => new
            {
                c.Id,
                c.Category,
                c.ConfigKey,
                c.ConfigValue,
                c.Name,
                c.Description,
                c.ValueType,
                c.UpdatedAt
            })
        });
    }

    /// <summary>
    /// 获取指定分类的系统参数
    /// </summary>
    [HttpGet("system/category/{category}")]
    public async Task<IActionResult> GetSystemConfigsByCategory(string category)
    {
        var configs = await _systemRepo.GetByCategoryAsync(category);
        return Ok(new { success = true, data = configs });
    }

    /// <summary>
    /// 设置系统参数（存在则更新，不存在则创建）
    /// </summary>
    [HttpPost("system")]
    public async Task<IActionResult> SetSystemConfig([FromBody] SystemConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.ConfigKey))
            return BadRequest(new { success = false, message = "分类和键名不能为空" });

        await _systemRepo.SetValueAsync(
            request.Category,
            request.ConfigKey,
            request.ConfigValue ?? "",
            request.Name,
            request.Description
        );

        await NotifySaaSCacheRefreshAsync("system");

        _logger.LogInformation("[Admin] 设置系统参数: {Category}:{Key}={Value}",
            request.Category, request.ConfigKey, request.ConfigValue);
        return Ok(new { success = true, message = "保存成功" });
    }

    /// <summary>
    /// 批量设置系统参数
    /// </summary>
    [HttpPost("system/batch")]
    public async Task<IActionResult> BatchSetSystemConfigs([FromBody] List<SystemConfigRequest> requests)
    {
        foreach (var req in requests)
        {
            if (!string.IsNullOrWhiteSpace(req.Category) && !string.IsNullOrWhiteSpace(req.ConfigKey))
            {
                await _systemRepo.SetValueAsync(req.Category, req.ConfigKey, req.ConfigValue ?? "", req.Name, req.Description);
            }
        }

        await NotifySaaSCacheRefreshAsync("system");

        _logger.LogInformation("[Admin] 批量设置 {Count} 个系统参数", requests.Count);
        return Ok(new { success = true, message = $"已保存 {requests.Count} 个参数" });
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 通知 SaaS 前台刷新缓存
    /// </summary>
    private async Task NotifySaaSCacheRefreshAsync(string type)
    {
        try
        {
            var url = type switch
            {
                "models" => SaaSModelCacheRefreshUrl,
                "system" => SaaSSystemCacheRefreshUrl,
                _ => SaaSCacheRefreshUrl
            };

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Internal-Call", "admin");
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Admin] SaaS 缓存刷新成功 (type={Type})", type);
            }
            else
            {
                _logger.LogWarning("[Admin] SaaS 缓存刷新失败: HTTP {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Admin] 通知 SaaS 刷新缓存失败（SaaS 可能未启动）");
        }
    }

    /// <summary>
    /// 遮盖 API Key（只显示前6位和后4位）
    /// </summary>
    private static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 12) return "***";
        return apiKey[..6] + "***" + apiKey[^4..];
    }

    #endregion
}

#region Request Models

public class ModelConfigRequest
{
    public string? ModelId { get; set; }
    public string? DisplayName { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? ModelName { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public bool? IsEnabled { get; set; }
    public int? SortOrder { get; set; }
    public string? Description { get; set; }
    public decimal? InputPricePerMToken { get; set; }
    public decimal? OutputPricePerMToken { get; set; }
    public string? PriceCurrency { get; set; }
}

#endregion
