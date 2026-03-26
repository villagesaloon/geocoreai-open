using System.Text.RegularExpressions;

namespace GeoCore.SaaS.Services.CitationBenchmark;

public class CitationBenchmarkService
{
    #region 4.42 分平台引用基准

    private static readonly Dictionary<string, PlatformBenchmark> PlatformBenchmarks = new()
    {
        ["chatgpt"] = new PlatformBenchmark
        {
            Platform = "chatgpt",
            DisplayName = "ChatGPT",
            AverageCitationRate = 0.35,
            TypicalCitationCount = 3,
            PreferredSourceTypes = new() { "Wikipedia", "官方文档", "学术论文", "权威媒体" },
            ContentPreferences = new() { "结构化内容", "清晰定义", "步骤说明", "事实陈述" },
            StructureBenchmark = new()
            {
                OptimalParagraphLength = 150,
                OptimalSectionCount = 5,
                PrefersBulletPoints = true,
                PrefersNumberedLists = true,
                PrefersTables = false,
                OptimalHeadingDepth = 3
            },
            UpdateFrequencyPreference = "6-12个月",
            Notes = "偏好权威来源，对维基百科有明显偏好"
        },
        ["perplexity"] = new PlatformBenchmark
        {
            Platform = "perplexity",
            DisplayName = "Perplexity",
            AverageCitationRate = 0.85,
            TypicalCitationCount = 8,
            PreferredSourceTypes = new() { "新闻网站", "博客", "论坛", "社交媒体", "官方网站" },
            ContentPreferences = new() { "最新内容", "多角度观点", "实时信息", "用户生成内容" },
            StructureBenchmark = new()
            {
                OptimalParagraphLength = 120,
                OptimalSectionCount = 4,
                PrefersBulletPoints = true,
                PrefersNumberedLists = false,
                PrefersTables = true,
                OptimalHeadingDepth = 2
            },
            UpdateFrequencyPreference = "实时",
            Notes = "引用最多，偏好新鲜内容，会引用Reddit/论坛"
        },
        ["gemini"] = new PlatformBenchmark
        {
            Platform = "gemini",
            DisplayName = "Google Gemini",
            AverageCitationRate = 0.45,
            TypicalCitationCount = 5,
            PreferredSourceTypes = new() { "Google索引页面", "YouTube", "学术来源", "新闻" },
            ContentPreferences = new() { "多模态内容", "视频", "图表", "结构化数据" },
            StructureBenchmark = new()
            {
                OptimalParagraphLength = 140,
                OptimalSectionCount = 6,
                PrefersBulletPoints = true,
                PrefersNumberedLists = true,
                PrefersTables = true,
                OptimalHeadingDepth = 3
            },
            UpdateFrequencyPreference = "1-3个月",
            Notes = "与多模态内容相关性 r=0.92"
        },
        ["claude"] = new PlatformBenchmark
        {
            Platform = "claude",
            DisplayName = "Claude",
            AverageCitationRate = 0.25,
            TypicalCitationCount = 2,
            PreferredSourceTypes = new() { "学术论文", "技术文档", "书籍", "权威报告" },
            ContentPreferences = new() { "深度分析", "逻辑论证", "专业术语", "长篇内容" },
            StructureBenchmark = new()
            {
                OptimalParagraphLength = 180,
                OptimalSectionCount = 7,
                PrefersBulletPoints = false,
                PrefersNumberedLists = true,
                PrefersTables = false,
                OptimalHeadingDepth = 4
            },
            UpdateFrequencyPreference = "3-6个月",
            Notes = "偏好深度、专业内容"
        },
        ["grok"] = new PlatformBenchmark
        {
            Platform = "grok",
            DisplayName = "Grok",
            AverageCitationRate = 0.55,
            TypicalCitationCount = 4,
            PreferredSourceTypes = new() { "X/Twitter", "新闻", "博客", "社交媒体" },
            ContentPreferences = new() { "实时信息", "热点话题", "观点内容", "简洁表达" },
            StructureBenchmark = new()
            {
                OptimalParagraphLength = 100,
                OptimalSectionCount = 3,
                PrefersBulletPoints = true,
                PrefersNumberedLists = false,
                PrefersTables = false,
                OptimalHeadingDepth = 2
            },
            UpdateFrequencyPreference = "实时",
            Notes = "与X平台深度整合"
        },
        ["copilot"] = new PlatformBenchmark
        {
            Platform = "copilot",
            DisplayName = "Microsoft Copilot",
            AverageCitationRate = 0.65,
            TypicalCitationCount = 6,
            PreferredSourceTypes = new() { "Bing索引页面", "Microsoft文档", "LinkedIn", "新闻" },
            ContentPreferences = new() { "专业内容", "商业信息", "技术文档", "结构化数据" },
            StructureBenchmark = new()
            {
                OptimalParagraphLength = 130,
                OptimalSectionCount = 5,
                PrefersBulletPoints = true,
                PrefersNumberedLists = true,
                PrefersTables = true,
                OptimalHeadingDepth = 3
            },
            UpdateFrequencyPreference = "1-2周",
            Notes = "与Bing搜索整合"
        }
    };

    public List<PlatformBenchmark> GetAllBenchmarks()
    {
        return PlatformBenchmarks.Values.ToList();
    }

    public PlatformBenchmark? GetBenchmark(string platform)
    {
        return PlatformBenchmarks.TryGetValue(platform.ToLower(), out var benchmark) ? benchmark : null;
    }

    #endregion

    #region 4.43 内容结构评分

