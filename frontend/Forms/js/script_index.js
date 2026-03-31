// '.edu', '.ac.ru', 'university.ru', 'kpfu.ru' 'КФУ', 'КИУ', 'КАИ', 'КГЭУ', 'РАНХИХИГС'
let config = {
    universityDomains: ['.edu', '.ac.ru', 'university.ru', 'kpfu.ru'],
    universities: [],
    faculties: [],
    passwordMinLength: 8
};

let csrfToken = null;  // Для защиты от CSRF атак

// Загрузка конфигурации из JSON
async function loadConfig() {
    try {
        const response = await fetch('js/config_index.json');
        if (response.ok) {
            const data = await response.json();
            config = { ...config, ...data };
            console.log('Конфигурация загружена:', config);
            
            // Заполняем выпадающие списки
            populateSelect('regUniversity', config.universities, 'Выберите университет');
            populateSelect('regFaculty', config.faculties, 'Выберите факультет');
        } else {
            console.warn('Не удалось загрузить config_index.json, используем значения по умолчанию');
            // Значения по умолчанию, если файл не найден
            config.universities = ['КФУ', 'КИУ', 'КАИ', 'КГЭУ', 'РАНХИХИГС'];
            config.faculties = ['Факультет информационных технологий', 'Факультет экономики', 'Факультет права', 'Факультет медицины', 'Факультет иностранных языков'];
            populateSelect('regUniversity', config.universities, 'Выберите университет');
            populateSelect('regFaculty', config.faculties, 'Выберите факультет');
        }
    } catch (error) {
        console.error('Ошибка загрузки конфигурации:', error);
        // Значения по умолчанию при ошибке
        config.universities = ['КФУ', 'КИУ', 'КАИ', 'КГЭУ', 'РАНХИХИГС'];
        config.faculties = ['Факультет информационных технологий', 'Факультет экономики', 'Факультет права', 'Факультет медицины', 'Факультет иностранных языков'];
        populateSelect('regUniversity', config.universities, 'Выберите университет');
        populateSelect('regFaculty', config.faculties, 'Выберите факультет');
    }
}

function populateSelect(selectId, items, defaultText) {
    const select = document.getElementById(selectId);
    if (!select) return;
    
    select.innerHTML = `<option value="">${defaultText}</option>`;
    items.forEach(item => {
        const option = document.createElement('option');
        option.value = item;
        option.textContent = item;
        select.appendChild(option);
    });
}

function isValidUniversityEmail(email) {
    const domains = config.universityDomains;
    return domains.some(domain => email.toLowerCase().endsWith(domain));
}

function isValidPassword(password) {
    const minLength = config.passwordMinLength || 8;
    return password.length >= minLength && /[A-Za-z]/.test(password) && /[0-9]/.test(password);
}

// Работа с CSRF токеном
async function fetchCsrfToken() {
    try {
        const response = await fetch(API.getCsrfUrl(), {
            method: 'GET',
            credentials: 'include',
            headers: API.getHeaders()
        });
        
        if (response.ok) {
            const data = await response.json();
            csrfToken = data.token;
            console.log('CSRF токен получен');
            return true;
        }
        return false;
    } catch (error) {
        console.error('Ошибка получения CSRF токена:', error);
        return false;
    }
}

function getSecureHeaders() {
    const headers = API.getHeaders();
    if (csrfToken) {
        headers['X-XSRF-TOKEN'] = csrfToken;
    }
    return headers;
}

// UI функции (переключение вкладок, сообщения)
function switchTab(tab) {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach(t => t.classList.remove('active'));
    
    if (tab === 'login') {
        tabs[0].classList.add('active');
        document.getElementById('loginForm').style.display = 'block';
        document.getElementById('registerForm').style.display = 'none';
        document.getElementById('forgotPasswordForm').style.display = 'none';
    } else {
        tabs[1].classList.add('active');
        document.getElementById('loginForm').style.display = 'none';
        document.getElementById('registerForm').style.display = 'block';
        document.getElementById('forgotPasswordForm').style.display = 'none';
    }
    
    hideMessages();
}

function showForgotPassword() {
    document.getElementById('loginForm').style.display = 'none';
    document.getElementById('registerForm').style.display = 'none';
    document.getElementById('forgotPasswordForm').style.display = 'block';
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    hideMessages();
}

function showLoginForm() {
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
    document.getElementById('forgotPasswordForm').style.display = 'none';
    document.querySelectorAll('.tab')[0].classList.add('active');
    document.querySelectorAll('.tab')[1].classList.remove('active');
    hideMessages();
}

function hideMessages() {
    document.getElementById('errorMessage').style.display = 'none';
    document.getElementById('successMessage').style.display = 'none';
}

function showError(message) {
    const errorDiv = document.getElementById('errorMessage');
    errorDiv.textContent = '❌ ' + message;
    errorDiv.style.display = 'block';
    setTimeout(() => {
        errorDiv.style.display = 'none';
    }, 5000);
}

