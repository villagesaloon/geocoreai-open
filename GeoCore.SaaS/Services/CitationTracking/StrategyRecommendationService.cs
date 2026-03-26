using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using GeoCore.SaaS.Services.CitationTracking.Models;
using Microsoft.Extensions.Logging;

namespace GeoCore.SaaS.Services.CitationTracking;

/// <summary>
/// 策略建议服务 - 基于监测数据生成优化建议
/// </summary>
public class StrategyRecommendationService
{
    private readonly ICitationMonitoringRepository _repository;
    private readonly ILogger<StrategyRecommendationService> _logger;

    public StrategyRecommendationService(
        ICitationMonitoringRepository repository,
        ILogger<StrategyRecommendationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 基于监测数据生成优化建议
    /// </summary>
    public async Task<List<StrategyRecommendation>> GenerateRecommendationsAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var recommendations = new List<StrategyRecommendation>();

        // 获取最近的监测结果
        var recentResults = await _repository.GetRecentResultsAsync(taskId, 100, cancellationToken);
        if (recentResults.Count == 0)
        {
            _logger.LogWarning("[Strategy] No recent results found for task {TaskId}", taskId);
            return recommendations;
        }

        // 分析各项指标
        var citedResults = recentResults.Where(r => r.IsCited).ToList();
        var citationRate = (double)citedResults.Count / recentResults.Count;
        var avgBvs = citedResults.Any() ? citedResults.Average(r => r.VisibilityScore) : 0;
        var linkRate = citedResults.Any() ? (double)citedResults.Count(r => r.HasLink) / citedResults.Count : 0;
        var firstPositionRate = citedResults.Any() 
            ? (double)citedResults.Count(r => r.CitationPosition == "first") / citedResults.Count 
            : 0;
        var positiveRate = citedResults.Any()
            ? (double)citedResults.Count(r => r.Sentiment == "positive") / citedResults.Count
            : 0;
        var avgWordCountRatio = citedResults.Any() ? citedResults.Average(r => r.WordCountRatio) : 0;
        var avgPas = citedResults.Any() ? citedResults.Average(r => r.PositionAdjustedScore) : 0;

        // 分平台分析
        var platformStats = recentResults
            .GroupBy(r => r.Platform)
            .ToDictionary(
                g => g.Key,
                g => new PlatformStats
                {
                    TotalQueries = g.Count(),
                    CitedCount = g.Count(r => r.IsCited),
                    CitationRate = (double)g.Count(r => r.IsCited) / g.Count(),
                    AvgBvs = g.Where(r => r.IsCited).Any() ? g.Where(r => r.IsCited).Average(r => r.VisibilityScore) : 0
                });

        // 生成建议

        // 1. 引用率低于目标
        if (citationRate < 0.30)
        {
            recommendations.Add(new StrategyRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Title = "提升整体引用率",
                Priority = StrategyPriority.High,
                Category = StrategyCategory.CitationRate,
                Description = $"当前引用率 {citationRate:P0}，低于目标 30%。需要优化内容以提升 AI 引用概率。",
                CurrentValue = citationRate,
                TargetValue = 0.30,
                ExpectedLift = 0.30 - citationRate,
                Actions = new List<string>
                {
                    "增加权威性内容，如数据、研究、案例",
                    "优化品牌描述，突出独特价值",
                    "确保内容结构清晰，便于 AI 理解",
                    "添加 Schema 结构化数据"
                },
                EstimatedDays = 7
            });
        }

