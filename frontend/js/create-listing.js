let currentUser = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();

    if (!currentUser) {
        alert('Пожалуйста, войдите в систему');
        window.location.href = 'index.html';
    }
};

function previewImage(event) {
    const file = event.target.files[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
        showError('Размер файла не должен превышать 5MB');
        event.target.value = '';
        return;
    }

    if (!file.type.startsWith('image/')) {
        showError('Пожалуйста, загрузите изображение');
        event.target.value = '';
        return;
    }

    const reader = new FileReader();
    reader.onload = function(e) {
        const preview = document.getElementById('imagePreview');
        if (preview) {
            preview.src = e.target.result;
            preview.classList.add('show');
        }

        const imageLabel = document.getElementById('imageLabel');
        if (imageLabel) imageLabel.textContent = file.name;
    };
    reader.readAsDataURL(file);
}

async function createListing(event) {
    event.preventDefault();

    if (!currentUser) {
        alert('Пожалуйста, войдите в систему');
        window.location.href = 'index.html';
        return;
    }

    const formData = {
        title: document.getElementById('name').value.trim(),
        course: parseInt(document.getElementById('category').value, 10),
        type: document.getElementById('type').value,
        description: document.getElementById('description').value.trim(),
        price: parseInt(document.getElementById('price').value, 10),
        location: document.getElementById('location').value.trim()
    };

    if (!validateListing(formData)) return;

    try {
        const response = await fetch(API.createAdvertisementUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            body: JSON.stringify(formData),
            credentials: 'include'
        });

        if (!response.ok) {
            const error = await response.json().catch(() => ({}));
            showError(error.message || 'Ошибка создания объявления');
            return;
        }

        const newListing = await response.json();
        const imageFile = document.getElementById('image').files[0];

        if (imageFile) {
            const imageFormData = new FormData();
            imageFormData.append('image', imageFile);

            const imageResponse = await fetch(API.getAdvertisementImageUrl(newListing.id), {
                method: 'POST',
                headers: getSecureFormHeaders(true),
                body: imageFormData,
                credentials: 'include'
            });

            if (!imageResponse.ok) {
                const error = await imageResponse.json().catch(() => ({}));
                showError(error.message || 'Ошибка загрузки изображения');
                return;
            }
        }

        showSuccess('Объявление создано и отправлено на модерацию');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 1500);
    } catch (error) {
        console.error('Ошибка API:', error);
        showError('Ошибка сохранения объявления в базе данных');
    }
}

function validateListing(formData) {
    if (!formData.title || !formData.course || !formData.type || !formData.description || !formData.price || !formData.location) {
        showError('Заполните все поля');
        return false;
    }

    if (formData.price <= 0) {
        showError('Цена должна быть больше 0');
        return false;
    }

    if (formData.description.length < 10) {
        showError('Описание должно содержать минимум 10 символов');
        return false;
    }

    return true;
}
