/**
 * GCore 统一导航组件
 * 区分全局导航和项目级导航
 */

const GCoreNav = {
    // 导航配置
    config: {
        // 全局导航项（不需要项目上下文）
        globalNav: [
            { section: '概览', items: [
                { id: 'dashboard', href: 'dashboard.html', icon: 'fa-tachometer-alt', label: '今日情报站' },
                { id: 'index', href: 'index.html', icon: 'fa-home', label: '仪表盘' },
                { id: 'projects', href: 'projects.html', icon: 'fa-folder', label: '项目管理' }
            ]},
            { section: '配置', step: '①', items: [
                { id: 'setup-wizard', href: 'setup-wizard.html', icon: 'fa-plus-circle', label: '新建项目' }
            ]},
            { section: '工具', items: [
                { id: 'docs', href: 'docs.html', icon: 'fa-book', label: '文档中心' }
            ]}
        ],
        
        // 项目级导航项（需要项目上下文）
        projectNav: [
            { section: '检测与分析', step: '②', items: [
                { id: 'visibility-analysis', href: 'visibility-analysis.html', icon: 'fa-chart-radar', label: '可见度分析' },
                { id: 'content-analysis', href: 'content-analysis.html', icon: 'fa-file-alt', label: '内容分析' },
                { id: 'competitors', href: 'competitors.html', icon: 'fa-users', label: '竞品分析' },
                { id: 'citation-tracker', href: 'citation-tracker.html', icon: 'fa-quote-left', label: '引用追踪' }
            ]},
            { section: '高级分析', step: '③', items: [
                { id: 'dimension-analysis', href: 'dimension-analysis.html', icon: 'fa-cubes', label: '三维分析' }
            ]},
            { section: '优化', step: '④', items: [
                { id: 'optimization-wizard', href: 'optimization-wizard.html', icon: 'fa-bolt', label: '优化向导' },
                { id: 'strategy-center', href: 'strategy-center.html', icon: 'fa-bullseye', label: '策略中心' },
                { id: 'prompt-studio', href: 'prompt-studio.html', icon: 'fa-magic', label: 'Prompt 工作室' },
                { id: 'distillation', href: 'distillation.html', icon: 'fa-filter', label: '蒸馏工作室' },
                { id: 'content-adapter', href: 'content-adapter.html', icon: 'fa-exchange-alt', label: '内容适配' },
                { id: 'channel-optimizer', href: 'channel-optimizer.html', icon: 'fa-broadcast-tower', label: '渠道优化' },
                { id: 'test-center', href: 'test-center.html', icon: 'fa-flask', label: '测试中心' }
            ]},
            { section: '监控', step: '⑤', items: [
                { id: 'monitoring', href: 'monitoring.html', icon: 'fa-chart-line', label: '效果监控' }
            ]},
            { section: '高级功能', items: [
                { id: 'trust-score', href: 'trust-score.html', icon: 'fa-shield-alt', label: 'TrustScore' },
                { id: 'llms-txt', href: 'llms-txt.html', icon: 'fa-file-code', label: 'llms.txt 生成' }
            ]}
        ]
    },

    // 获取当前项目
    getCurrentProject() {
        const stored = localStorage.getItem('geo_current_project');
        if (stored) {
            try {
                return JSON.parse(stored);
            } catch (e) {
                return null;
            }
        }
        return null;
    },

    // 获取当前页面ID
    getCurrentPageId() {
        const path = window.location.pathname;
        const filename = path.split('/').pop().replace('.html', '');
        return filename || 'index';
    },

    // 渲染导航HTML
    renderNav(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const currentProject = this.getCurrentProject();
        const currentPageId = this.getCurrentPageId();
        const hasProject = !!currentProject;

        let html = `
            <div class="p-4 border-b border-gray-100">
                <a href="index.html" class="flex items-center gap-2">
                    <img src="images/logo.svg" alt="GeoCore AI" class="h-9">
                </a>
            </div>
        `;

        // 当前项目上下文
        if (hasProject) {
            html += `
                <div class="p-3 mx-3 mt-3 bg-indigo-50 rounded-lg border border-indigo-100">
                    <div class="flex items-center justify-between">
                        <p class="text-xs text-indigo-600 font-medium">当前项目</p>
                        <button onclick="GCoreNav.clearProject()" class="text-xs text-gray-400 hover:text-red-500" title="切换项目">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                    <p class="text-sm font-semibold text-gray-900 truncate mt-1">${currentProject.project?.name || '未命名项目'}</p>
                    <a href="project-detail.html?id=${currentProject.id}" class="text-xs text-indigo-600 hover:underline mt-1 inline-block">
                        查看详情 →
                    </a>
                </div>
            `;
        }

        html += `<nav class="p-3 space-y-0.5 text-sm overflow-y-auto" style="max-height: calc(100vh - ${hasProject ? '220px' : '88px'});">`;

        // 渲染全局导航
        this.config.globalNav.forEach(section => {
            html += this.renderSection(section, currentPageId, true);
        });

        // 渲染项目级导航
        if (hasProject) {
            this.config.projectNav.forEach(section => {
                html += this.renderSection(section, currentPageId, true);
            });
        } else {
            // 显示项目级导航但禁用
            html += `
                <div class="mt-6 mx-3 p-3 bg-gray-50 rounded-lg border border-dashed border-gray-300">
                    <p class="text-xs text-gray-500 text-center mb-2">
                        <i class="fas fa-info-circle mr-1"></i>选择项目后解锁更多功能
                    </p>
                    <a href="projects.html" class="block text-center text-xs text-indigo-600 hover:underline">
                        前往项目管理 →
                    </a>
                </div>
            `;
            
            // 显示禁用的项目级导航预览
            this.config.projectNav.forEach(section => {
                html += this.renderSection(section, currentPageId, false);
            });
        }

        html += '</nav>';
        container.innerHTML = html;
    },

    // 渲染导航分组
    renderSection(section, currentPageId, enabled) {
        let html = `
            <p class="text-xs text-gray-400 uppercase tracking-wider mt-6 mb-2 px-3">
                ${section.step ? section.step + ' ' : ''}${section.section}
            </p>
        `;

        section.items.forEach(item => {
            const isActive = currentPageId === item.id;
            const activeClass = isActive ? 'active' : '';
            const disabledClass = enabled ? '' : 'opacity-40 pointer-events-none';
            const textClass = isActive ? '' : 'text-gray-600';

            html += `
                <a href="${enabled ? item.href : '#'}" 
                   class="sidebar-link ${activeClass} ${disabledClass} flex items-center gap-3 px-3 py-2.5 rounded-lg ${textClass}">
                    <i class="fas ${item.icon} w-5 text-center"></i>
                    <span>${item.label}</span>
                    ${!enabled ? '<i class="fas fa-lock text-xs ml-auto text-gray-400"></i>' : ''}
                </a>
            `;
        });

        return html;
    },

    // 清除当前项目
    clearProject() {
        if (confirm('确定要切换项目吗？')) {
            localStorage.removeItem('geo_current_project');
            window.location.href = 'projects.html';
        }
    },

    // 设置当前项目
    setCurrentProject(project) {
        localStorage.setItem('geo_current_project', JSON.stringify(project));
        this.renderNav('sidebar');
    },

    // 检查是否需要项目上下文
    requiresProject(pageId) {
        for (const section of this.config.projectNav) {
            if (section.items.some(item => item.id === pageId)) {
                return true;
            }
        }
        return false;
    },

    // 检查并重定向（如果页面需要项目但没有项目）
    checkProjectRequired() {
        const currentPageId = this.getCurrentPageId();
        const currentProject = this.getCurrentProject();
        
        if (this.requiresProject(currentPageId) && !currentProject) {
            // 显示提示模态框或重定向
            this.showNoProjectModal();
            return false;
        }
        return true;
    },

    // 显示无项目提示
    showNoProjectModal() {
        const modal = document.getElementById('noProjectModal');
        if (modal) {
            modal.classList.remove('hidden');
            modal.classList.add('flex');
        } else {
            // 动态创建模态框
            const modalHtml = `
                <div id="noProjectModal" class="fixed inset-0 bg-black bg-opacity-50 z-50 flex items-center justify-center">
                    <div class="bg-white rounded-xl p-6 max-w-md mx-4 shadow-2xl">
                        <div class="text-center">
                            <div class="w-16 h-16 bg-amber-100 rounded-full flex items-center justify-center mx-auto mb-4">
                                <i class="fas fa-exclamation-triangle text-amber-500 text-2xl"></i>
                            </div>
                            <h3 class="text-lg font-bold text-gray-900 mb-2">未选择项目</h3>
                            <p class="text-gray-600 mb-6">请先选择一个项目或创建新项目，然后再使用此功能。</p>
                            <div class="flex gap-3 justify-center">
                                <a href="projects.html" class="px-4 py-2 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 transition">
                                    <i class="fas fa-folder mr-2"></i>选择项目
                                </a>
                                <a href="setup-wizard.html" class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition">
                                    <i class="fas fa-plus mr-2"></i>新建项目
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', modalHtml);
        }
    },

    // 初始化
    init() {
        // 渲染导航
        this.renderNav('sidebar');
        
        // 检查项目要求
        this.checkProjectRequired();
    }
};

// 页面加载时初始化
document.addEventListener('DOMContentLoaded', () => {
    GCoreNav.init();
});
