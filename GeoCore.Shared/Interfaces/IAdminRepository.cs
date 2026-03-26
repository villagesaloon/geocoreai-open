using GeoCore.Shared.Models;

namespace GeoCore.Shared.Interfaces;

/// <summary>
/// 管理员 Repository 接口
/// </summary>
public interface IAdminRepository : IRepository<AdminDto>
{
    /// <summary>
    /// 根据用户名获取管理员
    /// </summary>
    Task<AdminDto?> GetByUsernameAsync(string username);

    /// <summary>
    /// 验证密码并登录
    /// </summary>
    Task<AdminDto?> ValidateAndLoginAsync(string username, string password);

    /// <summary>
    /// 创建管理员（带密码）
    /// </summary>
    Task<long> CreateAdminAsync(AdminDto admin, string password);

    /// <summary>
    /// 修改密码
    /// </summary>
    Task<bool> ChangePasswordAsync(long adminId, string newPassword);

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    Task<bool> UpdateLastLoginAsync(long adminId);
}
