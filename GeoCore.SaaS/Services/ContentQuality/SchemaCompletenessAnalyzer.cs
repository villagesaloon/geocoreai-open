using System.Text.Json;
using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// Schema 完整度分析器 (3.18)
/// 原理：Schema 标记增强权威信号，提升 AI 引用率
/// </summary>
public class SchemaCompletenessAnalyzer
{
    private readonly ILogger<SchemaCompletenessAnalyzer> _logger;

    // Schema 类型及其必需/推荐字段
    private static readonly Dictionary<string, SchemaFieldRequirements> SchemaRequirements = new()
    {
        ["FAQPage"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "mainEntity" },
            Recommended = new[] { "name", "description" }
        },
        ["HowTo"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "name", "step" },
            Recommended = new[] { "description", "totalTime", "estimatedCost", "supply", "tool" }
        },
        ["Article"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "headline", "author" },
            Recommended = new[] { "datePublished", "dateModified", "image", "publisher", "description" }
        },
        ["Organization"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "name" },
            Recommended = new[] { "url", "logo", "description", "contactPoint", "sameAs" }
        },
        ["Product"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "name" },
            Recommended = new[] { "description", "image", "brand", "offers", "review", "aggregateRating" }
        },
        ["Person"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "name" },
            Recommended = new[] { "jobTitle", "worksFor", "url", "image", "sameAs" }
        },
        ["WebPage"] = new SchemaFieldRequirements
        {
            Required = new[] { "@type", "name" },
            Recommended = new[] { "description", "url", "datePublished", "dateModified" }
        }
    };

    public SchemaCompletenessAnalyzer(ILogger<SchemaCompletenessAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 分析 Schema 完整度
    /// </summary>
    /// <param name="htmlContent">HTML 内容（包含 Schema 标记）</param>
    /// <param name="contentType">内容类型：faq, howto, article, product</param>
    /// <param name="language">语言</param>
    public Task<SchemaCompletenessMetric> AnalyzeSchemaCompletenessAsync(
        string htmlContent, 
        string contentType = "article",
        string language = "zh")
    {
        var detectedSchemas = new List<DetectedSchema>();
        var suggestions = new List<SchemaSuggestion>();

        // 1. 提取 JSON-LD Schema
        var jsonLdSchemas = ExtractJsonLdSchemas(htmlContent);
        foreach (var schema in jsonLdSchemas)
        {
            var detected = AnalyzeSchema(schema);
            if (detected != null)
            {
                detectedSchemas.Add(detected);
            }
        }

        // 2. 提取 Microdata Schema
        var microdataSchemas = ExtractMicrodataSchemas(htmlContent);
        foreach (var schemaType in microdataSchemas)
        {
            if (!detectedSchemas.Any(d => d.Type == schemaType))
            {
                detectedSchemas.Add(new DetectedSchema
                {
                    Type = schemaType,
                    CompletenessScore = 5.0, // Microdata 无法详细分析
                    IsValid = true
                });
            }
        }

        // 3. 生成建议
        suggestions = GenerateSchemaSuggestions(detectedSchemas, contentType, language);

        // 4. 计算总体评分
        var hasSchema = detectedSchemas.Count > 0;
        var avgCompleteness = detectedSchemas.Count > 0 
            ? detectedSchemas.Average(s => s.CompletenessScore) 
            : 0;
        
        // 基础分 + Schema 数量加成 + 完整度加成
        var score = 0.0;
        if (hasSchema)
        {
            score = 3.0; // 有 Schema 基础分
            score += Math.Min(3.0, detectedSchemas.Count * 1.0); // 数量加成
            score += avgCompleteness / 10 * 4; // 完整度加成
        }
        else
        {
            score = 2.0; // 无 Schema 给基础分
        }

        var completenessPercent = detectedSchemas.Count > 0
            ? detectedSchemas.Average(s => s.CompletenessScore) * 10
            : 0;

        _logger.LogDebug("[SchemaCompletenessAnalyzer] 检测到 {Count} 个 Schema, 评分 {Score}",
            detectedSchemas.Count, score);

        return Task.FromResult(new SchemaCompletenessMetric
        {
            Score = Math.Round(score, 1),
            HasSchema = hasSchema,
            DetectedSchemas = detectedSchemas,
            Suggestions = suggestions,
            CompletenessPercent = Math.Round(completenessPercent, 1),
            HasFAQSchema = detectedSchemas.Any(s => s.Type == "FAQPage"),
            HasHowToSchema = detectedSchemas.Any(s => s.Type == "HowTo"),
            HasArticleSchema = detectedSchemas.Any(s => s.Type == "Article"),
            HasOrganizationSchema = detectedSchemas.Any(s => s.Type == "Organization")
        });
    }

    /// <summary>
    /// 基于纯文本内容分析（无 HTML）
    /// 根据内容特征建议 Schema
    /// </summary>
    public Task<SchemaCompletenessMetric> AnalyzeContentForSchemaAsync(
        string content,
        string language = "zh")
    {
        var suggestions = new List<SchemaSuggestion>();
        var isZh = language == "zh";

        // 检测内容特征并建议 Schema
        
        // 1. FAQ 检测
        var faqPatterns = isZh
            ? new[] { @"问[:：]", @"答[:：]", @"Q[:：]", @"A[:：]", @"\?.*\n" }
            : new[] { @"Q[:：]", @"A[:：]", @"\?.*\n", @"FAQ" };
        
        var hasFaqContent = faqPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase));
        if (hasFaqContent)
        {
            suggestions.Add(new SchemaSuggestion
            {
                SchemaType = "FAQPage",
                Reason = isZh ? "检测到问答格式内容" : "FAQ-style content detected",
                Priority = "high",
                ExpectedBenefit = isZh ? "FAQ Schema 可提升 AI 引用率 40%" : "FAQ Schema can increase AI citation by 40%"
            });
        }

        // 2. HowTo 检测
        var howToPatterns = isZh
            ? new[] { @"步骤\s*[1一]", @"第[一二三四五]步", @"如何", @"怎样", @"方法" }
            : new[] { @"step\s*1", @"how to", @"guide", @"tutorial" };
        
        var hasHowToContent = howToPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase));
        if (hasHowToContent)
        {
            suggestions.Add(new SchemaSuggestion
            {
                SchemaType = "HowTo",
                Reason = isZh ? "检测到步骤/教程内容" : "Step-by-step content detected",
                Priority = "high",
                ExpectedBenefit = isZh ? "HowTo Schema 可获得富媒体搜索结果" : "HowTo Schema enables rich search results"
            });
        }

        // 3. Article 检测（默认建议）
        if (content.Length > 500)
        {
            suggestions.Add(new SchemaSuggestion
            {
                SchemaType = "Article",
                Reason = isZh ? "长文内容建议添加 Article Schema" : "Long-form content should have Article Schema",
                Priority = "medium",
                ExpectedBenefit = isZh ? "Article Schema 增强作者权威性" : "Article Schema enhances author authority"
            });
        }

        // 4. Product 检测
        var productPatterns = isZh
            ? new[] { @"价格", @"购买", @"规格", @"型号", @"￥\d+" }
            : new[] { @"price", @"buy", @"specifications", @"\$\d+" };
        
        var hasProductContent = productPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase));
        if (hasProductContent)
        {
            suggestions.Add(new SchemaSuggestion
            {
                SchemaType = "Product",
                Reason = isZh ? "检测到产品相关内容" : "Product-related content detected",
                Priority = "medium",
                ExpectedBenefit = isZh ? "Product Schema 可显示价格和评分" : "Product Schema enables price and rating display"
            });
        }

        // 计算评分（基于建议数量，无实际 Schema）
        var score = 2.0 + Math.Min(3.0, suggestions.Count * 0.5);

        return Task.FromResult(new SchemaCompletenessMetric
        {
            Score = Math.Round(score, 1),
            HasSchema = false,
            DetectedSchemas = new List<DetectedSchema>(),
            Suggestions = suggestions,
            CompletenessPercent = 0,
            HasFAQSchema = false,
            HasHowToSchema = false,
            HasArticleSchema = false,
            HasOrganizationSchema = false
        });
    }

    #region 私有方法

    private List<JsonElement> ExtractJsonLdSchemas(string htmlContent)
    {
        var schemas = new List<JsonElement>();
        
        // 匹配 <script type="application/ld+json">...</script>
        var pattern = @"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script>";
        var matches = Regex.Matches(htmlContent, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            try
            {
                var jsonContent = match.Groups[1].Value.Trim();
                var doc = JsonDocument.Parse(jsonContent);
                schemas.Add(doc.RootElement.Clone());
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("[SchemaCompletenessAnalyzer] JSON-LD 解析失败: {Error}", ex.Message);
            }
        }

        return schemas;
    }

    private List<string> ExtractMicrodataSchemas(string htmlContent)
    {
        var types = new List<string>();
        
        // 匹配 itemtype="https://schema.org/..."
        var pattern = @"itemtype\s*=\s*[""']https?://schema\.org/([^""']+)[""']";
        var matches = Regex.Matches(htmlContent, pattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            var type = match.Groups[1].Value;
            if (!types.Contains(type))
            {
                types.Add(type);
            }
        }

        return types;
    }

    private DetectedSchema? AnalyzeSchema(JsonElement schema)
    {
        try
        {
            string? schemaType = null;
            
            // 获取 @type
            if (schema.TryGetProperty("@type", out var typeElement))
            {
                schemaType = typeElement.ValueKind == JsonValueKind.Array
                    ? typeElement[0].GetString()
                    : typeElement.GetString();
            }

            if (string.IsNullOrEmpty(schemaType))
                return null;

            // 检查字段完整度
            var missingRequired = new List<string>();
            var missingRecommended = new List<string>();

            if (SchemaRequirements.TryGetValue(schemaType, out var requirements))
            {
                foreach (var field in requirements.Required)
                {
                    if (!schema.TryGetProperty(field, out _))
                    {
                        missingRequired.Add(field);
                    }
                }

                foreach (var field in requirements.Recommended)
                {
                    if (!schema.TryGetProperty(field, out _))
                    {
                        missingRecommended.Add(field);
                    }
                }
            }

            // 计算完整度评分
            var totalFields = (requirements?.Required.Length ?? 0) + (requirements?.Recommended.Length ?? 0);
            var presentFields = totalFields - missingRequired.Count - missingRecommended.Count;
            var completenessScore = totalFields > 0 
                ? (double)presentFields / totalFields * 10 
                : 5.0;

            // 必需字段缺失扣分更多
            if (missingRequired.Count > 0)
            {
                completenessScore *= 0.5;
            }

            return new DetectedSchema
            {
                Type = schemaType,
                CompletenessScore = Math.Round(completenessScore, 1),
                MissingRequiredFields = missingRequired,
                MissingRecommendedFields = missingRecommended,
                IsValid = missingRequired.Count == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[SchemaCompletenessAnalyzer] Schema 分析失败: {Error}", ex.Message);
            return null;
        }
    }

    private List<SchemaSuggestion> GenerateSchemaSuggestions(
        List<DetectedSchema> detectedSchemas, 
        string contentType,
        string language)
    {
        var suggestions = new List<SchemaSuggestion>();
        var existingTypes = detectedSchemas.Select(s => s.Type).ToHashSet();
        var isZh = language == "zh";

        // 根据内容类型建议缺失的 Schema
        var recommendedSchemas = contentType.ToLower() switch
        {
            "faq" => new[] { "FAQPage", "WebPage" },
            "howto" => new[] { "HowTo", "Article" },
            "article" => new[] { "Article", "WebPage" },
            "product" => new[] { "Product", "Organization" },
            _ => new[] { "Article", "WebPage" }
        };

        foreach (var schemaType in recommendedSchemas)
        {
            if (!existingTypes.Contains(schemaType))
            {
                suggestions.Add(new SchemaSuggestion
                {
                    SchemaType = schemaType,
                    Reason = isZh 
                        ? $"根据内容类型 '{contentType}' 建议添加 {schemaType} Schema"
                        : $"Based on content type '{contentType}', {schemaType} Schema is recommended",
                    Priority = schemaType == recommendedSchemas[0] ? "high" : "medium",
                    ExpectedBenefit = GetSchemaBenefit(schemaType, isZh)
                });
            }
        }

        // 检查已有 Schema 的完整度问题
        foreach (var schema in detectedSchemas.Where(s => !s.IsValid))
        {
            suggestions.Add(new SchemaSuggestion
            {
                SchemaType = schema.Type,
                Reason = isZh 
                    ? $"{schema.Type} Schema 缺少必需字段: {string.Join(", ", schema.MissingRequiredFields)}"
                    : $"{schema.Type} Schema missing required fields: {string.Join(", ", schema.MissingRequiredFields)}",
                Priority = "high",
                ExpectedBenefit = isZh ? "修复后可提升 Schema 有效性" : "Fixing will improve Schema validity"
            });
        }

        return suggestions;
    }

    private string GetSchemaBenefit(string schemaType, bool isZh)
    {
        return schemaType switch
        {
            "FAQPage" => isZh ? "FAQ Schema 可提升 AI 引用率 40%" : "FAQ Schema can increase AI citation by 40%",
            "HowTo" => isZh ? "HowTo Schema 可获得富媒体搜索结果" : "HowTo Schema enables rich search results",
            "Article" => isZh ? "Article Schema 增强作者权威性" : "Article Schema enhances author authority",
            "Product" => isZh ? "Product Schema 可显示价格和评分" : "Product Schema enables price and rating display",
            "Organization" => isZh ? "Organization Schema 增强品牌可信度" : "Organization Schema enhances brand credibility",
            "WebPage" => isZh ? "WebPage Schema 提供基础页面信息" : "WebPage Schema provides basic page information",
            _ => isZh ? "Schema 标记可提升 AI 引用率" : "Schema markup can improve AI citation"
        };
    }

    #endregion

    private class SchemaFieldRequirements
    {
        public string[] Required { get; set; } = Array.Empty<string>();
        public string[] Recommended { get; set; } = Array.Empty<string>();
    }
}
