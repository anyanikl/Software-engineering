let cart = [];
let favorites = [];
let currentUser = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }
    
    await loadCart();
    await loadFavorites();
    displayCart();
};

async function loadCart() {
    try {
        const response = await fetch(API.getCartUrl(), { credentials: 'include' });
        if (response.ok) {
            const data = await response.json();
            // CartDto: { items: [], totalCount, totalPrice }
            cart = data.items || [];
            localStorage.setItem('cart', JSON.stringify(cart));
        } else {
            loadCartFromLocal();
        }
    } catch (error) {
        console.error('Ошибка загрузки корзины из API:', error);
        loadCartFromLocal();
    }
    updateCartBadge();
}

function loadCartFromLocal() {
    cart = JSON.parse(localStorage.getItem('cart')) || [];
}

async function loadFavorites() {
    try {
        const response = await fetch(API.getFavoritesUrl(), { credentials: 'include' });
        if (response.ok) {
            favorites = await response.json();
            localStorage.setItem('favorites', JSON.stringify(favorites));
        } else {
            loadFavoritesFromLocal();
        }
    } catch (error) {
        console.error('Ошибка загрузки избранного из API:', error);
        loadFavoritesFromLocal();
    }
}

function loadFavoritesFromLocal() {
    favorites = JSON.parse(localStorage.getItem('favorites')) || [];
}

async function removeFromCart(productId) {
    try {
        const response = await fetch(API.removeFromCartUrl(productId), {
            method: 'DELETE',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        if (response.ok) {
            await loadCart();
            displayCart();
            showSuccess('Товар удален из корзины');
            return;
        }
    } catch (error) {
        console.error('Ошибка API:', error);
    }
    
    // Fallback на localStorage
    cart = cart.filter(item => item.id !== productId);
    localStorage.setItem('cart', JSON.stringify(cart));
    displayCart();
    updateCartBadge();
    showSuccess('Товар удален из корзины');
}

async function toggleFavorite(productId) {
    const product = cart.find(item => item.id === productId);
    if (!product) return;
    
    const isFavorite = favorites.some(f => f.id === productId);
    
    try {
        const response = await fetch(API.toggleFavoriteUrl(productId), {
            method: isFavorite ? 'DELETE' : 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        if (response.ok) {
            if (isFavorite) {
                favorites = favorites.filter(f => f.id !== productId);
            } else {
                favorites.push(product);
            }
            localStorage.setItem('favorites', JSON.stringify(favorites));
            displayCart();
            showSuccess(isFavorite ? 'Удалено из избранного' : 'Добавлено в избранное');
            return;
        }
    } catch (error) {
        console.error('Ошибка API:', error);
    }
    
    // Fallback на localStorage
    if (isFavorite) {
        favorites = favorites.filter(f => f.id !== productId);
    } else {
        favorites.push(product);
    }
    localStorage.setItem('favorites', JSON.stringify(favorites));
    displayCart();
    showSuccess(isFavorite ? 'Удалено из избранного' : 'Добавлено в избранное');
}

async function clearCart() {
    if (!confirm('Вы уверены, что хотите очистить корзину?')) return;
    
    try {
        const response = await fetch(API.clearCartUrl(), {
            method: 'DELETE',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        if (response.ok) {
            await loadCart();
            displayCart();
            showSuccess('Корзина очищена');
            return;
        }
    } catch (error) {
        console.error('Ошибка API:', error);
    }
    
    // Fallback на localStorage
    cart = [];
    localStorage.setItem('cart', JSON.stringify(cart));
    displayCart();
    updateCartBadge();
    showSuccess('Корзина очищена');
}

async function checkout() {
    if (cart.length === 0) {
        showError('Корзина пуста');
        return;
    }
    
    try {
        const response = await fetch(API.createOrderFromCartUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('✅ Заказ оформлен! Продавец получил уведомление.');
            await loadCart();
            displayCart();
        } else {
            const error = await response.json().catch(() => ({}));
            showError(error.message || 'Ошибка оформления заказа');
        }
    } catch (error) {
        console.error('Ошибка:', error);
        showError('Ошибка соединения с сервером');
    }
}

function displayCart() {
    const container = document.getElementById('cartContainer');
    if (!container) return;
    
    if (cart.length === 0) {
        container.innerHTML = `<div class="empty-cart"><p>😕 В корзине пока пусто</p><a href="shop.html">Перейти к товарам</a></div>`;
        return;
    }
    
    // Используем поля из AdvertisementCardDto
    const total = cart.reduce((sum, item) => sum + (item.price * (item.quantity || 1)), 0);
    const itemCount = cart.reduce((sum, item) => sum + (item.quantity || 1), 0);
    
    container.innerHTML = `
        <div class="cart-items">
            ${cart.map(item => {
                const isFavorite = favorites.some(f => f.id === item.id);
                const quantity = item.quantity || 1;
                const itemTotal = item.price * quantity;
                const title = item.title;
                const description = item.shortDescription || '';
                const imageUrl = item.mainImageUrl || 'https://via.placeholder.com/100';
                const price = item.price;
                
                return `
                    <div class="cart-item">
                        <img src="${imageUrl}" 
                             alt="${escapeHtml(title)}" 
                             class="cart-item-image"
                             onerror="this.src='https://via.placeholder.com/100'">
                        <div class="cart-item-info">
                            <div class="cart-item-name">${escapeHtml(title)}</div>
                            <div class="cart-item-description">${escapeHtml(description)}</div>
                            <div class="cart-item-price">${price.toLocaleString()} ₽ × ${quantity} = ${itemTotal.toLocaleString()} ₽</div>
                            <div class="cart-item-actions">
                                <button class="cart-item-btn remove-btn" onclick="removeFromCart(${item.id})">Удалить</button>
                                <button class="cart-item-btn favorite-btn ${isFavorite ? 'active' : ''}" onclick="toggleFavorite(${item.id})">${isFavorite ? '❤️' : '🤍'}</button>
                            </div>
                        </div>
                    </div>
                `;
            }).join('')}
        </div>
        <div class="cart-summary">
            <h3>Итого</h3>
            <div class="summary-row"><span>Товаров:</span><span>${itemCount} шт.</span></div>
            <div class="summary-row"><span>Сумма:</span><span class="summary-total">${total.toLocaleString()} ₽</span></div>
            <button class="checkout-btn" onclick="checkout()">✅ Оформить заказ</button>
            <button class="clear-cart-btn" onclick="clearCart()">Очистить корзину</button>
        </div>
    `;
}

function updateCartBadge() {
    const badge = document.getElementById('cartBadge');
    if (badge) {
        const count = cart.reduce((sum, item) => sum + (item.quantity || 1), 0);
        badge.style.display = count > 0 ? 'inline-block' : 'none';
        if (count > 0) badge.textContent = count;
    }
}

function showSuccess(message) {
    const toast = document.createElement('div');
    toast.className = 'notification-toast';
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#27ae60;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = '✅ ' + message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

function showError(message) {
    const toast = document.createElement('div');
    toast.className = 'notification-toast';
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#e74c3c;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = '❌ ' + message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

function escapeHtml(str) {
    if (!str) return '';
    return String(str).replace(/[&<>]/g, function(m) {
        if (m === '&') return '&amp;';
        if (m === '<') return '&lt;';
        if (m === '>') return '&gt;';
        return m;
    });
}