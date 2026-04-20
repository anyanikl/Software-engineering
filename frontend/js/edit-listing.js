let currentListing = null;
let currentUser = null;
let originalImageUrl = null;

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
        showError('Ошибка: объявление не найдено');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 2000);
        return;
    }
    
    if (currentUser) {
        try {
            const response = await fetch(API.getAdvertisementUrl(listingId), {
                credentials: 'include'
            });
            
            if (response.ok) {
                currentListing = await response.json();
                originalImageUrl = currentListing.mainImageUrl;
                fillFormWithListingData();
                
                // Проверка прав (только владелец может редактировать)
                if (currentListing.sellerId !== currentUser.id && currentListing.sellerEmail !== currentUser.email) {
                    showError('У вас нет прав на редактирование этого объявления');
                    setTimeout(() => {
                        window.location.href = 'my-listings.html';
                    }, 2000);
                }
                return;
            }
        } catch (error) {
            console.error('Ошибка загрузки объявления из API:', error);
        }
    }
    
    // Fallback на localStorage
    const listings = JSON.parse(localStorage.getItem('listings')) || [];
    currentListing = listings.find(l => l.id == listingId);
    
    if (!currentListing) {
        showError('Объявление не найдено');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 2000);
        return;
    }
    
    // Проверка прав
    if (currentListing.seller !== currentUser.fullName && 
        currentListing.sellerEmail !== currentUser.email &&
        currentUser.role !== 'admin') {
        showError('У вас нет прав на редактирование этого объявления');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 2000);
        return;
    }
    
    originalImageUrl = currentListing.image;
    fillFormWithListingData();
}

function fillFormWithListingData() {
    document.getElementById('listingId').value = currentListing.id;
    document.getElementById('name').value = currentListing.title || currentListing.name || '';
    
    // Определяем курс из категории
    let course = currentListing.course || '';
    if (!course && currentListing.category) {
        const match = currentListing.category.match(/(\d+)/);
        if (match) course = match[1];
    }
    if (course) document.getElementById('category').value = course;
    
    document.getElementById('type').value = currentListing.type || '';
    document.getElementById('description').value = currentListing.description || '';
    document.getElementById('price').value = currentListing.price || 0;
    document.getElementById('location').value = currentListing.location || '';
    
    if (originalImageUrl) {
        const currentImage = document.getElementById('currentImage');
        if (currentImage) {
            currentImage.src = originalImageUrl;
            currentImage.style.display = 'block';
        }
        const removeImageBtn = document.getElementById('removeImageBtn');
        if (removeImageBtn) removeImageBtn.style.display = 'inline-block';
    } else {
        const currentImage = document.getElementById('currentImage');
        if (currentImage) currentImage.style.display = 'none';
    }
    
    const statusWarning = document.getElementById('statusWarning');
    if (statusWarning) statusWarning.style.display = 'block';
    
    if (currentListing.status === 'pending') {
        statusWarning.innerHTML = `
            <p>⚠️ Объявление уже на модерации</p>
            <small>После редактирования статус останется "На модерации"</small>
        `;
    }
}

function previewImage(event) {
    const file = event.target.files[0];
    if (file) {
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
            
            const currentImage = document.getElementById('currentImage');
            if (currentImage) currentImage.style.display = 'none';
            
            const removeImageBtn = document.getElementById('removeImageBtn');
            if (removeImageBtn) removeImageBtn.style.display = 'inline-block';
        };
        reader.readAsDataURL(file);
    }
}

function removeImage() {
    const imageInput = document.getElementById('image');
    if (imageInput) imageInput.value = '';
    
    const imagePreview = document.getElementById('imagePreview');
    if (imagePreview) {
        imagePreview.classList.remove('show');
        imagePreview.src = '';
    }
    
    const imageLabel = document.getElementById('imageLabel');
    if (imageLabel) imageLabel.textContent = '📸 Нажмите для загрузки нового фото';
    
    const currentImage = document.getElementById('currentImage');
    const removeImageBtn = document.getElementById('removeImageBtn');
    
    if (originalImageUrl) {
        if (currentImage) currentImage.style.display = 'block';
        if (removeImageBtn) removeImageBtn.style.display = 'inline-block';
    } else {
        if (currentImage) currentImage.style.display = 'none';
        if (removeImageBtn) removeImageBtn.style.display = 'none';
    }
    
    // Отмечаем, что изображение нужно удалить
    window.imageRemoved = true;
}

