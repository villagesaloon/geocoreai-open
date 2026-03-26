using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// CMS 公开 API - SaaS 前端展示文章
/// Phase 10: 轻量 CMS
/// </summary>
[ApiController]
[Route("api/cms")]
public class CmsController : ControllerBase
{
    private readonly CmsArticleRepository _articleRepo;
    private readonly CmsCategoryRepository _categoryRepo;

    public CmsController(CmsArticleRepository articleRepo, CmsCategoryRepository categoryRepo)
    {
        _articleRepo = articleRepo;
        _categoryRepo = categoryRepo;
    }

    /// <summary>
    /// 获取已发布文章列表
    /// </summary>
    [HttpGet("articles")]
    public async Task<IActionResult> GetArticles([FromQuery] string? type = null, [FromQuery] string? category = null)
    {
        var articles = await _articleRepo.GetAllAsync(type, isPublished: true);
        
        if (!string.IsNullOrEmpty(category))
            articles = articles.Where(a => a.Category == category).ToList();
        
        return Ok(articles.Select(a => new {
            a.Id,
            a.ArticleType,
            a.Category,
            a.Slug,
            a.Title,
            a.Summary,
            a.CoverImage,
            a.Author,
            Tags = string.IsNullOrEmpty(a.Tags) ? Array.Empty<string>() : System.Text.Json.JsonSerializer.Deserialize<string[]>(a.Tags),
            a.IsFeatured,
            a.ViewCount,
            a.PublishedAt
        }));
    }

    /// <summary>
    /// 获取文章详情（通过 Slug）
    /// </summary>
    [HttpGet("articles/{slug}")]
    public async Task<IActionResult> GetArticle(string slug)
    {
        var article = await _articleRepo.GetBySlugAsync(slug);
        if (article == null)
            return NotFound(new { error = "文章不存在" });
        
        // 增加阅读量
        await _articleRepo.IncrementViewCountAsync(article.Id);
        
        return Ok(new {
            article.Id,
            article.ArticleType,
            article.Category,
            article.Slug,
            article.Title,
            article.Summary,
            article.Content,
            article.CoverImage,
            article.Author,
            Tags = string.IsNullOrEmpty(article.Tags) ? Array.Empty<string>() : System.Text.Json.JsonSerializer.Deserialize<string[]>(article.Tags),
            SeoTitle = article.SeoTitle ?? article.Title,
            SeoDescription = article.SeoDescription ?? article.Summary,
            article.ViewCount,
            article.PublishedAt
        });
    }

    /// <summary>
    /// 获取分类列表
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? type = null)
    {
        var categories = await _categoryRepo.GetAllAsync(type);
        return Ok(categories.Select(c => new {
            c.Id,
            c.ArticleType,
            c.Slug,
            c.Name,
            c.Description,
            c.Icon,
            c.SortOrder
        }));
    }
}
