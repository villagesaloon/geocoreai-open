using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.ContentBrief;

/// <summary>
/// 内容简报生成服务 - 基于 AI 引用数据生成内容创作指南
/// </summary>
public class ContentBriefService
{
    private readonly ILogger<ContentBriefService> _logger;
    private readonly CitationMonitoringRepository _citationRepo;

    public ContentBriefService(
        ILogger<ContentBriefService> logger,
        CitationMonitoringRepository citationRepo)
    {
        _logger = logger;
        _citationRepo = citationRepo;
    }

    /// <summary>
    /// 生成完整内容简报
    /// </summary>
    public async Task<ContentBriefReport> GenerateBriefAsync(TopicAnalysisRequest request)
    {
        _logger.LogInformation("[ContentBrief] Generating brief for task {TaskId}, brand {Brand}, topic {Topic}",
            request.TaskId, request.Brand, request.TargetTopic);

        var results = await _citationRepo.GetResultsByTaskIdAsync(request.TaskId);

        var report = new ContentBriefReport
        {
            Brand = request.Brand,
            TargetTopic = request.TargetTopic
        };

        // 1. AI 对齐主题推荐
        report.RecommendedTopics = AnalyzeAlignedTopics(results, request.Brand, request.TargetTopic);

        // 2. 标题结构建议
        report.SuggestedStructure = GenerateHeadingStructure(results, request);

        // 3. 引用事实建议
        report.CitableFacts = ExtractCitableFacts(results, request.Brand);

        // 4. 关键词建议
        report.KeywordSuggestions = GenerateKeywordSuggestions(results, request.Brand, request.TargetTopic);

        // 5. 竞品分析
        if (request.Competitors?.Any() == true)
        {
            report.CompetitorAnalysis = AnalyzeCompetitors(results, request.Brand, request.Competitors);
        }

        // 6. 优化清单
        report.Checklist = GenerateOptimizationChecklist(report);

        // 7. 生成摘要
        report.Summary = GenerateBriefSummary(report);

        return report;
    }

    /// <summary>
    /// 5.9 AI 对齐主题推荐
    /// </summary>
    private List<AlignedTopic> AnalyzeAlignedTopics(List<CitationResultEntity> results, string brand, string targetTopic)
    {
        var topics = new List<AlignedTopic>();
        var brandLower = brand.ToLowerInvariant();

        // 从问题中提取主题模式
        var questionPatterns = new Dictionary<string, (int count, List<string> questions)>();

        foreach (var result in results.Where(r => r.IsCited))
        {
            var question = result.Question.ToLowerInvariant();
            
            // 提取问题类型
            var topicType = ClassifyQuestionTopic(question);
            if (!string.IsNullOrEmpty(topicType))
            {
                if (!questionPatterns.ContainsKey(topicType))
                {
                    questionPatterns[topicType] = (0, new List<string>());
                }
                var current = questionPatterns[topicType];
                questionPatterns[topicType] = (current.count + 1, current.questions);
                if (current.questions.Count < 3)
                {
                    current.questions.Add(result.Question);
                }
            }
        }

        // 按引用频率排序
        var sortedTopics = questionPatterns
            .OrderByDescending(kv => kv.Value.count)
            .Take(10)
            .ToList();

        int priority = 1;
        foreach (var (topic, (count, questions)) in sortedTopics)
        {
            var citationRate = results.Any() ? (double)count / results.Count : 0;
            
            topics.Add(new AlignedTopic
            {
                Topic = topic,
                Description = GetTopicDescription(topic),
                CitationFrequency = citationRate,
                RelatedQuestions = questions,
                Reason = GenerateTopicReason(topic, count, citationRate),
                Priority = priority++,
                SuggestedContentType = GetSuggestedContentType(topic)
            });
        }

        // 如果没有足够数据，添加通用建议
        if (topics.Count < 3)
        {
            topics.AddRange(GetDefaultTopicSuggestions(brand, targetTopic, topics.Count));
        }

        return topics;
    }

