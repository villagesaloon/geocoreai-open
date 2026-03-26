using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentQuality;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// Schema 生成控制器
/// 生成 FAQPage Schema 和 llms.txt
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SchemaController : ControllerBase
{
    private readonly ILogger<SchemaController> _logger;
    private readonly SchemaGenerator _schemaGenerator;

    public SchemaController(
        ILogger<SchemaController> logger,
        SchemaGenerator schemaGenerator)
    {
        _logger = logger;
        _schemaGenerator = schemaGenerator;
    }

    /// <summary>
    /// 生成 FAQPage Schema (JSON-LD)
    /// </summary>
    /// <param name="request">FAQ 问答对列表</param>
    /// <returns>JSON-LD 格式的 FAQPage Schema</returns>
    [HttpPost("faq")]
    public ActionResult<object> GenerateFaqSchema([FromBody] FaqSchemaRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "Items are required" });
        }

        _logger.LogInformation("[Schema] 生成 FAQ Schema, 问答对数量={Count}", request.Items.Count);

        var schemaJson = _schemaGenerator.GenerateFaqSchema(request.Items, request.PageUrl);

        return Ok(new
        {
            schema = schemaJson,
            itemCount = request.Items.Count,
            format = "JSON-LD",
            usage = "将此 Schema 添加到页面的 <script type=\"application/ld+json\"> 标签中"
        });
    }

    /// <summary>
    /// 生成 llms.txt 文件内容
    /// </summary>
    /// <param name="request">品牌信息</param>
    /// <returns>llms.txt 文件内容</returns>
    [HttpPost("llms-txt")]
    public ActionResult<object> GenerateLlmsTxt([FromBody] LlmsTxtRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { error = "BrandName is required" });
        }

        _logger.LogInformation("[Schema] 生成 llms.txt, 品牌={Brand}", request.BrandName);

        var content = _schemaGenerator.GenerateLlmsTxt(request);

        return Ok(new
        {
            content = content,
            filename = "llms.txt",
            usage = "将此文件放置在网站根目录 (例如 https://example.com/llms.txt)"
        });
    }

    /// <summary>
    /// 下载 llms.txt 文件
    /// </summary>
    [HttpPost("llms-txt/download")]
    public IActionResult DownloadLlmsTxt([FromBody] LlmsTxtRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BrandName))
        {
            return BadRequest(new { error = "BrandName is required" });
        }

        var content = _schemaGenerator.GenerateLlmsTxt(request);
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        return File(bytes, "text/plain", "llms.txt");
    }

    /// <summary>
    /// 获取 Schema 使用说明
    /// </summary>
    [HttpGet("help")]
    public ActionResult GetHelp()
    {
        return Ok(new
        {
            faqSchema = new
            {
                description = "FAQPage Schema 是一种结构化数据格式，帮助搜索引擎和 AI 理解页面上的问答内容",
                format = "JSON-LD",
                benefits = new[]
                {
                    "提高在 Google 搜索结果中显示 FAQ 富媒体摘要的机会",
                    "帮助 AI 助手更准确地引用你的内容",
                    "提升内容的可发现性和可引用性"
                },
                usage = "将生成的 JSON-LD 添加到页面的 <head> 或 <body> 中的 <script type=\"application/ld+json\"> 标签内"
            },
            llmsTxt = new
            {
                description = "llms.txt 是一种新兴标准，用于向 AI 系统提供关于你的品牌/产品的结构化信息",
                format = "Markdown",
                benefits = new[]
                {
                    "帮助 AI 助手了解你的品牌定位和核心信息",
                    "提供 AI 可以直接引用的权威内容",
                    "控制 AI 如何描述你的品牌"
                },
                usage = "将 llms.txt 文件放置在网站根目录 (例如 https://example.com/llms.txt)"
            }
        });
    }
}
