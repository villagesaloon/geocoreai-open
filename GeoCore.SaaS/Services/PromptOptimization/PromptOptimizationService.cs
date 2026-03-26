using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.PromptOptimization;

/// <summary>
/// Prompt 优化服务 (5.31-5.32)
/// 提供 Context Engineering 和模型特定 Prompt 策略
/// </summary>
public class PromptOptimizationService
{
    private readonly ILogger<PromptOptimizationService> _logger;

    // 模型配置
    private static readonly Dictionary<string, ModelPromptConfig> ModelConfigs = new()
    {
        ["claude"] = new ModelPromptConfig
        {
            ModelFamily = "claude",
            ModelName = "Claude 3.5/4",
            PreferredFormat = "xml",
            OptimalWordRange = (150, 300),
            MaxTokens = 200000,
            SupportsXmlTags = true,
            RequiresCoT = false,
            SpecialNotes = new()
            {
                "使用 XML 标签组织结构（如 <context>, <task>, <output>）",
                "支持长上下文，但 3000 token 后性能下降",
                "偏好明确的角色定义和约束条件"
            }
        },
        ["gpt"] = new ModelPromptConfig
        {
            ModelFamily = "gpt",
            ModelName = "GPT-4/GPT-5",
            PreferredFormat = "markdown",
            OptimalWordRange = (100, 250),
            MaxTokens = 128000,
            SupportsXmlTags = false,
            RequiresCoT = false,
            SpecialNotes = new()
            {
                "GPT-5 可跳过 Chain of Thought，直接给出答案",
                "使用 Markdown 格式组织结构",
                "支持 System/User/Assistant 角色分离"
            }
        },
        ["gemini"] = new ModelPromptConfig
        {
            ModelFamily = "gemini",
            ModelName = "Gemini 2.0",
            PreferredFormat = "concise",
            OptimalWordRange = (80, 200),
            MaxTokens = 1000000,
            SupportsXmlTags = false,
            RequiresCoT = true,
            SpecialNotes = new()
            {
                "偏好简短直接的指令",
                "支持超长上下文但建议精简",
                "多模态场景下保持指令简洁"
            }
        },
        ["llama"] = new ModelPromptConfig
        {
            ModelFamily = "llama",
            ModelName = "Llama 3/4",
            PreferredFormat = "structured",
            OptimalWordRange = (100, 250),
            MaxTokens = 128000,
            SupportsXmlTags = false,
            RequiresCoT = true,
            SpecialNotes = new()
            {
                "使用清晰的分隔符（如 ###）",
                "需要明确的 CoT 引导",
                "避免过于复杂的嵌套结构"
            }
        }
    };

    // 模糊词列表
    private static readonly string[] AmbiguousWords = new[]
    {
        "好的", "适当的", "合适的", "一些", "某些", "大概", "可能", "也许",
        "尽量", "尽可能", "差不多", "左右", "大约", "相关的", "必要的",
        "good", "appropriate", "some", "maybe", "probably", "around",
        "roughly", "about", "relevant", "necessary", "proper", "suitable"
    };

    // 指令动词
    private static readonly string[] ActionVerbs = new[]
    {
        "分析", "总结", "列出", "解释", "比较", "评估", "生成", "创建",
        "编写", "修改", "优化", "检查", "验证", "提取", "转换", "计算",
        "analyze", "summarize", "list", "explain", "compare", "evaluate",
        "generate", "create", "write", "modify", "optimize", "check",
        "verify", "extract", "convert", "calculate"
    };

    public PromptOptimizationService(ILogger<PromptOptimizationService> logger)
    {
        _logger = logger;
    }

    #region 5.31 Context Engineering Prompt 优化

