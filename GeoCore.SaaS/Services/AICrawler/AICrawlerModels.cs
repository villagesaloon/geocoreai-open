namespace GeoCore.SaaS.Services.AICrawler;

#region 4.48 AI 爬虫配置审计

/// <summary>
/// AI 爬虫审计请求
/// </summary>
public class AICrawlerAuditRequest
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string SiteUrl { get; set; } = "";
    
    /// <summary>
    /// robots.txt 内容（可选，如果不提供则尝试获取）
    /// </summary>
    public string? RobotsTxt { get; set; }
}

/// <summary>
/// AI 爬虫审计结果
/// </summary>
public class AICrawlerAuditResult
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string SiteUrl { get; set; } = "";
    
    /// <summary>
    /// 审计时间
    /// </summary>
    public DateTime AuditTime { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 总体评分 (0-100)
    /// </summary>
    public double OverallScore { get; set; }
    
    /// <summary>
    /// 评级：A/B/C/D/F
    /// </summary>
    public string Grade { get; set; } = "";
    
    /// <summary>
    /// 各爬虫配置状态
    /// </summary>
    public List<CrawlerStatus> CrawlerStatuses { get; set; } = new();
    
    /// <summary>
    /// 已允许的爬虫数量
    /// </summary>
    public int AllowedCount { get; set; }
    
    /// <summary>
    /// 已阻止的爬虫数量
    /// </summary>
    public int BlockedCount { get; set; }
    
    /// <summary>
    /// 未配置的爬虫数量
    /// </summary>
    public int UnconfiguredCount { get; set; }
    
    /// <summary>
    /// 建议
    /// </summary>
    public List<AuditRecommendation> Recommendations { get; set; } = new();
    
    /// <summary>
    /// 推荐的 robots.txt 配置
    /// </summary>
    public string RecommendedRobotsTxt { get; set; } = "";
}

/// <summary>
/// 爬虫状态
/// </summary>
public class CrawlerStatus
{
    /// <summary>
    /// 爬虫名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 爬虫 User-Agent
    /// </summary>
    public string UserAgent { get; set; } = "";
    
    /// <summary>
    /// 所属公司/平台
    /// </summary>
    public string Company { get; set; } = "";
    
    /// <summary>
    /// 状态：allowed, blocked, unconfigured
    /// </summary>
    public string Status { get; set; } = "unconfigured";
    
    /// <summary>
    /// 重要性：high, medium, low
    /// </summary>
    public string Importance { get; set; } = "medium";
    
    /// <summary>
    /// 用途说明
    /// </summary>
    public string Purpose { get; set; } = "";
    
    /// <summary>
    /// 建议配置
    /// </summary>
    public string RecommendedAction { get; set; } = "";
}

/// <summary>
/// 审计建议
/// </summary>
public class AuditRecommendation
{
    /// <summary>
    /// 优先级：high, medium, low
    /// </summary>
    public string Priority { get; set; } = "medium";
    
    /// <summary>
    /// 建议内容
    /// </summary>
    public string Message { get; set; } = "";
    
    /// <summary>
    /// 影响说明
    /// </summary>
    public string Impact { get; set; } = "";
}

#endregion

#region 4.49 llms.txt 生成器

/// <summary>
/// llms.txt 生成请求
/// </summary>
public class LlmsTxtRequest
{
    /// <summary>
    /// 网站 URL
    /// </summary>
    public string SiteUrl { get; set; } = "";
    
    /// <summary>
    /// 网站名称
    /// </summary>
    public string SiteName { get; set; } = "";
    
    /// <summary>
    /// 网站描述
    /// </summary>
    public string? SiteDescription { get; set; }
    
    /// <summary>
    /// 页面列表（按优先级排序）
    /// </summary>
    public List<LlmsPage> Pages { get; set; } = new();
    
    /// <summary>
    /// 是否包含可选字段
    /// </summary>
    public bool IncludeOptionalFields { get; set; } = true;
}

