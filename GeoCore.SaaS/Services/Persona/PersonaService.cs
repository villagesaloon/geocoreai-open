using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace GeoCore.SaaS.Services.Persona;

/// <summary>
/// Persona 生成与管理服务
/// </summary>
public class PersonaService
{
    private readonly ILogger<PersonaService> _logger;
    private readonly ICitationMonitoringRepository? _citationRepository;

    private static readonly Dictionary<string, string> RoleTypeIcons = new()
    {
        ["decision_maker"] = "👔",
        ["influencer"] = "💡",
        ["end_user"] = "👤",
        ["gatekeeper"] = "🔒",
        ["champion"] = "⭐",
        ["evaluator"] = "🔍"
    };

    public PersonaService(
        ILogger<PersonaService> logger,
        ICitationMonitoringRepository? citationRepository = null)
    {
        _logger = logger;
        _citationRepository = citationRepository;
    }

    /// <summary>
    /// 生成买家角色（使用模板生成，模板通过 Admin 后台管理）
    /// </summary>
    public Task<PersonaGenerationResult> GeneratePersonasAsync(PersonaGenerationRequest request)
    {
        _logger.LogInformation("[Persona] Generating {Count} personas for brand: {Brand}, industry: {Industry}",
            request.Count, request.Brand, request.Industry);

        var result = new PersonaGenerationResult
        {
            Brand = request.Brand,
            Industry = request.Industry,
            Personas = GenerateTemplatePersonas(request)
        };

        result.Summary = GenerateSummary(result);

        _logger.LogInformation("[Persona] Generated {Count} personas", result.Personas.Count);
        return Task.FromResult(result);
    }

    /// <summary>
    /// 使用模板生成 Persona（模板通过 Admin 后台管理到数据库）
    /// </summary>
    public List<BuyerPersona> GenerateTemplatePersonas(PersonaGenerationRequest request)
    {
        var templates = GetIndustryTemplates(request.Industry);
        var count = Math.Min(request.Count, templates.Count);

        var personas = templates.Take(count).Select(t => new BuyerPersona
        {
            Name = t.Name,
            Title = t.Title,
            Description = $"负责{request.Brand}相关{t.Focus}的{t.Title}",
            AgeRange = t.AgeRange,
            RoleType = t.RoleType,
            Icon = RoleTypeIcons.GetValueOrDefault(t.RoleType, "👤"),
            Profile = new PersonaProfile
            {
                Goals = t.Goals.Select(g => g.Replace("{brand}", request.Brand).Replace("{product}", request.Product)).ToList(),
                PainPoints = t.PainPoints,
                DecisionFactors = t.DecisionFactors,
                InformationSources = t.InformationSources,
                SearchKeywords = t.SearchKeywords.Select(k => k.Replace("{brand}", request.Brand).Replace("{industry}", request.Industry)).ToList(),
                Objections = t.Objections,
                PreferredContentTypes = t.PreferredContentTypes,
                DecisionJourney = t.DecisionJourney
            }
        }).ToList();

        if (request.GenerateQuestions)
        {
            foreach (var persona in personas)
            {
                persona.Questions = GenerateQuestionsForPersona(persona, request);
            }
        }

        return personas;
    }

