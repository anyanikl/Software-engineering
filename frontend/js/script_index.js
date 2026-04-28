let config = {
    universityDomains: ['.edu', '.ac.ru'],
    universities: [],
    faculties: [],
    passwordMinLength: 8
};

document.addEventListener('DOMContentLoaded', async () => {
    await fetchCsrfToken();
    await loadConfig();
});

async function loadConfig() {
    try {
        const data = await requestJson(API.getConfigUrl(), {
            method: 'GET',
            useCsrf: false,
            fallbackMessage: 'Не удалось загрузить конфигурацию'
        });

        config = {
            universityDomains: Array.isArray(data.universityDomains) ? data.universityDomains : config.universityDomains,
            universities: Array.isArray(data.universities) ? data.universities : [],
            faculties: Array.isArray(data.faculties) ? data.faculties : [],
            passwordMinLength: Number(data.passwordMinLength) || 8
        };
    } catch (error) {
        console.error('Config load failed:', error);
    }

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
    const normalizedEmail = String(email || '').toLowerCase();
    return config.universityDomains.some(domain => normalizedEmail.endsWith(domain.toLowerCase()));
}

function isValidPassword(password) {
    const minLength = config.passwordMinLength || 8;
    return password.length >= minLength && /[A-Za-zА-Яа-я]/.test(password) && /[0-9]/.test(password);
}

function switchTab(tab) {
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach(item => item.classList.remove('active'));

    if (tab === 'login') {
        tabs[0]?.classList.add('active');
        document.getElementById('loginForm').style.display = 'block';
        document.getElementById('registerForm').style.display = 'none';
        document.getElementById('forgotPasswordForm').style.display = 'none';
    } else {
        tabs[1]?.classList.add('active');
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
    document.querySelectorAll('.tab').forEach(item => item.classList.remove('active'));
    hideMessages();
}

function showLoginForm() {
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('registerForm').style.display = 'none';
    document.getElementById('forgotPasswordForm').style.display = 'none';
    document.querySelectorAll('.tab')[0]?.classList.add('active');
    document.querySelectorAll('.tab')[1]?.classList.remove('active');
    hideMessages();
}

function hideMessages() {
    const errorDiv = document.getElementById('errorMessage');
    const successDiv = document.getElementById('successMessage');
    if (errorDiv) errorDiv.style.display = 'none';
    if (successDiv) successDiv.style.display = 'none';
}

async function login() {
    const email = document.getElementById('loginEmail').value.trim();
    const password = document.getElementById('loginPassword').value;

    if (!email || !password) {
        showError('Заполните email и пароль');
        return;
    }

    try {
        const data = await requestJson(API.getLoginUrl(), {
            method: 'POST',
            body: JSON.stringify({ email, password }),
            fallbackMessage: 'Ошибка входа'
        });

        if (!data?.isSuccess || !data.user) {
            throw new Error((data?.errors || ['Ошибка входа']).join(', '));
        }

        showSuccess(`Добро пожаловать, ${data.user.fullName || data.user.email}!`);
        setTimeout(() => {
            window.location.href = 'shop.html';
        }, 800);
    } catch (error) {
        showError(error.message || 'Ошибка входа');
    }
}

async function register() {
    const email = document.getElementById('regEmail').value.trim();
    const fullName = document.getElementById('regFullName').value.trim();
    const phone = document.getElementById('regPhone').value.trim();
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

    try {
        const data = await requestJson(API.getRegisterUrl(), {
            method: 'POST',
            body: JSON.stringify({
                email,
                fullName,
                phone,
                university,
                faculty,
                password,
                confirmPassword
            }),
            fallbackMessage: 'Ошибка регистрации'
        });

        showSuccess(data?.message || 'Регистрация завершена. Подтвердите email перед входом.');
        clearRegistrationForm();
        setTimeout(showLoginForm, 2500);
    } catch (error) {
        showError(error.message || 'Ошибка регистрации');
    }
}

function clearRegistrationForm() {
    document.getElementById('regEmail').value = '';
    document.getElementById('regFullName').value = '';
    document.getElementById('regPhone').value = '';
    document.getElementById('regUniversity').value = '';
    document.getElementById('regFaculty').value = '';
    document.getElementById('regPassword').value = '';
    document.getElementById('regConfirmPassword').value = '';
}

async function sendRecovery() {
    const email = document.getElementById('recoveryEmail').value.trim();

    if (!email) {
        showError('Введите email');
        return;
    }

    if (!isValidUniversityEmail(email)) {
        showError('Используйте университетскую почту');
        return;
    }

    try {
        await requestNoContent(API.getForgotPasswordUrl(), {
            method: 'POST',
            body: JSON.stringify({ email }),
            fallbackMessage: 'Ошибка отправки письма для восстановления'
        });

        showSuccess('Если аккаунт найден, инструкция для восстановления отправлена на email');
        document.getElementById('recoveryEmail').value = '';
        setTimeout(showLoginForm, 2500);
    } catch (error) {
        showError(error.message || 'Ошибка отправки письма');
    }
}
