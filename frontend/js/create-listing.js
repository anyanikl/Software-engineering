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
        };
        reader.readAsDataURL(file);
    }
}

async function createListing(event) {
    event.preventDefault();
    
    if (!currentUser) {
        alert('Пожалуйста, войдите в систему');
        window.location.href = 'index.html';
        return;
    }
    
    const formData = {
        title: document.getElementById('name').value,
        course: parseInt(document.getElementById('category').value),
        type: document.getElementById('type').value,
        description: document.getElementById('description').value,
        price: parseInt(document.getElementById('price').value),
        location: document.getElementById('location').value
    };
    
    // Валидация
    if (!formData.title || !formData.course || !formData.type || !formData.description || !formData.price || !formData.location) {
        showError('Заполните все поля');
        return;
    }
    
    if (formData.price <= 0) {
        showError('Цена должна быть больше 0');
        return;
    }
    
    if (formData.description.length < 10) {
        showError('Описание должно содержать минимум 10 символов');
        return;
    }
    
    const imageFile = document.getElementById('image').files[0];
    
    if (currentUser) {
        try {
            // Сначала создаём объявление через API
            const response = await fetch(API.createAdvertisementUrl(), {
                method: 'POST',
                headers: getSecureHeaders(true),
                body: JSON.stringify(formData),
                credentials: 'include'
            });
            
            if (response.ok) {
                const newListing = await response.json();
                
                // Если есть изображение, загружаем его отдельно
                if (imageFile) {
                    const imageFormData = new FormData();
                    imageFormData.append('image', imageFile);
                    
                    await fetch(`${API.getAdvertisementUrl(newListing.id)}/image`, {
                        method: 'POST',
                        body: imageFormData,
                        credentials: 'include'
                    });
                }
                
                showSuccess('✅ Объявление создано и отправлено на модерацию!');
                setTimeout(() => {
                    window.location.href = 'my-listings.html';
                }, 2000);
                return;
            } else {
                const error = await response.json();
                showError(error.message || 'Ошибка создания объявления');
            }
        } catch (error) {
            console.error('Ошибка API:', error);
        }
    }
    
    // Fallback на localStorage
    let listings = JSON.parse(localStorage.getItem('listings')) || [];
    const newId = listings.length > 0 ? Math.max(...listings.map(l => l.id)) + 1 : 1;
    
    const saveListing = (imageDataUrl = null) => {
        const listing = {
            id: newId,
            name: formData.title,
            category: `${formData.course} курс`,
            type: formData.type,
            description: formData.description,
            price: formData.price,
            location: formData.location,
            image: imageDataUrl,
            status: 'pending',
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            seller: currentUser.fullName || currentUser.name || 'Пользователь',
            sellerEmail: currentUser.email,
            sellerId: currentUser.id
        };
        
        listings.push(listing);
        localStorage.setItem('listings', JSON.stringify(listings));
        
        showSuccess('✅ Объявление создано и отправлено на модерацию!');
        setTimeout(() => {
            window.location.href = 'my-listings.html';
        }, 2000);
    };
    
    if (imageFile) {
        const reader = new FileReader();
        reader.onload = function(e) {
            saveListing(e.target.result);
        };
        reader.readAsDataURL(imageFile);
    } else {
        saveListing(null);
    }
}