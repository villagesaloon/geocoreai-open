using System.Text.Json;
using GeoCore.Data.Entities;
using GeoCore.Data.DbContext;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 模型配置初始化器 - 从 config.json 迁移到数据库（仅首次运行时执行）
/// </summary>
public class ModelConfigInitializer
{
    private readonly GeoDbContext _dbContext;

    public ModelConfigInitializer(GeoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 从 config.json 初始化模型配置到数据库（如果数据库中已有配置则跳过）
    /// </summary>
    public async Task InitializeFromConfigJsonAsync()
    {
        var db = _dbContext.Client;

        // 检查是否已有模型配置
        var existingCount = await db.Queryable<ModelConfigEntity>().CountAsync();
        if (existingCount > 0)
        {
            Console.WriteLine("[ModelConfig] 已存在配置，跳过初始化");
            return;
        }

        // 查找 config.json 文件
        var cfgPath = Path.Combine(AppContext.BaseDirectory, "prompts", "config.json");
        if (!File.Exists(cfgPath))
            cfgPath = @"C:\Users\Administrator\source\GCore\prompts\config.json";

        if (!File.Exists(cfgPath))
        {
            Console.WriteLine("[ModelConfig] config.json 未找到，跳过迁移");
            return;
        }

        Console.WriteLine("[ModelConfig] 开始从 config.json 迁移模型配置...");

        var json = await File.ReadAllTextAsync(cfgPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("models", out var models))
        {
            Console.WriteLine("[ModelConfig] config.json 中未找到 models 节点");
            return;
        }

        var displayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["gpt"] = "GPT",
            ["claude"] = "Claude",
            ["gemini"] = "Gemini",
            ["grok"] = "Grok",
            ["perplexity"] = "Perplexity"
        };

        var sortOrder = 0;
        var entities = new List<ModelConfigEntity>();

        foreach (var model in models.EnumerateObject())
        {
            sortOrder++;
            var modelId = model.Name.ToLower();
            var config = model.Value;

            entities.Add(new ModelConfigEntity
            {
                ModelId = modelId,
                DisplayName = displayNames.GetValueOrDefault(modelId, modelId),
                ApiEndpoint = config.TryGetProperty("endpoint", out var ep) ? ep.GetString() ?? "" : "",
                ApiKey = config.TryGetProperty("api_key", out var ak) ? ak.GetString() ?? "" : "",
                ModelName = config.TryGetProperty("model", out var mn) ? mn.GetString() ?? "" : "",
                Temperature = 0.7,
                MaxTokens = 16384,
                IsEnabled = true,
                SortOrder = sortOrder,
                Description = $"从 config.json 迁移 ({DateTime.UtcNow:yyyy-MM-dd})",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (entities.Count > 0)
        {
            await db.Insertable(entities).ExecuteCommandAsync();
            Console.WriteLine($"[ModelConfig] 已迁移 {entities.Count} 个模型配置到数据库");
        }
    }
}