    /// <summary>
    /// 分析 Prompt 质量
    /// 原理：150-300 词最佳，3000 token 后性能下降
    /// </summary>
    public PromptAnalysisResult AnalyzePrompt(string prompt, string language = "zh")
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new PromptAnalysisResult
            {
                Score = 0,
                Suggestions = new() { "Prompt 不能为空" }
            };
        }

        // 计算词数和 Token
        int wordCount = language == "zh"
            ? prompt.Length
            : prompt.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        int estimatedTokens = EstimateTokens(prompt, language);

        // 长度分析
        var (lengthStatus, lengthScore) = AnalyzeLength(wordCount, estimatedTokens);

        // 结构分析
        var structure = AnalyzeStructure(prompt, language);

        // 清晰度分析
        var clarity = AnalyzeClarity(prompt, language);

        // 计算综合评分
        double score = (lengthScore * 0.3) + (structure.CompletenessScore * 0.4) + (clarity.Score * 0.3);

        // 生成建议
        var suggestions = GenerateSuggestions(lengthStatus, structure, clarity, estimatedTokens);

        // 模型特定建议
        var modelSuggestions = GenerateModelSuggestions(prompt, wordCount, language);

        return new PromptAnalysisResult
        {
            Score = Math.Round(score, 1),
            WordCount = wordCount,
            EstimatedTokens = estimatedTokens,
            LengthStatus = lengthStatus,
            LengthScore = lengthScore,
            Structure = structure,
            Clarity = clarity,
            ModelSuggestions = modelSuggestions,
            Suggestions = suggestions
        };
    }

    private int EstimateTokens(string text, string language)
    {
        // 粗略估算：中文约 1.5 字符/token，英文约 4 字符/token
        if (language == "zh")
        {
            int chineseChars = Regex.Matches(text, @"[\u4e00-\u9fff]").Count;
            int otherChars = text.Length - chineseChars;
            return (int)(chineseChars / 1.5 + otherChars / 4);
        }
        return text.Length / 4;
    }

    private (string Status, double Score) AnalyzeLength(int wordCount, int tokens)
    {
        // 150-300 词最佳
        if (wordCount >= 150 && wordCount <= 300)
            return ("optimal", 10);
        
        if (wordCount < 80)
            return ("short", 5);
        
        if (wordCount < 150)
            return ("short", 7);
        
        if (wordCount <= 500)
            return ("long", 7);
        
        // 3000 token 后性能下降
        if (tokens > 3000)
            return ("excessive", 4);
        
        return ("long", 5);
    }

    private PromptStructureAnalysis AnalyzeStructure(string prompt, string language)
    {
        var analysis = new PromptStructureAnalysis();

        // 检测角色定义
        analysis.HasRoleDefinition = Regex.IsMatch(prompt, 
            @"(你是|作为|扮演|角色|You are|As a|Act as|Role)", RegexOptions.IgnoreCase);

        // 检测任务描述
        analysis.HasTaskDescription = Regex.IsMatch(prompt,
            @"(请|需要|任务|目标|Please|Task|Goal|Objective)", RegexOptions.IgnoreCase);

        // 检测输出格式
        analysis.HasOutputFormat = Regex.IsMatch(prompt,
            @"(格式|输出|返回|Format|Output|Return|JSON|Markdown|XML)", RegexOptions.IgnoreCase);

        // 检测约束条件
        analysis.HasConstraints = Regex.IsMatch(prompt,
            @"(不要|避免|必须|限制|Don't|Avoid|Must|Constraint|Limit)", RegexOptions.IgnoreCase);

        // 检测示例
        analysis.HasExamples = Regex.IsMatch(prompt,
            @"(例如|示例|比如|Example|For instance|e\.g\.)", RegexOptions.IgnoreCase);

        // 计算完整度评分
        int components = 0;
        if (analysis.HasRoleDefinition) components++;
        if (analysis.HasTaskDescription) components++;
        if (analysis.HasOutputFormat) components++;
        if (analysis.HasConstraints) components++;
        if (analysis.HasExamples) components++;

        analysis.CompletenessScore = (components / 5.0) * 10;

        return analysis;
    }

    private PromptClarityAnalysis AnalyzeClarity(string prompt, string language)
    {
        var analysis = new PromptClarityAnalysis();

        // 检测模糊词
        var foundAmbiguous = new List<string>();
        foreach (var word in AmbiguousWords)
        {
            if (prompt.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                foundAmbiguous.Add(word);
            }
        }
        analysis.AmbiguousWords = foundAmbiguous;
        analysis.AmbiguousWordCount = foundAmbiguous.Count;

        // 检测指令动词
        analysis.HasClearActionVerbs = ActionVerbs.Any(v => 
            prompt.Contains(v, StringComparison.OrdinalIgnoreCase));

        // 计算平均句子长度
        var sentences = Regex.Split(prompt, @"[。！？.!?]")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
        
        if (sentences.Length > 0)
        {
            analysis.AverageSentenceLength = sentences.Average(s => s.Length);
        }

        // 计算清晰度评分
        double score = 10;
        score -= foundAmbiguous.Count * 0.5;
        if (!analysis.HasClearActionVerbs) score -= 2;
        if (analysis.AverageSentenceLength > 50) score -= 1;
        if (analysis.AverageSentenceLength > 80) score -= 1;

        analysis.Score = Math.Max(0, Math.Min(10, score));

        return analysis;
    }

    private List<string> GenerateSuggestions(
        string lengthStatus, 
        PromptStructureAnalysis structure, 
        PromptClarityAnalysis clarity,
        int tokens)
    {
        var suggestions = new List<string>();

        // 长度建议
        switch (lengthStatus)
        {
            case "short":
                suggestions.Add("Prompt 较短，建议扩展到 150-300 词以提供更多上下文");
                break;
            case "long":
                suggestions.Add("Prompt 较长，建议精简到 300 词以内以保持聚焦");
                break;
            case "excessive":
                suggestions.Add($"⚠️ Token 数 ({tokens}) 超过 3000，性能可能下降，建议大幅精简");
                break;
        }

        // 结构建议
        if (!structure.HasRoleDefinition)
            suggestions.Add("建议添加角色定义（如：你是一个专业的...）");
        if (!structure.HasTaskDescription)
            suggestions.Add("建议明确任务描述（如：请分析/总结/生成...）");
        if (!structure.HasOutputFormat)
            suggestions.Add("建议指定输出格式（如：以 JSON/Markdown/列表形式输出）");
        if (!structure.HasConstraints)
            suggestions.Add("建议添加约束条件（如：不要包含/必须包含...）");

        // 清晰度建议
        if (clarity.AmbiguousWordCount > 0)
        {
            suggestions.Add($"检测到 {clarity.AmbiguousWordCount} 个模糊词，建议使用更具体的表述");
        }
        if (!clarity.HasClearActionVerbs)
        {
            suggestions.Add("建议使用明确的指令动词（如：分析、总结、列出、比较）");
        }
        if (clarity.AverageSentenceLength > 50)
        {
            suggestions.Add("句子较长，建议拆分为更短的指令");
        }

        return suggestions;
    }

    #endregion

    #region 5.32 模型特定 Prompt 策略

    /// <summary>
    /// 生成模型特定建议
    /// </summary>
    private List<ModelSpecificSuggestion> GenerateModelSuggestions(string prompt, int wordCount, string language)
    {
        var suggestions = new List<ModelSpecificSuggestion>();

        foreach (var (family, config) in ModelConfigs)
        {
            var modelSuggestion = new ModelSpecificSuggestion
            {
                ModelName = config.ModelName,
                ModelFamily = family,
                RecommendedFormat = config.PreferredFormat,
                Suggestions = new List<string>()
            };

            // 长度建议
            if (wordCount < config.OptimalWordRange.Min)
            {
                modelSuggestion.Suggestions.Add(
                    $"对于 {config.ModelName}，建议 Prompt 至少 {config.OptimalWordRange.Min} 词");
            }
            else if (wordCount > config.OptimalWordRange.Max)
            {
                modelSuggestion.Suggestions.Add(
                    $"对于 {config.ModelName}，建议 Prompt 不超过 {config.OptimalWordRange.Max} 词");
            }

            // 格式建议
            if (config.SupportsXmlTags && !prompt.Contains("<"))
            {
                modelSuggestion.Suggestions.Add("建议使用 XML 标签组织结构");
            }

            // CoT 建议
            if (config.RequiresCoT && !Regex.IsMatch(prompt, @"(步骤|step|think|reasoning|分析过程)", RegexOptions.IgnoreCase))
            {
                modelSuggestion.Suggestions.Add("建议添加 Chain of Thought 引导");
            }

            // 添加特殊说明
            modelSuggestion.Suggestions.AddRange(config.SpecialNotes);

            // 生成优化后的 Prompt
            modelSuggestion.OptimizedPrompt = OptimizeForModel(prompt, family, language);

            suggestions.Add(modelSuggestion);
        }

        return suggestions;
    }

    /// <summary>
    /// 为特定模型优化 Prompt
    /// </summary>
    public string OptimizeForModel(string prompt, string modelFamily, string language = "zh")
    {
        if (!ModelConfigs.TryGetValue(modelFamily.ToLower(), out var config))
        {
            return prompt;
        }

        return modelFamily.ToLower() switch
        {
            "claude" => OptimizeForClaude(prompt, language),
            "gpt" => OptimizeForGpt(prompt, language),
            "gemini" => OptimizeForGemini(prompt, language),
            "llama" => OptimizeForLlama(prompt, language),
            _ => prompt
        };
    }

    private string OptimizeForClaude(string prompt, string language)
    {
        // Claude 偏好 XML 标签
        if (prompt.Contains("<"))
            return prompt;

        var parts = new List<string>();
        
        // 检测并包装角色
        var roleMatch = Regex.Match(prompt, @"(你是|作为|You are|As a)[^。.!！？?]+[。.!！？?]?");
        if (roleMatch.Success)
        {
            parts.Add($"<role>\n{roleMatch.Value.Trim()}\n</role>");
            prompt = prompt.Replace(roleMatch.Value, "").Trim();
        }

        // 检测并包装任务
        var taskMatch = Regex.Match(prompt, @"(请|需要|Please|Task)[^。.!！？?]+[。.!！？?]?");
        if (taskMatch.Success)
        {
            parts.Add($"<task>\n{taskMatch.Value.Trim()}\n</task>");
            prompt = prompt.Replace(taskMatch.Value, "").Trim();
        }

        // 剩余内容作为上下文
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            parts.Add($"<context>\n{prompt.Trim()}\n</context>");
        }

        return string.Join("\n\n", parts);
    }

    private string OptimizeForGpt(string prompt, string language)
    {
        // GPT 偏好 Markdown 格式
        var lines = new List<string>();

        // 检测角色
        if (Regex.IsMatch(prompt, @"(你是|作为|You are|As a)", RegexOptions.IgnoreCase))
        {
            lines.Add("## Role");
            var roleMatch = Regex.Match(prompt, @"(你是|作为|You are|As a)[^。.!！？?]+[。.!！？?]?");
            if (roleMatch.Success)
            {
                lines.Add(roleMatch.Value.Trim());
                prompt = prompt.Replace(roleMatch.Value, "").Trim();
            }
            lines.Add("");
        }

        // 添加任务
        lines.Add("## Task");
        lines.Add(prompt.Trim());

        return string.Join("\n", lines);
    }

    private string OptimizeForGemini(string prompt, string language)
    {
        // Gemini 偏好简短直接
        // 移除冗余词
        var optimized = Regex.Replace(prompt, @"(请注意|需要注意的是|值得一提的是)", "");
        optimized = Regex.Replace(optimized, @"\s+", " ").Trim();
        
        return optimized;
    }

    private string OptimizeForLlama(string prompt, string language)
    {
        // Llama 使用分隔符
        var parts = new List<string>();

        parts.Add("### Instruction");
        parts.Add(prompt.Trim());
        parts.Add("");
        parts.Add("### Response");

        return string.Join("\n", parts);
    }

    /// <summary>
    /// 获取模型配置
    /// </summary>
    public ModelPromptConfig? GetModelConfig(string modelFamily)
    {
        return ModelConfigs.TryGetValue(modelFamily.ToLower(), out var config) ? config : null;
    }

    /// <summary>
    /// 获取所有支持的模型
    /// </summary>
    public List<ModelPromptConfig> GetAllModelConfigs()
    {
        return ModelConfigs.Values.ToList();
    }

    #endregion
}
