using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace GeoCore.SaaS.Services.PlatformDependency;

/// <summary>
/// 平台依赖度分析服务
/// 基于 Barry Schwartz 20年周期观察：平台会周期性衰落（Yahoo Answers→Quora→Wikipedia→Reddit）
/// </summary>
public class PlatformDependencyService
{
    private readonly ILogger<PlatformDependencyService> _logger;
    private readonly ICitationMonitoringRepository? _citationRepository;

    private static readonly Dictionary<string, PlatformLifecycleConfig> PlatformConfigs = new()
    {
        ["chatgpt"] = new PlatformLifecycleConfig
        {
            Platform = "chatgpt",
            DisplayName = "ChatGPT",
            Stage = "growth",
            HealthScore = 95,
            RiskLevel = "low",
            TrendDirection = "rising",
            Notes = "市场领导者，持续增长"
        },
        ["perplexity"] = new PlatformLifecycleConfig
        {
            Platform = "perplexity",
            DisplayName = "Perplexity",
            Stage = "growth",
            HealthScore = 90,
            RiskLevel = "low",
            TrendDirection = "rising",
            Notes = "搜索型 AI，快速增长"
        },
        ["claude"] = new PlatformLifecycleConfig
        {
            Platform = "claude",
            DisplayName = "Claude",
            Stage = "growth",
            HealthScore = 88,
            RiskLevel = "low",
            TrendDirection = "rising",
            Notes = "企业级 AI，稳步增长"
        },
        ["gemini"] = new PlatformLifecycleConfig
        {
            Platform = "gemini",
            DisplayName = "Gemini",
            Stage = "growth",
            HealthScore = 85,
            RiskLevel = "low",
            TrendDirection = "rising",
            Notes = "Google 生态整合"
        },
        ["grok"] = new PlatformLifecycleConfig
        {
            Platform = "grok",
            DisplayName = "Grok",
            Stage = "early",
            HealthScore = 70,
            RiskLevel = "medium",
            TrendDirection = "rising",
            Notes = "X/Twitter 生态，用户基数有限"
        },
        ["google_ai"] = new PlatformLifecycleConfig
        {
            Platform = "google_ai",
            DisplayName = "Google AI Overview",
            Stage = "growth",
            HealthScore = 92,
            RiskLevel = "low",
            TrendDirection = "rising",
            Notes = "搜索引擎整合，流量巨大"
        },
        ["reddit"] = new PlatformLifecycleConfig
        {
            Platform = "reddit",
            DisplayName = "Reddit",
            Stage = "mature",
            HealthScore = 75,
            RiskLevel = "medium",
            TrendDirection = "stable",
            Notes = "当前被 AI 大量引用，但历史显示平台会周期性衰落"
        },
        ["wikipedia"] = new PlatformLifecycleConfig
        {
            Platform = "wikipedia",
            DisplayName = "Wikipedia",
            Stage = "mature",
            HealthScore = 80,
            RiskLevel = "low",
            TrendDirection = "stable",
            Notes = "权威来源，但编辑政策严格"
        }
    };

    public PlatformDependencyService(
        ILogger<PlatformDependencyService> logger,
        ICitationMonitoringRepository? citationRepository = null)
    {
        _logger = logger;
        _citationRepository = citationRepository;
    }

    /// <summary>
    /// 生成平台依赖度分析报告
    /// </summary>
    public async Task<PlatformDependencyReport> GenerateReportAsync(PlatformDependencyRequest request)
    {
        _logger.LogInformation("[PlatformDependency] Generating report for task {TaskId}, brand: {Brand}",
            request.TaskId, request.Brand);

        var report = new PlatformDependencyReport
        {
            TaskId = request.TaskId,
            Brand = request.Brand
        };

        // 获取引用数据
        var results = await GetCitationResultsAsync(request.TaskId);
        var brandResults = FilterByBrand(results, request.Brand);

        // 计算各平台曝光占比
        report.Platforms = CalculatePlatformExposure(brandResults, request.DependencyThreshold);

        // 计算分散度评分
        report.DiversificationScore = CalculateDiversificationScore(report.Platforms);
        report.DependencyLevel = GetDependencyLevel(report.DiversificationScore);

        // 生成警告
        report.Alerts = GenerateAlerts(report.Platforms, request.DependencyThreshold);

        // 生成分散策略建议
        if (request.IncludeStrategies)
        {
            report.Strategies = GenerateStrategies(report);
        }

        // 生成趋势分析
        if (request.IncludeTrends)
        {
            report.Trend = GenerateTrend(results, request.Brand);
        }

        // 生成摘要
        report.Summary = GenerateSummary(report);

        return report;
    }

