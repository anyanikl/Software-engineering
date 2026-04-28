let currentListing = null;
let currentUser = null;
let originalImageUrl = null;
let imageRemoved = false;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser) {
        alert('Пожалуйста, войдите в систему');
        window.location.href = 'index.html';
        return;
    }

    await loadListingFromUrl();
};

async function loadListingFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    const listingId = urlParams.get('id');

    if (!listingId) {
        showError('Объявление не найдено');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 1500);
        return;
    }

    try {
        const response = await fetch(API.getAdvertisementUrl(listingId), {
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(await parseApiError(response, 'Не удалось загрузить объявление'));
        }

        currentListing = await response.json();
        if (!currentListing.canEdit) {
            throw new Error('У вас нет прав на редактирование этого объявления');
        }

        originalImageUrl = currentListing.mainImageUrl || currentListing.imageUrls?.[0] || null;
        fillFormWithListingData();
    } catch (error) {
        showError(error.message || 'Не удалось загрузить объявление');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 1500);
    }
}

function fillFormWithListingData() {
    document.getElementById('listingId').value = currentListing.id;
    document.getElementById('name').value = currentListing.title || '';
    document.getElementById('category').value = currentListing.course || '';
    document.getElementById('type').value = currentListing.type || '';
    document.getElementById('description').value = currentListing.description || '';
    document.getElementById('price').value = currentListing.price || 0;
    document.getElementById('location').value = currentListing.location || '';

    const currentImage = document.getElementById('currentImage');
    const removeImageBtn = document.getElementById('removeImageBtn');

    if (originalImageUrl) {
        currentImage.src = originalImageUrl;
        currentImage.style.display = 'block';
        removeImageBtn.style.display = 'inline-block';
    } else {
        currentImage.style.display = 'none';
        removeImageBtn.style.display = 'none';
    }

    const statusWarning = document.getElementById('statusWarning');
    if (statusWarning) {
        statusWarning.style.display = 'block';
        statusWarning.innerHTML = currentListing.status === 'pending'
            ? `
                <p>Объявление уже на модерации.</p>
                <small>После сохранения оно останется в статусе проверки.</small>
            `
            : `
                <p>После изменений объявление снова уйдет на модерацию.</p>
            `;
    }
}

function previewImage(event) {
    const file = event.target.files[0];
    if (!file) return;

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

    const reader = new FileReader();
    reader.onload = function(loadEvent) {
        const preview = document.getElementById('imagePreview');
        const currentImage = document.getElementById('currentImage');
        const imageLabel = document.getElementById('imageLabel');
        const removeImageBtn = document.getElementById('removeImageBtn');

        preview.src = loadEvent.target.result;
        preview.classList.add('show');
        currentImage.style.display = 'none';
        imageLabel.textContent = file.name;
        removeImageBtn.style.display = 'inline-block';
        imageRemoved = false;
    };
    reader.readAsDataURL(file);
}

function removeImage() {
    document.getElementById('image').value = '';
    document.getElementById('imagePreview').classList.remove('show');
    document.getElementById('imagePreview').src = '';
    document.getElementById('imageLabel').textContent = 'Нажмите для загрузки нового фото';
    document.getElementById('currentImage').style.display = 'none';
    document.getElementById('removeImageBtn').style.display = originalImageUrl ? 'inline-block' : 'none';
    imageRemoved = true;
}

async function updateListing(event) {
    event.preventDefault();

    if (!validateForm()) {
        return;
    }

    const listingId = Number(document.getElementById('listingId').value);
    const updatedData = {
        title: document.getElementById('name').value.trim(),
        course: Number(document.getElementById('category').value),
        type: document.getElementById('type').value,
        description: document.getElementById('description').value.trim(),
        price: Number(document.getElementById('price').value),
        location: document.getElementById('location').value.trim()
    };

    const imageFile = document.getElementById('image').files[0];

    try {
        await requestJson(API.updateAdvertisementUrl(listingId), {
            method: 'PUT',
            body: JSON.stringify(updatedData),
            fallbackMessage: 'Не удалось обновить объявление'
        });

        if (imageRemoved || imageFile) {
            await requestNoContent(API.getAdvertisementImageUrl(listingId), {
                method: 'DELETE',
                fallbackMessage: 'Не удалось удалить старое изображение'
            }).catch(() => {});
        }

        if (imageFile) {
            const imageFormData = new FormData();
            imageFormData.append('image', imageFile);

            await requestJson(API.getAdvertisementImageUrl(listingId), {
                method: 'POST',
                body: imageFormData,
                isForm: true,
                fallbackMessage: 'Не удалось загрузить изображение'
            });
        }

        showSuccess('Изменения сохранены. Объявление отправлено на модерацию.');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 1500);
    } catch (error) {
        showError(error.message || 'Не удалось сохранить изменения');
    }
}

function validateForm() {
    const name = document.getElementById('name').value.trim();
    const category = document.getElementById('category').value;
    const type = document.getElementById('type').value;
    const description = document.getElementById('description').value.trim();
    const price = document.getElementById('price').value;
    const location = document.getElementById('location').value.trim();

    if (!name) {
        showError('Введите название товара');
        return false;
    }

    if (name.length < 3) {
        showError('Название должно содержать минимум 3 символа');
        return false;
    }

    if (!category) {
        showError('Выберите курс');
        return false;
    }

    if (!type) {
        showError('Выберите тип');
        return false;
    }

    if (!description) {
        showError('Введите описание');
        return false;
    }

    if (description.length < 10) {
        showError('Описание должно содержать минимум 10 символов');
        return false;
    }

    if (!price || Number(price) <= 0) {
        showError('Введите корректную цену');
        return false;
    }

    if (!location) {
        showError('Введите местоположение');
        return false;
    }

    return true;
}

function goBack() {
    if (confirm('Отменить изменения?')) {
        window.location.href = 'my-listings.html';
    }
}