    public ContentStructureResult AnalyzeContentStructure(ContentStructureRequest request)
    {
        var content = request.Content;
        var platform = request.TargetPlatform.ToLower();
        var benchmark = GetBenchmark(platform) ?? PlatformBenchmarks["chatgpt"];

        var paragraphAnalysis = AnalyzeParagraphs(content, benchmark.StructureBenchmark.OptimalParagraphLength);
        var headingAnalysis = AnalyzeHeadings(content);
        var listAnalysis = AnalyzeLists(content);

        var issues = new List<StructureIssue>();
        var recommendations = new List<string>();

        // 检查段落问题
        if (paragraphAnalysis.TooLongParagraphs > 0)
        {
            issues.Add(new StructureIssue
            {
                Type = "paragraph_too_long",
                Severity = "warning",
                Description = $"有 {paragraphAnalysis.TooLongParagraphs} 个段落超过 180 词",
                Fix = "将长段落拆分为 120-180 词的小段落，可提升 70% 引用率"
            });
        }

        if (paragraphAnalysis.TooShortParagraphs > paragraphAnalysis.TotalParagraphs / 2)
        {
            issues.Add(new StructureIssue
            {
                Type = "paragraph_too_short",
                Severity = "info",
                Description = "多数段落过短，信息密度不足",
                Fix = "合并相关短段落，确保每段有完整的信息单元"
            });
        }

        // 检查标题问题
        if (headingAnalysis.H1Count == 0)
        {
            issues.Add(new StructureIssue
            {
                Type = "missing_h1",
                Severity = "critical",
                Description = "缺少 H1 标题",
                Fix = "添加一个清晰的 H1 标题"
            });
        }

        if (!headingAnalysis.HasProperHierarchy)
        {
            issues.Add(new StructureIssue
            {
                Type = "heading_hierarchy",
                Severity = "warning",
                Description = "标题层级不规范",
                Fix = "确保标题层级从 H1 → H2 → H3 递进"
            });
        }

        // 检查列表使用
        if (benchmark.StructureBenchmark.PrefersBulletPoints && listAnalysis.BulletListCount == 0)
        {
            recommendations.Add($"{benchmark.DisplayName} 偏好项目符号列表，建议添加要点列表");
        }

        if (benchmark.StructureBenchmark.PrefersTables && listAnalysis.TableCount == 0)
        {
            recommendations.Add($"{benchmark.DisplayName} 偏好表格数据，建议将对比信息转为表格");
        }

        // 计算总分
        var overallScore = (paragraphAnalysis.Score * 0.4 + headingAnalysis.Score * 0.3 + listAnalysis.Score * 0.3);

        return new ContentStructureResult
        {
            OverallScore = Math.Round(overallScore, 1),
            Grade = GetGrade(overallScore),
            ParagraphAnalysis = paragraphAnalysis,
            HeadingAnalysis = headingAnalysis,
            ListAnalysis = listAnalysis,
            Issues = issues,
            Recommendations = recommendations
        };
    }

    private ParagraphAnalysis AnalyzeParagraphs(string content, int optimalLength)
    {
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var wordCounts = paragraphs.Select(p => CountWords(p)).ToList();

        var optimal = wordCounts.Count(w => w >= 120 && w <= 180);
        var tooShort = wordCounts.Count(w => w < 50);
        var tooLong = wordCounts.Count(w => w > 200);

        var avgLength = wordCounts.Count > 0 ? wordCounts.Average() : 0;
        var score = Math.Min(100, (optimal * 100.0 / Math.Max(1, wordCounts.Count)) + 
                   (avgLength >= 100 && avgLength <= 180 ? 20 : 0));

        return new ParagraphAnalysis
        {
            TotalParagraphs = paragraphs.Length,
            AverageLength = Math.Round(avgLength, 1),
            OptimalParagraphs = optimal,
            TooShortParagraphs = tooShort,
            TooLongParagraphs = tooLong,
            Score = Math.Round(score, 1)
        };
    }

