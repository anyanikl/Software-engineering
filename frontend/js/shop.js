// Демо-товары для примера (всегда доступны) - в старом формате
const demoProducts = [
    {
        id: 1,
        name: 'Учебник по мат. анализу 1 курс',
        description: 'Высшая математика, линейная алгебра, аналитическая геометрия. Состояние отличное',
        price: 900,
        category: '1 курс',
        type: 'учебники',
        date: '2026-03-15',
        image: 'https://avatars.mds.yandex.net/get-marketpic/16279129/piccb080f0ee1dd31766ced51aeae85c96a/orig',
        seller: 'Иван П.'
    },
    {
        id: 2,
        name: 'Лабораторная работа по компьютерной графике',
        description: 'Готовая лаба по комп. графике, все расчеты и выводы',
        price: 1000,
        category: '3 курс',
        type: 'лабораторные',
        date: '2026-03-14',
        image: 'https://avatars.mds.yandex.net/i?id=bff5fbe586af0b8247542db28b2db6d7_l-5234434-images-thumbs&n=13',
        seller: 'Мария С.'
    },
    {
        id: 3,
        name: 'Курсовая по программированию',
        description: 'Веб-приложение на React + Node.js, полный код и пояснения',
        price: 5000,
        category: '3 курс',
        type: 'курсовые',
        date: '2026-03-13',
        image: 'https://www.angularspace.com/content/images/2024/07/GPYrUf8bMAAxaHu--1--1.jpg',
        seller: 'Алексей К.'
    },
    {
        id: 4,
        name: 'Дипломная работа "ИИ в образовании"',
        description: 'Готовая дипломная работа, 80 страниц, презентация, речь',
        price: 7000,
        category: '4 курс',
        type: 'дипломы',
        date: '2026-03-12',
        image: 'https://ic.pics.livejournal.com/alex_nevz/89963941/567382/567382_800.png',
        seller: 'Елена В.'
    },
    {
        id: 5,
        name: 'Отчет по практике',
        description: 'Производственная практика в IT компании, дневник и отчет',
        price: 1300,
        category: '3 курс',
        type: 'практики',
        date: '2026-03-11',
        image: 'https://cf.ppt-online.org/files/slide/6/6M3KguJXpFiI8UrBRxfjTva50HsYDGb7cZVSO1/slide-2.jpg',
        seller: 'Дмитрий Н.'
    },
    {
        id: 6,
        name: 'Лабораторная по физике',
        description: 'Полностью оформленная работа',
        price: 400,
        category: '1 курс',
        type: 'лабораторные',
        date: '2026-03-10',
        image: 'https://avatars.mds.yandex.net/i?id=623f208ab21a10188f935435769b7608_l-5877226-images-thumbs&n=13',
        seller: 'Анна М.'
    }
];

// Объединенный список всех товаров (демо + одобренные из localStorage + из API)
let allProducts = [...demoProducts];

// Избранное (хранится в localStorage)
let favorites = JSON.parse(localStorage.getItem('favorites')) || [];

// Корзина (хранится в localStorage)
let cart = JSON.parse(localStorage.getItem('cart')) || [];

let currentUser = null;
let currentModalProduct = null;

window.onload = async function() {
    // Инициализация API
    if (typeof fetchCsrfToken !== 'undefined') {
        await fetchCsrfToken();
        currentUser = await checkAuth();
        
        if (currentUser) {
            await loadFavoritesFromAPI();
            await loadCartFromAPI();
            await loadProductsFromAPI();
        }
    }
    
    loadUserProducts();
    updateCartBadge();
    
    const urlParams = new URLSearchParams(window.location.search);
    const page = urlParams.get('page');
    
    if (page === 'favorites') {
        document.querySelectorAll('.nav-links a').forEach(a => a.classList.remove('active'));
        document.getElementById('favoritesLink').classList.add('active');
        document.getElementById('filters-section').style.display = 'none';
        showFavorites();
    } else {
        document.querySelectorAll('.nav-links a').forEach(a => a.classList.remove('active'));
        const shopLink = document.querySelector('.nav-links a[href="shop.html"]');
        if (shopLink) shopLink.classList.add('active');
        document.getElementById('filters-section').style.display = 'block';
        displayProducts(allProducts);
    }

    // Закрытие модального окна
    const modal = document.getElementById('productModal');
    const closeBtn = document.querySelector('.close-modal');
    
    if (closeBtn) {
        closeBtn.onclick = function() {
            modal.style.display = 'none';
        }
    }
    
    window.onclick = function(event) {
        if (event.target === modal) {
            modal.style.display = 'none';
        }
    }
    
    // Обработчики для модального окна
    const modalBuyBtn = document.getElementById('modalBuyBtn');
    const modalFavoriteBtn = document.getElementById('modalFavoriteBtn');
    
    if (modalBuyBtn) {
        const newBuyBtn = modalBuyBtn.cloneNode(true);
        modalBuyBtn.parentNode.replaceChild(newBuyBtn, modalBuyBtn);
        newBuyBtn.onclick = function() {
            if (currentModalProduct) {
                toggleCart(currentModalProduct.id);
            }
        };
    }
    
    if (modalFavoriteBtn) {
        const newFavoriteBtn = modalFavoriteBtn.cloneNode(true);
        modalFavoriteBtn.parentNode.replaceChild(newFavoriteBtn, modalFavoriteBtn);
        newFavoriteBtn.onclick = function() {
            if (currentModalProduct) {
                toggleFavorite(currentModalProduct.id);
            }
        };
    }
};