    /// <summary>
    /// 计算各平台曝光占比
    /// </summary>
    public List<PlatformExposure> CalculatePlatformExposure(
        List<CitationResultEntity> results,
        double dependencyThreshold)
    {
        var exposures = new List<PlatformExposure>();
        var totalCitations = results.Count(r => r.IsCited);

        if (totalCitations == 0)
        {
            return exposures;
        }

        var platformGroups = results
            .Where(r => r.IsCited)
            .GroupBy(r => r.Platform.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var kvp in platformGroups.OrderByDescending(x => x.Value))
        {
            var platform = kvp.Key;
            var count = kvp.Value;
            var rate = (double)count / totalCitations;

            var config = GetPlatformConfig(platform);

            exposures.Add(new PlatformExposure
            {
                Platform = platform,
                DisplayName = config.DisplayName,
                CitationCount = count,
                ExposureRate = rate,
                IsOverDependent = rate > dependencyThreshold,
                HealthScore = config.HealthScore,
                LifecycleStage = config.Stage,
                RiskLevel = rate > dependencyThreshold ? "high" : config.RiskLevel,
                TrendDirection = config.TrendDirection
            });
        }

        return exposures;
    }

    /// <summary>
    /// 计算分散度评分（基于 Herfindahl-Hirschman Index 反向）
    /// </summary>
    public double CalculateDiversificationScore(List<PlatformExposure> platforms)
    {
        if (platforms.Count == 0)
        {
            return 0;
        }

        // 单一平台时，分散度为0
        if (platforms.Count == 1)
        {
            return 0;
        }

        // HHI = Σ(市场份额²)，范围 1/n 到 1
        // 分散度 = (1 - HHI) * 100，范围 0 到 100
        var hhi = platforms.Sum(p => Math.Pow(p.ExposureRate, 2));

        // 归一化：考虑平台数量
        var n = platforms.Count;
        var minHhi = 1.0 / n;
        var normalizedHhi = (hhi - minHhi) / (1 - minHhi);

        // 转换为分散度评分
        var diversificationScore = (1 - normalizedHhi) * 100;

        return Math.Max(0, Math.Min(100, diversificationScore));
    }

    /// <summary>
    /// 获取依赖度等级
    /// </summary>
    public string GetDependencyLevel(double diversificationScore)
    {
        return diversificationScore switch
        {
            >= 70 => "balanced",      // 分散良好
            >= 50 => "moderate",      // 中等依赖
            >= 30 => "concentrated",  // 集中
            _ => "critical"           // 严重依赖
        };
    }