    /// <summary>
    /// 5.10 标题结构建议
    /// </summary>
    private HeadingStructure GenerateHeadingStructure(List<CitationResultEntity> results, TopicAnalysisRequest request)
    {
        var structure = new HeadingStructure();
        var brand = request.Brand;
        var topic = request.TargetTopic;

        // 生成 H1
        structure.SuggestedH1 = GenerateH1Title(brand, topic, request.ContentType);

        // 基于内容类型生成 H2 结构
        structure.H2Sections = request.ContentType switch
        {
            "faq" => GenerateFAQStructure(results, brand, topic),
            "comparison" => GenerateComparisonStructure(results, brand, topic),
            "guide" => GenerateGuideStructure(results, brand, topic),
            _ => GenerateArticleStructure(results, brand, topic)
        };

        // 计算预估字数
        structure.EstimatedWordCount = structure.H2Sections.Sum(s => s.EstimatedWordCount);
        if (structure.EstimatedWordCount < request.TargetWordCount)
        {
            // 调整各节字数
            var multiplier = (double)request.TargetWordCount / structure.EstimatedWordCount;
            foreach (var section in structure.H2Sections)
            {
                section.EstimatedWordCount = (int)(section.EstimatedWordCount * multiplier);
            }
            structure.EstimatedWordCount = request.TargetWordCount;
        }

        structure.StructureRationale = GenerateStructureRationale(request.ContentType, structure.H2Sections.Count);

        return structure;
    }

    /// <summary>
    /// 5.11 引用事实建议
    /// </summary>
    private List<CitableFact> ExtractCitableFacts(List<CitationResultEntity> results, string brand)
    {
        var facts = new List<CitableFact>();
        var factPatterns = new Dictionary<string, (int count, List<string> platforms, string context)>();

        // 统计/数字模式
        var statPattern = new Regex(@"(\d+(?:\.\d+)?%|\d+(?:,\d{3})*(?:\.\d+)?)\s*(?:的|个|次|年|月|用户|客户|增长|提升|降低)", RegexOptions.Compiled);
        
        // 引用/研究模式
        var researchPattern = new Regex(@"(?:研究|调查|报告|数据)(?:显示|表明|指出)", RegexOptions.Compiled);

        foreach (var result in results.Where(r => r.IsCited && !string.IsNullOrEmpty(r.Response)))
        {
            var response = result.Response;
            
            // 提取统计数据
            var statMatches = statPattern.Matches(response);
            foreach (Match match in statMatches)
            {
                var context = ExtractFactContext(response, match.Index, 100);
                var key = match.Value;
                
                if (!factPatterns.ContainsKey(key))
                {
                    factPatterns[key] = (0, new List<string>(), context);
                }
                var current = factPatterns[key];
                if (!current.platforms.Contains(result.Platform))
                {
                    current.platforms.Add(result.Platform);
                }
                factPatterns[key] = (current.count + 1, current.platforms, current.context);
            }
        }

        // 转换为 CitableFact
        var sortedFacts = factPatterns
            .OrderByDescending(kv => kv.Value.count)
            .Take(15)
            .ToList();

        foreach (var (fact, (count, platforms, context)) in sortedFacts)
        {
            facts.Add(new CitableFact
            {
                Fact = context,
                FactType = DetermineFactType(context),
                Source = "AI 引用分析",
                CitationCount = count,
                CitedByPlatforms = platforms,
                UsageSuggestion = GenerateFactUsageSuggestion(fact, count),
                CredibilityScore = CalculateCredibilityScore(count, platforms.Count)
            });
        }

        // 添加通用可引用事实建议
        if (facts.Count < 5)
        {
            facts.AddRange(GetDefaultCitableFacts(brand));
        }

        return facts;
    }

    /// <summary>
    /// 关键词建议
    /// </summary>
    private List<KeywordSuggestion> GenerateKeywordSuggestions(List<CitationResultEntity> results, string brand, string topic)
    {
        var keywords = new Dictionary<string, (int count, string context)>();
        var brandLower = brand.ToLowerInvariant();

        // 从引用响应中提取关键词
        foreach (var result in results.Where(r => r.IsCited))
        {
            var words = ExtractKeywordsFromText(result.Response ?? "", brandLower);
            foreach (var word in words)
            {
                if (!keywords.ContainsKey(word))
                {
                    keywords[word] = (0, "");
                }
                var current = keywords[word];
                keywords[word] = (current.count + 1, result.Question);
            }
        }

        var suggestions = keywords
            .OrderByDescending(kv => kv.Value.count)
            .Take(20)
            .Select((kv, index) => new KeywordSuggestion
            {
                Keyword = kv.Key,
                KeywordType = index < 3 ? "primary" : (index < 10 ? "secondary" : "long_tail"),
                SearchVolume = EstimateSearchVolume(kv.Key),
                AICitationRate = results.Any() ? (double)kv.Value.count / results.Count : 0,
                UsageContext = kv.Value.context,
                Priority = index + 1
            })
            .ToList();

        return suggestions;
    }

