// SaaS App 侧边栏导航组件
(function() {
    const currentPage = window.location.pathname.split('/').pop() || 'dashboard.html';
    const currentProject = JSON.parse(localStorage.getItem('geo_current_project') || 'null');
    const hasProject = !!currentProject;
    
    // 全局导航 - 不需要选择项目
    const globalNavItems = [
        { icon: 'fa-home', label: '仪表盘', href: 'dashboard.html', id: 'dashboard' },
        { icon: 'fa-folder', label: '项目管理', href: 'projects.html', id: 'projects' },
        { icon: 'fa-plus-circle', label: '新建项目', href: 'setup-wizard.html', id: 'setup' }
    ];
    
    // 项目级导航 - 需要先选择项目（Phase 9.2 简化版）
    const projectNavItems = [
        { divider: true, label: '核心功能' },
        { icon: 'fa-eye', label: '可见度中心', href: 'visibility-center.html', id: 'visibility-center', requireProject: true, isNew: true },
        { icon: 'fa-quote-left', label: '引用中心', href: 'citation-center.html', id: 'citation-center', requireProject: true, isNew: true },
        { icon: 'fa-edit', label: '内容工作室', href: 'content-studio.html', id: 'content-studio', requireProject: true, isNew: true },
        { icon: 'fa-users', label: '竞品分析', href: 'competitors.html', id: 'competitors', requireProject: true },
        { icon: 'fa-search-plus', label: '网站审计', href: 'site-audit.html', id: 'site-audit', requireProject: true, isNew: true },
        { divider: true, label: '优化工具' },
        { icon: 'fa-lightbulb', label: '策略中心', href: 'strategy-center.html', id: 'strategy', requireProject: true },
        { icon: 'fa-magic', label: '优化向导', href: 'optimization-wizard.html', id: 'wizard', requireProject: true },
        { icon: 'fa-desktop', label: '实时监控', href: 'monitoring.html', id: 'monitoring', requireProject: true },
        { divider: true, label: '辅助功能' },
        { icon: 'fa-users', label: 'Persona 买家角色', href: 'persona.html', id: 'persona', requireProject: false },
        { icon: 'fa-flask', label: '测试中心', href: 'test-center.html', id: 'test', requireProject: true },
        { divider: true, label: '原有页面（已废弃）' },
        { icon: 'fa-eye', label: '可见度分析', href: 'visibility-analysis.html', id: 'visibility', requireProject: true, legacy: true },
        { icon: 'fa-globe', label: '多维分析', href: 'dimension-analysis.html', id: 'dimension', requireProject: true, legacy: true },
        { icon: 'fa-quote-left', label: '引用追踪', href: 'citation-tracker.html', id: 'citation', requireProject: true, legacy: true },
        { icon: 'fa-chart-line', label: '内容质量评估', href: 'content-quality.html', id: 'quality', requireProject: true, legacy: true }
    ];
    
    // 独立工具 - 不需要项目
    const toolNavItems = [
        { divider: true, label: '独立工具' },
        { icon: 'fa-file-code', label: 'llms.txt 生成', href: 'llms-txt.html', id: 'llms' }
    ];

    function renderNavItem(item) {
        if (item.divider) {
            return `<div class="pt-4 mt-4 border-t border-gray-200">
                <div class="text-gray-400 text-xs uppercase tracking-wider px-3 mb-2">${item.label}</div>
            </div>`;
        }
        
        const isActive = currentPage === item.href;
        const isDisabled = item.requireProject && !hasProject;
        
        if (isDisabled) {
            return `
                <div class="sidebar-link flex items-center gap-3 px-3 py-2 rounded-lg text-gray-300 cursor-not-allowed" title="请先选择项目">
                    <i class="fas ${item.icon} w-5 text-center"></i>
                    <span class="text-sm font-medium">${item.label}</span>
                    <i class="fas fa-lock text-xs ml-auto"></i>
                </div>
            `;
        }
        
        const activeClass = isActive ? 'bg-indigo-50 text-indigo-600 border-r-3 border-indigo-600' : 'text-gray-600 hover:bg-gray-50';
        const legacyClass = item.legacy ? 'opacity-60' : '';
        const newBadge = item.isNew ? '<span class="ml-auto px-1.5 py-0.5 bg-green-100 text-green-700 text-xs rounded">新</span>' : '';
        const legacyBadge = item.legacy ? '<span class="ml-auto px-1.5 py-0.5 bg-gray-100 text-gray-500 text-xs rounded">旧</span>' : '';
        return `
            <a href="${item.href}" class="sidebar-link flex items-center gap-3 px-3 py-2 rounded-lg ${activeClass} ${legacyClass}">
                <i class="fas ${item.icon} w-5 text-center"></i>
                <span class="text-sm font-medium">${item.label}</span>
                ${newBadge}${legacyBadge}
            </a>
        `;
    }

    function renderNavigation() {
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) return;

        let html = `
            <div class="p-4 border-b border-gray-200">
                <a href="../index.html" class="flex items-center gap-2">
                    <img src="../images/logo.svg" alt="GeoCore AI" class="h-9">
                </a>
            </div>
        `;
        
        // 当前项目指示器
        if (hasProject) {
            // 兼容多种数据结构：brandName（后端API）、project.name（旧格式）
            const projectName = currentProject.brandName || currentProject.project?.name || '未命名项目';
            const projectId = currentProject.id || currentProject.projectId;
            html += `
                <div class="px-4 py-3 bg-indigo-50 border-b border-indigo-100">
                    <div class="flex items-center gap-2">
                        <i class="fas fa-folder-open text-indigo-600"></i>
                        <div class="flex-1 min-w-0">
                            <p class="text-xs text-gray-500">当前项目</p>
                            <a href="project-detail.html?id=${projectId}" class="text-sm font-medium text-indigo-600 truncate hover:underline block">${projectName}</a>
                        </div>
                        <a href="projects.html" class="text-xs text-indigo-500 hover:underline">切换</a>
                    </div>
                </div>
            `;
        }
        
        html += `<nav class="p-4 space-y-1 overflow-y-auto" style="height: calc(100vh - ${hasProject ? '180px' : '130px'});">`;
        
        // 渲染全局导航
        globalNavItems.forEach(item => {
            html += renderNavItem(item);
        });
        
        // 渲染项目级导航
        projectNavItems.forEach(item => {
            html += renderNavItem(item);
        });
        
        // 渲染独立工具
        toolNavItems.forEach(item => {
            html += renderNavItem(item);
        });

        html += `
            </nav>
            <div class="absolute bottom-0 left-0 right-0 p-4 border-t border-gray-200 bg-white">
                <a href="../login.html" onclick="localStorage.clear();" class="flex items-center gap-3 px-3 py-2 text-gray-600 hover:bg-gray-50 rounded-lg">
                    <i class="fas fa-sign-out-alt w-5 text-center"></i>
                    <span class="text-sm font-medium">退出登录</span>
                </a>
            </div>
        `;

        sidebar.innerHTML = html;
    }

    // 页面加载后渲染导航
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', renderNavigation);
    } else {
        renderNavigation();
    }
})();
