namespace GeoCore.SaaS.Services.Suggestion;

/// <summary>
/// 建议规则库 - 包含所有预定义的优化建议规则
/// </summary>
public static class SuggestionRules
{
    /// <summary>
    /// 获取所有规则
    /// </summary>
    public static List<SuggestionRule> GetAllRules()
    {
        var rules = new List<SuggestionRule>();
        rules.AddRange(GetAiVisibilityRules());
        rules.AddRange(GetWebsiteTechRules());
        rules.AddRange(GetContentQualityRules());
        rules.AddRange(GetSeoRules());
        return rules;
    }

    #region AI 可见度规则

    public static List<SuggestionRule> GetAiVisibilityRules() => new()
    {
        // 品牌提及优化
        new SuggestionRule
        {
            RuleId = "ai_vis_001",
            Category = SuggestionCategory.AiVisibility,
            Subcategory = SuggestionSubcategory.BrandMention,
            Condition = "brand_mention_rate < 0.3",
            Priority = SuggestionPriority.High,
            ImpactScore = 9,
            EffortLevel = EffortLevel.Medium,
            Title = "品牌提及率过低",
            Description = "您的品牌在 AI 回答中的提及率仅为 {brand_mention_rate}%，低于行业平均水平 30%。这表明 AI 模型对您的品牌认知度不足。",
            ActionItems = new List<string>
            {
                "确保品牌名称在官网、社交媒体、第三方平台保持一致",
                "在高权威平台（Wikipedia、行业媒体）增加品牌曝光",
                "创建品牌故事和差异化内容",
                "增加用户评价和案例内容"
            },
            ReferenceUrls = new List<string> { "https://geocoreai.com/docs/geo-brand-optimization" }
        },
        new SuggestionRule
        {
            RuleId = "ai_vis_002",
            Category = SuggestionCategory.AiVisibility,
            Subcategory = SuggestionSubcategory.BrandMention,
            Condition = "avg_mention_position > 5",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 6,
            EffortLevel = EffortLevel.Medium,
            Title = "品牌提及位置靠后",
            Description = "您的品牌在 AI 回答中的平均提及位置为第 {avg_mention_position} 位，建议优化到前 3 位。",
            ActionItems = new List<string>
            {
                "分析竞品的内容策略",
                "增加品牌在行业关键词相关内容中的出现频率",
                "优化品牌关键词密度",
                "在问答平台积极回答行业问题"
            }
        },

        // 引用源优化
        new SuggestionRule
        {
            RuleId = "ai_vis_003",
            Category = SuggestionCategory.AiVisibility,
            Subcategory = SuggestionSubcategory.CitationSource,
            Condition = "user_site_cited == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 10,
            EffortLevel = EffortLevel.Hard,
            Title = "官网未被 AI 引用",
            Description = "您的官网尚未被任何 AI 模型作为引用源。这意味着 AI 不认为您的网站是可信的信息来源。",
            ActionItems = new List<string>
            {
                "提升网站内容的权威性和专业性",
                "添加原创研究数据和行业报告",
                "确保内容有明确的作者信息和发布日期",
                "增加外部权威网站的引用和链接",
                "优化网站技术配置（允许 AI 爬虫）"
            }
        },
        new SuggestionRule
        {
            RuleId = "ai_vis_004",
            Category = SuggestionCategory.AiVisibility,
            Subcategory = SuggestionSubcategory.CitationSource,
            Condition = "citation_count < 100",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 7,
            EffortLevel = EffortLevel.Hard,
            Title = "引用数量较少",
            Description = "您的品牌相关内容被 AI 引用的次数为 {citation_count}，建议通过内容营销增加引用。",
            ActionItems = new List<string>
            {
                "在 Reddit、Medium、知乎等平台发布高质量内容",
                "与行业 KOL 合作创作内容",
                "发布行业白皮书和研究报告",
                "参与行业论坛和问答社区"
            }
        },

        // 问答内容优化
        new SuggestionRule
        {
            RuleId = "ai_vis_005",
            Category = SuggestionCategory.AiVisibility,
            Subcategory = SuggestionSubcategory.QaContent,
            Condition = "has_faq_page == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 8,
            EffortLevel = EffortLevel.Medium,
            Title = "缺少 FAQ 页面",
            Description = "您的网站缺少专门的 FAQ 页面。FAQ 内容是 AI 模型最容易引用的内容类型之一。",
            ActionItems = new List<string>
            {
                "创建专门的 FAQ 页面",
                "收集用户常见问题",
                "为每个问题提供清晰、简洁的回答",
                "添加 FAQ Schema 标记"
            }
        }
    };