    /// <summary>
    /// 竞品分析
    /// </summary>
    private List<CompetitorContentAnalysis> AnalyzeCompetitors(
        List<CitationResultEntity> results, 
        string brand, 
        List<string> competitors)
    {
        var analysis = new List<CompetitorContentAnalysis>();

        foreach (var competitor in competitors.Take(5))
        {
            var competitorLower = competitor.ToLowerInvariant();
            var competitorResults = results.Where(r => 
                r.Response?.Contains(competitor, StringComparison.OrdinalIgnoreCase) == true).ToList();

            var mentionRate = results.Any() ? (double)competitorResults.Count / results.Count : 0;

            // 分析竞品优势主题
            var strengthTopics = competitorResults
                .Where(r => r.CitationPosition == "first" || r.SentimentScore > 0.3)
                .Select(r => ClassifyQuestionTopic(r.Question))
                .Where(t => !string.IsNullOrEmpty(t))
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            // 分析竞品弱势主题
            var weaknessTopics = competitorResults
                .Where(r => r.CitationPosition == "last" || r.SentimentScore < -0.1)
                .Select(r => ClassifyQuestionTopic(r.Question))
                .Where(t => !string.IsNullOrEmpty(t))
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            // 识别机会
            var opportunities = IdentifyOpportunities(results, brand, competitor, strengthTopics);

            analysis.Add(new CompetitorContentAnalysis
            {
                Competitor = competitor,
                MentionRate = mentionRate,
                StrengthTopics = strengthTopics,
                WeaknessTopics = weaknessTopics,
                ContentStrategy = InferContentStrategy(strengthTopics),
                Opportunities = opportunities
            });
        }

        return analysis;
    }

    /// <summary>
    /// 生成优化清单
    /// </summary>
    private List<OptimizationChecklist> GenerateOptimizationChecklist(ContentBriefReport report)
    {
        var checklist = new List<OptimizationChecklist>();
        int priority = 1;

        // 结构优化
        checklist.Add(new OptimizationChecklist
        {
            Category = "structure",
            Item = "使用清晰的标题层级",
            Description = $"建议使用 {report.SuggestedStructure.H2Sections.Count} 个 H2 章节",
            IsRequired = true,
            Priority = priority++
        });

        checklist.Add(new OptimizationChecklist
        {
            Category = "structure",
            Item = "添加目录导航",
            Description = "便于 AI 理解内容结构",
            IsRequired = false,
            Priority = priority++
        });

        // 内容优化
        checklist.Add(new OptimizationChecklist
        {
            Category = "content",
            Item = "包含可引用的统计数据",
            Description = $"建议包含 {Math.Min(5, report.CitableFacts.Count)} 个关键数据点",
            IsRequired = true,
            Priority = priority++
        });

        checklist.Add(new OptimizationChecklist
        {
            Category = "content",
            Item = "使用品牌名称",
            Description = $"确保 '{report.Brand}' 在关键位置出现",
            IsRequired = true,
            Priority = priority++
        });

        // AI 优化
        checklist.Add(new OptimizationChecklist
        {
            Category = "ai_optimization",
            Item = "添加 FAQ Schema",
            Description = "使用 JSON-LD 标记常见问题",
            IsRequired = true,
            Priority = priority++
        });

        checklist.Add(new OptimizationChecklist
        {
            Category = "ai_optimization",
            Item = "使用直接回答格式",
            Description = "在段落开头直接回答问题",
            IsRequired = true,
            Priority = priority++
        });

        checklist.Add(new OptimizationChecklist
        {
            Category = "ai_optimization",
            Item = "添加权威来源引用",
            Description = "引用行业报告或官方数据",
            IsRequired = false,
            Priority = priority++
        });

        // SEO 优化
        checklist.Add(new OptimizationChecklist
        {
            Category = "seo",
            Item = "优化 Meta Description",
            Description = "包含主要关键词和品牌名",
            IsRequired = true,
            Priority = priority++
        });

        checklist.Add(new OptimizationChecklist
        {
            Category = "seo",
            Item = "添加内部链接",
            Description = "链接到相关内容页面",
            IsRequired = false,
            Priority = priority++
        });

        return checklist;
    }

