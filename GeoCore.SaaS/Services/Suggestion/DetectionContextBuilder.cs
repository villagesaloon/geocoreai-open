using GeoCore.SaaS.Services.SiteAudit;

namespace GeoCore.SaaS.Services.Suggestion;

/// <summary>
/// 检测上下文构建器 - 从审计结果构建上下文
/// </summary>
public class DetectionContextBuilder
{
    private readonly DetectionContext _context = new();

    /// <summary>
    /// 设置品牌信息
    /// </summary>
    public DetectionContextBuilder WithBrand(string brandName, string websiteUrl)
    {
        _context.BrandName = brandName;
        _context.WebsiteUrl = websiteUrl;
        return this;
    }

    /// <summary>
    /// 设置 AI 可见度数据
    /// </summary>
    public DetectionContextBuilder WithAiVisibility(
        decimal brandMentionRate = 0,
        decimal avgMentionPosition = 0,
        bool userSiteCited = false,
        int citationCount = 0)
    {
        _context.BrandMentionRate = brandMentionRate;
        _context.AvgMentionPosition = avgMentionPosition;
        _context.UserSiteCited = userSiteCited;
        _context.CitationCount = citationCount;
        return this;
    }

    /// <summary>
    /// 从 SiteAuditResult 构建网站审计上下文
    /// </summary>
    public DetectionContextBuilder FromSiteAuditResult(SiteAuditResult auditResult)
    {
        var audit = new WebsiteAuditContext
        {
            OverallScore = auditResult.OverallScore,
            Grade = auditResult.Grade
        };

        // 技术审计
        if (auditResult.Technical != null)
        {
            audit.TechnicalScore = auditResult.Technical.Score;

            // robots.txt
            if (auditResult.Technical.RobotsTxt != null)
            {
                audit.AiCrawlersAllowed = auditResult.Technical.RobotsTxt.AllAICrawlersAllowed;
                audit.BlockedCrawlers = auditResult.Technical.RobotsTxt.BlockedCrawlers;
            }

            // llms.txt
            if (auditResult.Technical.LlmsTxt != null)
            {
                audit.LlmsTxtExists = auditResult.Technical.LlmsTxt.Exists;
            }

            // sitemap
            if (auditResult.Technical.Sitemap != null)
            {
                audit.SitemapExists = auditResult.Technical.Sitemap.Exists;
                audit.SitemapUrlCount = auditResult.Technical.Sitemap.UrlCount;
            }

            // HTTPS/Canonical
            if (auditResult.Technical.HttpsCanonical != null)
            {
                audit.HttpsEnabled = auditResult.Technical.HttpsCanonical.IsHttps;
                audit.HasCanonical = auditResult.Technical.HttpsCanonical.HasCanonical;
            }
        }

        // 内容审计
        if (auditResult.Content != null)
        {
            audit.ContentScore = auditResult.Content.Score;

            // Schema
            if (auditResult.Content.Schema != null)
            {
                audit.HasSchema = auditResult.Content.Schema.HasSchema;
                audit.SchemaTypes = auditResult.Content.Schema.Schemas.Select(s => s.Type).ToList();
                audit.HasFaqSchema = auditResult.Content.Schema.HasFAQSchema;
            }

            // 标题结构
            if (auditResult.Content.HeadingStructure != null)
            {
                audit.H1Count = auditResult.Content.HeadingStructure.H1Count;
                audit.H2Count = auditResult.Content.HeadingStructure.H2Count;
                // H1 应该只有 1 个
                audit.HeadingStructureOk = auditResult.Content.HeadingStructure.H1Count == 1;
            }

            // Meta 标签
            if (auditResult.Content.MetaTags != null)
            {
                audit.HasMetaDescription = auditResult.Content.MetaTags.HasDescription;
                // Title 长度应在 50-60 之间
                var titleLength = auditResult.Content.MetaTags.TitleLength;
                audit.MetaTitleOk = titleLength >= 30 && titleLength <= 70;
            }

            // OG 标签（从 MetaTags 推断）
            audit.HasOgTags = auditResult.Content.MetaTags?.HasDescription == true;
        }

        // E-E-A-T 审计
        if (auditResult.EEAT != null)
        {
            audit.EeatScore = auditResult.EEAT.Score;
            audit.HasAuthorInfo = auditResult.EEAT.HasAuthorInfo;
            audit.HasPublishDate = auditResult.EEAT.HasPublishDate;
            audit.HasCitations = auditResult.EEAT.HasCitations;
            audit.ExternalLinkCount = auditResult.EEAT.ExternalLinkCount;
        }

        // 检测是否有 FAQ 页面（简单判断）
        _context.HasFaqPage = audit.HasFaqSchema || 
            auditResult.Content?.Schema?.HasFAQSchema == true;

        // Answer Capsule 检测（需要更复杂的内容分析，这里简化处理）
        audit.HasAnswerCapsules = false;

        _context.WebsiteAudit = audit;
        return this;
    }

    /// <summary>
    /// 构建上下文
    /// </summary>
    public DetectionContext Build() => _context;
}
