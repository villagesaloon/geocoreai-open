using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.ContentPublish;

/// <summary>
/// 内容生成服务 - SaaS 前台
/// Phase 8.7-8.10: 模板选择、内容生成、草稿管理、发布前审核
/// </summary>
public class ContentGenerationService
{
    private readonly ContentTemplateRepository _templateRepo;
    private readonly PlatformContentRuleRepository _ruleRepo;
    private readonly ContentDraftRepository _draftRepo;
    private readonly PublishHistoryRepository _historyRepo;
    private readonly UserPlatformAccountRepository _accountRepo;
    private readonly ILogger<ContentGenerationService> _logger;

    public ContentGenerationService(
        ContentTemplateRepository templateRepo,
        PlatformContentRuleRepository ruleRepo,
        ContentDraftRepository draftRepo,
        PublishHistoryRepository historyRepo,
        UserPlatformAccountRepository accountRepo,
        ILogger<ContentGenerationService> logger)
    {
        _templateRepo = templateRepo;
        _ruleRepo = ruleRepo;
        _draftRepo = draftRepo;
        _historyRepo = historyRepo;
        _accountRepo = accountRepo;
        _logger = logger;
    }

    #region 模板管理 (8.7)

    /// <summary>
    /// 获取指定平台的可用模板
    /// </summary>
    public async Task<List<ContentTemplateDto>> GetTemplatesAsync(string platform)
    {
        var templates = await _templateRepo.GetByPlatformAsync(platform);
        return templates.Select(t => new ContentTemplateDto
        {
            Id = t.Id,
            Platform = t.Platform,
            TemplateType = t.TemplateType,
            TemplateName = t.TemplateName,
            ToneStyle = t.ToneStyle,
            Variables = ParseVariables(t.TemplateContent),
            Guidelines = t.Guidelines
        }).ToList();
    }