        // 2. 首位引用率低
        if (firstPositionRate < 0.40)
        {
            recommendations.Add(new StrategyRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Title = "提升首位引用比例",
                Priority = StrategyPriority.High,
                Category = StrategyCategory.Position,
                Description = $"当前首位引用率 {firstPositionRate:P0}，建议提升至 40% 以上。",
                CurrentValue = firstPositionRate,
                TargetValue = 0.40,
                ExpectedLift = 0.40 - firstPositionRate,
                Actions = new List<string>
                {
                    "在内容开头强化品牌定位语句",
                    "使用\"最佳\"、\"领先\"等正面关键词",
                    "确保品牌名称在关键段落首句出现",
                    "优化 llms.txt 文件中的品牌描述"
                },
                EstimatedDays = 5
            });
        }

        // 3. 带链接引用率低
        if (linkRate < 0.30)
        {
            recommendations.Add(new StrategyRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Title = "增加带链接引用",
                Priority = StrategyPriority.High,
                Category = StrategyCategory.Links,
                Description = $"当前带链接引用率 {linkRate:P0}，建议提升至 30% 以上。",
                CurrentValue = linkRate,
                TargetValue = 0.30,
                ExpectedLift = 0.30 - linkRate,
                Actions = new List<string>
                {
                    "在官网添加更多可引用的权威内容",
                    "创建专题页面，便于 AI 引用",
                    "优化 URL 结构，使其更易被识别",
                    "增加外部权威网站的反向链接"
                },
                EstimatedDays = 7
            });
        }

        // 4. 正面情感比例低
        if (positiveRate < 0.70)
        {
            recommendations.Add(new StrategyRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Title = "提升正面情感比例",
                Priority = StrategyPriority.Medium,
                Category = StrategyCategory.Sentiment,
                Description = $"当前正面情感比例 {positiveRate:P0}，建议提升至 70% 以上。",
                CurrentValue = positiveRate,
                TargetValue = 0.70,
                ExpectedLift = 0.70 - positiveRate,
                Actions = new List<string>
                {
                    "分析负面情感来源，针对性优化",
                    "增加正面评价和成功案例",
                    "优化产品描述，突出优势",
                    "处理常见负面问题的 FAQ"
                },
                EstimatedDays = 5
            });
        }

        // 5. 词数占比低
        if (avgWordCountRatio < 0.08)
        {
            recommendations.Add(new StrategyRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Title = "增加引用内容深度",
                Priority = StrategyPriority.Medium,
                Category = StrategyCategory.Content,
                Description = $"当前引用词数占比 {avgWordCountRatio:P1}，建议提升至 8% 以上。",
                CurrentValue = avgWordCountRatio,
                TargetValue = 0.08,
                ExpectedLift = 0.08 - avgWordCountRatio,
                Actions = new List<string>
                {
                    "提供更详细的品牌信息",
                    "增加技术深度内容",
                    "添加更多可引用的数据点",
                    "创建综合性的品牌介绍页面"
                },
                EstimatedDays = 5
            });
        }

        // 6. 分平台优化建议
        foreach (var (platform, stats) in platformStats)
        {
            if (stats.CitationRate < citationRate * 0.8) // 低于平均 20%
            {
                recommendations.Add(new StrategyRecommendation
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"优化 {platform} 平台引用率",
                    Priority = StrategyPriority.Medium,
                    Category = StrategyCategory.Platform,
                    Description = $"{platform} 引用率 {stats.CitationRate:P0}，低于平均水平 {citationRate:P0}。",
                    CurrentValue = stats.CitationRate,
                    TargetValue = citationRate,
                    ExpectedLift = citationRate - stats.CitationRate,
                    Actions = GetPlatformSpecificActions(platform),
                    EstimatedDays = 5
                });
            }
        }

        // 按优先级排序
        recommendations = recommendations
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.ExpectedLift)
            .ToList();

        _logger.LogInformation(
            "[Strategy] Generated {Count} recommendations for task {TaskId}",
            recommendations.Count, taskId);

        return recommendations;
    }

    private List<string> GetPlatformSpecificActions(string platform)
    {
        return platform.ToLower() switch
        {
            "chatgpt" => new List<string>
            {
                "优化内容的对话友好性",
                "增加清晰的问答格式内容",
                "确保品牌信息在训练数据截止日期前已发布"
            },
            "claude" => new List<string>
            {
                "使用更学术化的语言风格",
                "增加数据支撑和引用来源",
                "优化内容结构层级"
            },
            "perplexity" => new List<string>
            {
                "确保网站可被爬取",
                "优化页面加载速度",
                "增加实时更新的内容"
            },
            "gemini" => new List<string>
            {
                "优化多媒体内容",
                "增加 Google 生态系统的存在感",
                "使用 Google 推荐的结构化数据"
            },
            "grok" => new List<string>
            {
                "增加社交媒体存在感",
                "优化 X/Twitter 上的品牌内容",
                "使用更口语化的表达"
            },
            _ => new List<string>
            {
                "优化内容质量",
                "增加权威性来源",
                "提升品牌知名度"
            }
        };
    }

    /// <summary>
    /// 基于监测结果生成测试问题
    /// </summary>
    public async Task<List<string>> GenerateTestQuestionsAsync(
        int taskId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var questions = new List<string>();

        // 获取任务信息
        var task = await _repository.GetTaskAsync(taskId, cancellationToken);
        if (task == null)
        {
            _logger.LogWarning("[Strategy] Task {TaskId} not found", taskId);
            return questions;
        }

        // 获取最近的监测结果
        var recentResults = await _repository.GetRecentResultsAsync(taskId, 50, cancellationToken);
        
        // 分析哪些类型的问题引用率高
        var citedQuestions = recentResults
            .Where(r => r.IsCited)
            .Select(r => r.Question)
            .Distinct()
            .ToList();

        // 生成问题模板
        var templates = new List<string>
        {
            $"什么是 {task.BrandName}？",
            $"{task.BrandName} 有什么优势？",
            $"推荐一个类似 {task.BrandName} 的工具",
            $"{task.BrandName} 和竞品相比如何？",
            $"如何使用 {task.BrandName}？",
            $"{task.BrandName} 的价格是多少？",
            $"{task.BrandName} 适合什么场景？",
            $"为什么选择 {task.BrandName}？",
            $"{task.BrandName} 的用户评价如何？",
            $"{task.BrandName} 有哪些功能？"
        };

        // 返回指定数量的问题
        return templates.Take(count).ToList();
    }
}

/// <summary>
/// 策略建议
/// </summary>
public class StrategyRecommendation
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public StrategyPriority Priority { get; set; }
    public StrategyCategory Category { get; set; }
    public string Description { get; set; } = "";
    public double CurrentValue { get; set; }
    public double TargetValue { get; set; }
    public double ExpectedLift { get; set; }
    public List<string> Actions { get; set; } = new();
    public int EstimatedDays { get; set; }
    public StrategyStatus Status { get; set; } = StrategyStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum StrategyPriority
{
    Low,
    Medium,
    High
}

public enum StrategyCategory
{
    CitationRate,
    Position,
    Links,
    Sentiment,
    Content,
    Platform
}

public enum StrategyStatus
{
    Pending,
    InProgress,
    Completed,
    Dismissed
}

internal class PlatformStats
{
    public int TotalQueries { get; set; }
    public int CitedCount { get; set; }
    public double CitationRate { get; set; }
    public double AvgBvs { get; set; }
}
