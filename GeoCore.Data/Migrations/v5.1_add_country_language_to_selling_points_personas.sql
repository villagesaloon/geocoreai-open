-- v5.1: 为卖点表和受众表增加 country 和 language 字段
-- 日期: 2026-03-24
-- 说明: 支持按国家/语言分类存储卖点和受众数据

-- 1. 卖点表增加 country 和 language 字段
ALTER TABLE geo_project_selling_points 
ADD COLUMN country VARCHAR(10) NOT NULL DEFAULT 'CN' AFTER project_id,
ADD COLUMN language VARCHAR(20) NOT NULL DEFAULT 'zh-CN' AFTER country;

-- 2. 受众表增加 country 和 language 字段
ALTER TABLE geo_project_personas 
ADD COLUMN country VARCHAR(10) NOT NULL DEFAULT 'CN' AFTER project_id,
ADD COLUMN language VARCHAR(20) NOT NULL DEFAULT 'zh-CN' AFTER country;

-- 3. 为卖点表添加索引（按 project_id + country + language 查询）
CREATE INDEX idx_selling_points_project_country_lang 
ON geo_project_selling_points(project_id, country, language);

-- 4. 为受众表添加索引（按 project_id + country + language 查询）
CREATE INDEX idx_personas_project_country_lang 
ON geo_project_personas(project_id, country, language);