    /// <summary>
    /// 获取所有平台的模板摘要
    /// </summary>
    public async Task<Dictionary<string, int>> GetTemplateSummaryAsync()
    {
        var templates = await _templateRepo.GetActiveAsync();
        return templates.GroupBy(t => t.Platform)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion

    #region 内容生成 (8.8)

    /// <summary>
    /// 使用模板生成内容
    /// </summary>
    public async Task<ContentGenerationResult> GenerateContentAsync(ContentGenerationRequest request)
    {
        var template = await _templateRepo.GetByIdAsync(request.TemplateId);
        if (template == null)
            return new ContentGenerationResult { Success = false, Error = "模板不存在" };

        var content = template.TemplateContent;
        foreach (var variable in request.Variables)
        {
            content = content.Replace($"{{{{{variable.Key}}}}}", variable.Value);
        }

        var rules = await _ruleRepo.GetByPlatformAsync(template.Platform);
        var reviewResult = ReviewContent(content, rules);

        return new ContentGenerationResult
        {
            Success = true,
            GeneratedContent = content,
            Platform = template.Platform,
            TemplateType = template.TemplateType,
            ReviewResult = reviewResult
        };
    }

    #endregion

    #region 草稿管理 (8.9)

    /// <summary>
    /// 保存草稿
    /// </summary>
    public async Task<int> SaveDraftAsync(int userId, SaveDraftRequest request)
    {
        var draft = new ContentDraftEntity
        {
            UserId = userId,
            ProjectId = request.ProjectId,
            Platform = request.Platform,
            TemplateId = request.TemplateId,
            Title = request.Title,
            Content = request.Content,
            Status = "draft",
            Version = 1,
            VariableData = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null
        };

        var id = await _draftRepo.CreateAsync(draft);
        _logger.LogInformation("用户 {UserId} 创建草稿 {DraftId}", userId, id);
        return id;
    }

    /// <summary>
    /// 更新草稿
    /// </summary>
    public async Task<bool> UpdateDraftAsync(int userId, int draftId, SaveDraftRequest request)
    {
        var draft = await _draftRepo.GetByIdAsync(draftId);
        if (draft == null || draft.UserId != userId)
            return false;

        draft.Title = request.Title;
        draft.Content = request.Content;
        draft.VariableData = request.Variables != null ? JsonSerializer.Serialize(request.Variables) : null;
        draft.Version++;

        return await _draftRepo.UpdateAsync(draft);
    }

    /// <summary>
    /// 获取用户草稿列表
    /// </summary>
    public async Task<List<ContentDraftDto>> GetDraftsAsync(int userId, string? status = null)
    {
        var drafts = status != null
            ? await _draftRepo.GetByUserAndStatusAsync(userId, status)
            : await _draftRepo.GetByUserIdAsync(userId);

        return drafts.Select(d => new ContentDraftDto
        {
            Id = d.Id,
            Platform = d.Platform,
            Title = d.Title,
            ContentPreview = d.Content.Length > 100 ? d.Content[..100] + "..." : d.Content,
            Status = d.Status,
            Version = d.Version,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        }).ToList();
    }

    /// <summary>
    /// 获取草稿详情
    /// </summary>
    public async Task<ContentDraftEntity?> GetDraftAsync(int userId, int draftId)
    {
        var draft = await _draftRepo.GetByIdAsync(draftId);
        if (draft == null || draft.UserId != userId)
            return null;
        return draft;
    }

    /// <summary>
    /// 删除草稿
    /// </summary>
    public async Task<bool> DeleteDraftAsync(int userId, int draftId)
    {
        var draft = await _draftRepo.GetByIdAsync(draftId);
        if (draft == null || draft.UserId != userId)
            return false;

        return await _draftRepo.DeleteAsync(draftId);
    }

    #endregion

    #region 发布前审核 (8.10)

    /// <summary>
    /// 审核内容是否符合平台规范
    /// </summary>
    public async Task<ContentReviewResult> ReviewContentAsync(string platform, string content)
    {
        var rules = await _ruleRepo.GetByPlatformAsync(platform);
        return ReviewContent(content, rules);
    }

    /// <summary>
    /// 提交草稿进行审核
    /// </summary>
    public async Task<ContentReviewResult> SubmitForReviewAsync(int userId, int draftId)
    {
        var draft = await _draftRepo.GetByIdAsync(draftId);
        if (draft == null || draft.UserId != userId)
            return new ContentReviewResult { Passed = false, Issues = new[] { "草稿不存在" } };

        var rules = await _ruleRepo.GetByPlatformAsync(draft.Platform);
        var result = ReviewContent(draft.Content, rules);

        draft.Status = result.Passed ? "approved" : "reviewing";
        draft.ReviewResult = JsonSerializer.Serialize(result);
        await _draftRepo.UpdateAsync(draft);

        return result;
    }

    private ContentReviewResult ReviewContent(string content, List<PlatformContentRuleEntity> rules)
    {
        var issues = new List<string>();
        var warnings = new List<string>();

        foreach (var rule in rules.Where(r => r.IsActive))
        {
            try
            {
                var ruleValue = JsonSerializer.Deserialize<JsonElement>(rule.RuleValue);

                switch (rule.RuleType)
                {
                    case "char_limit":
                        var maxChars = ruleValue.GetProperty("max").GetInt32();
                        if (content.Length > maxChars)
                            issues.Add($"内容超过字符限制 ({content.Length}/{maxChars})");
                        break;

                    case "word_limit":
                        var maxWords = ruleValue.GetProperty("max").GetInt32();
                        var wordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        if (wordCount > maxWords)
                            issues.Add($"内容超过字数限制 ({wordCount}/{maxWords})");
                        break;

                    case "forbidden_words":
                        var forbiddenWords = ruleValue.EnumerateArray().Select(e => e.GetString()!).ToList();
                        foreach (var word in forbiddenWords)
                        {
                            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
                                issues.Add($"内容包含禁止词: {word}");
                        }
                        break;

                    case "hashtag_limit":
                        var maxHashtags = ruleValue.GetProperty("max").GetInt32();
                        var hashtagCount = Regex.Matches(content, @"#\w+").Count;
                        if (hashtagCount > maxHashtags)
                            warnings.Add($"标签数量超过建议值 ({hashtagCount}/{maxHashtags})");
                        break;

                    case "link_limit":
                        var maxLinks = ruleValue.GetProperty("max").GetInt32();
                        var linkCount = Regex.Matches(content, @"https?://\S+").Count;
                        if (linkCount > maxLinks)
                            warnings.Add($"链接数量超过建议值 ({linkCount}/{maxLinks})");
                        break;

                    case "self_promo_ratio":
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "规则 {RuleId} 解析失败", rule.Id);
            }
        }

        return new ContentReviewResult
        {
            Passed = issues.Count == 0,
            Issues = issues.ToArray(),
            Warnings = warnings.ToArray(),
            CharacterCount = content.Length,
            WordCount = content.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length
        };
    }

    #endregion

    #region 用户平台账号 (8.11)

    /// <summary>
    /// 获取用户绑定的平台账号
    /// </summary>
    public async Task<List<UserPlatformAccountDto>> GetUserAccountsAsync(int userId)
    {
        var accounts = await _accountRepo.GetByUserIdAsync(userId);
        return accounts.Select(a => new UserPlatformAccountDto
        {
            Id = a.Id,
            Platform = a.Platform,
            PlatformUsername = a.PlatformUsername,
            Status = a.Status,
            TokenExpiresAt = a.TokenExpiresAt,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// 检查用户是否已绑定指定平台
    /// </summary>
    public async Task<bool> HasPlatformAccountAsync(int userId, string platform)
    {
        var account = await _accountRepo.GetByUserAndPlatformAsync(userId, platform);
        return account != null && account.Status == "active";
    }

    #endregion

    #region 发布历史 (8.13)

    /// <summary>
    /// 获取用户发布历史
    /// </summary>
    public async Task<List<PublishHistoryDto>> GetPublishHistoryAsync(int userId, int limit = 50)
    {
        var history = await _historyRepo.GetByUserIdAsync(userId, limit);
        return history.Select(h => new PublishHistoryDto
        {
            Id = h.Id,
            Platform = h.Platform,
            Status = h.Status,
            PlatformUrl = h.PlatformUrl,
            PublishedAt = h.PublishedAt,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    #endregion

    #region 辅助方法

    private static List<string> ParseVariables(string template)
    {
        var matches = Regex.Matches(template, @"\{\{(\w+)\}\}");
        return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
    }

    #endregion
}

#region DTOs

public class ContentTemplateDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = "";
    public string TemplateType { get; set; } = "";
    public string TemplateName { get; set; } = "";
    public string? ToneStyle { get; set; }
    public List<string> Variables { get; set; } = new();
    public string? Guidelines { get; set; }
}

public class ContentGenerationRequest
{
    public int TemplateId { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
}

public class ContentGenerationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? GeneratedContent { get; set; }
    public string? Platform { get; set; }
    public string? TemplateType { get; set; }
    public ContentReviewResult? ReviewResult { get; set; }
}

public class SaveDraftRequest
{
    public int? ProjectId { get; set; }
    public string Platform { get; set; } = "";
    public int? TemplateId { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = "";
    public Dictionary<string, string>? Variables { get; set; }
}

public class ContentDraftDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = "";
    public string? Title { get; set; }
    public string ContentPreview { get; set; } = "";
    public string Status { get; set; } = "";
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ContentReviewResult
{
    public bool Passed { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
    public int CharacterCount { get; set; }
    public int WordCount { get; set; }
}

public class UserPlatformAccountDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = "";
    public string PlatformUsername { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublishHistoryDto
{
    public int Id { get; set; }
    public string Platform { get; set; } = "";
    public string Status { get; set; } = "";
    public string? PlatformUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion
