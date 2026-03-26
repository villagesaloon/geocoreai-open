using Microsoft.AspNetCore.Mvc;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 问题库管理控制器
/// </summary>
[ApiController]
[Route("api/question-library")]
public class QuestionLibraryController : ControllerBase
{
    private readonly ILogger<QuestionLibraryController> _logger;
    private readonly IQuestionLibraryRepository _repository;

    public QuestionLibraryController(
        ILogger<QuestionLibraryController> logger,
        IQuestionLibraryRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 获取所有问题
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? projectId = null)
    {
        try
        {
            var questions = await _repository.GetAllAsync(projectId);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to get questions");
            return StatusCode(500, new { error = "获取问题列表失败" });
        }
    }

    /// <summary>
    /// 搜索问题
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string keyword, 
        [FromQuery] string? projectId = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { error = "搜索关键词不能为空" });
            }

            var questions = await _repository.SearchAsync(keyword, projectId, limit);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to search questions");
            return StatusCode(500, new { error = "搜索问题失败" });
        }
    }

    /// <summary>
    /// 按类型获取问题
    /// </summary>
    [HttpGet("type/{questionType}")]
    public async Task<IActionResult> GetByType(string questionType, [FromQuery] string? projectId = null)
    {
        try
        {
            var questions = await _repository.GetByTypeAsync(questionType, projectId);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to get questions by type");
            return StatusCode(500, new { error = "获取问题失败" });
        }
    }

    /// <summary>
    /// 获取收藏的问题
    /// </summary>
    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites([FromQuery] string? projectId = null)
    {
        try
        {
            var questions = await _repository.GetFavoritesAsync(projectId);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to get favorite questions");
            return StatusCode(500, new { error = "获取收藏问题失败" });
        }
    }

    /// <summary>
    /// 获取最近使用的问题
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentlyUsed(
        [FromQuery] string? projectId = null,
        [FromQuery] int limit = 20)
    {
        try
        {
            var questions = await _repository.GetRecentlyUsedAsync(projectId, limit);
            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to get recent questions");
            return StatusCode(500, new { error = "获取最近使用问题失败" });
        }
    }

    /// <summary>
    /// 获取问题详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { error = "问题不存在" });
            }
            return Ok(new { success = true, data = question });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to get question {Id}", id);
            return StatusCode(500, new { error = "获取问题失败" });
        }
    }

    /// <summary>
    /// 添加问题
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuestionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { error = "问题内容不能为空" });
            }

            // 检查是否已存在
            if (await _repository.ExistsAsync(request.Question, request.ProjectId))
            {
                return BadRequest(new { error = "问题已存在" });
            }

            var entity = new QuestionLibraryEntity
            {
                ProjectId = request.ProjectId,
                Question = request.Question,
                Source = request.Source ?? "manual",
                QuestionType = request.QuestionType ?? "informational",
                Industry = request.Industry,
                Keywords = request.Keywords != null 
                    ? System.Text.Json.JsonSerializer.Serialize(request.Keywords) 
                    : null
            };

            var id = await _repository.CreateAsync(entity);
            return Ok(new { success = true, data = new { id } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to create question");
            return StatusCode(500, new { error = "添加问题失败" });
        }
    }

    /// <summary>
    /// 批量添加问题
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch([FromBody] BatchCreateQuestionRequest request)
    {
        try
        {
            if (request.Questions == null || !request.Questions.Any())
            {
                return BadRequest(new { error = "问题列表不能为空" });
            }

            var entities = new List<QuestionLibraryEntity>();
            var skipped = 0;

            foreach (var q in request.Questions)
            {
                if (string.IsNullOrWhiteSpace(q))
                    continue;

                if (await _repository.ExistsAsync(q, request.ProjectId))
                {
                    skipped++;
                    continue;
                }

                entities.Add(new QuestionLibraryEntity
                {
                    ProjectId = request.ProjectId,
                    Question = q,
                    Source = request.Source ?? "ai_generated",
                    QuestionType = request.QuestionType ?? "informational",
                    Industry = request.Industry
                });
            }

            if (entities.Any())
            {
                await _repository.CreateBatchAsync(entities);
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    added = entities.Count,
                    skipped
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to batch create questions");
            return StatusCode(500, new { error = "批量添加问题失败" });
        }
    }

    /// <summary>
    /// 更新问题
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateQuestionRequest request)
    {
        try
        {
            var question = await _repository.GetByIdAsync(id);
            if (question == null)
            {
                return NotFound(new { error = "问题不存在" });
            }

            if (!string.IsNullOrEmpty(request.Question))
                question.Question = request.Question;
            if (!string.IsNullOrEmpty(request.QuestionType))
                question.QuestionType = request.QuestionType;
            if (!string.IsNullOrEmpty(request.Industry))
                question.Industry = request.Industry;
            if (request.Keywords != null)
                question.Keywords = System.Text.Json.JsonSerializer.Serialize(request.Keywords);
            if (request.IsEnabled.HasValue)
                question.IsEnabled = request.IsEnabled.Value;

            await _repository.UpdateAsync(question);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to update question {Id}", id);
            return StatusCode(500, new { error = "更新问题失败" });
        }
    }

    /// <summary>
    /// 记录问题使用
    /// </summary>
    [HttpPost("{id}/use")]
    public async Task<IActionResult> RecordUsage(int id)
    {
        try
        {
            await _repository.IncrementUsageAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to record usage for question {Id}", id);
            return StatusCode(500, new { error = "记录使用失败" });
        }
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    [HttpPost("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        try
        {
            await _repository.ToggleFavoriteAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to toggle favorite for question {Id}", id);
            return StatusCode(500, new { error = "切换收藏状态失败" });
        }
    }

    /// <summary>
    /// 删除问题
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _repository.DeleteAsync(id);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QuestionLibrary] Failed to delete question {Id}", id);
            return StatusCode(500, new { error = "删除问题失败" });
        }
    }
}

public class CreateQuestionRequest
{
    public string? ProjectId { get; set; }
    public string Question { get; set; } = "";
    public string? Source { get; set; }
    public string? QuestionType { get; set; }
    public string? Industry { get; set; }
    public List<string>? Keywords { get; set; }
}

public class BatchCreateQuestionRequest
{
    public string? ProjectId { get; set; }
    public List<string> Questions { get; set; } = new();
    public string? Source { get; set; }
    public string? QuestionType { get; set; }
    public string? Industry { get; set; }
}

public class UpdateQuestionRequest
{
    public string? Question { get; set; }
    public string? QuestionType { get; set; }
    public string? Industry { get; set; }
    public List<string>? Keywords { get; set; }
    public bool? IsEnabled { get; set; }
}
