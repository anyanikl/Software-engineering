let listings = [];
let currentUser = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    const role = currentUser?.role?.toLowerCase();
    if (!currentUser || (role !== 'moderator' && role !== 'admin')) {
        alert('Доступ запрещен');
        window.location.href = 'index.html';
        return;
    }

    await loadListings();
};

async function loadListings() {
    showLoading();

    try {
        const response = await fetch(API.getModerationPendingUrl(), {
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(await parseApiError(response, 'Не удалось загрузить объявления на модерации'));
        }

        listings = await response.json();
        updateStats();
        displayPendingListings();
    } catch (error) {
        showError(error.message || 'Не удалось загрузить очередь модерации');
    } finally {
        hideLoading();
    }
}

function updateStats() {
    const statsContainer = document.getElementById('stats');
    if (!statsContainer) return;

    statsContainer.innerHTML = `
        <div class="stat-card pending">
            <h3>Ожидают проверки</h3>
            <div class="stat-number">${listings.length}</div>
        </div>
        <div class="stat-card">
            <h3>С фото</h3>
            <div class="stat-number">${listings.filter(item => item.imageUrls?.length).length}</div>
        </div>
        <div class="stat-card">
            <h3>Без фото</h3>
            <div class="stat-number">${listings.filter(item => !item.imageUrls?.length).length}</div>
        </div>
    `;
}

function displayPendingListings() {
    const container = document.getElementById('pendingListings');
    if (!container) return;

    if (!listings.length) {
        container.innerHTML = '<div class="no-pending">Нет объявлений, ожидающих проверки</div>';
        return;
    }

    container.innerHTML = listings.map(listing => `
        <div class="listing-item" data-id="${listing.id}">
            <div class="listing-header">
                <span class="listing-title">${escapeHtml(listing.title)}</span>
                <span class="listing-seller">${escapeHtml(listing.sellerName)}</span>
            </div>
            <div class="listing-content">
                <img src="${escapeHtml(listing.imageUrls?.[0] || 'https://via.placeholder.com/150x150?text=Нет+фото')}"
                     alt="${escapeHtml(listing.title)}"
                     class="listing-image"
                     onerror="this.src='https://via.placeholder.com/150x150?text=Ошибка'">
                <div class="listing-details">
                    <div class="detail-row">
                        <span class="detail-label">Цена:</span>
                        <span class="detail-value">${Number(listing.price || 0).toLocaleString()} ₽</span>
                    </div>
                    <div class="detail-row">
                        <span class="detail-label">Местоположение:</span>
                        <span class="detail-value">${escapeHtml(listing.location || 'Не указано')}</span>
                    </div>
                    <div class="listing-description">
                        <strong>Описание:</strong><br>
                        ${escapeHtml(listing.description || 'Нет описания')}
                    </div>
                </div>
            </div>
            <div class="moderation-actions">
                <input type="text" id="comment-${listing.id}" class="comment-input" placeholder="Комментарий для пользователя">
                <button class="approve-btn" onclick="moderateListing(${listing.id}, 'approved')">Одобрить</button>
                <button class="revision-btn" onclick="moderateListing(${listing.id}, 'revision')">На доработку</button>
                <button class="reject-btn" onclick="moderateListing(${listing.id}, 'rejected')">Отклонить</button>
            </div>
        </div>
    `).join('');
}

async function moderateListing(listingId, newStatus) {
    const commentInput = document.getElementById(`comment-${listingId}`);
    const comment = commentInput ? commentInput.value.trim() : '';

    if ((newStatus === 'revision' || newStatus === 'rejected') && !comment) {
        showError('Для этого решения нужен комментарий');
        commentInput?.focus();
        return;
    }

    const url = newStatus === 'approved'
        ? API.approveModerationUrl(listingId)
        : newStatus === 'revision'
            ? API.revisionModerationUrl(listingId)
            : API.rejectModerationUrl(listingId);

    try {
        await requestNoContent(url, {
            method: 'POST',
            body: JSON.stringify({ comment: comment || null }),
            fallbackMessage: 'Не удалось выполнить модерацию'
        });

        await loadListings();
        showSuccess(getStatusMessage(newStatus));
    } catch (error) {
        showError(error.message || 'Не удалось выполнить модерацию');
    }
}

function getStatusMessage(status) {
    switch (status) {
        case 'approved':
            return 'Объявление одобрено';
        case 'revision':
            return 'Объявление отправлено на доработку';
        case 'rejected':
            return 'Объявление отклонено';
        default:
            return 'Действие выполнено';
    }
}

function showLoading() {
    const container = document.getElementById('pendingListings');
    if (container) {
        container.innerHTML = '<div class="no-pending">Загрузка...</div>';
    }
}

function hideLoading() {}
