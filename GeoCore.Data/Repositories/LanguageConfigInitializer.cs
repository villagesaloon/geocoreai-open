using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 语言配置初始化器 - 预置默认配置
/// </summary>
public class LanguageConfigInitializer
{
    private readonly GeoDbContext _context;

    public LanguageConfigInitializer(GeoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 初始化默认配置（如果表为空）
    /// </summary>
    public async Task InitializeAsync()
    {
        var hasLanguages = await _context.Client.Queryable<LanguageConfigEntity>().AnyAsync();
        if (!hasLanguages)
        {
            await InitializeLanguagesAsync();
            Console.WriteLine("[LanguageConfig] 语言配置初始化完成");
        }

        var hasPatterns = await _context.Client.Queryable<ExtractionPatternEntity>().AnyAsync();
        if (!hasPatterns)
        {
            await InitializePatternsAsync();
            Console.WriteLine("[LanguageConfig] 提取模式初始化完成");
        }

        var hasEntities = await _context.Client.Queryable<KnownEntityEntity>().AnyAsync();
        if (!hasEntities)
        {
            await InitializeEntitiesAsync();
            Console.WriteLine("[LanguageConfig] 已知实体初始化完成");
        }

        var hasSentiments = await _context.Client.Queryable<SentimentKeywordEntity>().AnyAsync();
        if (!hasSentiments)
        {
            await InitializeSentimentKeywordsAsync();
            Console.WriteLine("[LanguageConfig] 情感关键词初始化完成");
        }

        var hasExclusions = await _context.Client.Queryable<KeywordExclusionEntity>().AnyAsync();
        if (!hasExclusions)
        {
            await InitializeKeywordExclusionsAsync();
            Console.WriteLine("[LanguageConfig] 关键词排除词初始化完成");
        }
    }

    private async Task InitializeLanguagesAsync()
    {
        var languages = new List<LanguageConfigEntity>
        {
            new()
            {
                LanguageCode = "zh",
                LanguageName = "简体中文",
                LanguageFamily = "cjk",
                TokenizationMethod = "character",
                SentenceDelimiters = "[\"。\", \"！\", \"？\", \"；\"]",
                QuoteCharPairs = "[[\"\u201c\", \"\u201d\"], [\"\u2018\", \"\u2019\"]]",
                SortOrder = 1
            },
            new()
            {
                LanguageCode = "zh_tw",
                LanguageName = "繁體中文",
                LanguageFamily = "cjk",
                TokenizationMethod = "character",
                SentenceDelimiters = "[\"。\", \"！\", \"？\", \"；\"]",
                QuoteCharPairs = "[[\"\u300c\", \"\u300d\"], [\"\u300e\", \"\u300f\"]]",
                SortOrder = 2
            },
            new()
            {
                LanguageCode = "en",
                LanguageName = "English",
                LanguageFamily = "latin",
                TokenizationMethod = "space",
                SentenceDelimiters = "[\".\", \"!\", \"?\"]",
                QuoteCharPairs = "[[\"\\\"\", \"\\\"\"]]",
                SortOrder = 3
            },
            new()
            {
                LanguageCode = "ja",
                LanguageName = "日本語",
                LanguageFamily = "cjk",
                TokenizationMethod = "character",
                SentenceDelimiters = "[\"。\", \"！\", \"？\"]",
                QuoteCharPairs = "[[\"\u300c\", \"\u300d\"], [\"\u300e\", \"\u300f\"]]",
                SortOrder = 4
            },
            new()
            {
                LanguageCode = "ko",
                LanguageName = "한국어",
                LanguageFamily = "cjk",
                TokenizationMethod = "space",
                SentenceDelimiters = "[\".\", \"!\", \"?\"]",
                QuoteCharPairs = "[[\"\u201c\", \"\u201d\"]]",
                SortOrder = 5
            },
            new()
            {
                LanguageCode = "de",
                LanguageName = "Deutsch",
                LanguageFamily = "latin",
                TokenizationMethod = "space",
                SentenceDelimiters = "[\".\", \"!\", \"?\"]",
                QuoteCharPairs = "[[\"\u201e\", \"\u201c\"]]",
                SortOrder = 6
            },
            new()
            {
                LanguageCode = "fr",
                LanguageName = "Français",
                LanguageFamily = "latin",
                TokenizationMethod = "space",
                SentenceDelimiters = "[\".\", \"!\", \"?\"]",
                QuoteCharPairs = "[[\"\u00ab\", \"\u00bb\"]]",
                SortOrder = 7
            },
            new()
            {
                LanguageCode = "es",
                LanguageName = "Español",
                LanguageFamily = "latin",
                TokenizationMethod = "space",
                SentenceDelimiters = "[\".\", \"!\", \"?\", \"¡\", \"¿\"]",
                QuoteCharPairs = "[[\"\u00ab\", \"\u00bb\"], [\"\\\"\", \"\\\"\"]]",
                SortOrder = 8
            }
        };

        foreach (var lang in languages)
        {
            lang.CreatedAt = DateTime.UtcNow;
            lang.UpdatedAt = DateTime.UtcNow;
        }

        await _context.Client.Insertable(languages).ExecuteCommandAsync();
    }

    private async Task InitializePatternsAsync()
    {
        var patterns = new List<ExtractionPatternEntity>();

        // ========== 全局通用模式 ==========
        
        // 数字型 claims - 全局通用
        patterns.AddRange(new[]
        {
            new ExtractionPatternEntity { Category = "claim_number", Scope = "global", Pattern = @"\d+\.?\d*\s*[%％]", Description = "百分比" },
            new ExtractionPatternEntity { Category = "claim_number", Scope = "global", Pattern = @"\d+\.?\d*\s*[xX×]", Description = "倍数" },
            new ExtractionPatternEntity { Category = "claim_number", Scope = "global", Pattern = @"\d{4}", Description = "年份" },
            new ExtractionPatternEntity { Category = "claim_number", Scope = "global", Pattern = @"\d+\.?\d*\s*(million|billion|thousand)", Description = "英文数量词" },
        });

        // ========== CJK 语系模式 ==========
        
        patterns.AddRange(new[]
        {
            new ExtractionPatternEntity { Category = "claim_number", Scope = "family", ScopeValue = "cjk", Pattern = @"\d+\.?\d*\s*[万亿千百]", Description = "中文数量词" },
            new ExtractionPatternEntity { Category = "claim_number", Scope = "family", ScopeValue = "cjk", Pattern = @"\d{4}\s*年", Description = "年份（带年字）" },
            new ExtractionPatternEntity { Category = "claim_number", Scope = "family", ScopeValue = "cjk", Pattern = @"第\s*\d+", Description = "序数词" },
            new ExtractionPatternEntity { Category = "claim_number", Scope = "family", ScopeValue = "cjk", Pattern = @"\d+\s*(个|项|条|种|款|家|人|次|天|小时|分钟)", Description = "量词" },
        });

        // ========== 中文特定模式 ==========
        
        patterns.AddRange(new[]
        {
            // 统计型
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "zh", Pattern = @"根据.{2,30}(研究|调查|报告|数据|统计)", Description = "根据...研究" },
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "zh", Pattern = @"数据(显示|表明|证明).{5,50}", Description = "数据显示" },
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "zh", Pattern = @"(研究|调查|报告)(显示|表明|发现).{5,50}", Description = "研究显示" },
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "zh", Pattern = @"超过.{2,20}(用户|客户|企业|公司)", Description = "超过...用户" },
            