    /// <summary>
    /// 为 Persona 生成问题
    /// </summary>
    public List<PersonaQuestion> GenerateQuestionsForPersona(BuyerPersona persona, PersonaGenerationRequest request)
    {
        var questions = new List<PersonaQuestion>();
        var count = request.QuestionsPerPersona;
        var journey = persona.Profile.DecisionJourney;

        foreach (var q in journey.Awareness.TypicalQuestions.Take(count / 3 + 1))
        {
            questions.Add(new PersonaQuestion
            {
                Question = q.Replace("{brand}", request.Brand).Replace("{product}", request.Product),
                Intent = "informational",
                Stage = "awareness",
                Motivation = $"{persona.Name}在认知阶段想了解基本概念",
                BrandOpportunityScore = 6
            });
        }

        foreach (var q in journey.Consideration.TypicalQuestions.Take(count / 3 + 1))
        {
            questions.Add(new PersonaQuestion
            {
                Question = q.Replace("{brand}", request.Brand).Replace("{product}", request.Product),
                Intent = "commercial",
                Stage = "consideration",
                Motivation = $"{persona.Name}在考虑阶段评估不同选项",
                BrandOpportunityScore = 8
            });
        }

        foreach (var q in journey.Decision.TypicalQuestions.Take(count / 3 + 1))
        {
            questions.Add(new PersonaQuestion
            {
                Question = q.Replace("{brand}", request.Brand).Replace("{product}", request.Product),
                Intent = "transactional",
                Stage = "decision",
                Motivation = $"{persona.Name}在决策阶段需要最终确认",
                BrandOpportunityScore = 9
            });
        }

        foreach (var painPoint in persona.Profile.PainPoints.Take(2))
        {
            questions.Add(new PersonaQuestion
            {
                Question = $"如何解决{painPoint}？",
                Intent = "informational",
                Stage = "awareness",
                Motivation = $"{persona.Name}面临的核心痛点",
                ExpectedAnswerPoints = new List<string> { $"{request.Brand}如何解决此问题", "具体方案", "成功案例" },
                BrandOpportunityScore = 7
            });
        }

        return questions.Take(count).ToList();
    }

    /// <summary>
    /// 生成可见度矩阵
    /// </summary>
    public async Task<PersonaVisibilityMatrix> GenerateVisibilityMatrixAsync(PersonaVisibilityRequest request)
    {
        _logger.LogInformation("[Persona] Generating visibility matrix for {PersonaCount} personas, {TopicCount} topics",
            request.Personas.Count, request.Topics.Count);

        var matrix = new PersonaVisibilityMatrix
        {
            Brand = request.Brand,
            PersonaNames = request.Personas.Select(p => p.Name).ToList(),
            Topics = request.Topics.Any() ? request.Topics : ExtractTopicsFromPersonas(request.Personas)
        };

        var citationResults = await GetCitationResultsAsync(request.TaskId);

        foreach (var persona in request.Personas)
        {
            foreach (var topic in matrix.Topics)
            {
                var cell = CalculateVisibilityCell(persona, topic, citationResults, request.Brand);
                matrix.Cells.Add(cell);
            }
        }

        matrix.PersonaAverages = CalculatePersonaAverages(matrix);
        matrix.TopicAverages = CalculateTopicAverages(matrix);
        matrix.OverallVisibility = matrix.Cells.Any() ? matrix.Cells.Average(c => c.VisibilityScore) : 0;
        matrix.Insights = GenerateVisibilityInsights(matrix);

        return matrix;
    }

    private List<string> ExtractTopicsFromPersonas(List<BuyerPersona> personas)
    {
        var topics = new HashSet<string>();

        foreach (var persona in personas)
        {
            foreach (var keyword in persona.Profile.SearchKeywords.Take(3))
                topics.Add(keyword);

            foreach (var painPoint in persona.Profile.PainPoints.Take(2))
                topics.Add(painPoint);
        }

        return topics.Take(10).ToList();
    }

