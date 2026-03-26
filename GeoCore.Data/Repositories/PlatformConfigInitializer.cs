using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 平台配置初始化器 - 初始化 AI 爬虫、LLM Referrer 等默认数据
/// </summary>
public class PlatformConfigInitializer
{
    private readonly GeoDbContext _db;

    public PlatformConfigInitializer(GeoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 初始化所有平台配置数据
    /// </summary>
    public async Task InitializeAsync()
    {
        await InitAICrawlersAsync();
        await InitLLMReferrersAsync();
        await InitPlatformPreferencesAsync();
        await InitCitationBenchmarksAsync();
        Console.WriteLine("[PlatformConfig] 平台配置初始化完成");
    }

    /// <summary>
    /// 初始化 AI 爬虫数据（完整数据，包含 Purpose/Importance/TrafficShare/AlternativeNames）
    /// </summary>
    private async Task InitAICrawlersAsync()
    {
        var count = await _db.Client.Queryable<AICrawlerEntity>().CountAsync();
        if (count > 0) return;

        var crawlers = new List<AICrawlerEntity>
        {
            // OpenAI 爬虫
            new() { Name = "GPTBot", UserAgentPattern = "GPTBot", Company = "OpenAI", Platform = "ChatGPT", Purpose = "训练 GPT 模型和 ChatGPT 搜索", Importance = "high", TrafficShare = 40.0, AlternativeNames = "[\"gptbot\"]", DocumentationUrl = "https://platform.openai.com/docs/gptbot", RespectsRobotsTxt = true, SortOrder = 1 },
            new() { Name = "ChatGPT-User", UserAgentPattern = "ChatGPT-User", Company = "OpenAI", Platform = "ChatGPT", Purpose = "ChatGPT 用户实时浏览", Importance = "high", TrafficShare = 15.0, AlternativeNames = "[\"chatgpt-user\"]", DocumentationUrl = "https://platform.openai.com/docs/plugins/bot", RespectsRobotsTxt = true, SortOrder = 2 },
            new() { Name = "OAI-SearchBot", UserAgentPattern = "OAI-SearchBot", Company = "OpenAI", Platform = "ChatGPT Search", Purpose = "ChatGPT 搜索功能", Importance = "high", TrafficShare = 10.0, AlternativeNames = "[\"oai-searchbot\"]", RespectsRobotsTxt = true, SortOrder = 3 },
            // Anthropic 爬虫
            new() { Name = "ClaudeBot", UserAgentPattern = "ClaudeBot", Company = "Anthropic", Platform = "Claude", Purpose = "训练 Claude 模型", Importance = "high", TrafficShare = 8.0, AlternativeNames = "[\"claudebot\",\"anthropic-ai\"]", DocumentationUrl = "https://www.anthropic.com/claude-bot", RespectsRobotsTxt = true, SortOrder = 4 },
            new() { Name = "Claude-Web", UserAgentPattern = "Claude-Web", Company = "Anthropic", Platform = "Claude", Purpose = "Claude 网页浏览功能", Importance = "medium", TrafficShare = 2.0, AlternativeNames = "[\"claude-web\"]", RespectsRobotsTxt = true, SortOrder = 5 },
            // Perplexity 爬虫
            new() { Name = "PerplexityBot", UserAgentPattern = "PerplexityBot", Company = "Perplexity AI", Platform = "Perplexity", Purpose = "Perplexity 搜索引擎", Importance = "high", TrafficShare = 8.0, AlternativeNames = "[\"perplexitybot\"]", DocumentationUrl = "https://docs.perplexity.ai/docs/perplexitybot", RespectsRobotsTxt = true, SortOrder = 6 },
            // Google 爬虫
            new() { Name = "Google-Extended", UserAgentPattern = "Google-Extended", Company = "Google", Platform = "Gemini / Bard", Purpose = "训练 Gemini/Bard 模型", Importance = "high", TrafficShare = 5.0, AlternativeNames = "[\"google-extended\"]", DocumentationUrl = "https://developers.google.com/search/docs/crawling-indexing/google-extended", RespectsRobotsTxt = true, SortOrder = 7 },
            new() { Name = "Googlebot", UserAgentPattern = "Googlebot", Company = "Google", Platform = "Google AI Overviews", Purpose = "Google 搜索和 AI Overviews", Importance = "high", TrafficShare = 5.0, AlternativeNames = "[\"googlebot\"]", RespectsRobotsTxt = true, SortOrder = 8 },
            // Microsoft/Bing 爬虫
            new() { Name = "Bingbot", UserAgentPattern = "Bingbot", Company = "Microsoft", Platform = "Bing / Copilot", Purpose = "Bing 搜索和 Copilot", Importance = "medium", TrafficShare = 3.0, AlternativeNames = "[\"bingbot\"]", RespectsRobotsTxt = true, SortOrder = 9 },
            // Meta 爬虫
            new() { Name = "FacebookBot", UserAgentPattern = "FacebookBot", Company = "Meta", Platform = "Meta AI", Purpose = "训练 Meta AI 模型", Importance = "medium", TrafficShare = 1.0, AlternativeNames = "[\"facebookbot\",\"meta-externalagent\"]", RespectsRobotsTxt = true, SortOrder = 10 },
            new() { Name = "Meta-ExternalAgent", UserAgentPattern = "Meta-ExternalAgent", Company = "Meta", Platform = "Meta AI", Purpose = "Meta AI 外部代理", Importance = "medium", TrafficShare = 0.5, AlternativeNames = "[\"meta-externalagent\"]", RespectsRobotsTxt = true, SortOrder = 11 },
            // Apple 爬虫
            new() { Name = "Applebot-Extended", UserAgentPattern = "Applebot-Extended", Company = "Apple", Platform = "Apple Intelligence", Purpose = "训练 Apple Intelligence", Importance = "medium", TrafficShare = 1.0, AlternativeNames = "[\"applebot-extended\"]", RespectsRobotsTxt = true, SortOrder = 12 },
            // 其他 AI 爬虫
            new() { Name = "cohere-ai", UserAgentPattern = "cohere-ai", Company = "Cohere", Platform = "Cohere", Purpose = "训练 Cohere 模型", Importance = "low", TrafficShare = 0.5, AlternativeNames = "[\"cohere-ai\"]", RespectsRobotsTxt = true, SortOrder = 13 },
            new() { Name = "Bytespider", UserAgentPattern = "Bytespider", Company = "ByteDance", Platform = "TikTok / Doubao", Purpose = "训练字节跳动 AI 模型", Importance = "low", TrafficShare = 0.5, AlternativeNames = "[\"bytespider\"]", RespectsRobotsTxt = true, SortOrder = 14 },
            new() { Name = "CCBot", UserAgentPattern = "CCBot", Company = "Common Crawl", Platform = "多个 AI 平台", Purpose = "Common Crawl 数据集（被多个 AI 使用）", Importance = "medium", TrafficShare = 0.5, AlternativeNames = "[\"ccbot\"]", DocumentationUrl = "https://commoncrawl.org/ccbot", RespectsRobotsTxt = true, SortOrder = 15 },
            new() { Name = "omgili", UserAgentPattern = "omgili", Company = "Webz.io", Platform = "多个 AI 平台", Purpose = "数据聚合（被多个 AI 使用）", Importance = "low", TrafficShare = 0.2, AlternativeNames = "[\"omgili\",\"omgilibot\"]", RespectsRobotsTxt = true, SortOrder = 16 },
            new() { Name = "Diffbot", UserAgentPattern = "Diffbot", Company = "Diffbot", Platform = "知识图谱", Purpose = "结构化数据提取", Importance = "low", TrafficShare = 0.2, AlternativeNames = "[\"diffbot\"]", RespectsRobotsTxt = true, SortOrder = 17 },
            new() { Name = "YouBot", UserAgentPattern = "YouBot", Company = "You.com", Platform = "You.com", Purpose = "You.com AI 搜索", Importance = "low", TrafficShare = 0.3, AlternativeNames = "[\"youbot\"]", RespectsRobotsTxt = true, SortOrder = 18 }
        };

        await _db.Client.Insertable(crawlers).ExecuteCommandAsync();
        Console.WriteLine($"[PlatformConfig] 初始化 {crawlers.Count} 个 AI 爬虫配置");
    }

    /// <summary>
    /// 初始化 LLM Referrer 数据（完整数据，包含 GA4 追踪所需字段）
    /// </summary>
    private async Task InitLLMReferrersAsync()
    {
        var count = await _db.Client.Queryable<LLMReferrerEntity>().CountAsync();
        if (count > 0) return;

        var referrers = new List<LLMReferrerEntity>
        {
            // ChatGPT / OpenAI
            new() { PlatformName = "ChatGPT", Company = "OpenAI", ReferrerPattern = "chat.openai.com", UserAgentPatterns = "[\"ChatGPT-User\",\"GPTBot\"]", TrafficType = "referral", EstimatedShare = 87.4, Description = "OpenAI ChatGPT 网页版", Notes = "驱动 87.4% 的 AI 引荐流量", SortOrder = 1 },
            new() { PlatformName = "ChatGPT", Company = "OpenAI", ReferrerPattern = "chatgpt.com", UserAgentPatterns = "[\"ChatGPT-User\",\"GPTBot\"]", TrafficType = "referral", EstimatedShare = 0, Description = "OpenAI ChatGPT 新域名", SortOrder = 2 },
            new() { PlatformName = "ChatGPT", Company = "OpenAI", ReferrerPattern = "openai.com", UserAgentPatterns = "[\"ChatGPT-User\",\"GPTBot\"]", TrafficType = "referral", EstimatedShare = 0, Description = "OpenAI 官网", SortOrder = 3 },
            // Perplexity
            new() { PlatformName = "Perplexity", Company = "Perplexity AI", ReferrerPattern = "perplexity.ai", UserAgentPatterns = "[\"PerplexityBot\"]", TrafficType = "referral", EstimatedShare = 5.0, Description = "Perplexity AI 搜索", Notes = "AI 搜索引擎，始终带引用链接", SortOrder = 4 },
            new() { PlatformName = "Perplexity", Company = "Perplexity AI", ReferrerPattern = "www.perplexity.ai", UserAgentPatterns = "[\"PerplexityBot\"]", TrafficType = "referral", EstimatedShare = 0, Description = "Perplexity AI 搜索", SortOrder = 5 },
            // Claude / Anthropic
            new() { PlatformName = "Claude", Company = "Anthropic", ReferrerPattern = "claude.ai", UserAgentPatterns = "[\"ClaudeBot\",\"Claude-Web\"]", TrafficType = "referral", EstimatedShare = 3.0, Description = "Anthropic Claude 网页版", Notes = "专业 AI 助手", SortOrder = 6 },
            new() { PlatformName = "Claude", Company = "Anthropic", ReferrerPattern = "anthropic.com", UserAgentPatterns = "[\"ClaudeBot\",\"Claude-Web\"]", TrafficType = "referral", EstimatedShare = 0, Description = "Anthropic 官网", SortOrder = 7 },
            // Google AI / Gemini
            new() { PlatformName = "Google AI", Company = "Google", ReferrerPattern = "gemini.google.com", UserAgentPatterns = "[\"Google-Extended\"]", TrafficType = "referral", EstimatedShare = 2.0, Description = "Google Gemini", Notes = "Google Gemini/Bard", SortOrder = 8 },
            new() { PlatformName = "Google AI", Company = "Google", ReferrerPattern = "bard.google.com", UserAgentPatterns = "[\"Google-Extended\"]", TrafficType = "referral", EstimatedShare = 0, Description = "Google Bard（已更名）", SortOrder = 9 },
            new() { PlatformName = "Google AI", Company = "Google", ReferrerPattern = "ai.google", UserAgentPatterns = "[\"Google-Extended\"]", TrafficType = "referral", EstimatedShare = 0, Description = "Google AI", SortOrder = 10 },
            // Microsoft Copilot
            new() { PlatformName = "Copilot", Company = "Microsoft", ReferrerPattern = "copilot.microsoft.com", UserAgentPatterns = "[\"Bingbot\"]", TrafficType = "referral", EstimatedShare = 1.5, Description = "Microsoft Copilot", Notes = "Microsoft Copilot / Bing Chat", SortOrder = 11 },
            new() { PlatformName = "Copilot", Company = "Microsoft", ReferrerPattern = "bing.com/chat", UserAgentPatterns = "[\"Bingbot\"]", TrafficType = "referral", EstimatedShare = 0, Description = "Bing Chat", SortOrder = 12 },
            // You.com
            new() { PlatformName = "You.com", Company = "You.com", ReferrerPattern = "you.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.5, Description = "You.com AI 搜索", Notes = "AI 搜索引擎", SortOrder = 13 },
            // Phind
            new() { PlatformName = "Phind", Company = "Phind", ReferrerPattern = "phind.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.3, Description = "Phind 开发者 AI 搜索", Notes = "开发者 AI 搜索", SortOrder = 14 },
            new() { PlatformName = "Phind", Company = "Phind", ReferrerPattern = "www.phind.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0, Description = "Phind 开发者 AI 搜索", SortOrder = 15 },
            // Kagi
            new() { PlatformName = "Kagi", Company = "Kagi", ReferrerPattern = "kagi.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.2, Description = "Kagi 付费 AI 搜索引擎", Notes = "付费 AI 搜索引擎", SortOrder = 16 },
            // Poe
            new() { PlatformName = "Poe", Company = "Quora", ReferrerPattern = "poe.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.1, Description = "Quora Poe 聚合平台", SortOrder = 17 },
            // 国内平台
            new() { PlatformName = "Kimi", Company = "月之暗面", ReferrerPattern = "kimi.moonshot.cn", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.1, Description = "月之暗面 Kimi", SortOrder = 18 },
            new() { PlatformName = "通义千问", Company = "阿里巴巴", ReferrerPattern = "tongyi.aliyun.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.1, Description = "阿里通义千问", SortOrder = 19 },
            new() { PlatformName = "文心一言", Company = "百度", ReferrerPattern = "yiyan.baidu.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.1, Description = "百度文心一言", SortOrder = 20 },
            new() { PlatformName = "Grok", Company = "xAI", ReferrerPattern = "x.com", UserAgentPatterns = "[]", TrafficType = "referral", EstimatedShare = 0.1, Description = "X/Twitter Grok", SortOrder = 21 }
        };

        await _db.Client.Insertable(referrers).ExecuteCommandAsync();
        Console.WriteLine($"[PlatformConfig] 初始化 {referrers.Count} 个 LLM Referrer 配置");
    }

    /// <summary>
    /// 初始化平台偏好数据
    /// </summary>
    private async Task InitPlatformPreferencesAsync()
    {
        var count = await _db.Client.Queryable<LLMPlatformPreferenceEntity>().CountAsync();
        if (count > 0) return;

        var preferences = new List<LLMPlatformPreferenceEntity>
        {
            // Perplexity 偏好
            new() { PlatformName = "Perplexity", PreferenceCategory = "source_type", PreferenceName = "authoritative_sources", PreferenceValue = 90, Description = "偏好权威来源如学术论文、官方文档", DataSource = "Perplexity 官方文档" },
            new() { PlatformName = "Perplexity", PreferenceCategory = "source_type", PreferenceName = "recent_content", PreferenceValue = 85, Description = "偏好最近更新的内容", DataSource = "Perplexity 官方文档" },
            new() { PlatformName = "Perplexity", PreferenceCategory = "content_format", PreferenceName = "structured_data", PreferenceValue = 80, Description = "偏好结构化数据如表格、列表", DataSource = "用户研究" },
            // ChatGPT 偏好
            new() { PlatformName = "ChatGPT", PreferenceCategory = "source_type", PreferenceName = "diverse_sources", PreferenceValue = 75, Description = "倾向于综合多个来源", DataSource = "OpenAI 研究" },
            new() { PlatformName = "ChatGPT", PreferenceCategory = "content_format", PreferenceName = "detailed_explanations", PreferenceValue = 85, Description = "偏好详细解释性内容", DataSource = "OpenAI 研究" },
            // Claude 偏好
            new() { PlatformName = "Claude", PreferenceCategory = "source_type", PreferenceName = "primary_sources", PreferenceValue = 80, Description = "偏好一手资料", DataSource = "Anthropic 文档" },
            new() { PlatformName = "Claude", PreferenceCategory = "content_format", PreferenceName = "nuanced_content", PreferenceValue = 85, Description = "偏好有细微差别的内容", DataSource = "Anthropic 文档" },
            // Gemini 偏好
            new() { PlatformName = "Gemini", PreferenceCategory = "source_type", PreferenceName = "google_indexed", PreferenceValue = 90, Description = "偏好 Google 索引的内容", DataSource = "Google 搜索整合" },
            new() { PlatformName = "Gemini", PreferenceCategory = "content_format", PreferenceName = "multimedia", PreferenceValue = 75, Description = "支持多模态内容", DataSource = "Google 研究" }
        };

        await _db.Client.Insertable(preferences).ExecuteCommandAsync();
        Console.WriteLine($"[PlatformConfig] 初始化 {preferences.Count} 个平台偏好配置");
    }

    /// <summary>
    /// 初始化引用基准数据
    /// </summary>
    private async Task InitCitationBenchmarksAsync()
    {
        var count = await _db.Client.Queryable<CitationBenchmarkEntity>().CountAsync();
        if (count > 0) return;

        var benchmarks = new List<CitationBenchmarkEntity>
        {
            new() { PlatformName = "Perplexity", MetricName = "avg_citations_per_response", MetricValue = 5.2, MetricUnit = "个", Description = "每次回复平均引用数", DataSource = "2024 研究报告" },
            new() { PlatformName = "Perplexity", MetricName = "source_diversity_score", MetricValue = 78, MetricUnit = "%", Description = "来源多样性评分", DataSource = "2024 研究报告" },
            new() { PlatformName = "ChatGPT", MetricName = "avg_citations_per_response", MetricValue = 2.1, MetricUnit = "个", Description = "每次回复平均引用数（Browse 模式）", DataSource = "2024 研究报告" },
            new() { PlatformName = "Claude", MetricName = "avg_citations_per_response", MetricValue = 1.8, MetricUnit = "个", Description = "每次回复平均引用数", DataSource = "2024 研究报告" },
            new() { PlatformName = "Gemini", MetricName = "avg_citations_per_response", MetricValue = 3.5, MetricUnit = "个", Description = "每次回复平均引用数", DataSource = "2024 研究报告" },
            new() { PlatformName = "Perplexity", MetricName = "freshness_weight", MetricValue = 85, MetricUnit = "%", Description = "内容新鲜度权重", DataSource = "2024 研究报告" },
            new() { PlatformName = "ChatGPT", MetricName = "freshness_weight", MetricValue = 60, MetricUnit = "%", Description = "内容新鲜度权重", DataSource = "2024 研究报告" }
        };

        await _db.Client.Insertable(benchmarks).ExecuteCommandAsync();
        Console.WriteLine($"[PlatformConfig] 初始化 {benchmarks.Count} 个引用基准配置");
    }
}
