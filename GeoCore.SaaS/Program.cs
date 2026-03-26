using GeoCore.Data.DbContext;
using GeoCore.Data.Repositories;
using GeoCore.Shared.Interfaces;
using GeoCore.SaaS.Services;
using GeoCore.SaaS.Services.GoogleTrends;
using GeoCore.SaaS.Services.ContentQuality;
using GeoCore.SaaS.Services.AICrawlerAudit;
using GeoCore.SaaS.Services.LlmsTxt;
using GeoCore.SaaS.Services.GA4AITracking;
using GeoCore.SaaS.Services.ContentFreshnessAudit;
using GeoCore.SaaS.Services.Notification;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 8080 (通过 Cloudflared 映射到 geocoreai.com)
// 配置 Kestrel 超时时间（支持长时间运行的 AI 请求）
builder.WebHost.UseUrls("http://0.0.0.0:8080");
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

// 添加控制器
builder.Services.AddControllers();

// 添加 CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 注册数据库上下文
builder.Services.AddSingleton<GeoDbContext>();
builder.Services.AddScoped<SqlSugar.ISqlSugarClient>(sp => sp.GetRequiredService<GeoDbContext>().Client);

// 注册 Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGeoProjectRepository, GeoProjectRepository>();
builder.Services.AddScoped<IGeoQuestionRepository, GeoQuestionRepository>();
builder.Services.AddScoped<ISysSourcePlatformRepository, SysSourcePlatformRepository>();
builder.Services.AddScoped<PromptConfigRepository>();
builder.Services.AddScoped<SystemConfigRepository>();
builder.Services.AddScoped<ModelConfigRepository>();

// 注册配置缓存服务（Singleton，全局共享）
builder.Services.AddSingleton<ConfigCacheService>();

// 配置 HttpClient 用于 URL 分析
builder.Services.AddHttpClient("WebScraper", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

// 配置 HttpClient 用于 Google Trends 代理
builder.Services.AddHttpClient("GoogleTrends", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
});

// 注册 Google Trends 服务
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GoogleTrendsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

// 注册语言配置服务 (Phase 3 重构)
builder.Services.AddScoped<LanguageConfigRepository>();
builder.Services.AddScoped<LanguageConfigProvider>();

// 注册内容质量评估服务 (Phase 3)
builder.Services.AddScoped<ClaimExtractor>();
builder.Services.AddScoped<EntityExtractor>();
builder.Services.AddScoped<AiseoAuditAnalyzer>();
builder.Services.AddScoped<ListicleAnalyzer>();
builder.Services.AddScoped<SchemaCompletenessAnalyzer>();
builder.Services.AddScoped<AdvancedContentAnalyzer>();
builder.Services.AddScoped<ContentQualityAnalyzer>();
builder.Services.AddScoped<GEOAdvancedAnalyzer>();
builder.Services.AddScoped<SchemaGenerator>();

// 注册引用追踪服务 (Phase 4)
builder.Services.AddScoped<CitationMonitoringRepository>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.ICitationAnalyzer, GeoCore.SaaS.Services.CitationTracking.CitationAnalyzer>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.IPlatformAdapter, GeoCore.SaaS.Services.CitationTracking.Adapters.ChatGPTAdapter>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.IPlatformAdapter, GeoCore.SaaS.Services.CitationTracking.Adapters.PerplexityAdapter>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.IPlatformAdapter, GeoCore.SaaS.Services.CitationTracking.Adapters.ClaudeAdapter>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.IPlatformAdapter, GeoCore.SaaS.Services.CitationTracking.Adapters.GeminiAdapter>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.IPlatformAdapter, GeoCore.SaaS.Services.CitationTracking.Adapters.GrokAdapter>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.IPlatformAdapter, GeoCore.SaaS.Services.CitationTracking.Adapters.GoogleAIAdapter>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.CitationTrackerService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationTracking.KeywordExtractorService>();

// 注册 Phase 5 服务 (Prompt 库 + 时效性)
builder.Services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
builder.Services.AddScoped<IQuestionLibraryRepository, QuestionLibraryRepository>();
builder.Services.AddScoped<IContentFreshnessRepository, ContentFreshnessRepository>();
builder.Services.AddScoped<PromptTemplateService>();

// 注册 Phase 5A 服务 (洞察分析 + 可执行建议)
builder.Services.AddScoped<GeoCore.SaaS.Services.InsightAnalyzer.InsightAnalyzerService>();

