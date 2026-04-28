let profileData = null;
let currentUser = null;
let currentRating = 0;
let isEditing = false;
let pendingAvatarFile = null;
let avatarMarkedForDeletion = false;
let availableFaculties = [];

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }

    setupProfileEventHandlers();
    updateAdminNavigation();
    await loadFacultyOptions();
    await refreshProfilePage();
};

function setupProfileEventHandlers() {
    const avatarInput = document.getElementById('avatarInput');
    if (avatarInput) {
        avatarInput.addEventListener('change', handleAvatarUpload);
    }

    updateEditingState();
}

async function refreshProfilePage() {
    await loadProfileData();
    await loadReviews();
    await loadCompletedDeals();
}

function updateAdminNavigation() {
    const adminPanelLink = document.getElementById('adminPanelLink');
    if (!adminPanelLink) return;

    const role = (currentUser.role || '').toLowerCase();
    adminPanelLink.style.display = role === 'admin' ? '' : 'none';
}

async function loadProfileData() {
    const response = await fetch(API.getMyProfileUrl(), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить профиль'));
    }

    profileData = await response.json();
    displayProfileData();
    displayStats();
    updateEditingState();
}

function displayProfileData() {
    if (!profileData) return;

    const fullNameInput = document.getElementById('fullNameInput');
    const emailInput = document.getElementById('emailInput');
    const phoneInput = document.getElementById('phoneInput');
    const universityInput = document.getElementById('universityInput');
    const facultySelect = document.getElementById('facultySelect');
    const ratingStars = document.getElementById('ratingStars');
    const ratingValue = document.getElementById('ratingValue');
    const ratingCount = document.getElementById('ratingCount');

    if (fullNameInput) fullNameInput.value = profileData.fullName || '';
    if (emailInput) emailInput.value = profileData.email || '';
    if (phoneInput) phoneInput.value = profileData.phone || '';
    if (universityInput) universityInput.value = profileData.university || '';
    if (facultySelect) facultySelect.value = profileData.faculty || '';

    renderAvatarPreview(profileData.avatarUrl);

    if (ratingValue) ratingValue.textContent = Number(profileData.rating || 0).toFixed(1);
    if (ratingStars) ratingStars.textContent = getStars(profileData.rating || 0);
    if (ratingCount) ratingCount.textContent = `(${profileData.reviewsCount || 0} отзывов)`;
}

function displayStats() {
    if (!profileData) return;

    const activeListings = document.getElementById('activeListings');
    const totalSales = document.getElementById('totalSales');
    const totalPurchases = document.getElementById('totalPurchases');
    const totalDeals = document.getElementById('totalDeals');

    if (activeListings) activeListings.textContent = profileData.activeAdvertisementsCount || 0;
    if (totalSales) totalSales.textContent = profileData.salesCount || 0;
    if (totalPurchases) totalPurchases.textContent = profileData.purchasesCount || 0;
    if (totalDeals) {
        totalDeals.textContent = (profileData.salesCount || 0) + (profileData.purchasesCount || 0);
    }
}

