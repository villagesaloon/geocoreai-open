using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;

namespace GeoCore.Data.Repositories;

/// <summary>
/// 来源平台仓储实现（系统级）
/// </summary>
public class SysSourcePlatformRepository : ISysSourcePlatformRepository
{
    private readonly GeoDbContext _dbContext;

    public SysSourcePlatformRepository(GeoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SysSourcePlatformDto>> GetAllPlatformsAsync()
    {
        var entities = await _dbContext.Client.Queryable<SysSourcePlatformEntity>()
            .Where(p => p.IsEnabled)
            .OrderByDescending(p => p.AuthorityBaseScore)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<SysSourcePlatformDto?> GetPlatformByDomainAsync(string domain)
    {
        var entity = await _dbContext.Client.Queryable<SysSourcePlatformEntity>()
            .Where(p => p.Domain == domain)
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<long> CreatePlatformAsync(SysSourcePlatformDto platform)
    {
        var entity = new SysSourcePlatformEntity
        {
            Domain = platform.Domain,
            Name = platform.Name,
            PlatformType = platform.PlatformType,
            Language = platform.Language,
            Region = platform.Region,
            AuthorityBaseScore = platform.AuthorityBaseScore,
            HasLoginSkill = platform.HasLoginSkill,
            HasPublishSkill = platform.HasPublishSkill,
            HasCommentSkill = platform.HasCommentSkill,
            HasCrawlSkill = platform.HasCrawlSkill,
            SkillConfig = platform.SkillConfig,
            Notes = platform.Notes,
            IsEnabled = platform.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };

        return await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<bool> UpdatePlatformAsync(SysSourcePlatformDto platform)
    {
        var entity = new SysSourcePlatformEntity
        {
            Id = platform.Id,
            Domain = platform.Domain,
            Name = platform.Name,
            PlatformType = platform.PlatformType,
            Language = platform.Language,
            Region = platform.Region,
            AuthorityBaseScore = platform.AuthorityBaseScore,
            HasLoginSkill = platform.HasLoginSkill,
            HasPublishSkill = platform.HasPublishSkill,
            HasCommentSkill = platform.HasCommentSkill,
            HasCrawlSkill = platform.HasCrawlSkill,
            SkillConfig = platform.SkillConfig,
            Notes = platform.Notes,
            IsEnabled = platform.IsEnabled,
            UpdatedAt = DateTime.UtcNow
        };

        return await _dbContext.Client.Updateable(entity)
            .IgnoreColumns(e => e.CreatedAt)
            .ExecuteCommandAsync() > 0;
    }

    private static SysSourcePlatformDto MapToDto(SysSourcePlatformEntity entity) => new()
    {
        Id = entity.Id,
        Domain = entity.Domain,
        Name = entity.Name,
        PlatformType = entity.PlatformType,
        Language = entity.Language,
        Region = entity.Region,
        AuthorityBaseScore = entity.AuthorityBaseScore,
        HasLoginSkill = entity.HasLoginSkill,
        HasPublishSkill = entity.HasPublishSkill,
        HasCommentSkill = entity.HasCommentSkill,
        HasCrawlSkill = entity.HasCrawlSkill,
        SkillConfig = entity.SkillConfig,
        Notes = entity.Notes,
        IsEnabled = entity.IsEnabled
    };
}
