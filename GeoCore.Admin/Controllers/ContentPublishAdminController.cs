using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// 内容模板管理 API (Admin)
/// Phase 8.1: 内容模板管理
/// </summary>
[ApiController]
[Route("api/content-templates")]
public class ContentTemplateAdminController : ControllerBase
{
    private readonly ContentTemplateRepository _repo;
    private readonly ILogger<ContentTemplateAdminController> _logger;

    public ContentTemplateAdminController(
        ContentTemplateRepository repo,
        ILogger<ContentTemplateAdminController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有内容模板
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _repo.GetAllAsync();
        return Ok(new { success = true, data = templates });
    }

    /// <summary>
    /// 按平台获取模板
    /// </summary>
    [HttpGet("platform/{platform}")]
    public async Task<IActionResult> GetByPlatform(string platform)
    {
        var templates = await _repo.GetByPlatformAsync(platform);
        return Ok(new { success = true, data = templates });
    }

    /// <summary>
    /// 获取单个模板
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var template = await _repo.GetByIdAsync(id);
        if (template == null)
            return NotFound(new { success = false, message = "模板不存在" });
        return Ok(new { success = true, data = template });
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContentTemplateEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Platform) || string.IsNullOrWhiteSpace(entity.TemplateName))
            return BadRequest(new { success = false, message = "平台和模板名称不能为空" });

        var id = await _repo.CreateAsync(entity);
        _logger.LogInformation("创建内容模板: {Platform}/{Name}", entity.Platform, entity.TemplateName);
        return Ok(new { success = true, data = new { id } });
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContentTemplateEntity entity)
    {
        entity.Id = id;
        var result = await _repo.UpdateAsync(entity);
        if (!result)
            return NotFound(new { success = false, message = "模板不存在或更新失败" });

        _logger.LogInformation("更新内容模板: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repo.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "模板不存在" });

        _logger.LogInformation("删除内容模板: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 获取支持的平台列表
    /// </summary>
    [HttpGet("platforms")]
    public IActionResult GetPlatforms()
    {
        var platforms = new[]
        {
            new { id = "reddit", name = "Reddit", icon = "fab fa-reddit" },
            new { id = "linkedin", name = "LinkedIn", icon = "fab fa-linkedin" },
            new { id = "medium", name = "Medium", icon = "fab fa-medium" },
            new { id = "twitter", name = "Twitter/X", icon = "fab fa-twitter" },
            new { id = "youtube", name = "YouTube", icon = "fab fa-youtube" }
        };
        return Ok(new { success = true, data = platforms });
    }
}

/// <summary>
/// 平台内容规则管理 API (Admin)
/// Phase 8.2: 平台内容规则
/// </summary>
[ApiController]
[Route("api/platform-content-rules")]
public class PlatformContentRuleAdminController : ControllerBase
{
    private readonly PlatformContentRuleRepository _repo;
    private readonly ILogger<PlatformContentRuleAdminController> _logger;

    public PlatformContentRuleAdminController(
        PlatformContentRuleRepository repo,
        ILogger<PlatformContentRuleAdminController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有规则
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rules = await _repo.GetAllAsync();
        return Ok(new { success = true, data = rules });
    }

    /// <summary>
    /// 按平台获取规则
    /// </summary>
    [HttpGet("platform/{platform}")]
    public async Task<IActionResult> GetByPlatform(string platform)
    {
        var rules = await _repo.GetByPlatformAsync(platform);
        return Ok(new { success = true, data = rules });
    }

    /// <summary>
    /// 获取单个规则
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rule = await _repo.GetByIdAsync(id);
        if (rule == null)
            return NotFound(new { success = false, message = "规则不存在" });
        return Ok(new { success = true, data = rule });
    }

    /// <summary>
    /// 创建规则
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlatformContentRuleEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Platform) || string.IsNullOrWhiteSpace(entity.RuleType))
            return BadRequest(new { success = false, message = "平台和规则类型不能为空" });

        var id = await _repo.CreateAsync(entity);
        _logger.LogInformation("创建平台规则: {Platform}/{Type}", entity.Platform, entity.RuleType);
        return Ok(new { success = true, data = new { id } });
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PlatformContentRuleEntity entity)
    {
        entity.Id = id;
        var result = await _repo.UpdateAsync(entity);
        if (!result)
            return NotFound(new { success = false, message = "规则不存在或更新失败" });

        _logger.LogInformation("更新平台规则: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repo.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "规则不存在" });

        _logger.LogInformation("删除平台规则: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 获取规则类型列表
    /// </summary>
    [HttpGet("rule-types")]
    public IActionResult GetRuleTypes()
    {
        var types = new[]
        {
            new { id = "char_limit", name = "字符限制", description = "内容最大字符数" },
            new { id = "word_limit", name = "字数限制", description = "内容最大字数" },
            new { id = "forbidden_words", name = "禁止词", description = "不允许出现的词汇" },
            new { id = "best_practices", name = "最佳实践", description = "平台推荐的内容规范" },
            new { id = "hashtag_limit", name = "标签限制", description = "最大标签数量" },
            new { id = "link_limit", name = "链接限制", description = "最大链接数量" },
            new { id = "self_promo_ratio", name = "自我推广比例", description = "允许的自我推广内容比例" }
        };
        return Ok(new { success = true, data = types });
    }
}

/// <summary>
/// 发布平台 App 配置管理 API (Admin)
/// Phase 8.4: 平台 App 配置
/// </summary>
[ApiController]
[Route("api/publish-platform-apps")]
public class PublishPlatformAppAdminController : ControllerBase
{
    private readonly PublishPlatformAppRepository _repo;
    private readonly ILogger<PublishPlatformAppAdminController> _logger;

    public PublishPlatformAppAdminController(
        PublishPlatformAppRepository repo,
        ILogger<PublishPlatformAppAdminController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有 App 配置
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var apps = await _repo.GetAllAsync();
        var result = apps.Select(a => new
        {
            a.Id,
            a.Platform,
            a.AppName,
            ClientId = MaskSecret(a.ClientId),
            ClientSecret = "********",
            a.RedirectUri,
            a.Scopes,
            a.ApiBaseUrl,
            a.IsActive,
            a.Notes,
            a.CreatedAt,
            a.UpdatedAt
        });
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 获取单个 App 配置
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _repo.GetByIdAsync(id);
        if (app == null)
            return NotFound(new { success = false, message = "App 配置不存在" });

        var result = new
        {
            app.Id,
            app.Platform,
            app.AppName,
            ClientId = MaskSecret(app.ClientId),
            ClientSecret = "********",
            app.RedirectUri,
            app.Scopes,
            app.ApiBaseUrl,
            app.IsActive,
            app.Notes,
            app.CreatedAt,
            app.UpdatedAt
        };
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// 创建 App 配置
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PublishPlatformAppEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Platform) || string.IsNullOrWhiteSpace(entity.ClientId))
            return BadRequest(new { success = false, message = "平台和 Client ID 不能为空" });

        var id = await _repo.CreateAsync(entity);
        _logger.LogInformation("创建平台 App 配置: {Platform}/{AppName}", entity.Platform, entity.AppName);
        return Ok(new { success = true, data = new { id } });
    }

    /// <summary>
    /// 更新 App 配置
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PublishPlatformAppEntity entity)
    {
        var existing = await _repo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { success = false, message = "App 配置不存在" });

        entity.Id = id;
        if (entity.ClientSecret == "********")
            entity.ClientSecret = existing.ClientSecret;

        var result = await _repo.UpdateAsync(entity);
        if (!result)
            return BadRequest(new { success = false, message = "更新失败" });

        _logger.LogInformation("更新平台 App 配置: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除 App 配置
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repo.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "App 配置不存在" });

        _logger.LogInformation("删除平台 App 配置: {Id}", id);
        return Ok(new { success = true });
    }

    private static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length <= 8)
            return "****";
        return secret[..4] + "****" + secret[^4..];
    }
}

