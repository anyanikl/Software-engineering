let listings = [];
let deals = [];
let currentUser = null;
let currentFilter = 'all';

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    setupEventListeners();
    await refreshPageData();
};

async function refreshPageData() {
    await Promise.all([loadListings(), loadDeals()]);
    displayListings();
    checkForNewOrders();
}

async function loadListings() {
    const response = await fetch(API.getMyAdvertisementsUrl(), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить объявления'));
    }

    listings = await response.json();
}

async function loadDeals() {
    const response = await fetch(API.getSellerOrdersUrl(), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить заказы'));
    }

    deals = await response.json();
}

function setupEventListeners() {
    document.querySelectorAll('.status-tab').forEach(tab => {
        tab.addEventListener('click', function() {
            const status = this.dataset.status;
            filterListings(status);
        });
    });
}

function filterListings(status) {
    currentFilter = status;

    document.querySelectorAll('.status-tab').forEach(tab => {
        tab.classList.toggle('active', tab.dataset.status === status);
    });

    displayListings();
}

function displayListings() {
    const container = document.getElementById('listingsContainer');
    if (!container) return;

    let filteredListings = [...listings];
    if (currentFilter !== 'all') {
        filteredListings = filteredListings.filter(item => item.status === currentFilter);
    }

    filteredListings.sort((left, right) => right.id - left.id);

    if (filteredListings.length === 0) {
        container.innerHTML = getEmptyStateHTML();
        return;
    }

    container.innerHTML = filteredListings.map(getListingCardHTML).join('');
}

function getEmptyStateHTML() {
    const messages = {
        all: 'У вас пока нет объявлений',
        pending: 'Нет объявлений на модерации',
        approved: 'Нет активных объявлений',
        rejected: 'Нет отклоненных объявлений',
        revision: 'Нет объявлений на доработке',
        archived: 'Нет объявлений в архиве'
    };

    return `
        <div class="no-listings">
            <p>${messages[currentFilter] || 'Нет объявлений'}</p>
            <a href="create-listing.html">+ Создать объявление</a>
        </div>
    `;
}

function getListingCardHTML(listing) {
    const statusConfig = getStatusConfig(listing.status);
    const listingOrders = deals.filter(order => order.advertisementId === listing.id);
    const hasNewOrders = listingOrders.some(order => order.status === 'pending');
    const mainImageUrl = listing.mainImageUrl || 'https://via.placeholder.com/400x200?text=Нет+фото';

    return `
        <div class="listing-card" data-id="${listing.id}">
            ${hasNewOrders ? '<div class="order-notification">Новый заказ</div>' : ''}
            <img src="${escapeHtml(mainImageUrl)}"
                 alt="${escapeHtml(listing.title)}"
                 class="listing-image"
                 onerror="this.src='https://via.placeholder.com/400x200?text=Ошибка'">
            <div class="listing-info">
                <div class="listing-header">
                    <div class="listing-name">${escapeHtml(listing.title)}</div>
                </div>
                <div class="listing-description">${escapeHtml(listing.description || 'Нет описания')}</div>
                <div class="listing-details">
                    <span class="listing-price">${Number(listing.price || 0).toLocaleString()} ₽</span>
                    <span class="listing-location">${escapeHtml(listing.location || 'Не указано')}</span>
                </div>
                <div class="listing-meta">
                    <span class="status-badge ${statusConfig.class}">${statusConfig.text}</span>
                </div>

                ${listing.moderatorComment ? `
                    <div class="moderator-comment">
                        <strong>Комментарий модератора:</strong>
                        ${escapeHtml(listing.moderatorComment)}
                    </div>
                ` : ''}

                ${listingOrders.length > 0 ? getOrdersSectionHTML(listingOrders, listing.id, listing.title) : ''}

                <div class="listing-actions">
                    ${getActionButtonsHTML(listing)}
                </div>
            </div>
        </div>
    `;
}

function getStatusConfig(status) {
    const configs = {
        pending: { text: 'На модерации', class: 'status-pending' },
        approved: { text: 'Активно', class: 'status-approved' },
        rejected: { text: 'Отклонено', class: 'status-rejected' },
        revision: { text: 'На доработке', class: 'status-revision' },
        archived: { text: 'В архиве', class: 'status-archived' }
    };

    return configs[status] || { text: status || 'Неизвестно', class: 'status-pending' };
}

