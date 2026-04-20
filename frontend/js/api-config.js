const isDocker = window.location.hostname !== 'localhost' && window.location.hostname !== '127.0.0.1';

const API_BASE_URL = isDocker ? 'http://backend:8080' : 'http://localhost:8080';

const API = {
    // Auth
    getCsrfUrl: () => `${API_BASE_URL}/api/auth/csrf`,
    getLoginUrl: () => `${API_BASE_URL}/api/auth/login`,
    getRegisterUrl: () => `${API_BASE_URL}/api/auth/register`,
    getForgotPasswordUrl: () => `${API_BASE_URL}/api/auth/forgot-password`,
    getResetPasswordUrl: () => `${API_BASE_URL}/api/auth/reset-password`,
    getLogoutUrl: () => `${API_BASE_URL}/api/auth/logout`,
    getMeUrl: () => `${API_BASE_URL}/api/auth/me`,
    
    // Users
    getMyProfileUrl: () => `${API_BASE_URL}/api/users/me`,
    getUpdateProfileUrl: () => `${API_BASE_URL}/api/users/me`,
    getUserProfileUrl: (userId) => `${API_BASE_URL}/api/users/${userId}`,
    
    // Advertisements
    getAdvertisementsUrl: () => `${API_BASE_URL}/api/advertisements`,
    getAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}`,
    getMyAdvertisementsUrl: () => `${API_BASE_URL}/api/advertisements/my`,
    createAdvertisementUrl: () => `${API_BASE_URL}/api/advertisements`,
    updateAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}`,
    archiveAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}/archive`,
    deleteAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}`,
    
    // Favorites
    getFavoritesUrl: () => `${API_BASE_URL}/api/favorites`,
    toggleFavoriteUrl: (id) => `${API_BASE_URL}/api/favorites/${id}`,
    checkFavoriteUrl: (id) => `${API_BASE_URL}/api/favorites/${id}/exists`,
    
    // Cart
    getCartUrl: () => `${API_BASE_URL}/api/cart`,
    addToCartUrl: (id) => `${API_BASE_URL}/api/cart/items/${id}`,
    removeFromCartUrl: (id) => `${API_BASE_URL}/api/cart/items/${id}`,
    clearCartUrl: () => `${API_BASE_URL}/api/cart`,
    
    // Orders
    createOrderFromCartUrl: () => `${API_BASE_URL}/api/orders/from-cart`,
    createSingleOrderUrl: (id) => `${API_BASE_URL}/api/orders/single/${id}`,
    getBuyerOrdersUrl: () => `${API_BASE_URL}/api/orders/buyer`,
    getSellerOrdersUrl: () => `${API_BASE_URL}/api/orders/seller`,
    getOrderUrl: (id) => `${API_BASE_URL}/api/orders/${id}`,
    completeOrderUrl: (id) => `${API_BASE_URL}/api/orders/${id}/complete`,
    cancelOrderUrl: (id) => `${API_BASE_URL}/api/orders/${id}/cancel`,
    
    // Reviews
    getUserReviewsUrl: (userId) => `${API_BASE_URL}/api/reviews/users/${userId}`,
    canLeaveReviewUrl: (orderId) => `${API_BASE_URL}/api/reviews/can-leave?orderId=${orderId}`,
    createReviewUrl: () => `${API_BASE_URL}/api/reviews`,
    
    // Chats
    getChatsUrl: () => `${API_BASE_URL}/api/chats`,
    getChatUrl: (id) => `${API_BASE_URL}/api/chats/${id}`,
    createOrGetChatUrl: (advertisementId) => `${API_BASE_URL}/api/chats/or-create/${advertisementId}`,
    sendMessageUrl: () => `${API_BASE_URL}/api/chats/messages`,
    markChatReadUrl: (chatId) => `${API_BASE_URL}/api/chats/${chatId}/read`,
    
    // Notifications
    getNotificationsUrl: () => `${API_BASE_URL}/api/notifications`,
    getUnreadCountUrl: () => `${API_BASE_URL}/api/notifications/unread-count`,
    markNotificationReadUrl: (id) => `${API_BASE_URL}/api/notifications/${id}/read`,
    markAllReadUrl: () => `${API_BASE_URL}/api/notifications/read-all`,
    
    // Admin
    getAdminUsersUrl: () => `${API_BASE_URL}/api/admin/users`,
    getAdminStatsUrl: () => `${API_BASE_URL}/api/admin/stats`,
    blockUserUrl: (userId) => `${API_BASE_URL}/api/admin/users/${userId}/block`,
    unblockUserUrl: (userId) => `${API_BASE_URL}/api/admin/users/${userId}/unblock`,
    exportCsvUrl: () => `${API_BASE_URL}/api/admin/export/csv`,
    exportJsonUrl: () => `${API_BASE_URL}/api/admin/export/json`,
};

// CSRF токен (глобальный)
let csrfToken = null;

// Получение CSRF токена
async function fetchCsrfToken() {
    try {
        const response = await fetch(API.getCsrfUrl(), {
            method: 'GET',
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            csrfToken = data.token;
            return true;
        }
        return false;
    } catch (error) {
        console.error('Ошибка получения CSRF токена:', error);
        return false;
    }
}

// Получение заголовков с CSRF токеном
function getSecureHeaders(forWrite = true) {
    const headers = {
        'Content-Type': 'application/json'
    };
    
    if (forWrite && csrfToken) {
        headers['X-XSRF-TOKEN'] = csrfToken;
    }
    
    return headers;
}

// Проверка авторизации
async function checkAuth() {
    try {
        const response = await fetch(API.getMeUrl(), {
            method: 'GET',
            headers: getSecureHeaders(false),
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            if (data.isSuccess && data.user) {
                localStorage.setItem('user', JSON.stringify(data.user));
                return data.user;
            }
        }
        return null;
    } catch (error) {
        console.error('Ошибка проверки авторизации:', error);
        return null;
    }
}

// Выход
async function logout() {
    if (!csrfToken) {
        await fetchCsrfToken();
    }
    
    try {
        await fetch(API.getLogoutUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
    } catch (error) {
        console.error('Ошибка выхода:', error);
    }
    
    localStorage.removeItem('user');
    window.location.href = 'index.html';
}

// Утилиты
function showError(message) {
    const errorDiv = document.getElementById('errorMessage');
    if (errorDiv) {
        errorDiv.textContent = '❌ ' + message;
        errorDiv.style.display = 'block';
        setTimeout(() => {
            errorDiv.style.display = 'none';
        }, 5000);
    } else {
        alert(message);
    }
}

function showSuccess(message) {
    const successDiv = document.getElementById('successMessage');
    if (successDiv) {
        successDiv.textContent = '✅ ' + message;
        successDiv.style.display = 'block';
        setTimeout(() => {
            successDiv.style.display = 'none';
        }, 3000);
    } else {
        alert(message);
    }
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}