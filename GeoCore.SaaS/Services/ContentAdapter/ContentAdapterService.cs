using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.ContentAdapter;

public class ContentAdapterService
{
    private readonly Dictionary<string, List<PlatformMediaSpec>> _mediaSpecs;
    private readonly Dictionary<string, List<OptimalTimeSlot>> _postingTimes;

    public ContentAdapterService()
    {
        _mediaSpecs = InitializeMediaSpecs();
        _postingTimes = InitializePostingTimes();
    }

    #region 5.14 社媒尺寸适配

    public MediaAdaptResult GetMediaAdaptations(MediaAdaptRequest request)
    {
        var result = new MediaAdaptResult
        {
            SourceUrl = request.SourceUrl,
            Adaptations = new List<PlatformMediaSpec>()
        };

        var platforms = request.TargetPlatforms.Count > 0 
            ? request.TargetPlatforms 
            : _mediaSpecs.Keys.ToList();

        foreach (var platform in platforms)
        {
            if (_mediaSpecs.TryGetValue(platform.ToLower(), out var specs))
            {
                result.Adaptations.AddRange(specs);
            }
        }

        return result;
    }

    public List<PlatformMediaSpec> GetPlatformMediaSpecs(string platform)
    {
        return _mediaSpecs.TryGetValue(platform.ToLower(), out var specs) ? specs : new List<PlatformMediaSpec>();
    }

