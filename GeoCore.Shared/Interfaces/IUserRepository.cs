using System.Threading.Tasks;
using GeoCore.Shared.Models;

namespace GeoCore.Shared.Interfaces;

/// <summary>
/// 用户 Repository 接口
/// </summary>
public interface IUserRepository : IRepository<UserDto>
{
    /// <summary>
    /// 根据 Firebase UID 获取用户
    /// </summary>
    Task<UserDto?> GetByFirebaseUidAsync(string firebaseUid);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<UserDto?> GetByEmailAsync(string email);

    /// <summary>
    /// 用户登录或注册 (如果不存在则创建)
    /// </summary>
    Task<UserDto> LoginOrRegisterAsync(UserDto user);

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    Task UpdateLastLoginAsync(long userId);
}
