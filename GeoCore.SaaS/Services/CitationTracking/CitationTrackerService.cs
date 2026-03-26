using System.Text.Json;
using GeoCore.Data.Entities;
using GeoCore.Data.Repositories;
using GeoCore.SaaS.Services.CitationTracking.Models;

namespace GeoCore.SaaS.Services.CitationTracking;

/// <summary>
/// 引用追踪服务
/// </summary>
public class CitationTrackerService
{
    private readonly ILogger<CitationTrackerService> _logger;
    private readonly CitationMonitoringRepository _repository;
    private readonly IEnumerable<IPlatformAdapter> _adapters;
    private readonly ICitationAnalyzer _citationAnalyzer;

    public CitationTrackerService(
        ILogger<CitationTrackerService> logger,
        CitationMonitoringRepository repository,
        IEnumerable<IPlatformAdapter> adapters,
        ICitationAnalyzer citationAnalyzer)
    {
        _logger = logger;
        _repository = repository;
        _adapters = adapters;
        _citationAnalyzer = citationAnalyzer;
    }

    /// <summary>
    /// 创建监测任务
    /// </summary>
    public async Task<int> CreateTaskAsync(MonitoringTaskRequest request)
    {
        var task = new CitationMonitoringTaskEntity
        {
            ProjectId = request.ProjectId ?? "",
            BrandName = request.BrandName,
            BrandAliases = JsonSerializer.Serialize(request.BrandAliases),
            Competitors = JsonSerializer.Serialize(request.Competitors),
            Questions = JsonSerializer.Serialize(request.Questions),
            TargetPlatforms = JsonSerializer.Serialize(request.TargetPlatforms.Select(p => p.ToString().ToLower())),
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var taskId = await _repository.CreateTaskAsync(task);
        _logger.LogInformation("[CitationTracker] Created task {TaskId} for brand {Brand}", taskId, request.BrandName);
        
        return taskId;
    }

    /// <summary>
    /// 执行监测任务
    /// </summary>
    public async Task<MonitoringSummary> RunTaskAsync(int taskId, CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            throw new ArgumentException($"Task {taskId} not found");
        }

        // 创建执行记录
        var run = new CitationMonitoringRunEntity
        {
            TaskId = taskId,
            StartedAt = DateTime.UtcNow,
            Status = "running",
            TotalQuestions = 0,
            CompletedQuestions = 0
        };
        var runId = await _repository.CreateRunAsync(run);

        // 更新任务状态
        task.Status = "running";
        task.LastRunAt = DateTime.UtcNow;
        await _repository.UpdateTaskAsync(task);

        var summary = new MonitoringSummary
        {
            TaskId = taskId,
            RunId = runId,
            StartedAt = run.StartedAt
        };

        try
        {
            // 解析配置
            var brandAliases = JsonSerializer.Deserialize<List<string>>(task.BrandAliases ?? "[]") ?? new();
            var competitors = JsonSerializer.Deserialize<List<string>>(task.Competitors ?? "[]") ?? new();
            var questions = JsonSerializer.Deserialize<List<string>>(task.Questions) ?? new();
            var targetPlatforms = JsonSerializer.Deserialize<List<string>>(task.TargetPlatforms) ?? new();

            run.TotalQuestions = questions.Count * targetPlatforms.Count;
            await _repository.UpdateRunAsync(run);
            summary.TotalQueries = run.TotalQuestions;

            // 获取可用的适配器
            var adapters = _adapters
                .Where(a => a.IsAvailable && targetPlatforms.Contains(a.Platform.ToString().ToLower()))
                .ToList();

            _logger.LogInformation(
                "[CitationTracker] Running task {TaskId} with {Questions} questions on {Platforms} platforms",
                taskId, questions.Count, adapters.Count);

            var results = new List<CitationResultEntity>();
            var resultLock = new object();

            // 构建所有查询任务（问题 x 平台）
            var queryTasks = new List<(string Question, IPlatformAdapter Adapter)>();
            foreach (var question in questions)
            {
                foreach (var adapter in adapters)
                {
                    queryTasks.Add((question, adapter));
                }
            }

            _logger.LogInformation(
                "[CitationTracker] Starting {Count} concurrent queries for task {TaskId}",
                queryTasks.Count, taskId);

            // 并发执行所有查询（限制并发数避免过载）
            var semaphore = new SemaphoreSlim(6); // 最多 6 个并发请求
            var tasks = queryTasks.Select(async qt =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var (question, adapter) = qt;

                    try
                    {
                        // 查询平台
                        var response = await adapter.QueryAsync(question, cancellationToken);
                        
                        // 分析引用
                        var analysis = await adapter.AnalyzeCitationAsync(
                            response, task.BrandName, brandAliases, competitors, cancellationToken);

                        // 保存结果
                        var result = new CitationResultEntity
                        {
                            RunId = runId,
                            TaskId = taskId,
                            Platform = adapter.Platform.ToString().ToLower(),
                            Question = question,
                            Response = response.Response,
                            IsCited = analysis.IsCited,
                            CitationPosition = analysis.Position.ToString().ToLower(),
                            PositionRatio = analysis.PositionRatio,
                            HasLink = analysis.HasLink,
                            DetectedLink = analysis.DetectedLink,
                            CitationContext = analysis.CitationContext,
                            Sentiment = analysis.Sentiment.ToString().ToLower(),
                            SentimentScore = analysis.SentimentScore,
                            VisibilityScore = analysis.VisibilityScore,
                            WordCountRatio = analysis.WordCountRatio,
                            CitationWordCount = analysis.CitationWordCount,
                            TotalWordCount = analysis.TotalWordCount,
                            PositionAdjustedScore = analysis.PositionAdjustedScore,
                            CompetitorCitations = JsonSerializer.Serialize(analysis.CompetitorCitations),
                            ResponseTimeMs = response.ResponseTimeMs,
                            ApiCost = response.ApiCost,
                            CreatedAt = DateTime.UtcNow
                        };

                        lock (resultLock)
                        {
                            results.Add(result);
                            run.CompletedQuestions++;

                            // 添加到汇总
                            summary.Results.Add(new CitationResultDetail
                            {
                                Platform = adapter.Platform,
                                Question = question,
                                ResponsePreview = response.Response.Length > 200 
                                    ? response.Response.Substring(0, 200) + "..." 
                                    : response.Response,
                                IsCited = analysis.IsCited,
                                Position = analysis.Position,
                                HasLink = analysis.HasLink,
                                Sentiment = analysis.Sentiment,
                                VisibilityScore = analysis.VisibilityScore,
                                CompetitorCitations = analysis.CompetitorCitations,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, 
                            "[CitationTracker] Failed to query {Platform} for question: {Question}",
                            adapter.Platform, question);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            // 等待所有任务完成
            await Task.WhenAll(tasks);

            // 更新进度
            await _repository.UpdateRunAsync(run);

            // 批量保存结果
            if (results.Any())
            {
                await _repository.CreateResultsAsync(results);
            }

            // 计算汇总指标
            CalculateSummaryMetrics(summary, results, task.BrandName, competitors);

            // 保存每日指标
            await SaveDailyMetricsAsync(taskId, summary);

            // 更新执行记录
            run.Status = "completed";
            run.CompletedAt = DateTime.UtcNow;
            run.SummaryMetrics = JsonSerializer.Serialize(new
            {
                summary.CitationFrequency,
                summary.BrandVisibilityScore,
                summary.ShareOfVoice,
                summary.PositiveSentimentRatio,
                summary.LinkedCitationRatio,
                summary.FirstPositionRatio
            });
            await _repository.UpdateRunAsync(run);

            // 更新任务状态
            task.Status = "completed";
            task.NextRunAt = CalculateNextRunTime(task.Frequency);
            await _repository.UpdateTaskAsync(task);

            summary.CompletedAt = run.CompletedAt;
            summary.CompletedQueries = run.CompletedQuestions;

            _logger.LogInformation(
                "[CitationTracker] Task {TaskId} completed. Citation Frequency: {CF:P1}, BVS: {BVS:F1}",
                taskId, summary.CitationFrequency, summary.BrandVisibilityScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CitationTracker] Task {TaskId} failed", taskId);
            
            run.Status = "failed";
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
            await _repository.UpdateRunAsync(run);

            task.Status = "failed";
            await _repository.UpdateTaskAsync(task);

            throw;
        }

        return summary;
    }

    /// <summary>
    /// 快速监测（不保存到数据库）
    /// </summary>
    public async Task<MonitoringSummary> QuickMonitorAsync(
        MonitoringTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var summary = new MonitoringSummary
        {
            StartedAt = DateTime.UtcNow
        };

        var adapters = _adapters
            .Where(a => a.IsAvailable && request.TargetPlatforms.Contains(a.Platform))
            .ToList();

        summary.TotalQueries = request.Questions.Count * adapters.Count;

        var results = new List<CitationResultEntity>();

        foreach (var question in request.Questions)
        {
            foreach (var adapter in adapters)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var response = await adapter.QueryAsync(question, cancellationToken);
                    var analysis = await adapter.AnalyzeCitationAsync(
                        response, request.BrandName, request.BrandAliases, request.Competitors, cancellationToken);

                    var result = new CitationResultEntity
                    {
                        Platform = adapter.Platform.ToString().ToLower(),
                        Question = question,
                        Response = response.Response,
                        IsCited = analysis.IsCited,
                        CitationPosition = analysis.Position.ToString().ToLower(),
                        PositionRatio = analysis.PositionRatio,
                        HasLink = analysis.HasLink,
                        Sentiment = analysis.Sentiment.ToString().ToLower(),
                        SentimentScore = analysis.SentimentScore,
                        VisibilityScore = analysis.VisibilityScore,
                        WordCountRatio = analysis.WordCountRatio,
                        CitationWordCount = analysis.CitationWordCount,
                        TotalWordCount = analysis.TotalWordCount,
                        PositionAdjustedScore = analysis.PositionAdjustedScore,
                        ResponseTimeMs = response.ResponseTimeMs,
                        ApiCost = response.ApiCost
                    };
                    results.Add(result);

                    summary.Results.Add(new CitationResultDetail
                    {
                        Platform = adapter.Platform,
                        Question = question,
                        ResponsePreview = response.Response.Length > 200 
                            ? response.Response.Substring(0, 200) + "..." 
                            : response.Response,
                        IsCited = analysis.IsCited,
                        Position = analysis.Position,
                        HasLink = analysis.HasLink,
                        Sentiment = analysis.Sentiment,
                        VisibilityScore = analysis.VisibilityScore,
                        CompetitorCitations = analysis.CompetitorCitations,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[CitationTracker] Quick monitor failed for {Platform}", adapter.Platform);
                }
            }
        }

        CalculateSummaryMetrics(summary, results, request.BrandName, request.Competitors);
        summary.CompletedAt = DateTime.UtcNow;
        summary.CompletedQueries = results.Count;

        return summary;
    }

    /// <summary>
    /// 获取 Dashboard 数据
    /// </summary>
    public async Task<CitationDashboard> GetDashboardAsync(int taskId)
    {
        var dashboard = new CitationDashboard();

        // 获取最新指标
        var latestMetrics = await _repository.GetLatestDailyMetricsAsync(taskId);
        if (latestMetrics != null)
        {
            dashboard.CitationFrequency = latestMetrics.CitationFrequency;
            dashboard.BrandVisibilityScore = latestMetrics.BrandVisibilityScore;
            dashboard.ShareOfVoice = latestMetrics.ShareOfVoice;
            dashboard.PositiveSentimentRatio = latestMetrics.PositiveSentimentRatio;
            dashboard.LinkedCitationRatio = latestMetrics.LinkedCitationRatio;
        }

        // 获取历史趋势（最近 30 天）
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-30);
        var history = await _repository.GetDailyMetricsAsync(taskId, startDate, endDate, "all");

        dashboard.TrendHistory = history.Select(h => new TrendDataPoint
        {
            Date = h.MetricDate,
            CitationFrequency = h.CitationFrequency,
            BrandVisibilityScore = h.BrandVisibilityScore,
            ShareOfVoice = h.ShareOfVoice
        }).ToList();

        // 计算趋势
        if (history.Count >= 2)
        {
            var recent = history.TakeLast(7).ToList();
            var previous = history.SkipLast(7).TakeLast(7).ToList();

            if (recent.Any() && previous.Any())
            {
                var recentCF = recent.Average(x => x.CitationFrequency);
                var previousCF = previous.Average(x => x.CitationFrequency);
                dashboard.CitationFrequencyTrend = GetTrendSymbol(recentCF, previousCF);

                var recentBVS = recent.Average(x => x.BrandVisibilityScore);
                var previousBVS = previous.Average(x => x.BrandVisibilityScore);
                dashboard.BrandVisibilityTrend = GetTrendSymbol(recentBVS, previousBVS);

                var recentSOV = recent.Average(x => x.ShareOfVoice);
                var previousSOV = previous.Average(x => x.ShareOfVoice);
                dashboard.ShareOfVoiceTrend = GetTrendSymbol(recentSOV, previousSOV);
            }
        }

        // 获取分平台数据
        var latestRun = await _repository.GetLatestRunByTaskIdAsync(taskId);
        if (latestRun != null)
        {
            var platformStats = await _repository.GetPlatformStatsAsync(latestRun.Id);
            foreach (var (platform, stats) in platformStats)
            {
                dashboard.PlatformData.Add(new PlatformMetrics
                {
                    Platform = Enum.Parse<AIPlatform>(platform, true),
                    TotalQueries = stats.total,
                    CitedCount = stats.cited,
                    CitationFrequency = stats.total > 0 ? (double)stats.cited / stats.total : 0
                });
            }
        }

        return dashboard;
    }

    private void CalculateSummaryMetrics(
        MonitoringSummary summary, 
        List<CitationResultEntity> results,
        string brandName,
        List<string> competitors)
    {
        if (!results.Any()) return;

        // Citation Frequency
        var citedCount = results.Count(r => r.IsCited);
        summary.CitationFrequency = (double)citedCount / results.Count;

        // Brand Visibility Score (平均)
        summary.BrandVisibilityScore = results.Where(r => r.IsCited).DefaultIfEmpty()
            .Average(r => r?.VisibilityScore ?? 0);

        // Positive Sentiment Ratio
        var citedResults = results.Where(r => r.IsCited).ToList();
        if (citedResults.Any())
        {
            summary.PositiveSentimentRatio = (double)citedResults.Count(r => r.Sentiment == "positive") / citedResults.Count;
        }

        // Linked Citation Ratio
        if (citedResults.Any())
        {
            summary.LinkedCitationRatio = (double)citedResults.Count(r => r.HasLink) / citedResults.Count;
        }

        // First Position Ratio
        if (citedResults.Any())
        {
            summary.FirstPositionRatio = (double)citedResults.Count(r => r.CitationPosition == "first") / citedResults.Count;
        }

        // Share of Voice
        var brandCitations = citedCount;
        var totalCitations = brandCitations;
        var competitorCitationCounts = new Dictionary<string, int>();

        foreach (var competitor in competitors)
        {
            var count = results.Count(r =>
            {
                if (string.IsNullOrEmpty(r.CompetitorCitations)) return false;
                var citations = JsonSerializer.Deserialize<Dictionary<string, bool>>(r.CompetitorCitations);
                return citations?.GetValueOrDefault(competitor, false) ?? false;
            });
            competitorCitationCounts[competitor] = count;
            totalCitations += count;
        }

        summary.ShareOfVoice = totalCitations > 0 ? (double)brandCitations / totalCitations : 0;

        foreach (var (competitor, count) in competitorCitationCounts)
        {
            summary.CompetitorShareOfVoice[competitor] = totalCitations > 0 ? (double)count / totalCitations : 0;
        }

        // Platform Metrics
        var platformGroups = results.GroupBy(r => r.Platform);
        foreach (var group in platformGroups)
        {
            var platform = Enum.Parse<AIPlatform>(group.Key, true);
            var platformResults = group.ToList();
            var platformCited = platformResults.Where(r => r.IsCited).ToList();

            summary.PlatformMetrics[platform] = new PlatformMetrics
            {
                Platform = platform,
                TotalQueries = platformResults.Count,
                CitedCount = platformCited.Count,
                CitationFrequency = (double)platformCited.Count / platformResults.Count,
                AverageVisibilityScore = platformCited.DefaultIfEmpty().Average(r => r?.VisibilityScore ?? 0),
                PositiveSentimentRatio = platformCited.Any() 
                    ? (double)platformCited.Count(r => r.Sentiment == "positive") / platformCited.Count 
                    : 0,
                LinkedCitationRatio = platformCited.Any()
                    ? (double)platformCited.Count(r => r.HasLink) / platformCited.Count
                    : 0
            };
        }
    }

    private async Task SaveDailyMetricsAsync(int taskId, MonitoringSummary summary)
    {
        var today = DateTime.UtcNow.Date;

        // 保存全平台汇总
        await _repository.UpsertDailyMetricsAsync(new CitationDailyMetricsEntity
        {
            TaskId = taskId,
            MetricDate = today,
            Platform = "all",
            CitationFrequency = summary.CitationFrequency,
            BrandVisibilityScore = summary.BrandVisibilityScore,
            ShareOfVoice = summary.ShareOfVoice,
            PositiveSentimentRatio = summary.PositiveSentimentRatio,
            LinkedCitationRatio = summary.LinkedCitationRatio,
            FirstPositionRatio = summary.FirstPositionRatio,
            TotalQueries = summary.TotalQueries,
            CitedCount = (int)(summary.TotalQueries * summary.CitationFrequency),
            CompetitorMetrics = JsonSerializer.Serialize(summary.CompetitorShareOfVoice),
            CreatedAt = DateTime.UtcNow
        });

        // 保存分平台指标
        foreach (var (platform, metrics) in summary.PlatformMetrics)
        {
            await _repository.UpsertDailyMetricsAsync(new CitationDailyMetricsEntity
            {
                TaskId = taskId,
                MetricDate = today,
                Platform = platform.ToString().ToLower(),
                CitationFrequency = metrics.CitationFrequency,
                BrandVisibilityScore = metrics.AverageVisibilityScore,
                PositiveSentimentRatio = metrics.PositiveSentimentRatio,
                LinkedCitationRatio = metrics.LinkedCitationRatio,
                TotalQueries = metrics.TotalQueries,
                CitedCount = metrics.CitedCount,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private DateTime? CalculateNextRunTime(string frequency)
    {
        return frequency switch
        {
            "daily" => DateTime.UtcNow.AddDays(1),
            "weekly" => DateTime.UtcNow.AddDays(7),
            "monthly" => DateTime.UtcNow.AddMonths(1),
            _ => null
        };
    }

    private string GetTrendSymbol(double current, double previous)
    {
        var change = (current - previous) / (previous == 0 ? 1 : previous);
        return change switch
        {
            > 0.05 => "↑",
            < -0.05 => "↓",
            _ => "→"
        };
    }
}
