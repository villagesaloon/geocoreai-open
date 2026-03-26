using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace GeoCore.SaaS.Middleware;

/// <summary>
/// Firebase JWT 认证中间件
/// 验证 Firebase ID Token 并提取用户信息
/// </summary>
public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FirebaseAuthMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    // Firebase 公钥缓存
    private static Dictionary<string, SecurityKey>? _googleKeys;
    private static DateTime _keysLastFetched = DateTime.MinValue;
    private static readonly TimeSpan KeysCacheDuration = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim _keysFetchLock = new(1, 1);

    // 不需要认证的路径
    private static readonly string[] PublicPaths = new[]
    {
        "/api/health",
        "/api/cache/refresh",
        "/api/trends",
        "/app/",
        "/css/",
        "/js/",
        "/images/",
        "/favicon.ico",
        "/"
    };

    public FirebaseAuthMiddleware(
        RequestDelegate next,
        ILogger<FirebaseAuthMiddleware> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task InvokeAsync(HttpContext context)
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

        // 获取 Firebase 项目 ID
        var firebaseProjectId = _configuration["Firebase:ProjectId"];
        
        // 如果没有配置 Firebase，使用旧的 X-User-Id 方式（开发模式）
        if (string.IsNullOrEmpty(firebaseProjectId))
        {
            _logger.LogDebug("[FirebaseAuth] Firebase 未配置，使用开发模式");
            await HandleDevelopmentMode(context);
            return;
        }

        // 获取 Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            // 检查是否有 X-User-Id（向后兼容）
            if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                _logger.LogWarning("[FirebaseAuth] 使用不安全的 X-User-Id 认证: {UserId}", userIdHeader);
                await HandleDevelopmentMode(context);
                return;
            }
            
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "未提供认证令牌" });
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            // 验证 Firebase ID Token
            var principal = await ValidateFirebaseTokenAsync(token, firebaseProjectId);
            
            if (principal == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { success = false, error = "无效的认证令牌" });
                return;
            }

            // 提取用户信息
            var firebaseUid = principal.FindFirst("user_id")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(firebaseUid))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { success = false, error = "无法提取用户ID" });
                return;
            }

            // 将用户信息添加到 HttpContext
            context.Items["FirebaseUid"] = firebaseUid;
            context.Items["UserEmail"] = email;
            context.Items["UserName"] = name;
            
            // 设置 X-User-Id header（供后续代码使用）
            // 注意：这里需要从数据库查询或创建用户，获取内部用户ID
            var internalUserId = await GetOrCreateInternalUserIdAsync(firebaseUid, email, name);
            context.Request.Headers["X-User-Id"] = internalUserId.ToString();
            context.Items["UserId"] = internalUserId;

            _logger.LogDebug("[FirebaseAuth] 用户认证成功: FirebaseUid={Uid}, InternalId={Id}", firebaseUid, internalUserId);

            await _next(context);
        }
        catch (SecurityTokenExpiredException)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "认证令牌已过期" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FirebaseAuth] 令牌验证失败");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, error = "认证失败: " + ex.Message });
        }
    }

    private bool IsPublicPath(string path)
    {
        return PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    private async Task HandleDevelopmentMode(HttpContext context)
    {
        // 开发模式：从 X-User-Id header 获取用户ID
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
            long.TryParse(userIdHeader, out var userId))
        {
            context.Items["UserId"] = userId;
        }
        
        await _next(context);
    }

    private async Task<ClaimsPrincipal?> ValidateFirebaseTokenAsync(string token, string projectId)
    {
        // 获取 Google 公钥
        var keys = await GetGooglePublicKeysAsync();
        if (keys == null || keys.Count == 0)
        {
            _logger.LogError("[FirebaseAuth] 无法获取 Google 公钥");
            return null;
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true,
            IssuerSigningKeys = keys.Values,
            ValidateIssuerSigningKey = true
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParameters, out _);
        
        return principal;
    }

    private async Task<Dictionary<string, SecurityKey>?> GetGooglePublicKeysAsync()
    {
        // 检查缓存
        if (_googleKeys != null && DateTime.UtcNow - _keysLastFetched < KeysCacheDuration)
        {
            return _googleKeys;
        }

        await _keysFetchLock.WaitAsync();
        try
        {
            // 双重检查
            if (_googleKeys != null && DateTime.UtcNow - _keysLastFetched < KeysCacheDuration)
            {
                return _googleKeys;
            }

            // 从 Google 获取公钥
            var response = await _httpClient.GetStringAsync(
                "https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com");
            
            var keyDict = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            if (keyDict == null)
            {
                return null;
            }

            var keys = new Dictionary<string, SecurityKey>();
            foreach (var kvp in keyDict)
            {
                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    System.Text.Encoding.UTF8.GetBytes(kvp.Value));
                keys[kvp.Key] = new X509SecurityKey(cert);
            }

            _googleKeys = keys;
            _keysLastFetched = DateTime.UtcNow;
            
            _logger.LogInformation("[FirebaseAuth] 已刷新 Google 公钥，共 {Count} 个", keys.Count);
            
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FirebaseAuth] 获取 Google 公钥失败");
            return _googleKeys; // 返回旧的缓存（如果有）
        }
        finally
        {
            _keysFetchLock.Release();
        }
    }

    private async Task<long> GetOrCreateInternalUserIdAsync(string firebaseUid, string? email, string? name)
    {
        // TODO: 从数据库查询或创建用户
        // 这里暂时使用 Firebase UID 的哈希值作为内部用户ID
        // 生产环境应该查询 geo_users 表
        
        // 简单的哈希算法，将 Firebase UID 转换为 long
        var hash = firebaseUid.GetHashCode();
        return Math.Abs((long)hash);
    }
}

/// <summary>
/// Firebase 认证中间件扩展
/// </summary>
public static class FirebaseAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseFirebaseAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FirebaseAuthMiddleware>();
    }
}
