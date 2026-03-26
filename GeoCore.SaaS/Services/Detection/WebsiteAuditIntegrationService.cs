using System.Text.Json;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using GeoCore.SaaS.Services.SiteAudit;

namespace GeoCore.SaaS.Services.Detection;

/// <summary>
/// 网站审计集成服务
/// 整合 SiteAuditService 和 CrawlerService，将结果保存到数据库
/// </summary>
public interface IWebsiteAuditIntegrationService
{
    Task<GeoWebsiteAuditEntity> AuditAndSaveAsync(long projectId, string url, long? taskId = null);
    Task<GeoWebsiteAuditEntity?> GetCachedAuditAsync(long projectId);
}

/// <summary>
/// 网站审计集成服务实现
/// </summary>
public class WebsiteAuditIntegrationService : IWebsiteAuditIntegrationService
{
    private readonly SiteAuditService _siteAuditService;
    private readonly IWebsiteAuditRepository _auditRepository;
    private readonly ConfigCacheService _configCache;
    private readonly ILogger<WebsiteAuditIntegrationService> _logger;

    public WebsiteAuditIntegrationService(
        SiteAuditService siteAuditService,
        IWebsiteAuditRepository auditRepository,
        ConfigCacheService configCache,
        ILogger<WebsiteAuditIntegrationService> logger)
    {
        _siteAuditService = siteAuditService;
        _auditRepository = auditRepository;
        _configCache = configCache;
        _logger = logger;
    }

    /// <summary>
    /// 执行审计并保存结果
    /// </summary>
    public async Task<GeoWebsiteAuditEntity> AuditAndSaveAsync(long projectId, string url, long? taskId = null)
    {
        _logger.LogInformation("[WebsiteAudit] Starting audit for project {ProjectId}: {Url}", projectId, url);

        // 执行审计
        var auditResult = await _siteAuditService.AuditSiteAsync(new SiteAuditRequest
        {
            Url = url,
            IncludeTechnical = true,
            IncludeContent = true,
            IncludeEEAT = true
        });

        // 转换为数据库实体
        var entity = ConvertToEntity(projectId, url, taskId, auditResult);

        // 保存到数据库
        entity.Id = await _auditRepository.CreateAsync(entity);

        _logger.LogInformation("[WebsiteAudit] Audit completed for project {ProjectId}: score={Score}, grade={Grade}",
            projectId, entity.OverallScore, entity.Grade);

        return entity;
    }

    /// <summary>
    /// 获取缓存的审计结果
    /// </summary>
    public async Task<GeoWebsiteAuditEntity?> GetCachedAuditAsync(long projectId)
    {
        return await _auditRepository.GetCachedByProjectAsync(projectId);
    }

    /// <summary>
    /// 将审计结果转换为数据库实体
    /// </summary>
    private GeoWebsiteAuditEntity ConvertToEntity(long projectId, string url, long? taskId, SiteAuditResult result)
    {
        var cacheHours = _configCache.GetSystemIntValue("detection", "audit_cache_hours", 24);

        var entity = new GeoWebsiteAuditEntity
        {
            ProjectId = projectId,
            TaskId = taskId,
            Url = url,
            OverallScore = result.OverallScore,
            Grade = result.Grade,
            CacheExpiresAt = DateTime.UtcNow.AddHours(cacheHours)
        };

        // 技术审计
        if (result.Technical != null)
        {
            entity.TechnicalScore = result.Technical.Score;

            // robots.txt
            if (result.Technical.RobotsTxt != null)
            {
                entity.RobotsTxtExists = result.Technical.RobotsTxt.Exists;
                entity.RobotsTxtContent = result.Technical.RobotsTxt.Content;
                entity.AiCrawlersAllowed = result.Technical.RobotsTxt.AllAICrawlersAllowed;
                entity.BlockedCrawlers = result.Technical.RobotsTxt.BlockedCrawlers.Count > 0
                    ? JsonSerializer.Serialize(result.Technical.RobotsTxt.BlockedCrawlers)
                    : null;
            }

            // Sitemap
            if (result.Technical.Sitemap != null)
            {
                entity.SitemapExists = result.Technical.Sitemap.Exists;
                entity.SitemapUrlCount = result.Technical.Sitemap.UrlCount;
            }

            // llms.txt
            if (result.Technical.LlmsTxt != null)
            {
                entity.LlmsTxtExists = result.Technical.LlmsTxt.Exists;
                entity.LlmsTxtEntryCount = result.Technical.LlmsTxt.EntryCount;
            }

            // HTTPS/Canonical
            if (result.Technical.HttpsCanonical != null)
            {
                entity.HttpsEnabled = result.Technical.HttpsCanonical.IsHttps;
                entity.HasCanonical = result.Technical.HttpsCanonical.HasCanonical;
            }

            // JS Rendering
            if (result.Technical.JSRendering != null)
            {
                entity.JsRenderingIssue = result.Technical.JSRendering.CriticalContentInJS;
            }
        }

        // 内容审计
        if (result.Content != null)
        {
            entity.ContentScore = result.Content.Score;

            // Schema
            if (result.Content.Schema != null)
            {
                entity.HasSchema = result.Content.Schema.HasSchema;
                entity.HasArticleSchema = result.Content.Schema.HasArticleSchema;
                entity.HasFaqSchema = result.Content.Schema.HasFAQSchema;
                entity.SchemaTypes = result.Content.Schema.Schemas.Count > 0
                    ? JsonSerializer.Serialize(result.Content.Schema.Schemas.Select(s => s.Type))
                    : null;
            }

            // Answer Capsules
            if (result.Content.AnswerCapsule != null)
            {
                entity.HasAnswerCapsules = result.Content.AnswerCapsule.HasAnswerCapsules;
                entity.AnswerCapsuleCoverage = (decimal)result.Content.AnswerCapsule.CoveragePercentage;
            }

            // Heading Structure
            if (result.Content.HeadingStructure != null)
            {
                entity.HeadingStructureOk = result.Content.HeadingStructure.HasProperHierarchy;
                entity.H1Count = result.Content.HeadingStructure.H1Count;
                entity.H2Count = result.Content.HeadingStructure.H2Count;
            }

            // Meta Tags
            if (result.Content.MetaTags != null)
            {
                entity.MetaTitleOk = result.Content.MetaTags.HasTitle && result.Content.MetaTags.TitleOptimal;
                entity.MetaDescriptionOk = result.Content.MetaTags.HasDescription && result.Content.MetaTags.DescriptionOptimal;
                entity.HasOgTags = result.Content.MetaTags.HasOgTags;
            }
        }

        // E-E-A-T 审计
        if (result.EEAT != null)
        {
            entity.EeatScore = result.EEAT.Score;
            entity.HasAuthorInfo = result.EEAT.HasAuthorInfo;
            entity.HasPublishDate = result.EEAT.HasPublishDate;
            entity.HasUpdateDate = result.EEAT.HasUpdateDate;
            entity.HasCitations = result.EEAT.HasCitations;
            entity.ExternalLinkCount = result.EEAT.ExternalLinkCount;
        }

        // Issues 和 Recommendations
        if (result.Issues.Count > 0)
        {
            entity.Issues = JsonSerializer.Serialize(result.Issues.Select(i => new
            {
                i.Category,
                i.Severity,
                i.Title,
                i.Description
            }));
        }

        if (result.Recommendations.Count > 0)
        {
            entity.Recommendations = JsonSerializer.Serialize(result.Recommendations);
        }

        return entity;
    }
}
