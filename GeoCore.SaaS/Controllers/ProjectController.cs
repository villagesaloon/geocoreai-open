using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GeoCore.Data.DbContext;
using GeoCore.Data.Repositories;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// GEO 项目管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IGeoProjectRepository _projectRepo;
    private readonly IGeoQuestionRepository _questionRepo;
    private readonly ISysSourcePlatformRepository _platformRepo;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IGeoProjectRepository projectRepo,
        IGeoQuestionRepository questionRepo,
        ISysSourcePlatformRepository platformRepo,
        ILogger<ProjectController> logger)
    {
        _projectRepo = projectRepo;
        _questionRepo = questionRepo;
        _platformRepo = platformRepo;
        _logger = logger;
    }

    #region 项目 CRUD

    /// <summary>
    /// 检查项目是否存在（按品牌名称）
    /// </summary>
    [HttpGet("check-exists")]
    public async Task<IActionResult> CheckProjectExists([FromQuery] string brandName)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            if (string.IsNullOrWhiteSpace(brandName))
            {
                return BadRequest(new { success = false, message = "品牌名称不能为空" });
            }

            var projects = await _projectRepo.GetProjectsByUserIdAsync(userId);
            var existingProject = projects.FirstOrDefault(p => 
                p.BrandName.Equals(brandName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existingProject != null)
            {
                return Ok(new { 
                    success = true, 
                    exists = true, 
                    projectId = existingProject.Id,
                    message = $"已存在同名项目「{existingProject.BrandName}」" 
                });
            }

            return Ok(new { success = true, exists = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查项目是否存在失败, BrandName={BrandName}", brandName);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 创建项目
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        _logger.LogWarning("========== [PROJECT] 开始创建项目 ==========");
        _logger.LogWarning("[PROJECT] 请求数据: BrandName={BrandName}, ProductName={ProductName}, Industry={Industry}, Config={HasConfig}, Competitors={CompetitorCount}, SellingPoints={SellingPointCount}, Personas={PersonaCount}, Stages={StageCount}",
            request.BrandName, request.ProductName, request.Industry, 
            request.Config != null ? "有" : "无",
            request.Competitors?.Count ?? 0, request.SellingPoints?.Count ?? 0,
            request.Personas?.Count ?? 0, request.Stages?.Count ?? 0);
        
        // 详细打印 Config 内容
        if (request.Config != null)
        {
            _logger.LogWarning("[PROJECT] Config.Countries: {Countries}", 
                request.Config.Countries != null ? System.Text.Json.JsonSerializer.Serialize(request.Config.Countries) : "null");
            _logger.LogWarning("[PROJECT] Config.Markets: {Markets}", 
                request.Config.Markets != null ? System.Text.Json.JsonSerializer.Serialize(request.Config.Markets) : "null");
            _logger.LogWarning("[PROJECT] Config.Languages: {Languages}", 
                request.Config.Languages != null ? System.Text.Json.JsonSerializer.Serialize(request.Config.Languages) : "null");
        }
        
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogWarning("[PROJECT] 用户ID: {UserId}", userId);
            
            if (userId == 0)
            {
                _logger.LogWarning("[PROJECT] 用户未登录，返回 401");
                return Unauthorized(new { success = false, message = "未登录" });
            }

            // 创建项目
            var project = new GeoProjectDto
            {
                UserId = userId,
                BrandName = request.BrandName,
                ProductName = request.ProductName,
                Industry = request.Industry,
                Description = request.Description,
                MonitorUrl = request.MonitorUrl,
                Status = "active"
            };

            var projectId = await _projectRepo.CreateProjectAsync(project);
            _logger.LogWarning("[PROJECT] 项目主记录创建成功, ProjectId={ProjectId}", projectId);

            // 保存配置
            if (request.Config != null)
            {
                _logger.LogWarning("[PROJECT] 保存项目配置: Countries={Countries}, Markets={Markets}, EffectiveCountries={EffectiveCountries}, Languages={Languages}, Models={Models}",
                    request.Config.Countries != null ? string.Join(",", request.Config.Countries) : "null",
                    request.Config.Markets != null ? string.Join(",", request.Config.Markets) : "null",
                    request.Config.EffectiveCountries != null ? string.Join(",", request.Config.EffectiveCountries) : "null",
                    request.Config.Languages != null ? string.Join(",", request.Config.Languages) : "null",
                    request.Config.Models != null ? string.Join(",", request.Config.Models) : "null");
                try
                {
                    request.Config.ProjectId = projectId;
                    await _projectRepo.SaveProjectConfigAsync(projectId, request.Config);
                    _logger.LogInformation("项目配置保存成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "项目配置保存失败");
                }
            }

            // 保存竞品
            if (request.Competitors != null && request.Competitors.Count > 0)
            {
                _logger.LogDebug("保存 {Count} 个竞品", request.Competitors.Count);
                try
                {
                    await _projectRepo.SaveCompetitorsAsync(projectId, request.Competitors);
                    _logger.LogInformation("竞品保存成功, Count={Count}", request.Competitors.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "竞品保存失败");
                }
            }

            // 保存卖点
            if (request.SellingPoints != null && request.SellingPoints.Count > 0)
            {
                _logger.LogWarning("保存 {Count} 个卖点，详情:", request.SellingPoints.Count);
                foreach (var sp in request.SellingPoints.Take(5))
                {
                    _logger.LogWarning("  卖点: Point={Point}, Country={Country}, Language={Language}", 
                        sp.Point?.Substring(0, Math.Min(20, sp.Point?.Length ?? 0)), sp.Country, sp.Language);
                }
                try
                {
                    await _projectRepo.SaveSellingPointsAsync(projectId, request.SellingPoints);
                    _logger.LogInformation("卖点保存成功, Count={Count}", request.SellingPoints.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "卖点保存失败");
                }
            }

            // 保存画像
            if (request.Personas != null && request.Personas.Count > 0)
            {
                _logger.LogDebug("保存 {Count} 个画像", request.Personas.Count);
                try
                {
                    await _projectRepo.SavePersonasAsync(projectId, request.Personas);
                    _logger.LogInformation("画像保存成功, Count={Count}", request.Personas.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "画像保存失败");
                }
            }

            // 保存阶段
            if (request.Stages != null && request.Stages.Count > 0)
            {
                _logger.LogDebug("保存 {Count} 个阶段", request.Stages.Count);
                try
                {
                    await _projectRepo.SaveStagesAsync(projectId, request.Stages);
                    _logger.LogInformation("阶段保存成功, Count={Count}", request.Stages.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "阶段保存失败");
                }
            }

            _logger.LogInformation("========== 创建项目完成: ProjectId={ProjectId}, BrandName={BrandName} ==========", projectId, request.BrandName);

            return Ok(new { success = true, data = new { projectId } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建项目失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取项目列表
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var projects = await _projectRepo.GetProjectsByUserIdAsync(userId);

            return Ok(new { success = true, data = projects });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取项目详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(long id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var project = await _projectRepo.GetProjectByIdAsync(id, userId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            // 加载关联数据
            project.Config = await _projectRepo.GetProjectConfigAsync(id);
            project.Competitors = await _projectRepo.GetCompetitorsAsync(id);
            project.SellingPoints = await _projectRepo.GetSellingPointsAsync(id);
            project.Personas = await _projectRepo.GetPersonasAsync(id);
            project.Stages = await _projectRepo.GetStagesAsync(id);

            return Ok(new { success = true, data = project });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取项目详情失败, ProjectId={ProjectId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProject(long id, [FromBody] CreateProjectRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var existing = await _projectRepo.GetProjectByIdAsync(id, userId);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            // 更新项目
            var project = new GeoProjectDto
            {
                Id = id,
                UserId = userId,
                BrandName = request.BrandName,
                ProductName = request.ProductName,
                Industry = request.Industry,
                Description = request.Description,
                MonitorUrl = request.MonitorUrl,
                Status = existing.Status
            };

            await _projectRepo.UpdateProjectAsync(project);

            // 更新配置
            if (request.Config != null)
            {
                request.Config.ProjectId = id;
                await _projectRepo.SaveProjectConfigAsync(id, request.Config);
            }

            // 更新竞品
            if (request.Competitors != null)
            {
                await _projectRepo.SaveCompetitorsAsync(id, request.Competitors);
            }

            // 更新卖点
            if (request.SellingPoints != null)
            {
                await _projectRepo.SaveSellingPointsAsync(id, request.SellingPoints);
            }

            // 更新画像
            if (request.Personas != null)
            {
                await _projectRepo.SavePersonasAsync(id, request.Personas);
            }

            // 更新阶段
            if (request.Stages != null)
            {
                await _projectRepo.SaveStagesAsync(id, request.Stages);
            }

            _logger.LogInformation("更新项目成功, ProjectId={ProjectId}", id);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新项目失败, ProjectId={ProjectId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 删除项目
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(long id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var result = await _projectRepo.DeleteProjectAsync(id, userId);
            if (!result)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            _logger.LogInformation("删除项目成功, ProjectId={ProjectId}", id);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除项目失败, ProjectId={ProjectId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region 问题相关

    /// <summary>
    /// 保存问题到项目
    /// </summary>
    [HttpPost("{id}/questions")]
    public async Task<IActionResult> SaveQuestions(long id, [FromBody] SaveQuestionsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var project = await _projectRepo.GetProjectByIdAsync(id, userId);
            if (project == null)
            {
                return NotFound(new { success = false, message = "项目不存在" });
            }

            var savedCount = 0;

            foreach (var q in request.Questions)
            {
                // 创建问题
                var question = new GeoQuestionDto
                {
                    UserId = userId,
                    ProjectId = id,
                    TaskId = request.TaskId,
                    Question = q.Question,
                    Language = q.Language ?? "zh_cn",
                    Pattern = q.Pattern,
                    Intent = q.Intent,
                    Stage = q.Stage,
                    Persona = q.Persona,
                    SellingPoint = q.SellingPoint,
                    QuestionSource = q.QuestionSource ?? "ai",
                    SourceDetail = q.SourceDetail,
                    SourceUrl = q.SourceUrl,
                    GoogleTrendsHeat = q.GoogleTrendsHeat
                };

                var questionId = await _questionRepo.CreateQuestionAsync(question);

                // 保存回答
                if (q.Answers != null)
                {
                    foreach (var a in q.Answers)
                    {
                        var answer = new GeoQuestionAnswerDto
                        {
                            UserId = userId,
                            QuestionId = questionId,
                            Model = a.Model,
                            Answer = a.Answer,
                            SearchIndex = a.SearchIndex,
                            BrandFitIndex = a.BrandFitIndex,
                            Score = a.Score,
                            BrandAnalysis = a.BrandAnalysis,
                            CitationDifficulty = a.CitationDifficulty,
                            AnswerMode = a.AnswerMode ?? "simulation"
                        };

                        var answerId = await _questionRepo.CreateAnswerAsync(answer);

                        // 保存来源
                        if (a.Sources != null)
                        {
                            foreach (var (s, idx) in a.Sources.Select((s, i) => (s, i)))
                            {
                                var source = new GeoQuestionSourceDto
                                {
                                    UserId = userId,
                                    QuestionId = questionId,
                                    AnswerId = answerId,
                                    Model = a.Model,
                                    Url = s.Url,
                                    Domain = ExtractDomain(s.Url),
                                    Title = s.Title,
                                    Snippet = s.Snippet,
                                    SourceType = s.SourceType,
                                    AuthorityScore = s.AuthorityScore,
                                    SortOrder = idx
                                };

                                // 尝试匹配平台
                                if (!string.IsNullOrEmpty(source.Domain))
                                {
                                    var platform = await _platformRepo.GetPlatformByDomainAsync(source.Domain);
                                    if (platform != null)
                                    {
                                        source.PlatformId = platform.Id;
                                        source.AuthorityScore = platform.AuthorityBaseScore;
                                    }
                                }

                                await _questionRepo.CreateSourceAsync(source);
                            }
                        }
                    }
                }

                savedCount++;
            }

            _logger.LogInformation("保存问题成功, ProjectId={ProjectId}, SavedCount={SavedCount}", id, savedCount);

            return Ok(new { success = true, data = new { savedCount } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存问题失败, ProjectId={ProjectId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 获取项目问题
    /// </summary>
    [HttpGet("{id}/questions")]
    public async Task<IActionResult> GetQuestions(long id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogWarning("[GetQuestions] ProjectId={ProjectId}, UserId={UserId}", id, userId);
            
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var project = await _projectRepo.GetProjectByIdAsync(id, userId);
            if (project == null)
            {
                _logger.LogWarning("[GetQuestions] 项目不存在或无权限: ProjectId={ProjectId}, UserId={UserId}", id, userId);
                return NotFound(new { success = false, message = "项目不存在" });
            }

            var questions = await _questionRepo.GetQuestionsByProjectIdAsync(id, userId);
            _logger.LogWarning("[GetQuestions] 查询到 {Count} 个问题", questions.Count);

            // 加载回答和来源
            foreach (var q in questions)
            {
                q.Answers = await _questionRepo.GetAnswersByQuestionIdAsync(q.Id);
                q.Sources = await _questionRepo.GetSourcesByQuestionIdAsync(q.Id);

                // 加载每个回答的来源
                if (q.Answers != null)
                {
                    foreach (var a in q.Answers)
                    {
                        a.Sources = await _questionRepo.GetSourcesByAnswerIdAsync(a.Id);
                    }
                }
            }

            return Ok(new { success = true, data = questions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取问题失败, ProjectId={ProjectId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 删除项目问题
    /// </summary>
    [HttpDelete("{id}/questions")]
    public async Task<IActionResult> DeleteQuestions(long id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            await _questionRepo.DeleteQuestionsByProjectIdAsync(id, userId);

            _logger.LogInformation("删除问题成功, ProjectId={ProjectId}", id);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除问题失败, ProjectId={ProjectId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region 来源平台

    /// <summary>
    /// 获取所有来源平台
    /// </summary>
    [HttpGet("platforms")]
    public async Task<IActionResult> GetPlatforms()
    {
        try
        {
            var platforms = await _platformRepo.GetAllPlatformsAsync();
            return Ok(new { success = true, data = platforms });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取平台列表失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 更新来源软文状态
    /// </summary>
    [HttpPut("sources/{sourceId}/status")]
    public async Task<IActionResult> UpdateSourceStatus(long sourceId, [FromBody] UpdateSourceStatusRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var result = await _questionRepo.UpdateSourceContentStatusAsync(sourceId, userId, request.Status);
            if (!result)
            {
                return NotFound(new { success = false, message = "来源不存在" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新来源状态失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 设置来源为软文目标
    /// </summary>
    [HttpPut("sources/{sourceId}/target")]
    public async Task<IActionResult> SetSourceAsTarget(long sourceId, [FromBody] SetSourceTargetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { success = false, message = "未登录" });
            }

            var result = await _questionRepo.SetSourceAsTargetAsync(sourceId, userId, request.IsTarget);
            if (!result)
            {
                return NotFound(new { success = false, message = "来源不存在" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置来源目标失败");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Helper

    private long GetCurrentUserId()
    {
        // 从 Header 获取用户 ID（由前端 Firebase 认证后传递）
        if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
        {
            if (long.TryParse(userIdHeader.FirstOrDefault(), out var userId))
            {
                return userId;
            }
        }

        // 商业系统：不使用默认用户 ID，必须登录
        return 0;
    }

    private static string? ExtractDomain(string url)
    {
        try
        {
            if (string.IsNullOrEmpty(url)) return null;
            var uri = new Uri(url);
            return uri.Host.Replace("www.", "");
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

#region Request DTOs

public class CreateProjectRequest
{
    public string BrandName { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? MonitorUrl { get; set; }
    public GeoProjectConfigDto? Config { get; set; }
    public List<GeoCompetitorDto>? Competitors { get; set; }
    public List<GeoSellingPointDto>? SellingPoints { get; set; }
    public List<GeoPersonaDto>? Personas { get; set; }
    public List<GeoStageDto>? Stages { get; set; }
}

public class SaveQuestionsRequest
{
    public string? TaskId { get; set; }
    public List<QuestionInput> Questions { get; set; } = new();
}

public class QuestionInput
{
    public string Question { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? Pattern { get; set; }
    public string? Intent { get; set; }
    public string? Stage { get; set; }
    public string? Persona { get; set; }
    public string? SellingPoint { get; set; }
    public string? QuestionSource { get; set; }
    public string? SourceDetail { get; set; }
    public string? SourceUrl { get; set; }
    public int? GoogleTrendsHeat { get; set; }
    public List<AnswerInput>? Answers { get; set; }
}

public class AnswerInput
{
    public string Model { get; set; } = string.Empty;
    public string? Answer { get; set; }
    public int SearchIndex { get; set; }
    public int BrandFitIndex { get; set; }
    public int Score { get; set; }
    public string? BrandAnalysis { get; set; }
    public string? CitationDifficulty { get; set; }
    public string? AnswerMode { get; set; }
    public List<SourceInput>? Sources { get; set; }
}

public class SourceInput
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Snippet { get; set; }
    public string? SourceType { get; set; }
    public int AuthorityScore { get; set; } = 50;
}

public class UpdateSourceStatusRequest
{
    public string Status { get; set; } = "none";
}

public class SetSourceTargetRequest
{
    public bool IsTarget { get; set; }
}

#endregion
