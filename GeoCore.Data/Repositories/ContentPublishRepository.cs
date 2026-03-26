using GeoCore.Data.Entities;
using GeoCore.Data.DbContext;
using SqlSugar;

namespace GeoCore.Data.Repositories;

#region 内容模板仓储（Admin 配置）

/// <summary>
/// 内容模板仓储 (Storage Adapter 层)
/// Phase 8.1: 内容模板管理
/// </summary>
public class ContentTemplateRepository
{
    private readonly SqlSugarScope _db;

    public ContentTemplateRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取所有模板
    /// </summary>
    public async Task<List<ContentTemplateEntity>> GetAllAsync()
    {
        return await _db.Queryable<ContentTemplateEntity>()
            .OrderBy(x => x.Platform)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 获取启用的模板
    /// </summary>
    public async Task<List<ContentTemplateEntity>> GetActiveAsync()
    {
        return await _db.Queryable<ContentTemplateEntity>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Platform)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 按平台获取模板
    /// </summary>
    public async Task<List<ContentTemplateEntity>> GetByPlatformAsync(string platform)
    {
        return await _db.Queryable<ContentTemplateEntity>()
            .Where(x => x.Platform == platform && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<ContentTemplateEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<ContentTemplateEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建模板
    /// </summary>
    public async Task<int> CreateAsync(ContentTemplateEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新模板
    /// </summary>
    public async Task<bool> UpdateAsync(ContentTemplateEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity)
            .IgnoreColumns(x => x.CreatedAt)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除模板
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<ContentTemplateEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 平台内容规则仓储 (Storage Adapter 层)
/// Phase 8.2: 平台内容规则
/// </summary>
public class PlatformContentRuleRepository
{
    private readonly SqlSugarScope _db;

    public PlatformContentRuleRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取所有规则
    /// </summary>
    public async Task<List<PlatformContentRuleEntity>> GetAllAsync()
    {
        return await _db.Queryable<PlatformContentRuleEntity>()
            .OrderBy(x => x.Platform)
            .ToListAsync();
    }

    /// <summary>
    /// 按平台获取规则
    /// </summary>
    public async Task<List<PlatformContentRuleEntity>> GetByPlatformAsync(string platform)
    {
        return await _db.Queryable<PlatformContentRuleEntity>()
            .Where(x => x.Platform == platform && x.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<PlatformContentRuleEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<PlatformContentRuleEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建规则
    /// </summary>
    public async Task<int> CreateAsync(PlatformContentRuleEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    public async Task<bool> UpdateAsync(PlatformContentRuleEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<PlatformContentRuleEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

#endregion

#region 发布平台配置仓储（Admin 配置）

/// <summary>
/// 发布平台 App 配置仓储 (Storage Adapter 层)
/// Phase 8.4: 平台 App 配置
/// </summary>
public class PublishPlatformAppRepository
{
    private readonly SqlSugarScope _db;

    public PublishPlatformAppRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取所有 App 配置
    /// </summary>
    public async Task<List<PublishPlatformAppEntity>> GetAllAsync()
    {
        return await _db.Queryable<PublishPlatformAppEntity>()
            .OrderBy(x => x.Platform)
            .ToListAsync();
    }

    /// <summary>
    /// 获取启用的 App 配置
    /// </summary>
    public async Task<List<PublishPlatformAppEntity>> GetActiveAsync()
    {
        return await _db.Queryable<PublishPlatformAppEntity>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Platform)
            .ToListAsync();
    }

    /// <summary>
    /// 按平台获取 App 配置
    /// </summary>
    public async Task<PublishPlatformAppEntity?> GetByPlatformAsync(string platform)
    {
        return await _db.Queryable<PublishPlatformAppEntity>()
            .Where(x => x.Platform == platform && x.IsActive)
            .FirstAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<PublishPlatformAppEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<PublishPlatformAppEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建 App 配置
    /// </summary>
    public async Task<int> CreateAsync(PublishPlatformAppEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新 App 配置
    /// </summary>
    public async Task<bool> UpdateAsync(PublishPlatformAppEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity)
            .IgnoreColumns(x => x.CreatedAt)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除 App 配置
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<PublishPlatformAppEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 发布规则仓储 (Storage Adapter 层)
/// Phase 8.5: 发布规则配置
/// </summary>
public class PublishRuleRepository
{
    private readonly SqlSugarScope _db;

    public PublishRuleRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取所有规则
    /// </summary>
    public async Task<List<PublishRuleEntity>> GetAllAsync()
    {
        return await _db.Queryable<PublishRuleEntity>()
            .OrderBy(x => x.Platform)
            .ToListAsync();
    }

    /// <summary>
    /// 按平台获取规则
    /// </summary>
    public async Task<List<PublishRuleEntity>> GetByPlatformAsync(string platform)
    {
        return await _db.Queryable<PublishRuleEntity>()
            .Where(x => (x.Platform == platform || x.Platform == "all") && x.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<PublishRuleEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<PublishRuleEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建规则
    /// </summary>
    public async Task<int> CreateAsync(PublishRuleEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    public async Task<bool> UpdateAsync(PublishRuleEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity).ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除规则
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<PublishRuleEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

#endregion

#region 用户内容仓储（SaaS 用户数据）

/// <summary>
/// 用户平台账号仓储 (Storage Adapter 层)
/// Phase 8.11: 账号绑定
/// </summary>
public class UserPlatformAccountRepository
{
    private readonly SqlSugarScope _db;

    public UserPlatformAccountRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取用户的所有平台账号
    /// </summary>
    public async Task<List<UserPlatformAccountEntity>> GetByUserIdAsync(int userId)
    {
        return await _db.Queryable<UserPlatformAccountEntity>()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Platform)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户指定平台的账号
    /// </summary>
    public async Task<UserPlatformAccountEntity?> GetByUserAndPlatformAsync(int userId, string platform)
    {
        return await _db.Queryable<UserPlatformAccountEntity>()
            .Where(x => x.UserId == userId && x.Platform == platform)
            .FirstAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<UserPlatformAccountEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<UserPlatformAccountEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建账号绑定
    /// </summary>
    public async Task<int> CreateAsync(UserPlatformAccountEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新账号信息
    /// </summary>
    public async Task<bool> UpdateAsync(UserPlatformAccountEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity)
            .IgnoreColumns(x => x.CreatedAt)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除账号绑定
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<UserPlatformAccountEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 更新 Token
    /// </summary>
    public async Task<bool> UpdateTokenAsync(int id, string accessToken, string? refreshToken, DateTime? expiresAt)
    {
        return await _db.Updateable<UserPlatformAccountEntity>()
            .SetColumns(x => x.AccessToken == accessToken)
            .SetColumns(x => x.RefreshToken == refreshToken)
            .SetColumns(x => x.TokenExpiresAt == expiresAt)
            .SetColumns(x => x.UpdatedAt == DateTime.UtcNow)
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 内容草稿仓储 (Storage Adapter 层)
/// Phase 8.9: 草稿管理
/// </summary>
public class ContentDraftRepository
{
    private readonly SqlSugarScope _db;

    public ContentDraftRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取用户的所有草稿
    /// </summary>
    public async Task<List<ContentDraftEntity>> GetByUserIdAsync(int userId)
    {
        return await _db.Queryable<ContentDraftEntity>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取用户指定状态的草稿
    /// </summary>
    public async Task<List<ContentDraftEntity>> GetByUserAndStatusAsync(int userId, string status)
    {
        return await _db.Queryable<ContentDraftEntity>()
            .Where(x => x.UserId == userId && x.Status == status)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 获取项目的所有草稿
    /// </summary>
    public async Task<List<ContentDraftEntity>> GetByProjectIdAsync(int projectId)
    {
        return await _db.Queryable<ContentDraftEntity>()
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<ContentDraftEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<ContentDraftEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建草稿
    /// </summary>
    public async Task<int> CreateAsync(ContentDraftEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新草稿
    /// </summary>
    public async Task<bool> UpdateAsync(ContentDraftEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Updateable(entity)
            .IgnoreColumns(x => x.CreatedAt)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 更新草稿状态
    /// </summary>
    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        return await _db.Updateable<ContentDraftEntity>()
            .SetColumns(x => x.Status == status)
            .SetColumns(x => x.UpdatedAt == DateTime.UtcNow)
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 删除草稿
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        return await _db.Deleteable<ContentDraftEntity>()
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

/// <summary>
/// 发布历史仓储 (Storage Adapter 层)
/// Phase 8.13: 发布历史
/// </summary>
public class PublishHistoryRepository
{
    private readonly SqlSugarScope _db;

    public PublishHistoryRepository(GeoDbContext dbContext)
    {
        _db = dbContext.Client;
    }

    /// <summary>
    /// 获取用户的发布历史
    /// </summary>
    public async Task<List<PublishHistoryEntity>> GetByUserIdAsync(int userId, int limit = 50)
    {
        return await _db.Queryable<PublishHistoryEntity>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 获取草稿的发布历史
    /// </summary>
    public async Task<List<PublishHistoryEntity>> GetByDraftIdAsync(int draftId)
    {
        return await _db.Queryable<PublishHistoryEntity>()
            .Where(x => x.DraftId == draftId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 按 ID 获取
    /// </summary>
    public async Task<PublishHistoryEntity?> GetByIdAsync(int id)
    {
        return await _db.Queryable<PublishHistoryEntity>()
            .Where(x => x.Id == id)
            .FirstAsync();
    }

    /// <summary>
    /// 创建发布记录
    /// </summary>
    public async Task<int> CreateAsync(PublishHistoryEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        return await _db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 更新发布状态
    /// </summary>
    public async Task<bool> UpdateStatusAsync(int id, string status, string? platformPostId = null, string? platformUrl = null, string? errorMessage = null)
    {
        var updateable = _db.Updateable<PublishHistoryEntity>()
            .SetColumns(x => x.Status == status)
            .Where(x => x.Id == id);

        if (platformPostId != null)
            updateable = updateable.SetColumns(x => x.PlatformPostId == platformPostId);
        if (platformUrl != null)
            updateable = updateable.SetColumns(x => x.PlatformUrl == platformUrl);
        if (errorMessage != null)
            updateable = updateable.SetColumns(x => x.ErrorMessage == errorMessage);
        if (status == "success")
            updateable = updateable.SetColumns(x => x.PublishedAt == DateTime.UtcNow);

        return await updateable.ExecuteCommandHasChangeAsync();
    }

    /// <summary>
    /// 关联引用追踪任务
    /// </summary>
    public async Task<bool> LinkCitationTaskAsync(int id, int citationTaskId)
    {
        return await _db.Updateable<PublishHistoryEntity>()
            .SetColumns(x => x.CitationTaskId == citationTaskId)
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }
}

#endregion
