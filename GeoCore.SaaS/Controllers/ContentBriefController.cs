using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentBrief;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// 内容简报控制器 - 数据驱动的内容创作指南
/// </summary>
[ApiController]
[Route("api/content-brief")]
public class ContentBriefController : ControllerBase
{
    private readonly ILogger<ContentBriefController> _logger;
    private readonly ContentBriefService _briefService;

    public ContentBriefController(
        ILogger<ContentBriefController> logger,
        ContentBriefService briefService)
    {
        _logger = logger;
        _briefService = briefService;
    }

    /// <summary>
    /// 生成完整内容简报
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateBrief([FromBody] TopicAnalysisRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            if (request.TaskId <= 0)
            {
                return BadRequest(new { error = "任务ID无效" });
            }

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new { success = true, data = report });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to generate brief for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "生成内容简报失败" });
        }
    }

    /// <summary>
    /// 获取 AI 对齐主题推荐
    /// </summary>
    [HttpGet("topics/{taskId}")]
    public async Task<IActionResult> GetAlignedTopics(int taskId, [FromQuery] string brand, [FromQuery] string? topic = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new TopicAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                TargetTopic = topic ?? ""
            };

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    topics = report.RecommendedTopics,
                    totalTopics = report.RecommendedTopics.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to get aligned topics for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取主题推荐失败" });
        }
    }

    /// <summary>
    /// 获取标题结构建议
    /// </summary>
    [HttpGet("structure/{taskId}")]
    public async Task<IActionResult> GetHeadingStructure(
        int taskId, 
        [FromQuery] string brand, 
        [FromQuery] string? topic = null,
        [FromQuery] string contentType = "article",
        [FromQuery] int targetWordCount = 1500)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new TopicAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                TargetTopic = topic ?? "",
                ContentType = contentType,
                TargetWordCount = targetWordCount
            };

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new
            {
                success = true,
                data = report.SuggestedStructure
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to get heading structure for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取标题结构失败" });
        }
    }

    /// <summary>
    /// 获取可引用事实建议
    /// </summary>
    [HttpGet("facts/{taskId}")]
    public async Task<IActionResult> GetCitableFacts(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new TopicAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand
            };

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    facts = report.CitableFacts,
                    totalFacts = report.CitableFacts.Count,
                    byType = report.CitableFacts.GroupBy(f => f.FactType).ToDictionary(
                        g => g.Key,
                        g => g.Count()
                    )
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to get citable facts for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取引用事实失败" });
        }
    }

    /// <summary>
    /// 获取关键词建议
    /// </summary>
    [HttpGet("keywords/{taskId}")]
    public async Task<IActionResult> GetKeywordSuggestions(int taskId, [FromQuery] string brand, [FromQuery] string? topic = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new TopicAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                TargetTopic = topic ?? ""
            };

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    keywords = report.KeywordSuggestions,
                    primary = report.KeywordSuggestions.Where(k => k.KeywordType == "primary").ToList(),
                    secondary = report.KeywordSuggestions.Where(k => k.KeywordType == "secondary").ToList(),
                    longTail = report.KeywordSuggestions.Where(k => k.KeywordType == "long_tail").ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to get keyword suggestions for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取关键词建议失败" });
        }
    }

    /// <summary>
    /// 获取竞品分析
    /// </summary>
    [HttpGet("competitors/{taskId}")]
    public async Task<IActionResult> GetCompetitorAnalysis(int taskId, [FromQuery] string brand, [FromQuery] string competitors)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            if (string.IsNullOrWhiteSpace(competitors))
            {
                return BadRequest(new { error = "竞品列表不能为空" });
            }

            var competitorList = competitors.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();

            var request = new TopicAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand,
                Competitors = competitorList
            };

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new
            {
                success = true,
                data = report.CompetitorAnalysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to get competitor analysis for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取竞品分析失败" });
        }
    }

    /// <summary>
    /// 获取优化清单
    /// </summary>
    [HttpGet("checklist/{taskId}")]
    public async Task<IActionResult> GetOptimizationChecklist(int taskId, [FromQuery] string brand)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var request = new TopicAnalysisRequest
            {
                TaskId = taskId,
                Brand = brand
            };

            var report = await _briefService.GenerateBriefAsync(request);
            return Ok(new
            {
                success = true,
                data = new
                {
                    checklist = report.Checklist,
                    byCategory = report.Checklist.GroupBy(c => c.Category).ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    ),
                    requiredItems = report.Checklist.Where(c => c.IsRequired).ToList(),
                    optionalItems = report.Checklist.Where(c => !c.IsRequired).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to get optimization checklist for task {TaskId}", taskId);
            return StatusCode(500, new { error = "获取优化清单失败" });
        }
    }

    /// <summary>
    /// 导出内容简报
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> ExportBrief([FromBody] ContentBriefExportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var analysisRequest = new TopicAnalysisRequest
            {
                TaskId = request.TaskId,
                Brand = request.Brand,
                TargetTopic = request.TargetTopic ?? "",
                ContentType = request.ContentType ?? "article",
                TargetWordCount = request.TargetWordCount,
                Competitors = request.Competitors
            };

            var report = await _briefService.GenerateBriefAsync(analysisRequest);
            var export = _briefService.ExportBrief(report, request.Format ?? "markdown");

            return Ok(new
            {
                success = true,
                data = export
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to export brief for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "导出内容简报失败" });
        }
    }

    /// <summary>
    /// 下载内容简报文件
    /// </summary>
    [HttpPost("download")]
    public async Task<IActionResult> DownloadBrief([FromBody] ContentBriefExportRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Brand))
            {
                return BadRequest(new { error = "品牌名称不能为空" });
            }

            var analysisRequest = new TopicAnalysisRequest
            {
                TaskId = request.TaskId,
                Brand = request.Brand,
                TargetTopic = request.TargetTopic ?? "",
                ContentType = request.ContentType ?? "article",
                TargetWordCount = request.TargetWordCount,
                Competitors = request.Competitors
            };

            var report = await _briefService.GenerateBriefAsync(analysisRequest);
            var export = _briefService.ExportBrief(report, request.Format ?? "markdown");

            var contentType = request.Format switch
            {
                "html" => "text/html",
                "json" => "application/json",
                _ => "text/markdown"
            };

            return File(System.Text.Encoding.UTF8.GetBytes(export.Content), contentType, export.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ContentBrief] Failed to download brief for task {TaskId}", request.TaskId);
            return StatusCode(500, new { error = "下载内容简报失败" });
        }
    }
}

/// <summary>
/// 内容简报导出请求
/// </summary>
public class ContentBriefExportRequest
{
    public int TaskId { get; set; }
    public string Brand { get; set; } = "";
    public string? TargetTopic { get; set; }
    public string? ContentType { get; set; }
    public int TargetWordCount { get; set; } = 1500;
    public List<string>? Competitors { get; set; }
    public string? Format { get; set; } = "markdown";
}