// 注册 Prompt 优化服务 (5.31-5.32)
builder.Services.AddScoped<GeoCore.SaaS.Services.PromptOptimization.PromptOptimizationService>();

// 注册 Reddit 服务 (5.22, 5.23, 5.29, 5.30, 5.33)
builder.Services.AddScoped<GeoCore.SaaS.Services.Reddit.RedditService>();

// 注册内容复用服务 (5.18-5.19)
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentReuse.ContentReuseService>();

// 注册 AI 爬虫服务 (4.48-4.53, 4.55-4.58)
builder.Services.AddScoped<GeoCore.SaaS.Services.AICrawler.AICrawlerService>();

// 注册 LLM 预览服务 (3.31, 4.54)
builder.Services.AddScoped<GeoCore.SaaS.Services.LLMPreview.LLMPreviewService>();

// 注册 Phase 5B 服务 (内容简报生成)
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentBrief.ContentBriefService>();

// 注册 Phase 4G 服务 (引用来源分析)
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationSource.CitationSourceService>();

// 注册 Phase 4H 服务 (情感分析增强)
builder.Services.AddScoped<GeoCore.SaaS.Services.SentimentAnalysis.SentimentAnalysisService>();

// 注册 Phase 4I 服务 (内容新鲜度监测)
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentFreshness.ContentFreshnessService>();

// 注册 Phase 4J 服务 (平台依赖度监测)
builder.Services.AddScoped<GeoCore.SaaS.Services.PlatformDependency.PlatformDependencyService>();

// 注册 Phase 1 服务 (Persona 系统)
builder.Services.AddScoped<GeoCore.SaaS.Services.Persona.PersonaService>();

// 注册 Phase 4 AI 可见度监测服务 (4.48-4.51)
builder.Services.AddScoped<AICrawlerAuditService>();
builder.Services.AddScoped<LlmsTxtService>();
builder.Services.AddScoped<GA4AITrackingService>();
builder.Services.AddScoped<ContentFreshnessAuditService>();

// 注册 Phase 6 网站 GEO/SEO 审计服务 (6.5-6.22)
builder.Services.AddScoped<GeoCore.SaaS.Services.SiteAudit.SiteAuditService>();

// 注册 Phase 7 LLM 引用源优化服务 (7.1-7.8)
builder.Services.AddScoped<GeoCore.SaaS.Services.LLMCitationOptimization.LLMCitationOptimizationService>();

// 注册 Phase 5 内容适配引擎服务 (5.14-5.28)
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentAdapter.ContentAdapterService>();

// 注册 Phase 4 引用基准服务 (4.42-4.45)
builder.Services.AddScoped<GeoCore.SaaS.Services.CitationBenchmark.CitationBenchmarkService>();

// 注册高级 GEO 服务 (7.9-7.13, 4.39-4.41, 5.21, 5.29-5.30)
builder.Services.AddScoped<GeoCore.SaaS.Services.AdvancedGEO.AdvancedGEOService>();

// 注册 Phase 8 内容发布服务
builder.Services.AddScoped<ContentTemplateRepository>();
builder.Services.AddScoped<PlatformContentRuleRepository>();
builder.Services.AddScoped<UserPlatformAccountRepository>();
builder.Services.AddScoped<ContentDraftRepository>();
builder.Services.AddScoped<PublishHistoryRepository>();
builder.Services.AddScoped<PublishPlatformAppRepository>();
builder.Services.AddScoped<PublishRuleRepository>();
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentPublish.ContentGenerationService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentPublish.OAuthService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentPublish.PublishService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.ContentPublish.PublishEffectService>();

// Phase 10: CMS Repository
builder.Services.AddScoped<CmsArticleRepository>();
builder.Services.AddScoped<CmsCategoryRepository>();

// Phase 6B: AuthorityScore 服务（引用来源权威度）
builder.Services.AddSingleton<GeoCore.SaaS.Services.AuthorityScore.AuthorityScoreService>();

// 检测系统 Repository (P0 基础设施)
builder.Services.AddScoped<IDetectionTaskRepository, DetectionTaskRepository>();
builder.Services.AddScoped<IWebsiteAuditRepository, WebsiteAuditRepository>();
builder.Services.AddScoped<IDetectionMetricRepository, DetectionMetricRepository>();
builder.Services.AddScoped<IDetectionSuggestionRepository, DetectionSuggestionRepository>();
builder.Services.AddScoped<INotificationSettingRepository, NotificationSettingRepository>();

