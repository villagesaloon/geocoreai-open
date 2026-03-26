-- ============================================================
-- GEO 检测系统数据库迁移脚本
-- 版本: v5.0
-- 日期: 2026-03-22
-- 说明: P0 基础设施 - 创建检测系统相关表
-- ============================================================

-- ============================================================
-- 1. 扩展 geo_projects 表，添加检测相关字段
-- ============================================================

ALTER TABLE geo_projects 
ADD COLUMN IF NOT EXISTS detection_status VARCHAR(20) DEFAULT 'none' 
    COMMENT '检测状态: none/pending/running/completed/failed';

ALTER TABLE geo_projects 
ADD COLUMN IF NOT EXISTS last_detection_at DATETIME 
    COMMENT '最后检测时间';

ALTER TABLE geo_projects 
ADD COLUMN IF NOT EXISTS last_detection_task_id BIGINT 
    COMMENT '最后检测任务ID';

ALTER TABLE geo_projects 
ADD COLUMN IF NOT EXISTS last_website_audit_at DATETIME 
    COMMENT '最后网站审计时间';

ALTER TABLE geo_projects 
ADD COLUMN IF NOT EXISTS visibility_score DECIMAL(5,2) 
    COMMENT 'AI可见度得分 0-100';

ALTER TABLE geo_projects 
ADD COLUMN IF NOT EXISTS website_health_score DECIMAL(5,2) 
    COMMENT '网站健康度得分 0-100';

-- 添加索引（如果不存在）
-- ALTER TABLE geo_projects ADD INDEX IF NOT EXISTS idx_detection_status (detection_status);
-- ALTER TABLE geo_projects ADD INDEX IF NOT EXISTS idx_last_detection (last_detection_at);

-- ============================================================
-- 2. 创建 geo_detection_tasks 表
-- ============================================================

