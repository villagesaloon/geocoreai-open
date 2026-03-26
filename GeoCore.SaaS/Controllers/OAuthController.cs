using Microsoft.AspNetCore.Mvc;
using GeoCore.SaaS.Services.ContentPublish;

namespace GeoCore.SaaS.Controllers;

/// <summary>
/// OAuth 授权 API (SaaS 前台)
/// Phase 8.11: 平台账号绑定
/// </summary>
[ApiController]
[Route("api/oauth")]
public class OAuthController : ControllerBase
{
    private readonly OAuthService _oauthService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(OAuthService oauthService, ILogger<OAuthController> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    private int GetUserId() => 1;

    /// <summary>
    /// 获取 OAuth 授权 URL
    /// </summary>
    [HttpGet("authorize/{platform}")]
    public async Task<IActionResult> GetAuthorizeUrl(string platform)
    {
        var userId = GetUserId();
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/oauth/callback/{platform}";
        
        var result = await _oauthService.GetAuthorizeUrlAsync(userId, platform, redirectUri);
        
        if (!result.Success)
            return BadRequest(new { success = false, message = result.Error });
            
        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// OAuth 回调处理
    /// </summary>
    [HttpGet("callback/{platform}")]
    public async Task<IActionResult> Callback(string platform, [FromQuery] string code, [FromQuery] string state, [FromQuery] string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("OAuth 授权被拒绝: {Platform} {Error}", platform, error);
            return Redirect($"/content-publish.html?error={Uri.EscapeDataString(error)}");
        }

        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/oauth/callback/{platform}";
        var result = await _oauthService.HandleCallbackAsync(platform, code, state, redirectUri);

        if (!result.Success)
        {
            _logger.LogError("OAuth 回调处理失败: {Platform} {Error}", platform, result.Error);
            return Redirect($"/content-publish.html?error={Uri.EscapeDataString(result.Error ?? "授权失败")}");
        }

        return Redirect($"/content-publish.html?success=1&platform={platform}&username={Uri.EscapeDataString(result.Username ?? "")}");
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    [HttpPost("refresh/{accountId}")]
    public async Task<IActionResult> RefreshToken(int accountId)
    {
        var result = await _oauthService.RefreshTokenAsync(accountId);
        return Ok(new { success = result });
    }

    /// <summary>
    /// 解绑平台账号
    /// </summary>
    [HttpDelete("unbind/{accountId}")]
    public async Task<IActionResult> UnbindAccount(int accountId)
    {
        var userId = GetUserId();
        var result = await _oauthService.UnbindAccountAsync(userId, accountId);
        
        if (!result)
            return NotFound(new { success = false, message = "账号不存在或无权操作" });
            
        return Ok(new { success = true });
    }
}
