const configuredApiBaseUrl =
    window.__APP_CONFIG__?.apiBaseUrl ||
    document.querySelector('meta[name="api-base-url"]')?.content ||
    '';

const API_BASE_URL = configuredApiBaseUrl.endsWith('/')
    ? configuredApiBaseUrl.slice(0, -1)
    : configuredApiBaseUrl;

const API = {
    getConfigUrl: () => `${API_BASE_URL}/api/config`,

    // Auth
    getCsrfUrl: () => `${API_BASE_URL}/api/auth/csrf`,
    getLoginUrl: () => `${API_BASE_URL}/api/auth/login`,
    getRegisterUrl: () => `${API_BASE_URL}/api/auth/register`,
    getForgotPasswordUrl: () => `${API_BASE_URL}/api/auth/forgot-password`,
    getResetPasswordUrl: () => `${API_BASE_URL}/api/auth/reset-password`,
    getConfirmEmailUrl: (token) => `${API_BASE_URL}/api/auth/confirm-email?token=${encodeURIComponent(token)}`,
    getResendConfirmationUrl: () => `${API_BASE_URL}/api/auth/resend-confirmation`,
    getLogoutUrl: () => `${API_BASE_URL}/api/auth/logout`,
    getMeUrl: () => `${API_BASE_URL}/api/auth/me`,

    // Users
    getMyProfileUrl: () => `${API_BASE_URL}/api/users/me`,
    getUpdateProfileUrl: () => `${API_BASE_URL}/api/users/me`,
    getUserProfileUrl: (userId) => `${API_BASE_URL}/api/users/${userId}`,
    getUpdateAvatarUrl: () => `${API_BASE_URL}/api/users/me/avatar`,
    getDeleteAvatarUrl: () => `${API_BASE_URL}/api/users/me/avatar`,

    // Advertisements
    getAdvertisementsUrl: () => `${API_BASE_URL}/api/advertisements`,
    getAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}`,
    getAdvertisementImageUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}/image`,
    getMyAdvertisementsUrl: (status) => {
        const baseUrl = `${API_BASE_URL}/api/advertisements/my`;
        return status ? `${baseUrl}?status=${encodeURIComponent(status)}` : baseUrl;
    },
    createAdvertisementUrl: () => `${API_BASE_URL}/api/advertisements`,
    updateAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}`,
    archiveAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}/archive`,
    restoreAdvertisementUrl: (id) => `${API_BASE_URL}/api/advertisements/${id}/restore`,
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
    createOrGetChatUrl: (advertisementId, participantUserId) => {
        const baseUrl = `${API_BASE_URL}/api/chats/or-create/${advertisementId}`;
        return participantUserId ? `${baseUrl}?participantUserId=${encodeURIComponent(participantUserId)}` : baseUrl;
    },
    sendMessageUrl: () => `${API_BASE_URL}/api/chats/messages`,
    markChatReadUrl: (chatId) => `${API_BASE_URL}/api/chats/${chatId}/read`,

    // Notifications
    getNotificationsUrl: () => `${API_BASE_URL}/api/notifications`,
    getUnreadCountUrl: () => `${API_BASE_URL}/api/notifications/unread-count`,
    markNotificationReadUrl: (id) => `${API_BASE_URL}/api/notifications/${id}/read`,
    markAllReadUrl: () => `${API_BASE_URL}/api/notifications/read-all`,

    // Moderation
    getModerationPendingUrl: () => `${API_BASE_URL}/api/moderation/pending`,
    approveModerationUrl: (id) => `${API_BASE_URL}/api/moderation/${id}/approve`,
    revisionModerationUrl: (id) => `${API_BASE_URL}/api/moderation/${id}/revision`,
    rejectModerationUrl: (id) => `${API_BASE_URL}/api/moderation/${id}/reject`,

    // Admin
    getAdminUsersUrl: () => `${API_BASE_URL}/api/admin/users`,
    getAdminStatsUrl: () => `${API_BASE_URL}/api/admin/stats`,
    blockUserUrl: (userId) => `${API_BASE_URL}/api/admin/users/${userId}/block`,
    unblockUserUrl: (userId) => `${API_BASE_URL}/api/admin/users/${userId}/unblock`,
    exportCsvUrl: () => `${API_BASE_URL}/api/admin/export/csv`,
    exportJsonUrl: () => `${API_BASE_URL}/api/admin/export/json`,
};

window.API_BASE_URL = API_BASE_URL;
window.API = API;

let csrfToken = null;

async function fetchCsrfToken() {
    try {
        const response = await fetch(API.getCsrfUrl(), {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            return false;
        }

        const data = await response.json();
        csrfToken = data.token || null;
        return Boolean(csrfToken);
    } catch (error) {
        console.error('CSRF token request failed:', error);
        return false;
    }
}

function getSecureHeaders(forWrite = true) {
    const headers = {
        'Content-Type': 'application/json'
    };

    if (forWrite && csrfToken) {
        headers['X-XSRF-TOKEN'] = csrfToken;
    }

    return headers;
}

function getSecureFormHeaders(forWrite = true) {
    const headers = {};

    if (forWrite && csrfToken) {
        headers['X-XSRF-TOKEN'] = csrfToken;
    }

    return headers;
}

async function ensureCsrfToken() {
    if (csrfToken) {
        return true;
    }

    return fetchCsrfToken();
}

async function parseApiError(response, fallbackMessage = 'Request failed') {
    try {
        const data = await response.json();
        if (Array.isArray(data?.errors) && data.errors.length > 0) {
            return data.errors.join(', ');
        }

        if (typeof data?.message === 'string' && data.message.trim()) {
            return data.message.trim();
        }
    } catch (error) {
        console.error('Failed to parse API error:', error);
    }

    return fallbackMessage;
}

async function requestJson(url, options = {}) {
    const {
        method = 'GET',
        body = null,
        useCsrf = method !== 'GET',
        isForm = false,
        fallbackMessage = 'Request failed'
    } = options;

    if (useCsrf) {
        await ensureCsrfToken();
    }

    const response = await fetch(url, {
        method,
        headers: isForm ? getSecureFormHeaders(useCsrf) : getSecureHeaders(useCsrf),
        body,
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, fallbackMessage));
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

async function requestNoContent(url, options = {}) {
    await requestJson(url, options);
}

async function checkAuth() {
    try {
        const response = await fetch(API.getMeUrl(), {
            method: 'GET',
            headers: getSecureHeaders(false),
            credentials: 'include'
        });

        if (!response.ok) {
            return null;
        }

        const data = await response.json();
        return data.isSuccess && data.user ? data.user : null;
    } catch (error) {
        console.error('Auth check failed:', error);
        return null;
    }
}

async function logout() {
    try {
        await ensureCsrfToken();
        await fetch(API.getLogoutUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
    } catch (error) {
        console.error('Logout failed:', error);
    }

    window.location.href = 'index.html';
}

function showError(message) {
    const errorDiv = document.getElementById('errorMessage');
    if (errorDiv) {
        errorDiv.textContent = `Ошибка: ${message}`;
        errorDiv.style.display = 'block';
        setTimeout(() => {
            errorDiv.style.display = 'none';
        }, 5000);
        return;
    }

    alert(message);
}

function showSuccess(message) {
    const successDiv = document.getElementById('successMessage');
    if (successDiv) {
        successDiv.textContent = message;
        successDiv.style.display = 'block';
        setTimeout(() => {
            successDiv.style.display = 'none';
        }, 3000);
        return;
    }

    alert(message);
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