    #endregion

    #region 网站技术规则

    public static List<SuggestionRule> GetWebsiteTechRules() => new()
    {
        // AI 爬虫配置
        new SuggestionRule
        {
            RuleId = "tech_001",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.AiCrawler,
            Condition = "ai_crawlers_allowed == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 10,
            EffortLevel = EffortLevel.Easy,
            Title = "robots.txt 阻止了 AI 爬虫",
            Description = "您的 robots.txt 文件阻止了 {blocked_crawlers} 爬取网站内容。这会严重影响 AI 可见度。",
            ActionItems = new List<string>
            {
                "编辑 robots.txt 文件",
                "添加允许 AI 爬虫的规则",
                "保存并验证配置"
            },
            ExampleCode = @"# 允许 AI 爬虫
User-agent: GPTBot
Allow: /

User-agent: ClaudeBot
Allow: /

User-agent: Google-Extended
Allow: /

User-agent: anthropic-ai
Allow: /",
            ReferenceUrls = new List<string>
            {
                "https://platform.openai.com/docs/gptbot",
                "https://www.anthropic.com/claude-bot"
            }
        },
        new SuggestionRule
        {
            RuleId = "tech_002",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.AiCrawler,
            Condition = "llms_txt_exists == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 5,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少 llms.txt 文件",
            Description = "您的网站缺少 llms.txt 文件。llms.txt 可以帮助 AI 模型更好地理解您的网站内容。",
            ActionItems = new List<string>
            {
                "在网站根目录创建 llms.txt 文件",
                "添加网站描述、主要内容类型等信息",
                "定期更新文件内容"
            },
            ExampleCode = @"# llms.txt
# 网站名称
name: Example Company

# 网站描述
description: 我们是一家专注于...

# 主要内容
content_types:
  - 产品信息
  - 技术文档
  - 行业资讯

# 联系方式
contact: info@example.com"
        },
        new SuggestionRule
        {
            RuleId = "tech_003",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.AiCrawler,
            Condition = "sitemap_exists == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 7,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少 sitemap.xml",
            Description = "您的网站缺少 sitemap.xml 文件。Sitemap 帮助搜索引擎和 AI 爬虫发现您的所有页面。",
            ActionItems = new List<string>
            {
                "生成 sitemap.xml 文件",
                "包含所有重要页面的 URL",
                "在 robots.txt 中声明 sitemap 位置",
                "定期更新 sitemap"
            }
        },

        // 页面技术
        new SuggestionRule
        {
            RuleId = "tech_004",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.PageTech,
            Condition = "https_enabled == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 9,
            EffortLevel = EffortLevel.Medium,
            Title = "未启用 HTTPS",
            Description = "您的网站未启用 HTTPS。HTTPS 是搜索引擎和 AI 模型信任网站的基本要求。",
            ActionItems = new List<string>
            {
                "获取 SSL 证书",
                "配置服务器启用 HTTPS",
                "设置 HTTP 到 HTTPS 的重定向",
                "更新所有内部链接为 HTTPS"
            }
        },
        new SuggestionRule
        {
            RuleId = "tech_005",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.PageTech,
            Condition = "has_canonical == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 5,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少 Canonical 标签",
            Description = "您的页面缺少 canonical 标签。这可能导致重复内容问题，影响搜索引擎和 AI 的理解。",
            ActionItems = new List<string>
            {
                "为每个页面添加 canonical 标签",
                "确保 canonical URL 指向正确的规范页面"
            },
            ExampleCode = @"<link rel=""canonical"" href=""https://www.example.com/page-url"" />"
        },

        // 结构化数据
        new SuggestionRule
        {
            RuleId = "tech_006",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.StructuredData,
            Condition = "has_schema == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 8,
            EffortLevel = EffortLevel.Medium,
            Title = "缺少 Schema 标记",
            Description = "您的网站缺少 Schema.org 结构化数据标记。结构化数据帮助 AI 更好地理解您的内容。",
            ActionItems = new List<string>
            {
                "添加 Organization Schema 标记",
                "为产品页面添加 Product Schema",
                "为文章添加 Article Schema",
                "使用 Google 结构化数据测试工具验证"
            }
        },
        new SuggestionRule
        {
            RuleId = "tech_007",
            Category = SuggestionCategory.WebsiteTech,
            Subcategory = SuggestionSubcategory.StructuredData,
            Condition = "has_faq_schema == false && has_faq_content == true",
            Priority = SuggestionPriority.High,
            ImpactScore = 8,
            EffortLevel = EffortLevel.Easy,
            Title = "FAQ 内容缺少 Schema 标记",
            Description = "您的网站有 FAQ 内容，但缺少 FAQPage Schema 标记。添加后可以提高在 AI 搜索中的展示机会。",
            ActionItems = new List<string>
            {
                "为 FAQ 页面添加 FAQPage Schema",
                "确保每个问答对都有正确的标记",
                "验证 Schema 标记的正确性"
            },
            ExampleCode = @"<script type=""application/ld+json"">
{
  ""@context"": ""https://schema.org"",
  ""@type"": ""FAQPage"",
  ""mainEntity"": [{
    ""@type"": ""Question"",
    ""name"": ""问题内容？"",
    ""acceptedAnswer"": {
      ""@type"": ""Answer"",
      ""text"": ""答案内容。""
    }
  }]
}
</script>"
        }
    };

