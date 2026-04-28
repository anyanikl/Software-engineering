let favorites = [];
let cart = [];
let currentUser = null;
let currentModalProduct = null;

const fallbackImage =
    'data:image/svg+xml;charset=UTF-8,' +
    encodeURIComponent(`
        <svg xmlns="http://www.w3.org/2000/svg" width="360" height="240" viewBox="0 0 360 240">
            <rect width="360" height="240" fill="#f3f4f6"/>
            <text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle"
                  font-family="Arial, sans-serif" font-size="24" fill="#6b7280">Нет фото</text>
        </svg>
    `);

window.onload = initializeFavoritesPage;

async function initializeFavoritesPage() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    setupModalHandlers();

    try {
        await Promise.all([loadFavorites(), loadCart()]);
    } catch (error) {
        showError(error.message || 'Не удалось загрузить избранное');
    }

    displayFavorites();
}

async function loadFavorites() {
    const data = await requestJson(API.getFavoritesUrl(), {
        useCsrf: false,
        fallbackMessage: 'Не удалось загрузить избранное'
    });

    favorites = Array.isArray(data) ? data : [];
}

async function loadCart() {
    const data = await requestJson(API.getCartUrl(), {
        useCsrf: false,
        fallbackMessage: 'Не удалось загрузить корзину'
    });

    cart = Array.isArray(data?.items) ? data.items : [];
}

async function removeFromFavorites(productId) {
    try {
        await requestNoContent(API.toggleFavoriteUrl(productId), {
            method: 'DELETE',
            fallbackMessage: 'Не удалось удалить товар из избранного'
        });

        await loadFavorites();
        displayFavorites();

        if (currentModalProduct?.id === productId) {
            closeModal();
        }

        showSuccess('Товар удален из избранного');
    } catch (error) {
        showError(error.message || 'Не удалось удалить товар из избранного');
    }
}

async function addToCart(product) {
    if (cart.some(item => item.id === product.id)) {
        showError('Товар уже в корзине');
        return;
    }

    try {
        await requestNoContent(API.addToCartUrl(product.id), {
            method: 'POST',
            fallbackMessage: 'Не удалось добавить товар в корзину'
        });

        await loadCart();
        displayFavorites();
        updateCartBadgeInModal(true);
        showSuccess('Товар добавлен в корзину');
    } catch (error) {
        showError(error.message || 'Не удалось добавить товар в корзину');
    }
}

function displayFavorites() {
    const container = document.getElementById('favoritesContainer');
    const emptyState = document.getElementById('emptyFavorites');
    if (!container) {
        return;
    }

    const favoritesToShow = getFilteredAndSortedFavorites();

    if (favoritesToShow.length === 0) {
        container.innerHTML = '';
        container.style.display = 'none';
        if (emptyState) {
            emptyState.style.display = 'block';
        }
        return;
    }

    container.style.display = 'grid';
    if (emptyState) {
        emptyState.style.display = 'none';
    }

    container.innerHTML = favoritesToShow.map(renderFavoriteCard).join('');
}

function getFilteredAndSortedFavorites() {
    const searchText = (document.getElementById('searchInput')?.value || '').trim().toLowerCase();
    const sortBy = document.getElementById('sortFilter')?.value || 'date_desc';

    const filtered = favorites.filter(item => {
        const title = String(item.title || '').toLowerCase();
        const description = String(item.shortDescription || '').toLowerCase();
        return !searchText || title.includes(searchText) || description.includes(searchText);
    });

    return filtered.sort((left, right) => compareFavorites(left, right, sortBy));
}

function compareFavorites(left, right, sortBy) {
    if (sortBy === 'price_asc') {
        return Number(left.price || 0) - Number(right.price || 0);
    }

    if (sortBy === 'price_desc') {
        return Number(right.price || 0) - Number(left.price || 0);
    }

    const leftDate = new Date(left.createdAt || 0).getTime();
    const rightDate = new Date(right.createdAt || 0).getTime();

    return sortBy === 'date_asc'
        ? leftDate - rightDate
        : rightDate - leftDate;
}

function renderFavoriteCard(product) {
    const isInCart = cart.some(item => item.id === product.id);
    const productJson = encodeURIComponent(JSON.stringify(product));
    const imageUrl = normalizeAssetUrl(product.mainImageUrl);
    const meta = [product.course ? `${product.course} курс` : '', product.type || '']
        .filter(Boolean)
        .join(' • ');

    return `
        <div class="favorite-card" onclick="openProductModal(JSON.parse(decodeURIComponent('${productJson}')))">
            <img
                src="${escapeAttribute(imageUrl)}"
                alt="${escapeAttribute(product.title || 'Товар')}"
                class="favorite-card-image"
                onerror="this.src='${fallbackImage}'">
            <div class="favorite-card-info">
                <div class="favorite-card-name">${escapeHtml(product.title || 'Без названия')}</div>
                <div class="favorite-card-description">${escapeHtml(product.shortDescription || 'Описание отсутствует')}</div>
                <div class="favorite-card-meta">
                    <span class="favorite-card-category">${escapeHtml(meta)}</span>
                    <span class="favorite-card-price">${Number(product.price || 0).toLocaleString('ru-RU')} ₽</span>
                </div>
                <div class="favorite-card-footer">
                    <span class="favorite-card-seller">${escapeHtml(product.sellerName || 'Продавец')}</span>
                    <button class="remove-favorite-btn" onclick="event.stopPropagation(); removeFromFavorites(${product.id})">Удалить</button>
                </div>
                <div class="favorite-card-actions">
                    <button class="buy-btn ${isInCart ? 'added' : ''}" onclick="event.stopPropagation(); addToCart(JSON.parse(decodeURIComponent('${productJson}')))">
                        ${isInCart ? 'В корзине' : 'В корзину'}
                    </button>
                </div>
            </div>
        </div>
    `;
}

