using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.Persona;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Persona 买家角色管理控制器
/// </summary>
[ApiController]
[Route("api/persona")]
public class PersonaController : ControllerBase
{
    private readonly ILogger<PersonaController> _logger;
    private readonly PersonaService _service;

    public PersonaController(
        ILogger<PersonaController> logger,
        PersonaService service)
    {
        _logger = logger;
        _service = service;
    }

    /// <summary>
    /// 生成买家角色
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GeneratePersonas([FromBody] PersonaGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("[Persona] Generate request for brand: {Brand}, industry: {Industry}",
                request.Brand, request.Industry);

            if (string.IsNullOrWhiteSpace(request.Brand) && string.IsNullOrWhiteSpace(request.Industry))
            {
                return BadRequest(new { error = "品牌或行业至少需要填写一项" });
            }

            if (request.Count < 1 || request.Count > 5)
            {
                request.Count = 3;
            }

            var result = await _service.GeneratePersonasAsync(request);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Persona] Failed to generate personas");
            return StatusCode(500, new { error = "生成买家角色失败" });
        }
    }

    /// <summary>
    /// 获取 Persona 深度画像
    /// </summary>
    [HttpPost("profile")]
    public async Task<IActionResult> GetDeepProfile([FromBody] PersonaGenerationRequest request)
    {
        try
        {
            _logger.LogInformation("[Persona] Get deep profile for brand: {Brand}", request.Brand);

            request.IncludeDeepProfile = true;
            request.GenerateQuestions = false;

            var result = await _service.GeneratePersonasAsync(request);
            
            var profiles = result.Personas.Select(p => new
            {
                p.Id,
                p.Name,
                p.Title,
                p.Description,
                p.RoleType,
                p.Icon,
                Profile = new
                {
                    p.Profile.Goals,
                    p.Profile.PainPoints,
                    p.Profile.DecisionFactors,
                    p.Profile.InformationSources,
                    p.Profile.Objections,
                    p.Profile.PreferredContentTypes,
                    DecisionJourney = new
                    {
                        Awareness = p.Profile.DecisionJourney.Awareness,
                        Consideration = p.Profile.DecisionJourney.Consideration,
                        Decision = p.Profile.DecisionJourney.Decision,
                        PostPurchase = p.Profile.DecisionJourney.PostPurchase
                    }
                }
            });

            return Ok(new { success = true, data = profiles });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Persona] Failed to get deep profile");
            return StatusCode(500, new { error = "获取深度画像失败" });
        }
    }

    /// <summary>
    /// 从 Persona 反推问题
    /// </summary>
    [HttpPost("questions")]
    public async Task<IActionResult> GenerateQuestions([FromBody] PersonaQuestionRequest request)
    {
        try
        {
            _logger.LogInformation("[Persona] Generate questions for brand: {Brand}, count: {Count}",
                request.Brand, request.QuestionsPerPersona);

            var genRequest = new PersonaGenerationRequest
            {
                Brand = request.Brand,
                Product = request.Product,
                Industry = request.Industry,
                Description = request.Description,
                Language = request.Language,
                Count = request.PersonaCount,
                IncludeDeepProfile = true,
                GenerateQuestions = true,
                QuestionsPerPersona = request.QuestionsPerPersona
            };

            var result = await _service.GeneratePersonasAsync(genRequest);

            var questionsData = result.Personas.Select(p => new
            {
                Persona = new { p.Id, p.Name, p.Title, p.RoleType, p.Icon },
                Questions = p.Questions.Select(q => new
                {
                    q.Question,
                    q.Intent,
                    q.Stage,
                    q.Motivation,
                    q.ExpectedAnswerPoints,
                    q.BrandOpportunityScore
                })
            });

            var totalQuestions = result.Personas.Sum(p => p.Questions.Count);

            return Ok(new
            {
                success = true,
                data = new
                {
                    Brand = request.Brand,
                    TotalQuestions = totalQuestions,
                    PersonaQuestions = questionsData
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Persona] Failed to generate questions");
            return StatusCode(500, new { error = "生成问题失败" });
        }
    }

    /// <summary>
    /// 生成 Persona 可见度矩阵
    /// </summary>
    [HttpPost("visibility-matrix")]
    public async Task<IActionResult> GenerateVisibilityMatrix([FromBody] PersonaVisibilityMatrixRequest request)
    {
        try
        {
            _logger.LogInformation("[Persona] Generate visibility matrix for task: {TaskId}, brand: {Brand}",
                request.TaskId, request.Brand);

            if (request.TaskId <= 0)
            {
                return BadRequest(new { error = "任务ID无效" });
            }

            // 先生成 Personas
            var genRequest = new PersonaGenerationRequest
            {
                Brand = request.Brand,
                Product = request.Product,
                Industry = request.Industry,
                Count = request.PersonaCount,
                GenerateQuestions = false
            };

            var personaResult = await _service.GeneratePersonasAsync(genRequest);

            // 生成可见度矩阵
            var visibilityRequest = new PersonaVisibilityRequest
            {
                TaskId = request.TaskId,
                Brand = request.Brand,
                Personas = personaResult.Personas,
                Topics = request.Topics ?? new List<string>()
            };

            var matrix = await _service.GenerateVisibilityMatrixAsync(visibilityRequest);

            return Ok(new { success = true, data = matrix });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Persona] Failed to generate visibility matrix");
            return StatusCode(500, new { error = "生成可见度矩阵失败" });
        }
    }

    /// <summary>
    /// 获取 Persona 模板列表
    /// </summary>
    [HttpGet("templates")]
    public IActionResult GetTemplates([FromQuery] string? industry = null)
    {
        try
        {
            var request = new PersonaGenerationRequest
            {
                Industry = industry ?? "通用",
                Count = 5,
                GenerateQuestions = false
            };

            var personas = _service.GenerateTemplatePersonas(request);

            var templates = personas.Select(p => new
            {
                p.Name,
                p.Title,
                p.RoleType,
                p.Icon,
                p.Description,
                GoalsCount = p.Profile.Goals.Count,
                PainPointsCount = p.Profile.PainPoints.Count
            });

            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Persona] Failed to get templates");
            return StatusCode(500, new { error = "获取模板失败" });
        }
    }
}

/// <summary>
/// Persona 问题生成请求
/// </summary>
public class PersonaQuestionRequest
{
    public string Brand { get; set; } = "";
    public string Product { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Description { get; set; } = "";
    public string Language { get; set; } = "zh-CN";
    public int PersonaCount { get; set; } = 3;
    public int QuestionsPerPersona { get; set; } = 5;
}

/// <summary>
/// Persona 可见度矩阵请求
/// </summary>
public class PersonaVisibilityMatrixRequest
{
    public int TaskId { get; set; }
    public string Brand { get; set; } = "";
    public string Product { get; set; } = "";
    public string Industry { get; set; } = "";
    public int PersonaCount { get; set; } = 3;
    public List<string>? Topics { get; set; }
}
