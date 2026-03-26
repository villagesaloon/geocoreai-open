using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// 数据配置管理控制器 - Admin 后台专用
/// 管理语言配置、提取模式、已知实体、情感关键词、关键词排除词等
/// </summary>
[ApiController]
[Route("api/admin/data-config")]
public class DataConfigAdminController : ControllerBase
{
    private readonly GeoDbContext _db;
    private readonly ILogger<DataConfigAdminController> _logger;

    public DataConfigAdminController(GeoDbContext db, ILogger<DataConfigAdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region 语言配置 (language_configs)

    /// <summary>
    /// 获取所有语言配置
    /// </summary>
    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages([FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<LanguageConfigEntity>();
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.SortOrder).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get languages");
            return StatusCode(500, new { error = "获取语言配置失败" });
        }
    }

    /// <summary>
    /// 创建语言配置
    /// </summary>
    [HttpPost("languages")]
    public async Task<IActionResult> CreateLanguage([FromBody] LanguageConfigEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建语言配置: {Code}", entity.LanguageCode);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create language");
            return StatusCode(500, new { error = "创建语言配置失败" });
        }
    }

    /// <summary>
    /// 更新语言配置
    /// </summary>
    [HttpPut("languages/{id}")]
    public async Task<IActionResult> UpdateLanguage(int id, [FromBody] LanguageConfigEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新语言配置: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update language {Id}", id);
            return StatusCode(500, new { error = "更新语言配置失败" });
        }
    }

    /// <summary>
    /// 删除语言配置
    /// </summary>
    [HttpDelete("languages/{id}")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        try
        {
            await _db.Client.Deleteable<LanguageConfigEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除语言配置: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete language {Id}", id);
            return StatusCode(500, new { error = "删除语言配置失败" });
        }
    }

    #endregion

    #region 提取模式 (extraction_patterns)

    /// <summary>
    /// 获取所有提取模式
    /// </summary>
    [HttpGet("extraction-patterns")]
    public async Task<IActionResult> GetExtractionPatterns([FromQuery] string? category = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<ExtractionPatternEntity>();
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(x => x.Category == category);
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.Category).OrderBy(x => x.SortOrder).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get extraction patterns");
            return StatusCode(500, new { error = "获取提取模式失败" });
        }
    }

    /// <summary>
    /// 创建提取模式
    /// </summary>
    [HttpPost("extraction-patterns")]
    public async Task<IActionResult> CreateExtractionPattern([FromBody] ExtractionPatternEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建提取模式: {Category}", entity.Category);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create extraction pattern");
            return StatusCode(500, new { error = "创建提取模式失败" });
        }
    }

    /// <summary>
    /// 更新提取模式
    /// </summary>
    [HttpPut("extraction-patterns/{id}")]
    public async Task<IActionResult> UpdateExtractionPattern(int id, [FromBody] ExtractionPatternEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新提取模式: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update extraction pattern {Id}", id);
            return StatusCode(500, new { error = "更新提取模式失败" });
        }
    }

    /// <summary>
    /// 删除提取模式
    /// </summary>
    [HttpDelete("extraction-patterns/{id}")]
    public async Task<IActionResult> DeleteExtractionPattern(int id)
    {
        try
        {
            await _db.Client.Deleteable<ExtractionPatternEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除提取模式: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete extraction pattern {Id}", id);
            return StatusCode(500, new { error = "删除提取模式失败" });
        }
    }

    #endregion

    #region 已知实体 (known_entities)

    /// <summary>
    /// 获取所有已知实体
    /// </summary>
    [HttpGet("known-entities")]
    public async Task<IActionResult> GetKnownEntities([FromQuery] string? entityType = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<KnownEntityEntity>();
            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(x => x.EntityType == entityType);
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.EntityType).OrderBy(x => x.EntityName).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get known entities");
            return StatusCode(500, new { error = "获取已知实体失败" });
        }
    }

    /// <summary>
    /// 创建已知实体
    /// </summary>
    [HttpPost("known-entities")]
    public async Task<IActionResult> CreateKnownEntity([FromBody] KnownEntityEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建已知实体: {Name}", entity.EntityName);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create known entity");
            return StatusCode(500, new { error = "创建已知实体失败" });
        }
    }

    /// <summary>
    /// 更新已知实体
    /// </summary>
    [HttpPut("known-entities/{id}")]
    public async Task<IActionResult> UpdateKnownEntity(int id, [FromBody] KnownEntityEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新已知实体: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update known entity {Id}", id);
            return StatusCode(500, new { error = "更新已知实体失败" });
        }
    }

    /// <summary>
    /// 删除已知实体
    /// </summary>
    [HttpDelete("known-entities/{id}")]
    public async Task<IActionResult> DeleteKnownEntity(int id)
    {
        try
        {
            await _db.Client.Deleteable<KnownEntityEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除已知实体: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete known entity {Id}", id);
            return StatusCode(500, new { error = "删除已知实体失败" });
        }
    }

    #endregion

    #region 情感关键词 (sentiment_keywords)

    /// <summary>
    /// 获取所有情感关键词
    /// </summary>
    [HttpGet("sentiment-keywords")]
    public async Task<IActionResult> GetSentimentKeywords([FromQuery] string? sentimentType = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<SentimentKeywordEntity>();
            if (!string.IsNullOrEmpty(sentimentType))
            {
                query = query.Where(x => x.SentimentType == sentimentType);
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.SentimentType).OrderBy(x => x.Keyword).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get sentiment keywords");
            return StatusCode(500, new { error = "获取情感关键词失败" });
        }
    }

    /// <summary>
    /// 创建情感关键词
    /// </summary>
    [HttpPost("sentiment-keywords")]
    public async Task<IActionResult> CreateSentimentKeyword([FromBody] SentimentKeywordEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建情感关键词: {Keyword}", entity.Keyword);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create sentiment keyword");
            return StatusCode(500, new { error = "创建情感关键词失败" });
        }
    }

    /// <summary>
    /// 更新情感关键词
    /// </summary>
    [HttpPut("sentiment-keywords/{id}")]
    public async Task<IActionResult> UpdateSentimentKeyword(int id, [FromBody] SentimentKeywordEntity entity)
    {
        try
        {
            entity.Id = id;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.Client.Updateable(entity).IgnoreColumns(x => x.CreatedAt).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 更新情感关键词: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to update sentiment keyword {Id}", id);
            return StatusCode(500, new { error = "更新情感关键词失败" });
        }
    }

    /// <summary>
    /// 删除情感关键词
    /// </summary>
    [HttpDelete("sentiment-keywords/{id}")]
    public async Task<IActionResult> DeleteSentimentKeyword(int id)
    {
        try
        {
            await _db.Client.Deleteable<SentimentKeywordEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除情感关键词: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete sentiment keyword {Id}", id);
            return StatusCode(500, new { error = "删除情感关键词失败" });
        }
    }

    #endregion

    #region 关键词排除词 (keyword_exclusions)

    /// <summary>
    /// 获取所有关键词排除词
    /// </summary>
    [HttpGet("keyword-exclusions")]
    public async Task<IActionResult> GetKeywordExclusions([FromQuery] string? languageCode = null, [FromQuery] bool enabledOnly = false)
    {
        try
        {
            var query = _db.Client.Queryable<KeywordExclusionEntity>();
            if (!string.IsNullOrEmpty(languageCode))
            {
                query = query.Where(x => x.LanguageCode == languageCode || x.LanguageCode == "global");
            }
            if (enabledOnly)
            {
                query = query.Where(x => x.IsEnabled);
            }
            var list = await query.OrderBy(x => x.LanguageCode).OrderBy(x => x.Category).OrderBy(x => x.Word).ToListAsync();
            return Ok(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to get keyword exclusions");
            return StatusCode(500, new { error = "获取关键词排除词失败" });
        }
    }

    /// <summary>
    /// 批量创建关键词排除词
    /// </summary>
    [HttpPost("keyword-exclusions/batch")]
    public async Task<IActionResult> BatchCreateKeywordExclusions([FromBody] BatchKeywordExclusionRequest request)
    {
        try
        {
            var words = request.Words.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim())
                .Where(w => !string.IsNullOrEmpty(w))
                .Distinct()
                .ToList();

            var entities = words.Select(w => new KeywordExclusionEntity
            {
                Word = w,
                LanguageCode = request.LanguageCode ?? "global",
                Category = request.Category ?? "common",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _db.Client.Insertable(entities).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 批量创建关键词排除词: {Count} 个", entities.Count);
            return Ok(new { success = true, count = entities.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to batch create keyword exclusions");
            return StatusCode(500, new { error = "批量创建关键词排除词失败" });
        }
    }

    /// <summary>
    /// 创建关键词排除词
    /// </summary>
    [HttpPost("keyword-exclusions")]
    public async Task<IActionResult> CreateKeywordExclusion([FromBody] KeywordExclusionEntity entity)
    {
        try
        {
            entity.CreatedAt = DateTime.UtcNow;
            var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
            _logger.LogInformation("[Admin] 创建关键词排除词: {Word}", entity.Word);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to create keyword exclusion");
            return StatusCode(500, new { error = "创建关键词排除词失败" });
        }
    }

    /// <summary>
    /// 删除关键词排除词
    /// </summary>
    [HttpDelete("keyword-exclusions/{id}")]
    public async Task<IActionResult> DeleteKeywordExclusion(int id)
    {
        try
        {
            await _db.Client.Deleteable<KeywordExclusionEntity>().Where(x => x.Id == id).ExecuteCommandAsync();
            _logger.LogInformation("[Admin] 删除关键词排除词: {Id}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Admin] Failed to delete keyword exclusion {Id}", id);
            return StatusCode(500, new { error = "删除关键词排除词失败" });
        }
    }

    #endregion
}

#region Request Models

public class BatchKeywordExclusionRequest
{
    public string Words { get; set; } = "";
    public string? LanguageCode { get; set; }
    public string? Category { get; set; }
}

#endregion