async function updateListing(event) {
    event.preventDefault();
    
    if (!validateForm()) return;
    
    const listingId = parseInt(document.getElementById('listingId').value);
    const updatedData = {
        title: document.getElementById('name').value.trim(),
        course: parseInt(document.getElementById('category').value),
        type: document.getElementById('type').value,
        description: document.getElementById('description').value.trim(),
        price: parseInt(document.getElementById('price').value),
        location: document.getElementById('location').value.trim()
    };
    
    const imageFile = document.getElementById('image').files[0];
    const removeImageFlag = window.imageRemoved === true;
    
    if (currentUser) {
        try {
            // Обновляем данные объявления
            const response = await fetch(API.updateAdvertisementUrl(listingId), {
                method: 'PUT',
                headers: getSecureHeaders(true),
                body: JSON.stringify(updatedData),
                credentials: 'include'
            });
            
            if (response.ok) {
                // Если есть новое изображение
                if (imageFile) {
                    const imageFormData = new FormData();
                    imageFormData.append('image', imageFile);
                    await fetch(`${API.getAdvertisementUrl(listingId)}/image`, {
                        method: 'POST',
                        body: imageFormData,
                        credentials: 'include'
                    });
                } else if (removeImageFlag && originalImageUrl) {
                    // Если нужно удалить изображение
                    await fetch(`${API.getAdvertisementUrl(listingId)}/image`, {
                        method: 'DELETE',
                        headers: getSecureHeaders(true),
                        credentials: 'include'
                    });
                }
                
                showSuccess('✅ Изменения сохранены! Объявление отправлено на модерацию.');
                setTimeout(() => {
                    window.location.href = 'my-listings.html';
                }, 2000);
                return;
            } else {
                const error = await response.json();
                showError(error.message || 'Ошибка при обновлении');
            }
        } catch (error) {
            console.error('Ошибка API:', error);
        }
    }
    
    // Fallback на localStorage
    let listings = JSON.parse(localStorage.getItem('listings')) || [];
    const index = listings.findIndex(l => l.id === listingId);
    
    if (index !== -1) {
        const oldListing = listings[index];
        
        listings[index] = {
            ...oldListing,
            name: updatedData.title,
            category: `${updatedData.course} курс`,
            type: updatedData.type,
            description: updatedData.description,
            price: updatedData.price,
            location: updatedData.location,
            status: 'pending',
            updatedAt: new Date().toISOString(),
            moderatorComment: null,
            editCount: (oldListing.editCount || 0) + 1
        };
        
        // Обработка изображения
        if (imageFile) {
            const reader = new FileReader();
            reader.onload = function(e) {
                listings[index].image = e.target.result;
                localStorage.setItem('listings', JSON.stringify(listings));
            };
            reader.readAsDataURL(imageFile);
        } else if (removeImageFlag) {
            listings[index].image = null;
            localStorage.setItem('listings', JSON.stringify(listings));
        } else {
            localStorage.setItem('listings', JSON.stringify(listings));
        }
        
        showSuccess('✅ Изменения сохранены! Объявление отправлено на модерацию.');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 2000);
    } else {
        showError('Объявление не найдено');
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
        document.getElementById('name').focus();
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
        document.getElementById('description').focus();
        return false;
    }
    
    if (description.length < 10) {
        showError('Описание должно содержать минимум 10 символов');
        return false;
    }
    
    if (!price || price <= 0) {
        showError('Введите корректную цену');
        document.getElementById('price').focus();
        return false;
    }
    
    if (!location) {
        showError('Введите местоположение');
        document.getElementById('location').focus();
        return false;
    }
    
    return true;
}

function goBack() {
    if (confirm('Вы уверены, что хотите отменить изменения?')) {
        window.location.href = 'my-listings.html';
    }
}