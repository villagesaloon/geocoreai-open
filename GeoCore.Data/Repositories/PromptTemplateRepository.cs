using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;

namespace GeoCore.Data.Repositories;

/// <summary>
/// Prompt 模板仓储接口
/// </summary>
public interface IPromptTemplateRepository
{
    Task<List<PromptTemplateEntity>> GetAllAsync(bool enabledOnly = true);
    Task<List<PromptTemplateEntity>> GetByCategoryAsync(string category, bool enabledOnly = true);
    Task<PromptTemplateEntity?> GetByIdAsync(int id);
    Task<PromptTemplateEntity?> GetDefaultByCategoryAsync(string category);
    Task<int> CreateAsync(PromptTemplateEntity template);
    Task UpdateAsync(PromptTemplateEntity template);
    Task DeleteAsync(int id);
    Task<List<PromptTemplateVersionEntity>> GetVersionsAsync(int templateId);
    Task<PromptTemplateVersionEntity?> GetVersionAsync(int templateId, int version);
    Task<int> CreateVersionAsync(PromptTemplateVersionEntity version);
}

/// <summary>
/// Prompt 模板仓储实现
/// </summary>
public class PromptTemplateRepository : IPromptTemplateRepository
{
    private readonly GeoDbContext _context;

    public PromptTemplateRepository(GeoDbContext context)
    {
        _context = context;
    }

    public async Task<List<PromptTemplateEntity>> GetAllAsync(bool enabledOnly = true)
    {
        var query = _context.Client.Queryable<PromptTemplateEntity>();
        if (enabledOnly)
        {
            query = query.Where(t => t.IsEnabled);
        }
        return await query.OrderBy(t => t.Category).OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<List<PromptTemplateEntity>> GetByCategoryAsync(string category, bool enabledOnly = true)
    {
        var query = _context.Client.Queryable<PromptTemplateEntity>()
            .Where(t => t.Category == category);
        if (enabledOnly)
        {
            query = query.Where(t => t.IsEnabled);
        }
        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<PromptTemplateEntity?> GetByIdAsync(int id)
    {
        return await _context.Client.Queryable<PromptTemplateEntity>()
            .Where(t => t.Id == id)
            .FirstAsync();
    }

    public async Task<PromptTemplateEntity?> GetDefaultByCategoryAsync(string category)
    {
        return await _context.Client.Queryable<PromptTemplateEntity>()
            .Where(t => t.Category == category && t.IsDefault && t.IsEnabled)
            .FirstAsync();
    }

    public async Task<int> CreateAsync(PromptTemplateEntity template)
    {
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;
        return await _context.Client.Insertable(template).ExecuteReturnIdentityAsync();
    }

    public async Task UpdateAsync(PromptTemplateEntity template)
    {
        template.UpdatedAt = DateTime.UtcNow;
        await _context.Client.Updateable(template).ExecuteCommandAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await _context.Client.Deleteable<PromptTemplateEntity>()
            .Where(t => t.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task<List<PromptTemplateVersionEntity>> GetVersionsAsync(int templateId)
    {
        return await _context.Client.Queryable<PromptTemplateVersionEntity>()
            .Where(v => v.TemplateId == templateId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();
    }

    public async Task<PromptTemplateVersionEntity?> GetVersionAsync(int templateId, int version)
    {
        return await _context.Client.Queryable<PromptTemplateVersionEntity>()
            .Where(v => v.TemplateId == templateId && v.Version == version)
            .FirstAsync();
    }

    public async Task<int> CreateVersionAsync(PromptTemplateVersionEntity version)
    {
        version.CreatedAt = DateTime.UtcNow;
        return await _context.Client.Insertable(version).ExecuteReturnIdentityAsync();
    }
}
