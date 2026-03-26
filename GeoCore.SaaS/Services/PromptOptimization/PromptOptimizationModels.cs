namespace GeoCore.SaaS.Services.PromptOptimization;

#region 5.31 Context Engineering Prompt 优化

/// <summary>
/// Prompt 分析结果
/// </summary>
public class PromptAnalysisResult
{
    /// <summary>
    /// 综合评分 (0-10)
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 词数
    /// </summary>
    public int WordCount { get; set; }
    
    /// <summary>
    /// Token 估算
    /// </summary>
    public int EstimatedTokens { get; set; }
    
    /// <summary>
    /// 长度状态：short, optimal, long, excessive
    /// </summary>
    public string LengthStatus { get; set; } = "";
    
    /// <summary>
    /// 长度评分
    /// </summary>
    public double LengthScore { get; set; }
    
    /// <summary>
    /// 结构分析
    /// </summary>
    public PromptStructureAnalysis Structure { get; set; } = new();
    
    /// <summary>
    /// 清晰度分析
    /// </summary>
    public PromptClarityAnalysis Clarity { get; set; } = new();
    
    /// <summary>
    /// 模型特定建议
    /// </summary>
    public List<ModelSpecificSuggestion> ModelSuggestions { get; set; } = new();
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 优化后的 Prompt（如果需要）
    /// </summary>
    public string? OptimizedPrompt { get; set; }
}

/// <summary>
/// Prompt 结构分析
/// </summary>
public class PromptStructureAnalysis
{
    /// <summary>
    /// 是否有明确角色定义
    /// </summary>
    public bool HasRoleDefinition { get; set; }
    
    /// <summary>
    /// 是否有任务描述
    /// </summary>
    public bool HasTaskDescription { get; set; }
    
    /// <summary>
    /// 是否有输出格式要求
    /// </summary>
    public bool HasOutputFormat { get; set; }
    
    /// <summary>
    /// 是否有约束条件
    /// </summary>
    public bool HasConstraints { get; set; }
    
    /// <summary>
    /// 是否有示例
    /// </summary>
    public bool HasExamples { get; set; }
    
    /// <summary>
    /// 结构完整度评分
    /// </summary>
    public double CompletenessScore { get; set; }
}

/// <summary>
/// Prompt 清晰度分析
/// </summary>
public class PromptClarityAnalysis
{
    /// <summary>
    /// 清晰度评分
    /// </summary>
    public double Score { get; set; }
    
    /// <summary>
    /// 模糊词数量
    /// </summary>
    public int AmbiguousWordCount { get; set; }
    
    /// <summary>
    /// 检测到的模糊词
    /// </summary>
    public List<string> AmbiguousWords { get; set; } = new();
    
    /// <summary>
    /// 平均句子长度
    /// </summary>
    public double AverageSentenceLength { get; set; }
    
    /// <summary>
    /// 是否有明确指令动词
    /// </summary>
    public bool HasClearActionVerbs { get; set; }
}

#endregion

#region 5.32 模型特定 Prompt 策略

/// <summary>
/// 模型特定建议
/// </summary>
public class ModelSpecificSuggestion
{
    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = "";
    
    /// <summary>
    /// 模型系列：claude, gpt, gemini, llama
    /// </summary>
    public string ModelFamily { get; set; } = "";
    
    /// <summary>
    /// 优化建议
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
    
    /// <summary>
    /// 推荐的 Prompt 格式
    /// </summary>
    public string RecommendedFormat { get; set; } = "";
    
    /// <summary>
    /// 优化后的 Prompt
    /// </summary>
    public string? OptimizedPrompt { get; set; }
}

/// <summary>
/// 模型配置
/// </summary>
public class ModelPromptConfig
{
    /// <summary>
    /// 模型系列
    /// </summary>
    public string ModelFamily { get; set; } = "";
    
    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = "";
    
    /// <summary>
    /// 推荐的 Prompt 格式
    /// </summary>
    public string PreferredFormat { get; set; } = "";
    
    /// <summary>
    /// 最佳 Prompt 长度范围（词）
    /// </summary>
    public (int Min, int Max) OptimalWordRange { get; set; }
    
    /// <summary>
    /// 最大 Token 限制
    /// </summary>
    public int MaxTokens { get; set; }
    
    /// <summary>
    /// 是否支持 XML 标签
    /// </summary>
    public bool SupportsXmlTags { get; set; }
    
    /// <summary>
    /// 是否需要 CoT（Chain of Thought）
    /// </summary>
    public bool RequiresCoT { get; set; }
    
    /// <summary>
    /// 特殊说明
    /// </summary>
    public List<string> SpecialNotes { get; set; } = new();
}

/// <summary>
/// Prompt 模板
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// 模板 ID
    /// </summary>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// 模板名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 模板描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 适用场景
    /// </summary>
    public List<string> UseCases { get; set; } = new();
    
    /// <summary>
    /// 模板内容
    /// </summary>
    public string Template { get; set; } = "";
    
    /// <summary>
    /// 适用的模型系列
    /// </summary>
    public List<string> SupportedModels { get; set; } = new();
    
    /// <summary>
    /// 变量列表
    /// </summary>
    public List<TemplateVariable> Variables { get; set; } = new();
}

/// <summary>
/// 模板变量
/// </summary>
public class TemplateVariable
{
    /// <summary>
    /// 变量名
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 变量描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }
    
    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }
}

#endregion
