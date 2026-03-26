using SqlSugar;

namespace GeoCore.Data.Entities;

#region 内容模板管理（Admin 配置）

/// <summary>
/// 内容模板实体 - Admin 后台管理各平台内容模板
/// Phase 8.1: 内容模板管理
/// </summary>
[SugarTable("content_templates")]
public class ContentTemplateEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 目标平台：reddit/linkedin/medium/twitter/youtube
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 模板类型：post/comment/article/tweet/description
    /// </summary>
    [SugarColumn(Length = 50)]
    public string TemplateType { get; set; } = "";

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(Length = 200)]
    public string TemplateName { get; set; } = "";

    /// <summary>
    /// 模板内容（含变量占位符，如 {{brand_name}}, {{product_info}}）
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string TemplateContent { get; set; } = "";

    /// <summary>
    /// 可用变量列表（JSON 格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Variables { get; set; }

    /// <summary>
    /// 使用指南
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Guidelines { get; set; }

    /// <summary>
    /// 适用行业（JSON 数组，如 ["saas", "ecommerce", "finance"]）
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Industries { get; set; }

    /// <summary>
    /// 语气风格：professional/casual/friendly/authoritative
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? ToneStyle { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

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
/// 平台内容规则实体 - Admin 后台配置各平台内容规范
/// Phase 8.2: 平台内容规则
/// </summary>
[SugarTable("platform_content_rules")]
public class PlatformContentRuleEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 目标平台：reddit/linkedin/medium/twitter/youtube
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 规则类型：char_limit/word_limit/forbidden_words/best_practices/hashtag_limit
    /// </summary>
    [SugarColumn(Length = 50)]
    public string RuleType { get; set; } = "";

    /// <summary>
    /// 规则值（JSON 格式，根据 RuleType 不同结构不同）
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string RuleValue { get; set; } = "";

    /// <summary>
    /// 规则描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 发布平台配置（Admin 配置）

/// <summary>
/// 发布平台 App 配置实体 - Admin 后台配置 OAuth App 凭证
/// Phase 8.4: 平台 App 配置
/// </summary>
[SugarTable("publish_platform_apps")]
public class PublishPlatformAppEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 平台标识：reddit/linkedin/medium/twitter/prnewswire/businesswire
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// App 名称
    /// </summary>
    [SugarColumn(Length = 200)]
    public string AppName { get; set; } = "";

    /// <summary>
    /// Client ID（加密存储）
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ClientId { get; set; } = "";

    /// <summary>
    /// Client Secret（加密存储）
    /// </summary>
    [SugarColumn(Length = 500)]
    public string ClientSecret { get; set; } = "";

    /// <summary>
    /// OAuth 回调 URI
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? RedirectUri { get; set; }

    /// <summary>
    /// 授权范围（逗号分隔）
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Scopes { get; set; }

    /// <summary>
    /// API 基础 URL
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Notes { get; set; }

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
/// 发布规则实体 - Admin 后台配置发布频率限制
/// Phase 8.5: 发布规则配置
/// </summary>
[SugarTable("publish_rules")]
public class PublishRuleEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 平台标识：reddit/linkedin/medium/twitter/all
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 规则类型：rate_limit/daily_limit/cooldown_minutes/karma_required
    /// </summary>
    [SugarColumn(Length = 50)]
    public string RuleType { get; set; } = "";

    /// <summary>
    /// 规则值
    /// </summary>
    public int RuleValue { get; set; }

    /// <summary>
    /// 规则描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region 用户内容管理（SaaS 用户数据）

/// <summary>
/// 用户平台账号实体 - SaaS 用户绑定的平台账号
/// Phase 8.11: 账号绑定
/// </summary>
[SugarTable("user_platform_accounts")]
public class UserPlatformAccountEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 平台标识：reddit/linkedin/medium/twitter
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 平台用户ID
    /// </summary>
    [SugarColumn(Length = 200)]
    public string PlatformUserId { get; set; } = "";

    /// <summary>
    /// 平台用户名
    /// </summary>
    [SugarColumn(Length = 200)]
    public string PlatformUsername { get; set; } = "";

    /// <summary>
    /// Access Token（加密存储）
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string AccessToken { get; set; } = "";

    /// <summary>
    /// Refresh Token（加密存储）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Token 过期时间
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// 账号状态：active/expired/revoked
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Status { get; set; } = "active";

    /// <summary>
    /// 额外数据（JSON 格式，如 Reddit karma 等）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ExtraData { get; set; }

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
/// 内容草稿实体 - SaaS 用户创建的内容草稿
/// Phase 8.9: 草稿管理
/// </summary>
[SugarTable("content_drafts")]
public class ContentDraftEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 项目ID
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// 目标平台：reddit/linkedin/medium/twitter
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 使用的模板ID
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// 内容标题
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Title { get; set; }

    /// <summary>
    /// 内容正文
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Content { get; set; } = "";

    /// <summary>
    /// 状态：draft/reviewing/approved/published/failed
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Status { get; set; } = "draft";

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 父草稿ID（用于版本关联）
    /// </summary>
    public int? ParentDraftId { get; set; }

    /// <summary>
    /// 生成时使用的变量数据（JSON 格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? VariableData { get; set; }

    /// <summary>
    /// 审核结果（JSON 格式，包含规范检查结果）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ReviewResult { get; set; }

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
/// 发布历史实体 - SaaS 用户的发布记录
/// Phase 8.13: 发布历史
/// </summary>
[SugarTable("publish_history")]
public class PublishHistoryEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 草稿ID
    /// </summary>
    public int DraftId { get; set; }

    /// <summary>
    /// 目标平台
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Platform { get; set; } = "";

    /// <summary>
    /// 平台帖子/文章ID
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? PlatformPostId { get; set; }

    /// <summary>
    /// 平台帖子/文章 URL
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    public string? PlatformUrl { get; set; }

    /// <summary>
    /// 发布状态：pending/success/failed
    /// </summary>
    [SugarColumn(Length = 50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 发布时间
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// 关联的引用追踪任务ID
    /// </summary>
    public int? CitationTaskId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

#endregion