    /// <summary>
    /// 5.12 导出内容简报
    /// </summary>
    public ContentBriefExport ExportBrief(ContentBriefReport report, string format = "markdown")
    {
        var export = new ContentBriefExport
        {
            Format = format,
            FileName = $"content-brief-{report.Brand.ToLowerInvariant().Replace(" ", "-")}-{DateTime.UtcNow:yyyyMMdd}.{GetFileExtension(format)}"
        };

        export.Content = format switch
        {
            "markdown" => ExportToMarkdown(report),
            "html" => ExportToHtml(report),
            "json" => System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            _ => ExportToMarkdown(report)
        };

        return export;
    }

    #region Helper Methods

    private string ClassifyQuestionTopic(string question)
    {
        var q = question.ToLowerInvariant();
        
        if (q.Contains("最好") || q.Contains("推荐") || q.Contains("best") || q.Contains("recommend"))
            return "产品推荐";
        if (q.Contains("比较") || q.Contains("对比") || q.Contains("vs") || q.Contains("compare"))
            return "产品对比";
        if (q.Contains("如何") || q.Contains("怎么") || q.Contains("how to"))
            return "使用指南";
        if (q.Contains("是什么") || q.Contains("什么是") || q.Contains("what is"))
            return "概念解释";
        if (q.Contains("价格") || q.Contains("多少钱") || q.Contains("cost") || q.Contains("price"))
            return "价格咨询";
        if (q.Contains("优点") || q.Contains("缺点") || q.Contains("pros") || q.Contains("cons"))
            return "优缺点分析";
        if (q.Contains("评价") || q.Contains("评测") || q.Contains("review"))
            return "产品评测";
        if (q.Contains("替代") || q.Contains("alternative"))
            return "替代方案";
        
        return "";
    }

    private string GetTopicDescription(string topic)
    {
        return topic switch
        {
            "产品推荐" => "用户寻求产品推荐时，AI 经常引用的内容类型",
            "产品对比" => "用户比较产品时，AI 引用的对比分析内容",
            "使用指南" => "用户寻求操作指导时，AI 引用的教程内容",
            "概念解释" => "用户了解概念时，AI 引用的定义和解释",
            "价格咨询" => "用户询问价格时，AI 引用的定价信息",
            "优缺点分析" => "用户评估产品时，AI 引用的分析内容",
            "产品评测" => "用户寻求评价时，AI 引用的评测内容",
            "替代方案" => "用户寻找替代品时，AI 引用的对比内容",
            _ => "AI 经常引用的内容主题"
        };
    }

    private string GenerateTopicReason(string topic, int count, double rate)
    {
        return $"在 {count} 次引用中出现，引用率 {rate:P1}，是 AI 高频引用的主题类型";
    }

    private string GetSuggestedContentType(string topic)
    {
        return topic switch
        {
            "产品推荐" => "guide",
            "产品对比" => "comparison",
            "使用指南" => "guide",
            "概念解释" => "article",
            "价格咨询" => "article",
            "优缺点分析" => "comparison",
            "产品评测" => "article",
            "替代方案" => "comparison",
            _ => "article"
        };
    }

    private List<AlignedTopic> GetDefaultTopicSuggestions(string brand, string topic, int existingCount)
    {
        var defaults = new List<AlignedTopic>
        {
            new() { Topic = "产品推荐", Description = "创建推荐类内容", Priority = existingCount + 1, SuggestedContentType = "guide" },
            new() { Topic = "使用指南", Description = "创建操作教程", Priority = existingCount + 2, SuggestedContentType = "guide" },
            new() { Topic = "常见问题", Description = "创建 FAQ 页面", Priority = existingCount + 3, SuggestedContentType = "faq" }
        };
        return defaults.Take(3 - existingCount).ToList();
    }

