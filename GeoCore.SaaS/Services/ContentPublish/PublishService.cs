using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GeoCore.SaaS.Services.ContentPublish;

/// <summary>
/// 内容发布服务 - SaaS 前台
/// Phase 8.12: 实际发布 API 集成
/// </summary>
public class PublishService
{
    private readonly UserPlatformAccountRepository _accountRepo;
    private readonly ContentDraftRepository _draftRepo;
    private readonly PublishHistoryRepository _historyRepo;
    private readonly PublishRuleRepository _ruleRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublishService> _logger;

    public PublishService(
        UserPlatformAccountRepository accountRepo,
        ContentDraftRepository draftRepo,
        PublishHistoryRepository historyRepo,
        PublishRuleRepository ruleRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<PublishService> logger)
    {
        _accountRepo = accountRepo;
        _draftRepo = draftRepo;
        _historyRepo = historyRepo;
        _ruleRepo = ruleRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 发布内容到指定平台
    /// </summary>
    public async Task<PublishResult> PublishAsync(int userId, PublishRequest request)
    {
        var account = await _accountRepo.GetByUserAndPlatformAsync(userId, request.Platform);
        if (account == null || account.Status != "active")
        {
            return new PublishResult { Success = false, Error = $"未绑定 {request.Platform} 账号或账号已失效" };
        }

        if (account.TokenExpiresAt.HasValue && account.TokenExpiresAt.Value < DateTime.UtcNow)
        {
            return new PublishResult { Success = false, Error = "Token 已过期，请重新授权" };
        }

        var rateCheckResult = await CheckRateLimitAsync(userId, request.Platform);
        if (!rateCheckResult.Allowed)
        {
            return new PublishResult { Success = false, Error = rateCheckResult.Message };
        }

        try
        {
            var result = request.Platform switch
            {
                "reddit" => await PublishToRedditAsync(account, request),
                "linkedin" => await PublishToLinkedInAsync(account, request),
                "twitter" => await PublishToTwitterAsync(account, request),
                "medium" => await PublishToMediumAsync(account, request),
                _ => new PublishResult { Success = false, Error = $"不支持的平台: {request.Platform}" }
            };

            var history = new PublishHistoryEntity
            {
                UserId = userId,
                DraftId = request.DraftId ?? 0,
                Platform = request.Platform,
                Status = result.Success ? "published" : "failed",
                PlatformPostId = result.PostId,
                PlatformUrl = result.Url,
                ErrorMessage = result.Error,
                PublishedAt = result.Success ? DateTime.UtcNow : null
            };
            await _historyRepo.CreateAsync(history);

            if (result.Success && request.DraftId.HasValue)
            {
                var draft = await _draftRepo.GetByIdAsync(request.DraftId.Value);
                if (draft != null)
                {
                    draft.Status = "published";
                    await _draftRepo.UpdateAsync(draft);
                }
            }

            _logger.LogInformation("用户 {UserId} 发布到 {Platform}: {Success}", userId, request.Platform, result.Success);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布失败: {Platform}", request.Platform);
            return new PublishResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// 发布草稿
    /// </summary>
    public async Task<PublishResult> PublishDraftAsync(int userId, int draftId)
    {
        var draft = await _draftRepo.GetByIdAsync(draftId);
        if (draft == null || draft.UserId != userId)
        {
            return new PublishResult { Success = false, Error = "草稿不存在" };
        }

        if (draft.Status != "approved" && draft.Status != "draft")
        {
            return new PublishResult { Success = false, Error = $"草稿状态不允许发布: {draft.Status}" };
        }

        return await PublishAsync(userId, new PublishRequest
        {
            Platform = draft.Platform,
            DraftId = draftId,
            Title = draft.Title,
            Content = draft.Content
        });
    }

    #region Platform-specific Publishing

    private async Task<PublishResult> PublishToRedditAsync(UserPlatformAccountEntity account, PublishRequest request)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);
        client.DefaultRequestHeaders.Add("User-Agent", "GeoCore/1.0");

        var postData = new Dictionary<string, string>
        {
            ["kind"] = "self",
            ["sr"] = request.Subreddit ?? "test",
            ["title"] = request.Title ?? "Post from GeoCore",
            ["text"] = request.Content
        };

        var response = await client.PostAsync(
            "https://oauth.reddit.com/api/submit",
            new FormUrlEncodedContent(postData));

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Reddit 发布失败: {Status} {Content}", response.StatusCode, content);
            return new PublishResult { Success = false, Error = "Reddit 发布失败" };
        }

        try
        {
            var json = JsonDocument.Parse(content);
            var data = json.RootElement.GetProperty("json").GetProperty("data");
            var postId = data.GetProperty("id").GetString();
            var url = data.GetProperty("url").GetString();

            return new PublishResult
            {
                Success = true,
                PostId = postId,
                Url = url
            };
        }
        catch
        {
            return new PublishResult { Success = true, PostId = "unknown" };
        }
    }

