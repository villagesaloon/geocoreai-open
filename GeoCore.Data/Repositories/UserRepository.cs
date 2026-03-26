using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 用户 Repository 实现 (Storage Adapter 层)
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly GeoDbContext _db;

    public UserRepository(GeoDbContext db)
    {
        _db = db;
    }

    public async Task<UserDto?> GetByIdAsync(long id)
    {
        var entity = await _db.Client.Queryable<UserEntity>()
            .Where(u => u.Id == id)
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var entities = await _db.Client.Queryable<UserEntity>()
            .Where(u => u.Status != "deleted")
            .ToListAsync();

        return entities.ConvertAll(MapToDto);
    }

    public async Task<(List<UserDto> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize)
    {
        var total = new SqlSugar.RefAsync<int>();
        var entities = await _db.Client.Queryable<UserEntity>()
            .Where(u => u.Status != "deleted")
            .OrderByDescending(u => u.CreatedAt)
            .ToPageListAsync(pageIndex, pageSize, total);

        return (entities.ConvertAll(MapToDto), total.Value);
    }

    public async Task<long> AddAsync(UserDto dto)
    {
        var entity = MapToEntity(dto);
        entity.CreatedAt = DateTime.UtcNow;

        var id = await _db.Client.Insertable(entity).ExecuteReturnIdentityAsync();
        return id;
    }

    public async Task<int> AddRangeAsync(List<UserDto> dtos)
    {
        var entities = dtos.ConvertAll(MapToEntity);
        foreach (var e in entities) e.CreatedAt = DateTime.UtcNow;

        return await _db.Client.Insertable(entities).ExecuteCommandAsync();
    }

    public async Task<bool> UpdateAsync(UserDto dto)
    {
        var entity = MapToEntity(dto);
        entity.UpdatedAt = DateTime.UtcNow;

        var result = await _db.Client.Updateable(entity)
            .IgnoreColumns(u => u.CreatedAt)
            .ExecuteCommandAsync();

        return result > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        // 软删除：将状态设置为 deleted
        var result = await _db.Client.Updateable<UserEntity>()
            .SetColumns(u => u.Status == "deleted")
            .SetColumns(u => u.UpdatedAt == DateTime.UtcNow)
            .Where(u => u.Id == id)
            .ExecuteCommandAsync();

        return result > 0;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _db.Client.Queryable<UserEntity>()
            .Where(u => u.Id == id && u.Status != "deleted")
            .AnyAsync();
    }

    public async Task<UserDto?> GetByFirebaseUidAsync(string firebaseUid)
    {
        var entity = await _db.Client.Queryable<UserEntity>()
            .Where(u => u.FirebaseUid == firebaseUid && u.Status != "deleted")
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var entity = await _db.Client.Queryable<UserEntity>()
            .Where(u => u.Email == email && u.Status != "deleted")
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<UserDto> LoginOrRegisterAsync(UserDto user)
    {
        // 先查找是否存在
        var existing = await GetByFirebaseUidAsync(user.FirebaseUid);

        if (existing != null)
        {
            // 更新登录信息
            await UpdateLastLoginAsync(existing.Id);
            existing.LastLoginAt = DateTime.UtcNow;
            existing.LoginCount++;
            return existing;
        }

        // 创建新用户
        user.LastLoginAt = DateTime.UtcNow;
        user.LoginCount = 1;
        var id = await AddAsync(user);
        user.Id = id;
        return user;
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        await _db.Client.Updateable<UserEntity>()
            .SetColumns(u => u.LastLoginAt == DateTime.UtcNow)
            .SetColumns(u => u.LoginCount == u.LoginCount + 1)
            .Where(u => u.Id == userId)
            .ExecuteCommandAsync();
    }

    private static UserDto MapToDto(UserEntity entity)
    {
        return new UserDto
        {
            Id = entity.Id,
            FirebaseUid = entity.FirebaseUid,
            Email = entity.Email,
            DisplayName = entity.DisplayName,
            PhotoUrl = entity.PhotoUrl,
            Provider = entity.Provider,
            Company = entity.Company,
            Role = entity.Role,
            Status = entity.Status,
            LastLoginAt = entity.LastLoginAt,
            LoginCount = entity.LoginCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static UserEntity MapToEntity(UserDto dto)
    {
        return new UserEntity
        {
            Id = dto.Id,
            FirebaseUid = dto.FirebaseUid,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            PhotoUrl = dto.PhotoUrl,
            Provider = dto.Provider,
            Company = dto.Company,
            Role = dto.Role,
            Status = dto.Status,
            LastLoginAt = dto.LastLoginAt,
            LoginCount = dto.LoginCount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}