// Преобразование DTO из API в формат, понятный фронту
function convertAPIToLocalFormat(apiProduct) {
    // AdvertisementCardDto и AdvertisementDto имеют разные поля
    const isCardDto = apiProduct.shortDescription !== undefined;
    
    // Определяем категорию из course (если есть)
    let category = '';
    if (apiProduct.course) {
        category = `${apiProduct.course} курс`;
    }
    
    // Определяем тип на русском
    let typeMap = {
        'book': 'учебники',
        'laboratory': 'лабораторные',
        'coursework': 'курсовые',
        'practice': 'практики',
        'diploma': 'дипломы'
    };
    let type = typeMap[apiProduct.type] || apiProduct.type || '';
    
    return {
        id: apiProduct.id,
        name: apiProduct.title,
        description: isCardDto ? (apiProduct.shortDescription || 'Нет описания') : (apiProduct.description || 'Нет описания'),
        price: apiProduct.price,
        category: category,
        type: type,
        image: apiProduct.mainImageUrl || (apiProduct.imageUrls ? apiProduct.imageUrls[0] : null),
        seller: apiProduct.sellerName,
        location: apiProduct.location || '',
        status: apiProduct.status || 'approved',
        moderatorComment: apiProduct.moderatorComment || null,
        isFavorite: apiProduct.isFavorite || false,
        isInCart: apiProduct.isInCart || false,
        createdAt: apiProduct.createdAt,
        ordersCount: apiProduct.ordersCount || 0
    };
}

