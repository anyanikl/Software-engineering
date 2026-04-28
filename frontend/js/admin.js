let users = [];
let pendingListings = [];
let currentUser = null;
let currentStats = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser || currentUser.role?.toLowerCase() !== 'admin') {
        alert('Доступ запрещен');
        window.location.href = 'index.html';
        return;
    }

    setupEventListeners();
    await refreshAdminData();
};

async function refreshAdminData() {
    await Promise.all([loadUsers(), loadStats(), loadPendingModeration()]);
    displayUsers();
    updateStatsDisplay(currentStats || {});
    displayPendingModeration();
    displayStats();
    displayReports();
}

async function loadUsers() {
    const searchText = document.getElementById('userSearch')?.value?.trim() || '';
    const url = searchText
        ? `${API.getAdminUsersUrl()}?search=${encodeURIComponent(searchText)}`
        : API.getAdminUsersUrl();

    const response = await fetch(url, {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить пользователей'));
    }

    users = await response.json();
}

async function loadStats() {
    const response = await fetch(API.getAdminStatsUrl(), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить статистику'));
    }

    currentStats = await response.json();
}

async function loadPendingModeration() {
    const response = await fetch(API.getModerationPendingUrl(), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить очередь модерации'));
    }

    pendingListings = await response.json();
}

function displayUsers() {
    const tbody = document.getElementById('usersList');
    if (!tbody) return;

    if (!users.length) {
        tbody.innerHTML = '<tr><td colspan="9" style="text-align:center;">Пользователи не найдены</td></tr>';
        return;
    }

    tbody.innerHTML = users.map(user => `
        <tr>
            <td>
                <img src="${escapeHtml(user.avatarUrl || 'https://via.placeholder.com/40')}"
                     class="user-avatar"
                     onerror="this.src='https://via.placeholder.com/40'">
            </td>
            <td>${escapeHtml(user.fullName)}</td>
            <td>${escapeHtml(user.email)}</td>
            <td>${escapeHtml(user.role)}</td>
            <td>${formatDate(user.createdAt)}</td>
            <td>${Number(user.dealsCount || 0)}</td>
            <td>${Number(user.rating || 0).toFixed(1)}</td>
            <td>
                <span class="status-badge ${user.isBlocked ? 'status-blocked' : 'status-active'}">
                    ${user.isBlocked ? 'Заблокирован' : 'Активен'}
                </span>
            </td>
            <td>
                ${user.role?.toLowerCase() !== 'admin' ? (
                    user.isBlocked
                        ? `<button class="action-btn unblock-btn" onclick="toggleBlock(${user.id}, 'unblock')">Разблокировать</button>`
                        : `<button class="action-btn block-btn" onclick="toggleBlock(${user.id}, 'block')">Заблокировать</button>`
                ) : '—'}
            </td>
        </tr>
    `).join('');
}

async function toggleBlock(userId, action) {
    try {
        await requestNoContent(action === 'block' ? API.blockUserUrl(userId) : API.unblockUserUrl(userId), {
            method: 'POST',
            fallbackMessage: `Не удалось ${action === 'block' ? 'заблокировать' : 'разблокировать'} пользователя`
        });

        await refreshAdminData();
        showSuccess(action === 'block' ? 'Пользователь заблокирован' : 'Пользователь разблокирован');
    } catch (error) {
        showError(error.message || 'Не удалось изменить статус пользователя');
    }
}

function filterUsers() {
    loadUsers()
        .then(displayUsers)
        .catch(error => showError(error.message || 'Не удалось загрузить пользователей'));
}

function switchTab(tabName) {
    document.querySelectorAll('.admin-tab').forEach(tab => tab.classList.toggle('active', tab.dataset.tab === tabName));

    document.getElementById('usersTab').style.display = tabName === 'users' ? 'block' : 'none';
    document.getElementById('moderationTab').style.display = tabName === 'moderation' ? 'block' : 'none';
    document.getElementById('statsTab').style.display = tabName === 'stats' ? 'block' : 'none';
    document.getElementById('reportsTab').style.display = tabName === 'reports' ? 'block' : 'none';

    if (tabName === 'stats') {
        displayStats();
    }

    if (tabName === 'reports') {
        displayReports();
    }
}

function updateStatsDisplay(stats) {
    const totalUsers = document.getElementById('totalUsers');
    const totalListings = document.getElementById('totalListings');
    const totalDeals = document.getElementById('totalDeals');
    const blockedUsers = document.getElementById('blockedUsers');

    if (totalUsers) totalUsers.textContent = stats.totalUsers || 0;
    if (totalListings) totalListings.textContent = stats.activeAdvertisements || 0;
    if (totalDeals) totalDeals.textContent = stats.completedOrders || 0;
    if (blockedUsers) blockedUsers.textContent = stats.blockedUsers || 0;
}

function displayPendingModeration() {
    const container = document.getElementById('pendingModerationList');
    if (!container) return;

    if (!pendingListings.length) {
        container.innerHTML = '<div style="padding:20px;text-align:center;color:#7f8c8d;">Нет объявлений на модерации</div>';
        return;
    }

    container.innerHTML = pendingListings.map(listing => {
        const imageUrl = listing.imageUrls?.[0] || '';
        const imageHtml = imageUrl
            ? `<img src="${escapeHtml(imageUrl)}" alt="${escapeHtml(listing.title)}" style="width:120px;height:90px;object-fit:cover;border-radius:6px;">`
            : '<div style="width:120px;height:90px;background:#f1f2f6;border-radius:6px;display:flex;align-items:center;justify-content:center;color:#7f8c8d;">Нет фото</div>';

        return `
            <div style="display:grid;grid-template-columns:120px 1fr;gap:16px;padding:16px;border-bottom:1px solid #ecf0f1;">
                ${imageHtml}
                <div>
                    <h4 style="margin:0 0 8px;">${escapeHtml(listing.title)}</h4>
                    <p style="margin:0 0 8px;color:#566573;">${escapeHtml(listing.description || '')}</p>
                    <div style="margin-bottom:10px;">
                        <strong>${Number(listing.price || 0).toLocaleString()} ₽</strong>
                        <span style="margin-left:12px;color:#7f8c8d;">${escapeHtml(listing.location || '')}</span>
                        <span style="margin-left:12px;color:#7f8c8d;">${escapeHtml(listing.sellerName || '')}</span>
                    </div>
                    <input id="moderation-comment-${listing.id}" type="text" placeholder="Комментарий" style="width:100%;max-width:520px;margin-bottom:10px;padding:8px;">
                    <div>
                        <button class="action-btn unblock-btn" onclick="moderateFromAdmin(${listing.id}, 'approved')">Одобрить</button>
                        <button class="action-btn block-btn" onclick="moderateFromAdmin(${listing.id}, 'rejected')">Отклонить</button>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

async function moderateFromAdmin(listingId, decision) {
    const commentInput = document.getElementById(`moderation-comment-${listingId}`);
    const comment = commentInput?.value.trim() || null;

    if (decision === 'rejected' && !comment) {
        alert('Для отклонения нужен комментарий');
        commentInput?.focus();
        return;
    }

    const url = decision === 'approved'
        ? API.approveModerationUrl(listingId)
        : API.rejectModerationUrl(listingId);

    try {
        await requestNoContent(url, {
            method: 'POST',
            body: JSON.stringify({ comment }),
            fallbackMessage: 'Не удалось выполнить модерацию'
        });

        await refreshAdminData();
        showSuccess(decision === 'approved' ? 'Объявление одобрено' : 'Объявление отклонено');
    } catch (error) {
        showError(error.message || 'Не удалось выполнить модерацию');
    }
}

function displayStats() {
    const container = document.getElementById('categoryStats');
    if (!container || !currentStats) return;

    container.innerHTML = `
        <div style="padding:20px;">
            <div style="margin:12px 0;padding:12px;background:#f8f9fa;border-radius:8px;">
                <strong>Очередь модерации</strong>
                <div style="margin-top:6px;">${pendingListings.length} объявлений ожидают проверки</div>
            </div>
            <div style="margin:12px 0;padding:12px;background:#f8f9fa;border-radius:8px;">
                <strong>Средний рейтинг пользователей</strong>
                <div style="margin-top:6px;">${getAverageUserRating().toFixed(1)}</div>
            </div>
        </div>
    `;
}

function displayReports() {
    const activityBody = document.getElementById('activityReport');
    const categoriesBody = document.getElementById('categoriesReport');

    if (activityBody) {
        const nonAdminUsers = users.filter(user => user.role?.toLowerCase() !== 'admin');
        activityBody.innerHTML = nonAdminUsers.length
            ? nonAdminUsers.map(user => `
                <tr>
                    <td>${escapeHtml(user.fullName)}</td>
                    <td>${Number(user.dealsCount || 0)}</td>
                    <td>${Number(user.rating || 0).toFixed(1)}</td>
                    <td>${user.isBlocked ? 'Заблокирован' : 'Активен'}</td>
                    <td>${escapeHtml(user.email)}</td>
                </tr>
            `).join('')
            : '<tr><td colspan="5" style="text-align:center;">Нет данных</td></tr>';
    }

    if (categoriesBody) {
        categoriesBody.innerHTML = `
            <tr>
                <td colspan="4" style="text-align:center;">
                    Для полного отчета используйте экспорт JSON/CSV из backend.
                </td>
            </tr>
        `;
    }
}

function getAverageUserRating() {
    const ratedUsers = users.filter(user => Number(user.rating || 0) > 0);
    if (!ratedUsers.length) {
        return 0;
    }

    return ratedUsers.reduce((sum, user) => sum + Number(user.rating || 0), 0) / ratedUsers.length;
}

async function exportReport() {
    try {
        const data = await requestJson(API.exportJsonUrl(), {
            method: 'GET',
            useCsrf: false,
            fallbackMessage: 'Не удалось экспортировать отчет'
        });

        const dataStr = JSON.stringify(data, null, 2);
        const dataUri = `data:application/json;charset=utf-8,${encodeURIComponent(dataStr)}`;
        const exportFileDefaultName = `report-${new Date().toISOString().slice(0, 10)}.json`;
        const linkElement = document.createElement('a');
        linkElement.setAttribute('href', dataUri);
        linkElement.setAttribute('download', exportFileDefaultName);
        linkElement.click();
        showSuccess('Отчет экспортирован');
    } catch (error) {
        showError(error.message || 'Не удалось экспортировать отчет');
    }
}

function setupEventListeners() {
    const searchInput = document.getElementById('userSearch');
    if (searchInput) {
        searchInput.addEventListener('input', filterUsers);
    }
}
