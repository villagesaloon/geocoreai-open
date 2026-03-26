using GeoCore.Data.Entities;
using GeoCore.Data.DbContext;

namespace GeoCore.Data.Repositories;

/// <summary>
/// Prompt 配置初始化器 - 初始化默认 Prompt 模板
/// </summary>
public class PromptConfigInitializer
{
    private readonly GeoDbContext _dbContext;

    public PromptConfigInitializer(GeoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 初始化默认 Prompt 配置（如果不存在）
    /// </summary>
    public async Task InitializeDefaultPromptsAsync()
    {
        var db = _dbContext.Client;
        
        // 检查是否已有配置
        var existingCount = await db.Queryable<PromptConfigEntity>().CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine("[Prompt] 已存在配置，跳过初始化");
            return;
        }

        Console.WriteLine("[Prompt] 开始初始化默认 Prompt 配置...");

        var defaultPrompts = GetDefaultPrompts();
        await db.Insertable(defaultPrompts).ExecuteCommandAsync();
        
        Console.WriteLine($"[Prompt] 已初始化 {defaultPrompts.Count} 个默认 Prompt 配置");

        // 初始化系统配置
        await InitializeSystemConfigsAsync();
    }

    /// <summary>
    /// 启动时将文件版 Prompt 同步到数据库（以文件为准覆盖数据库）
    /// 自动扫描 promptsDir 下所有 .md 文件，按 {category}-{configKey}.md 命名约定解析
    /// 例如: questions-general.md → category=questions, configKey=general
    ///       distillation-selling-points.md → category=distillation, configKey=selling-points
    /// </summary>
    public async Task SyncFilePromptsToDbAsync(string promptsDir)
    {
        if (!Directory.Exists(promptsDir))
        {
            Console.WriteLine($"[PromptSync] Prompt 目录不存在: {promptsDir}，跳过同步");
            return;
        }

        var db = _dbContext.Client;
        var mdFiles = Directory.GetFiles(promptsDir, "*.md");

        if (mdFiles.Length == 0)
        {
            Console.WriteLine("[PromptSync] 目录下无 .md 文件，跳过同步");
            return;
        }

        var syncCount = 0;
        var createCount = 0;
        var skipCount = 0;

        foreach (var filePath in mdFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // 按第一个 '-' 拆分为 category 和 configKey
            // 例如: "questions-general" → ("questions", "general")
            //       "distillation-selling-points" → ("distillation", "selling-points")
            var dashIndex = fileName.IndexOf('-');
            if (dashIndex <= 0 || dashIndex >= fileName.Length - 1)
            {
                Console.WriteLine($"[PromptSync] 跳过无法解析的文件: {Path.GetFileName(filePath)}（需 {{category}}-{{configKey}}.md 格式）");
                continue;
            }

            var category = fileName[..dashIndex];
            var configKey = fileName[(dashIndex + 1)..];

            var fileContent = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(fileContent)) continue;

            // 查找数据库中对应记录
            var dbRecord = await db.Queryable<PromptConfigEntity>()
                .Where(x => x.Category == category && x.ConfigKey == configKey)
                .FirstAsync();

            if (dbRecord != null)
            {
                // 比较内容是否一致（trim 后比较，忽略首尾空白差异）
                if (dbRecord.PromptTemplate?.Trim() != fileContent.Trim())
                {
                    dbRecord.PromptTemplate = fileContent;
                    dbRecord.UpdatedAt = DateTime.UtcNow;
                    dbRecord.UpdatedBy = "file-sync";
                    await db.Updateable(dbRecord)
                        .UpdateColumns(x => new { x.PromptTemplate, x.UpdatedAt, x.UpdatedBy })
                        .ExecuteCommandAsync();
                    syncCount++;
                    Console.WriteLine($"[PromptSync] 已更新 {category}/{configKey} (id={dbRecord.Id})");
                }
                else
                {
                    skipCount++;
                }
            }
            else
            {
                // 数据库中不存在，新建
                var newEntity = new PromptConfigEntity
                {
                    Category = category,
                    ConfigKey = configKey,
                    Name = $"{category}/{configKey}",
                    Description = $"从文件同步: {Path.GetFileName(filePath)}",
                    PromptTemplate = fileContent,
                    IsEnabled = true,
                    SortOrder = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = "file-sync"
                };
                await db.Insertable(newEntity).ExecuteCommandAsync();
                createCount++;
                Console.WriteLine($"[PromptSync] 已创建 {category}/{configKey}");
            }
        }

