using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace GeoCore.SaaS.Services.ContentPublish;

/// <summary>
/// OAuth 授权服务 - SaaS 前台
/// Phase 8.11: 用户平台账号绑定
/// </summary>
public class OAuthService
{
    private readonly PublishPlatformAppRepository _appRepo;
    private readonly UserPlatformAccountRepository _accountRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OAuthService> _logger;

    private static readonly Dictionary<string, PlatformOAuthConfig> PlatformConfigs = new()
    {
        ["reddit"] = new PlatformOAuthConfig
        {
            AuthorizeUrl = "https://www.reddit.com/api/v1/authorize",
            TokenUrl = "https://www.reddit.com/api/v1/access_token",
            UserInfoUrl = "https://oauth.reddit.com/api/v1/me",
            DefaultScopes = "identity submit read"
        },
        ["linkedin"] = new PlatformOAuthConfig
        {
            AuthorizeUrl = "https://www.linkedin.com/oauth/v2/authorization",
            TokenUrl = "https://www.linkedin.com/oauth/v2/accessToken",
            UserInfoUrl = "https://api.linkedin.com/v2/userinfo",
            DefaultScopes = "openid profile w_member_social"
        },
        ["twitter"] = new PlatformOAuthConfig
        {
            AuthorizeUrl = "https://twitter.com/i/oauth2/authorize",
            TokenUrl = "https://api.twitter.com/2/oauth2/token",
            UserInfoUrl = "https://api.twitter.com/2/users/me",
            DefaultScopes = "tweet.read tweet.write users.read offline.access"
        },
        ["medium"] = new PlatformOAuthConfig
        {
            AuthorizeUrl = "https://medium.com/m/oauth/authorize",
            TokenUrl = "https://api.medium.com/v1/tokens",
            UserInfoUrl = "https://api.medium.com/v1/me",
            DefaultScopes = "basicProfile,publishPost"
        }
    };

