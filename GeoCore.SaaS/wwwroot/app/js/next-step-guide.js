/**
 * 下一步引导组件 - GeoCore AI 用户体验优化
 * 根据当前页面和用户状态，智能推荐下一步操作
 */

const NextStepGuide = {
    // 页面流程定义
    workflows: {
        // 新用户流程
        newUser: [
            { page: 'dashboard', next: 'setup-wizard', label: '创建第一个项目', icon: 'fa-plus-circle', desc: '配置品牌信息和目标模型' },
            { page: 'setup-wizard', next: 'citation-tracker', label: '开始引用检测', icon: 'fa-search', desc: '检测品牌在 AI 中的引用情况' },
            { page: 'citation-tracker', next: 'content-quality', label: '评估内容质量', icon: 'fa-check-double', desc: '分析内容的 AI 可引用性' },
            { page: 'content-quality', next: 'citation-sources', label: '获取优化策略', icon: 'fa-lightbulb', desc: '生成平台特定优化建议' },
            { page: 'citation-sources', next: 'visibility-analysis', label: '查看可见度分析', icon: 'fa-chart-line', desc: '综合评估 AI 可见度' }
        ],
        // 优化流程
        optimization: [
            { page: 'visibility-analysis', next: 'insight-analysis', label: '查看洞察分析', icon: 'fa-brain', desc: '获取四信号分析和行动建议' },
            { page: 'insight-analysis', next: 'content-quality', label: '优化内容质量', icon: 'fa-edit', desc: '根据建议优化内容' },
            { page: 'content-quality', next: 'citation-benchmark', label: '对比引用基准', icon: 'fa-balance-scale', desc: '与平台基准对比' },
            { page: 'citation-benchmark', next: 'citation-sources', label: '制定平台策略', icon: 'fa-bullseye', desc: '生成 30 天优化路线图' },
            { page: 'citation-sources', next: 'content-publish', label: '发布优化内容', icon: 'fa-paper-plane', desc: '一键发布到各平台' }
        ],
        // 监控流程
        monitoring: [
            { page: 'citation-tracker', next: 'visibility-analysis', label: '查看可见度变化', icon: 'fa-chart-area', desc: '追踪优化效果' },
            { page: 'visibility-analysis', next: 'citation-source', label: '分析引用来源', icon: 'fa-link', desc: '了解引用来源分布' },
            { page: 'citation-source', next: 'competitors', label: '竞品对比', icon: 'fa-users', desc: '与竞品对比分析' }
        ]
    },

    // 页面上下文映射
    pageContext: {
        'dashboard': { title: '仪表盘', category: 'overview' },
        'setup-wizard': { title: '项目配置', category: 'setup' },
        'projects': { title: '项目管理', category: 'setup' },
        'citation-tracker': { title: '引用追踪', category: 'detection' },
        'citation-benchmark': { title: '引用基准', category: 'analysis' },
        'citation-source': { title: '引用来源', category: 'analysis' },
        'citation-sources': { title: '引用源优化', category: 'optimization' },
        'content-quality': { title: '内容质量', category: 'optimization' },
        'content-analysis': { title: '内容分析', category: 'analysis' },
        'visibility-analysis': { title: '可见度分析', category: 'analysis' },
        'dimension-analysis': { title: '多维分析', category: 'analysis' },
        'insight-analysis': { title: '洞察分析', category: 'analysis' },
        'content-publish': { title: '内容发布', category: 'publish' },
        'competitors': { title: '竞品分析', category: 'analysis' },
        'llms-txt': { title: 'llms.txt', category: 'optimization' },
        'site-audit': { title: '网站审计', category: 'audit' }
    },

    // 获取当前页面名称
    getCurrentPage() {
        const path = window.location.pathname;
        const filename = path.split('/').pop().replace('.html', '');
        return filename || 'dashboard';
    },

    // 获取下一步建议
    getNextStep() {
        const currentPage = this.getCurrentPage();
        const hasProject = !!localStorage.getItem('geo_current_project');
        
        // 根据用户状态选择流程
        let workflow = hasProject ? this.workflows.optimization : this.workflows.newUser;
        
        // 查找当前页面的下一步
        const step = workflow.find(s => s.page === currentPage);
        if (step) return step;

        // 如果当前页面不在流程中，返回默认建议
        return this.getDefaultNextStep(currentPage, hasProject);
    },

    // 获取默认下一步
    getDefaultNextStep(currentPage, hasProject) {
        if (!hasProject) {
            return { next: 'setup-wizard', label: '创建项目', icon: 'fa-plus-circle', desc: '开始您的 GEO 优化之旅' };
        }

        const context = this.pageContext[currentPage];
        if (!context) {
            return { next: 'dashboard', label: '返回仪表盘', icon: 'fa-home', desc: '查看整体概览' };
        }

        // 根据页面类别推荐
        switch (context.category) {
            case 'setup':
                return { next: 'citation-tracker', label: '开始检测', icon: 'fa-search', desc: '检测 AI 引用情况' };
            case 'detection':
                return { next: 'content-quality', label: '评估内容', icon: 'fa-check-double', desc: '分析内容可引用性' };
            case 'analysis':
                return { next: 'citation-sources', label: '获取策略', icon: 'fa-lightbulb', desc: '生成优化建议' };
            case 'optimization':
                return { next: 'content-publish', label: '发布内容', icon: 'fa-paper-plane', desc: '一键发布到平台' };
            case 'publish':
                return { next: 'citation-tracker', label: '追踪效果', icon: 'fa-chart-line', desc: '监控发布效果' };
            default:
                return { next: 'dashboard', label: '返回仪表盘', icon: 'fa-home', desc: '查看整体概览' };
        }
    },

    // 获取相关页面建议
    getRelatedPages() {
        const currentPage = this.getCurrentPage();
        const context = this.pageContext[currentPage];
        if (!context) return [];

        const related = [];
        
        // 根据类别推荐相关页面
        switch (context.category) {
            case 'detection':
                related.push(
                    { page: 'citation-benchmark', label: '引用基准', icon: 'fa-balance-scale' },
                    { page: 'citation-source', label: '引用来源', icon: 'fa-link' }
                );
                break;
            case 'analysis':
                related.push(
                    { page: 'content-quality', label: '内容质量', icon: 'fa-check-double' },
                    { page: 'competitors', label: '竞品分析', icon: 'fa-users' }
                );
                break;
            case 'optimization':
                related.push(
                    { page: 'llms-txt', label: 'llms.txt', icon: 'fa-file-code' },
                    { page: 'site-audit', label: '网站审计', icon: 'fa-search' }
                );
                break;
        }

        return related.filter(r => r.page !== currentPage);
    },

    // 渲染下一步引导组件
    render(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const nextStep = this.getNextStep();
        const relatedPages = this.getRelatedPages();

        let html = `
            <div class="bg-gradient-to-r from-indigo-50 to-purple-50 rounded-xl p-5 border border-indigo-100">
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-4">
                        <div class="w-12 h-12 bg-indigo-100 rounded-full flex items-center justify-center">
                            <i class="fas ${nextStep.icon} text-indigo-600 text-xl"></i>
                        </div>
                        <div>
                            <p class="text-sm text-gray-500">下一步</p>
                            <p class="font-semibold text-gray-900">${nextStep.label}</p>
                            <p class="text-xs text-gray-500">${nextStep.desc}</p>
                        </div>
                    </div>
                    <a href="${nextStep.next}.html" class="px-5 py-2.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition flex items-center gap-2">
                        <span>前往</span>
                        <i class="fas fa-arrow-right"></i>
                    </a>
                </div>
        `;

        // 添加相关页面
        if (relatedPages.length > 0) {
            html += `
                <div class="mt-4 pt-4 border-t border-indigo-100">
                    <p class="text-xs text-gray-500 mb-2">相关功能</p>
                    <div class="flex gap-2">
            `;
            relatedPages.forEach(page => {
                html += `
                    <a href="${page.page}.html" class="px-3 py-1.5 bg-white text-gray-600 rounded-lg text-sm hover:bg-gray-50 transition flex items-center gap-2 border border-gray-200">
                        <i class="fas ${page.icon} text-gray-400"></i>
                        <span>${page.label}</span>
                    </a>
                `;
            });
            html += `
                    </div>
                </div>
            `;
        }

        html += `</div>`;
        container.innerHTML = html;
    },

    // 渲染简洁版引导（用于页面底部）
    renderCompact(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const nextStep = this.getNextStep();

        container.innerHTML = `
            <div class="flex items-center justify-between p-4 bg-indigo-50 rounded-lg border border-indigo-100">
                <div class="flex items-center gap-3">
                    <i class="fas ${nextStep.icon} text-indigo-600"></i>
                    <span class="text-gray-700">下一步: <strong>${nextStep.label}</strong></span>
                    <span class="text-gray-400 text-sm">- ${nextStep.desc}</span>
                </div>
                <a href="${nextStep.next}.html" class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition text-sm">
                    前往 <i class="fas fa-arrow-right ml-1"></i>
                </a>
            </div>
        `;
    },

    // 自动初始化
    init() {
        // 查找页面中的引导容器
        const fullContainer = document.getElementById('nextStepGuide');
        const compactContainer = document.getElementById('nextStepGuideCompact');

        if (fullContainer) this.render('nextStepGuide');
        if (compactContainer) this.renderCompact('nextStepGuideCompact');
    }
};

// 页面加载后自动初始化
document.addEventListener('DOMContentLoaded', () => {
    NextStepGuide.init();
});