    /// <summary>
    /// 生成依赖度警告
    /// </summary>
    public List<DependencyAlert> GenerateAlerts(
        List<PlatformExposure> platforms,
        double threshold)
    {
        var alerts = new List<DependencyAlert>();

        // 检查过度依赖单一平台
        var overDependentPlatforms = platforms.Where(p => p.ExposureRate > threshold).ToList();
        foreach (var platform in overDependentPlatforms)
        {
            var level = platform.ExposureRate > 0.7 ? "critical" : "warning";
            alerts.Add(new DependencyAlert
            {
                Type = "over_dependency",
                Level = level,
                Title = $"过度依赖 {platform.DisplayName}",
                Description = $"品牌在 {platform.DisplayName} 的曝光占比达到 {platform.ExposureRate:P0}，" +
                              $"超过 {threshold:P0} 的警戒线。平台会周期性衰落，需分散风险。",
                Platform = platform.Platform,
                CurrentRate = platform.ExposureRate,
                SuggestedAction = $"增加在其他平台的曝光，将 {platform.DisplayName} 占比降至 {threshold:P0} 以下"
            });
        }

        // 检查平台健康度风险
        var riskyPlatforms = platforms.Where(p => p.HealthScore < 75 && p.ExposureRate > 0.2).ToList();
        foreach (var platform in riskyPlatforms)
        {
            alerts.Add(new DependencyAlert
            {
                Type = "platform_risk",
                Level = "warning",
                Title = $"{platform.DisplayName} 平台风险",
                Description = $"{platform.DisplayName} 健康度评分 {platform.HealthScore}，" +
                              $"但品牌曝光占比 {platform.ExposureRate:P0}。建议关注平台动态。",
                Platform = platform.Platform,
                CurrentRate = platform.ExposureRate,
                SuggestedAction = "监控平台变化，准备备选方案"
            });
        }

        // 检查曝光过于分散（可能效率低）
        if (platforms.Count > 5 && platforms.All(p => p.ExposureRate < 0.15))
        {
            alerts.Add(new DependencyAlert
            {
                Type = "too_dispersed",
                Level = "info",
                Title = "曝光过于分散",
                Description = "品牌在多个平台的曝光都较低，可能导致资源分散、效率降低。",
                Platform = "",
                CurrentRate = 0,
                SuggestedAction = "考虑聚焦 2-3 个核心平台，提高单平台影响力"
            });
        }

        // 检查缺失重要平台
        var importantPlatforms = new[] { "chatgpt", "perplexity", "google_ai" };
        var presentPlatforms = platforms.Select(p => p.Platform).ToHashSet();
        var missingPlatforms = importantPlatforms.Where(p => !presentPlatforms.Contains(p)).ToList();

        if (missingPlatforms.Any())
        {
            var config = GetPlatformConfig(missingPlatforms.First());
            alerts.Add(new DependencyAlert
            {
                Type = "missing_platform",
                Level = "info",
                Title = "缺失重要平台曝光",
                Description = $"品牌在 {string.Join("、", missingPlatforms.Select(p => GetPlatformConfig(p).DisplayName))} 缺少曝光。",
                Platform = missingPlatforms.First(),
                CurrentRate = 0,
                SuggestedAction = "考虑优化内容以获得这些平台的引用"
            });
        }

        return alerts.OrderByDescending(a => a.Level == "critical" ? 3 : a.Level == "warning" ? 2 : 1).ToList();
    }