// 检测系统 Redis 服务 (P1 队列服务)
builder.Services.AddSingleton<GeoCore.SaaS.Services.Detection.RedisConnectionService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.Detection.ITaskQueueService, GeoCore.SaaS.Services.Detection.TaskQueueService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.Detection.ITaskStatusService, GeoCore.SaaS.Services.Detection.TaskStatusService>();
builder.Services.AddHostedService<GeoCore.SaaS.Services.Detection.DetectionWorker>();

// 检测系统爬虫和审计服务 (P3 网站审计)
builder.Services.AddScoped<GeoCore.SaaS.Services.Detection.ICrawlerService, GeoCore.SaaS.Services.Detection.AbotCrawlerService>();
builder.Services.AddScoped<GeoCore.SaaS.Services.Detection.IWebsiteAuditIntegrationService, GeoCore.SaaS.Services.Detection.WebsiteAuditIntegrationService>();

// 建议系统 (P5 建议系统)
builder.Services.AddScoped<GeoCore.SaaS.Services.Suggestion.ISuggestionGenerator, GeoCore.SaaS.Services.Suggestion.SuggestionGenerator>();

// 通知系统 (P8 邮件通知)
builder.Services.AddScoped<ISysConfigRepository, SysConfigRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailSendService, ResendEmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<NotificationWorker>();

// 定时任务系统 (P9 调度 + 监控)
builder.Services.AddScoped<ISchedulerRepository, SchedulerRepository>();
builder.Services.AddHostedService<GeoCore.SaaS.Services.Scheduler.SchedulerService>();

// HTTP Client Factory（用于 Resend 等外部 API 调用）
builder.Services.AddHttpClient();

// 添加静态文件支持
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// 初始化数据库表和默认 Prompt 配置
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<GeoDbContext>();
    db.InitTables();
    
    // 初始化默认 Prompt 配置
    var promptInitializer = new PromptConfigInitializer(db);
    promptInitializer.InitializeDefaultPromptsAsync().GetAwaiter().GetResult();
    
    // 启动时将文件版 Prompt 同步到数据库（受配置开关控制）
    // 开发环境开启（修改文件即同步），生产环境关闭（直接改数据库）
    var enableFileSync = app.Configuration.GetValue<bool>("PromptSync:EnableFileSync");
    if (enableFileSync)
    {
        var promptsDir = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "prompts");
        promptInitializer.SyncFilePromptsToDbAsync(promptsDir).GetAwaiter().GetResult();
    }
    
    // 初始化模型配置（从 config.json 迁移到数据库）
    var modelConfigInit = new ModelConfigInitializer(db);
    modelConfigInit.InitializeFromConfigJsonAsync().GetAwaiter().GetResult();
    
    // 初始化 BrightData 和 GoogleTrends 配置
    var brightDataInit = new BrightDataConfigInitializer(db);
    brightDataInit.InitializeAsync().GetAwaiter().GetResult();
    
    // 初始化语言配置（Phase 3 重构）
    var languageConfigInit = new LanguageConfigInitializer(db);
    languageConfigInit.InitializeAsync().GetAwaiter().GetResult();
    
    // 初始化平台配置（Phase 7 前后台分离）
    var platformConfigInit = new PlatformConfigInitializer(db);
    platformConfigInit.InitializeAsync().GetAwaiter().GetResult();
    
    // 初始化系统配置（P8 通知系统 - Resend 等）
    var sysConfigInit = new SysConfigInitializer(db);
    sysConfigInit.InitializeAsync().GetAwaiter().GetResult();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "数据库初始化失败，部分功能可能不可用");
}

// 预热配置缓存（从数据库加载到内存）- 必须执行，独立于其他初始化
try
{
    var configCache = app.Services.GetRequiredService<ConfigCacheService>();
    configCache.LoadAllAsync().GetAwaiter().GetResult();
    app.Logger.LogInformation("配置缓存预热完成");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "配置缓存加载失败");
}

// 启用 CORS
app.UseCors();

// 启用静态文件
app.UseDefaultFiles();
app.UseStaticFiles();

// 启用路由和控制器
app.UseRouting();
app.MapControllers();

// API 路由
app.MapGet("/api/health", () => new { Status = "OK", Service = "GeoCore.SaaS", Time = DateTime.UtcNow });

app.Run();
