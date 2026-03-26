/**
 * Firebase 配置和认证模块
 * GeoCore AI - Google 登录集成
 */

// Firebase 配置
const firebaseConfig = {
    apiKey: "AIzaSyA-3N-CCGLEqn6Yh5fcwCIkqyKr_g3z6PI",
    authDomain: "geocoreai.firebaseapp.com",
    projectId: "geocoreai",
    storageBucket: "geocoreai.firebasestorage.app",
    messagingSenderId: "910069234630",
    appId: "1:910069234630:web:58c5883dd07ae4501490c7",
    measurementId: "G-D6WGG20LMC"
};

// 全局变量
let firebaseApp = null;
let firebaseAuth = null;

/**
 * 初始化 Firebase
 */
async function initFirebase() {
    if (firebaseApp) return;
    
    const { initializeApp } = await import('https://www.gstatic.com/firebasejs/11.9.0/firebase-app.js');
    const { getAuth, onAuthStateChanged } = await import('https://www.gstatic.com/firebasejs/11.9.0/firebase-auth.js');
    
    firebaseApp = initializeApp(firebaseConfig);
    firebaseAuth = getAuth(firebaseApp);
    
    // 监听登录状态变化
    onAuthStateChanged(firebaseAuth, (user) => {
        if (user) {
            // 用户已登录
            localStorage.setItem('isLoggedIn', 'true');
            localStorage.setItem('user', JSON.stringify({
                uid: user.uid,
                email: user.email,
                name: user.displayName,
                photoURL: user.photoURL,
                provider: user.providerData[0]?.providerId || 'unknown'
            }));
            
            // 触发自定义事件
            window.dispatchEvent(new CustomEvent('authStateChanged', { detail: { user } }));
        } else {
            // 用户已登出
            localStorage.removeItem('isLoggedIn');
            localStorage.removeItem('user');
            window.dispatchEvent(new CustomEvent('authStateChanged', { detail: { user: null } }));
        }
    });
    
    return firebaseAuth;
}

/**
 * Google 登录
 */
async function signInWithGoogle() {
    try {
        console.log('[GeoAuth] 开始 Google 登录...');
        await initFirebase();
        console.log('[GeoAuth] Firebase 初始化完成');
        
        const { GoogleAuthProvider, signInWithPopup } = await import('https://www.gstatic.com/firebasejs/11.9.0/firebase-auth.js');
        console.log('[GeoAuth] Firebase Auth 模块加载完成');
        
        const provider = new GoogleAuthProvider();
        provider.addScope('email');
        provider.addScope('profile');
        
        console.log('[GeoAuth] 打开 Google 登录弹窗...');
        const result = await signInWithPopup(firebaseAuth, provider);
        const user = result.user;
        
        console.log('[GeoAuth] Google 登录成功:', user.email);
        
        // 调用后端 API 保存用户到数据库
        console.log('[GeoAuth] 保存用户到数据库...');
        const dbUser = await saveUserToDatabase({
            firebaseUid: user.uid,
            email: user.email,
            displayName: user.displayName,
            photoUrl: user.photoURL,
            provider: user.providerData[0]?.providerId || 'google.com'
        });
        console.log('[GeoAuth] 数据库保存结果:', dbUser);
        
        return {
            success: true,
            user: {
                uid: user.uid,
                id: dbUser?.id,
                email: user.email,
                name: user.displayName,
                photoURL: user.photoURL
            }
        };
    } catch (error) {
        // 详细错误日志
        console.error('[GeoAuth] Google 登录失败');
        console.error('[GeoAuth] 错误代码:', error.code);
        console.error('[GeoAuth] 错误消息:', error.message);
        console.error('[GeoAuth] 完整错误:', error);
        
        let message = '登录失败，请重试';
        if (error.code === 'auth/popup-closed-by-user') {
            message = '登录窗口已关闭';
        } else if (error.code === 'auth/popup-blocked') {
            message = '弹窗被阻止，请允许弹窗后重试';
        } else if (error.code === 'auth/network-request-failed') {
            message = '网络错误，请检查网络连接';
        } else if (error.code === 'auth/unauthorized-domain') {
            message = '当前域名未授权，请在 Firebase Console 添加域名';
        } else if (error.code === 'auth/operation-not-allowed') {
            message = 'Google 登录未启用，请在 Firebase Console 启用';
        } else if (error.code === 'auth/cancelled-popup-request') {
            message = '登录请求被取消';
        } else if (error.message) {
            message = error.message;
        }
        
        return {
            success: false,
            error: message,
            code: error.code,
            details: error.message
        };
    }
}

/**
 * 保存用户到数据库
 */
async function saveUserToDatabase(userData) {
    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                firebaseUid: userData.firebaseUid,
                email: userData.email,
                displayName: userData.displayName,
                photoUrl: userData.photoUrl,
                provider: userData.provider
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            console.log('用户已保存到数据库:', result.user);
            // 更新本地存储的用户信息
            const currentUser = JSON.parse(localStorage.getItem('user') || '{}');
            localStorage.setItem('user', JSON.stringify({
                ...currentUser,
                dbId: result.user.id,
                role: result.user.role,
                loginCount: result.user.loginCount
            }));
            return result.user;
        } else {
            console.error('保存用户失败:', result.error);
            return null;
        }
    } catch (error) {
        console.error('调用 API 失败:', error);
        return null;
    }
}

/**
 * 登出
 */
async function signOut() {
    try {
        await initFirebase();
        
        const { signOut: firebaseSignOut } = await import('https://www.gstatic.com/firebasejs/11.9.0/firebase-auth.js');
        
        await firebaseSignOut(firebaseAuth);
        
        // 清除本地存储
        localStorage.removeItem('isLoggedIn');
        localStorage.removeItem('user');
        localStorage.removeItem('geo_current_project');
        
        console.log('已登出');
        return { success: true };
    } catch (error) {
        console.error('登出失败:', error);
        return { success: false, error: error.message };
    }
}

/**
 * 获取当前用户
 */
function getCurrentUser() {
    const userStr = localStorage.getItem('user');
    if (userStr) {
        try {
            return JSON.parse(userStr);
        } catch (e) {
            return null;
        }
    }
    return null;
}

/**
 * 检查是否已登录
 */
function isLoggedIn() {
    return localStorage.getItem('isLoggedIn') === 'true';
}

/**
 * 获取 Firebase ID Token (用于后端验证)
 */
async function getIdToken() {
    try {
        await initFirebase();
        
        if (firebaseAuth.currentUser) {
            return await firebaseAuth.currentUser.getIdToken();
        }
        return null;
    } catch (error) {
        console.error('获取 ID Token 失败:', error);
        return null;
    }
}

// 导出到全局
window.GeoAuth = {
    initFirebase,
    signInWithGoogle,
    signOut,
    getCurrentUser,
    isLoggedIn,
    getIdToken
};