    private HeadingAnalysis AnalyzeHeadings(string content)
    {
        var h1 = Regex.Matches(content, @"^#\s+.+$|<h1[^>]*>.+</h1>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        var h2 = Regex.Matches(content, @"^##\s+.+$|<h2[^>]*>.+</h2>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        var h3 = Regex.Matches(content, @"^###\s+.+$|<h3[^>]*>.+</h3>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;

        var hasProperHierarchy = h1 >= 1 && (h2 == 0 || h1 <= h2) && (h3 == 0 || h2 <= h3);
        var score = (h1 >= 1 ? 40 : 0) + (h2 >= 2 ? 30 : h2 * 15) + (hasProperHierarchy ? 30 : 10);

        return new HeadingAnalysis
        {
            H1Count = h1,
            H2Count = h2,
            H3Count = h3,
            HasProperHierarchy = hasProperHierarchy,
            Score = Math.Min(100, score)
        };
    }

    private ListAnalysis AnalyzeLists(string content)
    {
        var bullets = Regex.Matches(content, @"^[\-\*]\s+.+$|<li[^>]*>.+</li>", RegexOptions.Multiline).Count;
        var numbered = Regex.Matches(content, @"^\d+\.\s+.+$", RegexOptions.Multiline).Count;
        var tables = Regex.Matches(content, @"<table|^\|.+\|$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;

        var score = Math.Min(100, (bullets > 0 ? 40 : 0) + (numbered > 0 ? 30 : 0) + (tables > 0 ? 30 : 0));

        return new ListAnalysis
        {
            BulletListCount = bullets,
            NumberedListCount = numbered,
            TableCount = tables,
            Score = score
        };
    }

    #endregion

    #region 4.44 多模态内容检测

    public MultimodalAnalysisResult AnalyzeMultimodal(MultimodalAnalysisRequest request)
    {
        var content = request.Content;

        var imageAnalysis = AnalyzeImages(content);
        var videoAnalysis = AnalyzeVideos(content);
        var tableAnalysis = AnalyzeTablesMultimodal(content);
        var chartAnalysis = AnalyzeCharts(content);

        var recommendations = new List<string>();

        if (imageAnalysis.ImageCount == 0)
        {
            recommendations.Add("添加相关图片可提升 Google AI 引用概率（r=0.92 相关性）");
        }
        else if (imageAnalysis.ImagesWithAlt < imageAnalysis.ImageCount)
        {
            recommendations.Add($"有 {imageAnalysis.ImageCount - imageAnalysis.ImagesWithAlt} 张图片缺少 alt 属性");
        }

        if (videoAnalysis.VideoCount == 0)
        {
            recommendations.Add("考虑添加视频内容，YouTube 视频对 Gemini 有加成");
        }

        if (tableAnalysis.TableCount == 0)
        {
            recommendations.Add("结构化数据（表格）可提升 AI 提取准确度");
        }

        var multimodalScore = (imageAnalysis.Score * 0.35 + videoAnalysis.Score * 0.25 + 
                               tableAnalysis.Score * 0.25 + chartAnalysis.Score * 0.15);

        return new MultimodalAnalysisResult
        {
            MultimodalScore = Math.Round(multimodalScore, 1),
            ImageAnalysis = imageAnalysis,
            VideoAnalysis = videoAnalysis,
            TableAnalysis = tableAnalysis,
            ChartAnalysis = chartAnalysis,
            GoogleAICorrelation = 0.92,
            Recommendations = recommendations
        };
    }

    private ImageAnalysis AnalyzeImages(string content)
    {
        var imgMatches = Regex.Matches(content, @"<img[^>]*>|!\[([^\]]*)\]\([^)]+\)", RegexOptions.IgnoreCase);
        var withAlt = Regex.Matches(content, @"<img[^>]*alt=[""'][^""']+[""'][^>]*>|!\[[^\]]+\]\([^)]+\)", RegexOptions.IgnoreCase).Count;
        var withCaption = Regex.Matches(content, @"<figcaption|<figure[^>]*>.*?<img", RegexOptions.IgnoreCase | RegexOptions.Singleline).Count;

        var issues = new List<string>();
        if (imgMatches.Count > 0 && withAlt < imgMatches.Count)
        {
            issues.Add($"{imgMatches.Count - withAlt} 张图片缺少 alt 属性");
        }

        var score = imgMatches.Count > 0 
            ? Math.Min(100, 40 + (withAlt * 30.0 / imgMatches.Count) + (withCaption * 30.0 / imgMatches.Count))
            : 0;

        return new ImageAnalysis
        {
            ImageCount = imgMatches.Count,
            ImagesWithAlt = withAlt,
            ImagesWithCaption = withCaption,
            Score = Math.Round(score, 1),
            Issues = issues
        };
    }

    private VideoAnalysis AnalyzeVideos(string content)
    {
        var videos = Regex.Matches(content, @"<video|<iframe[^>]*youtube|<iframe[^>]*vimeo", RegexOptions.IgnoreCase).Count;
        var withTranscript = Regex.Matches(content, @"transcript|字幕|文字稿", RegexOptions.IgnoreCase).Count > 0 ? videos : 0;

        var score = videos > 0 ? Math.Min(100, 50 + (withTranscript > 0 ? 50 : 0)) : 0;

        return new VideoAnalysis
        {
            VideoCount = videos,
            VideosWithTranscript = withTranscript,
            Score = score
        };
    }

    private TableAnalysis AnalyzeTablesMultimodal(string content)
    {
        var tables = Regex.Matches(content, @"<table|^\|.+\|.+\|$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;
        var withHeaders = Regex.Matches(content, @"<th|^\|[\s\-:]+\|$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Count;

        var score = tables > 0 ? Math.Min(100, 50 + (withHeaders > 0 ? 50 : 0)) : 0;

        return new TableAnalysis
        {
            TableCount = tables,
            TablesWithHeaders = Math.Min(tables, withHeaders),
            Score = score
        };
    }

    private ChartAnalysis AnalyzeCharts(string content)
    {
        var charts = Regex.Matches(content, @"<canvas|<svg[^>]*chart|chart\.js|echarts|d3\.js", RegexOptions.IgnoreCase).Count;
        var withDesc = Regex.Matches(content, @"aria-label|<desc>|图表说明", RegexOptions.IgnoreCase).Count;

        var score = charts > 0 ? Math.Min(100, 50 + (withDesc > 0 ? 50 : 0)) : 0;

        return new ChartAnalysis
        {
            ChartCount = charts,
            ChartsWithDescription = Math.Min(charts, withDesc),
            Score = score
        };
    }

    #endregion

    #region 4.45 实体密度分析

    public EntityDensityResult AnalyzeEntityDensity(EntityDensityRequest request)
    {
        var content = request.Content;
        var wordCount = CountWords(content);

        var entities = ExtractEntities(content);
        var totalEntities = entities.Count;
        var density = wordCount > 0 ? (totalEntities * 100.0 / wordCount) : 0;

        // 15+ 实体 → 4.8x 选中率
        var multiplier = totalEntities >= 15 ? 4.8 : (totalEntities >= 10 ? 2.5 : (totalEntities >= 5 ? 1.5 : 1.0));

        var breakdown = new EntityBreakdown
        {
            PersonCount = entities.Count(e => e.Type == "PERSON"),
            OrganizationCount = entities.Count(e => e.Type == "ORG"),
            LocationCount = entities.Count(e => e.Type == "LOC"),
            ProductCount = entities.Count(e => e.Type == "PRODUCT"),
            DateCount = entities.Count(e => e.Type == "DATE"),
            NumberCount = entities.Count(e => e.Type == "NUMBER"),
            OtherCount = entities.Count(e => e.Type == "OTHER")
        };

        var score = Math.Min(100, totalEntities * 5 + (density > 3 ? 20 : 0));
        var recommendations = new List<string>();

        if (totalEntities < 15)
        {
            recommendations.Add($"当前 {totalEntities} 个实体，增加到 15+ 可获得 4.8x 选中率提升");
        }
        if (breakdown.NumberCount < 3)
        {
            recommendations.Add("添加更多统计数据和数字，可提升事实密度");
        }
        if (breakdown.PersonCount == 0)
        {
            recommendations.Add("添加专家引用或人物引述，可提升权威性");
        }

        return new EntityDensityResult
        {
            TotalEntities = totalEntities,
            EntityDensity = Math.Round(density, 2),
            SelectionMultiplier = multiplier,
            Breakdown = breakdown,
            TopEntities = entities.OrderByDescending(e => e.Frequency).Take(10).ToList(),
            Score = Math.Round((double)score, 1),
            Grade = GetGrade(score),
            Recommendations = recommendations
        };
    }

    private List<ExtractedEntity> ExtractEntities(string content)
    {
        var entities = new List<ExtractedEntity>();

        // 提取人名（简化版）
        var personPatterns = new[] { 
            @"([A-Z][a-z]+\s+[A-Z][a-z]+)",
            @"([\u4e00-\u9fa5]{2,4}(?:教授|博士|先生|女士|总|院士))"
        };
        foreach (var pattern in personPatterns)
        {
            foreach (Match m in Regex.Matches(content, pattern))
            {
                entities.Add(new ExtractedEntity { Text = m.Value, Type = "PERSON", Frequency = 1, Relevance = 0.8 });
            }
        }

        // 提取组织
        var orgPatterns = new[] {
            @"([A-Z][a-z]*(?:\s+[A-Z][a-z]*)*\s+(?:Inc|Corp|Ltd|LLC|Company|University|Institute))",
            @"([\u4e00-\u9fa5]+(?:公司|集团|大学|研究院|机构|组织))"
        };
        foreach (var pattern in orgPatterns)
        {
            foreach (Match m in Regex.Matches(content, pattern))
            {
                entities.Add(new ExtractedEntity { Text = m.Value, Type = "ORG", Frequency = 1, Relevance = 0.9 });
            }
        }

        // 提取数字/统计
        var numberPattern = @"(\d+(?:\.\d+)?%|\d+(?:,\d{3})*(?:\.\d+)?(?:\s*(?:万|亿|million|billion|thousand))?)";
        foreach (Match m in Regex.Matches(content, numberPattern))
        {
            entities.Add(new ExtractedEntity { Text = m.Value, Type = "NUMBER", Frequency = 1, Relevance = 0.7 });
        }

        // 提取日期
        var datePattern = @"(\d{4}年\d{1,2}月|\d{4}-\d{2}-\d{2}|\d{1,2}/\d{1,2}/\d{4}|January|February|March|April|May|June|July|August|September|October|November|December\s+\d{4})";
        foreach (Match m in Regex.Matches(content, datePattern, RegexOptions.IgnoreCase))
        {
            entities.Add(new ExtractedEntity { Text = m.Value, Type = "DATE", Frequency = 1, Relevance = 0.6 });
        }

        // 合并重复实体
        return entities
            .GroupBy(e => e.Text.ToLower())
            .Select(g => new ExtractedEntity
            {
                Text = g.First().Text,
                Type = g.First().Type,
                Frequency = g.Count(),
                Relevance = g.Max(e => e.Relevance)
            })
            .ToList();
    }

    #endregion

    #region 综合分析

    public ComprehensiveBenchmarkResult AnalyzeComprehensive(ComprehensiveBenchmarkRequest request)
    {
        var structureResult = AnalyzeContentStructure(new ContentStructureRequest
        {
            Content = request.Content,
            TargetPlatform = request.TargetPlatforms.FirstOrDefault() ?? "chatgpt"
        });

        var multimodalResult = AnalyzeMultimodal(new MultimodalAnalysisRequest
        {
            Content = request.Content,
            Url = request.Url
        });

        var entityResult = AnalyzeEntityDensity(new EntityDensityRequest
        {
            Content = request.Content
        });

        var platformFits = request.TargetPlatforms.Select(p => CalculatePlatformFit(p, structureResult, multimodalResult, entityResult)).ToList();
        if (platformFits.Count == 0)
        {
            platformFits = PlatformBenchmarks.Keys.Select(p => CalculatePlatformFit(p, structureResult, multimodalResult, entityResult)).ToList();
        }

        var overallScore = (structureResult.OverallScore * 0.4 + multimodalResult.MultimodalScore * 0.3 + entityResult.Score * 0.3);

        var topRecommendations = structureResult.Recommendations
            .Concat(multimodalResult.Recommendations)
            .Concat(entityResult.Recommendations)
            .Take(5)
            .ToList();

        return new ComprehensiveBenchmarkResult
        {
            OverallScore = Math.Round(overallScore, 1),
            Grade = GetGrade(overallScore),
            StructureAnalysis = structureResult,
            MultimodalAnalysis = multimodalResult,
            EntityAnalysis = entityResult,
            PlatformFitScores = platformFits.OrderByDescending(p => p.FitScore).ToList(),
            TopRecommendations = topRecommendations
        };
    }

    private PlatformFitScore CalculatePlatformFit(string platform, ContentStructureResult structure, MultimodalAnalysisResult multimodal, EntityDensityResult entity)
    {
        var benchmark = GetBenchmark(platform);
        if (benchmark == null) return new PlatformFitScore { Platform = platform, FitScore = 50 };

        var strengths = new List<string>();
        var weaknesses = new List<string>();

        // 段落长度匹配
        var paragraphDiff = Math.Abs(structure.ParagraphAnalysis.AverageLength - benchmark.StructureBenchmark.OptimalParagraphLength);
        if (paragraphDiff < 30) strengths.Add("段落长度匹配");
        else weaknesses.Add($"段落长度偏离最优值 {benchmark.StructureBenchmark.OptimalParagraphLength} 词");

        // 列表偏好
        if (benchmark.StructureBenchmark.PrefersBulletPoints && structure.ListAnalysis.BulletListCount > 0)
            strengths.Add("使用了项目符号列表");
        if (benchmark.StructureBenchmark.PrefersTables && structure.ListAnalysis.TableCount > 0)
            strengths.Add("使用了表格");

        // 多模态（Gemini 特别看重）
        if (platform == "gemini" && multimodal.MultimodalScore > 60)
            strengths.Add("多模态内容丰富");

        // 实体密度
        if (entity.TotalEntities >= 15)
            strengths.Add("实体密度高 (4.8x 选中率)");
        else if (entity.TotalEntities < 10)
            weaknesses.Add("实体密度不足");

        var fitScore = 50 + (strengths.Count * 12) - (weaknesses.Count * 8);
        fitScore = Math.Max(20, Math.Min(100, fitScore));

        return new PlatformFitScore
        {
            Platform = platform,
            DisplayName = benchmark.DisplayName,
            FitScore = fitScore,
            FitLevel = fitScore >= 80 ? "excellent" : fitScore >= 60 ? "good" : fitScore >= 40 ? "fair" : "poor",
            Strengths = strengths,
            Weaknesses = weaknesses
        };
    }

    #endregion

    #region 4.42 分平台引用基准（扩展）

    private static readonly Dictionary<string, PlatformCitationMetrics> PlatformMetrics = new()
    {
        ["chatgpt"] = new PlatformCitationMetrics
        {
            AverageCitationsPerResponse = 3.2,
            CitationDiversity = 0.45,
            FreshnessWeight = 0.25,
            AuthorityWeight = 0.50,
            RelevanceWeight = 0.25,
            UpdateFrequency = "6-12个月",
            TypicalSourceCount = 3
        },
        ["perplexity"] = new PlatformCitationMetrics
        {
            AverageCitationsPerResponse = 8.5,
            CitationDiversity = 0.85,
            FreshnessWeight = 0.50,
            AuthorityWeight = 0.20,
            RelevanceWeight = 0.30,
            UpdateFrequency = "实时",
            TypicalSourceCount = 8
        },
        ["gemini"] = new PlatformCitationMetrics
        {
            AverageCitationsPerResponse = 5.0,
            CitationDiversity = 0.60,
            FreshnessWeight = 0.35,
            AuthorityWeight = 0.35,
            RelevanceWeight = 0.30,
            UpdateFrequency = "1-3个月",
            TypicalSourceCount = 5
        },
        ["claude"] = new PlatformCitationMetrics
        {
            AverageCitationsPerResponse = 2.0,
            CitationDiversity = 0.30,
            FreshnessWeight = 0.15,
            AuthorityWeight = 0.60,
            RelevanceWeight = 0.25,
            UpdateFrequency = "3-6个月",
            TypicalSourceCount = 2
        },
        ["grok"] = new PlatformCitationMetrics
        {
            AverageCitationsPerResponse = 4.0,
            CitationDiversity = 0.70,
            FreshnessWeight = 0.55,
            AuthorityWeight = 0.15,
            RelevanceWeight = 0.30,
            UpdateFrequency = "实时",
            TypicalSourceCount = 4
        },
        ["copilot"] = new PlatformCitationMetrics
        {
            AverageCitationsPerResponse = 6.0,
            CitationDiversity = 0.65,
            FreshnessWeight = 0.40,
            AuthorityWeight = 0.35,
            RelevanceWeight = 0.25,
            UpdateFrequency = "1-2周",
            TypicalSourceCount = 6
        }
    };

    private static readonly Dictionary<string, List<PlatformTopSource>> PlatformTopSources = new()
    {
        ["chatgpt"] = new List<PlatformTopSource>
        {
            new() { Rank = 1, SourceType = "Wikipedia", Description = "维基百科占 ChatGPT 引用的 47.9%", CitationShare = 0.479, Actionability = "low", Examples = new() { "en.wikipedia.org", "zh.wikipedia.org" } },
            new() { Rank = 2, SourceType = "官方文档", Description = "技术文档和官方指南", CitationShare = 0.15, Actionability = "high", Examples = new() { "docs.microsoft.com", "developer.apple.com" } },
            new() { Rank = 3, SourceType = "学术论文", Description = "学术期刊和研究论文", CitationShare = 0.12, Actionability = "medium", Examples = new() { "arxiv.org", "scholar.google.com" } },
            new() { Rank = 4, SourceType = "权威媒体", Description = "主流新闻和行业媒体", CitationShare = 0.10, Actionability = "medium", Examples = new() { "nytimes.com", "bbc.com" } }
        },
        ["perplexity"] = new List<PlatformTopSource>
        {
            new() { Rank = 1, SourceType = "新闻网站", Description = "实时新闻和热点报道", CitationShare = 0.25, Actionability = "medium", Examples = new() { "reuters.com", "apnews.com" } },
            new() { Rank = 2, SourceType = "Reddit", Description = "Reddit 讨论（6.1x 引用倍数）", CitationShare = 0.18, Actionability = "high", Examples = new() { "reddit.com/r/technology", "reddit.com/r/science" } },
            new() { Rank = 3, SourceType = "博客", Description = "专业博客和个人网站", CitationShare = 0.15, Actionability = "high", Examples = new() { "medium.com", "substack.com" } },
            new() { Rank = 4, SourceType = "官方网站", Description = "品牌官网和产品页面", CitationShare = 0.12, Actionability = "high", Examples = new() { "company.com/blog", "brand.com/resources" } }
        },
        ["gemini"] = new List<PlatformTopSource>
        {
            new() { Rank = 1, SourceType = "Google 索引", Description = "Google 搜索结果优先", CitationShare = 0.35, Actionability = "high", Examples = new() { "高排名页面", "Featured Snippets" } },
            new() { Rank = 2, SourceType = "YouTube", Description = "视频内容（16% 社交引用）", CitationShare = 0.16, Actionability = "high", Examples = new() { "youtube.com", "视频转录" } },
            new() { Rank = 3, SourceType = "学术来源", Description = "Google Scholar 索引内容", CitationShare = 0.14, Actionability = "medium", Examples = new() { "scholar.google.com", "学术期刊" } },
            new() { Rank = 4, SourceType = "新闻", Description = "Google News 索引内容", CitationShare = 0.12, Actionability = "medium", Examples = new() { "news.google.com", "主流媒体" } }
        },
        ["claude"] = new List<PlatformTopSource>
        {
            new() { Rank = 1, SourceType = "学术论文", Description = "深度研究和学术内容", CitationShare = 0.30, Actionability = "medium", Examples = new() { "arxiv.org", "学术期刊" } },
            new() { Rank = 2, SourceType = "技术文档", Description = "详细技术规范和文档", CitationShare = 0.25, Actionability = "high", Examples = new() { "官方文档", "技术白皮书" } },
            new() { Rank = 3, SourceType = "书籍", Description = "专业书籍和教材", CitationShare = 0.20, Actionability = "low", Examples = new() { "O'Reilly", "专业出版社" } },
            new() { Rank = 4, SourceType = "权威报告", Description = "行业报告和研究", CitationShare = 0.15, Actionability = "medium", Examples = new() { "Gartner", "McKinsey" } }
        },
        ["grok"] = new List<PlatformTopSource>
        {
            new() { Rank = 1, SourceType = "X/Twitter", Description = "X 平台实时内容", CitationShare = 0.40, Actionability = "high", Examples = new() { "x.com", "Twitter 讨论" } },
            new() { Rank = 2, SourceType = "新闻", Description = "实时新闻报道", CitationShare = 0.20, Actionability = "medium", Examples = new() { "突发新闻", "热点事件" } },
            new() { Rank = 3, SourceType = "Reddit", Description = "Reddit 讨论（2.3x 引用倍数）", CitationShare = 0.15, Actionability = "high", Examples = new() { "reddit.com", "热门讨论" } },
            new() { Rank = 4, SourceType = "博客", Description = "观点和评论内容", CitationShare = 0.10, Actionability = "high", Examples = new() { "个人博客", "专栏" } }
        },
        ["copilot"] = new List<PlatformTopSource>
        {
            new() { Rank = 1, SourceType = "Bing 索引", Description = "Bing 搜索结果优先", CitationShare = 0.30, Actionability = "high", Examples = new() { "Bing 高排名页面", "Bing Webmaster" } },
            new() { Rank = 2, SourceType = "LinkedIn", Description = "B2B 专业内容", CitationShare = 0.18, Actionability = "high", Examples = new() { "linkedin.com/pulse", "LinkedIn 文章" } },
            new() { Rank = 3, SourceType = "Microsoft 文档", Description = "Microsoft 生态内容", CitationShare = 0.15, Actionability = "high", Examples = new() { "docs.microsoft.com", "learn.microsoft.com" } },
            new() { Rank = 4, SourceType = "新闻", Description = "MSN 新闻和主流媒体", CitationShare = 0.12, Actionability = "medium", Examples = new() { "msn.com", "主流媒体" } }
        }
    };

    private static readonly Dictionary<string, PlatformContentGuidelines> PlatformGuidelines = new()
    {
        ["chatgpt"] = new PlatformContentGuidelines
        {
            OptimalWordCount = 2500,
            OptimalParagraphLength = 150,
            TonePreference = "权威、客观、教育性",
            MustHaveElements = new() { "清晰定义", "步骤说明", "事实陈述", "引用来源" },
            AvoidElements = new() { "过度营销", "主观观点", "未经验证的声明" },
            StructurePreference = "层级清晰的 H2/H3 结构，带编号列表"
        },
        ["perplexity"] = new PlatformContentGuidelines
        {
            OptimalWordCount = 2000,
            OptimalParagraphLength = 120,
            TonePreference = "新鲜、多角度、实用",
            MustHaveElements = new() { "最新日期", "多来源观点", "实时数据", "用户评价" },
            AvoidElements = new() { "过时信息", "单一视角", "缺乏时效性" },
            StructurePreference = "简洁段落，项目符号列表，表格对比"
        },
        ["gemini"] = new PlatformContentGuidelines
        {
            OptimalWordCount = 3000,
            OptimalParagraphLength = 140,
            TonePreference = "多模态、视觉化、结构化",
            MustHaveElements = new() { "图片", "视频", "表格", "Schema 标记" },
            AvoidElements = new() { "纯文本", "缺乏视觉元素", "无结构化数据" },
            StructurePreference = "多模态内容，FAQ Schema，视频嵌入"
        },
        ["claude"] = new PlatformContentGuidelines
        {
            OptimalWordCount = 4000,
            OptimalParagraphLength = 180,
            TonePreference = "深度、专业、逻辑严谨",
            MustHaveElements = new() { "深度分析", "逻辑论证", "专业术语", "学术引用" },
            AvoidElements = new() { "浅显内容", "缺乏深度", "过于简化" },
            StructurePreference = "长篇深度内容，编号列表，详细论证"
        },
        ["grok"] = new PlatformContentGuidelines
        {
            OptimalWordCount = 1500,
            OptimalParagraphLength = 100,
            TonePreference = "实时、简洁、观点鲜明",
            MustHaveElements = new() { "实时信息", "热点关联", "简洁表达", "社交证据" },
            AvoidElements = new() { "过长内容", "过时信息", "缺乏观点" },
            StructurePreference = "简短段落，项目符号，社交媒体风格"
        },
        ["copilot"] = new PlatformContentGuidelines
        {
            OptimalWordCount = 2500,
            OptimalParagraphLength = 130,
            TonePreference = "专业、商业、实用",
            MustHaveElements = new() { "专业内容", "商业价值", "技术细节", "LinkedIn 优化" },
            AvoidElements = new() { "非专业内容", "缺乏商业价值", "过于休闲" },
            StructurePreference = "专业结构，表格数据，B2B 导向"
        }
    };

    public PlatformBenchmarkDetailResult GetPlatformBenchmarkDetail(PlatformBenchmarkDetailRequest request)
    {
        var platform = request.Platform.ToLower();
        var benchmark = GetBenchmark(platform);
        
        if (benchmark == null)
        {
            return new PlatformBenchmarkDetailResult
            {
                Platform = platform,
                DisplayName = platform,
                OptimizationTips = new() { "未找到该平台的基准数据" }
            };
        }

        var metrics = PlatformMetrics.GetValueOrDefault(platform, new PlatformCitationMetrics());
        var topSources = PlatformTopSources.GetValueOrDefault(platform, new List<PlatformTopSource>());
        var guidelines = PlatformGuidelines.GetValueOrDefault(platform, new PlatformContentGuidelines());

        var tips = GeneratePlatformOptimizationTips(platform, benchmark, metrics);

        return new PlatformBenchmarkDetailResult
        {
            Platform = platform,
            DisplayName = benchmark.DisplayName,
            Benchmark = benchmark,
            CitationMetrics = metrics,
            TopSources = topSources,
            ContentGuidelines = guidelines,
            OptimizationTips = tips
        };
    }

    public AllPlatformBenchmarksResult GetAllPlatformBenchmarks()
    {
        var summaries = PlatformBenchmarks.Values.Select(b => new PlatformBenchmarkSummary
        {
            Platform = b.Platform,
            DisplayName = b.DisplayName,
            CitationRate = b.AverageCitationRate,
            TypicalCitations = b.TypicalCitationCount,
            TopSourceType = b.PreferredSourceTypes.FirstOrDefault() ?? "",
            FreshnessPreference = b.UpdateFrequencyPreference,
            ContentStyle = b.ContentPreferences.FirstOrDefault() ?? ""
        }).ToList();

        var comparisonMatrix = new PlatformComparisonMatrix
        {
            Platforms = PlatformBenchmarks.Keys.ToList(),
            OverlapRates = new Dictionary<string, Dictionary<string, double>>
            {
                ["chatgpt"] = new() { ["perplexity"] = 0.11, ["gemini"] = 0.25, ["claude"] = 0.30, ["grok"] = 0.08 },
                ["perplexity"] = new() { ["chatgpt"] = 0.11, ["gemini"] = 0.18, ["claude"] = 0.12, ["grok"] = 0.22 },
                ["gemini"] = new() { ["chatgpt"] = 0.25, ["perplexity"] = 0.18, ["claude"] = 0.20, ["grok"] = 0.15 },
                ["claude"] = new() { ["chatgpt"] = 0.30, ["perplexity"] = 0.12, ["gemini"] = 0.20, ["grok"] = 0.10 },
                ["grok"] = new() { ["chatgpt"] = 0.08, ["perplexity"] = 0.22, ["gemini"] = 0.15, ["claude"] = 0.10 }
            },
            Insight = "各平台引用重叠率仅 11%，需要针对每个平台制定差异化策略"
        };

        var crossPlatformTips = new List<string>
        {
            "Perplexity 偏好新鲜内容，Claude 偏好深度内容 - 同一主题需要不同版本",
            "Reddit 对 Perplexity (6.1x) 和 Grok (2.3x) 有显著加成",
            "YouTube 是 Gemini 的重要引用源（16%），考虑视频内容策略",
            "LinkedIn 对 Copilot 有加成，B2B 品牌应优先",
            "Wikipedia 风格内容对 ChatGPT 最有效（47.9% 引用）"
        };

        return new AllPlatformBenchmarksResult
        {
            Platforms = summaries,
            ComparisonMatrix = comparisonMatrix,
            CrossPlatformTips = crossPlatformTips
        };
    }

    private List<string> GeneratePlatformOptimizationTips(string platform, PlatformBenchmark benchmark, PlatformCitationMetrics metrics)
    {
        var tips = new List<string>();

        switch (platform)
        {
            case "chatgpt":
                tips.Add("采用 Wikipedia 风格：中立语气、引用丰富、结构化");
                tips.Add("确保内容有明确的定义和步骤说明");
                tips.Add("添加权威来源引用，提升可信度");
                break;
            case "perplexity":
                tips.Add("保持内容新鲜，添加最新日期标记");
                tips.Add("在 Reddit 相关社区建立存在感（6.1x 引用倍数）");
                tips.Add("提供多角度观点和实时数据");
                break;
            case "gemini":
                tips.Add("添加图片、视频等多模态内容（r=0.92 相关性）");
                tips.Add("确保 Google 搜索排名，Gemini 优先引用高排名页面");
                tips.Add("使用 FAQ Schema 和结构化数据");
                break;
            case "claude":
                tips.Add("提供深度分析和详细论证");
                tips.Add("使用专业术语和学术引用");
                tips.Add("内容长度可以更长（4000+ 词）");
                break;
            case "grok":
                tips.Add("保持内容简洁，关联热点话题");
                tips.Add("在 X/Twitter 建立存在感");
                tips.Add("Reddit 参与也有帮助（2.3x 引用倍数）");
                break;
            case "copilot":
                tips.Add("优化 Bing 搜索排名");
                tips.Add("在 LinkedIn 发布专业内容");
                tips.Add("B2B 导向，强调商业价值");
                break;
        }

        return tips;
    }

    #endregion

    #region 4.47 平台偏好差异化

    private static readonly Dictionary<string, string> PlatformCorePreferences = new()
    {
        ["chatgpt"] = "权威性 - 偏好 Wikipedia 风格的权威、结构化内容",
        ["perplexity"] = "新鲜度 - 偏好实时、多来源、最新发布的内容",
        ["gemini"] = "多模态 - 偏好图片、视频、结构化数据的多模态内容",
        ["claude"] = "深度 - 偏好深度分析、专业论证、学术风格内容",
        ["grok"] = "实时性 - 偏好 X/Twitter 实时内容和热点话题",
        ["copilot"] = "专业性 - 偏好 B2B 专业内容和 LinkedIn 来源"
    };

    public PlatformPreferenceDiffResult AnalyzePlatformPreferenceDiff(PlatformPreferenceDiffRequest request)
    {
        var platforms = request.TargetPlatforms.Count > 0 
            ? request.TargetPlatforms 
            : PlatformBenchmarks.Keys.ToList();

        var structureResult = AnalyzeContentStructure(new ContentStructureRequest
        {
            Content = request.Content,
            TargetPlatform = platforms.FirstOrDefault() ?? "chatgpt"
        });

        var multimodalResult = AnalyzeMultimodal(new MultimodalAnalysisRequest
        {
            Content = request.Content
        });

        var entityResult = AnalyzeEntityDensity(new EntityDensityRequest
        {
            Content = request.Content
        });

        var platformStrategies = platforms.Select(p => GeneratePlatformStrategy(
            p, request.Content, structureResult, multimodalResult, entityResult
        )).ToList();

        var differentiationMatrix = GenerateDifferentiationMatrix(platforms);
        var contentVariations = GenerateContentVariations(platforms, request.Content);

        var unifiedRecommendations = GenerateUnifiedRecommendations(platformStrategies);

        return new PlatformPreferenceDiffResult
        {
            PlatformStrategies = platformStrategies,
            DifferentiationMatrix = differentiationMatrix,
            ContentVariations = contentVariations,
            UnifiedRecommendations = unifiedRecommendations
        };
    }

    private PlatformSpecificStrategy GeneratePlatformStrategy(
        string platform,
        string content,
        ContentStructureResult structure,
        MultimodalAnalysisResult multimodal,
        EntityDensityResult entity)
    {
        var benchmark = GetBenchmark(platform);
        if (benchmark == null)
        {
            return new PlatformSpecificStrategy { Platform = platform };
        }

        var fitScore = CalculatePlatformFit(platform, structure, multimodal, entity);
        var corePreference = PlatformCorePreferences.GetValueOrDefault(platform, "通用");

        var strengths = new List<string>();
        var weaknesses = new List<string>();
        var actions = new List<PlatformOptimizationAction>();

        // 根据平台特性分析
        switch (platform)
        {
            case "chatgpt":
                if (entity.TotalEntities >= 15) strengths.Add("实体密度高，符合 Wikipedia 风格");
                else
                {
                    weaknesses.Add("实体密度不足");
                    actions.Add(new PlatformOptimizationAction
                    {
                        Action = "增加实体密度到 15+",
                        Reason = "ChatGPT 偏好 Wikipedia 风格的事实密集内容",
                        Priority = "high",
                        ImpactScore = 4.8
                    });
                }
                if (structure.HeadingAnalysis.HasProperHierarchy) strengths.Add("标题层级规范");
                break;

            case "perplexity":
                if (content.Contains("2026") || content.Contains("2025") || content.Contains("最新"))
                    strengths.Add("包含时效性标记");
                else
                {
                    weaknesses.Add("缺乏时效性标记");
                    actions.Add(new PlatformOptimizationAction
                    {
                        Action = "添加年份和更新日期",
                        Reason = "Perplexity 偏好新鲜内容，90 天内 2x 引用率",
                        Priority = "high",
                        ImpactScore = 2.0
                    });
                }
                actions.Add(new PlatformOptimizationAction
                {
                    Action = "在相关 Reddit 社区建立存在感",
                    Reason = "Reddit 对 Perplexity 有 6.1x 引用倍数",
                    Priority = "high",
                    ImpactScore = 6.1
                });
                break;

            case "gemini":
                if (multimodal.MultimodalScore >= 60) strengths.Add("多模态内容丰富");
                else
                {
                    weaknesses.Add("多模态内容不足");
                    actions.Add(new PlatformOptimizationAction
                    {
                        Action = "添加图片和视频内容",
                        Reason = "Gemini 与多模态内容相关性 r=0.92",
                        Priority = "high",
                        ImpactScore = 3.5
                    });
                }
                actions.Add(new PlatformOptimizationAction
                {
                    Action = "优化 Google 搜索排名",
                    Reason = "Gemini 优先引用 Google 高排名页面",
                    Priority = "medium",
                    ImpactScore = 2.5
                });
                break;

            case "claude":
                if (structure.ParagraphAnalysis.AverageLength >= 150) strengths.Add("段落长度适合深度内容");
                else
                {
                    weaknesses.Add("段落过短，深度不足");
                    actions.Add(new PlatformOptimizationAction
                    {
                        Action = "扩展段落长度到 150-180 词",
                        Reason = "Claude 偏好深度、专业的长篇内容",
                        Priority = "medium",
                        ImpactScore = 1.8
                    });
                }
                actions.Add(new PlatformOptimizationAction
                {
                    Action = "添加学术引用和专业术语",
                    Reason = "Claude 偏好学术风格内容",
                    Priority = "medium",
                    ImpactScore = 1.5
                });
                break;

            case "grok":
                if (structure.ParagraphAnalysis.AverageLength <= 120) strengths.Add("段落简洁");
                else
                {
                    weaknesses.Add("段落过长");
                    actions.Add(new PlatformOptimizationAction
                    {
                        Action = "缩短段落到 100 词以内",
                        Reason = "Grok 偏好简洁、实时的内容",
                        Priority = "medium",
                        ImpactScore = 1.5
                    });
                }
                actions.Add(new PlatformOptimizationAction
                {
                    Action = "在 X/Twitter 建立存在感",
                    Reason = "Grok 与 X 平台深度整合",
                    Priority = "high",
                    ImpactScore = 3.0
                });
                break;

            case "copilot":
                actions.Add(new PlatformOptimizationAction
                {
                    Action = "在 LinkedIn 发布专业内容",
                    Reason = "LinkedIn 对 Copilot 有加成",
                    Priority = "high",
                    ImpactScore = 2.5
                });
                actions.Add(new PlatformOptimizationAction
                {
                    Action = "优化 Bing 搜索排名",
                    Reason = "Copilot 优先引用 Bing 索引内容",
                    Priority = "medium",
                    ImpactScore = 2.0
                });
                break;
        }

        var expectedImpact = actions.Count > 0
            ? $"预计可提升 {actions.Max(a => a.ImpactScore):F1}x 引用率"
            : "当前内容已较好匹配该平台";

        return new PlatformSpecificStrategy
        {
            Platform = platform,
            DisplayName = benchmark.DisplayName,
            CorePreference = corePreference,
            CurrentFitScore = fitScore.FitScore,
            StrengthsForPlatform = strengths,
            WeaknessesForPlatform = weaknesses,
            OptimizationActions = actions.OrderByDescending(a => a.ImpactScore).ToList(),
            ExpectedImpact = expectedImpact
        };
    }

    private PlatformDifferentiationMatrix GenerateDifferentiationMatrix(List<string> platforms)
    {
        var dimensions = new List<DifferentiationDimension>
        {
            new()
            {
                Dimension = "内容新鲜度",
                PlatformValues = new()
                {
                    ["chatgpt"] = "6-12个月",
                    ["perplexity"] = "实时",
                    ["gemini"] = "1-3个月",
                    ["claude"] = "3-6个月",
                    ["grok"] = "实时",
                    ["copilot"] = "1-2周"
                },
                Recommendation = "为 Perplexity/Grok 准备实时版本，为 Claude 准备深度版本"
            },
            new()
            {
                Dimension = "内容深度",
                PlatformValues = new()
                {
                    ["chatgpt"] = "中等",
                    ["perplexity"] = "浅-中",
                    ["gemini"] = "中等",
                    ["claude"] = "深度",
                    ["grok"] = "浅",
                    ["copilot"] = "中等"
                },
                Recommendation = "Claude 需要 4000+ 词深度内容，Grok 需要 1500 词简洁版本"
            },
            new()
            {
                Dimension = "首选来源",
                PlatformValues = new()
                {
                    ["chatgpt"] = "Wikipedia",
                    ["perplexity"] = "Reddit/新闻",
                    ["gemini"] = "YouTube/Google",
                    ["claude"] = "学术论文",
                    ["grok"] = "X/Twitter",
                    ["copilot"] = "LinkedIn/Bing"
                },
                Recommendation = "根据目标平台选择内容分发渠道"
            },
            new()
            {
                Dimension = "内容格式",
                PlatformValues = new()
                {
                    ["chatgpt"] = "结构化/列表",
                    ["perplexity"] = "项目符号/表格",
                    ["gemini"] = "多模态/视频",
                    ["claude"] = "长篇论证",
                    ["grok"] = "简短/社交",
                    ["copilot"] = "专业/B2B"
                },
                Recommendation = "同一内容需要多种格式版本"
            }
        };

        return new PlatformDifferentiationMatrix
        {
            Dimensions = dimensions.Where(d => 
                d.PlatformValues.Keys.Any(k => platforms.Contains(k))
            ).ToList(),
            KeyInsight = "各平台引用重叠率仅 11%，差异化策略是关键"
        };
    }

    private List<ContentVariation> GenerateContentVariations(List<string> platforms, string content)
    {
        var variations = new List<ContentVariation>();

        if (platforms.Contains("perplexity") && platforms.Contains("claude"))
        {
            variations.Add(new ContentVariation
            {
                Platform = "perplexity vs claude",
                VariationType = "深度差异",
                OriginalApproach = "统一深度内容",
                OptimizedApproach = "Perplexity: 简洁版 + 最新日期; Claude: 深度版 + 学术引用",
                Rationale = "Perplexity 偏新鲜，Claude 偏深度"
            });
        }

        if (platforms.Contains("gemini"))
        {
            variations.Add(new ContentVariation
            {
                Platform = "gemini",
                VariationType = "多模态增强",
                OriginalApproach = "纯文本内容",
                OptimizedApproach = "添加图片、视频、FAQ Schema",
                Rationale = "Gemini 与多模态内容相关性 r=0.92"
            });
        }

        if (platforms.Contains("grok"))
        {
            variations.Add(new ContentVariation
            {
                Platform = "grok",
                VariationType = "社交优化",
                OriginalApproach = "长篇内容",
                OptimizedApproach = "简短版本 + X/Twitter 发布",
                Rationale = "Grok 与 X 平台深度整合"
            });
        }

        if (platforms.Contains("copilot"))
        {
            variations.Add(new ContentVariation
            {
                Platform = "copilot",
                VariationType = "B2B 优化",
                OriginalApproach = "通用内容",
                OptimizedApproach = "LinkedIn 文章 + 专业术语 + 商业价值",
                Rationale = "Copilot 偏好 LinkedIn 和 B2B 内容"
            });
        }

        return variations;
    }

    private List<string> GenerateUnifiedRecommendations(List<PlatformSpecificStrategy> strategies)
    {
        var recommendations = new List<string>();

        // 找出共同的高优先级行动
        var allActions = strategies.SelectMany(s => s.OptimizationActions).ToList();
        
        if (allActions.Any(a => a.Action.Contains("Reddit")))
        {
            recommendations.Add("Reddit 策略：对 Perplexity (6.1x) 和 Grok (2.3x) 都有显著加成");
        }

        if (allActions.Any(a => a.Action.Contains("多模态") || a.Action.Contains("图片")))
        {
            recommendations.Add("多模态内容：对 Gemini 有 r=0.92 相关性，也有助于其他平台");
        }

        if (allActions.Any(a => a.Action.Contains("时效") || a.Action.Contains("年份")))
        {
            recommendations.Add("时效性：90 天内内容 2x 引用率，年份更新 +71%");
        }

        recommendations.Add("差异化策略：各平台引用重叠率仅 11%，需要针对性优化");
        recommendations.Add("优先级：先优化 Perplexity（引用最多），再优化 ChatGPT（用户最多）");

        return recommendations;
    }

    #endregion

    #region 辅助方法

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        
        // 中文按字符计数，英文按空格分词
        var chineseCount = Regex.Matches(text, @"[\u4e00-\u9fa5]").Count;
        var englishWords = Regex.Matches(text, @"\b[a-zA-Z]+\b").Count;
        
        return chineseCount + englishWords;
    }

    private string GetGrade(double score)
    {
        return score >= 90 ? "A+" : score >= 80 ? "A" : score >= 70 ? "B" : score >= 60 ? "C" : score >= 50 ? "D" : "F";
    }

    #endregion
}
