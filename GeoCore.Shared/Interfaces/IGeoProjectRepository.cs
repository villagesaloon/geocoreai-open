using System.Collections.Generic;
using System.Threading.Tasks;
using GeoCore.Shared.Models;

namespace GeoCore.Shared.Interfaces;

/// <summary>
/// GEO 项目仓储接口
/// </summary>
public interface IGeoProjectRepository
{
    // 项目 CRUD
    Task<long> CreateProjectAsync(GeoProjectDto project);
    Task<GeoProjectDto?> GetProjectByIdAsync(long projectId, long userId);
    Task<List<GeoProjectDto>> GetProjectsByUserIdAsync(long userId);
    Task<bool> UpdateProjectAsync(GeoProjectDto project);
    Task<bool> DeleteProjectAsync(long projectId, long userId);
    
    // 项目配置
    Task<bool> SaveProjectConfigAsync(long projectId, GeoProjectConfigDto config);
    Task<GeoProjectConfigDto?> GetProjectConfigAsync(long projectId);
    
    // 竞品
    Task<bool> SaveCompetitorsAsync(long projectId, List<GeoCompetitorDto> competitors);
    Task<List<GeoCompetitorDto>> GetCompetitorsAsync(long projectId);
    
    // 卖点
    Task<bool> SaveSellingPointsAsync(long projectId, List<GeoSellingPointDto> sellingPoints);
    Task<List<GeoSellingPointDto>> GetSellingPointsAsync(long projectId);
    
    // 画像
    Task<bool> SavePersonasAsync(long projectId, List<GeoPersonaDto> personas);
    Task<List<GeoPersonaDto>> GetPersonasAsync(long projectId);
    
    // 阶段
    Task<bool> SaveStagesAsync(long projectId, List<GeoStageDto> stages);
    Task<List<GeoStageDto>> GetStagesAsync(long projectId);
}

/// <summary>
/// GEO 问题仓储接口
/// </summary>
public interface IGeoQuestionRepository
{
    // 问题
    Task<long> CreateQuestionAsync(GeoQuestionDto question);
    Task<List<long>> CreateQuestionsAsync(List<GeoQuestionDto> questions);
    Task<GeoQuestionDto?> GetQuestionByIdAsync(long questionId, long userId);
    Task<List<GeoQuestionDto>> GetQuestionsByProjectIdAsync(long projectId, long userId);
    Task<List<GeoQuestionDto>> GetQuestionsByTaskIdAsync(string taskId, long userId);
    Task<bool> DeleteQuestionsByProjectIdAsync(long projectId, long userId);
    
    // 回答
    Task<long> CreateAnswerAsync(GeoQuestionAnswerDto answer);
    Task<List<long>> CreateAnswersAsync(List<GeoQuestionAnswerDto> answers);
    Task<List<GeoQuestionAnswerDto>> GetAnswersByQuestionIdAsync(long questionId);
    
    // 来源
    Task<long> CreateSourceAsync(GeoQuestionSourceDto source);
    Task<List<long>> CreateSourcesAsync(List<GeoQuestionSourceDto> sources);
    Task<List<GeoQuestionSourceDto>> GetSourcesByQuestionIdAsync(long questionId);
    Task<List<GeoQuestionSourceDto>> GetSourcesByAnswerIdAsync(long answerId);
    Task<bool> UpdateSourceContentStatusAsync(long sourceId, long userId, string status);
    Task<bool> SetSourceAsTargetAsync(long sourceId, long userId, bool isTarget);
}

/// <summary>
/// 来源平台仓储接口（系统级）
/// </summary>
public interface ISysSourcePlatformRepository
{
    Task<List<SysSourcePlatformDto>> GetAllPlatformsAsync();
    Task<SysSourcePlatformDto?> GetPlatformByDomainAsync(string domain);
    Task<long> CreatePlatformAsync(SysSourcePlatformDto platform);
    Task<bool> UpdatePlatformAsync(SysSourcePlatformDto platform);
}
