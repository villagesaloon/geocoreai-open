namespace GeoCore.SaaS.Services.LlmsTxt;

/// <summary>
/// llms.txt 生成请求
/// </summary>
public class LlmsTxtGenerateRequest
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// 网站标题
    /// </summary>
    public string? SiteTitle { get; set; }

    /// <summary>
    /// 网站描述
    /// </summary>
    public string? SiteDescription { get; set; }

    /// <summary>
    /// Sitemap URL（可选，如果不提供会尝试自动发现）
    /// </summary>
    public string? SitemapUrl { get; set; }

    /// <summary>
    /// 手动指定的重要页面列表
    /// </summary>
    public List<LlmsTxtPage>? ImportantPages { get; set; }

    /// <summary>
    /// 最大页面数量（默认 50）
    /// </summary>
    public int MaxPages { get; set; } = 50;

    /// <summary>
    /// 项目 ID（可选）
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// 输出格式：standard, extended
    /// </summary>
    public string Format { get; set; } = "standard";
}

/// <summary>
/// llms.txt 页面条目
/// </summary>
public class LlmsTxtPage
{
    /// <summary>
    /// 页面 URL
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// 页面标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 页面描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级（1-10，10 最高）
    /// </summary>
    public int Priority { get; set; } = 5;

    /// <summary>
    /// 页面类型：homepage, product, blog, documentation, faq, about, contact, other
    /// </summary>
    public string PageType { get; set; } = "other";

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// 关键词标签
    /// </summary>
    public List<string>? Keywords { get; set; }
}

/// <summary>
/// llms.txt 生成结果
/// </summary>
public class LlmsTxtGenerateResult
{
    /// <summary>
    /// 生成的 llms.txt 内容
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// 网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 包含的页面数量
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// 页面列表
    /// </summary>
    public List<LlmsTxtPage> Pages { get; set; } = new();

    /// <summary>
    /// 生成统计
    /// </summary>
    public LlmsTxtStats Stats { get; set; } = new();

    /// <summary>
    /// 部署建议
    /// </summary>
    public List<string> DeploymentInstructions { get; set; } = new();
}

/// <summary>
/// llms.txt 统计信息
/// </summary>
public class LlmsTxtStats
{
    /// <summary>
    /// 从 sitemap 发现的页面数
    /// </summary>
    public int SitemapPagesFound { get; set; }

    /// <summary>
    /// 手动添加的页面数
    /// </summary>
    public int ManualPagesAdded { get; set; }

    /// <summary>
    /// 最终包含的页面数
    /// </summary>
    public int FinalPageCount { get; set; }

    /// <summary>
    /// 按类型分布
    /// </summary>
    public Dictionary<string, int> PageTypeDistribution { get; set; } = new();

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public int FileSizeBytes { get; set; }
}

/// <summary>
/// Sitemap 解析结果
/// </summary>
public class SitemapParseResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 解析到的 URL 列表
    /// </summary>
    public List<SitemapEntry> Entries { get; set; } = new();
}

/// <summary>
/// Sitemap 条目
/// </summary>
public class SitemapEntry
{
    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// 更新频率
    /// </summary>
    public string? ChangeFrequency { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public double? Priority { get; set; }
}

#region 7.15 llms.txt 行业模板库

/// <summary>
/// llms.txt 行业模板请求
/// </summary>
public class LlmsTxtIndustryTemplateRequest
{
    /// <summary>
    /// 行业类型：saas, ecommerce, content, news, b2b, healthcare, finance, education
    /// </summary>
    public string Industry { get; set; } = "";

    /// <summary>
    /// 公司名称
    /// </summary>
    public string CompanyName { get; set; } = "";

    /// <summary>
    /// 公司描述
    /// </summary>
    public string CompanyDescription { get; set; } = "";

    /// <summary>
    /// 网站 URL
    /// </summary>
    public string WebsiteUrl { get; set; } = "";

    /// <summary>
    /// 主要产品/服务列表
    /// </summary>
    public List<string> KeyProducts { get; set; } = new();

    /// <summary>
    /// 重要页面列表
    /// </summary>
    public List<string> KeyPages { get; set; } = new();
}

/// <summary>
/// llms.txt 行业模板结果
/// </summary>
public class LlmsTxtIndustryTemplateResult
{
    /// <summary>
    /// 行业类型
    /// </summary>
    public string Industry { get; set; } = "";

    /// <summary>
    /// 行业显示名称
    /// </summary>
    public string IndustryDisplayName { get; set; } = "";

    /// <summary>
    /// 生成的 llms.txt 内容
    /// </summary>
    public string GeneratedLlmsTxt { get; set; } = "";

    /// <summary>
    /// 验证结果
    /// </summary>
    public LlmsTxtValidationResult Validation { get; set; } = new();

    /// <summary>
    /// 模板章节
    /// </summary>
    public List<LlmsTxtTemplateSection> Sections { get; set; } = new();

    /// <summary>
    /// 最佳实践
    /// </summary>
    public List<string> BestPractices { get; set; } = new();

    /// <summary>
    /// 行业特定建议
    /// </summary>
    public List<string> IndustrySpecificTips { get; set; } = new();
}

/// <summary>
/// llms.txt 验证结果
/// </summary>
public class LlmsTxtValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 必需元素列表
    /// </summary>
    public List<string> RequiredElements { get; set; } = new();

    /// <summary>
    /// 缺失元素列表
    /// </summary>
    public List<string> MissingElements { get; set; } = new();

    /// <summary>
    /// 警告信息
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 完整度评分（0-100）
    /// </summary>
    public int CompletenessScore { get; set; }
}

/// <summary>
/// llms.txt 模板章节
/// </summary>
public class LlmsTxtTemplateSection
{
    /// <summary>
    /// 章节名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 章节描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 章节内容
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// 是否必需
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 优先级（1-10）
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// 行业模板定义
/// </summary>
public class LlmsTxtIndustryTemplate
{
    /// <summary>
    /// 行业代码
    /// </summary>
    public string IndustryCode { get; set; } = "";

    /// <summary>
    /// 行业显示名称
    /// </summary>
    public string IndustryName { get; set; } = "";

    /// <summary>
    /// 模板内容
    /// </summary>
    public string Template { get; set; } = "";

    /// <summary>
    /// 必需章节
    /// </summary>
    public List<string> RequiredSections { get; set; } = new();

    /// <summary>
    /// 推荐章节
    /// </summary>
    public List<string> RecommendedSections { get; set; } = new();

    /// <summary>
    /// 示例
    /// </summary>
    public List<string> Examples { get; set; } = new();
}

#endregion
