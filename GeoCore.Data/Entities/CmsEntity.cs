using SqlSugar;

namespace GeoCore.Data.Entities;

#region CMS 内容管理（Admin 配置）

/// <summary>
/// CMS 文章实体 - Admin 后台管理博客/文档/FAQ
/// Phase 10: 轻量 CMS
/// </summary>
[SugarTable("cms_articles")]
public class CmsArticleEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 文章类型：blog/doc/faq/case
    /// </summary>
    [SugarColumn(Length = 50)]
    public string ArticleType { get; set; } = "blog";

    /// <summary>
    /// 分类：getting-started/sheep-model/optimization/api 等
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>
    /// URL Slug（用于 SEO 友好 URL）
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Slug { get; set; } = "";

    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(Length = 500)]
    public string Title { get; set; } = "";

    /// <summary>
    /// 摘要（用于列表展示和 SEO description）
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    public string? Summary { get; set; }

    /// <summary>
    /// 正文内容（Markdown 格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "mediumtext")]
    public string Content { get; set; } = "";

    /// <summary>
    /// 封面图片 URL
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? CoverImage { get; set; }

    /// <summary>
    /// 标签（JSON 数组，如 ["geo", "seo", "ai"]）
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Tags { get; set; }

    /// <summary>
    /// 作者名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Author { get; set; }

    /// <summary>
    /// SEO 标题（可选，默认用 Title）
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? SeoTitle { get; set; }

    /// <summary>
    /// SEO 描述（可选，默认用 Summary）
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? SeoDescription { get; set; }

    /// <summary>
    /// 是否发布
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// 是否置顶
    /// </summary>
    public bool IsFeatured { get; set; } = false;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 阅读量
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// CMS 分类实体 - Admin 后台管理文章分类
/// Phase 10: 轻量 CMS
/// </summary>
[SugarTable("cms_categories")]
public class CmsCategoryEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 文章类型：blog/doc/faq/case
    /// </summary>
    [SugarColumn(Length = 50)]
    public string ArticleType { get; set; } = "blog";

    /// <summary>
    /// 分类标识（用于 URL）
    /// </summary>
    [SugarColumn(Length = 100)]
    public string Slug { get; set; } = "";

    /// <summary>
    /// 分类名称
    /// </summary>
    [SugarColumn(Length = 200)]
    public string Name { get; set; } = "";

    /// <summary>
    /// 分类描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 图标（FontAwesome 类名）
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Icon { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion
