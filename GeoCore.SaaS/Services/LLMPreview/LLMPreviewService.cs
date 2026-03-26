using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.LLMPreview;

/// <summary>
/// LLM 预览服务 (3.31, 4.54)
/// </summary>
public class LLMPreviewService
{
    private readonly ILogger<LLMPreviewService> _logger;

    // 平台配置
    private static readonly Dictionary<string, (string DisplayName, string Style)> PlatformStyles = new()
    {
        ["chatgpt"] = ("ChatGPT", "conversational"),
        ["perplexity"] = ("Perplexity", "citation-heavy"),
        ["claude"] = ("Claude", "analytical"),
        ["gemini"] = ("Gemini", "concise"),
        ["copilot"] = ("Copilot", "helpful")
    };

    public LLMPreviewService(ILogger<LLMPreviewService> logger)
    {
        _logger = logger;
    }

    #region 3.31 LLM 预览模拟

    /// <summary>
    /// 生成 LLM 预览
    /// </summary>
    public LLMPreviewResult GeneratePreview(LLMPreviewRequest request)
    {
        var result = new LLMPreviewResult();

        // 为每个平台生成预览
        foreach (var platform in request.TargetPlatforms)
        {
            if (PlatformStyles.TryGetValue(platform.ToLower(), out var config))
            {
                var preview = GeneratePlatformPreview(request.Content, request.Title, platform, config, request.SimulatedQuery);
                result.Previews.Add(preview);
            }
        }

        // 预测引用片段
        result.PredictedCitations = PredictCitations(request.Content);

        // 计算综合可引用性评分
        result.OverallCitabilityScore = CalculateOverallCitability(result);

        // 生成建议
        result.Suggestions = GeneratePreviewSuggestions(result, request.Content);

        return result;
    }

    private PlatformPreview GeneratePlatformPreview(string content, string? title, string platform, (string DisplayName, string Style) config, string? query)
    {
        var preview = new PlatformPreview
        {
            Platform = platform,
            DisplayName = config.DisplayName
        };

        // 分析内容特征
        var wordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var hasStructure = Regex.IsMatch(content, @"(##|###|\d+\.|[-•])");
        var hasFacts = Regex.IsMatch(content, @"\d+%|\d+\.\d+|统计|数据|研究");
        var hasDirectAnswer = content.Split('.').FirstOrDefault()?.Length < 150;

        // 根据平台风格计算引用概率
        preview.CitationProbability = config.Style switch
        {
            "citation-heavy" => hasFacts ? 75 : 50, // Perplexity 偏好事实
            "analytical" => wordCount > 500 ? 70 : 45, // Claude 偏好深度
            "conversational" => hasDirectAnswer ? 65 : 40, // ChatGPT 偏好直接答案
            "concise" => hasStructure ? 60 : 35, // Gemini 偏好结构
            _ => 50
        };

        preview.LikelyCited = preview.CitationProbability >= 60;

        // 预测引用位置
        preview.CitationPosition = preview.CitationProbability >= 70 ? "主要引用" :
                                   preview.CitationProbability >= 50 ? "补充引用" : "可能不被引用";

        // 模拟回答
        preview.SimulatedResponse = GenerateSimulatedResponse(content, title, config.Style, query);

        // 平台特定建议
        preview.PlatformTips = GetPlatformTips(platform, content);

        return preview;
    }

    private string GenerateSimulatedResponse(string content, string? title, string style, string? query)
    {
        var firstParagraph = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.Trim() ?? "";

        var excerpt = firstParagraph.Length > 200 ? firstParagraph[..200] + "..." : firstParagraph;

        return style switch
        {
            "citation-heavy" => $"根据相关资料[1]，{excerpt}\n\n[1] {title ?? "来源"}",
            "analytical" => $"让我分析一下这个问题。{excerpt}\n\n这表明...",
            "conversational" => $"{excerpt}\n\n希望这对你有帮助！",
            "concise" => excerpt,
            _ => excerpt
        };
    }

    private List<string> GetPlatformTips(string platform, string content)
    {
        return platform.ToLower() switch
        {
            "perplexity" => new List<string>
            {
                "Perplexity 偏好新鲜内容，确保内容时效性",
                "添加具体数据和统计以提高引用概率",
                "使用清晰的来源引用格式"
            },
            "claude" => new List<string>
            {
                "Claude 偏好深度分析内容",
                "提供详细的论证和推理",
                "使用 XML 结构标记重要信息"
            },
            "chatgpt" => new List<string>
            {
                "ChatGPT 偏好直接回答问题",
                "将核心答案放在内容开头",
                "使用简洁明了的语言"
            },
            "gemini" => new List<string>
            {
                "Gemini 偏好简短精炼的内容",
                "使用列表和结构化格式",
                "避免冗长的解释"
            },
            _ => new List<string> { "优化内容结构和清晰度" }
        };
    }

    private List<PredictedCitation> PredictCitations(string content)
    {
        var citations = new List<PredictedCitation>();

        // 分割成句子
        var sentences = Regex.Split(content, @"(?<=[。.!?！？])\s*")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        int position = 0;
        foreach (var sentence in sentences.Take(10))
        {
            var wordCount = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            
            // 评估引用概率
            double probability = 30;
            string reason = "";

            // 包含数据
            if (Regex.IsMatch(sentence, @"\d+%|\d+\.\d+"))
            {
                probability += 25;
                reason = "包含具体数据";
            }

            // 定义性陈述
            if (Regex.IsMatch(sentence, @"(是|指|定义|means|refers to|is defined as)", RegexOptions.IgnoreCase))
            {
                probability += 20;
                reason = string.IsNullOrEmpty(reason) ? "定义性陈述" : reason + "，定义性陈述";
            }

            // 最佳长度 (134-167 词)
            if (wordCount >= 20 && wordCount <= 50)
            {
                probability += 15;
            }

            // 在前 30% 内容
            if (position < content.Length * 0.3)
            {
                probability += 10;
                reason = string.IsNullOrEmpty(reason) ? "位于内容前30%" : reason + "，位于内容前30%";
            }

            if (probability >= 50)
            {
                citations.Add(new PredictedCitation
                {
                    Text = sentence.Length > 150 ? sentence[..150] + "..." : sentence,
                    StartPosition = position,
                    WordCount = wordCount,
                    Probability = Math.Min(95, probability),
                    Reason = reason
                });
            }

            position += sentence.Length;
        }

        return citations.OrderByDescending(c => c.Probability).Take(5).ToList();
    }

    private double CalculateOverallCitability(LLMPreviewResult result)
    {
        if (result.Previews.Count == 0) return 0;

        var avgProbability = result.Previews.Average(p => p.CitationProbability);
        var citationBonus = result.PredictedCitations.Count * 5;

        return Math.Min(100, avgProbability + citationBonus);
    }

    private List<PreviewSuggestion> GeneratePreviewSuggestions(LLMPreviewResult result, string content)
    {
        var suggestions = new List<PreviewSuggestion>();

        // 低引用概率平台
        var lowProbPlatforms = result.Previews.Where(p => p.CitationProbability < 50).ToList();
        if (lowProbPlatforms.Count > 0)
        {
            suggestions.Add(new PreviewSuggestion
            {
                Priority = "high",
                Message = "部分平台引用概率较低，建议优化内容结构",
                Impact = $"可能提升 {string.Join(", ", lowProbPlatforms.Select(p => p.DisplayName))} 的引用率",
                AffectedPlatforms = lowProbPlatforms.Select(p => p.Platform).ToList()
            });
        }

        // 缺少数据支持
        if (!Regex.IsMatch(content, @"\d+%|\d+\.\d+"))
        {
            suggestions.Add(new PreviewSuggestion
            {
                Priority = "medium",
                Message = "添加具体数据和统计信息",
                Impact = "数据支持可提升 25% 引用概率",
                AffectedPlatforms = new List<string> { "perplexity", "chatgpt" }
            });
        }

        // 缺少结构
        if (!Regex.IsMatch(content, @"(##|###|\d+\.|[-•])"))
        {
            suggestions.Add(new PreviewSuggestion
            {
                Priority = "medium",
                Message = "添加标题和列表结构",
                Impact = "结构化内容更易被 AI 解析和引用",
                AffectedPlatforms = new List<string> { "gemini", "claude" }
            });
        }

        return suggestions;
    }

    #endregion

    #region 4.54 持续优化循环

    /// <summary>
    /// 获取优化循环状态
    /// </summary>
    public OptimizationLoopStatus GetLoopStatus(OptimizationLoopConfig config)
    {
        var status = new OptimizationLoopStatus
        {
            ProjectId = config.ProjectId,
            CurrentStage = "detect"
        };

        // 模拟检测问题
        status.DetectedIssues = DetectIssues(config);

        // 确定当前阶段
        if (status.DetectedIssues.Count > 0)
        {
            status.CurrentStage = "fix";
            status.NextActions = new List<string>
            {
                $"发现 {status.DetectedIssues.Count} 个问题需要修复",
                "查看问题详情并应用建议的修复",
                "修复后等待 24-48 小时进行验证"
            };
        }
        else if (status.PendingFixes.Count > 0)
        {
            status.CurrentStage = "verify";
            status.NextActions = new List<string>
            {
                $"有 {status.PendingFixes.Count} 个修复待验证",
                "检查修复效果",
                "记录改进数据"
            };
        }
        else
        {
            status.CurrentStage = "detect";
            status.NextActions = new List<string>
            {
                "继续监测 AI 可见度指标",
                "定期检查内容新鲜度",
                "关注竞品动态"
            };
        }

        return status;
    }

    private List<DetectedIssue> DetectIssues(OptimizationLoopConfig config)
    {
        var issues = new List<DetectedIssue>();

        // 模拟检测逻辑
        foreach (var url in config.MonitoredUrls.Take(3))
        {
            // 内容新鲜度检测
            issues.Add(new DetectedIssue
            {
                IssueId = Guid.NewGuid().ToString("N")[..8],
                Type = "content_freshness",
                Description = "内容超过 60 天未更新",
                Severity = "medium",
                AffectedUrl = url,
                SuggestedFix = "更新内容中的日期和数据",
                DetectedAt = DateTime.UtcNow
            });
        }

        return issues;
    }

    /// <summary>
    /// 运行检测阶段
    /// </summary>
    public List<DetectedIssue> RunDetection(OptimizationLoopConfig config)
    {
        return DetectIssues(config);
    }

    /// <summary>
    /// 应用修复
    /// </summary>
    public PendingFix ApplyFix(string issueId, string fixDescription)
    {
        return new PendingFix
        {
            FixId = Guid.NewGuid().ToString("N")[..8],
            IssueId = issueId,
            Description = fixDescription,
            AppliedAt = DateTime.UtcNow,
            ExpectedVerificationTime = DateTime.UtcNow.AddHours(48)
        };
    }

    /// <summary>
    /// 验证修复效果
    /// </summary>
    public VerifiedImprovement? VerifyFix(string fixId, double beforeMetric, double afterMetric)
    {
        if (afterMetric <= beforeMetric)
            return null;

        return new VerifiedImprovement
        {
            ImprovementId = Guid.NewGuid().ToString("N")[..8],
            FixId = fixId,
            Description = "修复已验证有效",
            BeforeMetric = beforeMetric,
            AfterMetric = afterMetric,
            ImprovementPercentage = ((afterMetric - beforeMetric) / beforeMetric) * 100,
            VerifiedAt = DateTime.UtcNow
        };
    }

    #endregion
}
