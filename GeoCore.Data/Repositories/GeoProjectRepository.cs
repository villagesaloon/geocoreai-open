using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GeoCore.Data.DbContext;
using GeoCore.Data.Entities;
using GeoCore.Shared.Interfaces;
using GeoCore.Shared.Models;
using SqlSugar;

namespace GeoCore.Data.Repositories;

/// <summary>
/// GEO 项目仓储实现
/// </summary>
public class GeoProjectRepository : IGeoProjectRepository
{
    private readonly GeoDbContext _dbContext;

    public GeoProjectRepository(GeoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region 项目 CRUD

    public async Task<long> CreateProjectAsync(GeoProjectDto project)
    {
        var entity = new GeoProjectEntity
        {
            UserId = project.UserId,
            BrandName = project.BrandName,
            ProductName = project.ProductName,
            Industry = project.Industry,
            Description = project.Description,
            MonitorUrl = project.MonitorUrl,
            Status = project.Status,
            CreatedAt = DateTime.UtcNow
        };

        return await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<GeoProjectDto?> GetProjectByIdAsync(long projectId, long userId)
    {
        var entity = await _dbContext.Client.Queryable<GeoProjectEntity>()
            .Where(p => p.Id == projectId && p.UserId == userId)
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<GeoProjectDto>> GetProjectsByUserIdAsync(long userId)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectEntity>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<bool> UpdateProjectAsync(GeoProjectDto project)
    {
        var entity = new GeoProjectEntity
        {
            Id = project.Id,
            UserId = project.UserId,
            BrandName = project.BrandName,
            ProductName = project.ProductName,
            Industry = project.Industry,
            Description = project.Description,
            MonitorUrl = project.MonitorUrl,
            Status = project.Status,
            UpdatedAt = DateTime.UtcNow
        };

        return await _dbContext.Client.Updateable(entity)
            .IgnoreColumns(e => e.CreatedAt)
            .Where(e => e.Id == project.Id && e.UserId == project.UserId)
            .ExecuteCommandAsync() > 0;
    }

    public async Task<bool> DeleteProjectAsync(long projectId, long userId)
    {
        // 级联删除：先删除关联数据
        await _dbContext.Client.Deleteable<GeoQuestionSourceEntity>()
            .Where(s => SqlFunc.Subqueryable<GeoQuestionEntity>()
                .Where(q => q.Id == s.QuestionId && q.ProjectId == projectId && q.UserId == userId)
                .Any())
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoQuestionAnswerEntity>()
            .Where(a => SqlFunc.Subqueryable<GeoQuestionEntity>()
                .Where(q => q.Id == a.QuestionId && q.ProjectId == projectId && q.UserId == userId)
                .Any())
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoQuestionEntity>()
            .Where(q => q.ProjectId == projectId && q.UserId == userId)
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoProjectStageEntity>()
            .Where(s => s.ProjectId == projectId)
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoProjectPersonaEntity>()
            .Where(p => p.ProjectId == projectId)
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoProjectSellingPointEntity>()
            .Where(s => s.ProjectId == projectId)
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoProjectCompetitorEntity>()
            .Where(c => c.ProjectId == projectId)
            .ExecuteCommandAsync();

        await _dbContext.Client.Deleteable<GeoProjectConfigEntity>()
            .Where(c => c.ProjectId == projectId)
            .ExecuteCommandAsync();

        return await _dbContext.Client.Deleteable<GeoProjectEntity>()
            .Where(p => p.Id == projectId && p.UserId == userId)
            .ExecuteCommandAsync() > 0;
    }

    #endregion

    #region 项目配置

    public async Task<bool> SaveProjectConfigAsync(long projectId, GeoProjectConfigDto config)
    {
        var entity = new GeoProjectConfigEntity
        {
            ProjectId = projectId,
            // 直接使用 Countries（新版），如果为空则尝试 Markets（旧版向后兼容）
            Markets = (config.Countries != null && config.Countries.Count > 0) 
                ? JsonSerializer.Serialize(config.Countries) 
                : (config.Markets != null && config.Markets.Count > 0 ? JsonSerializer.Serialize(config.Markets) : null),
            Languages = config.Languages != null ? JsonSerializer.Serialize(config.Languages) : null,
            Models = config.Models != null ? JsonSerializer.Serialize(config.Models) : null,
            AnswerMode = config.AnswerMode,
            EnableGoogleTrends = config.EnableGoogleTrends,
            EnableRedditSearch = config.EnableRedditSearch,
            EnableLightweightMode = config.EnableLightweightMode,
            QuestionsPerModel = config.QuestionsPerModel,
            CoreNeeds = config.CoreNeeds != null ? JsonSerializer.Serialize(config.CoreNeeds) : null,
            CreatedAt = DateTime.UtcNow
        };

        // 先删除旧配置
        await _dbContext.Client.Deleteable<GeoProjectConfigEntity>()
            .Where(c => c.ProjectId == projectId)
            .ExecuteCommandAsync();

        return await _dbContext.Client.Insertable(entity).ExecuteCommandAsync() > 0;
    }

    public async Task<GeoProjectConfigDto?> GetProjectConfigAsync(long projectId)
    {
        var entity = await _dbContext.Client.Queryable<GeoProjectConfigEntity>()
            .Where(c => c.ProjectId == projectId)
            .FirstAsync();

        return entity == null ? null : MapConfigToDto(entity);
    }

    #endregion

    #region 竞品

    public async Task<bool> SaveCompetitorsAsync(long projectId, List<GeoCompetitorDto> competitors)
    {
        // 先删除旧数据
        await _dbContext.Client.Deleteable<GeoProjectCompetitorEntity>()
            .Where(c => c.ProjectId == projectId)
            .ExecuteCommandAsync();

        if (competitors.Count == 0) return true;

        var entities = competitors.Select((c, i) => new GeoProjectCompetitorEntity
        {
            ProjectId = projectId,
            Name = c.Name,
            Url = c.Url,
            FocusPoints = c.FocusPoints != null ? JsonSerializer.Serialize(c.FocusPoints) : null,
            SortOrder = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        return await _dbContext.Client.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    public async Task<List<GeoCompetitorDto>> GetCompetitorsAsync(long projectId)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectCompetitorEntity>()
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        return entities.Select(MapCompetitorToDto).ToList();
    }

    #endregion

    #region 卖点

    public async Task<bool> SaveSellingPointsAsync(long projectId, List<GeoSellingPointDto> sellingPoints)
    {
        // 删除项目所有卖点（用于全量替换场景）
        await _dbContext.Client.Deleteable<GeoProjectSellingPointEntity>()
            .Where(s => s.ProjectId == projectId)
            .ExecuteCommandAsync();

        if (sellingPoints.Count == 0) return true;

        var entities = sellingPoints.Select((s, i) => new GeoProjectSellingPointEntity
        {
            ProjectId = projectId,
            Country = s.Country,
            Language = s.Language,
            Point = s.Point,
            UsageDesc = s.UsageDesc,
            Weight = s.Weight,
            IsSelected = s.IsSelected,
            SortOrder = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        return await _dbContext.Client.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 按国家/语言保存卖点（只删除该国家/语言的卖点，保留其他）
    /// </summary>
    public async Task<bool> SaveSellingPointsByCountryLanguageAsync(long projectId, string country, string language, List<GeoSellingPointDto> sellingPoints)
    {
        // 只删除该国家/语言的卖点
        await _dbContext.Client.Deleteable<GeoProjectSellingPointEntity>()
            .Where(s => s.ProjectId == projectId && s.Country == country && s.Language == language)
            .ExecuteCommandAsync();

        if (sellingPoints.Count == 0) return true;

        var entities = sellingPoints.Select((s, i) => new GeoProjectSellingPointEntity
        {
            ProjectId = projectId,
            Country = country,
            Language = language,
            Point = s.Point,
            UsageDesc = s.UsageDesc,
            Weight = s.Weight,
            IsSelected = s.IsSelected,
            SortOrder = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        return await _dbContext.Client.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    public async Task<List<GeoSellingPointDto>> GetSellingPointsAsync(long projectId)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectSellingPointEntity>()
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Country)
            .OrderBy(s => s.Language)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return entities.Select(MapSellingPointToDto).ToList();
    }

    /// <summary>
    /// 按国家/语言获取卖点
    /// </summary>
    public async Task<List<GeoSellingPointDto>> GetSellingPointsByCountryLanguageAsync(long projectId, string country, string language)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectSellingPointEntity>()
            .Where(s => s.ProjectId == projectId && s.Country == country && s.Language == language)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return entities.Select(MapSellingPointToDto).ToList();
    }

    #endregion

    #region 画像

    public async Task<bool> SavePersonasAsync(long projectId, List<GeoPersonaDto> personas)
    {
        // 删除项目所有画像（用于全量替换场景）
        await _dbContext.Client.Deleteable<GeoProjectPersonaEntity>()
            .Where(p => p.ProjectId == projectId)
            .ExecuteCommandAsync();

        if (personas.Count == 0) return true;

        var entities = personas.Select((p, i) => new GeoProjectPersonaEntity
        {
            ProjectId = projectId,
            Country = p.Country,
            Language = p.Language,
            Name = p.Name,
            Description = p.Description,
            IsSelected = p.IsSelected,
            SortOrder = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        return await _dbContext.Client.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 按国家/语言保存画像（只删除该国家/语言的画像，保留其他）
    /// </summary>
    public async Task<bool> SavePersonasByCountryLanguageAsync(long projectId, string country, string language, List<GeoPersonaDto> personas)
    {
        // 只删除该国家/语言的画像
        await _dbContext.Client.Deleteable<GeoProjectPersonaEntity>()
            .Where(p => p.ProjectId == projectId && p.Country == country && p.Language == language)
            .ExecuteCommandAsync();

        if (personas.Count == 0) return true;

        var entities = personas.Select((p, i) => new GeoProjectPersonaEntity
        {
            ProjectId = projectId,
            Country = country,
            Language = language,
            Name = p.Name,
            Description = p.Description,
            IsSelected = p.IsSelected,
            SortOrder = i,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        return await _dbContext.Client.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    public async Task<List<GeoPersonaDto>> GetPersonasAsync(long projectId)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectPersonaEntity>()
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.Country)
            .OrderBy(p => p.Language)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return entities.Select(MapPersonaToDto).ToList();
    }

    /// <summary>
    /// 按国家/语言获取画像
    /// </summary>
    public async Task<List<GeoPersonaDto>> GetPersonasByCountryLanguageAsync(long projectId, string country, string language)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectPersonaEntity>()
            .Where(p => p.ProjectId == projectId && p.Country == country && p.Language == language)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        return entities.Select(MapPersonaToDto).ToList();
    }

    #endregion

    #region 阶段

    public async Task<bool> SaveStagesAsync(long projectId, List<GeoStageDto> stages)
    {
        await _dbContext.Client.Deleteable<GeoProjectStageEntity>()
            .Where(s => s.ProjectId == projectId)
            .ExecuteCommandAsync();

        if (stages.Count == 0) return true;

        var entities = stages.Select(s => new GeoProjectStageEntity
        {
            ProjectId = projectId,
            StageKey = s.StageKey,
            StageName = s.StageName,
            IsSelected = s.IsSelected,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        return await _dbContext.Client.Insertable(entities).ExecuteCommandAsync() > 0;
    }

    public async Task<List<GeoStageDto>> GetStagesAsync(long projectId)
    {
        var entities = await _dbContext.Client.Queryable<GeoProjectStageEntity>()
            .Where(s => s.ProjectId == projectId)
            .ToListAsync();

        return entities.Select(MapStageToDto).ToList();
    }

    #endregion

    #region Mapping

    private static GeoProjectDto MapToDto(GeoProjectEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        BrandName = entity.BrandName,
        ProductName = entity.ProductName,
        Industry = entity.Industry,
        Description = entity.Description,
        MonitorUrl = entity.MonitorUrl,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static GeoProjectConfigDto MapConfigToDto(GeoProjectConfigEntity entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        Markets = TryDeserializeList(entity.Markets),
        Languages = TryDeserializeList(entity.Languages),
        Models = TryDeserializeList(entity.Models),
        AnswerMode = entity.AnswerMode,
        EnableGoogleTrends = entity.EnableGoogleTrends,
        EnableRedditSearch = entity.EnableRedditSearch,
        EnableLightweightMode = entity.EnableLightweightMode,
        QuestionsPerModel = entity.QuestionsPerModel,
        CoreNeeds = TryDeserializeList(entity.CoreNeeds)
    };

    private static GeoCompetitorDto MapCompetitorToDto(GeoProjectCompetitorEntity entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        Name = entity.Name,
        Url = entity.Url,
        FocusPoints = TryDeserializeList(entity.FocusPoints),
        SortOrder = entity.SortOrder
    };

    private static GeoSellingPointDto MapSellingPointToDto(GeoProjectSellingPointEntity entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        Country = entity.Country,
        Language = entity.Language,
        Point = entity.Point,
        UsageDesc = entity.UsageDesc,
        Weight = entity.Weight,
        IsSelected = entity.IsSelected,
        SortOrder = entity.SortOrder
    };

    private static GeoPersonaDto MapPersonaToDto(GeoProjectPersonaEntity entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        Country = entity.Country,
        Language = entity.Language,
        Name = entity.Name,
        Description = entity.Description,
        IsSelected = entity.IsSelected,
        SortOrder = entity.SortOrder
    };

    private static GeoStageDto MapStageToDto(GeoProjectStageEntity entity) => new()
    {
        Id = entity.Id,
        ProjectId = entity.ProjectId,
        StageKey = entity.StageKey,
        StageName = entity.StageName,
        IsSelected = entity.IsSelected
    };

    private static List<string>? TryDeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
