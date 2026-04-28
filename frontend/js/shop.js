let allProducts = [];
let favorites = [];
let cart = [];
let currentUser = null;
let currentModalProduct = null;
let currentPage = 'shop';
let requestedProductId = null;

const fallbackImage =
    'data:image/svg+xml;charset=UTF-8,' +
    encodeURIComponent(`
        <svg xmlns="http://www.w3.org/2000/svg" width="560" height="360" viewBox="0 0 560 360">
            <rect width="560" height="360" fill="#f3f4f6"/>
            <text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle"
                  font-family="Arial, sans-serif" font-size="28" fill="#6b7280">Нет фото</text>
        </svg>
    `);

window.onload = initializeShopPage;

async function initializeShopPage() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    const urlParams = new URLSearchParams(window.location.search);
    currentPage = urlParams.get('page') === 'favorites' ? 'favorites' : 'shop';
    requestedProductId = urlParams.get('product') ? Number(urlParams.get('product')) : null;

    if (!currentUser && currentPage === 'favorites') {
        currentPage = 'shop';
    }

    setupModal();
    setupActiveNavigation();

    if (currentUser) {
        try {
            await Promise.all([loadFavoritesFromAPI(), loadCartFromAPI()]);
        } catch (error) {
            showToast(error.message || 'Не удалось загрузить данные пользователя', true);
        }
    } else {
        favorites = [];
        cart = [];
    }

    await loadProductsFromAPI();
    updateCartBadge();
    await openRequestedProductFromUrl();
}

function setupActiveNavigation() {
    document.querySelectorAll('.nav-links a').forEach(link => link.classList.remove('active'));

    if (currentPage === 'favorites') {
        const favoritesLink = document.getElementById('favoritesLink');
        if (favoritesLink) {
            favoritesLink.classList.add('active');
        }

        const filters = document.getElementById('filters-section');
        if (filters) {
            filters.style.display = 'none';
        }
        return;
    }

    const shopLink = document.querySelector('.nav-links a[href="shop.html"]');
    if (shopLink) {
        shopLink.classList.add('active');
    }

    const filters = document.getElementById('filters-section');
    if (filters) {
        filters.style.display = 'block';
    }
}