    /// <summary>
    /// 生成分散策略建议
    /// </summary>
    public List<DiversificationStrategy> GenerateStrategies(PlatformDependencyReport report)
    {
        var strategies = new List<DiversificationStrategy>();

        // 找出过度依赖的平台
        var overDependentPlatforms = report.Platforms.Where(p => p.IsOverDependent).ToList();

        // 找出曝光不足的平台
        var underExposedPlatforms = PlatformConfigs.Values
            .Where(c => c.HealthScore >= 80)
            .Where(c => !report.Platforms.Any(p => p.Platform == c.Platform && p.ExposureRate > 0.1))
            .ToList();

        // 策略1：减少对过度依赖平台的依赖
        foreach (var platform in overDependentPlatforms)
        {
            strategies.Add(new DiversificationStrategy
            {
                Type = "reduce_dependency",
                Title = $"降低 {platform.DisplayName} 依赖度",
                Description = $"当前 {platform.DisplayName} 占比 {platform.ExposureRate:P0}，需要分散到其他平台。",
                TargetPlatform = platform.Platform,
                Priority = platform.ExposureRate > 0.7 ? 10 : 8,
                ExpectedOutcome = $"将 {platform.DisplayName} 占比降至 40% 以下",
                Effort = "medium",
                ActionSteps = new List<string>
                {
                    "分析其他平台的内容偏好和引用模式",
                    "创建适合其他平台的内容版本",
                    "监控新平台的引用效果",
                    "逐步调整内容分发策略"
                },
                SuggestedTimeline = "2-4 周"
            });
        }

        // 策略2：增加高健康度平台曝光
        foreach (var config in underExposedPlatforms.Take(3))
        {
            strategies.Add(new DiversificationStrategy
            {
                Type = "increase_exposure",
                Title = $"增加 {config.DisplayName} 曝光",
                Description = $"{config.DisplayName} 健康度 {config.HealthScore}，是优质的曝光渠道。",
                TargetPlatform = config.Platform,
                Priority = (int)(config.HealthScore / 10),
                ExpectedOutcome = $"在 {config.DisplayName} 获得 10%+ 的曝光占比",
                Effort = GetEffortForPlatform(config.Platform),
                ActionSteps = GetActionStepsForPlatform(config.Platform),
                SuggestedTimeline = "3-6 周"
            });
        }

        // 策略3：多渠道内容分发
        if (report.DiversificationScore < 60)
        {
            strategies.Add(new DiversificationStrategy
            {
                Type = "multi_channel",
                Title = "建立多渠道内容分发体系",
                Description = "一次创作，多平台分发（Create Once, Distribute Forever）",
                TargetPlatform = "",
                Priority = 9,
                ExpectedOutcome = "实现 5+ 平台均衡曝光",
                Effort = "high",
                ActionSteps = new List<string>
                {
                    "建立核心内容库（长文/视频）",
                    "为每个平台创建适配版本",
                    "制定内容日历和发布计划",
                    "监控各平台效果并优化"
                },
                SuggestedTimeline = "1-2 个月"
            });
        }

        // 策略4：官网内容强化
        if (!report.Platforms.Any(p => p.Platform == "official_site" && p.ExposureRate > 0.15))
        {
            strategies.Add(new DiversificationStrategy
            {
                Type = "official_site",
                Title = "强化官网内容权威性",
                Description = "官网是最可控的曝光渠道，不受平台周期影响。",
                TargetPlatform = "official_site",
                Priority = 8,
                ExpectedOutcome = "官网内容被 AI 直接引用",
                Effort = "medium",
                ActionSteps = new List<string>
                {
                    "优化官网内容结构（H1-H3、Schema）",
                    "添加 llms.txt 文件",
                    "创建高质量 FAQ 和 How-to 内容",
                    "确保内容定期更新（每 3-6 个月）"
                },
                SuggestedTimeline = "2-4 周"
            });
        }

        return strategies.OrderByDescending(s => s.Priority).ToList();
    }

    /// <summary>
    /// 生成趋势分析
    /// </summary>
    public DependencyTrend GenerateTrend(List<CitationResultEntity> allResults, string brand)
    {
        var trend = new DependencyTrend();

        // 按周分组
        var weeklyGroups = allResults
            .Where(r => r.IsCited && ContainsBrand(r, brand))
            .GroupBy(r => GetWeekStart(r.CreatedAt))
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var week in weeklyGroups)
        {
            var platforms = CalculatePlatformExposure(week.ToList(), 0.5);
            var score = CalculateDiversificationScore(platforms);
            var dominant = platforms.OrderByDescending(p => p.ExposureRate).FirstOrDefault();

            trend.DataPoints.Add(new DependencyDataPoint
            {
                Date = week.Key,
                DiversificationScore = score,
                DominantPlatform = dominant?.DisplayName ?? "",
                DominantRate = dominant?.ExposureRate ?? 0
            });
        }

        // 计算趋势方向
        if (trend.DataPoints.Count >= 2)
        {
            var recent = trend.DataPoints.TakeLast(2).ToList();
            var change = recent[1].DiversificationScore - recent[0].DiversificationScore;
            trend.DiversificationChange = change;

            trend.Direction = change switch
            {
                > 5 => "improving",
                < -5 => "declining",
                _ => "stable"
            };

            trend.Description = trend.Direction switch
            {
                "improving" => $"分散度正在改善，上升 {change:F1} 分",
                "declining" => $"分散度正在下降，下降 {Math.Abs(change):F1} 分，需关注",
                _ => "分散度保持稳定"
            };
        }