            // 引用型
            new ExtractionPatternEntity { Category = "claim_citation", Scope = "language", ScopeValue = "zh", Pattern = @"(专家|学者|分析师|CEO|创始人|负责人).{0,10}(表示|认为|指出|说)", Description = "专家表示" },
            new ExtractionPatternEntity { Category = "claim_citation", Scope = "language", ScopeValue = "zh", Pattern = @"(MIT|Harvard|Stanford|Google|OpenAI|微软|谷歌|阿里|腾讯).{0,20}(研究|报告|发布)", Description = "机构研究" },
            
            // 事实型
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "zh", Pattern = @"成立于.{2,20}", Description = "成立于" },
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "zh", Pattern = @"位于.{2,30}", Description = "位于" },
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "zh", Pattern = @"获得了.{2,30}(融资|投资|奖项|认证)", Description = "获得融资" },
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "zh", Pattern = @"拥有.{2,20}(用户|客户|员工|专利)", Description = "拥有用户" },
            
            // 人名
            new ExtractionPatternEntity { Category = "entity_person", Scope = "language", ScopeValue = "zh", Pattern = @"(?:CEO|创始人|总裁|董事长|负责人)\s*([^\s,，。]{2,4})", Description = "职位+人名" },
            
            // 日期
            new ExtractionPatternEntity { Category = "entity_date", Scope = "language", ScopeValue = "zh", Pattern = @"\d{4}\s*年\s*\d{1,2}\s*月", Description = "年月" },
            new ExtractionPatternEntity { Category = "entity_date", Scope = "language", ScopeValue = "zh", Pattern = @"\d{1,2}\s*月\s*\d{1,2}\s*日", Description = "月日" },
        });

        // ========== 英文特定模式 ==========
        
        patterns.AddRange(new[]
        {
            // 统计型
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "en", Pattern = @"according to.{5,50}", Description = "according to" },
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "en", Pattern = @"(research|study|survey|data) (shows?|indicates?|reveals?).{5,50}", Description = "research shows" },
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "en", Pattern = @"more than.{5,30}(users?|customers?|companies)", Description = "more than users" },
            
            // 引用型
            new ExtractionPatternEntity { Category = "claim_citation", Scope = "language", ScopeValue = "en", Pattern = @"(expert|analyst|CEO|founder|researcher)s?\s+(say|said|believe|note)", Description = "expert says" },
            new ExtractionPatternEntity { Category = "claim_citation", Scope = "language", ScopeValue = "en", Pattern = @"(MIT|Harvard|Stanford|Google|OpenAI|Microsoft).{0,20}(research|report|study)", Description = "institution research" },
            
            // 事实型
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "en", Pattern = @"founded in.{2,20}", Description = "founded in" },
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "en", Pattern = @"located in.{2,30}", Description = "located in" },
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "en", Pattern = @"raised.{2,30}(funding|investment)", Description = "raised funding" },
            new ExtractionPatternEntity { Category = "claim_fact", Scope = "language", ScopeValue = "en", Pattern = @"has.{2,20}(users|customers|employees|patents)", Description = "has users" },
            
            // 人名
            new ExtractionPatternEntity { Category = "entity_person", Scope = "language", ScopeValue = "en", Pattern = @"(?:CEO|founder|president|director)\s+([A-Z][a-z]+\s+[A-Z][a-z]+)", Description = "title + name" },
            
            // 日期
            new ExtractionPatternEntity { Category = "entity_date", Scope = "language", ScopeValue = "en", Pattern = @"(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{1,2},?\s+\d{4}", Description = "month day year" },
            new ExtractionPatternEntity { Category = "entity_date", Scope = "language", ScopeValue = "en", Pattern = @"(?:Q[1-4]|H[12])\s+\d{4}", Description = "quarter year" },
        });

        // ========== 日文特定模式 ==========
        
        patterns.AddRange(new[]
        {
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "ja", Pattern = @"によると.{5,50}", Description = "によると (according to)" },
            new ExtractionPatternEntity { Category = "claim_statistic", Scope = "language", ScopeValue = "ja", Pattern = @"(調査|研究|報告)(によれば|では).{5,50}", Description = "調査によれば" },
            new ExtractionPatternEntity { Category = "entity_date", Scope = "language", ScopeValue = "ja", Pattern = @"\d{4}\s*年\s*\d{1,2}\s*月\s*\d{1,2}\s*日", Description = "年月日" },
        });

        int sortOrder = 0;
        foreach (var p in patterns)
        {
            p.SortOrder = sortOrder++;
            p.CreatedAt = DateTime.UtcNow;
            p.UpdatedAt = DateTime.UtcNow;
        }

        await _context.Client.Insertable(patterns).ExecuteCommandAsync();
    }

    private async Task InitializeEntitiesAsync()
    {
        var entities = new List<KnownEntityEntity>();

        // ========== 全局品牌 ==========
        var globalBrands = new[]
        {
            "Google", "Microsoft", "Apple", "Amazon", "Meta", "OpenAI", "Anthropic",
            "Tesla", "Netflix", "Uber", "Airbnb", "Spotify", "Adobe", "Salesforce", "Oracle",
            "IBM", "Intel", "AMD", "Nvidia", "Samsung", "Sony", "Nintendo",
            "ChatGPT", "GPT-4", "Claude", "Gemini", "Perplexity", "Copilot", "Midjourney",
            "GeoCore", "GeoCoreAI"
        };
        
        foreach (var brand in globalBrands)
        {
            entities.Add(new KnownEntityEntity { EntityType = "brand", Scope = "global", EntityName = brand });
        }

        // ========== 中文品牌 ==========
        var zhBrands = new[]
        {
            ("阿里巴巴", "[\"阿里\", \"Alibaba\"]"),
            ("腾讯", "[\"Tencent\"]"),
            ("百度", "[\"Baidu\"]"),
            ("字节跳动", "[\"ByteDance\", \"抖音\", \"TikTok\"]"),
            ("华为", "[\"Huawei\"]"),
            ("小米", "[\"Xiaomi\"]"),
            ("京东", "[\"JD\"]"),
            ("美团", "[\"Meituan\"]"),
            ("拼多多", "[\"Pinduoduo\"]"),
            ("滴滴", "[\"Didi\"]"),
        };
        
        foreach (var (name, aliases) in zhBrands)
        {
            entities.Add(new KnownEntityEntity { EntityType = "brand", Scope = "language", ScopeValue = "zh", EntityName = name, Aliases = aliases });
        }

        // ========== 全局人名 ==========
        var globalPersons = new[]
        {
            "Elon Musk", "Sam Altman", "Satya Nadella", "Sundar Pichai", "Tim Cook",
            "Mark Zuckerberg", "Jeff Bezos", "Bill Gates", "Jensen Huang"
        };
        
        foreach (var person in globalPersons)
        {
            entities.Add(new KnownEntityEntity { EntityType = "person", Scope = "global", EntityName = person });
        }

        // ========== 中文人名 ==========
        var zhPersons = new[] { "马云", "马化腾", "李彦宏", "雷军", "任正非", "刘强东", "张一鸣" };
        foreach (var person in zhPersons)
        {
            entities.Add(new KnownEntityEntity { EntityType = "person", Scope = "language", ScopeValue = "zh", EntityName = person });
        }

        // ========== 中文地点 ==========
        var zhLocations = new[]
        {
            "北京", "上海", "深圳", "广州", "杭州", "成都", "武汉", "西安", "南京", "苏州",
            "中国", "美国", "日本", "韩国", "英国", "德国", "法国", "新加坡", "香港", "台湾",
            "硅谷", "纽约", "旧金山", "洛杉矶", "西雅图", "波士顿", "伦敦", "东京", "首尔"
        };
        foreach (var loc in zhLocations)
        {
            entities.Add(new KnownEntityEntity { EntityType = "location", Scope = "language", ScopeValue = "zh", EntityName = loc });
        }

        // ========== 英文地点 ==========
        var enLocations = new[]
        {
            "Beijing", "Shanghai", "Shenzhen", "Guangzhou", "Hangzhou",
            "China", "USA", "Japan", "Korea", "UK", "Germany", "France", "Singapore",
            "Silicon Valley", "New York", "San Francisco", "Los Angeles", "Seattle",
            "Boston", "London", "Tokyo", "Seoul", "California", "Texas"
        };
        foreach (var loc in enLocations)
        {
            entities.Add(new KnownEntityEntity { EntityType = "location", Scope = "language", ScopeValue = "en", EntityName = loc });
        }

        foreach (var e in entities)
        {
            e.CreatedAt = DateTime.UtcNow;
            e.UpdatedAt = DateTime.UtcNow;
        }

        await _context.Client.Insertable(entities).ExecuteCommandAsync();
    }

    private async Task InitializeSentimentKeywordsAsync()
    {
        var keywords = new List<SentimentKeywordEntity>();

        // ========== 全局正面关键词（英文）==========
        var globalPositive = new[]
        {
            "best", "leading", "top", "excellent", "outstanding", "recommended", "trusted",
            "reliable", "innovative", "powerful", "efficient", "popular", "preferred",
            "award-winning", "industry-leading", "highly rated", "well-known", "renowned",
            "superior", "premium", "exceptional", "remarkable", "impressive", "advanced"
        };
        foreach (var kw in globalPositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "global", Keyword = kw });
        }

        // ========== 全局负面关键词（英文）==========
        var globalNegative = new[]
        {
            "expensive", "costly", "difficult", "complex", "limited", "outdated", "slow",
            "unreliable", "poor", "lacking", "issues", "problems", "complaints", "concerns",
            "buggy", "frustrating", "disappointing", "inferior", "overpriced", "complicated"
        };
        foreach (var kw in globalNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "global", Keyword = kw });
        }

        // ========== 中文正面关键词 ==========
        var zhPositive = new[]
        {
            "最佳", "领先", "顶级", "优秀", "卓越", "推荐", "可信赖", "可靠", "创新",
            "强大", "高效", "热门", "首选", "获奖", "行业领先", "好评", "知名", "著名",
            "优质", "出色", "专业", "权威", "一流", "领军", "标杆"
        };
        foreach (var kw in zhPositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "language", ScopeValue = "zh", Keyword = kw });
        }

        // ========== 中文负面关键词 ==========
        var zhNegative = new[]
        {
            "昂贵", "贵", "困难", "复杂", "有限", "过时", "慢", "不可靠", "差",
            "缺乏", "问题", "投诉", "担忧", "不足", "难用", "卡顿", "bug多", "坑"
        };
        foreach (var kw in zhNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "language", ScopeValue = "zh", Keyword = kw });
        }

        // ========== 日文正面关键词 ==========
        var jaPositive = new[]
        {
            "最高", "優秀", "信頼", "革新的", "強力", "効率的", "人気", "推奨",
            "受賞", "業界トップ", "高評価", "有名", "一流", "優れた"
        };
        foreach (var kw in jaPositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "language", ScopeValue = "ja", Keyword = kw });
        }

        // ========== 日文负面关键词 ==========
        var jaNegative = new[]
        {
            "高価", "難しい", "複雑", "限定的", "古い", "遅い", "信頼できない",
            "悪い", "不足", "問題", "苦情", "心配"
        };
        foreach (var kw in jaNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "language", ScopeValue = "ja", Keyword = kw });
        }

        // ========== 韩文正面关键词 ==========
        var koPositive = new[]
        {
            "최고", "우수", "신뢰", "혁신적", "강력", "효율적", "인기", "추천",
            "수상", "업계 선두", "높은 평가", "유명", "일류"
        };
        foreach (var kw in koPositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "language", ScopeValue = "ko", Keyword = kw });
        }

        // ========== 韩文负面关键词 ==========
        var koNegative = new[]
        {
            "비싼", "어려운", "복잡한", "제한적", "오래된", "느린", "신뢰할 수 없는",
            "나쁜", "부족", "문제", "불만", "걱정"
        };
        foreach (var kw in koNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "language", ScopeValue = "ko", Keyword = kw });
        }

        // ========== 德文正面关键词 ==========
        var dePositive = new[]
        {
            "beste", "führend", "ausgezeichnet", "zuverlässig", "innovativ", "leistungsstark",
            "effizient", "beliebt", "empfohlen", "preisgekrönt", "bekannt"
        };
        foreach (var kw in dePositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "language", ScopeValue = "de", Keyword = kw });
        }

        // ========== 德文负面关键词 ==========
        var deNegative = new[]
        {
            "teuer", "schwierig", "komplex", "begrenzt", "veraltet", "langsam",
            "unzuverlässig", "schlecht", "mangelhaft", "Probleme", "Beschwerden"
        };
        foreach (var kw in deNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "language", ScopeValue = "de", Keyword = kw });
        }

        // ========== 法文正面关键词 ==========
        var frPositive = new[]
        {
            "meilleur", "leader", "excellent", "fiable", "innovant", "puissant",
            "efficace", "populaire", "recommandé", "primé", "renommé"
        };
        foreach (var kw in frPositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "language", ScopeValue = "fr", Keyword = kw });
        }

        // ========== 法文负面关键词 ==========
        var frNegative = new[]
        {
            "cher", "difficile", "complexe", "limité", "obsolète", "lent",
            "peu fiable", "mauvais", "manquant", "problèmes", "plaintes"
        };
        foreach (var kw in frNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "language", ScopeValue = "fr", Keyword = kw });
        }

        // ========== 西班牙文正面关键词 ==========
        var esPositive = new[]
        {
            "mejor", "líder", "excelente", "confiable", "innovador", "potente",
            "eficiente", "popular", "recomendado", "premiado", "reconocido"
        };
        foreach (var kw in esPositive)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "positive", Scope = "language", ScopeValue = "es", Keyword = kw });
        }

        // ========== 西班牙文负面关键词 ==========
        var esNegative = new[]
        {
            "caro", "difícil", "complejo", "limitado", "obsoleto", "lento",
            "poco confiable", "malo", "carente", "problemas", "quejas"
        };
        foreach (var kw in esNegative)
        {
            keywords.Add(new SentimentKeywordEntity { SentimentType = "negative", Scope = "language", ScopeValue = "es", Keyword = kw });
        }

        foreach (var k in keywords)
        {
            k.CreatedAt = DateTime.UtcNow;
            k.UpdatedAt = DateTime.UtcNow;
        }

        await _context.Client.Insertable(keywords).ExecuteCommandAsync();
    }

    private async Task InitializeKeywordExclusionsAsync()
    {
        var exclusions = new List<KeywordExclusionEntity>();

        // ========== 全局英文常见词 ==========
        var globalCommon = new[]
        {
            "The", "And", "For", "With", "This", "That", "From", "Have", "Has", "Had",
            "Are", "Was", "Were", "Been", "Being", "Will", "Would", "Could", "Should",
            "May", "Might", "Must", "Can", "Cannot", "Not", "But", "Or", "If", "Then",
            "When", "Where", "What", "Which", "Who", "How", "Why", "All", "Any", "Some",
            "Most", "More", "Less", "Very", "Just", "Only", "Also", "Even", "Still",
            "Here", "There", "Now", "Today", "Tomorrow", "Yesterday",
            "New", "Old", "Good", "Bad", "Best", "Worst", "First", "Last", "Next",
            "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Yes", "No", "True", "False", "However", "Therefore", "Moreover", "Furthermore",
            "Although", "Because", "Since", "While", "Before", "After", "During", "Until",
            "About", "Above", "Below", "Between", "Into", "Through", "Over", "Under"
        };
        foreach (var w in globalCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "global", Category = "common" });
        }

        // ========== 全局技术词 ==========
        var globalTech = new[]
        {
            "AI", "API", "URL", "HTTP", "HTTPS", "HTML", "CSS", "JSON", "XML", "SQL",
            "SDK", "CLI", "GUI", "IDE", "OS", "CPU", "GPU", "RAM", "SSD", "HDD",
            "iOS", "Android", "Windows", "Linux", "macOS", "Unix"
        };
        foreach (var w in globalTech)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "global", Category = "tech" });
        }

        // ========== 全局时间词 ==========
        var globalTime = new[]
        {
            "January", "February", "March", "April", "May", "June", "July", "August",
            "September", "October", "November", "December",
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
        };
        foreach (var w in globalTime)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "global", Category = "time" });
        }

        // ========== 中文常见词 ==========
        var zhCommon = new[]
        {
            "\u7684", "\u662f", "\u5728", "\u6709", "\u548c", "\u4e0e", "\u6216", "\u4f46",  // 的是在有和与或但
            "\u5982\u679c", "\u90a3\u4e48", "\u56e0\u4e3a", "\u6240\u4ee5",  // 如果那么因为所以
            "\u8fd9\u4e2a", "\u90a3\u4e2a", "\u4ec0\u4e48", "\u600e\u4e48",  // 这个那个什么怎么
            "\u4e3a\u4ec0\u4e48", "\u54ea\u91cc", "\u8c01", "\u5982\u4f55",  // 为什么哪里谁如何
            "\u53ef\u4ee5", "\u80fd\u591f", "\u5df2\u7ecf", "\u6b63\u5728",  // 可以能够已经正在
            "\u5c06\u8981", "\u5e94\u8be5", "\u5fc5\u987b", "\u53ef\u80fd",  // 将要应该必须可能
            "\u4e5f\u8bb8", "\u4e00\u4e2a", "\u8fd9\u4e9b", "\u90a3\u4e9b"   // 也许一个这些那些
        };
        foreach (var w in zhCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "zh", Category = "stopword" });
        }

        // ========== 日文常见词 ==========
        var jaCommon = new[]
        {
            "\u306e", "\u306f", "\u304c", "\u3092", "\u306b", "\u3067", "\u3068", "\u3082",  // の は が を に で と も
            "\u3053\u308c", "\u305d\u308c", "\u3042\u308c", "\u3069\u308c",  // これ それ あれ どれ
            "\u3053\u306e", "\u305d\u306e", "\u3042\u306e", "\u3069\u306e",  // この その あの どの
            "\u3067\u3059", "\u307e\u3059", "\u3042\u308b", "\u3044\u308b"   // です ます ある いる
        };
        foreach (var w in jaCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "ja", Category = "stopword" });
        }

        // ========== 韩文常见词 ==========
        var koCommon = new[]
        {
            "\uc758", "\ub97c", "\uc774", "\uac00", "\uc5d0", "\uc5d0\uc11c", "\ub85c",  // 의 를 이 가 에 에서 로
            "\uc640", "\uacfc", "\ub3c4", "\ub9cc", "\uc740", "\ub294",  // 와 과 도 만 은 는
            "\uc774\uac83", "\uadf8\uac83", "\uc800\uac83", "\ubb34\uc5c7"   // 이것 그것 저것 무엇
        };
        foreach (var w in koCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "ko", Category = "stopword" });
        }

        // ========== 德文常见词 ==========
        var deCommon = new[]
        {
            "der", "die", "das", "und", "ist", "in", "zu", "den", "mit", "von",
            "auf", "nicht", "sich", "des", "ein", "eine", "als", "auch", "es", "an"
        };
        foreach (var w in deCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "de", Category = "stopword" });
        }

        // ========== 法文常见词 ==========
        var frCommon = new[]
        {
            "le", "la", "les", "de", "et", "est", "un", "une", "du", "en",
            "que", "qui", "dans", "ce", "il", "pour", "pas", "sur", "avec", "plus"
        };
        foreach (var w in frCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "fr", Category = "stopword" });
        }

        // ========== 西班牙文常见词 ==========
        var esCommon = new[]
        {
            "el", "la", "los", "las", "de", "y", "en", "que", "es", "un",
            "una", "del", "al", "con", "para", "por", "su", "se", "no", "como"
        };
        foreach (var w in esCommon)
        {
            exclusions.Add(new KeywordExclusionEntity { Word = w, LanguageCode = "es", Category = "stopword" });
        }

        await _context.Client.Insertable(exclusions).ExecuteCommandAsync();
    }
}
