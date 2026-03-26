using System.Text.RegularExpressions;
using GeoCore.SaaS.Services.ContentQuality.Models;

namespace GeoCore.SaaS.Services.ContentQuality;

/// <summary>
/// GEO 高级分析器 (7.27-7.31)
/// Phase 7 高优先级功能实现
/// </summary>
public class GEOAdvancedAnalyzer
{
    private readonly ILogger<GEOAdvancedAnalyzer> _logger;

    public GEOAdvancedAnalyzer(ILogger<GEOAdvancedAnalyzer> logger)
    {
        _logger = logger;
    }

    #region 7.27 Listicle 架构审计

    /// <summary>
    /// Listicle 架构审计 (7.27)
    /// 原理：GenOptima 449 引用实测显示 74.2% AI 引用来自 "Top N" 结构
    /// 服务页/案例页 = 0 引用
    /// </summary>
    public ListicleArchitectureAudit AuditListicleArchitecture(string content, string? title = null, string language = "zh")
    {
        var audit = new ListicleArchitectureAudit
        {
            HasTopNTitle = false,
            HasNumberedList = false,
            HasComparisonTable = false,
            IsServicePage = false,
            IsCasePage = false,
            ListicleScore = 0,
            Issues = new List<string>(),
            Suggestions = new List<string>()
        };

        var lowerContent = content.ToLower();
        var lowerTitle = title?.ToLower() ?? "";

        // 检测 "Top N" 标题模式
        var topNPatterns = language == "zh"
            ? new[] { @"(top|最佳|最好|最受欢迎|最热门|最推荐)\s*\d+", @"\d+\s*(个|种|款|大|佳)", @"(排行榜|排名|推荐)" }
            : new[] { @"top\s*\d+", @"\d+\s*(best|top|most)", @"(ranking|ranked|list of)" };

        foreach (var pattern in topNPatterns)
        {
            if (Regex.IsMatch(lowerTitle, pattern, RegexOptions.IgnoreCase) ||
                Regex.IsMatch(content.Split('\n').FirstOrDefault() ?? "", pattern, RegexOptions.IgnoreCase))
            {
                audit.HasTopNTitle = true;
                audit.TopNValue = ExtractTopNValue(title ?? content, pattern);
                break;
            }
        }

        // 检测编号列表
        var numberedListPattern = language == "zh"
            ? @"(?:^|\n)\s*(\d+)[\.、]\s*.{10,}"
            : @"(?:^|\n)\s*(\d+)\.\s*.{10,}";
        var numberedMatches = Regex.Matches(content, numberedListPattern, RegexOptions.Multiline);
        audit.HasNumberedList = numberedMatches.Count >= 3;
        audit.NumberedItemCount = numberedMatches.Count;

        // 检测对比表格
        var tablePatterns = new[] { @"\|[^\|]+\|[^\|]+\|", @"<table", @"┌|┐|└|┘|│|─" };
        audit.HasComparisonTable = tablePatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase));

        // 检测是否为服务页（0 引用风险）
        var servicePagePatterns = language == "zh"
            ? new[] { @"我们的服务", @"联系我们", @"立即咨询", @"免费试用", @"获取报价", @"服务范围", @"我们提供" }
            : new[] { @"our services", @"contact us", @"get started", @"free trial", @"get a quote", @"we offer", @"our solutions" };
        var serviceSignals = servicePagePatterns.Count(p => Regex.IsMatch(lowerContent, p, RegexOptions.IgnoreCase));
        audit.IsServicePage = serviceSignals >= 2;

        // 检测是否为案例页（0 引用风险）
        var casePagePatterns = language == "zh"
            ? new[] { @"客户案例", @"成功案例", @"案例研究", @"客户故事", @"合作案例", @"项目案例" }
            : new[] { @"case study", @"customer story", @"success story", @"client case", @"portfolio" };
        var caseSignals = casePagePatterns.Count(p => Regex.IsMatch(lowerContent, p, RegexOptions.IgnoreCase));
        audit.IsCasePage = caseSignals >= 1;

        // 计算 Listicle 分数
        audit.ListicleScore = CalculateListicleArchitectureScore(audit);

        // 生成问题和建议
        GenerateListicleIssuesAndSuggestions(audit, language);

        _logger.LogDebug("[GEOAdvanced] Listicle 审计: TopN={TopN}, Score={Score}, Service={Service}, Case={Case}",
            audit.HasTopNTitle, audit.ListicleScore, audit.IsServicePage, audit.IsCasePage);

        return audit;
    }

    private int? ExtractTopNValue(string text, string pattern)
    {
        var match = Regex.Match(text, @"\d+", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Value, out var n) ? n : null;
    }

    private double CalculateListicleArchitectureScore(ListicleArchitectureAudit audit)
    {
        if (audit.IsServicePage || audit.IsCasePage)
            return 0; // 服务页和案例页 = 0 引用

        var score = 0.0;

        // Top N 标题 (+3)
        if (audit.HasTopNTitle) score += 3;

        // 编号列表 (+3)
        if (audit.HasNumberedList)
        {
            score += audit.NumberedItemCount switch
            {
                >= 10 => 3,
                >= 7 => 2.5,
                >= 5 => 2,
                >= 3 => 1.5,
                _ => 0
            };
        }

        // 对比表格 (+2)
        if (audit.HasComparisonTable) score += 2;

        // Top N 值匹配列表项数量 (+2)
        if (audit.HasTopNTitle && audit.TopNValue.HasValue &&
            Math.Abs(audit.NumberedItemCount - audit.TopNValue.Value) <= 1)
        {
            score += 2;
        }

        return Math.Min(10, score);
    }

    private void GenerateListicleIssuesAndSuggestions(ListicleArchitectureAudit audit, string language)
    {
        if (audit.IsServicePage)
        {
            audit.Issues.Add(language == "zh"
                ? "⚠️ 检测到服务页特征，此类页面 AI 引用率接近 0%"
                : "⚠️ Service page detected, AI citation rate is near 0%");
            audit.Suggestions.Add(language == "zh"
                ? "将服务页转换为 'Top N [服务类型] 解决方案' 格式的 Listicle"
                : "Convert service page to 'Top N [Service Type] Solutions' listicle format");
        }

        if (audit.IsCasePage)
        {
            audit.Issues.Add(language == "zh"
                ? "⚠️ 检测到案例页特征，此类页面 AI 引用率接近 0%"
                : "⚠️ Case study page detected, AI citation rate is near 0%");
            audit.Suggestions.Add(language == "zh"
                ? "将案例页转换为 'N 个 [行业] 最佳实践' 格式"
                : "Convert case study to 'N Best Practices in [Industry]' format");
        }

        if (!audit.HasTopNTitle)
        {
            audit.Issues.Add(language == "zh"
                ? "缺少 'Top N' 或 'N 个最佳' 格式标题"
                : "Missing 'Top N' or 'N Best' format title");
            audit.Suggestions.Add(language == "zh"
                ? "使用 'Top 10 [主题]' 或 '10 个最佳 [主题]' 格式标题"
                : "Use 'Top 10 [Topic]' or '10 Best [Topic]' title format");
        }

        if (!audit.HasNumberedList)
        {
            audit.Issues.Add(language == "zh"
                ? "缺少编号列表结构"
                : "Missing numbered list structure");
            audit.Suggestions.Add(language == "zh"
                ? "使用 1. 2. 3. 编号格式组织内容"
                : "Organize content using 1. 2. 3. numbered format");
        }

        if (!audit.HasComparisonTable && audit.NumberedItemCount >= 3)
        {
            audit.Suggestions.Add(language == "zh"
                ? "添加对比表格可提升 2.8x 引用率"
                : "Adding comparison table can boost citation rate by 2.8x");
        }
    }

    #endregion

    #region 7.28 Triple JSON-LD Stack

    /// <summary>
    /// 生成三层 JSON-LD Stack (7.28)
    /// 原理：Article + ItemList + FAQPage 三层 Schema = 1.8x 引用
    /// </summary>
    public TripleJsonLdResult GenerateTripleJsonLd(
        string title,
        string description,
        string url,
        string authorName,
        DateTime publishDate,
        List<string> listItems,
        List<(string question, string answer)> faqs,
        string? imageUrl = null)
    {
        var result = new TripleJsonLdResult
        {
            ArticleSchema = GenerateArticleSchema(title, description, url, authorName, publishDate, imageUrl),
            ItemListSchema = GenerateItemListSchema(title, url, listItems),
            FaqPageSchema = GenerateFaqPageSchema(url, faqs),
            CombinedScript = ""
        };

        // 生成组合脚本
        result.CombinedScript = $@"<script type=""application/ld+json"">
{result.ArticleSchema}
</script>
<script type=""application/ld+json"">
{result.ItemListSchema}
</script>
<script type=""application/ld+json"">
{result.FaqPageSchema}
</script>";

        result.ValidationMessages = ValidateTripleJsonLd(result);

        _logger.LogDebug("[GEOAdvanced] 生成 Triple JSON-LD: Article + ItemList({Items}) + FAQ({Faqs})",
            listItems.Count, faqs.Count);

        return result;
    }

    private string GenerateArticleSchema(string title, string description, string url, string authorName, DateTime publishDate, string? imageUrl)
    {
        var image = !string.IsNullOrEmpty(imageUrl) ? $@",
  ""image"": ""{imageUrl}""" : "";

        return $@"{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""Article"",
  ""headline"": ""{EscapeJson(title)}"",
  ""description"": ""{EscapeJson(description)}"",
  ""url"": ""{url}"",
  ""author"": {{
    ""@type"": ""Person"",
    ""name"": ""{EscapeJson(authorName)}""
  }},
  ""datePublished"": ""{publishDate:yyyy-MM-dd}"",
  ""dateModified"": ""{DateTime.UtcNow:yyyy-MM-dd}""{image}
}}";
    }

    private string GenerateItemListSchema(string title, string url, List<string> items)
    {
        var itemElements = items.Select((item, index) => $@"    {{
      ""@type"": ""ListItem"",
      ""position"": {index + 1},
      ""name"": ""{EscapeJson(item)}""
    }}");

        return $@"{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""ItemList"",
  ""name"": ""{EscapeJson(title)}"",
  ""url"": ""{url}"",
  ""numberOfItems"": {items.Count},
  ""itemListElement"": [
{string.Join(",\n", itemElements)}
  ]
}}";
    }

    private string GenerateFaqPageSchema(string url, List<(string question, string answer)> faqs)
    {
        var faqElements = faqs.Select(faq => $@"    {{
      ""@type"": ""Question"",
      ""name"": ""{EscapeJson(faq.question)}"",
      ""acceptedAnswer"": {{
        ""@type"": ""Answer"",
        ""text"": ""{EscapeJson(faq.answer)}""
      }}
    }}");

        return $@"{{
  ""@context"": ""https://schema.org"",
  ""@type"": ""FAQPage"",
  ""mainEntity"": [
{string.Join(",\n", faqElements)}
  ]
}}";
    }

    private List<string> ValidateTripleJsonLd(TripleJsonLdResult result)
    {
        var messages = new List<string>();

        if (string.IsNullOrEmpty(result.ArticleSchema))
            messages.Add("Article Schema 生成失败");

        if (result.ItemListSchema.Contains("\"numberOfItems\": 0"))
            messages.Add("ItemList 为空，建议添加至少 3 个列表项");

        if (result.FaqPageSchema.Contains("\"mainEntity\": []"))
            messages.Add("FAQPage 为空，建议添加至少 3 个 FAQ");

        if (messages.Count == 0)
            messages.Add("✅ Triple JSON-LD 验证通过");

        return messages;
    }

    private string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    #endregion

    #region 7.29 ChatGPT 无源响应过滤

    /// <summary>
    /// ChatGPT 无源响应过滤 (7.29)
    /// 原理：53.6% ChatGPT 响应无 web 源，从指标中排除避免数据失真
    /// </summary>
    public SourcelessResponseFilter FilterSourcelessResponses(List<AIResponse> responses)
    {
        var result = new SourcelessResponseFilter
        {
            TotalResponses = responses.Count,
            SourcelessResponses = new List<AIResponse>(),
            SourcedResponses = new List<AIResponse>(),
            FilteredMetrics = new FilteredMetrics()
        };

        foreach (var response in responses)
        {
            var hasSource = DetectWebSource(response);
            if (hasSource)
            {
                result.SourcedResponses.Add(response);
            }
            else
            {
                result.SourcelessResponses.Add(response);
            }
        }

        result.SourcelessRate = responses.Count > 0
            ? (double)result.SourcelessResponses.Count / responses.Count
            : 0;

        // 计算过滤后的指标
        if (result.SourcedResponses.Any())
        {
            result.FilteredMetrics.CitationRate = result.SourcedResponses.Count(r => r.IsBrandCited) /
                (double)result.SourcedResponses.Count;
            result.FilteredMetrics.AveragePosition = result.SourcedResponses
                .Where(r => r.IsBrandCited)
                .Select(r => r.CitationPosition)
                .DefaultIfEmpty(0)
                .Average();
        }

        // 生成警告
        if (result.SourcelessRate > 0.5)
        {
            result.Warnings.Add($"⚠️ {result.SourcelessRate:P0} 的 ChatGPT 响应无 web 源，已从指标中排除");
        }

        _logger.LogDebug("[GEOAdvanced] 无源响应过滤: {Total} 总响应, {Sourceless} 无源 ({Rate:P0})",
            result.TotalResponses, result.SourcelessResponses.Count, result.SourcelessRate);

        return result;
    }

    private bool DetectWebSource(AIResponse response)
    {
        if (response.Sources != null && response.Sources.Any())
            return true;

        // 检测响应文本中的 URL 或引用标记
        var sourcePatterns = new[]
        {
            @"https?://[^\s]+",
            @"\[source\]",
            @"\[\d+\]",
            @"according to",
            @"based on",
            @"来源[：:]",
            @"参考[：:]",
            @"引用自"
        };

        return sourcePatterns.Any(p => Regex.IsMatch(response.ResponseText, p, RegexOptions.IgnoreCase));
    }

    #endregion

    #region 7.30 AI Overview 7 因素评分

    /// <summary>
    /// AI Overview 7 因素评分 (7.30)
    /// 原理：Wellows/AI Mode Boost 研究的 7 个关键因素
    /// </summary>
    public AIOverviewScoreResult CalculateAIOverviewScore(string content, string? title = null, string language = "zh")
    {
        var result = new AIOverviewScoreResult
        {
            Factors = new List<AIOverviewFactor>(),
            TotalScore = 0,
            MaxScore = 70,
            Suggestions = new List<string>()
        };

        // 1. 语义完整性 (r=0.87) - 最高权重
        var semanticScore = AnalyzeSemanticCompleteness(content, language);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "语义完整性",
            NameEn = "Semantic Completeness",
            Score = semanticScore,
            MaxScore = 10,
            Correlation = 0.87,
            Description = "内容是否完整回答问题，无需额外上下文"
        });

        // 2. 多模态支持 (r=0.92) - 最高相关性
        var multimodalScore = AnalyzeMultimodalSupport(content);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "多模态支持",
            NameEn = "Multimodal Support",
            Score = multimodalScore,
            MaxScore = 10,
            Correlation = 0.92,
            Description = "是否包含图片、表格、代码等多种内容形式"
        });

        // 3. 事实验证性 (r=0.89)
        var factualScore = AnalyzeFactualVerifiability(content, language);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "事实验证性",
            NameEn = "Factual Verifiability",
            Score = factualScore,
            MaxScore = 10,
            Correlation = 0.89,
            Description = "是否包含可验证的数据、统计和引用"
        });

        // 4. 结构清晰度 (r=0.82)
        var structureScore = AnalyzeStructureClarity(content);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "结构清晰度",
            NameEn = "Structure Clarity",
            Score = structureScore,
            MaxScore = 10,
            Correlation = 0.82,
            Description = "标题层级、段落组织、列表使用"
        });

        // 5. 答案直接性 (r=0.78)
        var directnessScore = AnalyzeAnswerDirectness(content, language);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "答案直接性",
            NameEn = "Answer Directness",
            Score = directnessScore,
            MaxScore = 10,
            Correlation = 0.78,
            Description = "前 40-60 词是否直接回答问题"
        });

        // 6. 权威信号 (r=0.75)
        var authorityScore = AnalyzeAuthoritySignals(content, language);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "权威信号",
            NameEn = "Authority Signals",
            Score = authorityScore,
            MaxScore = 10,
            Correlation = 0.75,
            Description = "作者信息、专家引用、机构背书"
        });

        // 7. 新鲜度信号 (r=0.71)
        var freshnessScore = AnalyzeFreshnessSignals(content, language);
        result.Factors.Add(new AIOverviewFactor
        {
            Name = "新鲜度信号",
            NameEn = "Freshness Signals",
            Score = freshnessScore,
            MaxScore = 10,
            Correlation = 0.71,
            Description = "日期标记、年份引用、更新声明"
        });

        // 计算加权总分
        result.TotalScore = result.Factors.Sum(f => f.Score * f.Correlation) /
            result.Factors.Sum(f => f.Correlation) * 10;
        result.TotalScore = Math.Round(result.TotalScore, 1);

        // 生成建议
        GenerateAIOverviewSuggestions(result, language);

        _logger.LogDebug("[GEOAdvanced] AI Overview 评分: {Score}/10", result.TotalScore);

        return result;
    }

    private double AnalyzeSemanticCompleteness(string content, string language)
    {
        var score = 5.0;

        // 检测完整句子
        var sentences = Regex.Split(content, @"[。！？.!?]").Where(s => s.Trim().Length > 10).ToList();
        if (sentences.Count >= 5) score += 2;

        // 检测定义性语句
        var definitionPatterns = language == "zh"
            ? new[] { @"是指", @"是一种", @"定义为", @"指的是" }
            : new[] { @"is defined as", @"refers to", @"is a type of", @"means" };
        if (definitionPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
            score += 1.5;

        // 检测结论性语句
        var conclusionPatterns = language == "zh"
            ? new[] { @"因此", @"总之", @"综上", @"结论是" }
            : new[] { @"therefore", @"in conclusion", @"to summarize", @"as a result" };
        if (conclusionPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
            score += 1.5;

        return Math.Min(10, score);
    }

    private double AnalyzeMultimodalSupport(string content)
    {
        var score = 3.0; // 基础分

        // 图片
        if (Regex.IsMatch(content, @"!\[|<img|\.png|\.jpg|\.gif|\.webp", RegexOptions.IgnoreCase))
            score += 2.5;

        // 表格
        if (Regex.IsMatch(content, @"\|[^\|]+\|[^\|]+\||<table", RegexOptions.IgnoreCase))
            score += 2.5;

        // 代码块
        if (Regex.IsMatch(content, @"```|<code|<pre", RegexOptions.IgnoreCase))
            score += 1;

        // 视频嵌入
        if (Regex.IsMatch(content, @"youtube\.com|vimeo\.com|<video|<iframe", RegexOptions.IgnoreCase))
            score += 1;

        return Math.Min(10, score);
    }

    private double AnalyzeFactualVerifiability(string content, string language)
    {
        var score = 3.0;

        // 数字和统计
        var numberMatches = Regex.Matches(content, @"\d+%|\d+\.\d+|\d{3,}");
        score += Math.Min(3, numberMatches.Count * 0.3);

        // 引用标记
        var citationPatterns = new[] { @"\[\d+\]", @"\(.*\d{4}\)", @"according to", @"研究表明", @"数据显示" };
        var citationCount = citationPatterns.Sum(p => Regex.Matches(content, p, RegexOptions.IgnoreCase).Count);
        score += Math.Min(2, citationCount * 0.5);

        // 具体来源
        var sourcePatterns = new[] { @"Gartner|McKinsey|Forrester|IDC", @"大学|研究院|University", @"官方数据" };
        if (sourcePatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
            score += 2;

        return Math.Min(10, score);
    }

    private double AnalyzeStructureClarity(string content)
    {
        var score = 3.0;

        // H2 标题
        var h2Count = Regex.Matches(content, @"^##\s+[^#]|<h2", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        score += Math.Min(2, h2Count * 0.5);

        // H3 标题
        var h3Count = Regex.Matches(content, @"^###\s+[^#]|<h3", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        score += Math.Min(1.5, h3Count * 0.3);

        // 列表
        var listCount = Regex.Matches(content, @"^\s*[-*•]\s+|^\s*\d+\.\s+", RegexOptions.Multiline).Count;
        score += Math.Min(2, listCount * 0.2);

        // 段落分隔
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (paragraphs.Length >= 5) score += 1.5;

        return Math.Min(10, score);
    }

    private double AnalyzeAnswerDirectness(string content, string language)
    {
        var score = 3.0;

        // 获取前 100 词
        var words = content.Split(new[] { ' ', '\n', '\t', '，', '。' }, StringSplitOptions.RemoveEmptyEntries);
        var first100 = string.Join(" ", words.Take(100));

        // 检测直接答案模式
        var directPatterns = language == "zh"
            ? new[] { @"^[^，。]{5,50}[是指为]", @"^简单来说", @"^答案是", @"^首先" }
            : new[] { @"^[^,.]{5,50}\s+(is|are|means)", @"^simply put", @"^the answer is", @"^first" };

        if (directPatterns.Any(p => Regex.IsMatch(first100, p, RegexOptions.IgnoreCase | RegexOptions.Multiline)))
            score += 3;

        // 检测问题-答案格式
        if (Regex.IsMatch(first100, @"[?？].*[。.!！]", RegexOptions.Singleline))
            score += 2;

        // 前 60 词内有完整句子
        var first60 = string.Join(" ", words.Take(60));
        if (Regex.IsMatch(first60, @"[。.!！?？]"))
            score += 2;

        return Math.Min(10, score);
    }

    private double AnalyzeAuthoritySignals(string content, string language)
    {
        var score = 3.0;

        // 作者信息
        var authorPatterns = language == "zh"
            ? new[] { @"作者[：:]", @"撰写[：:]", @"编辑[：:]", @"专家[：:]" }
            : new[] { @"author:", @"written by", @"by [A-Z][a-z]+ [A-Z][a-z]+", @"expert:" };
        if (authorPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
            score += 2;

        // 专家引用
        var expertPatterns = language == "zh"
            ? new[] { @"教授", @"博士", @"专家", @"研究员", @"分析师" }
            : new[] { @"professor", @"dr\.", @"phd", @"expert", @"analyst" };
        var expertCount = expertPatterns.Sum(p => Regex.Matches(content, p, RegexOptions.IgnoreCase).Count);
        score += Math.Min(2.5, expertCount * 0.5);

        // 机构背书
        var orgPatterns = new[] { @"Gartner|McKinsey|Forrester|Harvard|Stanford|MIT", @"政府|官方|Government|Official" };
        if (orgPatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
            score += 2.5;

        return Math.Min(10, score);
    }

    private double AnalyzeFreshnessSignals(string content, string language)
    {
        var score = 3.0;
        var currentYear = DateTime.Now.Year;

        // 当前年份
        if (content.Contains(currentYear.ToString()))
            score += 3;
        else if (content.Contains((currentYear - 1).ToString()))
            score += 2;

        // 更新声明
        var updatePatterns = language == "zh"
            ? new[] { @"更新于", @"最后更新", @"最新版本", @"[2][0][2][0-9]年\d+月" }
            : new[] { @"updated on", @"last updated", @"as of [2][0][2][0-9]", @"[A-Z][a-z]+ [2][0][2][0-9]" };
        if (updatePatterns.Any(p => Regex.IsMatch(content, p, RegexOptions.IgnoreCase)))
            score += 2;

        // 时效性词汇
        var timelyPatterns = language == "zh"
            ? new[] { @"最新", @"最近", @"今年", @"本季度" }
            : new[] { @"latest", @"recent", @"this year", @"current" };
        var timelyCount = timelyPatterns.Sum(p => Regex.Matches(content, p, RegexOptions.IgnoreCase).Count);
        score += Math.Min(2, timelyCount * 0.5);

        return Math.Min(10, score);
    }

    private void GenerateAIOverviewSuggestions(AIOverviewScoreResult result, string language)
    {
        foreach (var factor in result.Factors.Where(f => f.Score < 6))
        {
            var suggestion = factor.NameEn switch
            {
                "Semantic Completeness" => language == "zh"
                    ? "添加定义性语句和结论性总结，确保内容自包含"
                    : "Add definitions and conclusions to make content self-contained",
                "Multimodal Support" => language == "zh"
                    ? "添加表格、图片或代码示例以提升多模态评分"
                    : "Add tables, images, or code examples to improve multimodal score",
                "Factual Verifiability" => language == "zh"
                    ? "增加具体数据、统计和权威来源引用"
                    : "Add specific data, statistics, and authoritative source citations",
                "Structure Clarity" => language == "zh"
                    ? "使用 H2/H3 标题和列表改善内容结构"
                    : "Use H2/H3 headings and lists to improve structure",
                "Answer Directness" => language == "zh"
                    ? "在前 40-60 词内直接回答核心问题"
                    : "Directly answer the core question within the first 40-60 words",
                "Authority Signals" => language == "zh"
                    ? "添加作者信息、专家引用或机构背书"
                    : "Add author info, expert quotes, or institutional endorsements",
                "Freshness Signals" => language == "zh"
                    ? $"添加 {DateTime.Now.Year} 年份标记和更新日期"
                    : $"Add {DateTime.Now.Year} year markers and update dates",
                _ => ""
            };

            if (!string.IsNullOrEmpty(suggestion))
                result.Suggestions.Add($"[{factor.Name}] {suggestion}");
        }
    }

    #endregion

    #region 7.31 最优段落长度检测

    /// <summary>
    /// 最优段落长度检测 (7.31)
    /// 原理：Wellows 研究显示 127-156 词是最优 AI 提取长度
    /// </summary>
    public ParagraphLengthAnalysis AnalyzeParagraphLengths(string content, string language = "zh")
    {
        var result = new ParagraphLengthAnalysis
        {
            Paragraphs = new List<ParagraphInfo>(),
            OptimalRange = (127, 156),
            Suggestions = new List<string>()
        };

        // 分割段落
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Trim().Length > 20)
            .ToList();

        var isCharacterBased = language == "zh" || language == "ja" || language == "ko";

        foreach (var para in paragraphs)
        {
            var wordCount = isCharacterBased
                ? para.Count(c => !char.IsWhiteSpace(c) && !char.IsPunctuation(c))
                : para.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                      .Count(w => w.Any(char.IsLetterOrDigit));

            // 中文字符数约等于英文词数的 1.5-2 倍
            var normalizedWordCount = isCharacterBased ? (int)(wordCount / 1.5) : wordCount;

            var status = normalizedWordCount switch
            {
                < 50 => "too_short",
                >= 50 and < 127 => "short",
                >= 127 and <= 156 => "optimal",
                > 156 and <= 200 => "acceptable",
                > 200 => "too_long"
            };

            result.Paragraphs.Add(new ParagraphInfo
            {
                Preview = para.Length > 100 ? para[..100] + "..." : para,
                WordCount = wordCount,
                NormalizedWordCount = normalizedWordCount,
                Status = status,
                IsOptimal = status == "optimal"
            });
        }

        // 统计
        result.TotalParagraphs = result.Paragraphs.Count;
        result.OptimalCount = result.Paragraphs.Count(p => p.IsOptimal);
        result.OptimalRate = result.TotalParagraphs > 0
            ? (double)result.OptimalCount / result.TotalParagraphs
            : 0;

        result.TooShortCount = result.Paragraphs.Count(p => p.Status == "too_short" || p.Status == "short");
        result.TooLongCount = result.Paragraphs.Count(p => p.Status == "too_long");

        // 计算分数
        result.Score = CalculateParagraphLengthScore(result);

        // 生成建议
        GenerateParagraphLengthSuggestions(result, language);

        _logger.LogDebug("[GEOAdvanced] 段落长度分析: {Total} 段落, {Optimal} 最优 ({Rate:P0})",
            result.TotalParagraphs, result.OptimalCount, result.OptimalRate);

        return result;
    }

    private double CalculateParagraphLengthScore(ParagraphLengthAnalysis result)
    {
        if (result.TotalParagraphs == 0) return 5;

        var score = 3.0;

        // 最优段落比例
        score += result.OptimalRate * 4;

        // 可接受段落
        var acceptableRate = result.Paragraphs.Count(p => p.Status == "acceptable") / (double)result.TotalParagraphs;
        score += acceptableRate * 2;

        // 惩罚过短/过长
        var badRate = (result.TooShortCount + result.TooLongCount) / (double)result.TotalParagraphs;
        score -= badRate * 2;

        return Math.Max(0, Math.Min(10, score));
    }

    private void GenerateParagraphLengthSuggestions(ParagraphLengthAnalysis result, string language)
    {
        if (result.TooShortCount > 0)
        {
            result.Suggestions.Add(language == "zh"
                ? $"有 {result.TooShortCount} 个段落过短（<127 词），建议合并或扩展"
                : $"{result.TooShortCount} paragraphs are too short (<127 words), consider merging or expanding");
        }

        if (result.TooLongCount > 0)
        {
            result.Suggestions.Add(language == "zh"
                ? $"有 {result.TooLongCount} 个段落过长（>200 词），建议拆分为 127-156 词段落"
                : $"{result.TooLongCount} paragraphs are too long (>200 words), consider splitting to 127-156 words");
        }

        if (result.OptimalRate < 0.3)
        {
            result.Suggestions.Add(language == "zh"
                ? "最优段落比例过低，目标是 50%+ 段落在 127-156 词范围内"
                : "Optimal paragraph rate is low, aim for 50%+ paragraphs in 127-156 word range");
        }

        if (result.OptimalRate >= 0.5)
        {
            result.Suggestions.Add(language == "zh"
                ? "✅ 段落长度分布良好，符合 AI 提取最优标准"
                : "✅ Paragraph length distribution is good, meets AI extraction optimal standards");
        }
    }

    #endregion
}

#region Models

public class ListicleArchitectureAudit
{
    public bool HasTopNTitle { get; set; }
    public int? TopNValue { get; set; }
    public bool HasNumberedList { get; set; }
    public int NumberedItemCount { get; set; }
    public bool HasComparisonTable { get; set; }
    public bool IsServicePage { get; set; }
    public bool IsCasePage { get; set; }
    public double ListicleScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class TripleJsonLdResult
{
    public string ArticleSchema { get; set; } = "";
    public string ItemListSchema { get; set; } = "";
    public string FaqPageSchema { get; set; } = "";
    public string CombinedScript { get; set; } = "";
    public List<string> ValidationMessages { get; set; } = new();
}

public class AIResponse
{
    public string Platform { get; set; } = "";
    public string ResponseText { get; set; } = "";
    public List<string>? Sources { get; set; }
    public bool IsBrandCited { get; set; }
    public double CitationPosition { get; set; }
}

public class SourcelessResponseFilter
{
    public int TotalResponses { get; set; }
    public List<AIResponse> SourcelessResponses { get; set; } = new();
    public List<AIResponse> SourcedResponses { get; set; } = new();
    public double SourcelessRate { get; set; }
    public FilteredMetrics FilteredMetrics { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class FilteredMetrics
{
    public double CitationRate { get; set; }
    public double AveragePosition { get; set; }
}

public class AIOverviewScoreResult
{
    public List<AIOverviewFactor> Factors { get; set; } = new();
    public double TotalScore { get; set; }
    public double MaxScore { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

public class AIOverviewFactor
{
    public string Name { get; set; } = "";
    public string NameEn { get; set; } = "";
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public double Correlation { get; set; }
    public string Description { get; set; } = "";
}

public class ParagraphLengthAnalysis
{
    public List<ParagraphInfo> Paragraphs { get; set; } = new();
    public (int min, int max) OptimalRange { get; set; }
    public int TotalParagraphs { get; set; }
    public int OptimalCount { get; set; }
    public double OptimalRate { get; set; }
    public int TooShortCount { get; set; }
    public int TooLongCount { get; set; }
    public double Score { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

public class ParagraphInfo
{
    public string Preview { get; set; } = "";
    public int WordCount { get; set; }
    public int NormalizedWordCount { get; set; }
    public string Status { get; set; } = "";
    public bool IsOptimal { get; set; }
}

#endregion
