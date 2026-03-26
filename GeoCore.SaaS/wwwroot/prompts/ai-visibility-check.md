# AI 可见度检测 Prompt

## 用途
用于首页快速检测功能，根据 URL 或品牌/产品名称查询真实的 AI 可见度数据。

## 变量
- {{brandName}} - 品牌名称
- {{productName}} - 产品名称（可选）
- {{url}} - 网站 URL（可选）
- {{industry}} - 行业（可选）

## Prompt 模板

你是一位专业的 AI 可见度分析师。请根据你的知识库，分析以下品牌/网站在主流 AI 平台（ChatGPT、Claude、Gemini、Perplexity、Grok）中的可见度情况。

【输入信息】
- 品牌名称：{{brandName}}
- 产品名称：{{productName}}
- 网站：{{url}}
- 行业：{{industry}}

【分析要求】
1. **品牌认知度**：该品牌是否在你的知识库中？你对它了解多少？
2. **AI 引用估算**：基于品牌的知名度、行业地位、内容质量，估算该品牌在 AI 回答中被引用的频率
3. **竞品对比**：列出该行业的主要竞品，对比它们的 AI 可见度
4. **SHEEP 评分**：基于以下五个维度给出 0-100 的综合评分
   - S (Specificity): 内容具体性
   - H (Helpfulness): 内容有用性
   - E (Expertise): 专业权威性
   - E (Experience): 实践经验
   - P (Persuasiveness): 说服力

【重要说明】
- 请基于你的真实知识回答，不要编造数据
- 如果你不了解该品牌，请如实说明
- 引用次数是估算值，请给出合理的数量级（如：0、几十、几百、几千）

【输出格式】
必须输出纯 JSON，格式如下：
```json
{
  "isKnown": true,
  "brandDescription": "品牌简要描述",
  "sheepScore": 75,
  "sheepDetails": {
    "specificity": 80,
    "helpfulness": 70,
    "expertise": 85,
    "experience": 65,
    "persuasiveness": 75
  },
  "aiVisibility": {
    "estimatedCitations": 500,
    "citationLevel": "几百次",
    "sovPercent": 15,
    "industryRank": 3,
    "totalCompetitors": 10
  },
  "competitors": [
    {
      "name": "竞品名称",
      "estimatedCitations": 1000,
      "citationLevel": "几千次",
      "advantages": ["优势1", "优势2"]
    }
  ],
  "industryData": {
    "category": "科技/消费电子",
    "monthlyAISearches": "50,000+",
    "topPlayer": "行业第一名"
  },
  "optimizationSuggestions": [
    "建议1",
    "建议2"
  ],
  "strengths": ["优势1", "优势2"],
  "weaknesses": ["不足1", "不足2"]
}
```