CREATE TABLE IF NOT EXISTS geo_detection_tasks (
    -- 主键
    id BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '任务ID',
    
    -- 关联信息
    project_id BIGINT NOT NULL COMMENT '项目ID',
    user_id BIGINT NOT NULL COMMENT '用户ID',
    
    -- 任务类型和状态
    task_type VARCHAR(20) NOT NULL DEFAULT 'full' 
        COMMENT '任务类型: full/quick/scheduled/website_only',
    status VARCHAR(20) NOT NULL DEFAULT 'pending' 
        COMMENT '状态: pending/queued/running/completed/failed/cancelled',
    current_phase VARCHAR(50) 
        COMMENT '当前阶段: data_prep/question_gen/ai_detection/website_audit/analysis',
    progress INT DEFAULT 0 
        COMMENT '进度 0-100',
    message TEXT 
        COMMENT '状态消息',
    error_message TEXT 
        COMMENT '错误消息',
    
    -- 队列信息
    queue_name VARCHAR(100) 
        COMMENT '队列名称',
    queue_position INT 
        COMMENT '入队时的队列位置',
    
    -- Phase 1: 数据准备结果
    selling_points_count INT DEFAULT 0 
        COMMENT '卖点数量',
    personas_count INT DEFAULT 0 
        COMMENT '画像数量',
    stages_count INT DEFAULT 0 
        COMMENT '阶段数量',
    
    -- Phase 2: 问题生成结果
    questions_count INT DEFAULT 0 
        COMMENT '生成问题数量',
    questions_validated INT DEFAULT 0 
        COMMENT '验证通过问题数量',
    
    -- Phase 3: AI 检测结果
    models_tested JSON 
        COMMENT '测试的模型列表 ["gpt","claude",...]',
    brand_mention_rate DECIMAL(5,4) 
        COMMENT '品牌提及率 0.0000-1.0000',
    avg_mention_position DECIMAL(5,2) 
        COMMENT '平均提及位置',
    citation_count INT DEFAULT 0 
        COMMENT '引用数量',
    user_site_cited BOOLEAN DEFAULT FALSE 
        COMMENT '用户网站是否被引用',
    
    -- Phase 4: 网站审计结果
    website_audit_skipped BOOLEAN DEFAULT FALSE 
        COMMENT '是否跳过网站审计',
    website_audit_cached BOOLEAN DEFAULT FALSE 
        COMMENT '是否使用缓存',
    website_overall_score INT 
        COMMENT '网站整体得分',
    website_technical_score INT 
        COMMENT '技术得分',
    website_content_score INT 
        COMMENT '内容得分',
    website_eeat_score INT 
        COMMENT 'E-E-A-T得分',
    
    -- Phase 5: 综合结果
    visibility_score DECIMAL(5,2) 
        COMMENT 'AI可见度得分',
    website_health_score DECIMAL(5,2) 
        COMMENT '网站健康度得分',
    issues_count INT DEFAULT 0 
        COMMENT '问题数量',
    recommendations_count INT DEFAULT 0 
        COMMENT '建议数量',
    result_summary JSON 
        COMMENT '结果摘要JSON',
    
    -- 时间戳
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP 
        COMMENT '创建时间',
    queued_at DATETIME 
        COMMENT '入队时间',
    started_at DATETIME 
        COMMENT '开始执行时间',
    completed_at DATETIME 
        COMMENT '完成时间',
    
    -- 索引
    INDEX idx_project_status (project_id, status),
    INDEX idx_user_created (user_id, created_at),
    INDEX idx_status_created (status, created_at),
    INDEX idx_task_type (task_type)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='GEO检测任务表';

-- ============================================================
-- 3. 创建 geo_website_audits 表
-- ============================================================

CREATE TABLE IF NOT EXISTS geo_website_audits (
    -- 主键
    id BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '审计ID',
    
    -- 关联信息
    project_id BIGINT NOT NULL COMMENT '项目ID',
    task_id BIGINT COMMENT '关联的检测任务ID',
    url VARCHAR(500) NOT NULL COMMENT '审计的URL',
    
    -- 整体评分
    overall_score INT DEFAULT 0 COMMENT '整体得分 0-100',
    grade VARCHAR(5) COMMENT '等级 A/B/C/D/F',
    
    -- ========== 技术审计 ==========
    technical_score INT DEFAULT 0 COMMENT '技术得分 0-100',
    
    -- robots.txt
    robots_txt_exists BOOLEAN DEFAULT FALSE COMMENT 'robots.txt是否存在',
    robots_txt_content TEXT COMMENT 'robots.txt内容',
    ai_crawlers_allowed BOOLEAN DEFAULT FALSE COMMENT '是否允许AI爬虫',
    blocked_crawlers JSON COMMENT '被阻止的爬虫列表',
    
    -- sitemap
    sitemap_exists BOOLEAN DEFAULT FALSE COMMENT 'sitemap是否存在',
    sitemap_url_count INT DEFAULT 0 COMMENT 'sitemap中的URL数量',
    
    -- llms.txt
    llms_txt_exists BOOLEAN DEFAULT FALSE COMMENT 'llms.txt是否存在',
    llms_txt_entry_count INT DEFAULT 0 COMMENT 'llms.txt条目数量',
    
    -- 技术细节
    https_enabled BOOLEAN DEFAULT FALSE COMMENT '是否启用HTTPS',
    has_canonical BOOLEAN DEFAULT FALSE COMMENT '是否有canonical标签',
    js_rendering_issue BOOLEAN DEFAULT FALSE COMMENT '是否有JS渲染问题',
    core_web_vitals JSON COMMENT 'Core Web Vitals指标',
    
    -- ========== 内容审计 ==========
    content_score INT DEFAULT 0 COMMENT '内容得分 0-100',
    
    -- Schema标记
    has_schema BOOLEAN DEFAULT FALSE COMMENT '是否有Schema标记',
    schema_types JSON COMMENT 'Schema类型列表',
    has_article_schema BOOLEAN DEFAULT FALSE COMMENT '是否有Article Schema',
    has_faq_schema BOOLEAN DEFAULT FALSE COMMENT '是否有FAQ Schema',
    
    -- Answer Capsule
    has_answer_capsules BOOLEAN DEFAULT FALSE COMMENT '是否有Answer Capsule',
    answer_capsule_coverage DECIMAL(5,2) COMMENT 'Answer Capsule覆盖率',
    
    -- 页面结构
    heading_structure_ok BOOLEAN DEFAULT FALSE COMMENT '标题结构是否正确',
    h1_count INT DEFAULT 0 COMMENT 'H1标签数量',
    h2_count INT DEFAULT 0 COMMENT 'H2标签数量',
    
    -- Meta标签
    meta_title_ok BOOLEAN DEFAULT FALSE COMMENT 'Meta Title是否正确',
    meta_description_ok BOOLEAN DEFAULT FALSE COMMENT 'Meta Description是否正确',
    has_og_tags BOOLEAN DEFAULT FALSE COMMENT '是否有Open Graph标签',
    
    -- ========== E-E-A-T 审计 ==========
    eeat_score INT DEFAULT 0 COMMENT 'E-E-A-T得分 0-100',
    
    has_author_info BOOLEAN DEFAULT FALSE COMMENT '是否有作者信息',
    has_publish_date BOOLEAN DEFAULT FALSE COMMENT '是否有发布日期',
    has_update_date BOOLEAN DEFAULT FALSE COMMENT '是否有更新日期',
    has_citations BOOLEAN DEFAULT FALSE COMMENT '是否有引用来源',
    external_link_count INT DEFAULT 0 COMMENT '外部链接数量',
    
    -- ========== 问题和建议 ==========
    issues JSON COMMENT '问题列表',
    recommendations JSON COMMENT '建议列表',
    
    -- ========== 爬虫详情 ==========
    pages_crawled INT DEFAULT 1 COMMENT '爬取的页面数',
    crawl_depth INT DEFAULT 1 COMMENT '爬取深度',
    crawl_duration_ms INT COMMENT '爬取耗时(毫秒)',
    pages_detail JSON COMMENT '页面详情列表',
    
    -- ========== 缓存控制 ==========
    cache_expires_at DATETIME COMMENT '缓存过期时间',
    
    -- 时间戳
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    
    -- 索引
    INDEX idx_project (project_id),
    INDEX idx_task (task_id),
    INDEX idx_cache (project_id, cache_expires_at),
    INDEX idx_created (created_at)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='网站审计表';

-- ============================================================
-- 4. 创建 geo_detection_metrics 表
-- ============================================================

CREATE TABLE IF NOT EXISTS geo_detection_metrics (
    -- 主键
    id BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '指标ID',
    
    -- 关联信息
    task_id BIGINT NOT NULL COMMENT '检测任务ID',
    project_id BIGINT NOT NULL COMMENT '项目ID',
    country_code VARCHAR(5) NOT NULL DEFAULT 'ALL' COMMENT '国家代码: US/CN/GB/ALL(全球汇总)',
    
    -- ========== AI 可见度指标 ==========
    visibility_score INT DEFAULT 0 COMMENT 'AI可见度得分 0-100',
    mention_count INT DEFAULT 0 COMMENT '提及次数',
    citation_page_count INT DEFAULT 0 COMMENT '引用页面数',
    brand_mention_rate DECIMAL(5,4) COMMENT '品牌提及率',
    avg_mention_position DECIMAL(5,2) COMMENT '平均提及位置',
    
    -- ========== 情感分析 ==========
    sentiment_positive DECIMAL(5,2) COMMENT '正面情感比例',
    sentiment_neutral DECIMAL(5,2) COMMENT '中性情感比例',
    sentiment_negative DECIMAL(5,2) COMMENT '负面情感比例',
    
    -- ========== 各模型数据 ==========
    -- ChatGPT
    chatgpt_mentions INT DEFAULT 0 COMMENT 'ChatGPT提及次数',
    chatgpt_citations INT DEFAULT 0 COMMENT 'ChatGPT引用数',
    chatgpt_visibility INT COMMENT 'ChatGPT可见度',
    
    -- Claude
    claude_mentions INT DEFAULT 0 COMMENT 'Claude提及次数',
    claude_citations INT DEFAULT 0 COMMENT 'Claude引用数',
    claude_visibility INT COMMENT 'Claude可见度',
    
    -- Gemini
    gemini_mentions INT DEFAULT 0 COMMENT 'Gemini提及次数',
    gemini_citations INT DEFAULT 0 COMMENT 'Gemini引用数',
    gemini_visibility INT COMMENT 'Gemini可见度',
    
    -- Perplexity
    perplexity_mentions INT DEFAULT 0 COMMENT 'Perplexity提及次数',
    perplexity_citations INT DEFAULT 0 COMMENT 'Perplexity引用数',
    perplexity_visibility INT COMMENT 'Perplexity可见度',
    
    -- Grok
    grok_mentions INT DEFAULT 0 COMMENT 'Grok提及次数',
    grok_citations INT DEFAULT 0 COMMENT 'Grok引用数',
    grok_visibility INT COMMENT 'Grok可见度',
    
    -- ========== SEO 指标（可选，从第三方API获取）==========
    authority_score INT COMMENT '权威度得分',
    organic_traffic INT COMMENT '自然流量',
    traffic_change_percent DECIMAL(5,2) COMMENT '流量变化百分比',
    
    -- 时间戳
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    
    -- 索引
    INDEX idx_task (task_id),
    INDEX idx_project_country (project_id, country_code),
    INDEX idx_created (created_at),
    INDEX idx_visibility (visibility_score)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='检测指标表（分国家）';

-- ============================================================
-- 5. 创建 geo_detection_suggestions 表
-- ============================================================

CREATE TABLE IF NOT EXISTS geo_detection_suggestions (
    -- 主键
    id BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '建议ID',
    
    -- 关联信息
    task_id BIGINT NOT NULL COMMENT '检测任务ID',
    project_id BIGINT NOT NULL COMMENT '项目ID',
    
    -- ========== 建议分类 ==========
    category VARCHAR(50) NOT NULL 
        COMMENT '分类: ai_visibility/website_tech/content_quality/seo',
    subcategory VARCHAR(50) 
        COMMENT '子分类: brand_mention/citation_source/robots_txt等',
    
    -- ========== 建议内容 ==========
    priority VARCHAR(10) NOT NULL DEFAULT 'medium'
        COMMENT '优先级: high/medium/low',
    title VARCHAR(200) NOT NULL 
        COMMENT '建议标题',
    description TEXT 
        COMMENT '建议描述',
    impact_score INT 
        COMMENT '预估影响分数 1-10',
    effort_level VARCHAR(10) 
        COMMENT '实施难度: easy/medium/hard',
    
    -- ========== 具体行动 ==========
    action_items JSON 
        COMMENT '行动步骤列表',
    example_code TEXT 
        COMMENT '示例代码',
    reference_urls JSON 
        COMMENT '参考链接列表',
    
    -- ========== 状态跟踪 ==========
    status VARCHAR(20) DEFAULT 'pending' 
        COMMENT '状态: pending/in_progress/completed/dismissed',
    completed_at DATETIME 
        COMMENT '完成时间',
    
    -- 时间戳
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    
    -- 索引
    INDEX idx_task (task_id),
    INDEX idx_project_category (project_id, category),
    INDEX idx_priority (priority),
    INDEX idx_status (status)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='优化建议表';

-- ============================================================
-- 6. 创建 geo_notification_settings 表
-- ============================================================

CREATE TABLE IF NOT EXISTS geo_notification_settings (
    -- 主键
    id BIGINT PRIMARY KEY AUTO_INCREMENT COMMENT '设置ID',
    
    -- 关联信息
    user_id BIGINT NOT NULL COMMENT '用户ID',
    
    -- ========== 邮件通知设置 ==========
    email_on_detection_complete BOOLEAN DEFAULT TRUE 
        COMMENT '检测完成时发送邮件',
    email_on_detection_failed BOOLEAN DEFAULT TRUE 
        COMMENT '检测失败时发送邮件',
    email_on_weekly_report BOOLEAN DEFAULT FALSE 
        COMMENT '发送周报',
    email_on_visibility_change BOOLEAN DEFAULT FALSE 
        COMMENT '可见度大幅变化时发送邮件',
    
    -- ========== 通知阈值 ==========
    visibility_change_threshold INT DEFAULT 10 
        COMMENT '可见度变化阈值（百分比）',
    
    -- 时间戳
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    
    -- 唯一约束
    UNIQUE KEY uk_user (user_id)
    
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='通知设置表';

-- ============================================================
-- 7. 初始化 system_configs 配置项
-- ============================================================

INSERT INTO system_configs (config_key, config_value, description, created_at) VALUES
-- Redis 配置
('redis.connection_string', '101.37.86.154:20697,password=NHLap2980V,defaultDatabase=1,syncTimeout=15000', 'Redis 连接字符串', NOW()),

-- Detection 配置
('detection.full_detection_daily_limit', '1', '完整检测每日限制次数', NOW()),
('detection.quick_detection_daily_limit', '0', '快速检测每日限制次数（0=无限）', NOW()),
('detection.website_audit_daily_limit', '1', '网站审计每日限制次数', NOW()),
('detection.detection_timeout_minutes', '30', '检测超时时间（分钟）', NOW()),

-- Crawler 配置
('crawler.request_interval_min_ms', '1000', '爬虫请求最小间隔（毫秒）', NOW()),
('crawler.request_interval_max_ms', '3000', '爬虫请求最大间隔（毫秒）', NOW()),
('crawler.max_pages_per_site', '50', '每站点最大爬取页面数', NOW()),
('crawler.max_crawl_depth', '3', '最大爬取深度', NOW()),

-- AI Detection 配置
('ai_detection.questions_per_detection', '15', '每次检测的问题数量', NOW()),
('ai_detection.default_models', 'gpt,claude,gemini,perplexity,grok', '默认检测模型列表', NOW()),
('ai_detection.enable_google_trends', 'true', '是否启用 Google Trends 验证', NOW()),

-- Queue 配置
('queue.detection_queue_name', 'geo:queue:detection', '检测任务队列名称', NOW()),
('queue.crawler_queue_name', 'geo:queue:crawler', '爬虫任务队列名称', NOW()),
('queue.notification_queue_name', 'geo:queue:notification', '通知任务队列名称', NOW()),
('queue.max_retry_count', '3', '任务最大重试次数', NOW())

ON DUPLICATE KEY UPDATE updated_at = NOW();

-- ============================================================
-- 完成
-- ============================================================
