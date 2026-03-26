/**
 * GCore Admin 后台导航组件
 */

const AdminNav = {
    // 导航配置
    config: {
        navItems: [
            { section: '概览', items: [
                { id: 'dashboard', href: 'dashboard.html', icon: 'fa-tachometer-alt', label: '仪表盘' }
            ]},
            { section: '用户管理', items: [
                { id: 'users', href: 'users.html', icon: 'fa-users', label: 'SaaS 用户' },
                { id: 'admins', href: 'admins.html', icon: 'fa-user-shield', label: '管理员' }
            ]},
            { section: '数据查看', items: [
                { id: 'analytics', href: 'analytics.html', icon: 'fa-chart-bar', label: '数据统计' },
                { id: 'logs', href: 'logs.html', icon: 'fa-file-alt', label: '操作日志' }
            ]},
            { section: '内容管理', items: [
                { id: 'cms', href: 'cms.html', icon: 'fa-newspaper', label: '内容管理 (CMS)' }
            ]},
            { section: '系统配置', items: [
                { id: 'settings', href: 'settings.html', icon: 'fa-cog', label: '系统设置' },
                { id: 'model-config', href: 'model-config.html', icon: 'fa-brain', label: '模型配置' },
                { id: 'api-keys', href: 'api-keys.html', icon: 'fa-key', label: 'API 密钥' },
                { id: 'prompt-config', href: 'prompt-config.html', icon: 'fa-file-code', label: 'Prompt 配置' },
                { id: 'system-config', href: 'system-config.html', icon: 'fa-sliders-h', label: '系统参数' },
                { id: 'platform-config', href: 'platform-config.html', icon: 'fa-server', label: '平台配置' },
                { id: 'content-publish-config', href: 'content-publish-config.html', icon: 'fa-paper-plane', label: '内容发布配置' }
            ]}
        ]
    },

    // 获取当前页面ID
    getCurrentPageId() {
        const path = window.location.pathname;
        const filename = path.split('/').pop().replace('.html', '');
        return filename || 'dashboard';
    },

    // 获取当前管理员
    getCurrentAdmin() {
        const stored = localStorage.getItem('adminUser');
        if (stored) {
            try {
                return JSON.parse(stored);
            } catch (e) {
                return null;
            }
        }
        return null;
    },

    // 渲染导航HTML
    renderNav(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const currentPageId = this.getCurrentPageId();
        const admin = this.getCurrentAdmin();

        let html = `
            <div class="p-6 border-b border-gray-100">
                <a href="dashboard.html" class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center">
                        <i class="fas fa-shield-alt text-white"></i>
                    </div>
                    <div>
                        <h1 class="font-bold text-gray-900">GCore Admin</h1>
                        <p class="text-xs text-gray-500">运营管理后台</p>
                    </div>
                </a>
            </div>
        `;

        // 管理员信息
        if (admin) {
            html += `
                <div class="p-3 mx-3 mt-3 bg-indigo-50 rounded-lg border border-indigo-100">
                    <div class="flex items-center gap-2">
                        <div class="w-8 h-8 bg-indigo-600 rounded-full flex items-center justify-center text-white text-sm font-bold">
                            ${(admin.displayName || admin.username || 'A').charAt(0).toUpperCase()}
                        </div>
                        <div class="flex-1 min-w-0">
                            <p class="text-sm font-semibold text-gray-900 truncate">${admin.displayName || admin.username}</p>
                            <p class="text-xs text-indigo-600">${admin.role || 'admin'}</p>
                        </div>
                    </div>
                </div>
            `;
        }

        html += `<nav class="p-3 space-y-0.5 text-sm overflow-y-auto" style="max-height: calc(100vh - 200px);">`;

        // 渲染导航项
        this.config.navItems.forEach(section => {
            html += `
                <p class="text-xs text-gray-400 uppercase tracking-wider mt-6 mb-2 px-3">
                    ${section.section}
                </p>
            `;

            section.items.forEach(item => {
                const isActive = currentPageId === item.id;
                const activeClass = isActive ? 'active' : '';
                const textClass = isActive ? '' : 'text-gray-600';

                html += `
                    <a href="${item.href}" 
                       class="sidebar-link ${activeClass} flex items-center gap-3 px-3 py-2.5 rounded-lg ${textClass}">
                        <i class="fas ${item.icon} w-5 text-center"></i>
                        <span>${item.label}</span>
                    </a>
                `;
            });
        });

        // 退出登录
        html += `
            <div class="mt-8 pt-4 border-t border-gray-200">
                <button onclick="AdminNav.logout()" class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-red-600 hover:bg-red-50 transition">
                    <i class="fas fa-sign-out-alt w-5 text-center"></i>
                    <span>退出登录</span>
                </button>
            </div>
        `;

        html += '</nav>';
        container.innerHTML = html;
    },

    // 退出登录
    logout() {
        if (confirm('确定要退出登录吗？')) {
            localStorage.removeItem('adminLoggedIn');
            localStorage.removeItem('adminToken');
            localStorage.removeItem('adminUser');
            window.location.href = 'login.html';
        }
    },

    // 检查登录状态
    checkAuth() {
        if (localStorage.getItem('adminLoggedIn') !== 'true') {
            window.location.href = 'login.html';
            return false;
        }
        return true;
    },

    // 初始化
    init() {
        // 检查登录状态
        if (!this.checkAuth()) return;
        
        // 渲染导航
        this.renderNav('sidebar');
    }
};

// 页面加载时初始化
document.addEventListener('DOMContentLoaded', () => {
    AdminNav.init();
});