function filterFavorites() {
    displayFavorites();
}

function openProductModal(product) {
    currentModalProduct = product;
    const modal = document.getElementById('productModal');
    if (!modal) {
        return;
    }

    const isInCart = cart.some(item => item.id === product.id);

    document.getElementById('modalImage').src = normalizeAssetUrl(product.mainImageUrl);
    document.getElementById('modalName').textContent = product.title || 'Без названия';
    document.getElementById('modalDescription').textContent = product.shortDescription || 'Описание отсутствует';
    document.getElementById('modalCategory').textContent = [product.course ? `${product.course} курс` : '', product.type || '']
        .filter(Boolean)
        .join(' • ');
    document.getElementById('modalPrice').textContent = `${Number(product.price || 0).toLocaleString('ru-RU')} ₽`;
    document.getElementById('modalSeller').textContent = `Продавец: ${product.sellerName || 'Не указан'}`;
    updateSellerActions(product);

    updateCartBadgeInModal(isInCart);
    modal.style.display = 'block';
}

function closeModal() {
    const modal = document.getElementById('productModal');
    if (modal) {
        modal.style.display = 'none';
    }

    currentModalProduct = null;
}

function setupModalHandlers() {
    const modal = document.getElementById('productModal');
    const closeButton = document.querySelector('.close-modal');

    if (closeButton) {
        closeButton.onclick = closeModal;
    }

    window.onclick = event => {
        if (event.target === modal) {
            closeModal();
        }
    };

    const buyButton = document.getElementById('modalBuyBtn');
    if (buyButton) {
        buyButton.onclick = () => {
            if (currentModalProduct) {
                addToCart(currentModalProduct);
            }
        };
    }

    const removeButton = document.getElementById('modalRemoveBtn');
    if (removeButton) {
        removeButton.onclick = () => {
            if (currentModalProduct) {
                removeFromFavorites(currentModalProduct.id);
            }
        };
    }

    const profileButton = document.getElementById('modalProfileBtn');
    if (profileButton) {
        profileButton.onclick = openSellerProfile;
    }

    const chatButton = document.getElementById('modalChatBtn');
    if (chatButton) {
        chatButton.onclick = startChatWithSeller;
    }
}

function updateCartBadgeInModal(isInCart) {
    const buyButton = document.getElementById('modalBuyBtn');
    if (!buyButton) {
        return;
    }

    buyButton.textContent = isInCart ? 'В корзине' : 'Купить';
    buyButton.style.backgroundColor = isInCart ? '#27ae60' : '#3498db';
}

function updateSellerActions(product) {
    const sellerActions = document.getElementById('modalSellerActions');
    const profileButton = document.getElementById('modalProfileBtn');
    const chatButton = document.getElementById('modalChatBtn');
    const sellerId = Number(product?.sellerId || 0);
    const isOwnListing = sellerId > 0 && Number(currentUser?.id || 0) === sellerId;
    const canOpenProfile = sellerId > 0 && !isOwnListing;
    const canStartChat = Boolean(product?.id) && !isOwnListing;

    if (profileButton) {
        profileButton.style.display = canOpenProfile ? 'inline-block' : 'none';
    }

    if (chatButton) {
        chatButton.style.display = canStartChat ? 'inline-block' : 'none';
    }

    if (sellerActions) {
        sellerActions.style.display = canOpenProfile || canStartChat ? 'flex' : 'none';
    }
}

function openSellerProfile() {
    if (!currentModalProduct) {
        return;
    }

    const sellerId = Number(currentModalProduct.sellerId || 0);
    if (!sellerId) {
        showError('Не удалось открыть профиль продавца');
        return;
    }

    window.location.href = `public-profile.html?id=${sellerId}&product=${currentModalProduct.id}`;
}

function startChatWithSeller() {
    if (!currentModalProduct) {
        return;
    }

    const sellerId = Number(currentModalProduct.sellerId || 0);
    if (sellerId > 0 && Number(currentUser?.id || 0) === sellerId) {
        showError('Нельзя начать чат с самим собой');
        return;
    }

    const chatUrl = sellerId > 0
        ? `chat.html?product=${currentModalProduct.id}&participant=${sellerId}`
        : `chat.html?product=${currentModalProduct.id}`;

    window.location.href = chatUrl;
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
    toast.style.cssText = `position:fixed;bottom:20px;right:20px;background:${isError ? '#e74c3c' : '#27ae60'};color:white;padding:12px 20px;border-radius:8px;z-index:10000;`;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}