function setupModal() {
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
                toggleCart(currentModalProduct.id);
            }
        };
    }

    const favoriteButton = document.getElementById('modalFavoriteBtn');
    if (favoriteButton) {
        favoriteButton.onclick = () => {
            if (currentModalProduct) {
                toggleFavorite(currentModalProduct.id);
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

function closeModal() {
    const modal = document.getElementById('productModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

function convertAPIToLocalFormat(apiProduct) {
    return {
        id: apiProduct.id,
        sellerId: apiProduct.sellerId || null,
        name: apiProduct.title,
        description: apiProduct.shortDescription || apiProduct.description || 'Описание отсутствует',
        price: Number(apiProduct.price || 0),
        category: apiProduct.course ? `${apiProduct.course} курс` : '',
        type: apiProduct.type || '',
        image: normalizeAssetUrl(apiProduct.mainImageUrl || apiProduct.imageUrls?.[0]),
        seller: apiProduct.sellerName || 'Продавец',
        location: apiProduct.location || '',
        status: apiProduct.status || 'approved',
        moderatorComment: apiProduct.moderatorComment || null,
        createdAt: apiProduct.createdAt || null
    };
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

async function loadProductsFromAPI() {
    const params = new URLSearchParams();
    const search = document.getElementById('searchInput')?.value.trim();
    const category = document.getElementById('categoryFilter')?.value;
    const type = document.getElementById('typeFilter')?.value;
    const sortBy = document.getElementById('sortFilter')?.value || 'date_desc';

    params.set('sortBy', sortBy);
    if (search) {
        params.set('search', search);
    }

    if (category && category !== 'all') {
        const match = category.match(/(\d+)/);
        if (match) {
            params.set('course', match[1]);
        }
    }

    if (type && type !== 'all') {
        params.set('type', type);
    }

    try {
        const data = await requestJson(`${API.getAdvertisementsUrl()}?${params.toString()}`, {
            useCsrf: false,
            fallbackMessage: 'Не удалось загрузить товары'
        });

        allProducts = Array.isArray(data) ? data.map(convertAPIToLocalFormat) : [];
    } catch (error) {
        console.error('Ошибка загрузки товаров из API:', error);
        allProducts = [];
        showToast(error.message || 'Не удалось загрузить товары', true);
    }

    renderCurrentPage();
}

async function loadFavoritesFromAPI() {
    const data = await requestJson(API.getFavoritesUrl(), {
        useCsrf: false,
        fallbackMessage: 'Не удалось загрузить избранное'
    });

    favorites = Array.isArray(data) ? data.map(convertAPIToLocalFormat) : [];
}

async function loadCartFromAPI() {
    const data = await requestJson(API.getCartUrl(), {
        useCsrf: false,
        fallbackMessage: 'Не удалось загрузить корзину'
    });

    cart = Array.isArray(data?.items) ? data.items.map(convertAPIToLocalFormat) : [];
}

function renderCurrentPage() {
    if (currentPage === 'favorites') {
        showFavorites();
        return;
    }

    displayProducts(allProducts);
}

function filterProducts() {
    if (currentPage === 'favorites') {
        return;
    }

    loadProductsFromAPI();
}

function openProductModal(product) {
    currentModalProduct = product;
    const modal = document.getElementById('productModal');
    if (!modal) {
        return;
    }

    const isFavorite = favorites.some(item => item.id === product.id);
    const isInCart = cart.some(item => item.id === product.id);

    document.getElementById('modalImage').src = product.image || fallbackImage;
    document.getElementById('modalName').textContent = product.name || 'Без названия';
    document.getElementById('modalDescription').textContent = product.description || 'Описание отсутствует';
    document.getElementById('modalCategory').textContent = [product.category, product.type].filter(Boolean).join(' • ');
    document.getElementById('modalPrice').textContent = `${Number(product.price || 0).toLocaleString('ru-RU')} ₽`;
    document.getElementById('modalSeller').textContent = `Продавец: ${product.seller || 'Не указан'}`;
    updateSellerActions(product);

    const favoriteButton = document.getElementById('modalFavoriteBtn');
    if (favoriteButton) {
        favoriteButton.textContent = isFavorite ? '♥' : '♡';
    }

    const buyButton = document.getElementById('modalBuyBtn');
    if (buyButton) {
        buyButton.textContent = isInCart ? 'В корзине' : 'Купить';
        buyButton.style.backgroundColor = isInCart ? '#27ae60' : '#3498db';
    }

    modal.style.display = 'block';
}

function displayProducts(productsToShow) {
    const container = document.getElementById('productsContainer');
    if (!container) {
        return;
    }

    if (productsToShow.length === 0) {
        container.innerHTML = '<div class="no-products">Товары не найдены</div>';
        return;
    }

    container.innerHTML = productsToShow.map(renderProductCard).join('');
}

function renderProductCard(product) {
    const isFavorite = favorites.some(item => item.id === product.id);
    const isInCart = cart.some(item => item.id === product.id);
    const productJson = encodeURIComponent(JSON.stringify(product));
    const meta = [product.category, product.type].filter(Boolean).join(' • ');

    return `
        <div class="product-card" onclick="openProductModal(JSON.parse(decodeURIComponent('${productJson}')))">
            <img
                src="${escapeAttribute(product.image || fallbackImage)}"
                alt="${escapeAttribute(product.name || 'Товар')}"
                class="product-image"
                onerror="this.src='${fallbackImage}'">
            <div class="product-info">
                <div class="product-name">${escapeHtml(product.name || 'Без названия')}</div>
                <div class="product-description">${escapeHtml(product.description || 'Описание отсутствует')}</div>
                <div class="product-meta">
                    <span class="product-category">${escapeHtml(meta)}</span>
                    <span class="product-price">${Number(product.price || 0).toLocaleString('ru-RU')} ₽</span>
                </div>
                <div class="product-footer">
                    <span class="product-seller">${escapeHtml(product.seller || 'Продавец')}</span>
                    <button class="favorite-btn" onclick="event.stopPropagation(); toggleFavorite(${product.id})">
                        ${isFavorite ? '♥' : '♡'}
                    </button>
                </div>
                <div class="product-actions">
                    <button class="buy-btn ${isInCart ? 'added' : ''}" onclick="event.stopPropagation(); toggleCart(${product.id})">
                        ${isInCart ? 'В корзине' : 'Купить'}
                    </button>
                </div>
            </div>
        </div>
    `;
}

function showFavorites() {
    const favoriteIds = new Set(favorites.map(item => item.id));
    displayProducts(allProducts.filter(product => favoriteIds.has(product.id)));
}

async function toggleFavorite(productId) {
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    const wasFavorite = favorites.some(item => item.id === productId);

    try {
        await requestNoContent(API.toggleFavoriteUrl(productId), {
            method: wasFavorite ? 'DELETE' : 'POST',
            fallbackMessage: wasFavorite
                ? 'Не удалось удалить товар из избранного'
                : 'Не удалось добавить товар в избранное'
        });

        await loadFavoritesFromAPI();
        renderCurrentPage();
        refreshModalState(productId);
        showToast(wasFavorite ? 'Товар удален из избранного' : 'Товар добавлен в избранное');
    } catch (error) {
        showToast(error.message || 'Не удалось обновить избранное', true);
    }
}

async function toggleCart(productId) {
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    const wasInCart = cart.some(item => item.id === productId);

    try {
        await requestNoContent(
            wasInCart ? API.removeFromCartUrl(productId) : API.addToCartUrl(productId),
            {
                method: wasInCart ? 'DELETE' : 'POST',
                fallbackMessage: wasInCart
                    ? 'Не удалось удалить товар из корзины'
                    : 'Не удалось добавить товар в корзину'
            }
        );

        await loadCartFromAPI();
        updateCartBadge();
        renderCurrentPage();
        refreshModalState(productId);
        showToast(wasInCart ? 'Товар удален из корзины' : 'Товар добавлен в корзину');
    } catch (error) {
        showToast(error.message || 'Не удалось обновить корзину', true);
    }
}

function refreshModalState(productId) {
    if (!currentModalProduct || currentModalProduct.id !== productId) {
        return;
    }

    const isFavorite = favorites.some(item => item.id === productId);
    const isInCart = cart.some(item => item.id === productId);

    const favoriteButton = document.getElementById('modalFavoriteBtn');
    if (favoriteButton) {
        favoriteButton.textContent = isFavorite ? '♥' : '♡';
    }

    const buyButton = document.getElementById('modalBuyBtn');
    if (buyButton) {
        buyButton.textContent = isInCart ? 'В корзине' : 'Купить';
        buyButton.style.backgroundColor = isInCart ? '#27ae60' : '#3498db';
    }
}

function updateCartBadge() {
    const badge = document.getElementById('cartBadge');
    if (!badge) {
        return;
    }

    badge.style.display = cart.length > 0 ? 'inline-block' : 'none';
    badge.textContent = String(cart.length);
}

function updateSellerActions(product) {
    const sellerActions = document.getElementById('modalSellerActions');
    const profileButton = document.getElementById('modalProfileBtn');
    const chatButton = document.getElementById('modalChatBtn');
    const sellerId = Number(product?.sellerId || 0);
    const isOwnListing = Boolean(currentUser && sellerId > 0 && Number(currentUser.id) === sellerId);
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
        showToast('Не удалось открыть профиль продавца', true);
        return;
    }

    window.location.href = `public-profile.html?id=${sellerId}&product=${currentModalProduct.id}`;
}

function startChatWithSeller() {
    if (!currentModalProduct) {
        return;
    }

    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    const sellerId = Number(currentModalProduct.sellerId || 0);
    if (sellerId > 0 && Number(currentUser.id) === sellerId) {
        showToast('Нельзя начать чат с самим собой', true);
        return;
    }

    const chatUrl = sellerId > 0
        ? `chat.html?product=${currentModalProduct.id}&participant=${sellerId}`
        : `chat.html?product=${currentModalProduct.id}`;

    window.location.href = chatUrl;
}

async function openRequestedProductFromUrl() {
    if (!requestedProductId) {
        return;
    }

    let product = allProducts.find(item => item.id === requestedProductId) || null;

    if (!product) {
        try {
            const apiProduct = await requestJson(API.getAdvertisementUrl(requestedProductId), {
                useCsrf: false,
                fallbackMessage: 'Не удалось открыть объявление'
            });

            product = convertAPIToLocalFormat(apiProduct);
        } catch (error) {
            showToast(error.message || 'Не удалось открыть объявление', true);
            requestedProductId = null;
            return;
        }
    }

    openProductModal(product);
    requestedProductId = null;
}

function showToast(message, isError = false) {
    const toast = document.createElement('div');
    toast.style.cssText = `position:fixed;bottom:20px;right:20px;background:${isError ? '#e74c3c' : '#27ae60'};color:white;padding:12px 20px;border-radius:8px;z-index:10000;`;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

function escapeHtml(str) {
    if (!str) {
        return '';
    }

    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function escapeAttribute(str) {
    return escapeHtml(str).replace(/`/g, '&#96;');
}
