let listings = [];
let currentUser = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    if (!currentUser || (currentUser.role !== 'moderator' && currentUser.role !== 'admin')) {
        alert('Доступ запрещен. Требуются права модератора.');
        window.location.href = 'index.html';
        return;
    }
    
    await loadListings();
};

async function loadListings() {
    showLoading();
    
    try {
        // GET /api/moderation/pending
        const response = await fetch(`${API_BASE_URL}/api/moderation/pending`, {
            credentials: 'include'
        });
        
        if (response.ok) {
            listings = await response.json();
            updateStats();
            displayPendingListings();
            hideLoading();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки объявлений из API:', error);
    }
    
    // Fallback на localStorage
    const allListings = JSON.parse(localStorage.getItem('listings')) || [];
    listings = allListings.filter(l => l.status === 'pending');
    updateStats();
    displayPendingListings();
    hideLoading();
}

function updateStats() {
    const allListings = JSON.parse(localStorage.getItem('listings')) || [];
    const stats = {
        pending: allListings.filter(l => l.status === 'pending').length,
        approved: allListings.filter(l => l.status === 'approved').length,
        rejected: allListings.filter(l => l.status === 'rejected').length,
        revision: allListings.filter(l => l.status === 'revision').length,
        total: allListings.length
    };
    
    const statsContainer = document.getElementById('stats');
    if (statsContainer) {
        statsContainer.innerHTML = `
            <div class="stat-card pending">
                <h3>⏳ Ожидают проверки</h3>
                <div class="stat-number">${stats.pending}</div>
            </div>
            <div class="stat-card approved">
                <h3>✅ Одобрено</h3>
                <div class="stat-number">${stats.approved}</div>
            </div>
            <div class="stat-card rejected">
                <h3>❌ Отклонено</h3>
                <div class="stat-number">${stats.rejected}</div>
            </div>
            <div class="stat-card revision">
                <h3>📝 На доработке</h3>
                <div class="stat-number">${stats.revision}</div>
            </div>
            <div class="stat-card">
                <h3>📊 Всего объявлений</h3>
                <div class="stat-number">${stats.total}</div>
            </div>
        `;
    }
}

function displayPendingListings() {
    const container = document.getElementById('pendingListings');
    if (!container) return;
    
    if (listings.length === 0) {
        container.innerHTML = '<div class="no-pending">✨ Нет объявлений, ожидающих проверки</div>';
        return;
    }
    
    // ModerationAdvertisementDto: id, title, description, price, location, sellerName, imageUrls
    container.innerHTML = listings.map(listing => `
        <div class="listing-item" data-id="${listing.id}">
            <div class="listing-header">
                <span class="listing-title">${escapeHtml(listing.title)}</span>
                <span class="listing-seller">👤 ${escapeHtml(listing.sellerName)}</span>
            </div>
            <div class="listing-content">
                <img src="${listing.imageUrls?.[0] || 'https://via.placeholder.com/150x150?text=Нет+фото'}" 
                     alt="${escapeHtml(listing.title)}" 
                     class="listing-image"
                     onerror="this.src='https://via.placeholder.com/150x150?text=Ошибка'">
                <div class="listing-details">
                    <div class="detail-row">
                        <span class="detail-label">Цена:</span>
                        <span class="detail-value">${(listing.price || 0).toLocaleString()} ₽</span>
                    </div>
                    <div class="detail-row">
                        <span class="detail-label">Местоположение:</span>
                        <span class="detail-value">📍 ${escapeHtml(listing.location || 'Не указано')}</span>
                    </div>
                    <div class="listing-description">
                        <strong>Описание:</strong><br>
                        ${escapeHtml(listing.description || 'Нет описания')}
                    </div>
                </div>
            </div>
            <div class="moderation-actions">
                <input type="text" id="comment-${listing.id}" class="comment-input" 
                       placeholder="Комментарий для пользователя (обязательно для отклонения/доработки)">
                <button class="approve-btn" onclick="moderateListing(${listing.id}, 'approved')">✅ Одобрить</button>
                <button class="revision-btn" onclick="moderateListing(${listing.id}, 'revision')">📝 На доработку</button>
                <button class="reject-btn" onclick="moderateListing(${listing.id}, 'rejected')">❌ Отклонить</button>
            </div>
        </div>
    `).join('');
}

async function moderateListing(listingId, newStatus) {
    const commentInput = document.getElementById(`comment-${listingId}`);
    const comment = commentInput ? commentInput.value.trim() : '';
    
    if ((newStatus === 'revision' || newStatus === 'rejected') && !comment) {
        showError(`Пожалуйста, укажите ${newStatus === 'revision' ? 'комментарий для доработки' : 'причину отклонения'}`);
        commentInput?.focus();
        return;
    }
    
    try {
        let url = '';
        
        switch(newStatus) {
            case 'approved':
                url = `${API_BASE_URL}/api/moderation/${listingId}/approve`;
                break;
            case 'revision':
                url = `${API_BASE_URL}/api/moderation/${listingId}/revision`;
                break;
            case 'rejected':
                url = `${API_BASE_URL}/api/moderation/${listingId}/reject`;
                break;
        }
        
        const response = await fetch(url, {
            method: 'POST',
            headers: getSecureHeaders(true),
            body: JSON.stringify({ comment: comment || null }),
            credentials: 'include'
        });
        
        if (response.ok || response.status === 204) {
            await loadListings();
            showSuccess(getStatusMessage(newStatus));
            return;
        }
    } catch (error) {
        console.error('Ошибка модерации:', error);
    }
    
    // Fallback на localStorage
    let allListings = JSON.parse(localStorage.getItem('listings')) || [];
    const index = allListings.findIndex(l => l.id === listingId);
    
    if (index !== -1) {
        allListings[index] = {
            ...allListings[index],
            status: newStatus,
            moderatorComment: comment || null,
            moderatedAt: new Date().toISOString(),
            moderatedBy: currentUser?.id
        };
        
        localStorage.setItem('listings', JSON.stringify(allListings));
        await loadListings();
        showSuccess(getStatusMessage(newStatus));
        createUserNotification(allListings[index], newStatus, comment);
    }
}

function getStatusMessage(status) {
    switch(status) {
        case 'approved': return '✅ Объявление одобрено';
        case 'revision': return '📝 Объявление отправлено на доработку';
        case 'rejected': return '❌ Объявление отклонено';
        default: return 'Действие выполнено';
    }
}

function createUserNotification(listing, status, comment) {
    let notifications = JSON.parse(localStorage.getItem('notifications')) || [];
    
    let message = '';
    let type = '';
    
    switch(status) {
        case 'approved':
            message = `✅ Ваше объявление "${listing.title}" одобрено и опубликовано!`;
            type = 'success';
            break;
        case 'revision':
            message = `📝 Ваше объявление "${listing.title}" отправлено на доработку. Комментарий: ${comment}`;
            type = 'warning';
            break;
        case 'rejected':
            message = `❌ Ваше объявление "${listing.title}" отклонено. Причина: ${comment}`;
            type = 'error';
            break;
    }
    
    const users = JSON.parse(localStorage.getItem('users')) || [];
    const user = users.find(u => u.email === listing.sellerEmail || u.id === listing.sellerId);
    
    if (user) {
        notifications.push({
            id: Date.now(),
            userId: user.id,
            type: type,
            title: 'Результат модерации',
            message: message,
            read: false,
            date: new Date().toISOString()
        });
        localStorage.setItem('notifications', JSON.stringify(notifications));
    }
}

function showLoading() {
    const container = document.getElementById('pendingListings');
    if (container) {
        container.innerHTML = '<div class="no-pending">⏳ Загрузка...</div>';
    }
}

function hideLoading() {}

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