namespace GeoCore.SaaS.Services.ContentAdapter;

#region 5.14 社媒尺寸适配

public class MediaAdaptRequest
{
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceType { get; set; } = "image"; // image, video
    public List<string> TargetPlatforms { get; set; } = new();
}

public class MediaAdaptResult
{
    public string SourceUrl { get; set; } = string.Empty;
    public List<PlatformMediaSpec> Adaptations { get; set; } = new();
}

public class PlatformMediaSpec
{
    public string Platform { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty; // post, story, reel, cover
    public int Width { get; set; }
    public int Height { get; set; }
    public string AspectRatio { get; set; } = string.Empty;
    public string MaxFileSize { get; set; } = string.Empty;
    public string MaxDuration { get; set; } = string.Empty;
    public string CropSuggestion { get; set; } = string.Empty;
    public List<string> Tips { get; set; } = new();
}

#endregion

#region 5.15 视频脚本生成

public class VideoScriptRequest
{
    public string ArticleContent { get; set; } = string.Empty;
    public string VideoType { get; set; } = "long"; // long, short, tutorial
    public int TargetDurationMinutes { get; set; } = 10;
    public string Tone { get; set; } = "professional"; // professional, casual, educational
}

public class VideoScriptResult
{
    public string Title { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public List<ScriptSection> Sections { get; set; } = new();
    public string CallToAction { get; set; } = string.Empty;
    public int EstimatedDurationSeconds { get; set; }
    public List<string> BRollSuggestions { get; set; } = new();
    public List<string> KeyPoints { get; set; } = new();
}

public class ScriptSection
{
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Narration { get; set; } = string.Empty;
    public string VisualSuggestion { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

#endregion

#region 5.16 短视频切片建议

public class ShortClipRequest
{
    public string VideoTranscript { get; set; } = string.Empty;
    public string VideoTitle { get; set; } = string.Empty;
    public int MaxClips { get; set; } = 5;
    public int ClipDurationSeconds { get; set; } = 60;
}

public class ShortClipResult
{
    public List<ClipSuggestion> Clips { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class ClipSuggestion
{
    public int ClipNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public string StartTimestamp { get; set; } = string.Empty;
    public string EndTimestamp { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string KeyMessage { get; set; } = string.Empty;
    public double ViralPotentialScore { get; set; }
    public List<string> Hashtags { get; set; } = new();
    public string TargetPlatform { get; set; } = string.Empty; // tiktok, youtube_shorts, instagram_reels
}

#endregion

#region 5.17 图文卡片生成

public class CarouselCardRequest
{
    public string ArticleContent { get; set; } = string.Empty;
    public string Platform { get; set; } = "instagram"; // instagram, linkedin, xiaohongshu
    public int MaxCards { get; set; } = 10;
    public string Style { get; set; } = "professional"; // professional, casual, bold
}

public class CarouselCardResult
{
    public string Title { get; set; } = string.Empty;
    public List<CarouselCard> Cards { get; set; } = new();
    public string CoverSuggestion { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
}

public class CarouselCard
{
    public int CardNumber { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string BodyText { get; set; } = string.Empty;
    public string VisualSuggestion { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string IconSuggestion { get; set; } = string.Empty;
}

#endregion

#region 5.20 发布时间建议

public class PostingTimeRequest
{
    public string Platform { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty; // global, us, china, europe
    public string Industry { get; set; } = string.Empty;
}

public class PostingTimeResult
{
    public string Platform { get; set; } = string.Empty;
    public List<OptimalTimeSlot> BestTimes { get; set; } = new();
    public List<string> AvoidTimes { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
    public Dictionary<string, string> DayOfWeekRecommendations { get; set; } = new();
}

public class OptimalTimeSlot
{
    public string DayOfWeek { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public int EngagementScore { get; set; } // 1-100
    public string Reason { get; set; } = string.Empty;
}

#endregion

#region 5.24-5.26 Reddit 专项

public class RedditSubredditMatchRequest
{
    public string Content { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty; // discussion, question, resource, news
}

public class RedditSubredditMatchResult
{
    public List<SubredditRecommendation> Recommendations { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class SubredditRecommendation
{
    public string Subreddit { get; set; } = string.Empty;
    public int MatchScore { get; set; } // 1-100
    public int Subscribers { get; set; }
    public string ActivityLevel { get; set; } = string.Empty; // high, medium, low
    public List<string> AllowedContentTypes { get; set; } = new();
    public List<string> ProhibitedContent { get; set; } = new();
    public string BestPostingTime { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
}

public class RedditRuleCheckRequest
{
    public string Subreddit { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class RedditRuleCheckResult
{
    public bool IsCompliant { get; set; }
    public List<RuleViolation> Violations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

public class RuleViolation
{
    public string Rule { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // critical, warning
    public string Description { get; set; } = string.Empty;
    public string Fix { get; set; } = string.Empty;
}

public class RedditAccountPlanRequest
{
    public string Industry { get; set; } = string.Empty;
    public List<string> TargetSubreddits { get; set; } = new();
    public int PlanDays { get; set; } = 14;
}

public class RedditAccountPlanResult
{
    public List<DailyPlan> DailyPlans { get; set; } = new();
    public List<string> GeneralTips { get; set; } = new();
    public string KarmaGoal { get; set; } = string.Empty;
    public List<string> RecommendedSubreddits { get; set; } = new();
}

public class DailyPlan
{
    public int Day { get; set; }
    public string Focus { get; set; } = string.Empty;
    public List<DailyTask> Tasks { get; set; } = new();
    public string Goal { get; set; } = string.Empty;
}

public class DailyTask
{
    public string TaskType { get; set; } = string.Empty; // comment, upvote, post, engage
    public string Subreddit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
}

#endregion

#region 5.27-5.28 效果追踪

public class PlatformROIRequest
{
    public string Platform { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public int Views { get; set; }
    public int Engagements { get; set; }
    public int Clicks { get; set; }
    public int Conversions { get; set; }
    public double TimeInvestedHours { get; set; }
    public double CostUSD { get; set; }
}

public class PlatformROIResult
{
    public string Platform { get; set; } = string.Empty;
    public double EngagementRate { get; set; }
    public double ClickThroughRate { get; set; }
    public double ConversionRate { get; set; }
    public double CostPerEngagement { get; set; }
    public double CostPerClick { get; set; }
    public double CostPerConversion { get; set; }
    public double ROIScore { get; set; } // 1-100
    public string ROIGrade { get; set; } = string.Empty; // A, B, C, D, F
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class ContentLifecycleRequest
{
    public string Platform { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public List<DailyMetric> DailyMetrics { get; set; } = new();
}

public class DailyMetric
{
    public int DayNumber { get; set; }
    public int Views { get; set; }
    public int Engagements { get; set; }
}

public class ContentLifecycleResult
{
    public string Platform { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public int PeakDay { get; set; }
    public double HalfLifeDays { get; set; }
    public double DecayRate { get; set; }
    public string LifecycleStage { get; set; } = string.Empty; // growth, peak, decline, long_tail
    public List<string> Insights { get; set; } = new();
    public string RepurposeSuggestion { get; set; } = string.Empty;
}

#endregion
