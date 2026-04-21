let resetToken = null;

document.addEventListener('DOMContentLoaded', function() {
    init();
});

function init() {
    resetToken = getTokenFromUrl();
    
    if (!resetToken) {
        showError('Отсутствует токен сброса пароля');
        disableForm();
        return;
    }
    
    setupEventListeners();
}

function getTokenFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get('token');
}

function setupEventListeners() {
    const passwordField = document.getElementById('newPassword');
    const confirmField = document.getElementById('confirmNewPassword');
    
    if (passwordField) {
        passwordField.addEventListener('input', function() {
            updateStrengthIndicator(this.value);
            checkPasswordsMatch();
        });
    }
    
    if (confirmField) {
        confirmField.addEventListener('input', checkPasswordsMatch);
    }
    
    document.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            submitResetPassword();
        }
    });
}

function checkPasswordStrength(password) {
    let strength = 0;
    const checks = {
        length: password.length >= 8,
        hasLetter: /[A-Za-zА-Яа-я]/.test(password),
        hasNumber: /[0-9]/.test(password)
    };
    
    if (checks.length) strength++;
    if (checks.hasLetter) strength++;
    if (checks.hasNumber) strength++;
    
    return { strength, checks };
}

function updateStrengthIndicator(password) {
    const { strength, checks } = checkPasswordStrength(password);
    const bars = document.querySelectorAll('.strength-bar');
    const strengthText = document.getElementById('strengthText');
    
    bars.forEach((bar, index) => {
        bar.classList.remove('weak', 'medium', 'strong');
        if (index < strength) {
            if (strength === 1) {
                bar.classList.add('weak');
                if (strengthText) strengthText.textContent = 'Слабый пароль';
            } else if (strength === 2) {
                bar.classList.add('medium');
                if (strengthText) strengthText.textContent = 'Средний пароль';
            } else if (strength === 3) {
                bar.classList.add('strong');
                if (strengthText) strengthText.textContent = 'Сильный пароль';
            }
        }
    });
    
    if (password.length === 0 && strengthText) {
        strengthText.textContent = 'Сложность пароля';
    }
    
    updateRequirement('req-length', checks.length);
    updateRequirement('req-letter', checks.hasLetter);
    updateRequirement('req-number', checks.hasNumber);
}

function updateRequirement(id, isValid) {
    const element = document.getElementById(id);
    if (element) {
        if (isValid) {
            element.classList.add('valid');
            const icon = element.querySelector('.req-icon');
            if (icon) icon.textContent = '✓';
        } else {
            element.classList.remove('valid');
            const icon = element.querySelector('.req-icon');
            if (icon) icon.textContent = '○';
        }
    }
}

function checkPasswordsMatch() {
    const password = document.getElementById('newPassword').value;
    const confirm = document.getElementById('confirmNewPassword').value;
    const hint = document.getElementById('matchHint');
    
    if (!hint) return;
    
    if (confirm.length > 0) {
        if (password === confirm) {
            hint.textContent = '✓ Пароли совпадают';
            hint.style.color = '#10b981';
        } else {
            hint.textContent = '✗ Пароли не совпадают';
            hint.style.color = '#ef4444';
        }
    } else {
        hint.textContent = '';
    }
}

function togglePassword(fieldId, button) {
    const field = document.getElementById(fieldId);
    if (!field) return;
    
    if (field.type === 'password') {
        field.type = 'text';
        button.textContent = '🙈';
    } else {
        field.type = 'password';
        button.textContent = '👁️';
    }
}

window.togglePassword = togglePassword;

async function submitResetPassword() {
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmNewPassword').value;
    
    if (!newPassword || !confirmPassword) {
        showError('Заполните все поля');
        return;
    }
    
    if (newPassword !== confirmPassword) {
        showError('Пароли не совпадают');
        return;
    }
    
    const { checks } = checkPasswordStrength(newPassword);
    if (!checks.length || !checks.hasLetter || !checks.hasNumber) {
        showError('Пароль должен содержать минимум 8 символов, буквы и цифры');
        return;
    }
    
    if (!resetToken) {
        showError('Недействительная ссылка для сброса пароля');
        return;
    }
    
    // Показываем индикатор загрузки
    const submitBtn = document.querySelector('.btn-primary');
    const originalText = submitBtn.textContent;
    submitBtn.textContent = '⏳ Сохранение...';
    submitBtn.disabled = true;
    
    try {
        // Используем API из api-config.js
        const response = await fetch(API.getResetPasswordUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            body: JSON.stringify({
                token: resetToken,
                newPassword: newPassword,
                confirmPassword: confirmPassword
            }),
            credentials: 'include'
        });
        
        if (response.status === 204 || response.ok) {
            showSuccess('✅ Пароль успешно изменен!');
            setTimeout(() => {
                window.location.href = 'index.html';
            }, 2000);
        } else {
            const data = await response.json().catch(() => ({}));
            showError(data.message || data.errors?.join(', ') || 'Ошибка сброса пароля');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
    } finally {
        submitBtn.textContent = originalText;
        submitBtn.disabled = false;
    }
}

function disableForm() {
    const inputs = document.querySelectorAll('input');
    const buttons = document.querySelectorAll('.btn');
    
    inputs.forEach(input => {
        input.disabled = true;
    });
    
    buttons.forEach(button => {
        button.disabled = true;
    });
}

window.submitResetPassword = submitResetPassword;