namespace GeoCore.SaaS.Services.Persona;

/// <summary>
/// Persona 生成请求
/// </summary>
public class PersonaGenerationRequest
{
    /// <summary>
    /// 品牌名称
    /// </summary>
    public string Brand { get; set; } = "";

    /// <summary>
    /// 产品/服务名称
    /// </summary>
    public string Product { get; set; } = "";

    /// <summary>
    /// 行业
    /// </summary>
    public string Industry { get; set; } = "";

    /// <summary>
    /// 品牌描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 目标市场
    /// </summary>
    public string TargetMarket { get; set; } = "";

    /// <summary>
    /// 语言
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 生成的 Persona 数量（3-5）
    /// </summary>
    public int Count { get; set; } = 3;

    /// <summary>
    /// 是否生成深度画像
    /// </summary>
    public bool IncludeDeepProfile { get; set; } = true;

    /// <summary>
    /// 是否生成问题
    /// </summary>
    public bool GenerateQuestions { get; set; } = true;

    /// <summary>
    /// 每个 Persona 生成的问题数量
    /// </summary>
    public int QuestionsPerPersona { get; set; } = 5;
}

/// <summary>
/// Persona 生成结果
/// </summary>
public class PersonaGenerationResult
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// 品牌名称
    /// </summary>
    public string Brand { get; set; } = "";

    /// <summary>
    /// 行业
    /// </summary>
    public string Industry { get; set; } = "";

    /// <summary>
    /// 生成的 Persona 列表
    /// </summary>
    public List<BuyerPersona> Personas { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 摘要
    /// </summary>
    public string Summary { get; set; } = "";
}

/// <summary>
/// 买家角色
/// </summary>
public class BuyerPersona
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// 角色名称（如：技术决策者、预算管理者）
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 角色标题/职位
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 角色描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 年龄范围
    /// </summary>
    public string AgeRange { get; set; } = "";

    /// <summary>
    /// 角色类型（decision_maker, influencer, end_user, gatekeeper）
    /// </summary>
    public string RoleType { get; set; } = "";

    /// <summary>
    /// 深度画像
    /// </summary>
    public PersonaProfile Profile { get; set; } = new();

    /// <summary>
    /// 该角色生成的问题
    /// </summary>
    public List<PersonaQuestion> Questions { get; set; } = new();

    /// <summary>
    /// 角色图标（emoji）
    /// </summary>
    public string Icon { get; set; } = "👤";
}

/// <summary>
/// Persona 深度画像
/// </summary>
public class PersonaProfile
{
    /// <summary>
    /// 主要目标（3-5个）
    /// </summary>
    public List<string> Goals { get; set; } = new();

    /// <summary>
    /// 痛点/挑战（3-5个）
    /// </summary>
    public List<string> PainPoints { get; set; } = new();

    /// <summary>
    /// 决策因素（影响购买决策的关键因素）
    /// </summary>
    public List<string> DecisionFactors { get; set; } = new();

    /// <summary>
    /// 信息来源（获取信息的渠道）
    /// </summary>
    public List<string> InformationSources { get; set; } = new();

    /// <summary>
    /// 决策过程阶段
    /// </summary>
    public DecisionJourney DecisionJourney { get; set; } = new();

    /// <summary>
    /// 常用搜索关键词
    /// </summary>
    public List<string> SearchKeywords { get; set; } = new();

    /// <summary>
    /// 反对意见/顾虑
    /// </summary>
    public List<string> Objections { get; set; } = new();

    /// <summary>
    /// 期望的内容类型
    /// </summary>
    public List<string> PreferredContentTypes { get; set; } = new();
}

/// <summary>
/// 决策旅程
/// </summary>
public class DecisionJourney
{
    /// <summary>
    /// 认知阶段行为
    /// </summary>
    public JourneyStage Awareness { get; set; } = new() { Stage = "awareness", Name = "认知阶段" };

    /// <summary>
    /// 考虑阶段行为
    /// </summary>
    public JourneyStage Consideration { get; set; } = new() { Stage = "consideration", Name = "考虑阶段" };

    /// <summary>
    /// 决策阶段行为
    /// </summary>
    public JourneyStage Decision { get; set; } = new() { Stage = "decision", Name = "决策阶段" };

    /// <summary>
    /// 购买后阶段行为
    /// </summary>
    public JourneyStage PostPurchase { get; set; } = new() { Stage = "post_purchase", Name = "购买后阶段" };
}

