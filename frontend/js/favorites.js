let favorites = [];
let cart = [];
let currentUser = null;
let currentModalProduct = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }
    
    await loadFavorites();
    await loadCart();
    displayFavorites();
    setupModalHandlers();
};

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

async function loadCart() {
    try {
        const response = await fetch(API.getCartUrl(), { credentials: 'include' });
        if (response.ok) {
            const data = await response.json();
            cart = data.items || [];
            localStorage.setItem('cart', JSON.stringify(cart));
        } else {
            loadCartFromLocal();
        }
    } catch (error) {
        console.error('Ошибка загрузки корзины из API:', error);
        loadCartFromLocal();
    }
}

function loadCartFromLocal() {
    cart = JSON.parse(localStorage.getItem('cart')) || [];
}

async function removeFromFavorites(productId) {
    try {
        const response = await fetch(API.toggleFavoriteUrl(productId), {
            method: 'DELETE',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        if (response.ok) {
            favorites = favorites.filter(f => f.id !== productId);
            localStorage.setItem('favorites', JSON.stringify(favorites));
            displayFavorites();
            showSuccess('Товар удален из избранного');
            if (currentModalProduct && currentModalProduct.id === productId) {
                closeModal();
            }
            return;
        }
    } catch (error) {
        console.error('Ошибка API:', error);
    }
    
    // Fallback на localStorage
    favorites = favorites.filter(f => f.id !== productId);
    localStorage.setItem('favorites', JSON.stringify(favorites));
    displayFavorites();
    showSuccess('Товар удален из избранного');
    if (currentModalProduct && currentModalProduct.id === productId) {
        closeModal();
    }
}

async function addToCart(product) {
    const isInCart = cart.some(item => item.id === product.id);
    
    if (isInCart) {
        showError('Товар уже в корзине');
        return;
    }
    
    try {
        const response = await fetch(API.addToCartUrl(product.id), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        if (response.ok) {
            await loadCart();
            showSuccess('Товар добавлен в корзину');
            updateCartBadgeInModal(true);
            return;
        }
    } catch (error) {
        console.error('Ошибка API:', error);
    }
    
    // Fallback на localStorage
    cart.push({ ...product, quantity: 1 });
    localStorage.setItem('cart', JSON.stringify(cart));
    showSuccess('Товар добавлен в корзину');
    updateCartBadgeInModal(true);
}

function updateCartBadgeInModal(isInCart) {
    const buyBtn = document.getElementById('modalBuyBtn');
    if (buyBtn) {
        buyBtn.textContent = isInCart ? '✓ В корзине' : 'Купить';
        buyBtn.style.backgroundColor = isInCart ? '#27ae60' : '#3498db';
    }
}

function displayFavorites() {
    const container = document.getElementById('favoritesContainer');
    const emptyDiv = document.getElementById('emptyFavorites');
    
    if (!container) return;
    
    if (favorites.length === 0) {
        if (container) container.style.display = 'none';
        if (emptyDiv) emptyDiv.style.display = 'block';
        return;
    }
    
    if (container) container.style.display = 'grid';
    if (emptyDiv) emptyDiv.style.display = 'none';
    
    const searchText = document.getElementById('searchInput')?.value.toLowerCase() || '';
    
    let filtered = [...favorites];
    if (searchText) {
        filtered = filtered.filter(item => 
            (item.title || '').toLowerCase().includes(searchText) || 
            (item.shortDescription || '').toLowerCase().includes(searchText)
        );
    }
    
    container.innerHTML = filtered.map(product => {
        const isInCart = cart.some(item => item.id === product.id);
        const title = product.title;
        const description = product.shortDescription || 'Нет описания';
        const imageUrl = product.mainImageUrl || 'https://via.placeholder.com/300x200?text=Нет+фото';
        const courseText = product.course ? `${product.course} курс` : '';
        const typeText = product.type || '';
        const seller = product.sellerName || 'Продавец';
        const price = product.price;
        
        return `
            <div class="favorite-card" onclick='openProductModal(${JSON.stringify(product).replace(/"/g, '&quot;')})'>
                <img src="${imageUrl}" 
                     alt="${escapeHtml(title)}" 
                     class="favorite-card-image"
                     onerror="this.src='https://via.placeholder.com/300x200?text=Ошибка'">
                <div class="favorite-card-info">
                    <div class="favorite-card-name">${escapeHtml(title)}</div>
                    <div class="favorite-card-description">${escapeHtml(description)}</div>
                    <div class="favorite-card-meta">
                        <span class="favorite-card-category">${courseText} ${typeText ? `• ${typeText}` : ''}</span>
                        <span class="favorite-card-price">${price.toLocaleString()} ₽</span>
                    </div>
                    <div class="favorite-card-footer">
                        <span class="favorite-card-seller">👤 ${escapeHtml(seller)}</span>
                        <button class="remove-favorite-btn" onclick="event.stopPropagation(); removeFromFavorites(${product.id})">🗑️</button>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

function filterFavorites() {
    displayFavorites();
}

function openProductModal(product) {
    currentModalProduct = product;
    const modal = document.getElementById('productModal');
    const isInCart = cart.some(item => item.id === product.id);
    
    const title = product.title;
    const description = product.description || product.shortDescription || 'Нет описания';
    const imageUrl = product.mainImageUrl || 'https://via.placeholder.com/600x300?text=Нет+фото';
    const courseText = product.course ? `${product.course} курс` : '';
    const typeText = product.type || '';
    const seller = product.sellerName || 'Не указан';
    const price = product.price;
    
    document.getElementById('modalImage').src = imageUrl;
    document.getElementById('modalName').textContent = title;
    document.getElementById('modalDescription').textContent = description;
    document.getElementById('modalCategory').textContent = `${courseText} ${typeText ? `• ${typeText}` : ''}`;
    document.getElementById('modalPrice').textContent = `${price.toLocaleString()} ₽`;
    document.getElementById('modalSeller').textContent = `👤 Продавец: ${escapeHtml(seller)}`;
    
    const buyBtn = document.getElementById('modalBuyBtn');
    buyBtn.textContent = isInCart ? '✓ В корзине' : 'Купить';
    buyBtn.style.backgroundColor = isInCart ? '#27ae60' : '#3498db';
    
    modal.style.display = 'block';
}

function closeModal() {
    document.getElementById('productModal').style.display = 'none';
    currentModalProduct = null;
}

function setupModalHandlers() {
    const modal = document.getElementById('productModal');
    const closeBtn = document.querySelector('.close-modal');
    if (closeBtn) closeBtn.onclick = closeModal;
    window.onclick = (event) => { if (event.target === modal) closeModal(); };
    
    document.getElementById('modalBuyBtn').onclick = () => {
        if (currentModalProduct) addToCart(currentModalProduct);
    };
    document.getElementById('modalRemoveBtn').onclick = () => {
        if (currentModalProduct) removeFromFavorites(currentModalProduct.id);
    };
}

function showSuccess(message) {
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#27ae60;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = '✅ ' + message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

function showError(message) {
    const toast = document.createElement('div');
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