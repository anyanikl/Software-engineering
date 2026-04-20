(function (global) {
    const configuredBaseUrl =
        global.__APP_CONFIG__?.apiBaseUrl ||
        document.querySelector('meta[name="api-base-url"]')?.content ||
        '';

    const baseUrl = configuredBaseUrl.endsWith('/')
        ? configuredBaseUrl.slice(0, -1)
        : configuredBaseUrl;

    const API_CONFIG = {
        baseUrl,

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

    const buildUrl = (path) => `${API_CONFIG.baseUrl}${path}`;

    global.API_CONFIG = API_CONFIG;
    global.API = {
        getBaseUrl: () => API_CONFIG.baseUrl,

        getCsrfUrl: () => buildUrl(API_CONFIG.endpoints.csrf),
        getLoginUrl: () => buildUrl(API_CONFIG.endpoints.login),
        getRegisterUrl: () => buildUrl(API_CONFIG.endpoints.register),
        getForgotPasswordUrl: () => buildUrl(API_CONFIG.endpoints.forgotPassword),
        getResetPasswordUrl: () => buildUrl(API_CONFIG.endpoints.resetPassword),
        getLogoutUrl: () => buildUrl(API_CONFIG.endpoints.logout),
        getMeUrl: () => buildUrl(API_CONFIG.endpoints.me),

        getHeaders: () => API_CONFIG.headers
    };
})(window);
