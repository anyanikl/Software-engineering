let cart = [];
let favorites = [];
let currentUser = null;

const fallbackImage =
    'data:image/svg+xml;charset=UTF-8,' +
    encodeURIComponent(`
        <svg xmlns="http://www.w3.org/2000/svg" width="240" height="160" viewBox="0 0 240 160">
            <rect width="240" height="160" fill="#f3f4f6"/>
            <text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle"
                  font-family="Arial, sans-serif" font-size="18" fill="#6b7280">Нет фото</text>
        </svg>
    `);

window.onload = initializeCartPage;

async function initializeCartPage() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    try {
        await Promise.all([loadCart(), loadFavorites()]);
    } catch (error) {
        showError(error.message || 'Не удалось загрузить корзину');
    }

    displayCart();
    updateCartBadge();
}

async function loadCart() {
    const data = await requestJson(API.getCartUrl(), {
        useCsrf: false,
        fallbackMessage: 'Не удалось загрузить корзину'
    });

    cart = Array.isArray(data?.items) ? data.items : [];
}

async function loadFavorites() {
    const data = await requestJson(API.getFavoritesUrl(), {
        useCsrf: false,
        fallbackMessage: 'Не удалось загрузить избранное'
    });

    favorites = Array.isArray(data) ? data : [];
}

async function removeFromCart(productId) {
    try {
        await requestNoContent(API.removeFromCartUrl(productId), {
            method: 'DELETE',
            fallbackMessage: 'Не удалось удалить товар из корзины'
        });

        await loadCart();
        displayCart();
        updateCartBadge();
        showSuccess('Товар удален из корзины');
    } catch (error) {
        showError(error.message || 'Не удалось удалить товар из корзины');
    }
}

async function toggleFavorite(productId) {
    const isFavorite = favorites.some(item => item.id === productId);

    try {
        await requestNoContent(API.toggleFavoriteUrl(productId), {
            method: isFavorite ? 'DELETE' : 'POST',
            fallbackMessage: isFavorite
                ? 'Не удалось удалить товар из избранного'
                : 'Не удалось добавить товар в избранное'
        });

        await loadFavorites();
        displayCart();
        showSuccess(isFavorite ? 'Товар удален из избранного' : 'Товар добавлен в избранное');
    } catch (error) {
        showError(error.message || 'Не удалось обновить избранное');
    }
}

async function clearCart() {
    if (!confirm('Очистить корзину?')) {
        return;
    }

    try {
        await requestNoContent(API.clearCartUrl(), {
            method: 'DELETE',
            fallbackMessage: 'Не удалось очистить корзину'
        });

        cart = [];
        displayCart();
        updateCartBadge();
        showSuccess('Корзина очищена');
    } catch (error) {
        showError(error.message || 'Не удалось очистить корзину');
    }
}

async function checkout() {
    if (cart.length === 0) {
        showError('Корзина пуста');
        return;
    }

    try {
        await requestJson(API.createOrderFromCartUrl(), {
            method: 'POST',
            fallbackMessage: 'Не удалось оформить заказ'
        });

        await loadCart();
        displayCart();
        updateCartBadge();
        showSuccess('Заказ оформлен');
    } catch (error) {
        showError(error.message || 'Не удалось оформить заказ');
    }
}

function displayCart() {
    const container = document.getElementById('cartContainer');
    if (!container) {
        return;
    }

    if (cart.length === 0) {
        container.innerHTML = `
            <div class="empty-cart">
                <p>В корзине пока пусто</p>
                <a href="shop.html">Перейти к товарам</a>
            </div>
        `;
        return;
    }

    const total = cart.reduce((sum, item) => sum + Number(item.price || 0), 0);
    const itemCount = cart.length;

    container.innerHTML = `
        <div class="cart-items">
            ${cart.map(renderCartItem).join('')}
        </div>
        <div class="cart-summary">
            <h3>Итого</h3>
            <div class="summary-row"><span>Товаров:</span><span>${itemCount} шт.</span></div>
            <div class="summary-row"><span>Сумма:</span><span class="summary-total">${total.toLocaleString('ru-RU')} ₽</span></div>
            <button class="checkout-btn" onclick="checkout()">Оформить заказ</button>
            <button class="clear-cart-btn" onclick="clearCart()">Очистить корзину</button>
        </div>
    `;
}

function renderCartItem(item) {
    const isFavorite = favorites.some(favorite => favorite.id === item.id);
    const imageUrl = normalizeAssetUrl(item.mainImageUrl);
    const title = escapeHtml(item.title || 'Без названия');
    const description = escapeHtml(item.shortDescription || 'Описание отсутствует');
    const sellerName = escapeHtml(item.sellerName || 'Продавец');
    const price = Number(item.price || 0).toLocaleString('ru-RU');

    return `
        <div class="cart-item">
            <img
                src="${escapeAttribute(imageUrl)}"
                alt="${escapeAttribute(item.title || 'Изображение товара')}"
                class="cart-item-image"
                onerror="this.src='${fallbackImage}'">
            <div class="cart-item-info">
                <div class="cart-item-name">${title}</div>
                <div class="cart-item-description">${description}</div>
                <div class="cart-item-price">${price} ₽</div>
                <div class="cart-item-seller">${sellerName}</div>
                <div class="cart-item-actions">
                    <button class="cart-item-btn remove-btn" onclick="removeFromCart(${item.id})">Удалить</button>
                    <button class="cart-item-btn favorite-btn ${isFavorite ? 'active' : ''}" onclick="toggleFavorite(${item.id})">
                        ${isFavorite ? 'Убрать из избранного' : 'В избранное'}
                    </button>
                </div>
            </div>
        </div>
    `;
}

function updateCartBadge() {
    const badge = document.getElementById('cartBadge');
    if (!badge) {
        return;
    }

    badge.style.display = cart.length > 0 ? 'inline-block' : 'none';
    badge.textContent = String(cart.length);
}

function normalizeAssetUrl(url) {
    if (!url) {
        return fallbackImage;
    }

    if (/^(https?:|data:|blob:)/i.test(url)) {
        return url;
    }

    return url.startsWith('/') ? url : `/${url}`;
}

function escapeAttribute(str) {
    return escapeHtml(str).replace(/`/g, '&#96;');
}

function showSuccess(message) {
    showToast(message, false);
}

function showError(message) {
    showToast(message, true);
}

function showToast(message, isError) {
    const toast = document.createElement('div');
    toast.className = 'notification-toast';
    toast.style.cssText = [
        'position: fixed',
        'bottom: 20px',
        'right: 20px',
        `background: ${isError ? '#e74c3c' : '#27ae60'}`,
        'color: white',
        'padding: 12px 20px',
        'border-radius: 8px',
        'z-index: 10000',
        'box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15)'
    ].join(';');
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}
