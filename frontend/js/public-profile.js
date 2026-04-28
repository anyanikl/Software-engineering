let currentUser = null;
let targetUser = null;
let targetUserId = null;
let targetProductId = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    const urlParams = new URLSearchParams(window.location.search);
    targetUserId = Number(urlParams.get('id'));
    targetProductId = urlParams.get('product') ? Number(urlParams.get('product')) : null;

    if (!targetUserId) {
        showError('Пользователь не найден');
        setTimeout(() => {
            window.location.href = 'shop.html';
        }, 1500);
        return;
    }

    await loadUserProfile(targetUserId);
};

async function loadUserProfile(userId) {
    showLoading();

    try {
        const response = await fetch(API.getUserProfileUrl(userId), {
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(await parseApiError(response, 'Не удалось загрузить профиль'));
        }

        targetUser = await response.json();
        displayProfile();
    } catch (error) {
        showError(error.message || 'Не удалось загрузить профиль');
    } finally {
        hideLoading();
    }
}

function displayProfile() {
    const container = document.getElementById('profileContent');
    if (!container || !targetUser) return;

    const reviews = Array.isArray(targetUser.reviews) ? targetUser.reviews : [];
    const listings = Array.isArray(targetUser.activeAdvertisements) ? targetUser.activeAdvertisements : [];
    const canMessageUser = Boolean(currentUser && currentUser.id !== targetUser.id);
    const canStartHeaderChat = canMessageUser && (Boolean(targetProductId) || listings.length === 1);

    container.innerHTML = `
        <div class="profile-card">
            <div class="profile-header">
                <div class="profile-avatar">
                    ${targetUser.avatarUrl ? `<img src="${escapeHtml(targetUser.avatarUrl)}" alt="${escapeHtml(targetUser.fullName)}">` : '👤'}
                </div>
                <div class="profile-info">
                    <h1>${escapeHtml(targetUser.fullName)}</h1>
                    ${targetUser.faculty ? `<p>${escapeHtml(targetUser.faculty)}</p>` : ''}
                    <div class="rating">
                        <span class="rating-stars">${getStars(targetUser.rating || 0)}</span>
                        <span class="rating-value">${Number(targetUser.rating || 0).toFixed(1)}</span>
                        <span class="rating-count">(${targetUser.reviewsCount || reviews.length} отзывов)</span>
                    </div>
                    ${canStartHeaderChat ? `
                        <button class="chat-btn" onclick="startChat(${targetProductId || listings[0].id})">Написать по объявлению</button>
                    ` : ''}
                    ${canMessageUser && !canStartHeaderChat ? `
                        <p class="chat-hint">Выберите объявление ниже, чтобы начать чат по конкретному товару.</p>
                    ` : ''}
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
                <div class="stat-value">${Number(targetUser.rating || 0).toFixed(1)}</div>
                <div class="stat-label">Рейтинг</div>
            </div>
        </div>

        <h2 class="section-title">Отзывы</h2>
        <div class="reviews-list">
            ${reviews.length > 0 ? reviews.map(review => `
                <div class="review-card">
                    <div class="review-header">
                        <div>
                            <span class="review-author">${escapeHtml(review.authorName || 'Пользователь')}</span>
                            <span class="review-date">${formatDate(review.createdAt)}</span>
                        </div>
                        <span class="review-rating">${'★'.repeat(review.rating)}${'☆'.repeat(5 - review.rating)}</span>
                    </div>
                    <div class="review-text">${escapeHtml(review.comment || '')}</div>
                    <div class="review-product">Товар: ${escapeHtml(review.productName || 'Не указан')}</div>
                </div>
            `).join('') : '<div class="no-data">Пока нет отзывов</div>'}
        </div>

        <h2 class="section-title">Активные объявления</h2>
        <div class="products-grid">
            ${listings.length > 0 ? listings.map(product => `
                <div class="product-card" onclick="viewProduct(${product.id})">
                    <img src="${escapeHtml(product.mainImageUrl || 'https://via.placeholder.com/300x180?text=Нет+фото')}"
                         alt="${escapeHtml(product.title)}"
                         class="product-image"
                         onerror="this.src='https://via.placeholder.com/300x180?text=Ошибка'">
                    <div class="product-info">
                        <div class="product-name">${escapeHtml(product.title)}</div>
                        <div class="product-category">${escapeHtml(product.type || '')}</div>
                        <div class="product-price">${Number(product.price || 0).toLocaleString('ru-RU')} ₽</div>
                        <div class="product-actions">
                            <button class="product-action-btn" onclick="event.stopPropagation(); viewProduct(${product.id})">Открыть</button>
                            ${canMessageUser ? `
                                <button class="product-action-btn product-chat-btn" onclick="event.stopPropagation(); startChat(${product.id})">Написать</button>
                            ` : ''}
                        </div>
                    </div>
                </div>
            `).join('') : '<div class="no-data">Нет активных объявлений</div>'}
        </div>
    `;
}

function getStars(rating) {
    const roundedRating = Number(rating || 0);
    const fullStars = Math.floor(roundedRating);
    const hasHalf = roundedRating % 1 >= 0.5;
    let stars = '';

    for (let index = 0; index < fullStars; index += 1) {
        stars += '★';
    }

    if (hasHalf) {
        stars += 'Ѕ';
    }

    for (let index = 0; index < 5 - fullStars - (hasHalf ? 1 : 0); index += 1) {
        stars += '☆';
    }

    return stars;
}

function viewProduct(productId) {
    window.location.href = `shop.html?product=${productId}`;
}

function startChat(productId) {
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    const fallbackProductId = targetUser?.activeAdvertisements?.[0]?.id || null;
    const resolvedProductId = productId || targetProductId || fallbackProductId;

    if (!resolvedProductId) {
        showError('Нельзя начать чат без объявления');
        return;
    }

    window.location.href = `chat.html?product=${resolvedProductId}&participant=${targetUser.id}`;
}

function showLoading() {
    const container = document.getElementById('profileContent');
    if (container) {
        container.innerHTML = '<div class="loading-spinner">Загрузка профиля...</div>';
    }
}

function hideLoading() {}