async function loadProductsFromAPI() {
    const search = document.getElementById('searchInput')?.value || '';
    const category = document.getElementById('categoryFilter')?.value;
    const type = document.getElementById('typeFilter')?.value;
    const sortBy = document.getElementById('sortFilter')?.value || 'date_desc';
    
    // Преобразуем категорию в course (число)
    let course = '';
    if (category && category !== 'all') {
        const match = category.match(/(\d+)/);
        if (match) course = match[1];
    }
    
    // Преобразуем тип на русском в английский
    let typeMapReverse = {
        'учебники': 'book',
        'лабораторные': 'laboratory',
        'курсовые': 'coursework',
        'практики': 'practice',
        'дипломы': 'diploma'
    };
    let apiType = typeMapReverse[type] || (type !== 'all' ? type : '');
    
    let url = `${API.getAdvertisementsUrl()}?sortBy=${sortBy}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    if (course) url += `&course=${course}`;
    if (apiType && apiType !== 'all') url += `&type=${apiType}`;
    
    try {
        const response = await fetch(url, { credentials: 'include' });
        if (response.ok) {
            const data = await response.json();
            if (data && data.length > 0) {
                // Преобразуем API данные в本地 формат
                const convertedProducts = data.map(apiProduct => convertAPIToLocalFormat(apiProduct));
                
                // Объединяем с существующими товарами, избегая дубликатов
                const existingIds = new Set(allProducts.map(p => p.id));
                const newProducts = convertedProducts.filter(p => !existingIds.has(p.id));
                allProducts = [...allProducts, ...newProducts];
                
                // Обновляем отображение
                const urlParams = new URLSearchParams(window.location.search);
                if (urlParams.get('page') === 'favorites') {
                    showFavorites();
                } else {
                    displayProducts(allProducts);
                }
            }
        }
    } catch (error) {
        console.error('Ошибка загрузки товаров из API:', error);
    }
}

async function loadFavoritesFromAPI() {
    if (!currentUser) return;
    try {
        const response = await fetch(API.getFavoritesUrl(), { credentials: 'include' });
        if (response.ok) {
            const data = await response.json();
            // Преобразуем и сохраняем в favorites
            favorites = data.map(apiProduct => convertAPIToLocalFormat(apiProduct));
            localStorage.setItem('favorites', JSON.stringify(favorites));
        }
    } catch (error) { console.error('Ошибка загрузки избранного:', error); }
}

async function loadCartFromAPI() {
    if (!currentUser) return;
    try {
        const response = await fetch(API.getCartUrl(), { credentials: 'include' });
        if (response.ok) {
            const data = await response.json();
            cart = (data.items || []).map(item => ({
                ...convertAPIToLocalFormat(item),
                quantity: item.quantity || 1
            }));
            localStorage.setItem('cart', JSON.stringify(cart));
            updateCartBadge();
        }
    } catch (error) { console.error('Ошибка загрузки корзины:', error); }
}

async function syncFavoriteToAPI(productId, isFavorite) {
    if (!currentUser) return false;
    try {
        const response = await fetch(API.toggleFavoriteUrl(productId), {
            method: isFavorite ? 'POST' : 'DELETE',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        return response.ok;
    } catch (error) {
        console.error('Ошибка синхронизации избранного:', error);
        return false;
    }
}

async function syncCartToAPI(productId, isInCart) {
    if (!currentUser) return false;
    try {
        const response = await fetch(isInCart ? API.addToCartUrl(productId) : API.removeFromCartUrl(productId), {
            method: isInCart ? 'POST' : 'DELETE',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        return response.ok;
    } catch (error) {
        console.error('Ошибка синхронизации корзины:', error);
        return false;
    }
}

// ========== ОСНОВНЫЕ ФУНКЦИИ (без изменений логики) ==========

function loadUserProducts() {
    // Загружаем объявления пользователей из localStorage
    const userListings = JSON.parse(localStorage.getItem('listings')) || [];
    
    // Фильтруем только одобренные
    const approvedListings = userListings.filter(listing => listing.status === 'approved');
    
    // Добавляем их к демо-товарам, но исключаем дубликаты по id
    const existingIds = new Set(demoProducts.map(p => p.id));
    const newListings = approvedListings.filter(listing => !existingIds.has(listing.id));
    
    allProducts = [...demoProducts, ...newListings];
}

function updateCartBadge() {
    const badge = document.getElementById('cartBadge');
    if (badge) {
        if (cart.length > 0) {
            badge.style.display = 'inline-block';
            badge.textContent = cart.length;
        } else {
            badge.style.display = 'none';
        }
    }
}

function filterProducts() {
    const searchText = document.getElementById('searchInput').value.toLowerCase();
    const category = document.getElementById('categoryFilter').value;
    const type = document.getElementById('typeFilter').value;
    const sort = document.getElementById('sortFilter').value;

    let filtered = allProducts.filter(product => {
        const matchesSearch = product.name.toLowerCase().includes(searchText) || 
                             (product.description || '').toLowerCase().includes(searchText);
        const matchesCategory = category === 'all' || product.category === category;
        const matchesType = type === 'all' || product.type === type;
        
        return matchesSearch && matchesCategory && matchesType;
    });

    if (sort === 'price_asc') {
        filtered.sort((a, b) => a.price - b.price);
    } else if (sort === 'price_desc') {
        filtered.sort((a, b) => b.price - a.price);
    } else if (sort === 'date_desc') {
        filtered.sort((a, b) => new Date(b.date || b.createdAt || 0) - new Date(a.date || a.createdAt || 0));
    } else if (sort === 'date_asc') {
        filtered.sort((a, b) => new Date(a.date || a.createdAt || 0) - new Date(b.date || b.createdAt || 0));
    }

    displayProducts(filtered);
}

function openProductModal(product) {
    currentModalProduct = product;
    const modal = document.getElementById('productModal');
    const isFavorite = favorites.some(f => f.id === product.id);
    const isInCart = cart.some(item => item.id === product.id);
    
    document.getElementById('modalImage').src = product.image || 'https://via.placeholder.com/600x300?text=Нет+фото';
    document.getElementById('modalName').textContent = product.name;
    document.getElementById('modalDescription').textContent = product.description || 'Нет описания';
    document.getElementById('modalCategory').textContent = `${product.category} • ${product.type}`;
    document.getElementById('modalPrice').textContent = `${product.price} ₽`;
    document.getElementById('modalSeller').textContent = `👤 Продавец: ${product.seller || 'Не указан'}`;
    
    const favoriteBtn = document.getElementById('modalFavoriteBtn');
    if (favoriteBtn) favoriteBtn.textContent = isFavorite ? '❤️' : '🤍';
    
    const buyBtn = document.getElementById('modalBuyBtn');
    if (buyBtn) {
        buyBtn.textContent = isInCart ? '✓ В корзине' : 'Купить';
        if (isInCart) {
            buyBtn.style.backgroundColor = '#27ae60';
        } else {
            buyBtn.style.backgroundColor = '#3498db';
        }
    }
    
    modal.style.display = 'block';
}

function displayProducts(productsToShow) {
    const container = document.getElementById('productsContainer');
    if (!container) return;
    
    if (productsToShow.length === 0) {
        container.innerHTML = '<div class="no-products">😕 Товары не найдены</div>';
        return;
    }
    
    container.innerHTML = productsToShow.map(product => {
        const isFavorite = favorites.some(f => f.id === product.id);
        const isInCart = cart.some(item => item.id === product.id);
        
        // Экранируем JSON для передачи в onclick
        const productJson = JSON.stringify(product).replace(/"/g, '&quot;');
        
        return `
            <div class="product-card" onclick="openProductModal(${productJson})">
                <img src="${product.image || 'https://via.placeholder.com/280x180?text=Нет+фото'}" 
                     alt="${product.name}" 
                     class="product-image"
                     onerror="this.src='https://via.placeholder.com/280x180?text=Ошибка'">
                <div class="product-info">
                    <div class="product-name">${escapeHtml(product.name)}</div>
                    <div class="product-description">${escapeHtml(product.description || 'Нет описания')}</div>
                    <div class="product-meta">
                        <span class="product-category">${product.category} • ${product.type}</span>
                        <span class="product-price">${product.price} ₽</span>
                    </div>
                    <div class="product-footer">
                        <span class="product-seller">👤 ${escapeHtml(product.seller || 'Продавец')}</span>
                        <button class="favorite-btn" onclick="event.stopPropagation(); toggleFavorite(${product.id})">
                            ${isFavorite ? '❤️' : '🤍'}
                        </button>
                    </div>
                    <div class="product-actions">
                        <button class="buy-btn ${isInCart ? 'added' : ''}" onclick="event.stopPropagation(); toggleCart(${product.id})">
                            ${isInCart ? '✓ В корзине' : 'Купить'}
                        </button>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

// Функция для экранирования HTML (безопасность)
function escapeHtml(str) {
    if (!str) return '';
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function showFavorites() {
    const favoritesToShow = allProducts.filter(product => 
        favorites.some(f => f.id === product.id)
    );
    displayProducts(favoritesToShow);
}

async function toggleFavorite(productId) {
    const product = allProducts.find(p => p.id === productId);
    if (!product) return;
    
    const index = favorites.findIndex(f => f.id === productId);
    const wasFavorite = index !== -1;
    
    // Сначала обновляем локально
    if (index === -1) {
        favorites.push(product);
    } else {
        favorites.splice(index, 1);
    }
    
    localStorage.setItem('favorites', JSON.stringify(favorites));
    
    // Синхронизируем с API (если пользователь авторизован)
    if (currentUser) {
        await syncFavoriteToAPI(productId, !wasFavorite);
    }
    
    // Обновляем модальное окно если открыто
    if (currentModalProduct && currentModalProduct.id === productId) {
        const isFavorite = favorites.some(f => f.id === productId);
        const favoriteBtn = document.getElementById('modalFavoriteBtn');
        if (favoriteBtn) {
            favoriteBtn.textContent = isFavorite ? '❤️' : '🤍';
        }
    }
    
    // Обновляем отображение в зависимости от текущей страницы
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('page') === 'favorites') {
        showFavorites();
    } else {
        filterProducts();
    }
}

async function toggleCart(productId) {
    const product = allProducts.find(p => p.id === productId);
    if (!product) return;
    
    const index = cart.findIndex(item => item.id === productId);
    const wasInCart = index !== -1;
    
    // Сначала обновляем локально
    if (index === -1) {
        cart.push({...product, quantity: 1});
    } else {
        cart.splice(index, 1);
    }
    
    localStorage.setItem('cart', JSON.stringify(cart));
    updateCartBadge();
    
    // Синхронизируем с API (если пользователь авторизован)
    if (currentUser) {
        await syncCartToAPI(productId, !wasInCart);
    }
    
    // Обновляем модальное окно если открыто
    if (currentModalProduct && currentModalProduct.id === productId) {
        const isInCart = cart.some(item => item.id === productId);
        const buyBtn = document.getElementById('modalBuyBtn');
        if (buyBtn) {
            buyBtn.textContent = isInCart ? '✓ В корзине' : 'Купить';
            buyBtn.style.backgroundColor = isInCart ? '#27ae60' : '#3498db';
        }
    }
    
    filterProducts(); // Обновляем отображение кнопок
}

function logout() {
    localStorage.removeItem('user');
    localStorage.removeItem('favorites');
    localStorage.removeItem('cart');
    window.location.href = 'index.html';
}