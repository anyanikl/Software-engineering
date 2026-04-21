let profileData = {
    id: null,
    fullName: '',
    email: '',
    phone: '',
    faculty: '',
    avatarUrl: null,
    rating: 0,
    reviewsCount: 0,
    salesCount: 0,
    purchasesCount: 0,
    activeAdvertisementsCount: 0
};
let currentUser = null;
let currentRating = 0;
let isEditing = false;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }
    
    await loadProfileData();
    await loadReviews();
    await loadCompletedDeals();
};

// Преобразование UserProfileDto в формат фронта
function convertUserProfileToLocalFormat(apiProfile) {
    return {
        id: apiProfile.id,
        fullName: apiProfile.fullName,
        email: apiProfile.email,
        phone: apiProfile.phone || '',
        avatarUrl: apiProfile.avatarUrl || null,
        faculty: apiProfile.faculty || '',
        rating: apiProfile.rating || 0,
        reviewsCount: apiProfile.reviewsCount || 0,
        salesCount: apiProfile.salesCount || 0,
        purchasesCount: apiProfile.purchasesCount || 0,
        activeAdvertisementsCount: apiProfile.activeAdvertisementsCount || 0
    };
}

async function loadProfileData() {
    try {
        const response = await fetch(API.getMyProfileUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            profileData = convertUserProfileToLocalFormat(data);
            localStorage.setItem('profileData', JSON.stringify(profileData));
            displayProfileData();
            displayStats();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки профиля из API:', error);
    }
    
    // Fallback на localStorage
    const savedProfile = JSON.parse(localStorage.getItem('profileData'));
    if (savedProfile) {
        profileData = savedProfile;
    } else if (currentUser) {
        profileData = {
            id: currentUser.id || 1,
            fullName: currentUser.fullName || currentUser.name || 'Пользователь',
            email: currentUser.email || '',
            phone: currentUser.phone || '',
            avatarUrl: currentUser.avatarUrl || null,
            faculty: currentUser.faculty || '',
            rating: 0,
            reviewsCount: 0,
            salesCount: 0,
            purchasesCount: 0,
            activeAdvertisementsCount: 0
        };
        localStorage.setItem('profileData', JSON.stringify(profileData));
    }
    
    displayProfileData();
    displayStats();
}

function displayProfileData() {
    const fullNameInput = document.getElementById('fullNameInput');
    const emailInput = document.getElementById('emailInput');
    const phoneInput = document.getElementById('phoneInput');
    const universityInput = document.getElementById('universityInput');
    const facultySelect = document.getElementById('facultySelect');
    const avatarPreview = document.getElementById('avatarPreview');
    const ratingStars = document.getElementById('ratingStars');
    const ratingValue = document.getElementById('ratingValue');
    const ratingCount = document.getElementById('ratingCount');
    
    if (fullNameInput) fullNameInput.value = profileData.fullName || '';
    if (emailInput) emailInput.value = profileData.email || '';
    if (phoneInput) phoneInput.value = profileData.phone || '';
    if (universityInput) universityInput.value = profileData.university || '';
    if (facultySelect) facultySelect.value = profileData.faculty || '';
    
    if (avatarPreview) {
        if (profileData.avatarUrl) {
            avatarPreview.innerHTML = `<img src="${profileData.avatarUrl}" alt="Avatar">`;
        } else {
            avatarPreview.innerHTML = '📷';
        }
    }
    
    if (ratingValue) ratingValue.textContent = profileData.rating.toFixed(1);
    if (ratingStars) ratingStars.textContent = getStars(profileData.rating);
    if (ratingCount) ratingCount.textContent = `(${profileData.reviewsCount} отзывов)`;
}

function displayStats() {
    const activeListings = document.getElementById('activeListings');
    const totalSales = document.getElementById('totalSales');
    const totalPurchases = document.getElementById('totalPurchases');
    const totalDeals = document.getElementById('totalDeals');
    
    if (activeListings) activeListings.textContent = profileData.activeAdvertisementsCount || 0;
    if (totalSales) totalSales.textContent = profileData.salesCount || 0;
    if (totalPurchases) totalPurchases.textContent = profileData.purchasesCount || 0;
    if (totalDeals) totalDeals.textContent = (profileData.salesCount || 0) + (profileData.purchasesCount || 0);
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

// Преобразование ReviewDto в формат фронта
function convertReviewToLocalFormat(review) {
    return {
        id: review.id,
        orderId: review.orderId,
        authorId: review.authorId,
        authorName: review.authorName,
        rating: review.rating,
        comment: review.comment,
        createdAt: review.createdAt,
        productName: review.productName
    };
}

async function loadReviews() {
    try {
        const response = await fetch(API.getUserReviewsUrl(profileData.id), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const reviews = await response.json();
            displayReviews(reviews.map(r => convertReviewToLocalFormat(r)));
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки отзывов из API:', error);
    }
    
    // Fallback на localStorage
    const reviews = JSON.parse(localStorage.getItem('reviews')) || [];
    const myReviews = reviews.filter(r => r.sellerId === profileData.id);
    displayReviews(myReviews);
}

function displayReviews(reviews) {
    const reviewsList = document.getElementById('reviewsList');
    if (!reviewsList) return;
    
    if (reviews.length === 0) {
        reviewsList.innerHTML = '<p style="color: #95a5a6; text-align: center; padding: 20px;">Пока нет отзывов</p>';
        return;
    }
    
    reviewsList.innerHTML = reviews.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt)).map(review => `
        <div class="review-card">
            <div class="review-header">
                <span class="review-author">👤 ${escapeHtml(review.authorName || 'Пользователь')}</span>
                <span class="review-rating">${'★'.repeat(review.rating)}${'☆'.repeat(5 - review.rating)}</span>
                <span class="review-date">${formatDate(review.createdAt)}</span>
            </div>
            <div class="review-text">${escapeHtml(review.comment)}</div>
            <div class="review-product">📦 Товар: ${escapeHtml(review.productName || 'Не указан')}</div>
        </div>
    `).join('');
}

// Преобразование OrderDto в формат фронта
function convertOrderToLocalFormat(order) {
    return {
        id: order.id,
        advertisementId: order.advertisementId,
        advertisementTitle: order.advertisementTitle,
        buyerId: order.buyerId,
        buyerName: order.buyerName,
        sellerId: order.sellerId,
        sellerName: order.sellerName,
        price: order.price,
        status: order.status,
        createdAt: order.createdAt,
        completedAt: order.completedAt
    };
}

async function loadCompletedDeals() {
    try {
        const response = await fetch(API.getBuyerOrdersUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const deals = await response.json();
            const completedDeals = deals.filter(d => d.status === 'completed').map(d => convertOrderToLocalFormat(d));
            displayCompletedDeals(completedDeals);
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки сделок из API:', error);
    }
    
    // Fallback на localStorage
    const deals = JSON.parse(localStorage.getItem('deals')) || [];
    const completedDeals = deals.filter(d => d.buyerId === profileData.id && d.status === 'completed');
    displayCompletedDeals(completedDeals);
}

function displayCompletedDeals(deals) {
    const select = document.getElementById('completedDeals');
    const writeReviewSection = document.getElementById('writeReviewSection');
    
    if (!select) return;
    
    if (deals.length === 0) {
        select.innerHTML = '<option value="">Нет завершенных сделок</option>';
        if (writeReviewSection) writeReviewSection.style.display = 'none';
        return;
    }
    
    select.innerHTML = '<option value="">Выберите завершенную сделку</option>' + 
        deals.map(deal => `
            <option value="${deal.id}">
                ${escapeHtml(deal.advertisementTitle)} - ${formatDate(deal.completedAt || deal.createdAt)}
            </option>
        `).join('');
    
    if (writeReviewSection) writeReviewSection.style.display = 'block';
}

function startEdit() {
    isEditing = true;
    
    const fullNameInput = document.getElementById('fullNameInput');
    const phoneInput = document.getElementById('phoneInput');
    const facultySelect = document.getElementById('facultySelect');
    const clearAvatarBtn = document.getElementById('clearAvatarBtn');
    
    if (fullNameInput) fullNameInput.disabled = false;
    if (phoneInput) phoneInput.disabled = false;
    if (facultySelect) facultySelect.disabled = false;
    if (clearAvatarBtn) clearAvatarBtn.style.display = 'inline-block';
    
    const actionsDiv = document.getElementById('profileActions');
    if (actionsDiv) {
        actionsDiv.innerHTML = `
            <button class="save-btn" onclick="saveProfile()">💾 Сохранить</button>
            <button class="cancel-btn" onclick="cancelEdit()">❌ Отмена</button>
        `;
    }
    
    const avatarInput = document.getElementById('avatarInput');
    if (avatarInput) avatarInput.addEventListener('change', handleAvatarUpload);
}

function cancelEdit() {
    isEditing = false;
    
    const fullNameInput = document.getElementById('fullNameInput');
    const phoneInput = document.getElementById('phoneInput');
    const facultySelect = document.getElementById('facultySelect');
    const clearAvatarBtn = document.getElementById('clearAvatarBtn');
    
    if (fullNameInput) fullNameInput.disabled = true;
    if (phoneInput) phoneInput.disabled = true;
    if (facultySelect) facultySelect.disabled = true;
    if (clearAvatarBtn) clearAvatarBtn.style.display = 'none';
    
    displayProfileData();
    
    const actionsDiv = document.getElementById('profileActions');
    if (actionsDiv) {
        actionsDiv.innerHTML = `<button class="edit-btn" onclick="startEdit()">✏️ Редактировать</button>`;
    }
}

async function saveProfile() {
    const updatedProfile = {
        fullName: document.getElementById('fullNameInput').value,
        phone: document.getElementById('phoneInput').value,
        faculty: document.getElementById('facultySelect').value
    };
    
    try {
        const response = await fetch(API.getUpdateProfileUrl(), {
            method: 'PUT',
            headers: getSecureHeaders(true),
            body: JSON.stringify(updatedProfile),
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            profileData.fullName = data.fullName;
            profileData.phone = data.phone;
            profileData.faculty = data.faculty;
            localStorage.setItem('profileData', JSON.stringify(profileData));
            cancelEdit();
            showSuccess('Профиль успешно обновлен');
            return;
        }
    } catch (error) {
        console.error('Ошибка сохранения профиля:', error);
    }
    
    // Fallback на localStorage
    profileData.fullName = updatedProfile.fullName;
    profileData.phone = updatedProfile.phone;
    profileData.faculty = updatedProfile.faculty;
    localStorage.setItem('profileData', JSON.stringify(profileData));
    cancelEdit();
    showSuccess('Профиль успешно обновлен');
}

function handleAvatarUpload(event) {
    const file = event.target.files[0];
    if (file) {
        if (file.size > 5 * 1024 * 1024) {
            showError('Размер файла не должен превышать 5MB');
            return;
        }
        if (!file.type.startsWith('image/')) {
            showError('Пожалуйста, загрузите изображение');
            return;
        }
        
        const reader = new FileReader();
        reader.onload = function(e) {
            profileData.avatarUrl = e.target.result;
            const avatarPreview = document.getElementById('avatarPreview');
            if (avatarPreview) {
                avatarPreview.innerHTML = `<img src="${e.target.result}" alt="Avatar">`;
            }
        };
        reader.readAsDataURL(file);
    }
}

function clearAvatar() {
    profileData.avatarUrl = null;
    const avatarPreview = document.getElementById('avatarPreview');
    const avatarInput = document.getElementById('avatarInput');
    
    if (avatarPreview) avatarPreview.innerHTML = '📷';
    if (avatarInput) avatarInput.value = '';
}

function setRating(rating) {
    currentRating = rating;
    const stars = document.querySelectorAll('.rating-input span');
    stars.forEach((star, index) => {
        star.textContent = index < rating ? '★' : '☆';
    });
}

async function submitReview() {
    const dealId = document.getElementById('completedDeals').value;
    const text = document.getElementById('reviewText').value.trim();
    
    if (!dealId) {
        showError('Выберите сделку');
        return;
    }
    if (currentRating === 0) {
        showError('Поставьте оценку');
        return;
    }
    if (!text) {
        showError('Напишите отзыв');
        return;
    }
    
    try {
        const response = await fetch(API.createReviewUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            body: JSON.stringify({
                orderId: parseInt(dealId),
                rating: currentRating,
                comment: text
            }),
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('Отзыв успешно отправлен!');
            resetReviewForm();
            await loadReviews();
            return;
        }
    } catch (error) {
        console.error('Ошибка отправки отзыва:', error);
    }
    
    // Fallback на localStorage
    const deals = JSON.parse(localStorage.getItem('deals')) || [];
    const deal = deals.find(d => d.id == dealId);
    
    if (deal) {
        const reviews = JSON.parse(localStorage.getItem('reviews')) || [];
        const review = {
            id: Date.now(),
            sellerId: deal.sellerId,
            productId: deal.advertisementId,
            productName: deal.advertisementTitle,
            authorId: profileData.id,
            authorName: profileData.fullName,
            rating: currentRating,
            comment: text,
            createdAt: new Date().toISOString()
        };
        reviews.push(review);
        localStorage.setItem('reviews', JSON.stringify(reviews));
        showSuccess('Отзыв успешно отправлен!');
        resetReviewForm();
        await loadReviews();
    }
}

function resetReviewForm() {
    document.getElementById('completedDeals').value = '';
    document.getElementById('reviewText').value = '';
    setRating(0);
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