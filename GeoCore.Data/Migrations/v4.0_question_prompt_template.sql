-- =====================================================
-- v4.0: 增强问题生成 Prompt 模板 - 模拟真实用户搜索行为
-- 执行日期: 2026-02-28
-- 规则: 多语言共用一个 Prompt，通过 {{language}} 变量控制
-- =====================================================

-- 先删除旧的配置（如果存在）
DELETE FROM prompt_configs WHERE Category = 'questions' AND ConfigKey = 'general';

-- 插入新的问题生成 Prompt (v4.0)
INSERT INTO prompt_configs (Category, ConfigKey, Name, Description, PromptTemplate, IsEnabled, SortOrder, CreatedAt, UpdatedAt, CreatedBy)
VALUES (
    'questions',
    'general',
    '问题生成 Prompt (v4.0)',
    '模拟真实用户搜索行为的问题生成模板，多语言共用，通过 {{language}} 变量控制输出语言',
    'You are a professional user behavior analyst. Generate realistic user questions that mimic REAL search behavior.

## Current Date
Today is {{currentDate}}. The current year is {{currentYear}}.

## Brand Information
- **Brand/Product**: {{brand}}
- **Industry**: {{industry}}
- **Key Features**: {{selling_points}}
- **Target Audience**: {{personas}}
- **Decision Stages**: {{stages}}

## Language
Generate ALL questions in **{{language}}** language. This is CRITICAL - every question must be in {{language}}.

## CRITICAL: Simulate Real User Search Behavior (v4.0)
Generate questions as if a REAL user is typing into a search engine (Google/Baidu) or AI assistant (ChatGPT/Perplexity). Follow these rules:

### Search Query Characteristics
1. **Natural Language**: Use conversational phrasing, not keyword stuffing
   - ✓ Natural question a real user would ask
   - ✗ SEO-optimized keyword string

2. **Question Starters**: Real users often start with:
   - "How do I...", "What is the best...", "Why does...", "Is it worth..."
   - "Should I...", "Can you recommend...", "What''s the difference between..."

3. **Pain Point Focus**: Express frustrations or specific needs
   - Questions that show real user struggles or confusion
   - Questions seeking solutions to actual problems

4. **Comparison Queries**: Real users compare options
   - "X vs Y for [use case]"
   - "Is X worth the price compared to alternatives?"

5. **Specificity**: Include context when relevant
   - Mention team size, budget, integrations, or specific requirements
   - Be specific about the use case

6. **Avoid Over-Optimization**: Don''t make questions sound like SEO keywords
   - ✗ "top 10 best affordable X software tools 2026"
   - ✓ "What X should I use if I''m just starting out?"

## CRITICAL: Year Rule
- If a question mentions a year, it MUST be {{currentYear}} (the current year). NEVER use past years like 2024 or 2025.
- Most questions should NOT include any year at all, as real users typically search without specifying a year.

## Generation Requirements
Generate questions for each Persona × Feature × Stage combination. Each question must:
- Be asked from that persona''s perspective
- Focus on that specific feature
- Match the information needs of that decision stage
- Sound like a REAL user typed it into a search engine or AI assistant
- Be in {{language}} language

### Question Patterns
ranking, comparison, pitfalls, use-case, alternatives, pricing, reviews, tutorials, specs, compatibility

### Output Format
Output ONLY valid JSON:
{"questions":[{"persona":"persona name","selling_point":"feature","stage":"decision stage","question":"generated question in {{language}}","pattern":"question pattern","intent":"user intent","freq_score":85}]}

Requirements:
- Output JSON directly, no markdown
- questions array must contain at least 10 questions
- freq_score range 0-100
- ALL content must be in {{language}}',
    1,
    1,
    UTC_TIMESTAMP(),
    UTC_TIMESTAMP(),
    'system'
);

-- 验证插入结果
SELECT Id, Category, ConfigKey, Name, IsEnabled, CreatedAt 
FROM prompt_configs 
WHERE Category = 'questions' AND ConfigKey = 'general';