    private Dictionary<string, List<PlatformMediaSpec>> InitializeMediaSpecs()
    {
        return new Dictionary<string, List<PlatformMediaSpec>>
        {
            ["instagram"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "Instagram", Format = "post_square", Width = 1080, Height = 1080, AspectRatio = "1:1", MaxFileSize = "30MB", MaxDuration = "60s", CropSuggestion = "居中裁剪", Tips = new() { "正方形最适合 Feed", "确保主体在中心" } },
                new() { Platform = "Instagram", Format = "post_portrait", Width = 1080, Height = 1350, AspectRatio = "4:5", MaxFileSize = "30MB", MaxDuration = "60s", CropSuggestion = "垂直裁剪", Tips = new() { "占据更多屏幕空间", "适合人像和产品" } },
                new() { Platform = "Instagram", Format = "story", Width = 1080, Height = 1920, AspectRatio = "9:16", MaxFileSize = "30MB", MaxDuration = "15s", CropSuggestion = "全屏垂直", Tips = new() { "添加互动元素", "使用贴纸和投票" } },
                new() { Platform = "Instagram", Format = "reels", Width = 1080, Height = 1920, AspectRatio = "9:16", MaxFileSize = "4GB", MaxDuration = "90s", CropSuggestion = "全屏垂直", Tips = new() { "前 3 秒抓住注意力", "使用热门音乐" } }
            },
            ["tiktok"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "TikTok", Format = "video", Width = 1080, Height = 1920, AspectRatio = "9:16", MaxFileSize = "287MB", MaxDuration = "10min", CropSuggestion = "全屏垂直", Tips = new() { "前 1 秒必须抓眼球", "使用热门音效", "添加字幕" } }
            },
            ["youtube"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "YouTube", Format = "video", Width = 1920, Height = 1080, AspectRatio = "16:9", MaxFileSize = "256GB", MaxDuration = "12h", CropSuggestion = "横屏", Tips = new() { "1080p 或更高", "添加章节标记" } },
                new() { Platform = "YouTube", Format = "shorts", Width = 1080, Height = 1920, AspectRatio = "9:16", MaxFileSize = "256GB", MaxDuration = "60s", CropSuggestion = "全屏垂直", Tips = new() { "60 秒以内", "循环播放友好" } },
                new() { Platform = "YouTube", Format = "thumbnail", Width = 1280, Height = 720, AspectRatio = "16:9", MaxFileSize = "2MB", MaxDuration = "N/A", CropSuggestion = "横屏", Tips = new() { "高对比度", "大字体", "人脸表情" } }
            },
            ["linkedin"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "LinkedIn", Format = "post_image", Width = 1200, Height = 627, AspectRatio = "1.91:1", MaxFileSize = "5MB", MaxDuration = "N/A", CropSuggestion = "横屏", Tips = new() { "专业风格", "清晰文字" } },
                new() { Platform = "LinkedIn", Format = "carousel", Width = 1080, Height = 1080, AspectRatio = "1:1", MaxFileSize = "10MB", MaxDuration = "N/A", CropSuggestion = "正方形", Tips = new() { "2-10 张图片", "每张一个要点" } },
                new() { Platform = "LinkedIn", Format = "video", Width = 1920, Height = 1080, AspectRatio = "16:9", MaxFileSize = "5GB", MaxDuration = "10min", CropSuggestion = "横屏", Tips = new() { "添加字幕", "专业内容" } }
            },
            ["twitter"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "X/Twitter", Format = "image", Width = 1200, Height = 675, AspectRatio = "16:9", MaxFileSize = "5MB", MaxDuration = "N/A", CropSuggestion = "横屏", Tips = new() { "高清图片", "避免过多文字" } },
                new() { Platform = "X/Twitter", Format = "video", Width = 1280, Height = 720, AspectRatio = "16:9", MaxFileSize = "512MB", MaxDuration = "140s", CropSuggestion = "横屏", Tips = new() { "2 分 20 秒以内", "添加字幕" } }
            },
            ["xiaohongshu"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "小红书", Format = "image", Width = 1080, Height = 1440, AspectRatio = "3:4", MaxFileSize = "20MB", MaxDuration = "N/A", CropSuggestion = "竖版", Tips = new() { "封面要吸引人", "添加文字标注" } },
                new() { Platform = "小红书", Format = "video", Width = 1080, Height = 1920, AspectRatio = "9:16", MaxFileSize = "100MB", MaxDuration = "15min", CropSuggestion = "全屏垂直", Tips = new() { "前 3 秒抓住注意力", "添加字幕" } }
            },
            ["zhihu"] = new List<PlatformMediaSpec>
            {
                new() { Platform = "知乎", Format = "article_image", Width = 1200, Height = 800, AspectRatio = "3:2", MaxFileSize = "10MB", MaxDuration = "N/A", CropSuggestion = "横屏", Tips = new() { "配合文章内容", "清晰专业" } }
            }
        };
    }

    #endregion

    #region 5.15 视频脚本生成

    public VideoScriptResult GenerateVideoScript(VideoScriptRequest request)
    {
        var paragraphs = request.ArticleContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var keyPoints = ExtractKeyPoints(request.ArticleContent);
        
        var sections = new List<ScriptSection>();
        var sectionDuration = (request.TargetDurationMinutes * 60) / Math.Max(paragraphs.Length, 5);
        var currentTime = 0;

        // 开场 Hook
        var hookDuration = Math.Min(30, sectionDuration);
        sections.Add(new ScriptSection
        {
            Order = 0,
            Title = "开场 Hook",
            Narration = GenerateHook(keyPoints.FirstOrDefault() ?? "本视频主题"),
            VisualSuggestion = "动态标题卡 + 问题展示",
            DurationSeconds = hookDuration,
            Timestamp = "0:00"
        });
        currentTime += hookDuration;

        // 主体内容
        var mainSections = Math.Min(paragraphs.Length, 5);
        for (int i = 0; i < mainSections; i++)
        {
            var para = paragraphs[i];
            var narration = SimplifyForNarration(para);
            
            sections.Add(new ScriptSection
            {
                Order = i + 1,
                Title = $"要点 {i + 1}",
                Narration = narration,
                VisualSuggestion = GetVisualSuggestion(i),
                DurationSeconds = sectionDuration,
                Timestamp = FormatTimestamp(currentTime)
            });
            currentTime += sectionDuration;
        }

        // 结尾 CTA
        sections.Add(new ScriptSection
        {
            Order = sections.Count,
            Title = "总结与 CTA",
            Narration = "以上就是今天分享的全部内容。如果觉得有帮助，请点赞订阅，我们下期再见！",
            VisualSuggestion = "总结要点 + 订阅提示",
            DurationSeconds = 30,
            Timestamp = FormatTimestamp(currentTime)
        });

        return new VideoScriptResult
        {
            Title = GenerateVideoTitle(keyPoints),
            Hook = sections[0].Narration,
            Sections = sections,
            CallToAction = "点赞、评论、订阅，开启通知小铃铛！",
            EstimatedDurationSeconds = currentTime + 30,
            BRollSuggestions = new List<string> { "相关产品展示", "数据图表动画", "真实案例截图", "专家采访片段" },
            KeyPoints = keyPoints
        };
    }

    private List<string> ExtractKeyPoints(string content)
    {
        var points = new List<string>();
        var sentences = content.Split(new[] { '。', '！', '？', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var sentence in sentences.Take(10))
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length > 20 && trimmed.Length < 100)
            {
                points.Add(trimmed);
            }
        }
        
        return points.Take(5).ToList();
    }

    private string GenerateHook(string topic)
    {
        return $"你是否想过 {topic}？今天我将分享一个可能改变你认知的方法。请看到最后，因为最重要的内容在后面。";
    }

    private string SimplifyForNarration(string text)
    {
        var simplified = Regex.Replace(text, @"\[.*?\]|\(.*?\)", "");
        simplified = Regex.Replace(simplified, @"\s+", " ");
        return simplified.Trim();
    }

    private string GetVisualSuggestion(int index)
    {
        var suggestions = new[] { "数据图表展示", "案例截图", "动画演示", "对比图", "流程图" };
        return suggestions[index % suggestions.Length];
    }

    private string FormatTimestamp(int seconds)
    {
        var minutes = seconds / 60;
        var secs = seconds % 60;
        return $"{minutes}:{secs:D2}";
    }

    private string GenerateVideoTitle(List<string> keyPoints)
    {
        if (keyPoints.Count == 0) return "深度解析：你需要知道的一切";
        var firstPoint = keyPoints[0];
        if (firstPoint.Length > 30) firstPoint = firstPoint.Substring(0, 30) + "...";
        return $"【深度解析】{firstPoint}";
    }

    #endregion

    #region 5.16 短视频切片建议

    public ShortClipResult SuggestShortClips(ShortClipRequest request)
    {
        var clips = new List<ClipSuggestion>();
        var sentences = request.VideoTranscript.Split(new[] { '。', '！', '？', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        // 识别高价值片段
        var valuableSections = new List<(int index, string content, double score)>();
        
        for (int i = 0; i < sentences.Length; i++)
        {
            var sentence = sentences[i].Trim();
            var score = CalculateViralScore(sentence);
            if (score > 0.5)
            {
                valuableSections.Add((i, sentence, score));
            }
        }

        var topSections = valuableSections.OrderByDescending(s => s.score).Take(request.MaxClips);
        var clipNumber = 1;

        foreach (var section in topSections)
        {
            var startTime = EstimateTimestamp(section.index, sentences.Length);
            
            clips.Add(new ClipSuggestion
            {
                ClipNumber = clipNumber++,
                Title = GenerateClipTitle(section.content),
                Hook = GenerateClipHook(section.content),
                StartTimestamp = startTime,
                EndTimestamp = AddSeconds(startTime, request.ClipDurationSeconds),
                DurationSeconds = request.ClipDurationSeconds,
                KeyMessage = section.content.Length > 100 ? section.content.Substring(0, 100) + "..." : section.content,
                ViralPotentialScore = section.score * 100,
                Hashtags = GenerateHashtags(section.content),
                TargetPlatform = section.score > 0.8 ? "tiktok" : "youtube_shorts"
            });
        }

        return new ShortClipResult
        {
            Clips = clips,
            Summary = $"从视频中识别出 {clips.Count} 个高价值短视频切片机会"
        };
    }

    private double CalculateViralScore(string content)
    {
        var score = 0.3;
        
        // 包含数字
        if (Regex.IsMatch(content, @"\d+")) score += 0.15;
        // 包含问题
        if (content.Contains("?") || content.Contains("？")) score += 0.2;
        // 包含惊叹
        if (content.Contains("!") || content.Contains("！")) score += 0.1;
        // 包含关键词
        var viralKeywords = new[] { "秘密", "技巧", "方法", "原因", "真相", "必须", "最", "secret", "tips", "hack", "why", "how" };
        if (viralKeywords.Any(k => content.ToLower().Contains(k))) score += 0.25;
        // 长度适中
        if (content.Length > 30 && content.Length < 150) score += 0.1;
        
        return Math.Min(1.0, score);
    }

    private string EstimateTimestamp(int sentenceIndex, int totalSentences)
    {
        var estimatedMinutes = (sentenceIndex * 10) / Math.Max(totalSentences, 1);
        return $"{estimatedMinutes}:00";
    }

    private string AddSeconds(string timestamp, int seconds)
    {
        var parts = timestamp.Split(':');
        var minutes = int.Parse(parts[0]);
        var secs = int.Parse(parts[1]) + seconds;
        minutes += secs / 60;
        secs %= 60;
        return $"{minutes}:{secs:D2}";
    }

    private string GenerateClipTitle(string content)
    {
        var title = content.Length > 50 ? content.Substring(0, 50) : content;
        return $"🔥 {title}";
    }

    private string GenerateClipHook(string content)
    {
        return $"你知道吗？{content.Split('，', ',')[0]}...";
    }

    private List<string> GenerateHashtags(string content)
    {
        return new List<string> { "#干货分享", "#涨知识", "#必看", "#fyp", "#viral" };
    }

    #endregion

    #region 5.17 图文卡片生成

    public CarouselCardResult GenerateCarouselCards(CarouselCardRequest request)
    {
        var paragraphs = request.ArticleContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var cards = new List<CarouselCard>();
        var colors = GetColorScheme(request.Style);

        // 封面卡
        cards.Add(new CarouselCard
        {
            CardNumber = 1,
            Headline = ExtractMainTitle(request.ArticleContent),
            BodyText = "滑动查看完整内容 →",
            VisualSuggestion = "品牌 Logo + 主题图片",
            BackgroundColor = colors.primary,
            TextColor = colors.text,
            IconSuggestion = "📌"
        });

        // 内容卡
        var cardNumber = 2;
        foreach (var para in paragraphs.Take(request.MaxCards - 2))
        {
            var keyPoint = ExtractKeyPoint(para);
            cards.Add(new CarouselCard
            {
                CardNumber = cardNumber++,
                Headline = $"要点 {cardNumber - 1}",
                BodyText = keyPoint,
                VisualSuggestion = GetCardVisual(cardNumber),
                BackgroundColor = colors.secondary,
                TextColor = colors.text,
                IconSuggestion = GetCardIcon(cardNumber)
            });
        }

        // CTA 卡
        cards.Add(new CarouselCard
        {
            CardNumber = cards.Count + 1,
            Headline = "想了解更多？",
            BodyText = "关注我获取更多干货内容！",
            VisualSuggestion = "CTA 按钮 + 二维码",
            BackgroundColor = colors.primary,
            TextColor = colors.text,
            IconSuggestion = "👆"
        });

        return new CarouselCardResult
        {
            Title = ExtractMainTitle(request.ArticleContent),
            Cards = cards,
            CoverSuggestion = "使用高对比度封面，包含主题关键词",
            Hashtags = GeneratePlatformHashtags(request.Platform)
        };
    }

    private (string primary, string secondary, string text) GetColorScheme(string style)
    {
        return style switch
        {
            "bold" => ("#FF6B6B", "#4ECDC4", "#FFFFFF"),
            "casual" => ("#FFE66D", "#95E1D3", "#333333"),
            _ => ("#2C3E50", "#ECF0F1", "#2C3E50")
        };
    }

    private string ExtractMainTitle(string content)
    {
        var firstLine = content.Split('\n')[0];
        return firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
    }

    private string ExtractKeyPoint(string paragraph)
    {
        var cleaned = Regex.Replace(paragraph, @"\[.*?\]|\(.*?\)", "").Trim();
        return cleaned.Length > 150 ? cleaned.Substring(0, 150) + "..." : cleaned;
    }

    private string GetCardVisual(int cardNumber)
    {
        var visuals = new[] { "图标插画", "数据图表", "流程图", "对比图", "案例截图" };
        return visuals[(cardNumber - 1) % visuals.Length];
    }

    private string GetCardIcon(int cardNumber)
    {
        var icons = new[] { "💡", "📊", "🎯", "✅", "🚀", "💪", "🔑", "⭐" };
        return icons[(cardNumber - 1) % icons.Length];
    }

    private List<string> GeneratePlatformHashtags(string platform)
    {
        return platform.ToLower() switch
        {
            "instagram" => new List<string> { "#干货分享", "#知识卡片", "#carousel", "#infographic", "#tips" },
            "linkedin" => new List<string> { "#职场干货", "#行业洞察", "#专业分享", "#careeradvice" },
            "xiaohongshu" => new List<string> { "#干货分享", "#知识博主", "#收藏起来", "#涨知识" },
            _ => new List<string> { "#干货", "#分享", "#知识" }
        };
    }

    #endregion

    #region 5.20 发布时间建议

    public PostingTimeResult GetPostingTimeSuggestion(PostingTimeRequest request)
    {
        var platform = request.Platform.ToLower();
        var times = _postingTimes.TryGetValue(platform, out var slots) ? slots : GetDefaultPostingTimes();

        // 根据目标受众调整
        if (request.TargetAudience.ToLower() == "china")
        {
            times = AdjustForChina(times);
        }

        return new PostingTimeResult
        {
            Platform = request.Platform,
            BestTimes = times,
            AvoidTimes = GetAvoidTimes(platform),
            Rationale = GetPostingRationale(platform),
            DayOfWeekRecommendations = GetDayRecommendations(platform)
        };
    }

    private Dictionary<string, List<OptimalTimeSlot>> InitializePostingTimes()
    {
        return new Dictionary<string, List<OptimalTimeSlot>>
        {
            ["instagram"] = new List<OptimalTimeSlot>
            {
                new() { DayOfWeek = "周二", TimeRange = "11:00-13:00", Timezone = "EST", EngagementScore = 95, Reason = "午餐时间高峰" },
                new() { DayOfWeek = "周三", TimeRange = "11:00-13:00", Timezone = "EST", EngagementScore = 90, Reason = "工作周中间活跃度高" },
                new() { DayOfWeek = "周五", TimeRange = "10:00-11:00", Timezone = "EST", EngagementScore = 85, Reason = "周末前放松时间" }
            },
            ["tiktok"] = new List<OptimalTimeSlot>
            {
                new() { DayOfWeek = "周二", TimeRange = "19:00-21:00", Timezone = "EST", EngagementScore = 95, Reason = "晚间娱乐高峰" },
                new() { DayOfWeek = "周四", TimeRange = "19:00-21:00", Timezone = "EST", EngagementScore = 92, Reason = "周末前夜活跃" },
                new() { DayOfWeek = "周六", TimeRange = "20:00-23:00", Timezone = "EST", EngagementScore = 88, Reason = "周末夜间高峰" }
            },
            ["linkedin"] = new List<OptimalTimeSlot>
            {
                new() { DayOfWeek = "周二", TimeRange = "08:00-10:00", Timezone = "EST", EngagementScore = 95, Reason = "工作日早间专业时间" },
                new() { DayOfWeek = "周三", TimeRange = "08:00-10:00", Timezone = "EST", EngagementScore = 92, Reason = "工作周中间" },
                new() { DayOfWeek = "周四", TimeRange = "09:00-11:00", Timezone = "EST", EngagementScore = 88, Reason = "专业内容消费高峰" }
            },
            ["twitter"] = new List<OptimalTimeSlot>
            {
                new() { DayOfWeek = "周一", TimeRange = "08:00-10:00", Timezone = "EST", EngagementScore = 90, Reason = "周一早间新闻消费" },
                new() { DayOfWeek = "周三", TimeRange = "12:00-13:00", Timezone = "EST", EngagementScore = 88, Reason = "午餐时间浏览" },
                new() { DayOfWeek = "周五", TimeRange = "09:00-10:00", Timezone = "EST", EngagementScore = 85, Reason = "周末前活跃" }
            },
            ["youtube"] = new List<OptimalTimeSlot>
            {
                new() { DayOfWeek = "周四", TimeRange = "14:00-16:00", Timezone = "EST", EngagementScore = 92, Reason = "周末前视频消费" },
                new() { DayOfWeek = "周五", TimeRange = "14:00-16:00", Timezone = "EST", EngagementScore = 95, Reason = "周末视频高峰前" },
                new() { DayOfWeek = "周六", TimeRange = "09:00-11:00", Timezone = "EST", EngagementScore = 88, Reason = "周末早间" }
            },
            ["reddit"] = new List<OptimalTimeSlot>
            {
                new() { DayOfWeek = "周一", TimeRange = "06:00-08:00", Timezone = "EST", EngagementScore = 90, Reason = "美国早间浏览" },
                new() { DayOfWeek = "周六", TimeRange = "08:00-10:00", Timezone = "EST", EngagementScore = 88, Reason = "周末早间活跃" },
                new() { DayOfWeek = "周日", TimeRange = "20:00-22:00", Timezone = "EST", EngagementScore = 85, Reason = "周末晚间" }
            }
        };
    }

    private List<OptimalTimeSlot> GetDefaultPostingTimes()
    {
        return new List<OptimalTimeSlot>
        {
            new() { DayOfWeek = "周二", TimeRange = "10:00-12:00", Timezone = "EST", EngagementScore = 80, Reason = "通用最佳时间" },
            new() { DayOfWeek = "周四", TimeRange = "14:00-16:00", Timezone = "EST", EngagementScore = 78, Reason = "通用次佳时间" }
        };
    }

    private List<OptimalTimeSlot> AdjustForChina(List<OptimalTimeSlot> times)
    {
        return times.Select(t => new OptimalTimeSlot
        {
            DayOfWeek = t.DayOfWeek,
            TimeRange = AdjustTimeForChina(t.TimeRange),
            Timezone = "CST",
            EngagementScore = t.EngagementScore,
            Reason = t.Reason
        }).ToList();
    }

    private string AdjustTimeForChina(string timeRange)
    {
        // EST to CST (+13 hours)
        return timeRange; // 简化处理
    }

    private List<string> GetAvoidTimes(string platform)
    {
        return platform switch
        {
            "linkedin" => new List<string> { "周末全天", "工作日晚间 20:00 后" },
            "tiktok" => new List<string> { "工作日上午 9:00 前", "深夜 2:00-6:00" },
            _ => new List<string> { "深夜 2:00-6:00", "节假日" }
        };
    }

    private string GetPostingRationale(string platform)
    {
        return platform switch
        {
            "linkedin" => "LinkedIn 用户主要在工作时间活跃，早间和午餐时间是最佳发布窗口",
            "tiktok" => "TikTok 用户在晚间和周末最活跃，娱乐时间是最佳发布窗口",
            "instagram" => "Instagram 用户在午餐和晚间最活跃，视觉内容在这些时段表现最好",
            "reddit" => "Reddit 用户在美国早间最活跃，周末活跃度也较高",
            _ => "根据平台算法和用户行为数据，选择最佳发布时间可提升 20-40% 的初始曝光"
        };
    }

    private Dictionary<string, string> GetDayRecommendations(string platform)
    {
        return new Dictionary<string, string>
        {
            ["周一"] = "适合发布工作相关内容",
            ["周二"] = "最佳发布日之一",
            ["周三"] = "工作周中间，参与度高",
            ["周四"] = "适合发布周末预热内容",
            ["周五"] = "适合轻松娱乐内容",
            ["周六"] = "适合长视频和深度内容",
            ["周日"] = "适合规划类和总结类内容"
        };
    }

    #endregion

    #region 5.24-5.26 Reddit 专项

    public RedditSubredditMatchResult MatchSubreddits(RedditSubredditMatchRequest request)
    {
        var recommendations = new List<SubredditRecommendation>();
        var industry = request.Industry.ToLower();
        var contentType = request.ContentType.ToLower();

        // 行业相关 subreddits
        var industrySubreddits = GetIndustrySubreddits(industry);
        foreach (var sub in industrySubreddits)
        {
            recommendations.Add(sub);
        }

        // 通用高流量 subreddits
        if (contentType == "question" || contentType == "discussion")
        {
            recommendations.Add(new SubredditRecommendation
            {
                Subreddit = "r/AskReddit",
                MatchScore = 70,
                Subscribers = 45000000,
                ActivityLevel = "high",
                AllowedContentTypes = new() { "question" },
                ProhibitedContent = new() { "自我推广", "链接" },
                BestPostingTime = "美国早间 6-8 AM EST",
                Rationale = "超高流量，适合问题类内容"
            });
        }

        return new RedditSubredditMatchResult
        {
            Recommendations = recommendations.OrderByDescending(r => r.MatchScore).Take(5).ToList(),
            Summary = $"根据 {request.Industry} 行业和 {request.ContentType} 内容类型，推荐以上 subreddits"
        };
    }

    private List<SubredditRecommendation> GetIndustrySubreddits(string industry)
    {
        var subreddits = new Dictionary<string, List<SubredditRecommendation>>
        {
            ["tech"] = new List<SubredditRecommendation>
            {
                new() { Subreddit = "r/technology", MatchScore = 90, Subscribers = 15000000, ActivityLevel = "high", AllowedContentTypes = new() { "news", "discussion" }, ProhibitedContent = new() { "自我推广" }, BestPostingTime = "美国早间", Rationale = "技术新闻和讨论" },
                new() { Subreddit = "r/programming", MatchScore = 85, Subscribers = 5000000, ActivityLevel = "high", AllowedContentTypes = new() { "resource", "discussion" }, ProhibitedContent = new() { "初学者问题" }, BestPostingTime = "工作日", Rationale = "编程相关" },
                new() { Subreddit = "r/webdev", MatchScore = 80, Subscribers = 2000000, ActivityLevel = "medium", AllowedContentTypes = new() { "resource", "question", "showcase" }, ProhibitedContent = new() { "垃圾链接" }, BestPostingTime = "工作日", Rationale = "Web 开发" }
            },
            ["marketing"] = new List<SubredditRecommendation>
            {
                new() { Subreddit = "r/marketing", MatchScore = 90, Subscribers = 500000, ActivityLevel = "medium", AllowedContentTypes = new() { "discussion", "question" }, ProhibitedContent = new() { "自我推广", "招聘" }, BestPostingTime = "工作日", Rationale = "营销讨论" },
                new() { Subreddit = "r/SEO", MatchScore = 85, Subscribers = 200000, ActivityLevel = "medium", AllowedContentTypes = new() { "question", "resource" }, ProhibitedContent = new() { "服务推广" }, BestPostingTime = "工作日", Rationale = "SEO 专业" },
                new() { Subreddit = "r/content_marketing", MatchScore = 80, Subscribers = 50000, ActivityLevel = "low", AllowedContentTypes = new() { "discussion", "resource" }, ProhibitedContent = new() { "自我推广" }, BestPostingTime = "工作日", Rationale = "内容营销" }
            },
            ["ai"] = new List<SubredditRecommendation>
            {
                new() { Subreddit = "r/artificial", MatchScore = 90, Subscribers = 1000000, ActivityLevel = "high", AllowedContentTypes = new() { "news", "discussion" }, ProhibitedContent = new() { "垃圾内容" }, BestPostingTime = "全天", Rationale = "AI 新闻和讨论" },
                new() { Subreddit = "r/MachineLearning", MatchScore = 85, Subscribers = 3000000, ActivityLevel = "high", AllowedContentTypes = new() { "research", "discussion" }, ProhibitedContent = new() { "初学者问题" }, BestPostingTime = "工作日", Rationale = "机器学习研究" },
                new() { Subreddit = "r/ChatGPT", MatchScore = 80, Subscribers = 5000000, ActivityLevel = "high", AllowedContentTypes = new() { "discussion", "showcase" }, ProhibitedContent = new() { "垃圾内容" }, BestPostingTime = "全天", Rationale = "ChatGPT 相关" }
            }
        };

        return subreddits.TryGetValue(industry, out var subs) ? subs : new List<SubredditRecommendation>();
    }

    public RedditRuleCheckResult CheckRedditRules(RedditRuleCheckRequest request)
    {
        var violations = new List<RuleViolation>();
        var warnings = new List<string>();
        var suggestions = new List<string>();

        // 检查标题
        if (request.Title.Length > 300)
        {
            violations.Add(new RuleViolation { Rule = "标题长度", Severity = "critical", Description = "标题超过 300 字符限制", Fix = "缩短标题" });
        }

        // 检查自我推广
        var selfPromoPatterns = new[] { "我的网站", "我的产品", "点击这里", "购买", "my website", "buy now", "click here" };
        foreach (var pattern in selfPromoPatterns)
        {
            if (request.Content.ToLower().Contains(pattern.ToLower()))
            {
                violations.Add(new RuleViolation { Rule = "自我推广", Severity = "critical", Description = $"检测到自我推广内容: {pattern}", Fix = "移除推广性语言，提供纯价值内容" });
            }
        }

        // 检查链接数量
        var linkCount = Regex.Matches(request.Content, @"https?://").Count;
        if (linkCount > 2)
        {
            warnings.Add($"包含 {linkCount} 个链接，可能被标记为垃圾内容");
        }

        // 建议
        if (request.Content.Length < 200)
        {
            suggestions.Add("内容较短，建议增加更多有价值的信息");
        }

        suggestions.Add("在发帖前先在该 subreddit 评论互动，建立声誉");
        suggestions.Add("使用问题形式的标题可以增加参与度");

        return new RedditRuleCheckResult
        {
            IsCompliant = violations.Count == 0,
            Violations = violations,
            Warnings = warnings,
            Suggestions = suggestions
        };
    }

    public RedditAccountPlanResult GenerateAccountPlan(RedditAccountPlanRequest request)
    {
        var dailyPlans = new List<DailyPlan>();
        var subreddits = request.TargetSubreddits.Count > 0 
            ? request.TargetSubreddits 
            : new List<string> { "r/AskReddit", "r/todayilearned", "r/technology" };

        for (int day = 1; day <= request.PlanDays; day++)
        {
            var focus = day switch
            {
                <= 3 => "观察和学习",
                <= 7 => "开始评论互动",
                <= 10 => "增加评论频率",
                _ => "准备发帖"
            };

            var tasks = new List<DailyTask>();

            if (day <= 3)
            {
                tasks.Add(new DailyTask { TaskType = "observe", Subreddit = subreddits[0], Description = "浏览热门帖子，了解社区风格", Count = 10 });
                tasks.Add(new DailyTask { TaskType = "upvote", Subreddit = subreddits[0], Description = "为有价值的内容点赞", Count = 5 });
            }
            else if (day <= 7)
            {
                tasks.Add(new DailyTask { TaskType = "comment", Subreddit = subreddits[day % subreddits.Count], Description = "发表有价值的评论", Count = 3 });
                tasks.Add(new DailyTask { TaskType = "upvote", Subreddit = subreddits[0], Description = "继续点赞互动", Count = 5 });
            }
            else if (day <= 10)
            {
                tasks.Add(new DailyTask { TaskType = "comment", Subreddit = subreddits[day % subreddits.Count], Description = "发表深度评论", Count = 5 });
                tasks.Add(new DailyTask { TaskType = "engage", Subreddit = subreddits[0], Description = "回复他人评论", Count = 3 });
            }
            else
            {
                tasks.Add(new DailyTask { TaskType = "comment", Subreddit = subreddits[day % subreddits.Count], Description = "继续评论互动", Count = 3 });
                tasks.Add(new DailyTask { TaskType = "post", Subreddit = subreddits[0], Description = "准备第一个帖子", Count = 1 });
            }

            dailyPlans.Add(new DailyPlan
            {
                Day = day,
                Focus = focus,
                Tasks = tasks,
                Goal = day <= 7 ? "积累 karma" : "建立声誉"
            });
        }

        return new RedditAccountPlanResult
        {
            DailyPlans = dailyPlans,
            GeneralTips = new List<string>
            {
                "永远不要在评论中推广自己的产品或服务",
                "提供真正有价值的信息和见解",
                "回复他人时保持友善和专业",
                "遵守每个 subreddit 的特定规则",
                "不要在多个 subreddit 发布相同内容"
            },
            KarmaGoal = "14 天后目标: 100+ comment karma",
            RecommendedSubreddits = subreddits
        };
    }

    #endregion

    #region 5.27-5.28 效果追踪

    public PlatformROIResult CalculatePlatformROI(PlatformROIRequest request)
    {
        var engagementRate = request.Views > 0 ? (double)request.Engagements / request.Views * 100 : 0;
        var ctr = request.Views > 0 ? (double)request.Clicks / request.Views * 100 : 0;
        var conversionRate = request.Clicks > 0 ? (double)request.Conversions / request.Clicks * 100 : 0;

        var totalCost = request.CostUSD + (request.TimeInvestedHours * 50); // 假设时间成本 $50/小时
        var cpe = request.Engagements > 0 ? totalCost / request.Engagements : 0;
        var cpc = request.Clicks > 0 ? totalCost / request.Clicks : 0;
        var cpConv = request.Conversions > 0 ? totalCost / request.Conversions : 0;

        // 计算 ROI 分数
        var roiScore = CalculateROIScore(engagementRate, ctr, conversionRate, cpe);
        var grade = roiScore >= 80 ? "A" : roiScore >= 60 ? "B" : roiScore >= 40 ? "C" : roiScore >= 20 ? "D" : "F";

        return new PlatformROIResult
        {
            Platform = request.Platform,
            EngagementRate = Math.Round(engagementRate, 2),
            ClickThroughRate = Math.Round(ctr, 2),
            ConversionRate = Math.Round(conversionRate, 2),
            CostPerEngagement = Math.Round(cpe, 2),
            CostPerClick = Math.Round(cpc, 2),
            CostPerConversion = Math.Round(cpConv, 2),
            ROIScore = roiScore,
            ROIGrade = grade,
            Insights = GenerateROIInsights(engagementRate, ctr, conversionRate, request.Platform),
            Recommendations = GenerateROIRecommendations(engagementRate, ctr, conversionRate)
        };
    }

    private double CalculateROIScore(double engagementRate, double ctr, double conversionRate, double cpe)
    {
        var score = 0.0;
        
        // 参与率评分 (权重 30%)
        score += Math.Min(30, engagementRate * 3);
        
        // CTR 评分 (权重 30%)
        score += Math.Min(30, ctr * 10);
        
        // 转化率评分 (权重 30%)
        score += Math.Min(30, conversionRate * 6);
        
        // 成本效率评分 (权重 10%)
        if (cpe > 0 && cpe < 1) score += 10;
        else if (cpe < 5) score += 5;

        return Math.Round(score, 1);
    }

    private List<string> GenerateROIInsights(double engagementRate, double ctr, double conversionRate, string platform)
    {
        var insights = new List<string>();

        if (engagementRate > 5)
            insights.Add($"参与率 {engagementRate}% 高于平均水平，内容质量良好");
        else if (engagementRate < 2)
            insights.Add($"参与率 {engagementRate}% 较低，需要优化内容吸引力");

        if (ctr > 3)
            insights.Add($"点击率 {ctr}% 表现优秀");
        else if (ctr < 1)
            insights.Add($"点击率 {ctr}% 较低，考虑优化 CTA");

        if (conversionRate > 5)
            insights.Add($"转化率 {conversionRate}% 非常出色");
        else if (conversionRate < 1)
            insights.Add($"转化率 {conversionRate}% 需要优化转化路径");

        return insights;
    }

    private List<string> GenerateROIRecommendations(double engagementRate, double ctr, double conversionRate)
    {
        var recs = new List<string>();

        if (engagementRate < 3)
            recs.Add("尝试更具互动性的内容格式（问题、投票、挑战）");
        
        if (ctr < 2)
            recs.Add("优化 CTA 文案和位置，使用更强的行动号召");
        
        if (conversionRate < 2)
            recs.Add("简化转化流程，减少步骤");

        recs.Add("持续 A/B 测试不同内容格式");
        recs.Add("分析高表现内容的共同特征");

        return recs;
    }

    public ContentLifecycleResult AnalyzeContentLifecycle(ContentLifecycleRequest request)
    {
        if (request.DailyMetrics.Count == 0)
        {
            return new ContentLifecycleResult
            {
                Platform = request.Platform,
                ContentType = request.ContentType,
                LifecycleStage = "unknown",
                Insights = new List<string> { "需要更多数据进行分析" }
            };
        }

        var peakDay = request.DailyMetrics.OrderByDescending(m => m.Views).First().DayNumber;
        var peakViews = request.DailyMetrics.Max(m => m.Views);
        var halfLifeViews = peakViews / 2;
        
        var halfLifeDay = request.DailyMetrics
            .Where(m => m.DayNumber > peakDay && m.Views <= halfLifeViews)
            .Select(m => m.DayNumber)
            .FirstOrDefault();

        var halfLifeDays = halfLifeDay > 0 ? halfLifeDay - peakDay : request.DailyMetrics.Count;
        
        var lastDay = request.DailyMetrics.Last();
        var decayRate = peakViews > 0 ? (1 - (double)lastDay.Views / peakViews) * 100 : 0;

        var stage = DetermineLifecycleStage(request.DailyMetrics, peakDay);

        return new ContentLifecycleResult
        {
            Platform = request.Platform,
            ContentType = request.ContentType,
            PeakDay = peakDay,
            HalfLifeDays = halfLifeDays,
            DecayRate = Math.Round(decayRate, 1),
            LifecycleStage = stage,
            Insights = GenerateLifecycleInsights(stage, halfLifeDays, decayRate),
            RepurposeSuggestion = GetRepurposeSuggestion(stage, request.ContentType)
        };
    }

    private string DetermineLifecycleStage(List<DailyMetric> metrics, int peakDay)
    {
        var lastMetric = metrics.Last();
        var peakMetric = metrics.First(m => m.DayNumber == peakDay);

        if (lastMetric.DayNumber < peakDay)
            return "growth";
        
        if (lastMetric.Views >= peakMetric.Views * 0.8)
            return "peak";
        
        if (lastMetric.Views >= peakMetric.Views * 0.2)
            return "decline";
        
        return "long_tail";
    }

    private List<string> GenerateLifecycleInsights(string stage, double halfLifeDays, double decayRate)
    {
        var insights = new List<string>();

        insights.Add(stage switch
        {
            "growth" => "内容仍在增长期，继续推广",
            "peak" => "内容处于高峰期，最大化曝光",
            "decline" => "内容进入衰退期，考虑复用",
            "long_tail" => "内容进入长尾期，可作为常青内容",
            _ => "需要更多数据分析"
        });

        if (halfLifeDays < 3)
            insights.Add("内容衰减较快，适合时效性平台");
        else if (halfLifeDays > 7)
            insights.Add("内容生命周期较长，适合作为常青内容");

        return insights;
    }

    private string GetRepurposeSuggestion(string stage, string contentType)
    {
        return stage switch
        {
            "decline" => "将高峰期数据制作成案例研究，或提取关键点制作短视频",
            "long_tail" => "更新内容并重新发布，或制作系列内容的续集",
            _ => "继续监测，等待最佳复用时机"
        };
    }

    #endregion
}
