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
/// GEO 问题仓储实现
/// </summary>
public class GeoQuestionRepository : IGeoQuestionRepository
{
    private readonly GeoDbContext _dbContext;

    public GeoQuestionRepository(GeoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region 问题

    public async Task<long> CreateQuestionAsync(GeoQuestionDto question)
    {
        var entity = MapToEntity(question);
        return await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<List<long>> CreateQuestionsAsync(List<GeoQuestionDto> questions)
    {
        if (questions.Count == 0) return new List<long>();

        var entities = questions.Select(MapToEntity).ToList();
        
        // SqlSugar 批量插入并返回 ID
        var ids = new List<long>();
        foreach (var entity in entities)
        {
            var id = await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
            ids.Add(id);
        }
        return ids;
    }

    public async Task<GeoQuestionDto?> GetQuestionByIdAsync(long questionId, long userId)
    {
        var entity = await _dbContext.Client.Queryable<GeoQuestionEntity>()
            .Where(q => q.Id == questionId && q.UserId == userId)
            .FirstAsync();

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<List<GeoQuestionDto>> GetQuestionsByProjectIdAsync(long projectId, long userId)
    {
        var entities = await _dbContext.Client.Queryable<GeoQuestionEntity>()
            .Where(q => q.ProjectId == projectId && q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<List<GeoQuestionDto>> GetQuestionsByTaskIdAsync(string taskId, long userId)
    {
        var entities = await _dbContext.Client.Queryable<GeoQuestionEntity>()
            .Where(q => q.TaskId == taskId && q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return entities.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteQuestionsByProjectIdAsync(long projectId, long userId)
    {
        // 先删除来源
        await _dbContext.Client.Deleteable<GeoQuestionSourceEntity>()
            .Where(s => SqlSugar.SqlFunc.Subqueryable<GeoQuestionEntity>()
                .Where(q => q.Id == s.QuestionId && q.ProjectId == projectId && q.UserId == userId)
                .Any())
            .ExecuteCommandAsync();

        // 再删除回答
        await _dbContext.Client.Deleteable<GeoQuestionAnswerEntity>()
            .Where(a => SqlSugar.SqlFunc.Subqueryable<GeoQuestionEntity>()
                .Where(q => q.Id == a.QuestionId && q.ProjectId == projectId && q.UserId == userId)
                .Any())
            .ExecuteCommandAsync();

        // 最后删除问题
        return await _dbContext.Client.Deleteable<GeoQuestionEntity>()
            .Where(q => q.ProjectId == projectId && q.UserId == userId)
            .ExecuteCommandAsync() > 0;
    }

    #endregion

    #region 回答

    public async Task<long> CreateAnswerAsync(GeoQuestionAnswerDto answer)
    {
        var entity = MapAnswerToEntity(answer);
        return await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<List<long>> CreateAnswersAsync(List<GeoQuestionAnswerDto> answers)
    {
        if (answers.Count == 0) return new List<long>();

        var ids = new List<long>();
        foreach (var answer in answers)
        {
            var entity = MapAnswerToEntity(answer);
            var id = await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
            ids.Add(id);
        }
        return ids;
    }

    public async Task<List<GeoQuestionAnswerDto>> GetAnswersByQuestionIdAsync(long questionId)
    {
        var entities = await _dbContext.Client.Queryable<GeoQuestionAnswerEntity>()
            .Where(a => a.QuestionId == questionId)
            .OrderBy(a => a.Model)
            .ToListAsync();

        return entities.Select(MapAnswerToDto).ToList();
    }

    #endregion

    #region 来源

    public async Task<long> CreateSourceAsync(GeoQuestionSourceDto source)
    {
        var entity = MapSourceToEntity(source);
        return await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    public async Task<List<long>> CreateSourcesAsync(List<GeoQuestionSourceDto> sources)
    {
        if (sources.Count == 0) return new List<long>();

        var ids = new List<long>();
        foreach (var source in sources)
        {
            var entity = MapSourceToEntity(source);
            var id = await _dbContext.Client.Insertable(entity).ExecuteReturnBigIdentityAsync();
            ids.Add(id);
        }
        return ids;
    }

    public async Task<List<GeoQuestionSourceDto>> GetSourcesByQuestionIdAsync(long questionId)
    {
        var entities = await _dbContext.Client.Queryable<GeoQuestionSourceEntity>()
            .Where(s => s.QuestionId == questionId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return entities.Select(MapSourceToDto).ToList();
    }

    public async Task<List<GeoQuestionSourceDto>> GetSourcesByAnswerIdAsync(long answerId)
    {
        var entities = await _dbContext.Client.Queryable<GeoQuestionSourceEntity>()
            .Where(s => s.AnswerId == answerId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return entities.Select(MapSourceToDto).ToList();
    }

    public async Task<bool> UpdateSourceContentStatusAsync(long sourceId, long userId, string status)
    {
        return await _dbContext.Client.Updateable<GeoQuestionSourceEntity>()
            .SetColumns(s => s.ContentStatus == status)
            .Where(s => s.Id == sourceId && s.UserId == userId)
            .ExecuteCommandAsync() > 0;
    }

    public async Task<bool> SetSourceAsTargetAsync(long sourceId, long userId, bool isTarget)
    {
        return await _dbContext.Client.Updateable<GeoQuestionSourceEntity>()
            .SetColumns(s => s.IsTargetForContent == isTarget)
            .Where(s => s.Id == sourceId && s.UserId == userId)
            .ExecuteCommandAsync() > 0;
    }

    #endregion

    #region Mapping

    private static GeoQuestionEntity MapToEntity(GeoQuestionDto dto) => new()
    {
        UserId = dto.UserId,
        ProjectId = dto.ProjectId,
        Country = dto.Country,
        TaskId = dto.TaskId,
        Question = dto.Question,
        Language = dto.Language,
        Pattern = dto.Pattern,
        Intent = dto.Intent,
        Stage = dto.Stage,
        Persona = dto.Persona,
        SellingPoint = dto.SellingPoint,
        QuestionSource = dto.QuestionSource,
        SourceDetail = dto.SourceDetail,
        SourceUrl = dto.SourceUrl,
        GoogleTrendsHeat = dto.GoogleTrendsHeat,
        CreatedAt = DateTime.UtcNow
    };

    private static GeoQuestionDto MapToDto(GeoQuestionEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        ProjectId = entity.ProjectId,
        Country = entity.Country,
        TaskId = entity.TaskId,
        Question = entity.Question,
        Language = entity.Language,
        Pattern = entity.Pattern,
        Intent = entity.Intent,
        Stage = entity.Stage,
        Persona = entity.Persona,
        SellingPoint = entity.SellingPoint,
        QuestionSource = entity.QuestionSource,
        SourceDetail = entity.SourceDetail,
        SourceUrl = entity.SourceUrl,
        GoogleTrendsHeat = entity.GoogleTrendsHeat,
        CreatedAt = entity.CreatedAt
    };

    private static GeoQuestionAnswerEntity MapAnswerToEntity(GeoQuestionAnswerDto dto) => new()
    {
        UserId = dto.UserId,
        QuestionId = dto.QuestionId,
        Model = dto.Model,
        Answer = dto.Answer,
        SearchIndex = dto.SearchIndex,
        BrandFitIndex = dto.BrandFitIndex,
        Score = dto.Score,
        BrandAnalysis = dto.BrandAnalysis,
        CitationDifficulty = dto.CitationDifficulty,
        AnswerMode = dto.AnswerMode,
        CreatedAt = DateTime.UtcNow
    };

    private static GeoQuestionAnswerDto MapAnswerToDto(GeoQuestionAnswerEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        QuestionId = entity.QuestionId,
        Model = entity.Model,
        Answer = entity.Answer,
        SearchIndex = entity.SearchIndex,
        BrandFitIndex = entity.BrandFitIndex,
        Score = entity.Score,
        BrandAnalysis = entity.BrandAnalysis,
        CitationDifficulty = entity.CitationDifficulty,
        AnswerMode = entity.AnswerMode,
        CreatedAt = entity.CreatedAt
    };

    private static GeoQuestionSourceEntity MapSourceToEntity(GeoQuestionSourceDto dto) => new()
    {
        UserId = dto.UserId,
        QuestionId = dto.QuestionId,
        AnswerId = dto.AnswerId,
        PlatformId = dto.PlatformId,
        Model = dto.Model,
        Url = dto.Url,
        Domain = dto.Domain,
        Title = dto.Title,
        Snippet = dto.Snippet,
        SourceType = dto.SourceType,
        AuthorityScore = dto.AuthorityScore,
        IsTargetForContent = dto.IsTargetForContent,
        ContentStatus = dto.ContentStatus,
        SortOrder = dto.SortOrder,
        CreatedAt = DateTime.UtcNow
    };

    private static GeoQuestionSourceDto MapSourceToDto(GeoQuestionSourceEntity entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        QuestionId = entity.QuestionId,
        AnswerId = entity.AnswerId,
        PlatformId = entity.PlatformId,
        Model = entity.Model,
        Url = entity.Url,
        Domain = entity.Domain,
        Title = entity.Title,
        Snippet = entity.Snippet,
        SourceType = entity.SourceType,
        AuthorityScore = entity.AuthorityScore,
        IsTargetForContent = entity.IsTargetForContent,
        ContentStatus = entity.ContentStatus,
        SortOrder = entity.SortOrder,
        CreatedAt = entity.CreatedAt
    };

    #endregion
}