/// <summary>
/// llms.txt 页面
/// </summary>
public class LlmsPage
{
    /// <summary>
    /// 页面 URL
    /// </summary>
    public string Url { get; set; } = "";
    
    /// <summary>
    /// 页面标题
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// 页面描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 优先级 (1-10)
    /// </summary>
    public int Priority { get; set; } = 5;
    
    /// <summary>
    /// 内容类型
    /// </summary>
    public string? ContentType { get; set; }
}

/// <summary>
/// llms.txt 生成结果
/// </summary>
public class LlmsTxtResult
{
    /// <summary>
    /// 生成的 llms.txt 内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 页面数量
    /// </summary>
    public int PageCount { get; set; }
    
    /// <summary>
    /// 部署说明
    /// </summary>
    public List<string> DeploymentInstructions { get; set; } = new();
    
    /// <summary>
    /// 验证结果
    /// </summary>
    public LlmsTxtValidation Validation { get; set; } = new();
}

/// <summary>
/// llms.txt 验证结果
/// </summary>
public class LlmsTxtValidation
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// 警告
    /// </summary>
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// 错误
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

#endregion

#region 4.50 GA4 AI 流量追踪

/// <summary>
/// GA4 AI 流量配置
/// </summary>
public class GA4AITrackingConfig
{
    /// <summary>
    /// LLM Referrers 列表
    /// </summary>
    public List<LLMReferrer> LLMReferrers { get; set; } = new();
    
    /// <summary>
    /// AI Bot User-Agents
    /// </summary>
    public List<string> AIBotUserAgents { get; set; } = new();
    
    /// <summary>
    /// GTM 配置代码
    /// </summary>
    public string GTMCode { get; set; } = "";
    
    /// <summary>
    /// GA4 自定义维度配置
    /// </summary>
    public List<CustomDimension> CustomDimensions { get; set; } = new();
}

/// <summary>
/// LLM Referrer
/// </summary>
public class LLMReferrer
{
    public string Name { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Pattern { get; set; } = "";
}

/// <summary>
/// 自定义维度
/// </summary>
public class CustomDimension
{
    public string Name { get; set; } = "";
    public string Scope { get; set; } = "";
    public string Description { get; set; } = "";
}

#endregion

#region 4.52 双平台优化策略

/// <summary>
/// 双平台优化分析请求
/// </summary>
public class DualPlatformRequest
{
    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = "";
    
    /// <summary>
    /// 目标关键词
    /// </summary>
    public List<string> Keywords { get; set; } = new();
}

/// <summary>
/// 双平台优化分析结果
/// 原理：AIO vs AI Mode 仅 13.7% 重叠
/// </summary>
public class DualPlatformResult
{
    /// <summary>
    /// AIO (AI Overview) 优化建议
    /// </summary>
    public PlatformOptimization AIOOptimization { get; set; } = new();
    
    /// <summary>
    /// AI Mode 优化建议
    /// </summary>
    public PlatformOptimization AIModeOptimization { get; set; } = new();
    
    /// <summary>
    /// 重叠度分析
    /// </summary>
    public OverlapAnalysis Overlap { get; set; } = new();
    
    /// <summary>
    /// 综合建议
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// 平台优化
/// </summary>
public class PlatformOptimization
{
    public string Platform { get; set; } = "";
    public double CurrentScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Improvements { get; set; } = new();
    public List<string> ContentTips { get; set; } = new();
}

/// <summary>
/// 重叠度分析
/// </summary>
public class OverlapAnalysis
{
    /// <summary>
    /// 重叠百分比
    /// </summary>
    public double OverlapPercentage { get; set; } = 13.7;
    
    /// <summary>
    /// 共同优化点
    /// </summary>
    public List<string> CommonOptimizations { get; set; } = new();
    
    /// <summary>
    /// AIO 独有优化点
    /// </summary>
    public List<string> AIOOnlyOptimizations { get; set; } = new();
    