        Console.WriteLine($"[PromptSync] 扫描 {mdFiles.Length} 个文件: 更新 {syncCount}, 新建 {createCount}, 一致 {skipCount}");
    }

    private async Task InitializeSystemConfigsAsync()
    {
        var db = _dbContext.Client;
        
        var existingCount = await db.Queryable<SystemConfigEntity>().CountAsync();
        if (existingCount > 0) return;

        var configs = new List<SystemConfigEntity>
        {
            new() { Category = "questions", ConfigKey = "min_freq_score", ConfigValue = "70", Name = "最低频率分数阈值", ValueType = "int" },
            new() { Category = "questions", ConfigKey = "questions_per_batch", ConfigValue = "20", Name = "每次生成问题数量", ValueType = "int" },
            new() { Category = "questions", ConfigKey = "high_freq_min", ConfigValue = "85", Name = "高频问题分数范围", ValueType = "int" },
            new() { Category = "questions", ConfigKey = "mid_freq_min", ConfigValue = "70", Name = "中频问题分数范围", ValueType = "int" },
            new() { Category = "ai", ConfigKey = "default_temperature", ConfigValue = "0.7", Name = "默认温度", ValueType = "string" },
            new() { Category = "ai", ConfigKey = "api_timeout", ConfigValue = "60", Name = "API超时时间", ValueType = "int" },
            new() { Category = "ai", ConfigKey = "max_retries", ConfigValue = "2", Name = "最大重试次数", ValueType = "int" },
        };

        await db.Insertable(configs).ExecuteCommandAsync();
        Console.WriteLine($"[Prompt] 已初始化 {configs.Count} 个系统配置");
    }

    private List<PromptConfigEntity> GetDefaultPrompts()
    {
        return new List<PromptConfigEntity>
        {
            // 卖点蒸馏
            new()
            {
                Category = "distillation",
                ConfigKey = "selling-points",
                Name = "卖点蒸馏 Prompt",
                Description = "从品牌/产品信息中提取核心卖点关键词（通用型，适用不同行业）",
                SortOrder = 1,
                PromptTemplate = @"你是一位跨行业的产品营销专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家，擅长从用户视角提炼产品卖点，帮助品牌在 AI 搜索中获得更高的可见度和在搜索引擎中的排名。

【核心任务】
根据产品信息，结合你对该行业用户搜索行为的了解，提炼出用户最关心的产品卖点关键词。
这些关键词将用于：
- 监控品牌/产品在 AI 搜索中的可见度
- 生成用户向 AI 提问的问题

【产品信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 产品描述：{{description}}
- 目标市场：{{markets}}
- 监控语言：{{languages}}
{{referenceInfo}}

【提炼原则】
1. **用户视角优先**：思考用户在搜索 {{productContext}} 时，最关心哪些特点？
   - 不同行业用户关注点不同（例如：手机用户关心性能/拍照，SaaS用户关心效率/成本）
   - 结合你对该行业的了解，提炼用户真正在意的卖点

2. **高频搜索词优先**：优先提取用户搜索频率最高的关键词
   - 这些词是用户在 AI 搜索中最常使用的表达方式
   - 避免专业术语，使用用户日常用语

3. **产品特点匹配**：从产品描述中找出与用户关注点匹配的特点
   - 产品有什么功能能满足用户需求？
   - 产品有什么优势能解决用户痛点？

4. **搜索关键词化**：将卖点转化为用户可能搜索的关键词
   - 4-8 个字/词，简洁有力
   - 是用户会在 AI 搜索中使用的词汇

5. {{outputLanguageHint}}

【提炼维度】（每个维度 3-4 个卖点，共 18-20 个）
1. **用户痛点**：该行业用户最常遇到的问题是什么？产品如何解决？
2. **核心功能**：产品最重要的功能是什么？用户会怎么描述它？
3. **差异化优势**：与同类产品相比，有什么独特之处？
4. **使用场景**：用户在什么场景下会需要这个产品？
5. **价值结果**：使用产品后能获得什么结果？

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""sellingPoints"": [
    { ""point"": ""卖点关键词"", ""weight"": 9, ""usage"": ""用户搜索场景"" }
  ]
}

【禁止】
- 禁止空泛词：核心功能、用户体验、品质保障、领先平台、性价比
- 禁止品牌方视角的营销术语，要用用户会搜索的词
- 禁止数量不足：必须生成 18-20 个卖点"
            },

            // 受众推断
            new()
            {
                Category = "distillation",
                ConfigKey = "audience",
                Name = "受众推断 Prompt",
                Description = "根据产品信息推断目标受众（通用型，适用不同行业）",
                SortOrder = 2,
                PromptTemplate = @"你是一位跨行业的用户研究专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家，擅长从用户搜索行为推断目标受众，帮助品牌精准定位在 AI 搜索和搜索引擎中的目标用户。

【核心任务】
根据产品信息和核心卖点，推断最可能搜索该产品的目标受众群体。

【产品信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 产品描述：{{description}}
- 核心卖点：{{sellingPoints}}

【推断原则】
1. **搜索行为导向**：思考什么样的用户会搜索 {{productContext}} 相关的问题？
2. **决策链路分析**：谁是信息搜索者？谁是最终决策者？
3. **诉求具体化**：用户搜索时的真实意图是什么？

【推断要求】
1. **用户类型**：从以下选择 1-2 个
   - b2b：企业客户
   - b2c：个人消费者
   - dev：开发者/技术人员

2. **决策角色**：从以下选择 1-3 个
   - tech：技术决策者（CTO、技术负责人）
   - biz：业务决策者（CMO、市场负责人）
   - procurement：采购/财务
   - enduser：终端使用者

3. **核心诉求**：基于产品卖点，推断 3-5 个用户的核心诉求
   - 必须与产品功能直接相关
   - 使用用户搜索时会用的表达方式（例如：""如何提升AI搜索排名""而非""提升排名""）
   - 禁止空泛词：产品价值、服务质量、性价比、用户体验

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""userTypes"": [""b2b""],
  ""roles"": [""biz"", ""tech""],
  ""coreNeeds"": [""如何监控品牌在AI中的曝光"", ""怎么提升AI搜索排名""]
}"
            },

            // 品牌检查
            new()
            {
                Category = "distillation",
                ConfigKey = "brand-check",
                Name = "品牌检查 Prompt",
                Description = "判断品牌是否在 AI 知识库中存在",
                SortOrder = 3,
                PromptTemplate = @"你是一位品牌研究专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家。请判断以下品牌/产品是否在你的知识库中存在。

【品牌信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 语言：{{languages}}

【判断标准】
1. **已知品牌**：你能够描述该品牌的主要产品、市场定位、用户群体、竞争优势等信息
2. **未知品牌**：你对该品牌没有足够的了解，无法提供详细信息

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""isKnown"": true,
  ""confidence"": 85,
  ""knownInfo"": ""该品牌的简要描述（如果已知）"",
  ""suggestedCompetitors"": [""竞品1"", ""竞品2"", ""竞品3""]
}"
            },

            // 竞品信息提取
            new()
            {
                Category = "distillation",
                ConfigKey = "competitor-info",
                Name = "竞品信息提取 Prompt",
                Description = "分析竞品的用户关注点和搜索关键词，支持已知/未知品牌场景",
                SortOrder = 4,
                PromptTemplate = @"你是一位行业研究专家，同时也是 GEO（Generative Engine Optimization，生成式引擎优化）领域的专家。

【核心任务】
为品牌 ""{{brandName}}"" 找出在 {{industry}} 行业中的直接竞品，并分析用户搜索这些竞品时的关注点。

【品牌信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 所属行业：{{industry}}
- 官网：{{brandUrl}}
- 产品描述：{{description}}
- 目标语言：{{languages}}
- 品牌知名度：{{brandKnownStatus}}

【重要说明】
{{brandKnownHint}}

【竞品发现要求】
1. **行业头部优先**：找出 {{industry}} 行业中市场份额最大、知名度最高的 5-8 个品牌
2. **直接竞品**：这些品牌必须提供与【产品描述】中相同或高度相似的服务
3. **真实性**：只返回真实存在、你确定了解的公司/品牌
4. **官网要求**：{{urlRequirement}}
5. **不确定则跳过**：如果你不确定某个竞品的信息，宁可不列出，也不要编造

【竞品分析维度】
对于每个竞品，分析：
1. **用户关注点**：用户搜索该竞品时最关心的 3-5 个特点（用户视角表达）
2. **核心优势**：该竞品的主要差异化优势
3. **用户痛点**：用户对该竞品的常见不满
4. **典型搜索词**：用户搜索该竞品时常用的关键词（4-8字）

【行业分析】
1. **行业共同关注点**：该行业用户普遍关心的核心问题
2. **行业共同痛点**：该行业用户普遍面临的痛点
3. **高频搜索词**：用户搜索该行业产品时最常用的关键词
4. **差异化机会**：新品牌可以突出的差异化方向

【输出格式】
必须输出纯 JSON，不要包含任何其他文字或 markdown：
{
  ""competitors"": [
    {
      ""name"": ""竞品名称"",
      ""url"": ""官网URL（如无则留空）"",
      ""description"": ""一句话简介（20字以内）"",
      ""marketPosition"": ""行业领导者/挑战者/细分领先"",
      ""userFocusPoints"": [""关注点1"", ""关注点2"", ""关注点3""],
      ""coreStrengths"": [""优势1"", ""优势2""],
      ""userPainPoints"": [""痛点1"", ""痛点2""],
      ""typicalSearchQueries"": [""搜索词1"", ""搜索词2""]
    }
  ],
  ""industryAnalysis"": {
    ""commonFocusPoints"": [""关注点1"", ""关注点2""],
    ""commonPainPoints"": [""痛点1"", ""痛点2""],
    ""highFrequencyKeywords"": [""关键词1"", ""关键词2"", ""关键词3""],
    ""differentiationOpportunities"": [""机会1"", ""机会2""]
  },
  ""analysisConfidence"": ""high/medium/low"",
  ""confidenceReason"": ""说明为什么给出这个置信度""
}"
            },

            // 通用问题生成
            new()
            {
                Category = "questions",
                ConfigKey = "general",
                Name = "通用模型问题生成 Prompt",
                Description = "GPT/Claude/Gemini/Grok 通用问题生成模板",
                SortOrder = 1,
                PromptTemplate = @"You are simulating REAL users searching for {{industry}} solutions on ChatGPT/Claude/Gemini.

## Context (for your reference only, DO NOT include brand name in questions)
{{productContext}}
Industry: {{industry}}
Features/Keywords: {{sellingPoints}}
Target Users: {{personas}}
{{regionGuidance}}

## CRITICAL RULES
1. **Questions must be GENERIC industry questions** - DO NOT include the brand name ""{{brandName}}"" in questions
2. **Only generate HIGH-FREQUENCY questions (freq >= 70)** - Skip low-frequency questions
3. **Answers should naturally mention {{brandName}}** as one of the recommended solutions
4. Generate questions in {{language}}, answers in {{language}}

## Question Frequency Guidelines
- **freq 85-100**: Must-have questions (what is, how to, best tools, top recommendations)
- **freq 70-84**: Important questions (comparison, pricing, reviews, alternatives)
- **DO NOT generate questions with freq < 70**

## Question Patterns
ranking, comparison, pitfalls, fit, alternatives, authenticity, price, reviews, tutorials, specs, after-sales, accessories, safety, timing

## Examples of CORRECT questions (generic, no brand):
- ""What are the best {{industry}} tools in 2026?""
- ""How to choose a {{industry}} solution?""
- ""What features should I look for in {{industry}}?""

## Examples of WRONG questions (contains brand name):
- ""{{brandName}} vs competitors"" X
- ""Is {{brandName}} good?"" X

## Task
Generate 20 generic HIGH-FREQUENCY (freq >= 70) industry questions. Sort by frequency. In answers, naturally recommend {{brandName}}.

## Output JSON
{""questions"":[{""question"":""..."",""freq"":90,""stage"":""..."",""intent"":""..."",""pattern"":""..."",""answer"":""..."",""sources"":[""...""]}]}"
            },

            // Perplexity 问题生成
            new()
            {
                Category = "questions",
                ConfigKey = "perplexity",
                Name = "Perplexity 问题生成 Prompt",
                Description = "Perplexity 专用模板（利用搜索能力获取真实频率）",
                SortOrder = 2,
                PromptTemplate = @"You are a search-powered question generator with access to real search data. Generate 20 realistic HIGH-FREQUENCY user questions about {{industry}}.

## Context (for your reference only, DO NOT include brand name in questions)
{{productContext}}
Industry: {{industry}}
Features/Keywords: {{sellingPoints}}
Target users: {{personas}}
{{regionGuidance}}

## CRITICAL RULES
1. **Questions must be GENERIC industry questions** - DO NOT include the brand name ""{{brandName}}"" in questions
2. **Use your search capability to find REAL high-frequency questions** - Only include questions that users actually search for frequently
3. **freq_score must reflect REAL search frequency** - Use your search data to estimate actual search volume
4. **Answers should naturally mention {{brandName}}** as one of the recommended solutions
5. Generate questions in {{language}}, answers in {{language}}

## Question Frequency Guidelines (based on real search data)
- **freq_score 85-100**: Very high search volume (thousands of searches per month)
- **freq_score 70-84**: High search volume (hundreds of searches per month)
- **DO NOT include questions with freq_score < 70** - These are not worth targeting

## Your Advantage as Perplexity
- You can search for real user questions on forums, Q&A sites, and search engines
- Use this to find questions people ACTUALLY ask, not hypothetical questions
- Verify frequency by checking how often similar questions appear in search results

IMPORTANT: You MUST output valid JSON. Do not refuse or explain - just generate the questions.

## Output JSON (ONLY valid JSON, no explanations)
{""questions"":[{""question"":""..."",""freq_score"":90,""stage"":""..."",""intent"":""..."",""pattern"":""..."",""answer"":""..."",""sources"":[""...""]}]}"
            }
        };
    }
}
