using GeoCore.Data.Entities;
using GeoCore.Data.DbContext;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// Prompt 配置仓储
/// </summary>
public class PromptConfigRepository
{
    private readonly SqlSugarScope _db;

    public PromptConfigRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取 Prompt 配置（按分类和键名）
    /// v4.0: 多语言共用一个 Prompt 模板，通过 {{language}} 变量控制输出语言
    /// </summary>
    public async Task<PromptConfigEntity?> GetByKeyAsync(string category, string configKey)
    {
        return await _db.Queryable<PromptConfigEntity>()
            .Where(x => x.Category == category && x.ConfigKey == configKey && x.IsEnabled)
            .FirstAsync();
    }

    /// <summary>
    /// 获取分类下所有配置
    /// </summary>
    public async Task<List<PromptConfigEntity>> GetByCategoryAsync(string category)
    {
        return await _db.Queryable<PromptConfigEntity>()
            .Where(x => x.Category == category && x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public async Task<List<PromptConfigEntity>> GetAllAsync()
    {
        return await _db.Queryable<PromptConfigEntity>()
            .OrderBy(x => x.Category)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 创建配置
    /// </summary>
    public async Task<int> CreateAsync(PromptConfigEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新配置
    /// </summary>
    public async Task<bool> UpdateAsync(PromptConfigEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity)
            .IgnoreColumns(x => x.CreatedAt)
            .IgnoreColumns(x => x.CreatedBy)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<PromptConfigEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 系统参数配置仓储
/// </summary>
public class SystemConfigRepository
{
    private readonly SqlSugarScope _db;

    public SystemConfigRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    public async Task<string?> GetValueAsync(string category, string configKey)
    {
        var config = await _db.Queryable<SystemConfigEntity>()
            .Where(x => x.Category == category && x.ConfigKey == configKey)
            .FirstAsync();
        return config?.ConfigValue;
    }

    /// <summary>
    /// 获取整数配置值
    /// </summary>
    public async Task<int> GetIntValueAsync(string category, string configKey, int defaultValue = 0)
    {
        var value = await GetValueAsync(category, configKey);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取分类下所有配置
    /// </summary>
    public async Task<List<SystemConfigEntity>> GetByCategoryAsync(string category)
    {
        return await _db.Queryable<SystemConfigEntity>()
            .Where(x => x.Category == category)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public async Task<List<SystemConfigEntity>> GetAllAsync()
    {
        return await _db.Queryable<SystemConfigEntity>()
            .OrderBy(x => x.Category)
            .ToListAsync();
    }

    /// <summary>
    /// 设置配置值（存在则更新，不存在则创建）
    /// </summary>
    public async Task<bool> SetValueAsync(string category, string configKey, string value, string? name = null, string? description = null)
    {
        var existing = await _db.Queryable<SystemConfigEntity>()
            .Where(x => x.Category == category && x.ConfigKey == configKey)
            .FirstAsync();

        if (existing != null)
        {
            existing.ConfigValue = value;
            existing.UpdatedAt = DateTime.UtcNow;
            if (name != null) existing.Name = name;
            if (description != null) existing.Description = description;
            return await _db.Updateable(existing).ExecuteCommandHasChangeAsync();
        }
        else
        {
            var entity = new SystemConfigEntity
            {
                Category = category,
                ConfigKey = configKey,
                ConfigValue = value,
                Name = name ?? configKey,
                Description = description,
                UpdatedAt = DateTime.UtcNow
            };
            return await _db.Insertable(entity).ExecuteCommandAsync() > 0;
        }
    }
}

/// <summary>
/// 大模型配置仓储 (Storage Adapter 层)
/// </summary>
public class ModelConfigRepository
{
    private readonly SqlSugarScope _db;

    public ModelConfigRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取所有启用的模型配置
    /// </summary>
    public async Task<List<ModelConfigEntity>> GetAllEnabledAsync()
    {
        return await _db.Queryable<ModelConfigEntity>()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有模型配置（含禁用的）
    /// </summary>
    public async Task<List<ModelConfigEntity>> GetAllAsync()
    {
        return await _db.Queryable<ModelConfigEntity>()
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 按模型标识获取配置
    /// </summary>
    public async Task<ModelConfigEntity?> GetByModelIdAsync(string modelId)
    {
        return await _db.Queryable<ModelConfigEntity>()
            .Where(x => x.ModelId == modelId && x.IsEnabled)
            .FirstAsync();
    }

    /// <summary>
    /// 按主键获取
    /// </summary>
    public async Task<ModelConfigEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<ModelConfigEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建模型配置
    /// </summary>
    public async Task<int> CreateAsync(ModelConfigEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新模型配置
    /// </summary>
    public async Task<bool> UpdateAsync(ModelConfigEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity)
            .IgnoreColumns(x => x.CreatedAt)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除模型配置
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<ModelConfigEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 检查模型标识是否已存在
    /// </summary>
    public async Task<bool> ExistsByModelIdAsync(string modelId)
    {
        return await _db.Queryable<ModelConfigEntity>()
            .Where(x => x.ModelId == modelId)
            .AnyAsync();
    }
}