/// <summary>
/// 发布规则管理 API (Admin)
/// Phase 8.5: 发布规则配置
/// </summary>
[ApiController]
[Route("api/publish-rules")]
public class PublishRuleAdminController : ControllerBase
{
    private readonly PublishRuleRepository _repo;
    private readonly ILogger<PublishRuleAdminController> _logger;

    public PublishRuleAdminController(
        PublishRuleRepository repo,
        ILogger<PublishRuleAdminController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有规则
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rules = await _repo.GetAllAsync();
        return Ok(new { success = true, data = rules });
    }

    /// <summary>
    /// 按平台获取规则
    /// </summary>
    [HttpGet("platform/{platform}")]
    public async Task<IActionResult> GetByPlatform(string platform)
    {
        var rules = await _repo.GetByPlatformAsync(platform);
        return Ok(new { success = true, data = rules });
    }

    /// <summary>
    /// 获取单个规则
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rule = await _repo.GetByIdAsync(id);
        if (rule == null)
            return NotFound(new { success = false, message = "规则不存在" });
        return Ok(new { success = true, data = rule });
    }

    /// <summary>
    /// 创建规则
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PublishRuleEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Platform) || string.IsNullOrWhiteSpace(entity.RuleType))
            return BadRequest(new { success = false, message = "平台和规则类型不能为空" });

        var id = await _repo.CreateAsync(entity);
        _logger.LogInformation("创建发布规则: {Platform}/{Type}", entity.Platform, entity.RuleType);
        return Ok(new { success = true, data = new { id } });
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PublishRuleEntity entity)
    {
        entity.Id = id;
        var result = await _repo.UpdateAsync(entity);
        if (!result)
            return NotFound(new { success = false, message = "规则不存在或更新失败" });

        _logger.LogInformation("更新发布规则: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _repo.DeleteAsync(id);
        if (!result)
            return NotFound(new { success = false, message = "规则不存在" });

        _logger.LogInformation("删除发布规则: {Id}", id);
        return Ok(new { success = true });
    }

    /// <summary>
    /// 获取规则类型列表
    /// </summary>
    [HttpGet("rule-types")]
    public IActionResult GetRuleTypes()
    {
        var types = new[]
        {
            new { id = "rate_limit", name = "频率限制", description = "每分钟最大请求数", unit = "次/分钟" },
            new { id = "daily_limit", name = "每日限制", description = "每天最大发布数", unit = "次/天" },
            new { id = "cooldown_minutes", name = "冷却时间", description = "两次发布间隔", unit = "分钟" },
            new { id = "karma_required", name = "Karma 要求", description = "Reddit 最低 Karma", unit = "点" }
        };
        return Ok(new { success = true, data = types });
    }
}
