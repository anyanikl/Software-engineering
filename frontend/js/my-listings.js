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
    
    await loadListings();
    await loadDeals();
    setupEventListeners();
    checkForNewOrders();
};

// Преобразование MyAdvertisementDto в формат фронта
function convertMyListingToLocalFormat(apiListing) {
    return {
        id: apiListing.id,
        name: apiListing.title,
        description: apiListing.description || 'Нет описания',
        price: apiListing.price,
        location: apiListing.location || '',
        status: apiListing.status,
        moderatorComment: apiListing.moderatorComment || null,
        ordersCount: apiListing.ordersCount || 0,
        image: apiListing.mainImageUrl,
    };
}

async function loadListings() {
    try {
        const response = await fetch(API.getMyAdvertisementsUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            // MyAdvertisementDto[]
            listings = data.map(item => convertMyListingToLocalFormat(item));
            localStorage.setItem('my-listings', JSON.stringify(listings));
            displayListings();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки объявлений из API:', error);
    }
    
    // Fallback на localStorage
    const allListings = JSON.parse(localStorage.getItem('listings')) || [];
    listings = allListings.filter(l => 
        l.sellerId === currentUser?.id || 
        l.sellerEmail === currentUser?.email
    ).map(l => ({
        id: l.id,
        name: l.title || l.name,
        description: l.description || 'Нет описания',
        price: l.price,
        location: l.location || '',
        status: l.status,
        moderatorComment: l.moderatorComment || null,
        ordersCount: l.ordersCount || 0,
        image: l.mainImageUrl || l.image,
    }));
    displayListings();
}

async function loadDeals() {
    try {
        const response = await fetch(API.getSellerOrdersUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            deals = await response.json();
            localStorage.setItem('my-deals', JSON.stringify(deals));
            displayListings();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки заказов из API:', error);
    }
    
    // Fallback на localStorage
    deals = JSON.parse(localStorage.getItem('deals')) || [];
    displayListings();
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
        tab.classList.remove('active');
        if (tab.dataset.status === status) {
            tab.classList.add('active');
        }
    });
    
    displayListings();
}

function displayListings() {
    const container = document.getElementById('listingsContainer');
    if (!container) return;
    
    let filteredListings = [...listings];
    if (currentFilter !== 'all') {
        filteredListings = filteredListings.filter(l => l.status === currentFilter);
    }
    
    // Сортировка: сначала новые (по id, так как createdAt нет)
    filteredListings.sort((a, b) => b.id - a.id);
    
    if (filteredListings.length === 0) {
        container.innerHTML = getEmptyStateHTML();
        return;
    }
    
    container.innerHTML = filteredListings.map(listing => getListingCardHTML(listing)).join('');
}

function getEmptyStateHTML() {
    const messages = {
        'all': 'У вас пока нет объявлений',
        'pending': 'Нет объявлений на модерации',
        'approved': 'Нет активных объявлений',
        'rejected': 'Нет отклоненных объявлений',
        'revision': 'Нет объявлений на доработке',
        'archived': 'Нет объявлений в архиве'
    };
    
    return `
        <div class="no-listings">
            <p>😕 ${messages[currentFilter] || 'Нет объявлений'}</p>
            <a href="create-listing.html">+ Создать объявление</a>
        </div>
    `;
}

function getListingCardHTML(listing) {
    const statusConfig = getStatusConfig(listing.status);
    const listingOrders = deals.filter(d => d.advertisementId === listing.id);
    const hasNewOrders = listingOrders.some(d => d.status === 'pending');
    
    return `
        <div class="listing-card" data-id="${listing.id}">
            ${hasNewOrders ? '<div class="order-notification">🆕 Новый заказ!</div>' : ''}
            <img src="${listing.image || 'https://via.placeholder.com/400x200?text=Нет+фото'}" 
                 alt="${escapeHtml(listing.name)}" 
                 class="listing-image"
                 onerror="this.src='https://via.placeholder.com/400x200?text=Ошибка'">
            <div class="listing-info">
                <div class="listing-header">
                    <div class="listing-name">${escapeHtml(listing.name)}</div>
                </div>
                <div class="listing-description">${escapeHtml(listing.description || 'Нет описания')}</div>
                <div class="listing-details">
                    <span class="listing-price">${(listing.price || 0).toLocaleString()} ₽</span>
                    <span class="listing-location">📍 ${escapeHtml(listing.location || 'Не указано')}</span>
                </div>
                <div class="listing-meta">
                    <span class="status-badge ${statusConfig.class}">${statusConfig.text}</span>
                </div>
                
                ${listing.moderatorComment ? `
                    <div class="moderator-comment">
                        <strong>💬 Комментарий модератора:</strong>
                        ${escapeHtml(listing.moderatorComment)}
                    </div>
                ` : ''}
                
                ${listingOrders.length > 0 ? getOrdersSectionHTML(listingOrders, listing.id, listing.name) : ''}
                
                <div class="listing-actions">
                    ${getActionButtonsHTML(listing)}
                </div>
            </div>
        </div>
    `;
}

function getStatusConfig(status) {
    const configs = {
        'pending': { text: '⏳ На модерации', class: 'status-pending' },
        'approved': { text: '✅ Активно', class: 'status-approved' },
        'rejected': { text: '❌ Отклонено', class: 'status-rejected' },
        'revision': { text: '📝 На доработке', class: 'status-revision' },
        'archived': { text: '📦 В архиве', class: 'status-archived' }
    };
    return configs[status] || { text: 'Неизвестно', class: 'status-pending' };
}