    #endregion

    #region 内容质量规则

    public static List<SuggestionRule> GetContentQualityRules() => new()
    {
        // E-E-A-T 优化
        new SuggestionRule
        {
            RuleId = "content_001",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.Eeat,
            Condition = "has_author_info == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 8,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少作者信息",
            Description = "您的内容缺少作者信息。作者信息是 E-E-A-T（经验、专业、权威、可信）的重要组成部分。",
            ActionItems = new List<string>
            {
                "为每篇文章添加作者署名",
                "创建作者简介页面",
                "展示作者的专业资质和经验",
                "添加作者的社交媒体链接"
            }
        },
        new SuggestionRule
        {
            RuleId = "content_002",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.Eeat,
            Condition = "has_publish_date == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 6,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少发布日期",
            Description = "您的内容缺少发布日期。日期信息帮助 AI 判断内容的时效性。",
            ActionItems = new List<string>
            {
                "为所有文章添加发布日期",
                "显示最后更新日期",
                "使用 Schema 标记日期信息"
            }
        },
        new SuggestionRule
        {
            RuleId = "content_003",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.Eeat,
            Condition = "has_citations == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 7,
            EffortLevel = EffortLevel.Medium,
            Title = "缺少引用来源",
            Description = "您的内容缺少外部引用来源。引用权威来源可以提高内容的可信度。",
            ActionItems = new List<string>
            {
                "为数据和事实添加来源引用",
                "链接到权威的外部资源",
                "使用脚注或参考文献格式"
            }
        },

        // 内容结构
        new SuggestionRule
        {
            RuleId = "content_004",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.ContentStructure,
            Condition = "heading_structure_ok == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 6,
            EffortLevel = EffortLevel.Easy,
            Title = "标题结构不规范",
            Description = "您的页面标题结构不规范（H1/H2/H3 层级混乱）。良好的标题结构帮助 AI 理解内容层次。",
            ActionItems = new List<string>
            {
                "确保每个页面只有一个 H1 标签",
                "使用 H2 作为主要章节标题",
                "使用 H3 作为子章节标题",
                "保持标题层级的逻辑性"
            }
        },
        new SuggestionRule
        {
            RuleId = "content_005",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.ContentStructure,
            Condition = "meta_title_ok == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 5,
            EffortLevel = EffortLevel.Easy,
            Title = "Meta Title 需要优化",
            Description = "您的页面 Meta Title 过长、过短或缺失。优化后的 Title 可以提高点击率和 AI 理解。",
            ActionItems = new List<string>
            {
                "确保 Title 长度在 50-60 字符之间",
                "包含主要关键词",
                "使 Title 具有描述性和吸引力"
            }
        },
        new SuggestionRule
        {
            RuleId = "content_006",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.ContentStructure,
            Condition = "has_meta_description == false",
            Priority = SuggestionPriority.Medium,
            ImpactScore = 5,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少 Meta Description",
            Description = "您的页面缺少 Meta Description。描述标签帮助搜索引擎和 AI 理解页面内容摘要。",
            ActionItems = new List<string>
            {
                "为每个页面添加 Meta Description",
                "确保描述长度在 150-160 字符之间",
                "包含主要关键词和价值主张"
            }
        },

        // Answer Capsule
        new SuggestionRule
        {
            RuleId = "content_007",
            Category = SuggestionCategory.ContentQuality,
            Subcategory = SuggestionSubcategory.AnswerCapsule,
            Condition = "has_answer_capsules == false",
            Priority = SuggestionPriority.High,
            ImpactScore = 9,
            EffortLevel = EffortLevel.Medium,
            Title = "缺少 Answer Capsule",
            Description = "您的内容缺少 Answer Capsule（直接回答问题的精炼段落）。这是 AI 最容易引用的内容格式。",
            ActionItems = new List<string>
            {
                "在文章开头添加 40-60 字的核心摘要",
                "使用清晰、直接的语言回答问题",
                "将 Answer Capsule 放在显眼位置",
                "使用粗体或特殊样式突出显示"
            },
            ExampleCode = @"<div class=""answer-capsule"">
  <strong>简短回答：</strong>
  [在这里用 40-60 字直接回答问题的核心内容]
</div>"
        }
    };

