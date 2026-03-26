namespace GeoCore.Shared.Interfaces;

/// <summary>
/// 通用 Repository 接口
/// </summary>
/// <typeparam name="TDto">DTO 类型</typeparam>
public interface IRepository<TDto> where TDto : class
{
    /// <summary>
    /// 根据 ID 获取
    /// </summary>
    Task<TDto?> GetByIdAsync(long id);

    /// <summary>
    /// 获取所有 (不包含已删除)
    /// </summary>
    Task<List<TDto>> GetAllAsync();

    /// <summary>
    /// 分页获取
    /// </summary>
    Task<(List<TDto> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize);

    /// <summary>
    /// 添加
    /// </summary>
    Task<long> AddAsync(TDto dto);

    /// <summary>
    /// 批量添加
    /// </summary>
    Task<int> AddRangeAsync(List<TDto> dtos);

    /// <summary>
    /// 更新
    /// </summary>
    Task<bool> UpdateAsync(TDto dto);

    /// <summary>
    /// 软删除
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 检查是否存在
    /// </summary>
    Task<bool> ExistsAsync(long id);
}