function getOrdersSectionHTML(orders, productId, productName) {
    return `
        <div class="orders-section">
            <h4>Заказы (${orders.length})</h4>
            ${orders.map(order => `
                <div class="order-item">
                    <div class="order-buyer">${escapeHtml(order.buyerName || 'Покупатель')}</div>
                    <div class="order-date">${formatDate(order.createdAt)}</div>
                    <div class="order-actions">
                        ${order.status === 'pending' ? `
                            <button class="complete-order-btn" onclick="completeOrder(${order.id})">
                                Завершить сделку
                            </button>
                        ` : order.status === 'completed' ? `
                            <span style="color: #27ae60; font-size: 12px;">Завершен</span>
                        ` : `
                            <span style="color: #7f8c8d; font-size: 12px;">${escapeHtml(order.status)}</span>
                        `}
                        <button class="chat-with-buyer-btn" onclick="chatWithBuyer(${order.buyerId}, ${productId})">
                            Чат
                        </button>
                    </div>
                </div>
            `).join('')}
        </div>
    `;
}

function getActionButtonsHTML(listing) {
    if (listing.status !== 'archived') {
        return `
            <button class="edit-btn" onclick="editListing(${listing.id})">Редактировать</button>
            <button class="archive-btn" onclick="archiveListing(${listing.id})">В архив</button>
            <button class="delete-btn" onclick="deleteListing(${listing.id})">Удалить</button>
        `;
    }

    return `
        <button class="edit-btn" onclick="restoreListing(${listing.id})">Из архива</button>
        <button class="delete-btn" onclick="deleteListing(${listing.id})">Удалить</button>
    `;
}

function editListing(listingId) {
    window.location.href = `edit-listing.html?id=${listingId}`;
}

async function archiveListing(listingId) {
    if (!confirm('Переместить объявление в архив?')) {
        return;
    }

    try {
        await requestNoContent(API.archiveAdvertisementUrl(listingId), {
            method: 'POST',
            fallbackMessage: 'Не удалось архивировать объявление'
        });

        await refreshPageData();
        showSuccess('Объявление перемещено в архив');
    } catch (error) {
        showError(error.message || 'Не удалось архивировать объявление');
    }
}

async function restoreListing(listingId) {
    if (!confirm('Восстановить объявление из архива? Оно снова уйдет на модерацию.')) {
        return;
    }

    try {
        await requestNoContent(API.restoreAdvertisementUrl(listingId), {
            method: 'POST',
            fallbackMessage: 'Не удалось восстановить объявление'
        });

        await refreshPageData();
        showSuccess('Объявление восстановлено и отправлено на модерацию');
    } catch (error) {
        showError(error.message || 'Не удалось восстановить объявление');
    }
}

async function deleteListing(listingId) {
    if (!confirm('Удалить объявление? Это действие нельзя отменить.')) {
        return;
    }

    try {
        await requestNoContent(API.deleteAdvertisementUrl(listingId), {
            method: 'DELETE',
            fallbackMessage: 'Не удалось удалить объявление'
        });

        await refreshPageData();
        showSuccess('Объявление удалено');
    } catch (error) {
        showError(error.message || 'Не удалось удалить объявление');
    }
}

async function completeOrder(orderId) {
    try {
        await requestNoContent(API.completeOrderUrl(orderId), {
            method: 'POST',
            fallbackMessage: 'Не удалось завершить сделку'
        });

        await refreshPageData();
        showSuccess('Сделка завершена');
    } catch (error) {
        showError(error.message || 'Не удалось завершить сделку');
    }
}

function chatWithBuyer(buyerId, productId) {
    window.location.href = `chat.html?product=${productId}&participant=${buyerId}`;
}

function checkForNewOrders() {
    const myListingsIds = listings.map(item => item.id);
    const pendingOrders = deals.filter(order =>
        myListingsIds.includes(order.advertisementId) && order.status === 'pending'
    );

    if (pendingOrders.length > 0) {
        showNotification(`У вас ${pendingOrders.length} новых заказов`);
    }
}

function showNotification(message) {
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;top:20px;right:20px;background:#3498db;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
}