    private string GenerateH1Title(string brand, string topic, string contentType)
    {
        return contentType switch
        {
            "faq" => $"{brand} 常见问题解答 - {topic}",
            "comparison" => $"{brand} vs 竞品对比：{topic}完整指南",
            "guide" => $"{brand} {topic}完整使用指南",
            _ => $"{brand} {topic}：专业解析与建议"
        };
    }

    private List<HeadingItem> GenerateArticleStructure(List<CitationResultEntity> results, string brand, string topic)
    {
        return new List<HeadingItem>
        {
            new() { Heading = $"什么是 {brand}？", Description = "品牌/产品介绍", EstimatedWordCount = 200, KeyPoints = new() { "核心定位", "主要功能", "目标用户" } },
            new() { Heading = $"{brand} 的核心优势", Description = "主要卖点", EstimatedWordCount = 300, KeyPoints = new() { "功能优势", "技术优势", "服务优势" } },
            new() { Heading = $"如何使用 {brand}", Description = "使用指南", EstimatedWordCount = 400, KeyPoints = new() { "入门步骤", "进阶技巧", "最佳实践" } },
            new() { Heading = $"{brand} 适合谁？", Description = "目标用户", EstimatedWordCount = 200, KeyPoints = new() { "适用场景", "用户画像" } },
            new() { Heading = "常见问题", Description = "FAQ", EstimatedWordCount = 300, KeyPoints = new() { "价格问题", "功能问题", "支持问题" } },
            new() { Heading = "总结", Description = "结论", EstimatedWordCount = 100, KeyPoints = new() { "核心价值", "行动建议" } }
        };
    }

    private List<HeadingItem> GenerateFAQStructure(List<CitationResultEntity> results, string brand, string topic)
    {
        var faqs = results
            .Where(r => r.IsCited)
            .Select(r => r.Question)
            .Distinct()
            .Take(10)
            .Select(q => new HeadingItem { Heading = q, Description = "FAQ 问题", EstimatedWordCount = 150 })
            .ToList();

        if (faqs.Count < 5)
        {
            faqs.AddRange(new[]
            {
                new HeadingItem { Heading = $"{brand} 是什么？", EstimatedWordCount = 150 },
                new HeadingItem { Heading = $"{brand} 多少钱？", EstimatedWordCount = 150 },
                new HeadingItem { Heading = $"{brand} 怎么用？", EstimatedWordCount = 150 },
                new HeadingItem { Heading = $"{brand} 有什么优势？", EstimatedWordCount = 150 },
                new HeadingItem { Heading = $"如何联系 {brand}？", EstimatedWordCount = 150 }
            }.Take(5 - faqs.Count));
        }

        return faqs;
    }

    private List<HeadingItem> GenerateComparisonStructure(List<CitationResultEntity> results, string brand, string topic)
    {
        return new List<HeadingItem>
        {
            new() { Heading = "对比概述", Description = "快速对比", EstimatedWordCount = 200 },
            new() { Heading = "功能对比", Description = "详细功能对比", EstimatedWordCount = 400, H3Subsections = new() { "核心功能", "高级功能", "集成能力" } },
            new() { Heading = "价格对比", Description = "定价方案对比", EstimatedWordCount = 300 },
            new() { Heading = "用户体验对比", Description = "易用性对比", EstimatedWordCount = 250 },
            new() { Heading = "适用场景", Description = "各自适用场景", EstimatedWordCount = 200 },
            new() { Heading = "结论：如何选择", Description = "选择建议", EstimatedWordCount = 150 }
        };
    }

    private List<HeadingItem> GenerateGuideStructure(List<CitationResultEntity> results, string brand, string topic)
    {
        return new List<HeadingItem>
        {
            new() { Heading = "入门准备", Description = "开始前的准备", EstimatedWordCount = 150 },
            new() { Heading = "第一步：注册与设置", Description = "基础设置", EstimatedWordCount = 200 },
            new() { Heading = "第二步：核心功能使用", Description = "主要功能", EstimatedWordCount = 400 },
            new() { Heading = "第三步：进阶技巧", Description = "高级用法", EstimatedWordCount = 300 },
            new() { Heading = "常见问题与解决方案", Description = "问题排查", EstimatedWordCount = 250 },
            new() { Heading = "最佳实践", Description = "专家建议", EstimatedWordCount = 200 }
        };
    }