    private async Task<PublishResult> PublishToLinkedInAsync(UserPlatformAccountEntity account, PublishRequest request)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

        var postData = new
        {
            author = $"urn:li:person:{account.PlatformUserId}",
            lifecycleState = "PUBLISHED",
            specificContent = new
            {
                comLinkedinUgcShareContent = new
                {
                    shareCommentary = new { text = request.Content },
                    shareMediaCategory = "NONE"
                }
            },
            visibility = new { comLinkedinUgcMemberNetworkVisibility = "PUBLIC" }
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(postData),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("https://api.linkedin.com/v2/ugcPosts", jsonContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("LinkedIn 发布失败: {Status} {Content}", response.StatusCode, content);
            return new PublishResult { Success = false, Error = "LinkedIn 发布失败" };
        }

        try
        {
            var json = JsonDocument.Parse(content);
            var postId = json.RootElement.GetProperty("id").GetString();
            return new PublishResult
            {
                Success = true,
                PostId = postId,
                Url = $"https://www.linkedin.com/feed/update/{postId}"
            };
        }
        catch
        {
            return new PublishResult { Success = true, PostId = "unknown" };
        }
    }

    private async Task<PublishResult> PublishToTwitterAsync(UserPlatformAccountEntity account, PublishRequest request)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

        var tweetData = new { text = request.Content };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(tweetData),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("https://api.twitter.com/2/tweets", jsonContent);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Twitter 发布失败: {Status} {Content}", response.StatusCode, content);
            return new PublishResult { Success = false, Error = "Twitter 发布失败" };
        }

        try
        {
            var json = JsonDocument.Parse(content);
            var tweetId = json.RootElement.GetProperty("data").GetProperty("id").GetString();
            return new PublishResult
            {
                Success = true,
                PostId = tweetId,
                Url = $"https://twitter.com/{account.PlatformUsername}/status/{tweetId}"
            };
        }
        catch
        {
            return new PublishResult { Success = true, PostId = "unknown" };
        }
    }

    private async Task<PublishResult> PublishToMediumAsync(UserPlatformAccountEntity account, PublishRequest request)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", account.AccessToken);

        var postData = new
        {
            title = request.Title ?? "Post from GeoCore",
            contentFormat = "markdown",
            content = request.Content,
            publishStatus = "public"
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(postData),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            $"https://api.medium.com/v1/users/{account.PlatformUserId}/posts",
            jsonContent);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Medium 发布失败: {Status} {Content}", response.StatusCode, content);
            return new PublishResult { Success = false, Error = "Medium 发布失败" };
        }

        try
        {
            var json = JsonDocument.Parse(content);
            var data = json.RootElement.GetProperty("data");
            var postId = data.GetProperty("id").GetString();
            var url = data.GetProperty("url").GetString();

            return new PublishResult
            {
                Success = true,
                PostId = postId,
                Url = url
            };
        }
        catch
        {
            return new PublishResult { Success = true, PostId = "unknown" };
        }
    }

    #endregion

    #region Rate Limiting

    private async Task<RateLimitCheckResult> CheckRateLimitAsync(int userId, string platform)
    {
        var rules = await _ruleRepo.GetByPlatformAsync(platform);
        var history = await _historyRepo.GetByUserIdAsync(userId, 100);
        var platformHistory = history.Where(h => h.Platform == platform && h.Status == "published").ToList();

        foreach (var rule in rules.Where(r => r.IsActive))
        {
            switch (rule.RuleType)
            {
                case "daily_limit":
                    var todayCount = platformHistory.Count(h => h.PublishedAt?.Date == DateTime.UtcNow.Date);
                    if (todayCount >= rule.RuleValue)
                    {
                        return new RateLimitCheckResult
                        {
                            Allowed = false,
                            Message = $"已达到每日发布限制 ({todayCount}/{rule.RuleValue})"
                        };
                    }
                    break;

                case "cooldown_minutes":
                    var lastPublish = platformHistory
                        .Where(h => h.PublishedAt.HasValue)
                        .OrderByDescending(h => h.PublishedAt)
                        .FirstOrDefault();
                    if (lastPublish?.PublishedAt != null)
                    {
                        var minutesSince = (DateTime.UtcNow - lastPublish.PublishedAt.Value).TotalMinutes;
                        if (minutesSince < rule.RuleValue)
                        {
                            var waitMinutes = rule.RuleValue - (int)minutesSince;
                            return new RateLimitCheckResult
                            {
                                Allowed = false,
                                Message = $"请等待 {waitMinutes} 分钟后再发布"
                            };
                        }
                    }
                    break;
            }
        }

        return new RateLimitCheckResult { Allowed = true };
    }

    #endregion
}

#region Models

public class PublishRequest
{
    public string Platform { get; set; } = "";
    public int? DraftId { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = "";
    public string? Subreddit { get; set; }
}

public class PublishResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? PostId { get; set; }
    public string? Url { get; set; }
}

public class RateLimitCheckResult
{
    public bool Allowed { get; set; }
    public string? Message { get; set; }
}

#endregion
