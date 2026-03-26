using GeoCore.Data.DbContext;
using GeoCore.Data.Repositories;
using GeoCore.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 配置端口 8081 (Admin 后台，仅本地访问)
builder.WebHost.UseUrls("http://localhost:8081");

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

// 注册 Repository
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<PromptConfigRepository>();
builder.Services.AddScoped<SystemConfigRepository>();
builder.Services.AddScoped<ModelConfigRepository>();
builder.Services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();

// Phase 8: 内容生成与发布 Repository
builder.Services.AddScoped<ContentTemplateRepository>();
builder.Services.AddScoped<PlatformContentRuleRepository>();
builder.Services.AddScoped<PublishPlatformAppRepository>();
builder.Services.AddScoped<PublishRuleRepository>();

// Phase 10: CMS Repository
builder.Services.AddScoped<CmsArticleRepository>();
builder.Services.AddScoped<CmsCategoryRepository>();

// 注册 HttpClientFactory（用于通知 SaaS 刷新缓存）
builder.Services.AddHttpClient();

// 添加静态文件支持
builder.Services.AddDirectoryBrowser();

var app = builder.Build();

// 初始化数据库表
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GeoDbContext>();
    db.InitTables();
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
app.MapGet("/api/health", () => new { Status = "OK", Service = "GeoCore.Admin", Time = DateTime.UtcNow });

// 根路径重定向到登录页
app.MapGet("/", () => Results.Redirect("/login.html"));

app.Run();