    private string GenerateStructureRationale(string contentType, int sectionCount)
    {
        return $"基于 {contentType} 类型内容的最佳实践，建议使用 {sectionCount} 个主要章节，便于 AI 提取和引用关键信息。";
    }

    private string ExtractFactContext(string text, int index, int contextLength)
    {
        var start = Math.Max(0, index - contextLength / 2);
        var end = Math.Min(text.Length, index + contextLength / 2);
        return text[start..end].Trim();
    }

    private string DetermineFactType(string context)
    {
        if (context.Contains("%") || Regex.IsMatch(context, @"\d+(?:,\d{3})+"))
            return "statistic";
        if (context.Contains("研究") || context.Contains("报告"))
            return "research";
        if (context.Contains("案例") || context.Contains("客户"))
            return "case_study";
        return "statistic";
    }

    private string GenerateFactUsageSuggestion(string fact, int count)
    {
        if (count >= 3)
            return "高频引用数据，建议在内容开头或关键章节使用";
        if (count >= 2)
            return "中频引用数据，建议在支撑论点时使用";
        return "可作为补充数据使用";
    }

    private double CalculateCredibilityScore(int citationCount, int platformCount)
    {
        var score = Math.Min(1.0, citationCount * 0.2 + platformCount * 0.15);
        return Math.Round(score, 2);
    }

    private List<CitableFact> GetDefaultCitableFacts(string brand)
    {
        return new List<CitableFact>
        {
            new() { Fact = "添加具体的用户数量或增长数据", FactType = "statistic", UsageSuggestion = "在介绍品牌规模时使用", CredibilityScore = 0.8 },
            new() { Fact = "添加客户满意度或 NPS 评分", FactType = "statistic", UsageSuggestion = "在证明产品价值时使用", CredibilityScore = 0.8 },
            new() { Fact = "添加行业奖项或认证", FactType = "quote", UsageSuggestion = "在建立权威性时使用", CredibilityScore = 0.9 }
        };
    }

    private List<string> ExtractKeywordsFromText(string text, string excludeBrand)
    {
        var words = Regex.Split(text.ToLowerInvariant(), @"\W+")
            .Where(w => w.Length >= 2 && w.Length <= 20)
            .Where(w => !w.Contains(excludeBrand))
            .Where(w => !IsStopWord(w))
            .Distinct()
            .ToList();
        return words;
    }

    private bool IsStopWord(string word)
    {
        var stopWords = new HashSet<string> { "的", "是", "在", "和", "了", "有", "这", "个", "为", "与", "the", "a", "an", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should", "may", "might", "must", "shall", "can", "need", "dare", "ought", "used", "to", "of", "in", "for", "on", "with", "at", "by", "from", "as", "into", "through", "during", "before", "after", "above", "below", "between", "under", "again", "further", "then", "once" };
        return stopWords.Contains(word);
    }

    private int EstimateSearchVolume(string keyword)
    {
        // 简化的搜索量估算
        return keyword.Length switch
        {
            <= 3 => 10000,
            <= 6 => 5000,
            <= 10 => 1000,
            _ => 500
        };
    }

    private List<string> IdentifyOpportunities(List<CitationResultEntity> results, string brand, string competitor, List<string> competitorStrengths)
    {
        var opportunities = new List<string>();
        
        // 找出竞品被引用但品牌未被引用的问题
        var competitorQuestions = results
            .Where(r => r.Response?.Contains(competitor, StringComparison.OrdinalIgnoreCase) == true)
            .Select(r => r.Question)
            .ToHashSet();

        var brandQuestions = results
            .Where(r => r.IsCited)
            .Select(r => r.Question)
            .ToHashSet();

        var gaps = competitorQuestions.Except(brandQuestions).Take(3).ToList();
        foreach (var gap in gaps)
        {
            opportunities.Add($"创建针对 '{gap}' 的内容");
        }

        if (competitorStrengths.Any())
        {
            opportunities.Add($"在 {competitorStrengths.First()} 主题上加强内容");
        }

        return opportunities;
    }

    private string InferContentStrategy(List<string> strengthTopics)
    {
        if (!strengthTopics.Any())
            return "内容策略不明确";
        
        return strengthTopics.First() switch
        {
            "产品推荐" => "以推荐类内容为主，强调产品优势",
            "产品对比" => "以对比内容为主，突出差异化",
            "使用指南" => "以教程内容为主，强调易用性",
            _ => "综合内容策略"
        };
    }

    private string GenerateBriefSummary(ContentBriefReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## 内容简报摘要 - {report.Brand}");
        sb.AppendLine();
        sb.AppendLine($"**目标主题**: {report.TargetTopic}");
        sb.AppendLine($"**推荐主题数**: {report.RecommendedTopics.Count}");
        sb.AppendLine($"**建议字数**: {report.SuggestedStructure.EstimatedWordCount}");
        sb.AppendLine($"**可引用事实**: {report.CitableFacts.Count}");
        sb.AppendLine();
        
        if (report.RecommendedTopics.Any())
        {
            sb.AppendLine("**优先主题**:");
            foreach (var topic in report.RecommendedTopics.Take(3))
            {
                sb.AppendLine($"- {topic.Topic} (引用率: {topic.CitationFrequency:P1})");
            }
        }

        return sb.ToString();
    }

    private string GetFileExtension(string format)
    {
        return format switch
        {
            "markdown" => "md",
            "html" => "html",
            "json" => "json",
            _ => "md"
        };
    }

    private string ExportToMarkdown(ContentBriefReport report)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"# 内容简报：{report.Brand}");
        sb.AppendLine($"*生成时间: {report.GeneratedAt:yyyy-MM-dd HH:mm}*");
        sb.AppendLine();
        
