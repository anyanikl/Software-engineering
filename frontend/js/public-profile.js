let currentUser = null;
let targetUser = null;
let targetUserId = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    const urlParams = new URLSearchParams(window.location.search);
    targetUserId = parseInt(urlParams.get('id'));
    const productId = urlParams.get('product');
    
    if (!targetUserId) {
        showError('Пользователь не найден');
        setTimeout(() => {
            window.location.href = 'shop.html';
        }, 2000);
        return;
    }
    
    if (productId) {
        window.targetProductId = parseInt(productId);
    }
    
    await loadUserProfile(targetUserId);
};

// Преобразование PublicUserProfileDto в формат фронта
function convertPublicProfileToLocalFormat(apiProfile) {
    // Преобразуем activeAdvertisements (AdvertisementCardDto[])
    const activeAdvertisements = (apiProfile.activeAdvertisements || []).map(ad => ({
        id: ad.id,
        name: ad.title,
        description: ad.shortDescription || 'Нет описания',
        price: ad.price,
        category: '',  // в AdvertisementCardDto нет course
        type: ad.type,
        image: ad.mainImageUrl,
        seller: ad.sellerName,
        isFavorite: ad.isFavorite || false,
        isInCart: ad.isInCart || false
    }));
    
    // Преобразуем reviews (ReviewDto[])
    const reviews = (apiProfile.reviews || []).map(review => ({
        id: review.id,
        orderId: review.orderId,
        authorId: review.authorId,
        authorName: review.authorName,
        rating: review.rating,
        comment: review.comment,
        createdAt: review.createdAt,
        productName: review.productName
    }));
    
    return {
        id: apiProfile.id,
        fullName: apiProfile.fullName,
        avatarUrl: apiProfile.avatarUrl || null,
        faculty: apiProfile.faculty || '',
        rating: apiProfile.rating || 0,
        reviewsCount: apiProfile.reviewsCount || 0,
        reviews: reviews,
        activeAdvertisements: activeAdvertisements
    };
}