    /// <summary>
    /// AI Mode 独有优化点
    /// </summary>
    public List<string> AIModeOnlyOptimizations { get; set; } = new();
}

#endregion

#region 4.53 JS 渲染检测

/// <summary>
/// JS 渲染检测请求
/// </summary>
public class JSRenderingRequest
{
    /// <summary>
    /// 页面 URL
    /// </summary>
    public string Url { get; set; } = "";
    
    /// <summary>
    /// HTML 内容（可选）
    /// </summary>
    public string? HtmlContent { get; set; }
}

/// <summary>
/// JS 渲染检测结果
/// 原理：AI 爬虫不执行 JS，客户端渲染内容不可见
/// </summary>
public class JSRenderingResult
{
    /// <summary>
    /// 页面 URL
    /// </summary>
    public string Url { get; set; } = "";
    
    /// <summary>
    /// 是否依赖 JS 渲染
    /// </summary>
    public bool RequiresJSRendering { get; set; }
    
    /// <summary>
    /// 风险等级：high, medium, low
    /// </summary>
    public string RiskLevel { get; set; } = "low";
    
    /// <summary>
    /// 检测到的问题
    /// </summary>
    public List<JSRenderingIssue> Issues { get; set; } = new();
    
    /// <summary>
    /// 建议
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
    
    /// <summary>
    /// AI 爬虫可见内容比例
    /// </summary>
    public double VisibleContentRatio { get; set; }
}

/// <summary>
/// JS 渲染问题
/// </summary>
public class JSRenderingIssue
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Solution { get; set; } = "";
}

#endregion

#region 4.55-4.58 竞品分析

/// <summary>
/// 竞品引用分析请求
/// </summary>
public class CompetitorAnalysisRequest
{
    /// <summary>
    /// 主题/关键词
    /// </summary>
    public string Topic { get; set; } = "";
    
    /// <summary>
    /// 竞品域名列表
    /// </summary>
    public List<string> CompetitorDomains { get; set; } = new();
    
    /// <summary>
    /// 自有域名
    /// </summary>
    public string OwnDomain { get; set; } = "";
}

/// <summary>
/// 竞品引用分析结果
/// </summary>
public class CompetitorAnalysisResult
{
    /// <summary>
    /// 主题
    /// </summary>
    public string Topic { get; set; } = "";
    
    /// <summary>
    /// SOV (Share of Voice) 数据
    /// </summary>
    public List<SOVData> SOVBreakdown { get; set; } = new();
    
    /// <summary>
    /// 引用来源分类
    /// </summary>
    public CitationSourceBreakdown SourceBreakdown { get; set; } = new();
    
    /// <summary>
    /// 趋势分析
    /// </summary>
    public TrendAnalysis Trends { get; set; } = new();
    
    /// <summary>
    /// 机会和风险
    /// </summary>
    public OpportunityRiskAnalysis OpportunityRisk { get; set; } = new();
}

/// <summary>
/// SOV 数据
/// </summary>
public class SOVData
{
    public string Domain { get; set; } = "";
    public double SharePercentage { get; set; }
    public string Trend { get; set; } = ""; // up, down, stable
    public double TrendChange { get; set; }
}

/// <summary>
/// 引用来源分类
/// </summary>
public class CitationSourceBreakdown
{
    /// <summary>
    /// 自有来源占比
    /// </summary>
    public double OwnedPercentage { get; set; }
    
    /// <summary>
    /// 社交来源占比
    /// </summary>
    public double SocialPercentage { get; set; }
    
    /// <summary>
    /// 竞品来源占比
    /// </summary>
    public double CompetitorPercentage { get; set; }
    
    /// <summary>
    /// 第三方来源占比
    /// </summary>
    public double ThirdPartyPercentage { get; set; }
}

/// <summary>
/// 趋势分析
/// </summary>
public class TrendAnalysis
{
    public string OverallTrend { get; set; } = "";
    public List<string> RisingCompetitors { get; set; } = new();
    public List<string> DecliningCompetitors { get; set; } = new();
}

/// <summary>
/// 机会和风险分析
/// </summary>
public class OpportunityRiskAnalysis
{
    public List<string> Opportunities { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public List<string> ActionItems { get; set; } = new();
}

#endregion
