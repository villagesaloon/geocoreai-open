using Microsoft.Extensions.Logging;

namespace GeoCore.SaaS.Services.Suggestion;

/// <summary>
/// 建议生成器接口
/// </summary>
public interface ISuggestionGenerator
{
    /// <summary>
    /// 根据检测上下文生成优化建议
    /// </summary>
    Task<List<DetectionSuggestion>> GenerateAsync(DetectionContext context, CancellationToken ct = default);

    /// <summary>
    /// 获取所有规则
    /// </summary>
    List<SuggestionRule> GetRules();
}

/// <summary>
/// 建议生成器实现 - 规则引擎
/// </summary>
public class SuggestionGenerator : ISuggestionGenerator
{
    private readonly List<SuggestionRule> _rules;
    private readonly ILogger<SuggestionGenerator> _logger;

    public SuggestionGenerator(ILogger<SuggestionGenerator> logger)
    {
        _logger = logger;
        _rules = SuggestionRules.GetAllRules();
    }

    public List<SuggestionRule> GetRules() => _rules;

    public Task<List<DetectionSuggestion>> GenerateAsync(DetectionContext context, CancellationToken ct = default)
    {
        var suggestions = new List<DetectionSuggestion>();

        foreach (var rule in _rules)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                if (EvaluateCondition(rule.Condition, context))
                {
                    var suggestion = CreateSuggestion(rule, context);
                    suggestions.Add(suggestion);
                    _logger.LogDebug("规则 {RuleId} 匹配成功: {Title}", rule.RuleId, rule.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "评估规则失败: {RuleId}", rule.RuleId);
            }
        }

        // 按优先级和影响分数排序
        var sortedSuggestions = suggestions
            .OrderByDescending(s => SuggestionPriority.GetOrder(s.Priority))
            .ThenByDescending(s => s.ImpactScore)
            .ToList();

        _logger.LogInformation("生成了 {Count} 条优化建议", sortedSuggestions.Count);
        return Task.FromResult(sortedSuggestions);
    }

    /// <summary>
    /// 评估条件表达式
    /// </summary>
    private bool EvaluateCondition(string condition, DetectionContext context)
    {
        // 简单的条件评估器
        // 支持基本的比较运算符和布尔值
        return condition switch
        {
            // AI 可见度条件
            "brand_mention_rate < 0.3" => context.BrandMentionRate < 0.3m,
            "avg_mention_position > 5" => context.AvgMentionPosition > 5,
            "user_site_cited == false" => !context.UserSiteCited,
            "citation_count < 100" => context.CitationCount < 100,
            "has_faq_page == false" => !context.HasFaqPage,

            // 网站技术条件
            "ai_crawlers_allowed == false" => context.WebsiteAudit?.AiCrawlersAllowed == false,
            "llms_txt_exists == false" => context.WebsiteAudit?.LlmsTxtExists == false,
            "sitemap_exists == false" => context.WebsiteAudit?.SitemapExists == false,
            "https_enabled == false" => context.WebsiteAudit?.HttpsEnabled == false,
            "has_canonical == false" => context.WebsiteAudit?.HasCanonical == false,
            "js_rendering_issue == true" => context.WebsiteAudit?.JsRenderingIssue == true,

            // 结构化数据条件
            "has_schema == false" => context.WebsiteAudit?.HasSchema == false,
            "has_faq_schema == false && has_faq_content == true" => 
                context.WebsiteAudit?.HasFaqSchema == false && context.HasFaqPage,

            // E-E-A-T 条件
            "has_author_info == false" => context.WebsiteAudit?.HasAuthorInfo == false,
            "has_publish_date == false" => context.WebsiteAudit?.HasPublishDate == false,
            "has_citations == false" => context.WebsiteAudit?.HasCitations == false,

            // 内容结构条件
            "heading_structure_ok == false" => context.WebsiteAudit?.HeadingStructureOk == false,
            "meta_title_ok == false" => context.WebsiteAudit?.MetaTitleOk == false,
            "has_meta_description == false" => context.WebsiteAudit?.HasMetaDescription == false,
            "has_answer_capsules == false" => context.WebsiteAudit?.HasAnswerCapsules == false,

            // SEO 条件
            "has_og_tags == false" => context.WebsiteAudit?.HasOgTags == false,
            "external_link_count < 3" => context.WebsiteAudit?.ExternalLinkCount < 3,

            _ => false
        };
    }

    /// <summary>
    /// 创建建议实例
    /// </summary>
    private DetectionSuggestion CreateSuggestion(SuggestionRule rule, DetectionContext context)
    {
        return new DetectionSuggestion
        {
            RuleId = rule.RuleId,
            Category = rule.Category,
            Subcategory = rule.Subcategory,
            Priority = rule.Priority,
            Title = InterpolateTemplate(rule.Title, context),
            Description = InterpolateTemplate(rule.Description, context),
            ImpactScore = rule.ImpactScore,
            EffortLevel = rule.EffortLevel,
            ActionItems = rule.ActionItems,
            ExampleCode = rule.ExampleCode,
            ReferenceUrls = rule.ReferenceUrls,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 模板变量替换
    /// </summary>
    private string InterpolateTemplate(string template, DetectionContext context)
    {
        if (string.IsNullOrEmpty(template)) return template;

        return template
            .Replace("{brand_mention_rate}", $"{context.BrandMentionRate * 100:F1}")
            .Replace("{avg_mention_position}", $"{context.AvgMentionPosition:F1}")
            .Replace("{citation_count}", context.CitationCount.ToString())
            .Replace("{blocked_crawlers}", string.Join(", ", 
                context.WebsiteAudit?.BlockedCrawlers ?? new List<string>()))
            .Replace("{brand_name}", context.BrandName)
            .Replace("{website_url}", context.WebsiteUrl);
    }
}