function getOrdersSectionHTML(orders, productId, productName) {
    return `
        <div class="orders-section">
            <h4>📦 Заказы (${orders.length})</h4>
            ${orders.map(order => `
                <div class="order-item">
                    <div class="order-buyer">👤 ${escapeHtml(order.buyerName || 'Покупатель')}</div>
                    <div class="order-date">${formatDate(order.createdAt)}</div>
                    <div class="order-actions">
                        ${order.status === 'pending' ? `
                            <button class="complete-order-btn" onclick="completeOrder(${order.id})">
                                ✅ Завершить сделку
                            </button>
                        ` : order.status === 'completed' ? `
                            <span style="color: #27ae60; font-size: 12px;">✓ Завершён</span>
                        ` : ''}
                        <button class="chat-with-buyer-btn" onclick="chatWithBuyer(${order.buyerId}, ${productId}, '${escapeHtml(productName).replace(/'/g, "\\'")}')">
                            💬 Чат
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
            <button class="edit-btn" onclick="editListing(${listing.id})">✏️ Редактировать</button>
            <button class="archive-btn" onclick="archiveListing(${listing.id})">📦 В архив</button>
            <button class="delete-btn" onclick="deleteListing(${listing.id})">🗑️ Удалить</button>
        `;
    } else {
        return `
            <button class="edit-btn" onclick="unarchiveListing(${listing.id})">📦 Из архива</button>
            <button class="delete-btn" onclick="deleteListing(${listing.id})">🗑️ Удалить</button>
        `;
    }
}

function editListing(listingId) {
    window.location.href = `edit-listing.html?id=${listingId}`;
}

async function archiveListing(listingId) {
    if (!confirm('Переместить объявление в архив?')) return;
    
    try {
        const response = await fetch(API.archiveAdvertisementUrl(listingId), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        
        if (response.ok) {
            await loadListings();
            showSuccess('Объявление перемещено в архив');
            return;
        }
    } catch (error) {
        console.error('Ошибка архивации:', error);
    }
    
    // Fallback на localStorage
    const index = listings.findIndex(l => l.id === listingId);
    if (index !== -1) {
        listings[index].status = 'archived';
        saveListingsToLocal();
        displayListings();
        showSuccess('Объявление перемещено в архив');
    }
}

async function unarchiveListing(listingId) {
    if (!confirm('Восстановить объявление из архива? Оно отправится на модерацию.')) return;
    
    try {
        const response = await fetch(API.updateAdvertisementUrl(listingId), {
            method: 'PUT',
            headers: getSecureHeaders(true),
            body: JSON.stringify({ status: 'pending' }),
            credentials: 'include'
        });
        
        if (response.ok) {
            await loadListings();
            showSuccess('Объявление восстановлено и отправлено на модерацию');
            return;
        }
    } catch (error) {
        console.error('Ошибка восстановления:', error);
    }
    
    // Fallback на localStorage
    const index = listings.findIndex(l => l.id === listingId);
    if (index !== -1) {
        listings[index].status = 'pending';
        listings[index].moderatorComment = null;
        saveListingsToLocal();
        displayListings();
        showSuccess('Объявление восстановлено и отправлено на модерацию');
    }
}

async function deleteListing(listingId) {
    if (!confirm('Вы уверены, что хотите удалить это объявление? Это действие нельзя отменить.')) return;
    
    try {
        const response = await fetch(API.deleteAdvertisementUrl(listingId), {
            method: 'DELETE',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        
        if (response.ok) {
            await loadListings();
            showSuccess('Объявление удалено');
            return;
        }
    } catch (error) {
        console.error('Ошибка удаления:', error);
    }
    
    // Fallback на localStorage
    listings = listings.filter(l => l.id !== listingId);
    saveListingsToLocal();
    displayListings();
    showSuccess('Объявление удалено');
}

function saveListingsToLocal() {
    const allListings = JSON.parse(localStorage.getItem('listings')) || [];
    const otherListings = allListings.filter(l => l.sellerId !== currentUser?.id);
    const updatedListings = [...otherListings, ...listings];
    localStorage.setItem('listings', JSON.stringify(updatedListings));
}

async function completeOrder(orderId) {
    try {
        const response = await fetch(API.completeOrderUrl(orderId), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        
        if (response.ok) {
            await loadDeals();
            displayListings();
            showSuccess('Сделка завершена! Покупатель может оставить отзыв.');
            return;
        }
    } catch (error) {
        console.error('Ошибка завершения заказа:', error);
    }
    
    // Fallback на localStorage
    const order = deals.find(d => d.id === orderId);
    if (order) {
        order.status = 'completed';
        order.completedAt = new Date().toISOString();
        localStorage.setItem('deals', JSON.stringify(deals));
        displayListings();
        showSuccess('Сделка завершена! Покупатель может оставить отзыв.');
    }
}

function chatWithBuyer(buyerId, productId, productName) {
    window.location.href = `chat.html?product=${productId}&seller=${buyerId}`;
}

function checkForNewOrders() {
    const myListingsIds = listings.map(l => l.id);
    const newOrders = deals.filter(deal => 
        myListingsIds.includes(deal.advertisementId) && 
        deal.status === 'pending'
    );
    
    if (newOrders.length > 0) {
        showNotification(`У вас ${newOrders.length} новый(х) заказ(ов)!`);
    }
}

function showNotification(message) {
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;top:20px;right:20px;background:#3498db;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.innerHTML = `<strong>🔔 Новый заказ!</strong><br>${message}`;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 5000);
}

function showSuccess(message) {
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#27ae60;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = '✅ ' + message;
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