        // 摘要
        sb.AppendLine("## 摘要");
        sb.AppendLine(report.Summary);
        sb.AppendLine();
        
        // 推荐主题
        sb.AppendLine("## AI 对齐主题推荐");
        foreach (var topic in report.RecommendedTopics)
        {
            sb.AppendLine($"### {topic.Priority}. {topic.Topic}");
            sb.AppendLine($"- **描述**: {topic.Description}");
            sb.AppendLine($"- **引用频率**: {topic.CitationFrequency:P1}");
            sb.AppendLine($"- **建议内容类型**: {topic.SuggestedContentType}");
            sb.AppendLine($"- **推荐原因**: {topic.Reason}");
            sb.AppendLine();
        }
        
        // 标题结构
        sb.AppendLine("## 建议标题结构");
        sb.AppendLine($"### H1: {report.SuggestedStructure.SuggestedH1}");
        sb.AppendLine();
        foreach (var section in report.SuggestedStructure.H2Sections)
        {
            sb.AppendLine($"#### H2: {section.Heading}");
            sb.AppendLine($"- 描述: {section.Description}");
            sb.AppendLine($"- 预估字数: {section.EstimatedWordCount}");
            if (section.KeyPoints.Any())
            {
                sb.AppendLine($"- 要点: {string.Join(", ", section.KeyPoints)}");
            }
            sb.AppendLine();
        }
        
        // 可引用事实
        sb.AppendLine("## 可引用事实");
        foreach (var fact in report.CitableFacts.Take(10))
        {
            sb.AppendLine($"- **{fact.Fact}**");
            sb.AppendLine($"  - 类型: {fact.FactType}, 引用次数: {fact.CitationCount}");
            sb.AppendLine($"  - 使用建议: {fact.UsageSuggestion}");
        }
        sb.AppendLine();
        
        // 优化清单
        sb.AppendLine("## 优化清单");
        var categories = report.Checklist.GroupBy(c => c.Category);
        foreach (var category in categories)
        {
            sb.AppendLine($"### {category.Key}");
            foreach (var item in category)
            {
                var required = item.IsRequired ? "✅ 必需" : "⬜ 可选";
                sb.AppendLine($"- [{required}] {item.Item}: {item.Description}");
            }
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    private string ExportToHtml(ContentBriefReport report)
    {
        var markdown = ExportToMarkdown(report);
        // 简单的 Markdown 到 HTML 转换
        var html = markdown
            .Replace("# ", "<h1>").Replace("\n## ", "</h1>\n<h2>").Replace("\n### ", "</h2>\n<h3>").Replace("\n#### ", "</h3>\n<h4>")
            .Replace("\n- ", "\n<li>").Replace("**", "<strong>").Replace("*", "<em>");
        
        return $"<!DOCTYPE html><html><head><meta charset='utf-8'><title>内容简报 - {report.Brand}</title></head><body>{html}</body></html>";
    }

    #endregion
}
