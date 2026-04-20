const API_CONFIG = {
    baseUrl: 'http://localhost:8080',
    
    endpoints: {
        csrf: '/api/auth/csrf',
        login: '/api/auth/login',
        register: '/api/auth/register',
        forgotPassword: '/api/auth/forgot-password',
        resetPassword: '/api/auth/reset-password',
        logout: '/api/auth/logout',
        me: '/api/auth/me'
    },
    
    headers: {
        'Content-Type': 'application/json'
    }
};

const API = {
    getBaseUrl: () => API_CONFIG.baseUrl,
    
    getCsrfUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.csrf}`,
    getLoginUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.login}`,
    getRegisterUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.register}`,
    getForgotPasswordUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.forgotPassword}`,
    getResetPasswordUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.resetPassword}`,
    getLogoutUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.logout}`,
    getMeUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.me}`,
    
    getHeaders: () => API_CONFIG.headers
};