    private VisibilityCell CalculateVisibilityCell(
        BuyerPersona persona, 
        string topic, 
        List<CitationResultEntity> citations,
        string brand)
    {
        var relevantCitations = citations.Where(c => 
            (c.Question?.Contains(topic, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (c.Response?.Contains(topic, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();

        var totalQueries = relevantCitations.Count;
        var citedCount = relevantCitations.Count(c => 
            c.IsCited && (c.Response?.Contains(brand, StringComparison.OrdinalIgnoreCase) ?? false));

        var visibilityScore = totalQueries > 0 ? (double)citedCount / totalQueries * 100 : 0;

        return new VisibilityCell
        {
            PersonaName = persona.Name,
            Topic = topic,
            VisibilityScore = visibilityScore,
            CitationCount = citedCount,
            TotalQueries = totalQueries,
            Level = GetVisibilityLevel(visibilityScore)
        };
    }

    public string GetVisibilityLevel(double score)
    {
        return score switch
        {
            >= 70 => "high",
            >= 40 => "medium",
            >= 10 => "low",
            _ => "none"
        };
    }

    private Dictionary<string, double> CalculatePersonaAverages(PersonaVisibilityMatrix matrix)
    {
        return matrix.PersonaNames.ToDictionary(
            name => name,
            name => matrix.Cells.Where(c => c.PersonaName == name).DefaultIfEmpty().Average(c => c?.VisibilityScore ?? 0)
        );
    }

    private Dictionary<string, double> CalculateTopicAverages(PersonaVisibilityMatrix matrix)
    {
        return matrix.Topics.ToDictionary(
            topic => topic,
            topic => matrix.Cells.Where(c => c.Topic == topic).DefaultIfEmpty().Average(c => c?.VisibilityScore ?? 0)
        );
    }

    public List<VisibilityInsight> GenerateVisibilityInsights(PersonaVisibilityMatrix matrix)
    {
        var insights = new List<VisibilityInsight>();

        foreach (var (persona, avg) in matrix.PersonaAverages.Where(x => x.Value < 30))
        {
            insights.Add(new VisibilityInsight
            {
                Type = "gap",
                PersonaName = persona,
                Title = $"{persona} 可见度不足",
                Description = $"该角色的平均可见度仅为 {avg:F0}%，需要针对性优化内容",
                SuggestedAction = "创建针对该角色痛点和问题的专门内容",
                Priority = 1
            });
        }

        foreach (var (topic, avg) in matrix.TopicAverages.Where(x => x.Value < 20))
        {
            insights.Add(new VisibilityInsight
            {
                Type = "gap",
                Topic = topic,
                Title = $"话题「{topic}」可见度低",
                Description = $"该话题的平均可见度仅为 {avg:F0}%",
                SuggestedAction = "增加该话题相关的高质量内容",
                Priority = 2
            });
        }

        foreach (var (persona, avg) in matrix.PersonaAverages.Where(x => x.Value >= 70))
        {
            insights.Add(new VisibilityInsight
            {
                Type = "strength",
                PersonaName = persona,
                Title = $"{persona} 可见度优秀",
                Description = $"该角色的平均可见度达到 {avg:F0}%，表现良好",
                SuggestedAction = "保持现有内容策略，持续监测",
                Priority = 5
            });
        }

        var lowCells = matrix.Cells.Where(c => c.Level == "none" && c.TotalQueries > 0).Take(3);
        foreach (var cell in lowCells)
        {
            insights.Add(new VisibilityInsight
            {
                Type = "opportunity",
                PersonaName = cell.PersonaName,
                Topic = cell.Topic,
                Title = $"优化机会：{cell.PersonaName} × {cell.Topic}",
                Description = $"该组合有 {cell.TotalQueries} 次查询但 0 次引用",
                SuggestedAction = "创建针对该角色在该话题上的专门内容",
                Priority = 1
            });
        }

        return insights.OrderBy(i => i.Priority).ToList();
    }

    private async Task<List<CitationResultEntity>> GetCitationResultsAsync(int taskId)
    {
        if (_citationRepository == null)
            return new List<CitationResultEntity>();

        return await _citationRepository.GetResultsByTaskIdAsync(taskId, 1000);
    }

    public string GenerateSummary(PersonaGenerationResult result)
    {
        var parts = new List<string>();
        
        parts.Add($"为品牌「{result.Brand}」生成了 {result.Personas.Count} 个买家角色。");

        var roleTypes = result.Personas.GroupBy(p => p.RoleType).Select(g => $"{GetRoleTypeName(g.Key)}({g.Count()}个)");
        parts.Add($"角色类型：{string.Join("、", roleTypes)}。");

        var totalQuestions = result.Personas.Sum(p => p.Questions.Count);
        if (totalQuestions > 0)
        {
            parts.Add($"共生成 {totalQuestions} 个针对性问题。");
        }

        return string.Join(" ", parts);
    }

    private string GetRoleTypeName(string roleType)
    {
        return roleType switch
        {
            "decision_maker" => "决策者",
            "influencer" => "影响者",
            "end_user" => "最终用户",
            "gatekeeper" => "把关人",
            "champion" => "支持者",
            "evaluator" => "评估者",
            _ => roleType
        };
    }

    /// <summary>
    /// 获取行业模板
    /// </summary>
    private List<PersonaTemplate> GetIndustryTemplates(string industry)
    {
        var defaultTemplates = new List<PersonaTemplate>
        {
            new()
            {
                Name = "技术决策者",
                Title = "CTO/技术总监",
                RoleType = "decision_maker",
                AgeRange = "35-50",
                Focus = "技术选型",
                Goals = new List<string> { "选择可靠的技术方案", "控制技术风险", "提升团队效率" },
                PainPoints = new List<string> { "技术选型困难", "团队学习成本高", "系统集成复杂" },
                DecisionFactors = new List<string> { "技术成熟度", "社区支持", "长期维护成本" },
                InformationSources = new List<string> { "技术博客", "GitHub", "技术会议" },
                SearchKeywords = new List<string> { "{brand} 技术架构", "{industry} 最佳实践", "{brand} vs 竞品" },
                Objections = new List<string> { "迁移成本太高", "团队不熟悉" },
                PreferredContentTypes = new List<string> { "技术白皮书", "架构文档", "案例研究" },
                DecisionJourney = new DecisionJourney
                {
                    Awareness = new JourneyStage
                    {
                        Stage = "awareness",
                        Name = "认知阶段",
                        Behaviors = new List<string> { "搜索行业趋势", "关注技术博客" },
                        TypicalQuestions = new List<string> { "什么是{product}？", "{industry}有哪些解决方案？" },
                        ContentNeeds = new List<string> { "概念介绍", "行业报告" }
                    },
                    Consideration = new JourneyStage
                    {
                        Stage = "consideration",
                        Name = "考虑阶段",
                        Behaviors = new List<string> { "对比不同方案", "查看案例" },
                        TypicalQuestions = new List<string> { "{brand}和竞品有什么区别？", "{brand}适合什么场景？" },
                        ContentNeeds = new List<string> { "对比分析", "客户案例" }
                    },
                    Decision = new JourneyStage
                    {
                        Stage = "decision",
                        Name = "决策阶段",
                        Behaviors = new List<string> { "申请试用", "评估 ROI" },
                        TypicalQuestions = new List<string> { "{brand}的价格是多少？", "如何开始使用{brand}？" },
                        ContentNeeds = new List<string> { "定价方案", "实施指南" }
                    }
                }
            },
            new()
            {
                Name = "业务负责人",
                Title = "业务总监/VP",
                RoleType = "influencer",
                AgeRange = "30-45",
                Focus = "业务增长",
                Goals = new List<string> { "提升业务效率", "降低运营成本", "实现业务目标" },
                PainPoints = new List<string> { "效率低下", "数据孤岛", "决策缺乏依据" },
                DecisionFactors = new List<string> { "ROI", "易用性", "实施周期" },
                InformationSources = new List<string> { "行业报告", "同行推荐", "商业媒体" },
                SearchKeywords = new List<string> { "{brand} ROI", "{industry} 效率提升", "{brand} 成功案例" },
                Objections = new List<string> { "实施周期太长", "效果难以量化" },
                PreferredContentTypes = new List<string> { "ROI 计算器", "客户证言", "行业报告" },
                DecisionJourney = new DecisionJourney
                {
                    Awareness = new JourneyStage
                    {
                        Stage = "awareness",
                        Name = "认知阶段",
                        Behaviors = new List<string> { "了解行业趋势", "参加行业活动" },
                        TypicalQuestions = new List<string> { "如何提升{industry}效率？", "{industry}有哪些新趋势？" },
                        ContentNeeds = new List<string> { "趋势报告", "行业洞察" }
                    },
                    Consideration = new JourneyStage
                    {
                        Stage = "consideration",
                        Name = "考虑阶段",
                        Behaviors = new List<string> { "评估 ROI", "咨询同行" },
                        TypicalQuestions = new List<string> { "{brand}能带来多少收益？", "哪些公司在用{brand}？" },
                        ContentNeeds = new List<string> { "ROI 分析", "客户案例" }
                    },
                    Decision = new JourneyStage
                    {
                        Stage = "decision",
                        Name = "决策阶段",
                        Behaviors = new List<string> { "申请演示", "谈判合同" },
                        TypicalQuestions = new List<string> { "{brand}的实施流程是什么？", "需要多长时间见效？" },
                        ContentNeeds = new List<string> { "实施方案", "服务承诺" }
                    }
                }
            },
            new()
            {
                Name = "一线用户",
                Title = "产品经理/运营",
                RoleType = "end_user",
                AgeRange = "25-35",
                Focus = "日常使用",
                Goals = new List<string> { "简化工作流程", "提高工作效率", "学习新技能" },
                PainPoints = new List<string> { "工具太复杂", "学习成本高", "缺乏支持" },
                DecisionFactors = new List<string> { "易用性", "功能完整性", "学习资源" },
                InformationSources = new List<string> { "产品文档", "视频教程", "社区论坛" },
                SearchKeywords = new List<string> { "{brand} 教程", "{brand} 怎么用", "{brand} 功能" },
                Objections = new List<string> { "太难学了", "功能不够用" },
                PreferredContentTypes = new List<string> { "视频教程", "快速入门", "FAQ" },
                DecisionJourney = new DecisionJourney
                {
                    Awareness = new JourneyStage
                    {
                        Stage = "awareness",
                        Name = "认知阶段",
                        Behaviors = new List<string> { "搜索解决方案", "看评测视频" },
                        TypicalQuestions = new List<string> { "{brand}是什么？", "{brand}能做什么？" },
                        ContentNeeds = new List<string> { "产品介绍", "功能演示" }
                    },
                    Consideration = new JourneyStage
                    {
                        Stage = "consideration",
                        Name = "考虑阶段",
                        Behaviors = new List<string> { "试用产品", "看用户评价" },
                        TypicalQuestions = new List<string> { "{brand}好用吗？", "{brand}有哪些功能？" },
                        ContentNeeds = new List<string> { "用户评价", "功能对比" }
                    },
                    Decision = new JourneyStage
                    {
                        Stage = "decision",
                        Name = "决策阶段",
                        Behaviors = new List<string> { "推荐给领导", "申请采购" },
                        TypicalQuestions = new List<string> { "如何说服领导采购{brand}？", "{brand}有免费版吗？" },
                        ContentNeeds = new List<string> { "价值说明", "免费试用" }
                    }
                }
            },
            new()
            {
                Name = "采购把关人",
                Title = "采购经理/财务",
                RoleType = "gatekeeper",
                AgeRange = "30-45",
                Focus = "成本控制",
                Goals = new List<string> { "控制采购成本", "确保合规", "管理供应商" },
                PainPoints = new List<string> { "预算有限", "审批流程复杂", "供应商管理难" },
                DecisionFactors = new List<string> { "价格", "付款方式", "合同条款" },
                InformationSources = new List<string> { "供应商官网", "采购平台", "同行推荐" },
                SearchKeywords = new List<string> { "{brand} 价格", "{brand} 企业版", "{brand} 折扣" },
                Objections = new List<string> { "超出预算", "合同条款不合理" },
                PreferredContentTypes = new List<string> { "定价页面", "合同模板", "采购指南" },
                DecisionJourney = new DecisionJourney
                {
                    Awareness = new JourneyStage
                    {
                        Stage = "awareness",
                        Name = "认知阶段",
                        Behaviors = new List<string> { "收到采购需求", "了解产品" },
                        TypicalQuestions = new List<string> { "{brand}是什么类型的产品？", "{brand}的供应商资质？" },
                        ContentNeeds = new List<string> { "公司介绍", "资质证书" }
                    },
                    Consideration = new JourneyStage
                    {
                        Stage = "consideration",
                        Name = "考虑阶段",
                        Behaviors = new List<string> { "比价", "评估供应商" },
                        TypicalQuestions = new List<string> { "{brand}的价格是多少？", "{brand}有哪些付款方式？" },
                        ContentNeeds = new List<string> { "报价单", "付款方式" }
                    },
                    Decision = new JourneyStage
                    {
                        Stage = "decision",
                        Name = "决策阶段",
                        Behaviors = new List<string> { "谈判价格", "签订合同" },
                        TypicalQuestions = new List<string> { "{brand}能否提供折扣？", "合同条款是否可协商？" },
                        ContentNeeds = new List<string> { "折扣政策", "合同模板" }
                    }
                }
            },
            new()
            {
                Name = "技术评估者",
                Title = "架构师/高级工程师",
                RoleType = "evaluator",
                AgeRange = "28-40",
                Focus = "技术评估",
                Goals = new List<string> { "评估技术可行性", "验证性能", "确保安全" },
                PainPoints = new List<string> { "文档不完整", "API 不稳定", "安全隐患" },
                DecisionFactors = new List<string> { "技术文档", "API 设计", "安全认证" },
                InformationSources = new List<string> { "技术文档", "API 文档", "安全报告" },
                SearchKeywords = new List<string> { "{brand} API", "{brand} 安全", "{brand} 性能" },
                Objections = new List<string> { "API 不够灵活", "安全性存疑" },
                PreferredContentTypes = new List<string> { "API 文档", "安全白皮书", "性能测试报告" },
                DecisionJourney = new DecisionJourney
                {
                    Awareness = new JourneyStage
                    {
                        Stage = "awareness",
                        Name = "认知阶段",
                        Behaviors = new List<string> { "查看技术文档", "了解架构" },
                        TypicalQuestions = new List<string> { "{brand}的技术架构是什么？", "{brand}支持哪些集成？" },
                        ContentNeeds = new List<string> { "架构文档", "集成指南" }
                    },
                    Consideration = new JourneyStage
                    {
                        Stage = "consideration",
                        Name = "考虑阶段",
                        Behaviors = new List<string> { "测试 API", "评估性能" },
                        TypicalQuestions = new List<string> { "{brand}的 API 限制是什么？", "{brand}的性能如何？" },
                        ContentNeeds = new List<string> { "API 文档", "性能报告" }
                    },
                    Decision = new JourneyStage
                    {
                        Stage = "decision",
                        Name = "决策阶段",
                        Behaviors = new List<string> { "编写评估报告", "提出建议" },
                        TypicalQuestions = new List<string> { "{brand}的安全认证有哪些？", "如何迁移到{brand}？" },
                        ContentNeeds = new List<string> { "安全认证", "迁移指南" }
                    }
                }
            }
        };

        return defaultTemplates;
    }

    /// <summary>
    /// Persona 模板
    /// </summary>
    private class PersonaTemplate
    {
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string RoleType { get; set; } = "";
        public string AgeRange { get; set; } = "";
        public string Focus { get; set; } = "";
        public List<string> Goals { get; set; } = new();
        public List<string> PainPoints { get; set; } = new();
        public List<string> DecisionFactors { get; set; } = new();
        public List<string> InformationSources { get; set; } = new();
        public List<string> SearchKeywords { get; set; } = new();
        public List<string> Objections { get; set; } = new();
        public List<string> PreferredContentTypes { get; set; } = new();
        public DecisionJourney DecisionJourney { get; set; } = new();
    }
}