    #endregion

    #region SEO 规则

    public static List<SuggestionRule> GetSeoRules() => new()
    {
        new SuggestionRule
        {
            RuleId = "seo_001",
            Category = SuggestionCategory.Seo,
            Subcategory = SuggestionSubcategory.PageOptimization,
            Condition = "has_og_tags == false",
            Priority = SuggestionPriority.Low,
            ImpactScore = 3,
            EffortLevel = EffortLevel.Easy,
            Title = "缺少 Open Graph 标签",
            Description = "您的页面缺少 Open Graph 标签。OG 标签可以优化社交媒体分享效果。",
            ActionItems = new List<string>
            {
                "添加 og:title 标签",
                "添加 og:description 标签",
                "添加 og:image 标签",
                "添加 og:url 标签"
            },
            ExampleCode = @"<meta property=""og:title"" content=""页面标题"" />
<meta property=""og:description"" content=""页面描述"" />
<meta property=""og:image"" content=""https://example.com/image.jpg"" />
<meta property=""og:url"" content=""https://example.com/page"" />"
        },
        new SuggestionRule
        {
            RuleId = "seo_002",
            Category = SuggestionCategory.Seo,
            Subcategory = SuggestionSubcategory.PageOptimization,
            Condition = "external_link_count < 3",
            Priority = SuggestionPriority.Low,
            ImpactScore = 4,
            EffortLevel = EffortLevel.Easy,
            Title = "外部链接过少",
            Description = "您的页面外部链接数量较少。适当的外部链接可以提高内容的可信度。",
            ActionItems = new List<string>
            {
                "添加指向权威来源的外部链接",
                "引用行业标准和研究报告",
                "链接到相关的官方文档"
            }
        }
    };

    #endregion
}