        return trend;
    }

    /// <summary>
    /// 生成报告摘要
    /// </summary>
    public string GenerateSummary(PlatformDependencyReport report)
    {
        var parts = new List<string>();

        // 分散度评价
        var levelDesc = report.DependencyLevel switch
        {
            "balanced" => "曝光分布均衡",
            "moderate" => "存在一定集中度",
            "concentrated" => "曝光较为集中",
            _ => "曝光严重集中"
        };
        parts.Add($"品牌 {report.Brand} 的平台分散度评分 {report.DiversificationScore:F0}/100，{levelDesc}。");

        // 主要平台
        var topPlatforms = report.Platforms.Take(3).ToList();
        if (topPlatforms.Any())
        {
            var platformDesc = string.Join("、",
                topPlatforms.Select(p => $"{p.DisplayName}({p.ExposureRate:P0})"));
            parts.Add($"主要曝光平台：{platformDesc}。");
        }

        // 警告数量
        var criticalCount = report.Alerts.Count(a => a.Level == "critical");
        var warningCount = report.Alerts.Count(a => a.Level == "warning");
        if (criticalCount > 0 || warningCount > 0)
        {
            parts.Add($"发现 {criticalCount} 个严重警告、{warningCount} 个一般警告。");
        }

        // 建议
        if (report.Strategies.Any())
        {
            var topStrategy = report.Strategies.First();
            parts.Add($"首要建议：{topStrategy.Title}。");
        }

        return string.Join(" ", parts);
    }

    #region Helper Methods

    private async Task<List<CitationResultEntity>> GetCitationResultsAsync(int taskId)
    {
        if (_citationRepository == null)
        {
            return new List<CitationResultEntity>();
        }

        return await _citationRepository.GetResultsByTaskIdAsync(taskId, 1000);
    }

    private List<CitationResultEntity> FilterByBrand(List<CitationResultEntity> results, string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            return results;
        }

        return results.Where(r => ContainsBrand(r, brand)).ToList();
    }

    private bool ContainsBrand(CitationResultEntity result, string brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            return true;
        }

        var response = result.Response ?? "";
        return response.Contains(brand, StringComparison.OrdinalIgnoreCase);
    }

    private PlatformLifecycleConfig GetPlatformConfig(string platform)
    {
        var key = platform.ToLowerInvariant();
        return PlatformConfigs.TryGetValue(key, out var config)
            ? config
            : new PlatformLifecycleConfig
            {
                Platform = platform,
                DisplayName = platform,
                Stage = "unknown",
                HealthScore = 50,
                RiskLevel = "medium"
            };
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private string GetEffortForPlatform(string platform)
    {
        return platform switch
        {
            "chatgpt" or "claude" => "medium",
            "perplexity" => "low",
            "google_ai" => "high",
            "reddit" => "medium",
            _ => "medium"
        };
    }

    private List<string> GetActionStepsForPlatform(string platform)
    {
        return platform switch
        {
            "chatgpt" => new List<string>
            {
                "优化内容结构，便于 AI 提取",
                "增加权威引用和数据支撑",
                "确保内容新鲜度（393-458天最佳）",
                "添加 Schema 标记"
            },
            "perplexity" => new List<string>
            {
                "在 Reddit 等社区增加讨论",
                "创建带链接的高质量内容",
                "确保官网 SEO 优化",
                "参与行业论坛讨论"
            },
            "google_ai" => new List<string>
            {
                "优化 Google 搜索排名",
                "添加 FAQ 和 HowTo Schema",
                "确保移动端体验",
                "提高页面加载速度"
            },
            "reddit" => new List<string>
            {
                "在相关 subreddit 提供价值",
                "避免直接推广，分享知识",
                "建立社区声誉",
                "参与 AMA 等活动"
            },
            _ => new List<string>
            {
                "分析平台内容偏好",
                "创建适合该平台的内容",
                "监控引用效果",
                "持续优化"
            }
        };
    }

    #endregion
}