/// <summary>
/// 决策旅程阶段
/// </summary>
public class JourneyStage
{
    /// <summary>
    /// 阶段标识
    /// </summary>
    public string Stage { get; set; } = "";

    /// <summary>
    /// 阶段名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 该阶段的典型行为
    /// </summary>
    public List<string> Behaviors { get; set; } = new();

    /// <summary>
    /// 该阶段的典型问题
    /// </summary>
    public List<string> TypicalQuestions { get; set; } = new();

    /// <summary>
    /// 该阶段的内容需求
    /// </summary>
    public List<string> ContentNeeds { get; set; } = new();
}

/// <summary>
/// Persona 生成的问题
/// </summary>
public class PersonaQuestion
{
    /// <summary>
    /// 问题内容
    /// </summary>
    public string Question { get; set; } = "";

    /// <summary>
    /// 问题类型（informational, commercial, transactional, navigational）
    /// </summary>
    public string Intent { get; set; } = "";

    /// <summary>
    /// 决策阶段
    /// </summary>
    public string Stage { get; set; } = "";

    /// <summary>
    /// 问题背后的动机
    /// </summary>
    public string Motivation { get; set; } = "";

    /// <summary>
    /// 预期的回答要点
    /// </summary>
    public List<string> ExpectedAnswerPoints { get; set; } = new();

    /// <summary>
    /// 品牌植入机会评分（1-10）
    /// </summary>
    public int BrandOpportunityScore { get; set; }
}

/// <summary>
/// Persona 可见度矩阵请求
/// </summary>
public class PersonaVisibilityRequest
{
    /// <summary>
    /// 监测任务 ID
    /// </summary>
    public int TaskId { get; set; }

    /// <summary>
    /// 品牌名称
    /// </summary>
    public string Brand { get; set; } = "";

    /// <summary>
    /// Persona 列表
    /// </summary>
    public List<BuyerPersona> Personas { get; set; } = new();

    /// <summary>
    /// 话题列表（可选，为空则自动提取）
    /// </summary>
    public List<string> Topics { get; set; } = new();
}

/// <summary>
/// Persona 可见度矩阵
/// </summary>
public class PersonaVisibilityMatrix
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// 品牌名称
    /// </summary>
    public string Brand { get; set; } = "";

    /// <summary>
    /// Persona 列表
    /// </summary>
    public List<string> PersonaNames { get; set; } = new();

    /// <summary>
    /// 话题列表
    /// </summary>
    public List<string> Topics { get; set; } = new();

    /// <summary>
    /// 矩阵数据（Persona × Topic → 可见度）
    /// </summary>
    public List<VisibilityCell> Cells { get; set; } = new();

    /// <summary>
    /// 每个 Persona 的平均可见度
    /// </summary>
    public Dictionary<string, double> PersonaAverages { get; set; } = new();

    /// <summary>
    /// 每个话题的平均可见度
    /// </summary>
    public Dictionary<string, double> TopicAverages { get; set; } = new();

    /// <summary>
    /// 总体平均可见度
    /// </summary>
    public double OverallVisibility { get; set; }

    /// <summary>
    /// 洞察和建议
    /// </summary>
    public List<VisibilityInsight> Insights { get; set; } = new();

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 可见度矩阵单元格
/// </summary>
public class VisibilityCell
{
    /// <summary>
    /// Persona 名称
    /// </summary>
    public string PersonaName { get; set; } = "";

    /// <summary>
    /// 话题
    /// </summary>
    public string Topic { get; set; } = "";

    /// <summary>
    /// 可见度评分（0-100）
    /// </summary>
    public double VisibilityScore { get; set; }

    /// <summary>
    /// 引用次数
    /// </summary>
    public int CitationCount { get; set; }

    /// <summary>
    /// 总查询次数
    /// </summary>
    public int TotalQueries { get; set; }

    /// <summary>
    /// 可见度等级（high, medium, low, none）
    /// </summary>
    public string Level { get; set; } = "";
}

/// <summary>
/// 可见度洞察
/// </summary>
public class VisibilityInsight
{
    /// <summary>
    /// 洞察类型（gap, strength, opportunity, risk）
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 相关 Persona
    /// </summary>
    public string? PersonaName { get; set; }

    /// <summary>
    /// 相关话题
    /// </summary>
    public string? Topic { get; set; }

    /// <summary>
    /// 洞察标题
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// 洞察描述
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// 建议行动
    /// </summary>
    public string SuggestedAction { get; set; } = "";

    /// <summary>
    /// 优先级（1-5）
    /// </summary>
    public int Priority { get; set; }
}
