const API_CONFIG = {
    baseUrl: 'https://localhost:5001',  // или http://localhost:5000
    
    endpoints: {
        csrf: '/api/auth/csrf',
        login: '/api/auth/login',
        logout: '/api/auth/logout',
        me: '/api/auth/me',
        register: '/api/users/register'  
    },
    
    headers: {
        'Content-Type': 'application/json'
    }
};

const API = {
    getBaseUrl: () => API_CONFIG.baseUrl,
    
    getCsrfUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.csrf}`,
    getLoginUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.login}`,
    getLogoutUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.logout}`,
    getMeUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.me}`,
    getRegisterUrl: () => `${API_CONFIG.baseUrl}${API_CONFIG.endpoints.register}`,
    
    getHeaders: () => API_CONFIG.headers
};