function showSuccess(message) {
    const successDiv = document.getElementById('successMessage');
    successDiv.textContent = '✅ ' + message;
    successDiv.style.display = 'block';
    setTimeout(() => {
        successDiv.style.display = 'none';
    }, 5000);
}

// Функция входа (Login)
async function login() {
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;
    
    if (!email || !password) {
        showError('Заполните все поля');
        return;
    }
    
    // Получаем CSRF токен перед входом
    if (!csrfToken) {
        await fetchCsrfToken();
    }
    
    try {
        const response = await fetch(API.getLoginUrl(), {
            method: 'POST',
            headers: getSecureHeaders(),
            credentials: 'include',
            body: JSON.stringify({
                email: email,
                password: password
            })
        });
        
        const data = await response.json();
        
        if (response.ok && data.isSuccess) {
            // Успешный вход
            showSuccess(`Добро пожаловать, ${data.user.fullName || data.user.email}!`);
            
            // Сохраняем данные пользователя
            localStorage.setItem('user', JSON.stringify(data.user));
            
            setTimeout(() => {
                window.location.href = 'shop.html';
            }, 1000);
        } else if (response.status === 401) {
            showError('Неверный email или пароль');
        } else {
            showError(data.errors?.join(', ') || 'Ошибка входа');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером. Убедитесь, что бэкенд запущен.');
    }
}

// Функция регистрации (Register)
async function register() {
    const email = document.getElementById('regEmail').value;
    const fullName = document.getElementById('regFullName').value;
    const phone = document.getElementById('regPhone').value;
    const university = document.getElementById('regUniversity').value;
    const faculty = document.getElementById('regFaculty').value;
    const password = document.getElementById('regPassword').value;
    const confirmPassword = document.getElementById('regConfirmPassword').value;
    
    // Валидация
    if (!email || !fullName || !phone || !university || !faculty || !password || !confirmPassword) {
        showError('Заполните все поля');
        return;
    }
    
    if (!isValidUniversityEmail(email)) {
        showError('Используйте университетскую почту');
        return;
    }
    
    if (!isValidPassword(password)) {
        showError(`Пароль должен быть минимум ${config.passwordMinLength} символов, содержать буквы и цифры`);
        return;
    }
    
    if (password !== confirmPassword) {
        showError('Пароли не совпадают');
        return;
    }
    
    // пока нет API регистрации
    // Убираем заглушку, когда появится эндпоинт
    showError('Регистрация временно недоступна. Пожалуйста, обратитесь к администратору.');
    return;
    
    /* ============================================
       КОГДА ПОЯВИТСЯ API РЕГИСТРАЦИИ, РАСКОММЕНТИРОВАТЬ:
    ============================================
    try {
        if (!csrfToken) {
            await fetchCsrfToken();
        }
        
        const response = await fetch(API.getRegisterUrl(), {
            method: 'POST',
            headers: getSecureHeaders(),
            credentials: 'include',
            body: JSON.stringify({
                email: email,
                fullName: fullName,
                phoneNumber: phone,
                university: university,
                faculty: faculty,
                password: password
            })
        });
        
        const data = await response.json();
        
        if (response.ok && data.isSuccess) {
            showSuccess('Регистрация прошла успешно! Теперь вы можете войти.');
            
            // Очищаем форму
            document.getElementById('regEmail').value = '';
            document.getElementById('regFullName').value = '';
            document.getElementById('regPhone').value = '';
            document.getElementById('regUniversity').value = '';
            document.getElementById('regFaculty').value = '';
            document.getElementById('regPassword').value = '';
            document.getElementById('regConfirmPassword').value = '';
            
            setTimeout(() => {
                showLoginForm();
            }, 2000);
        } else {
            showError(data.errors?.join(', ') || 'Ошибка регистрации');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
    }
    */
}

// Функция восстановления пароля
async function sendRecovery() {
    const email = document.getElementById('recoveryEmail').value;
    
    if (!email) {
        showError('Введите email');
        return;
    }
    
    if (!isValidUniversityEmail(email)) {
        showError('Используйте университетскую почту');
        return;
    }
    
    // Добавить эндпоинт восстановления пароля, когда появится
    showError('Функция восстановления пароля временно недоступна');
    return;
}

// Обработка нажатия Enter
document.addEventListener('keypress', function(e) {
    if (e.key === 'Enter') {
        if (document.getElementById('loginForm').style.display !== 'none') {
            login();
        } else if (document.getElementById('registerForm').style.display !== 'none') {
            register();
        } else if (document.getElementById('forgotPasswordForm').style.display !== 'none') {
            sendRecovery();
        }
    }
});

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', function() {
    loadConfig();
    fetchCsrfToken();  // Получаем CSRF токен при загрузке страницы
});