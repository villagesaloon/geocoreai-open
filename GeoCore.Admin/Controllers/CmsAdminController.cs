using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GeoCore.Admin.Controllers;

/// <summary>
/// CMS 管理 API - Admin 后台管理文章和分类
/// Phase 10: 轻量 CMS
/// </summary>
[ApiController]
[Route("api/admin/cms")]
public class CmsAdminController : ControllerBase
{
    private readonly CmsArticleRepository _articleRepo;
    private readonly CmsCategoryRepository _categoryRepo;

    public CmsAdminController(CmsArticleRepository articleRepo, CmsCategoryRepository categoryRepo)
    {
        _articleRepo = articleRepo;
        _categoryRepo = categoryRepo;
    }

    #region 文章管理

    [HttpGet("articles")]
    public async Task<IActionResult> GetArticles([FromQuery] string? type = null, [FromQuery] bool? published = null)
    {
        var articles = await _articleRepo.GetAllAsync(type, published);
        return Ok(articles);
    }

    [HttpGet("articles/{id}")]
    public async Task<IActionResult> GetArticle(int id)
    {
        var article = await _articleRepo.GetByIdAsync(id);
        if (article == null)
            return NotFound(new { error = "文章不存在" });
        return Ok(article);
    }

    [HttpPost("articles")]
    public async Task<IActionResult> CreateArticle([FromBody] CmsArticleEntity article)
    {
        if (string.IsNullOrEmpty(article.Title))
            return BadRequest(new { error = "标题不能为空" });
        
        if (string.IsNullOrEmpty(article.Slug))
            article.Slug = GenerateSlug(article.Title);
        
        var id = await _articleRepo.CreateAsync(article);
        return Ok(new { id, message = "文章创建成功" });
    }

    [HttpPut("articles/{id}")]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] CmsArticleEntity article)
    {
        var existing = await _articleRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { error = "文章不存在" });
        
        article.Id = id;
        article.CreatedAt = existing.CreatedAt;
        article.ViewCount = existing.ViewCount;
        
        var success = await _articleRepo.UpdateAsync(article);
        return Ok(new { success, message = success ? "更新成功" : "更新失败" });
    }

    [HttpDelete("articles/{id}")]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var success = await _articleRepo.DeleteAsync(id);
        return Ok(new { success, message = success ? "删除成功" : "删除失败" });
    }

    [HttpPost("articles/{id}/publish")]
    public async Task<IActionResult> PublishArticle(int id)
    {
        var article = await _articleRepo.GetByIdAsync(id);
        if (article == null)
            return NotFound(new { error = "文章不存在" });
        
        article.IsPublished = true;
        article.PublishedAt = DateTime.UtcNow;
        var success = await _articleRepo.UpdateAsync(article);
        return Ok(new { success, message = success ? "发布成功" : "发布失败" });
    }

    [HttpPost("articles/{id}/unpublish")]
    public async Task<IActionResult> UnpublishArticle(int id)
    {
        var article = await _articleRepo.GetByIdAsync(id);
        if (article == null)
            return NotFound(new { error = "文章不存在" });
        
        article.IsPublished = false;
        var success = await _articleRepo.UpdateAsync(article);
        return Ok(new { success, message = success ? "取消发布成功" : "操作失败" });
    }

    #endregion

    #region 分类管理

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories([FromQuery] string? type = null)
    {
        var categories = await _categoryRepo.GetAllAsync(type);
        return Ok(categories);
    }

    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _categoryRepo.GetByIdAsync(id);
        if (category == null)
            return NotFound(new { error = "分类不存在" });
        return Ok(category);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CmsCategoryEntity category)
    {
        if (string.IsNullOrEmpty(category.Name))
            return BadRequest(new { error = "分类名称不能为空" });
        
        if (string.IsNullOrEmpty(category.Slug))
            category.Slug = GenerateSlug(category.Name);
        
        var id = await _categoryRepo.CreateAsync(category);
        return Ok(new { id, message = "分类创建成功" });
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CmsCategoryEntity category)
    {
        var existing = await _categoryRepo.GetByIdAsync(id);
        if (existing == null)
            return NotFound(new { error = "分类不存在" });
        
        category.Id = id;
        category.CreatedAt = existing.CreatedAt;
        
        var success = await _categoryRepo.UpdateAsync(category);
        return Ok(new { success, message = success ? "更新成功" : "更新失败" });
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var success = await _categoryRepo.DeleteAsync(id);
        return Ok(new { success, message = success ? "删除成功" : "删除失败" });
    }

    #endregion

    #region 辅助方法

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("　", "-");
        
        var chars = new List<char>();
        foreach (var c in slug)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
                chars.Add(c);
        }
        
        return new string(chars.ToArray()).Trim('-');
    }

    #endregion
}
