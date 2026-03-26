/**
 * Google Trends 客户端 API
 * 在用户浏览器中执行，避免服务器被 Google 封禁
 * 
 * 注意：由于 CORS 限制，直接从浏览器请求 Google Trends API 会失败
 * 解决方案：
 * 1. 使用服务器端代理（但会有被封风险）
 * 2. 使用浏览器扩展（需要用户安装）
 * 3. 使用 Google Trends Embed Widget（只能展示，无法获取数据）
 * 
 * 本实现采用方案 1 的变体：通过后端轻量代理，但限制频率
 */

class GoogleTrendsClient {
    constructor(options = {}) {
        // 代理服务器地址（后端提供，用于绕过 CORS）
        this.proxyUrl = options.proxyUrl || '/api/trends/proxy';
        // 请求间隔（毫秒），避免被限流
        this.requestDelay = options.requestDelay || 1000;
        // 最后请求时间
        this.lastRequestTime = 0;
    }

    /**
     * 获取关键词的搜索热度数据
     * @param {string} keyword - 搜索关键词
     * @param {object} options - 可选参数
     * @returns {Promise<{avgHeat: number, recentHeat: number, relatedQueries: string[]}>}
     */
    async getTrendData(keyword, options = {}) {
        const geo = options.geo || '';  // 地区，如 'US', 'CN'
        const time = options.time || 'today 12-m';  // 时间范围
        const hl = options.hl || 'en-US';  // 语言

        // 限流控制
        await this._waitForRateLimit();

        try {
            // Step 1: 获取 explore 数据（包含 token）
            const exploreData = await this._fetchExplore(keyword, geo, time, hl);
            if (!exploreData || !exploreData.widgets) {
                return { avgHeat: 0, recentHeat: 0, relatedQueries: [], error: 'No explore data' };
            }

            // Step 2: 获取时间序列数据
            const timeseriesWidget = exploreData.widgets.find(w => w.id === 'TIMESERIES');
            let avgHeat = 0, recentHeat = 0;
            
            if (timeseriesWidget) {
                const timelineData = await this._fetchWidgetData(
                    'multiline',
                    timeseriesWidget.token,
                    timeseriesWidget.request,
                    hl
                );
                
                if (timelineData && timelineData.default && timelineData.default.timelineData) {
                    const points = timelineData.default.timelineData;
                    if (points.length > 0) {
                        const sum = points.reduce((acc, p) => acc + (p.value[0] || 0), 0);
                        avgHeat = Math.round(sum / points.length);
                        recentHeat = points[points.length - 1].value[0] || 0;
                    }
                }
            }

            // Step 3: 获取相关查询
            const relatedWidget = exploreData.widgets.find(w => w.id === 'RELATED_QUERIES');
            let relatedQueries = [];
            
            if (relatedWidget) {
                const relatedData = await this._fetchWidgetData(
                    'relatedsearches',
                    relatedWidget.token,
                    relatedWidget.request,
                    hl
                );
                
                if (relatedData && relatedData.default && relatedData.default.rankedList) {
                    for (const list of relatedData.default.rankedList) {
                        if (list.rankedKeyword) {
                            relatedQueries = relatedQueries.concat(
                                list.rankedKeyword.map(k => k.query)
                            );
                        }
                    }
                }
            }

            return {
                keyword,
                avgHeat,
                recentHeat,
                relatedQueries: relatedQueries.slice(0, 10),  // 最多返回 10 个
                success: true
            };

        } catch (error) {
            console.error('GoogleTrends error:', error);
            return {
                keyword,
                avgHeat: 0,
                recentHeat: 0,
                relatedQueries: [],
                success: false,
                error: error.message
            };
        }
    }

    /**
     * 批量获取多个关键词的热度数据
     * @param {string[]} keywords - 关键词数组
     * @param {object} options - 可选参数
     * @returns {Promise<Array>}
     */
    async getBatchTrendData(keywords, options = {}) {
        const results = [];
        for (const keyword of keywords) {
            const result = await this.getTrendData(keyword, options);
            results.push(result);
        }
        return results;
    }

    /**
     * 获取自动补全建议
     * @param {string} keyword - 搜索关键词
     * @returns {Promise<Array<{title: string, type: string}>>}
     */
    async getAutoComplete(keyword) {
        await this._waitForRateLimit();

        try {
            const url = `https://trends.google.com/trends/api/autocomplete/${encodeURIComponent(keyword)}?hl=en-US`;
            const response = await this._proxyFetch(url);
            const data = this._parseGoogleResponse(response);
            
            if (data && data.default && data.default.topics) {
                return data.default.topics.map(t => ({
                    title: t.title,
                    type: t.type
                }));
            }
            return [];
        } catch (error) {
            console.error('AutoComplete error:', error);
            return [];
        }
    }

    // ==================== 私有方法 ====================

    /**
     * 限流等待
     */
    async _waitForRateLimit() {
        const now = Date.now();
        const elapsed = now - this.lastRequestTime;
        if (elapsed < this.requestDelay) {
            await new Promise(resolve => setTimeout(resolve, this.requestDelay - elapsed));
        }
        this.lastRequestTime = Date.now();
    }

    /**
     * 获取 explore 数据
     */
    async _fetchExplore(keyword, geo, time, hl) {
        const req = JSON.stringify({
            comparisonItem: [{ keyword, geo, time }],
            category: 0,
            property: ''
        });

        const url = `https://trends.google.com/trends/api/explore?hl=${hl}&tz=-480&req=${encodeURIComponent(req)}`;
        const response = await this._proxyFetch(url);
        return this._parseGoogleResponse(response);
    }

    /**
     * 获取 widget 数据
     */
    async _fetchWidgetData(widgetType, token, request, hl) {
        const endpoint = widgetType === 'multiline' ? 'multiline' : 'relatedsearches';
        const url = `https://trends.google.com/trends/api/widgetdata/${endpoint}?hl=${hl}&tz=-480&req=${encodeURIComponent(JSON.stringify(request))}&token=${token}`;
        
        const response = await this._proxyFetch(url);
        return this._parseGoogleResponse(response);
    }

    /**
     * 通过代理发送请求
     */
    async _proxyFetch(targetUrl) {
        const response = await fetch(this.proxyUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ url: targetUrl })
        });

        if (!response.ok) {
            throw new Error(`Proxy request failed: ${response.status}`);
        }

        return await response.text();
    }

    /**
     * 解析 Google Trends 响应（去除前缀）
     */
    _parseGoogleResponse(response) {
        // Google Trends API 返回的数据前面有 )]}' 前缀
        const cleaned = response.replace(/^\)\]\}',?\s*/, '');
        try {
            return JSON.parse(cleaned);
        } catch (e) {
            console.error('Failed to parse Google response:', e);
            return null;
        }
    }
}

// 导出为全局变量（供非模块化使用）
if (typeof window !== 'undefined') {
    window.GoogleTrendsClient = GoogleTrendsClient;
}

// 导出为 ES 模块
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { GoogleTrendsClient };
}