async function loadUserProfile(userId) {
    showLoading();
    
    try {
        const response = await fetch(API.getUserProfileUrl(userId), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            targetUser = convertPublicProfileToLocalFormat(data);
            displayProfile();
            hideLoading();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки профиля из API:', error);
    }
    
    // Fallback на localStorage
    const users = JSON.parse(localStorage.getItem('users')) || [];
    const user = users.find(u => u.id === userId);
    
    if (!user) {
        targetUser = {
            id: userId,
            fullName: 'Пользователь',
            avatarUrl: null,
            faculty: '',
            rating: 0,
            reviewsCount: 0,
            reviews: [],
            activeAdvertisements: []
        };
    } else {
        targetUser = {
            id: user.id,
            fullName: user.fullName || user.name,
            avatarUrl: user.avatarUrl || null,
            faculty: user.faculty || '',
            rating: user.rating || 0,
            reviewsCount: 0,
            reviews: [],
            activeAdvertisements: []
        };
    }
    
    // Загружаем отзывы из localStorage
    const reviews = JSON.parse(localStorage.getItem('reviews')) || [];
    targetUser.reviews = reviews.filter(r => r.sellerId === userId).map(r => ({
        id: r.id,
        authorName: r.authorName,
        rating: r.rating,
        comment: r.comment || r.text,
        createdAt: r.createdAt || r.date,
        productName: r.productName
    }));
    targetUser.reviewsCount = targetUser.reviews.length;
    
    // Вычисляем рейтинг
    if (targetUser.reviews.length > 0) {
        const avgRating = targetUser.reviews.reduce((sum, r) => sum + r.rating, 0) / targetUser.reviews.length;
        targetUser.rating = avgRating;
    }
    
    // Загружаем объявления из localStorage
    const listings = JSON.parse(localStorage.getItem('listings')) || [];
    targetUser.activeAdvertisements = listings.filter(l => 
        (l.sellerId === userId || l.sellerEmail === user?.email) && 
        l.status === 'approved'
    ).map(l => ({
        id: l.id,
        name: l.title || l.name,
        description: l.description || 'Нет описания',
        price: l.price,
        category: l.category || '',
        type: l.type,
        image: l.mainImageUrl || l.image,
        seller: l.sellerName || l.seller
    }));
    
    displayProfile();
    hideLoading();
}

function displayProfile() {
    const container = document.getElementById('profileContent');
    if (!container) return;
    
    if (!targetUser) {
        container.innerHTML = `
            <div class="profile-card">
                <div class="no-data">😕 Пользователь не найден</div>
            </div>
        `;
        return;
    }
    
    const reviews = targetUser.reviews || [];
    const listings = targetUser.activeAdvertisements || [];
    const avgRating = targetUser.rating || 0;
    
    container.innerHTML = `
        <div class="profile-card">
            <div class="profile-header">
                <div class="profile-avatar">
                    ${targetUser.avatarUrl ? `<img src="${targetUser.avatarUrl}" alt="${escapeHtml(targetUser.fullName)}">` : '👤'}
                </div>
                <div class="profile-info">
                    <h1>${escapeHtml(targetUser.fullName)}</h1>
                    ${targetUser.faculty ? `<p>🎓 ${escapeHtml(targetUser.faculty)}</p>` : ''}
                    <div class="rating">
                        <span class="rating-stars">${getStars(avgRating)}</span>
                        <span class="rating-value">${avgRating.toFixed(1)}</span>
                        <span class="rating-count">(${targetUser.reviewsCount || reviews.length} отзывов)</span>
                    </div>
                    ${currentUser && currentUser.id !== targetUser.id ? 
                        `<button class="chat-btn" onclick="startChat()">💬 Написать сообщение</button>` : ''}
                </div>
            </div>
        </div>
        
        <div class="stats-grid">
            <div class="stat-card">
                <div class="stat-value">${listings.length}</div>
                <div class="stat-label">Объявлений</div>
            </div>
            <div class="stat-card">
                <div class="stat-value">${reviews.length}</div>
                <div class="stat-label">Отзывов</div>
            </div>
            <div class="stat-card">
                <div class="stat-value">${listings.filter(l => l.status !== 'archived').length}</div>
                <div class="stat-label">Активных</div>
            </div>
        </div>
        
        <h2 class="section-title">📝 Отзывы</h2>
        <div class="reviews-list">
            ${reviews.length > 0 ? reviews.map(review => `
                <div class="review-card">
                    <div class="review-header">
                        <div>
                            <span class="review-author">👤 ${escapeHtml(review.authorName || 'Пользователь')}</span>
                            <span class="review-date">${formatDate(review.createdAt)}</span>
                        </div>
                        <span class="review-rating">${'★'.repeat(review.rating)}${'☆'.repeat(5 - review.rating)}</span>
                    </div>
                    <div class="review-text">${escapeHtml(review.comment)}</div>
                    <div class="review-product">📦 Товар: ${escapeHtml(review.productName || 'Не указан')}</div>
                </div>
            `).join('') : '<div class="no-data">Пока нет отзывов</div>'}
        </div>
        
        <h2 class="section-title">📦 Активные объявления</h2>
        <div class="products-grid">
            ${listings.length > 0 ? 
                listings.map(product => `
                    <div class="product-card" onclick="viewProduct(${product.id})">
                        <img src="${product.image || 'https://via.placeholder.com/300x180?text=Нет+фото'}" 
                             alt="${escapeHtml(product.name)}" 
                             class="product-image"
                             onerror="this.src='https://via.placeholder.com/300x180?text=Ошибка'">
                        <div class="product-info">
                            <div class="product-name">${escapeHtml(product.name)}</div>
                            <div class="product-category">${escapeHtml(product.category || 'Без категории')} • ${escapeHtml(product.type || '')}</div>
                            <div class="product-price">${(product.price || 0).toLocaleString()} ₽</div>
                        </div>
                    </div>
                `).join('') : '<div class="no-data">Нет активных объявлений</div>'}
        </div>
    `;
}

function getStars(rating) {
    const fullStars = Math.floor(rating);
    const hasHalf = rating % 1 >= 0.5;
    let stars = '';
    for (let i = 0; i < fullStars; i++) stars += '★';
    if (hasHalf) stars += '½';
    for (let i = 0; i < 5 - fullStars - (hasHalf ? 1 : 0); i++) stars += '☆';
    return stars;
}

function viewProduct(productId) {
    window.location.href = `shop.html?product=${productId}`;
}

function startChat() {
    if (!currentUser) {
        if (confirm('Для отправки сообщения необходимо войти в систему. Перейти на страницу входа?')) {
            window.location.href = 'index.html';
        }
        return;
    }
    
    if (window.targetProductId) {
        window.location.href = `chat.html?product=${window.targetProductId}&seller=${targetUser.id}`;
    } else {
        window.location.href = `chat.html?seller=${targetUser.id}`;
    }
}

function showLoading() {
    const container = document.getElementById('profileContent');
    if (container) {
        container.innerHTML = '<div class="loading-spinner">⏳ Загрузка профиля...</div>';
    }
}

function hideLoading() {}

function showError(message) {
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#e74c3c;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = '❌ ' + message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3000);
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
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