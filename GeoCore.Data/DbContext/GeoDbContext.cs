using SqlSugar;
using GeoCore.Data.Entities;

namespace GeoCore.Data.DbContext;

/// <summary>
/// GeoCore 数据库上下文
/// </summary>
public class GeoDbContext
{
    private static readonly string ConnectionString = 
        "Data Source=101.37.86.154;User ID=root;Password=AW$ky*vGy*4Ydk56j$Ac2$;port=20696;Database=GeocoreAI;SslMode=none;Allow User Variables=True;AllowLoadLocalInfile=True;AllowPublicKeyRetrieval=True;charset=utf8mb4;Connection Timeout=60;Default Command Timeout=60;";

    private readonly SqlSugarScope _client;

    public GeoDbContext()
    {
        _client = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = ConnectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        },
        db =>
        {
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine($"[SQL] {sql}");
            };
        });
    }

    /// <summary>
    /// 获取数据库客户端
    /// </summary>
    public SqlSugarScope Client => _client;

    /// <summary>
    /// 初始化数据库表 (如果不存在则创建)
    /// </summary>
    public void InitTables()
    {
        // 基础表
        _client.CodeFirst.InitTables<UserEntity>();
        _client.CodeFirst.InitTables<AdminEntity>();
        _client.CodeFirst.InitTables<PromptConfigEntity>();
        _client.CodeFirst.InitTables<SystemConfigEntity>();
        _client.CodeFirst.InitTables<ModelConfigEntity>();
        
        // Phase 1.6: GEO 项目相关表
        _client.CodeFirst.InitTables<SysSourcePlatformEntity>();      // 系统级：来源平台
        _client.CodeFirst.InitTables<GeoProjectEntity>();             // 项目主表
        _client.CodeFirst.InitTables<GeoProjectConfigEntity>();       // 项目配置
        _client.CodeFirst.InitTables<GeoProjectCompetitorEntity>();   // 项目竞品
        _client.CodeFirst.InitTables<GeoProjectSellingPointEntity>(); // 项目卖点
        _client.CodeFirst.InitTables<GeoProjectPersonaEntity>();      // 项目画像
        _client.CodeFirst.InitTables<GeoProjectStageEntity>();        // 项目阶段
        _client.CodeFirst.InitTables<GeoQuestionEntity>();            // 问题
        _client.CodeFirst.InitTables<GeoQuestionAnswerEntity>();      // 问题回答
        _client.CodeFirst.InitTables<GeoQuestionSourceEntity>();      // 问题来源
        
        // Phase 3: 语言配置相关表
        _client.CodeFirst.InitTables<LanguageConfigEntity>();         // 语言配置
        _client.CodeFirst.InitTables<ExtractionPatternEntity>();      // 提取模式
        _client.CodeFirst.InitTables<KnownEntityEntity>();            // 已知实体
        _client.CodeFirst.InitTables<SentimentKeywordEntity>();       // 情感关键词
        
        // Phase 4: 引用监测相关表
        _client.CodeFirst.InitTables<CitationMonitoringTaskEntity>(); // 监测任务
        _client.CodeFirst.InitTables<CitationMonitoringRunEntity>();  // 执行记录
        _client.CodeFirst.InitTables<CitationResultEntity>();         // 引用结果
        _client.CodeFirst.InitTables<CitationDailyMetricsEntity>();   // 每日指标
        _client.CodeFirst.InitTables<KeywordExclusionEntity>();       // 关键词排除词
        
        // Phase 5: Prompt 库 + 时效性相关表
        _client.CodeFirst.InitTables<PromptTemplateEntity>();         // Prompt 模板
        _client.CodeFirst.InitTables<PromptTemplateVersionEntity>();  // Prompt 版本历史
        _client.CodeFirst.InitTables<QuestionLibraryEntity>();        // 问题库
        _client.CodeFirst.InitTables<ContentFreshnessEntity>();       // 内容时效性
        
        // Phase 7: 平台配置相关表（前后台分离）
        _client.CodeFirst.InitTables<AICrawlerEntity>();              // AI 爬虫配置
        _client.CodeFirst.InitTables<LLMReferrerEntity>();            // LLM Referrer 配置
        _client.CodeFirst.InitTables<LLMPlatformPreferenceEntity>();  // 平台偏好数据
        _client.CodeFirst.InitTables<CitationBenchmarkEntity>();      // 引用基准数据
        _client.CodeFirst.InitTables<PersonaTemplateEntity>();        // Persona 模板
        
        // Phase 8: 内容生成与发布相关表
        _client.CodeFirst.InitTables<ContentTemplateEntity>();        // 内容模板
        _client.CodeFirst.InitTables<PlatformContentRuleEntity>();    // 平台内容规则
        _client.CodeFirst.InitTables<PublishPlatformAppEntity>();     // 发布平台 App 配置
        _client.CodeFirst.InitTables<PublishRuleEntity>();            // 发布规则
        _client.CodeFirst.InitTables<UserPlatformAccountEntity>();    // 用户平台账号
        _client.CodeFirst.InitTables<ContentDraftEntity>();           // 内容草稿
        _client.CodeFirst.InitTables<PublishHistoryEntity>();         // 发布历史
        
        // Phase 10: CMS 相关表
        _client.CodeFirst.InitTables<CmsArticleEntity>();             // CMS 文章
        _client.CodeFirst.InitTables<CmsCategoryEntity>();            // CMS 分类
        
        // P8: 通知系统相关表
        _client.CodeFirst.InitTables<SysConfigEntity>();              // 系统配置（API Key 等）
        _client.CodeFirst.InitTables<GeoEmailTemplateEntity>();       // 邮件模板
        _client.CodeFirst.InitTables<GeoNotificationTaskEntity>();    // 通知任务队列
        _client.CodeFirst.InitTables<GeoEmailSendLogEntity>();        // 邮件发送日志
        
        // P9: 定时任务相关表
        _client.CodeFirst.InitTables<ScheduledJobEntity>();           // 定时任务配置
        _client.CodeFirst.InitTables<ScheduledJobLogEntity>();        // 定时任务执行日志
        
        Console.WriteLine("[DB] 数据库表初始化完成");
        
        // 修复表字符集为 utf8mb4
        FixTableCharset();
    }
    
    /// <summary>
    /// 修复数据库表字符集为 utf8mb4
    /// </summary>
    public void FixTableCharset()
    {
        var tables = new[]
        {
            "geo_projects", "geo_project_configs", "geo_project_competitors",
            "geo_project_selling_points", "geo_project_personas", "geo_project_stages",
            "geo_questions", "geo_question_answers", "geo_question_sources",
            "sys_source_platforms", "users", "admins", "prompt_configs", "system_configs", "model_configs",
            "language_configs", "extraction_patterns", "known_entities"
        };
        
        foreach (var table in tables)
        {
            try
            {
                _client.Ado.ExecuteCommand($"ALTER TABLE `{table}` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
                Console.WriteLine($"[DB] 表 {table} 字符集已修复为 utf8mb4");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] 修复表 {table} 字符集失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 使用自定义连接字符串创建数据库实例
    /// </summary>
    public static SqlSugarClient CreateClient(string connectionString)
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }
}
