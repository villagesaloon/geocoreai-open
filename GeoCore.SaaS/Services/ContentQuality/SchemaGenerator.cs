using System.Text.Json;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// Schema 生成器
/// 生成 FAQPage Schema (JSON-LD) 和 llms.txt 文件
/// </summary>
public class SchemaGenerator
{
    private readonly ILogger<SchemaGenerator> _logger;

    public SchemaGenerator(ILogger<SchemaGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 生成 FAQPage Schema (JSON-LD 格式)
    /// </summary>
    public string GenerateFaqSchema(List<QAPair> qaPairs, string? pageUrl = null)
    {
        if (qaPairs == null || qaPairs.Count == 0)
        {
            _logger.LogWarning("[SchemaGenerator] 无问答对，无法生成 FAQ Schema");
            return "{}";
        }

        var schema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage"
        };

        if (!string.IsNullOrEmpty(pageUrl))
        {
            schema["url"] = pageUrl;
        }

        var mainEntity = qaPairs.Select(qa => new Dictionary<string, object>
        {
            ["@type"] = "Question",
            ["name"] = qa.Question,
            ["acceptedAnswer"] = new Dictionary<string, object>
            {
                ["@type"] = "Answer",
                ["text"] = qa.Answer
            }
        }).ToList();

        schema["mainEntity"] = mainEntity;

        var json = JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        _logger.LogDebug("[SchemaGenerator] 生成 FAQ Schema，包含 {Count} 个问答对", qaPairs.Count);

        return json;
    }

    /// <summary>
    /// 生成 llms.txt 文件内容
    /// </summary>
    public string GenerateLlmsTxt(LlmsTxtRequest request)
    {
        var lines = new List<string>
        {
            $"# {request.BrandName}",
            ""
        };

        // 品牌描述
        if (!string.IsNullOrEmpty(request.Description))
        {
            lines.Add($"> {request.Description}");
            lines.Add("");
        }

        // 核心产品
        if (request.Products?.Any() == true)
        {
            lines.Add("## 核心产品");
            lines.Add("");
            foreach (var product in request.Products)
            {
                if (!string.IsNullOrEmpty(product.Description))
                {
                    lines.Add($"- **{product.Name}**: {product.Description}");
                }
                else
                {
                    lines.Add($"- {product.Name}");
                }
            }
            lines.Add("");
        }

        // 主要卖点
        if (request.SellingPoints?.Any() == true)
        {
            lines.Add("## 主要卖点");
            lines.Add("");
            foreach (var sp in request.SellingPoints)
            {
                lines.Add($"- {sp}");
            }
            lines.Add("");
        }

        // 目标受众
        if (request.TargetAudiences?.Any() == true)
        {
            lines.Add("## 目标受众");
            lines.Add("");
            foreach (var audience in request.TargetAudiences)
            {
                lines.Add($"- {audience}");
            }
            lines.Add("");
        }

        // 联系方式
        if (!string.IsNullOrEmpty(request.Website) || !string.IsNullOrEmpty(request.Email))
        {
            lines.Add("## 联系方式");
            lines.Add("");
            if (!string.IsNullOrEmpty(request.Website))
            {
                lines.Add($"- 官网: {request.Website}");
            }
            if (!string.IsNullOrEmpty(request.Email))
            {
                lines.Add($"- 邮箱: {request.Email}");
            }
            lines.Add("");
        }

        // 常见问题
        if (request.FAQs?.Any() == true)
        {
            lines.Add("## 常见问题");
            lines.Add("");
            foreach (var faq in request.FAQs)
            {
                lines.Add($"### {faq.Question}");
                lines.Add("");
                lines.Add(faq.Answer);
                lines.Add("");
            }
        }

        // 附加信息
        if (request.AdditionalInfo?.Any() == true)
        {
            lines.Add("## 附加信息");
            lines.Add("");
            foreach (var kvp in request.AdditionalInfo)
            {
                lines.Add($"- **{kvp.Key}**: {kvp.Value}");
            }
            lines.Add("");
        }

        var content = string.Join("\n", lines);

        _logger.LogDebug("[SchemaGenerator] 生成 llms.txt，共 {Lines} 行", lines.Count);

        return content;
    }
}

/// <summary>
/// llms.txt 生成请求
/// </summary>
public class LlmsTxtRequest
{
    /// <summary>
    /// 品牌名称
    /// </summary>
    public string BrandName { get; set; } = "";

    /// <summary>
    /// 品牌描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 产品列表
    /// </summary>
    public List<ProductInfo>? Products { get; set; }

    /// <summary>
    /// 卖点列表
    /// </summary>
    public List<string>? SellingPoints { get; set; }

    /// <summary>
    /// 目标受众
    /// </summary>
    public List<string>? TargetAudiences { get; set; }

    /// <summary>
    /// 官网
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 常见问题
    /// </summary>
    public List<QAPair>? FAQs { get; set; }

    /// <summary>
    /// 附加信息
    /// </summary>
    public Dictionary<string, string>? AdditionalInfo { get; set; }
}

/// <summary>
/// 产品信息
/// </summary>
public class ProductInfo
{
    /// <summary>
    /// 产品名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 产品描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// FAQ Schema 生成请求
/// </summary>
public class FaqSchemaRequest
{
    /// <summary>
    /// 问答对列表
    /// </summary>
    public List<QAPair> Items { get; set; } = new();

    /// <summary>
    /// 页面 URL（可选）
    /// </summary>
    public string? PageUrl { get; set; }
}
