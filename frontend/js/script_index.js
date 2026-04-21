let config = {
    universityDomains: ['.edu', '.ac.ru', 'university.ru', 'kpfu.ru'],
    universities: [],
    faculties: [],
    passwordMinLength: 8
};

async function loadConfig() {
    try {
        const response = await fetch('/api/config');
        
        if (response.ok) {
            const data = await response.json();
            config = { ...config, ...data };
            console.log('Конфигурация загружена:', config);
            
            populateSelect('regUniversity', config.universities, 'Выберите университет');
            populateSelect('regFaculty', config.faculties, 'Выберите направление');
        } else {
            setDefaultConfig();
        }
    } catch (error) {
        console.error('Ошибка загрузки конфигурации:', error);
        setDefaultConfig();
    }
}

function setDefaultConfig() {
    config.universities = ['КФУ', 'КИУ', 'КАИ', 'КГЭУ', 'РАНХИХИГС'];
    config.faculties = ['Информационные технологии', 'Экономика', 'Право', 'Медицина', 'Иностранные языки'];
    populateSelect('regUniversity', config.universities, 'Выберите университет');
    populateSelect('regFaculty', config.faculties, 'Выберите направление');
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
    const errorDiv = document.getElementById('errorMessage');
    const successDiv = document.getElementById('successMessage');
    if (errorDiv) errorDiv.style.display = 'none';
    if (successDiv) successDiv.style.display = 'none';
}

async function login() {
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;
    
    if (!email || !password) {
        showError('Заполните все поля');
        return;
    }
    
    if (!csrfToken) {
        await fetchCsrfToken();
    }
    
    try {
        const response = await fetch(API.getLoginUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include',
            body: JSON.stringify({ email, password })
        });
        
        const data = await response.json();
        
        if (response.ok && data.isSuccess) {
            showSuccess(`Добро пожаловать, ${data.user.fullName || data.user.email}!`);
            localStorage.setItem('user', JSON.stringify(data.user));
            
            setTimeout(() => {
                window.location.href = 'shop.html';
            }, 1000);
        } else if (response.status === 401) {
            showError(data.errors?.join(', ') || 'Неверный email или пароль');
        } else {
            showError(data.errors?.join(', ') || data.message || 'Ошибка входа');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
    }
}

async function register() {
    const email = document.getElementById('regEmail').value;
    const fullName = document.getElementById('regFullName').value;
    const phone = document.getElementById('regPhone').value;
    const university = document.getElementById('regUniversity').value;
    const faculty = document.getElementById('regFaculty').value;
    const password = document.getElementById('regPassword').value;
    const confirmPassword = document.getElementById('regConfirmPassword').value;
    
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
    
    if (!csrfToken) {
        await fetchCsrfToken();
    }
    
    try {
        const response = await fetch(API.getRegisterUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include',
            body: JSON.stringify({
                email, fullName, phone, university, faculty,
                password, confirmPassword
            })
        });
        
        const data = await response.json();
        
        if (response.ok && data.isSuccess) {
            showSuccess('Регистрация прошла успешно! Теперь вы можете войти.');
            
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
            showError(data.errors?.join(', ') || data.message || 'Ошибка регистрации');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
    }
}

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
    
    if (!csrfToken) {
        await fetchCsrfToken();
    }
    
    try {
        const response = await fetch(API.getForgotPasswordUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include',
            body: JSON.stringify({ email })
        });
        
        if (response.status === 204) {
            showSuccess('Инструкция по восстановлению отправлена на ваш email');
            document.getElementById('recoveryEmail').value = '';
            
            setTimeout(() => {
                showLoginForm();
            }, 3000);
        } else {
            const data = await response.json().catch(() => ({}));
            showError(data.message || 'Ошибка. Попробуйте позже.');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
    }
}

async function resetPassword(token, newPassword, confirmPassword) {
    if (!newPassword || !confirmPassword) {
        showError('Заполните все поля');
        return false;
    }
    
    if (newPassword !== confirmPassword) {
        showError('Пароли не совпадают');
        return false;
    }
    
    if (!isValidPassword(newPassword)) {
        showError(`Пароль должен быть минимум ${config.passwordMinLength} символов, содержать буквы и цифры`);
        return false;
    }
    
    if (!csrfToken) {
        await fetchCsrfToken();
    }
    
    try {
        const response = await fetch(API.getResetPasswordUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include',
            body: JSON.stringify({ token, newPassword, confirmPassword })
        });
        
        if (response.status === 204) {
            showSuccess('Пароль успешно изменен! Теперь вы можете войти.');
            return true;
        } else {
            const data = await response.json().catch(() => ({}));
            showError(data.message || 'Ошибка сброса пароля');
            return false;
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
        return false;
    }
}

async function getCurrentUser() {
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
        console.error('Ошибка получения пользователя:', error);
        return null;
    }
}

loadConfig();
fetchCsrfToken();