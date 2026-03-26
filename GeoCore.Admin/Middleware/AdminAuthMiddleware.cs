using System.Text.Json;
using GeoCore.Shared.Interfaces;

namespace GeoCore.Admin.Middleware;

/// <summary>
/// Admin 后台认证中间件
/// 验证管理员登录状态
/// </summary>
public class AdminAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AdminAuthMiddleware> _logger;

    // 不需要认证的路径
    private static readonly string[] PublicPaths = new[]
    {
        "/api/admin/auth/login",
        "/api/admin/auth/init",
        "/api/health",
        "/admin/login",
        "/css/",
        "/js/",
        "/images/",
        "/favicon.ico",
        "/"
    };

    public AdminAuthMiddleware(RequestDelegate next, ILogger<AdminAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAdminRepository adminRepository)
    {
        var path = context.Request.Path.Value ?? "";

        // 检查是否是公开路径
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // 检查是否是 API 路径
        if (!path.StartsWith("/api/"))
        {
            await _next(context);
            return;
        }

        // 获取 Admin ID 和 Token
        var adminIdHeader = context.Request.Headers["X-Admin-Id"].FirstOrDefault();
        var tokenHeader = context.Request.Headers["X-Admin-Token"].FirstOrDefault();

        if (string.IsNullOrEmpty(adminIdHeader) || !long.TryParse(adminIdHeader, out var adminId))
        {
            _logger.LogWarning("[AdminAuth] 缺少 X-Admin-Id header, Path: {Path}", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "未登录，请先登录管理后台" });
            return;
        }

        // 验证管理员是否存在且有效
        var admin = await adminRepository.GetByIdAsync(adminId);
        if (admin == null)
        {
            _logger.LogWarning("[AdminAuth] 管理员不存在: {AdminId}", adminId);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "管理员不存在" });
            return;
        }

        if (admin.Status != 1)
        {
            _logger.LogWarning("[AdminAuth] 管理员已禁用: {AdminId}", adminId);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "管理员账号已禁用" });
            return;
        }

        // TODO: 验证 Token（生产环境应该使用 JWT 或 Session）
        // 目前简单验证 Token 是否存在
        if (string.IsNullOrEmpty(tokenHeader))
        {
            _logger.LogWarning("[AdminAuth] 缺少 X-Admin-Token header, AdminId: {AdminId}", adminId);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "认证令牌无效" });
            return;
        }

        // 将管理员信息添加到 HttpContext
        context.Items["AdminId"] = adminId;
        context.Items["AdminRole"] = admin.Role;
        context.Items["AdminUsername"] = admin.Username;

        _logger.LogDebug("[AdminAuth] 管理员认证成功: {Username} ({Role})", admin.Username, admin.Role);

        await _next(context);
    }

    private bool IsPublicPath(string path)
    {
        return PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Admin 认证中间件扩展
/// </summary>
public static class AdminAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AdminAuthMiddleware>();
    }
}
