let users = [];
let currentUser = null;
let currentFilter = 'all';

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    if (!currentUser || (currentUser.role !== 'admin' && currentUser.role !== 'Admin')) {
        alert('Доступ запрещен. Требуются права администратора.');
        window.location.href = 'index.html';
        return;
    }
    
    await loadUsers();
    await loadStats();
    setupEventListeners();
};

async function loadUsers() {
    const searchText = document.getElementById('userSearch')?.value || '';
    
    try {
        let url = API.getAdminUsersUrl();
        if (searchText) url += `?search=${encodeURIComponent(searchText)}`;
        
        const response = await fetch(url, {
            credentials: 'include'
        });
        
        if (response.ok) {
            // AdminStatsDto[] - список пользователей для админа
            users = await response.json();
            displayUsers();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки пользователей из API:', error);
    }
    
    // Fallback на localStorage
    const allUsers = JSON.parse(localStorage.getItem('users')) || [];
    users = allUsers.filter(u => searchText ? 
        (u.fullName || u.name || '').toLowerCase().includes(searchText.toLowerCase()) || 
        (u.email || '').toLowerCase().includes(searchText.toLowerCase()) : true
    );
    displayUsers();
}

async function loadStats() {
    try {
        const response = await fetch(API.getAdminStatsUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            // UserAdminDto: totalUsers, activeAdvertisements, completedOrders, blockedUsers
            const stats = await response.json();
            updateStatsDisplay(stats);
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки статистики из API:', error);
    }
    
    // Fallback на localStorage
    const allUsers = JSON.parse(localStorage.getItem('users')) || [];
    const listings = JSON.parse(localStorage.getItem('listings')) || [];
    const deals = JSON.parse(localStorage.getItem('deals')) || [];
    
    updateStatsDisplay({
        totalUsers: allUsers.length,
        activeAdvertisements: listings.filter(l => l.status === 'approved').length,
        completedOrders: deals.filter(d => d.status === 'completed').length,
        blockedUsers: allUsers.filter(u => u.isBlocked || u.status === 'blocked').length
    });
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

function displayUsers() {
    const tbody = document.getElementById('usersList');
    if (!tbody) return;
    
    if (users.length === 0) {
        tbody.innerHTML = '<tr><td colspan="9" style="text-align: center;">Пользователи не найдены</td></tr>';
        return;
    }
    
    tbody.innerHTML = users.map(user => `
        <tr>
            <td>
                <img src="${user.avatarUrl || user.avatar || 'https://via.placeholder.com/40'}" 
                     class="user-avatar" 
                     onerror="this.src='https://via.placeholder.com/40'">
            </td>
            <td>${escapeHtml(user.fullName || user.name)}</td>
            <td>${escapeHtml(user.email)}</td>
            <td>${user.role === 'admin' ? 'Администратор' : 'Пользователь'}</td>
            <td>${formatDate(user.createdAt || user.registered)}</td>
            <td>${user.dealsCount || user.deals || 0}</td>
            <td>${user.rating || 0}</td>
            <td>
                <span class="status-badge ${user.isBlocked || user.status === 'blocked' ? 'status-blocked' : 'status-active'}">
                    ${user.isBlocked || user.status === 'blocked' ? 'Заблокирован' : 'Активен'}
                </span>
            </td>
            <td>
                ${user.role !== 'admin' ? (
                    (user.isBlocked || user.status === 'blocked') ? 
                        `<button class="action-btn unblock-btn" onclick="toggleBlock('${user.id}', 'unblock')">Разблокировать</button>` : 
                        `<button class="action-btn block-btn" onclick="toggleBlock('${user.id}', 'block')">Заблокировать</button>`
                ) : '—'}
            </td>
        </tr>
    `).join('');
}

async function toggleBlock(userId, action) {
    try {
        const url = action === 'block' ? API.blockUserUrl(userId) : API.unblockUserUrl(userId);
        const response = await fetch(url, {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        
        if (response.ok) {
            await loadUsers();
            await loadStats();
            showSuccess(`Пользователь ${action === 'block' ? 'заблокирован' : 'разблокирован'}`);
            return;
        }
    } catch (error) {
        console.error('Ошибка API:', error);
    }
    
    // Fallback на localStorage
    const allUsers = JSON.parse(localStorage.getItem('users')) || [];
    const userIndex = allUsers.findIndex(u => u.id == userId);
    
    if (userIndex !== -1) {
        allUsers[userIndex].status = action === 'block' ? 'blocked' : 'active';
        allUsers[userIndex].isBlocked = action === 'block';
        localStorage.setItem('users', JSON.stringify(allUsers));
        await loadUsers();
        await loadStats();
        showSuccess(`Пользователь ${action === 'block' ? 'заблокирован' : 'разблокирован'}`);
    }
}

function filterUsers() {
    loadUsers();
}

function switchTab(tabName) {
    document.querySelectorAll('.admin-tab').forEach(t => t.classList.remove('active'));
    const activeTab = Array.from(document.querySelectorAll('.admin-tab')).find(t => t.textContent.toLowerCase().includes(tabName));
    if (activeTab) activeTab.classList.add('active');
    
    const usersTab = document.getElementById('usersTab');
    const statsTab = document.getElementById('statsTab');
    const reportsTab = document.getElementById('reportsTab');
    
    if (usersTab) usersTab.style.display = tabName === 'users' ? 'block' : 'none';
    if (statsTab) statsTab.style.display = tabName === 'stats' ? 'block' : 'none';
    if (reportsTab) reportsTab.style.display = tabName === 'reports' ? 'block' : 'none';
    
    if (tabName === 'stats') {
        displayStats();
    } else if (tabName === 'reports') {
        displayReports();
    }
}

function displayStats() {
    const listings = JSON.parse(localStorage.getItem('listings')) || [];
    const categories = {};
    
    listings.forEach(l => {
        if (l.category) {
            categories[l.category] = (categories[l.category] || 0) + 1;
        }
    });
    
    const container = document.getElementById('categoryStats');
    if (container) {
        if (Object.keys(categories).length === 0) {
            container.innerHTML = '<div style="padding: 20px; text-align: center; color: #7f8c8d;">Нет данных для отображения</div>';
        } else {
            container.innerHTML = '<div style="padding: 20px;">' + 
                Object.entries(categories).map(([cat, count]) => 
                    `<div style="margin: 15px 0; padding: 10px; background: #f8f9fa; border-radius: 8px;">
                        <strong>📚 ${escapeHtml(cat)}</strong>
                        <div style="margin-top: 5px;"><span style="font-size: 24px; font-weight: bold;">${count}</span> товаров</div>
                    </div>`
                ).join('') + '</div>';
        }
    }
}

function displayReports() {
    const users = JSON.parse(localStorage.getItem('users')) || [];
    const listings = JSON.parse(localStorage.getItem('listings')) || [];
    const deals = JSON.parse(localStorage.getItem('deals')) || [];
    const reviews = JSON.parse(localStorage.getItem('reviews')) || [];
    
    const activityBody = document.getElementById('activityReport');
    if (activityBody) {
        activityBody.innerHTML = users.filter(u => u.role !== 'admin').map(user => `
            <tr>
                <td>${escapeHtml(user.fullName || user.name)}</td>
                <td>${listings.filter(l => l.seller === (user.fullName || user.name)).length}</td>
                <td>${deals.filter(d => d.sellerId === user.id).length}</td>
                <td>${deals.filter(d => d.buyerId === user.id).length}</td>
                <td>${reviews.filter(r => r.sellerId === user.id).length}</td>
            </tr>
        `).join('');
        
        if (users.filter(u => u.role !== 'admin').length === 0) {
            activityBody.innerHTML = '<tr><td colspan="5" style="text-align: center;">Нет данных</td></tr>';
        }
    }
    
    const categoriesBody = document.getElementById('categoriesReport');
    if (categoriesBody) {
        const categories = {};
        listings.forEach(l => {
            if (l.category) {
                if (!categories[l.category]) categories[l.category] = { count: 0, sum: 0 };
                categories[l.category].count++;
                categories[l.category].sum += l.price;
            }
        });
        
        if (Object.keys(categories).length === 0) {
            categoriesBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Нет данных</td></tr>';
        } else {
            categoriesBody.innerHTML = Object.entries(categories).map(([cat, data]) => `
                <tr>
                    <td>${escapeHtml(cat)}</td>
                    <td>${data.count}</td>
                    <td>${data.sum.toLocaleString()} ₽</td>
                    <td>${Math.round(data.sum / data.count).toLocaleString()} ₽</td>
                </tr>
            `).join('');
        }
    }
}

async function exportReport() {
    try {
        const response = await fetch(API.exportJsonUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            const dataStr = JSON.stringify(data, null, 2);
            const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr);
            const exportFileDefaultName = `report-${new Date().toLocaleDateString()}.json`;
            const linkElement = document.createElement('a');
            linkElement.setAttribute('href', dataUri);
            linkElement.setAttribute('download', exportFileDefaultName);
            linkElement.click();
            showSuccess('Отчет экспортирован');
            return;
        }
    } catch (error) {
        console.error('Ошибка экспорта:', error);
    }
    
    // Fallback на localStorage
    const report = {
        date: new Date().toISOString(),
        users: JSON.parse(localStorage.getItem('users')) || [],
        listings: JSON.parse(localStorage.getItem('listings')) || [],
        deals: JSON.parse(localStorage.getItem('deals')) || []
    };
    
    const dataStr = JSON.stringify(report, null, 2);
    const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr);
    const exportFileDefaultName = `report-${new Date().toLocaleDateString()}.json`;
    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', dataUri);
    linkElement.setAttribute('download', exportFileDefaultName);
    linkElement.click();
    showSuccess('Отчет экспортирован');
}

function setupEventListeners() {
    const searchInput = document.getElementById('userSearch');
    if (searchInput) {
        searchInput.addEventListener('input', filterUsers);
    }
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

function showSuccess(message) {
    const toast = document.createElement('div');
    toast.style.cssText = 'position:fixed;bottom:20px;right:20px;background:#27ae60;color:white;padding:12px 20px;border-radius:8px;z-index:10000;';
    toast.textContent = '✅ ' + message;
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