function getStars(rating) {
    const normalizedRating = Number(rating || 0);
    const fullStars = Math.floor(normalizedRating);
    const hasHalf = normalizedRating % 1 >= 0.5;
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

async function loadReviews() {
    const response = await fetch(API.getUserReviewsUrl(profileData.id), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить отзывы'));
    }

    const reviews = await response.json();
    displayReviews(reviews);
}

function displayReviews(reviews) {
    const reviewsList = document.getElementById('reviewsList');
    if (!reviewsList) return;

    if (!reviews.length) {
        reviewsList.innerHTML = '<p style="color:#95a5a6;text-align:center;padding:20px;">Пока нет отзывов</p>';
        return;
    }

    reviewsList.innerHTML = reviews
        .sort((left, right) => new Date(right.createdAt) - new Date(left.createdAt))
        .map(review => `
            <div class="review-card">
                <div class="review-header">
                    <span class="review-author">${escapeHtml(review.authorName || 'Пользователь')}</span>
                    <span class="review-rating">${'★'.repeat(review.rating)}${'☆'.repeat(5 - review.rating)}</span>
                    <span class="review-date">${formatDate(review.createdAt)}</span>
                </div>
                <div class="review-text">${escapeHtml(review.comment || '')}</div>
                <div class="review-product">Товар: ${escapeHtml(review.productName || 'Не указан')}</div>
            </div>
        `).join('');
}

async function loadCompletedDeals() {
    const response = await fetch(API.getBuyerOrdersUrl(), {
        credentials: 'include'
    });

    if (!response.ok) {
        throw new Error(await parseApiError(response, 'Не удалось загрузить сделки'));
    }

    const deals = await response.json();
    const completedDeals = deals.filter(item => item.status === 'completed');
    displayCompletedDeals(completedDeals);
}

function displayCompletedDeals(deals) {
    const select = document.getElementById('completedDeals');
    const writeReviewSection = document.getElementById('writeReviewSection');

    if (!select) return;

    if (!deals.length) {
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
    pendingAvatarFile = null;
    avatarMarkedForDeletion = false;
    updateEditingState();
    renderAvatarPreview(profileData?.avatarUrl || null);
}

function cancelEdit() {
    isEditing = false;
    pendingAvatarFile = null;
    avatarMarkedForDeletion = false;

    const avatarInput = document.getElementById('avatarInput');
    if (avatarInput) {
        avatarInput.value = '';
    }

    displayProfileData();
    updateEditingState();
}

async function saveProfile() {
    const updatedProfile = {
        fullName: document.getElementById('fullNameInput').value.trim(),
        phone: document.getElementById('phoneInput').value.trim(),
        faculty: document.getElementById('facultySelect').value
    };

    try {
        const updated = await requestJson(API.getUpdateProfileUrl(), {
            method: 'PUT',
            body: JSON.stringify(updatedProfile),
            fallbackMessage: 'Не удалось обновить профиль'
        });

        if (avatarMarkedForDeletion) {
            await requestNoContent(API.getDeleteAvatarUrl(), {
                method: 'DELETE',
                fallbackMessage: 'Не удалось удалить аватар'
            });
        }

        if (pendingAvatarFile) {
            const formData = new FormData();
            formData.append('avatar', pendingAvatarFile);

            const avatarResponse = await requestJson(API.getUpdateAvatarUrl(), {
                method: 'POST',
                body: formData,
                isForm: true,
                fallbackMessage: 'Не удалось загрузить аватар'
            });

            updated.avatarUrl = avatarResponse.avatarUrl;
        } else if (avatarMarkedForDeletion) {
            updated.avatarUrl = null;
        }

        profileData = {
            ...profileData,
            ...updated
        };

        cancelEdit();
        displayProfileData();
        showSuccess('Профиль успешно обновлен');
    } catch (error) {
        showError(error.message || 'Не удалось обновить профиль');
    }
}

function handleAvatarUpload(event) {
    const file = event.target.files[0];
    if (!file) {
        return;
    }

    if (!isEditing) {
        event.target.value = '';
        showError('Чтобы изменить фото, сначала нажмите "Редактировать"');
        return;
    }

    if (file.size > 5 * 1024 * 1024) {
        showError('Размер файла не должен превышать 5 МБ');
        event.target.value = '';
        return;
    }

    if (!file.type.startsWith('image/')) {
        showError('Загрузите изображение');
        event.target.value = '';
        return;
    }

    pendingAvatarFile = file;
    avatarMarkedForDeletion = false;
    updateEditingState();

    const reader = new FileReader();
    reader.onload = function(loadEvent) {
        renderAvatarPreview(loadEvent.target.result);
    };
    reader.readAsDataURL(file);
}

function clearAvatar() {
    if (!isEditing) {
        return;
    }

    pendingAvatarFile = null;
    avatarMarkedForDeletion = true;

    const avatarInput = document.getElementById('avatarInput');
    if (avatarInput) avatarInput.value = '';

    renderAvatarPreview(null);
    updateEditingState();
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
        await requestJson(API.createReviewUrl(), {
            method: 'POST',
            body: JSON.stringify({
                orderId: Number(dealId),
                rating: currentRating,
                comment: text
            }),
            fallbackMessage: 'Не удалось отправить отзыв'
        });

        resetReviewForm();
        await loadReviews();
        showSuccess('Отзыв успешно отправлен');
    } catch (error) {
        showError(error.message || 'Не удалось отправить отзыв');
    }
}

function resetReviewForm() {
    document.getElementById('completedDeals').value = '';
    document.getElementById('reviewText').value = '';
    setRating(0);
}

async function loadFacultyOptions() {
    try {
        const config = await requestJson(API.getConfigUrl(), {
            method: 'GET',
            useCsrf: false,
            fallbackMessage: 'Не удалось загрузить конфигурацию профиля'
        });

        availableFaculties = Array.isArray(config.faculties) ? config.faculties : [];
    } catch (error) {
        console.error('Faculty config load failed:', error);
        availableFaculties = [];
    }

    const facultySelect = document.getElementById('facultySelect');
    if (!facultySelect) return;

    facultySelect.innerHTML = '<option value="">Выберите направление</option>' +
        availableFaculties.map(faculty => `
            <option value="${escapeHtml(faculty)}">${escapeHtml(faculty)}</option>
        `).join('');
}

function renderAvatarPreview(avatarUrl) {
    const avatarPreview = document.getElementById('avatarPreview');
    if (!avatarPreview) {
        return;
    }

    avatarPreview.innerHTML = avatarUrl
        ? `<img src="${escapeHtml(avatarUrl)}" alt="Avatar">`
        : '📷';
}

function updateEditingState() {
    const fullNameInput = document.getElementById('fullNameInput');
    const phoneInput = document.getElementById('phoneInput');
    const facultySelect = document.getElementById('facultySelect');
    const clearAvatarBtn = document.getElementById('clearAvatarBtn');
    const avatarInput = document.getElementById('avatarInput');
    const avatarLabel = document.getElementById('avatarLabel');
    const actionsDiv = document.getElementById('profileActions');
    const hasAvatarToClear = Boolean(profileData?.avatarUrl || pendingAvatarFile);

    if (fullNameInput) fullNameInput.disabled = !isEditing;
    if (phoneInput) phoneInput.disabled = !isEditing;
    if (facultySelect) facultySelect.disabled = !isEditing;
    if (avatarInput) avatarInput.disabled = !isEditing;
    if (clearAvatarBtn) clearAvatarBtn.style.display = isEditing && hasAvatarToClear ? 'inline-block' : 'none';

    if (avatarLabel) {
        avatarLabel.classList.toggle('disabled', !isEditing);
        avatarLabel.textContent = isEditing
            ? 'Выбрать фото'
            : 'Фото меняется в режиме редактирования';
    }

    if (!actionsDiv) {
        return;
    }

    actionsDiv.innerHTML = isEditing
        ? `
            <button class="save-btn" onclick="saveProfile()">Сохранить</button>
            <button class="cancel-btn" onclick="cancelEdit()">Отмена</button>
        `
        : '<button class="edit-btn" onclick="startEdit()">Редактировать</button>';
}