    public OAuthService(
        PublishPlatformAppRepository appRepo,
        UserPlatformAccountRepository accountRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<OAuthService> logger)
    {
        _appRepo = appRepo;
        _accountRepo = accountRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 生成 OAuth 授权 URL
    /// </summary>
    public async Task<OAuthAuthorizeResult> GetAuthorizeUrlAsync(int userId, string platform, string redirectUri)
    {
        var app = await _appRepo.GetByPlatformAsync(platform);
        if (app == null || !app.IsActive)
        {
            return new OAuthAuthorizeResult { Success = false, Error = $"平台 {platform} 未配置或未启用" };
        }

        if (!PlatformConfigs.TryGetValue(platform, out var config))
        {
            return new OAuthAuthorizeResult { Success = false, Error = $"不支持的平台: {platform}" };
        }

        var state = GenerateState(userId, platform);
        var scopes = !string.IsNullOrEmpty(app.Scopes) ? app.Scopes : config.DefaultScopes;
        var callbackUri = !string.IsNullOrEmpty(app.RedirectUri) ? app.RedirectUri : redirectUri;

        var authorizeUrl = platform switch
        {
            "reddit" => BuildRedditAuthorizeUrl(app, config, callbackUri, state, scopes),
            "linkedin" => BuildLinkedInAuthorizeUrl(app, config, callbackUri, state, scopes),
            "twitter" => BuildTwitterAuthorizeUrl(app, config, callbackUri, state, scopes),
            "medium" => BuildMediumAuthorizeUrl(app, config, callbackUri, state, scopes),
            _ => null
        };

        if (authorizeUrl == null)
        {
            return new OAuthAuthorizeResult { Success = false, Error = "无法生成授权 URL" };
        }

        _logger.LogInformation("用户 {UserId} 请求 {Platform} OAuth 授权", userId, platform);

        return new OAuthAuthorizeResult
        {
            Success = true,
            AuthorizeUrl = authorizeUrl,
            State = state
        };
    }

    /// <summary>
    /// 处理 OAuth 回调
    /// </summary>
    public async Task<OAuthCallbackResult> HandleCallbackAsync(string platform, string code, string state, string redirectUri)
    {
        var (userId, platformFromState) = ParseState(state);
        if (userId <= 0 || platformFromState != platform)
        {
            return new OAuthCallbackResult { Success = false, Error = "无效的 state 参数" };
        }

        var app = await _appRepo.GetByPlatformAsync(platform);
        if (app == null)
        {
            return new OAuthCallbackResult { Success = false, Error = "平台配置不存在" };
        }

        if (!PlatformConfigs.TryGetValue(platform, out var config))
        {
            return new OAuthCallbackResult { Success = false, Error = "不支持的平台" };
        }

        try
        {
            var tokenResult = await ExchangeCodeForTokenAsync(platform, app, config, code, redirectUri);
            if (!tokenResult.Success)
            {
                return new OAuthCallbackResult { Success = false, Error = tokenResult.Error };
            }

            var userInfo = await GetUserInfoAsync(platform, config, tokenResult.AccessToken!);
            if (userInfo == null)
            {
                return new OAuthCallbackResult { Success = false, Error = "获取用户信息失败" };
            }

            var existingAccount = await _accountRepo.GetByUserAndPlatformAsync(userId, platform);
            if (existingAccount != null)
            {
                existingAccount.AccessToken = tokenResult.AccessToken;
                existingAccount.RefreshToken = tokenResult.RefreshToken;
                existingAccount.TokenExpiresAt = tokenResult.ExpiresAt;
                existingAccount.PlatformUserId = userInfo.UserId;
                existingAccount.PlatformUsername = userInfo.Username;
                existingAccount.Status = "active";
                existingAccount.UpdatedAt = DateTime.UtcNow;
                await _accountRepo.UpdateAsync(existingAccount);
            }
            else
            {
                var newAccount = new UserPlatformAccountEntity
                {
                    UserId = userId,
                    Platform = platform,
                    PlatformUserId = userInfo.UserId,
                    PlatformUsername = userInfo.Username,
                    AccessToken = tokenResult.AccessToken,
                    RefreshToken = tokenResult.RefreshToken,
                    TokenExpiresAt = tokenResult.ExpiresAt,
                    Status = "active"
                };
                await _accountRepo.CreateAsync(newAccount);
            }

            _logger.LogInformation("用户 {UserId} 成功绑定 {Platform} 账号 {Username}", userId, platform, userInfo.Username);

            return new OAuthCallbackResult
            {
                Success = true,
                Platform = platform,
                Username = userInfo.Username
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth 回调处理失败: {Platform}", platform);
            return new OAuthCallbackResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// 刷新 Access Token
    /// </summary>
    public async Task<bool> RefreshTokenAsync(int accountId)
    {
        var account = await _accountRepo.GetByIdAsync(accountId);
        if (account == null || string.IsNullOrEmpty(account.RefreshToken))
            return false;

        var app = await _appRepo.GetByPlatformAsync(account.Platform);
        if (app == null) return false;

        if (!PlatformConfigs.TryGetValue(account.Platform, out var config))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var tokenResult = await RefreshAccessTokenAsync(account.Platform, app, config, account.RefreshToken, client);

            if (tokenResult.Success)
            {
                account.AccessToken = tokenResult.AccessToken;
                if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
                    account.RefreshToken = tokenResult.RefreshToken;
                account.TokenExpiresAt = tokenResult.ExpiresAt;
                account.UpdatedAt = DateTime.UtcNow;
                await _accountRepo.UpdateAsync(account);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新 Token 失败: {AccountId}", accountId);
        }

        return false;
    }

    /// <summary>
    /// 解绑平台账号
    /// </summary>
    public async Task<bool> UnbindAccountAsync(int userId, int accountId)
    {
        var account = await _accountRepo.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            return false;

        return await _accountRepo.DeleteAsync(accountId);
    }

    #region Private Methods

    private static string GenerateState(int userId, string platform)
    {
        var data = $"{userId}:{platform}:{Guid.NewGuid():N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
    }

    private static (int userId, string platform) ParseState(string state)
    {
        try
        {
            var data = Encoding.UTF8.GetString(Convert.FromBase64String(state));
            var parts = data.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[0], out var userId))
            {
                return (userId, parts[1]);
            }
        }
        catch { }
        return (0, "");
    }

    private static string BuildRedditAuthorizeUrl(PublishPlatformAppEntity app, PlatformOAuthConfig config, string redirectUri, string state, string scopes)
    {
        return $"{config.AuthorizeUrl}?client_id={app.ClientId}&response_type=code&state={HttpUtility.UrlEncode(state)}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&duration=permanent&scope={HttpUtility.UrlEncode(scopes)}";
    }

    private static string BuildLinkedInAuthorizeUrl(PublishPlatformAppEntity app, PlatformOAuthConfig config, string redirectUri, string state, string scopes)
    {
        return $"{config.AuthorizeUrl}?response_type=code&client_id={app.ClientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&state={HttpUtility.UrlEncode(state)}&scope={HttpUtility.UrlEncode(scopes)}";
    }

    private static string BuildTwitterAuthorizeUrl(PublishPlatformAppEntity app, PlatformOAuthConfig config, string redirectUri, string state, string scopes)
    {
        var codeChallenge = Guid.NewGuid().ToString("N");
        return $"{config.AuthorizeUrl}?response_type=code&client_id={app.ClientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&scope={HttpUtility.UrlEncode(scopes)}&state={HttpUtility.UrlEncode(state)}&code_challenge={codeChallenge}&code_challenge_method=plain";
    }

    private static string BuildMediumAuthorizeUrl(PublishPlatformAppEntity app, PlatformOAuthConfig config, string redirectUri, string state, string scopes)
    {
        return $"{config.AuthorizeUrl}?client_id={app.ClientId}&scope={HttpUtility.UrlEncode(scopes)}&state={HttpUtility.UrlEncode(state)}&response_type=code&redirect_uri={HttpUtility.UrlEncode(redirectUri)}";
    }

    private async Task<TokenExchangeResult> ExchangeCodeForTokenAsync(string platform, PublishPlatformAppEntity app, PlatformOAuthConfig config, string code, string redirectUri)
    {
        var client = _httpClientFactory.CreateClient();

        HttpRequestMessage request;
        if (platform == "reddit")
        {
            request = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl);
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{app.ClientId}:{app.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            });
        }
        else
        {
            request = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["client_id"] = app.ClientId,
                ["client_secret"] = app.ClientSecret
            });
        }

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token 交换失败: {Status} {Content}", response.StatusCode, content);
            return new TokenExchangeResult { Success = false, Error = "Token 交换失败" };
        }

        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        return new TokenExchangeResult
        {
            Success = true,
            AccessToken = root.GetProperty("access_token").GetString(),
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            ExpiresAt = root.TryGetProperty("expires_in", out var exp)
                ? DateTime.UtcNow.AddSeconds(exp.GetInt32())
                : DateTime.UtcNow.AddHours(1)
        };
    }

    private async Task<TokenExchangeResult> RefreshAccessTokenAsync(string platform, PublishPlatformAppEntity app, PlatformOAuthConfig config, string refreshToken, HttpClient client)
    {
        HttpRequestMessage request;
        if (platform == "reddit")
        {
            request = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl);
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{app.ClientId}:{app.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });
        }
        else
        {
            request = new HttpRequestMessage(HttpMethod.Post, config.TokenUrl);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = app.ClientId,
                ["client_secret"] = app.ClientSecret
            });
        }

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new TokenExchangeResult { Success = false, Error = "Token 刷新失败" };
        }

        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        return new TokenExchangeResult
        {
            Success = true,
            AccessToken = root.GetProperty("access_token").GetString(),
            RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            ExpiresAt = root.TryGetProperty("expires_in", out var exp)
                ? DateTime.UtcNow.AddSeconds(exp.GetInt32())
                : DateTime.UtcNow.AddHours(1)
        };
    }

    private async Task<PlatformUserInfo?> GetUserInfoAsync(string platform, PlatformOAuthConfig config, string accessToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, config.UserInfoUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (platform == "reddit")
        {
            request.Headers.Add("User-Agent", "GeoCore/1.0");
        }

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("获取用户信息失败: {Status}", response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        return platform switch
        {
            "reddit" => new PlatformUserInfo
            {
                UserId = root.GetProperty("id").GetString() ?? "",
                Username = root.GetProperty("name").GetString() ?? ""
            },
            "linkedin" => new PlatformUserInfo
            {
                UserId = root.GetProperty("sub").GetString() ?? "",
                Username = root.GetProperty("name").GetString() ?? ""
            },
            "twitter" => new PlatformUserInfo
            {
                UserId = root.GetProperty("data").GetProperty("id").GetString() ?? "",
                Username = root.GetProperty("data").GetProperty("username").GetString() ?? ""
            },
            "medium" => new PlatformUserInfo
            {
                UserId = root.GetProperty("data").GetProperty("id").GetString() ?? "",
                Username = root.GetProperty("data").GetProperty("username").GetString() ?? ""
            },
            _ => null
        };
    }

    #endregion
}

#region Models

public class PlatformOAuthConfig
{
    public string AuthorizeUrl { get; set; } = "";
    public string TokenUrl { get; set; } = "";
    public string UserInfoUrl { get; set; } = "";
    public string DefaultScopes { get; set; } = "";
}

public class OAuthAuthorizeResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AuthorizeUrl { get; set; }
    public string? State { get; set; }
}

public class OAuthCallbackResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Platform { get; set; }
    public string? Username { get; set; }
}

public class TokenExchangeResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class PlatformUserInfo
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
}

#endregion
