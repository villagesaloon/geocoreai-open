using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 管理员 Repository 实现 (Storage Adapter 层)
/// </summary>
public class AdminRepository : IAdminRepository
{
    private readonly GeoDbContext _db;

    public AdminRepository(GeoDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDto?> GetByIdAsync(long id)
    {
        var entity = await _db.Client.Queryable<AdminEntity>()
            .Where(x => x.Id == id && x.Status == 1)
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<AdminDto>> GetAllAsync()
    {
        var entities = await _db.Client.Queryable<AdminEntity>()
            .Where(x => x.Status == 1)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<(List<AdminDto> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize)
    {
        var total = new RefAsync<int>();
        var entities = await _db.Client.Queryable<AdminEntity>()
            .Where(x => x.Status == 1)
            .OrderByDescending(x => x.CreatedAt)
            .ToPageListAsync(pageIndex, pageSize, total);

        return (entities.Select(MapToDto).ToList(), total.Value);
    }

    public async Task<long> AddAsync(AdminDto dto)
    {
        var entity = MapToEntity(dto);
        entity.CreatedAt = DateTime.UtcNow;
        return await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    public async Task<int> AddRangeAsync(List<AdminDto> dtos)
    {
        var entities = dtos.Select(MapToEntity).ToList();
        foreach (var entity in entities)
        {
            entity.CreatedAt = DateTime.UtcNow;
        }
        return await _db.Client.Insertable(entities).ExecuteCommandAsync();
    }

    public async Task<bool> UpdateAsync(AdminDto dto)
    {
        var entity = MapToEntity(dto);
        entity.UpdatedAt = DateTime.UtcNow;
        return await _db.Client.Updateable(entity)
            .IgnoreColumns(x => new { x.PasswordHash, x.CreatedAt })
            .ExecuteCommandHasChangeAsync();
    }

    public async Task<bool> DeleteAsync(long id)
    {
        // 软删除：设置 status = 0
        return await _db.Client.Updateable<AdminEntity>()
            .SetColumns(x => x.Status == 0)
            .SetColumns(x => x.UpdatedAt == DateTime.UtcNow)
            .Where(x => x.Id == id)
            .ExecuteCommandHasChangeAsync();
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _db.Client.Queryable<AdminEntity>()
            .Where(x => x.Id == id && x.Status == 1)
            .AnyAsync();
    }

    public async Task<AdminDto?> GetByUsernameAsync(string username)
    {
        var entity = await _db.Client.Queryable<AdminEntity>()
            .Where(x => x.Username == username && x.Status == 1)
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<AdminDto?> ValidateAndLoginAsync(string username, string password)
    {
        var entity = await _db.Client.Queryable<AdminEntity>()
            .Where(x => x.Username == username && x.Status == 1)
            .FirstAsync();

        if (entity == null)
            return null;

        // 验证密码
        if (!BCrypt.Net.BCrypt.Verify(password, entity.PasswordHash))
            return null;

        // 更新登录信息
        await UpdateLastLoginAsync(entity.Id);

        entity.LastLoginAt = DateTime.UtcNow;
        entity.LoginCount++;

        return MapToDto(entity);
    }

    public async Task<long> CreateAdminAsync(AdminDto admin, string password)
    {
        var entity = MapToEntity(admin);
        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        entity.CreatedAt = DateTime.UtcNow;
        entity.LoginCount = 0;

        return await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    public async Task<bool> ChangePasswordAsync(long adminId, string newPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        return await _db.Client.Updateable<AdminEntity>()
            .SetColumns(x => x.PasswordHash == hash)
            .SetColumns(x => x.UpdatedAt == DateTime.UtcNow)
            .Where(x => x.Id == adminId)
            .ExecuteCommandHasChangeAsync();
    }

    public async Task<bool> UpdateLastLoginAsync(long adminId)
    {
        return await _db.Client.Updateable<AdminEntity>()
            .SetColumns(x => x.LastLoginAt == DateTime.UtcNow)
            .SetColumns(x => x.LoginCount == x.LoginCount + 1)
            .Where(x => x.Id == adminId)
            .ExecuteCommandHasChangeAsync();
    }

    private static AdminDto MapToDto(AdminEntity entity)
    {
        return new AdminDto
        {
            Id = entity.Id,
            Username = entity.Username,
            DisplayName = entity.DisplayName,
            Email = entity.Email,
            Role = entity.Role,
            Status = entity.Status,
            LastLoginAt = entity.LastLoginAt,
            LoginCount = entity.LoginCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static AdminEntity MapToEntity(AdminDto dto)
    {
        return new AdminEntity
        {
            Id = dto.Id,
            Username = dto.Username,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            Role = dto.Role,
            Status = dto.Status,
            LastLoginAt = dto.LastLoginAt,
            LoginCount = dto.LoginCount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}
