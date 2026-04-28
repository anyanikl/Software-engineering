document.addEventListener('DOMContentLoaded', async () => {
    const messageElement = document.getElementById('confirmMessage');
    const token = new URLSearchParams(window.location.search).get('token');

    if (!token) {
        messageElement.textContent = 'Ссылка подтверждения недействительна: токен не найден.';
        return;
    }

    try {
        await fetchCsrfToken();
        const response = await fetch(API.getConfirmEmailUrl(token), {
            method: 'GET',
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(await parseApiError(response, 'Не удалось подтвердить email'));
        }

        messageElement.textContent = 'Email успешно подтвержден. Теперь можно войти в аккаунт.';
    } catch (error) {
        messageElement.textContent = error.message || 'Не удалось подтвердить email.';
    }
});
