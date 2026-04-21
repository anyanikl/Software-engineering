let currentUser = null;
let chats = [];
let messages = {};
let currentChatId = null;
let pollInterval = null;

window.onload = async function() {
    await fetchCsrfToken();
    currentUser = await checkAuth();
    
    if (!currentUser) {
        window.location.href = 'index.html';
        return;
    }
    
    await loadChats();
    setupRealTimeUpdates();
    checkUrlParams();
};

async function loadChats() {
    try {
        const response = await fetch(API.getChatsUrl(), {
            credentials: 'include'
        });
        
        if (response.ok) {
            chats = await response.json();
            localStorage.setItem('chats', JSON.stringify(chats));
            displayChatsList();
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки чатов из API:', error);
    }
    
    // Fallback на localStorage
    chats = JSON.parse(localStorage.getItem('chats')) || [];
    displayChatsList();
}

async function loadMessages(chatId) {
    try {
        const response = await fetch(API.getChatUrl(chatId), {
            credentials: 'include'
        });
        
        if (response.ok) {
            const chatData = await response.json();
            // ChatDto содержит messages: MessageDto[]
            messages[chatId] = chatData.messages || [];
            localStorage.setItem('messages', JSON.stringify(messages));
            displayMessages(chatId);
            markChatAsRead(chatId);
            return;
        }
    } catch (error) {
        console.error('Ошибка загрузки сообщений из API:', error);
    }
    
    // Fallback на localStorage
    const storedMessages = JSON.parse(localStorage.getItem('messages')) || {};
    messages = storedMessages;
    displayMessages(chatId);
    markChatAsRead(chatId);
}

function displayChatsList() {
    const chatsList = document.getElementById('chatsList');
    if (!chatsList) return;
    
    if (chats.length === 0) {
        chatsList.innerHTML = '<div style="padding: 20px; text-align: center; color: #95a5a6;">🤝 У вас пока нет чатов<br><small>Начните общение с продавцом из корзины</small></div>';
        return;
    }
    
    // ChatListItemDto: id, interlocutorName, advertisementTitle, lastMessage, lastMessageAt, unreadCount
    chats.sort((a, b) => new Date(b.lastMessageAt) - new Date(a.lastMessageAt));
    
    chatsList.innerHTML = chats.map(chat => {
        const isActive = currentChatId === chat.id;
        
        return `
            <div class="chat-item ${isActive ? 'active' : ''}" onclick="openChat(${chat.id})">
                <div class="chat-item-header">
                    <span class="chat-item-name">${escapeHtml(chat.interlocutorName)}</span>
                    <span class="chat-item-time">${formatTime(chat.lastMessageAt)}</span>
                </div>
                <div class="chat-item-product">📦 ${escapeHtml(chat.advertisementTitle)}</div>
                <div class="chat-item-preview">
                    ${chat.lastMessage ? escapeHtml(chat.lastMessage) : '💬 Напишите первое сообщение'}
                    ${chat.unreadCount > 0 ? `<span class="chat-item-unread" title="${chat.unreadCount} нов. сообщ."></span>` : ''}
                </div>
            </div>
        `;
    }).join('');
}

function openChat(chatId) {
    currentChatId = chatId;
    const chat = chats.find(c => c.id === chatId);
    if (!chat) return;
    
    displayChatWindow(chat);
    loadMessages(chatId);
    displayChatsList();
}

function displayChatWindow(chat) {
    const noChatSelected = document.getElementById('noChatSelected');
    if (noChatSelected) noChatSelected.style.display = 'none';
    
    const chatMain = document.getElementById('chatMain');
    if (!chatMain) return;
    
    // ChatListItemDto содержит interlocutorName и advertisementTitle
    chatMain.innerHTML = `
        <div class="chat-header">
            <div class="chat-header-info">
                <h2>${escapeHtml(chat.interlocutorName)}</h2>
                <p>Товар: <a href="shop.html?product=${chat.advertisementId}" class="product-link">📦 ${escapeHtml(chat.advertisementTitle)}</a></p>
            </div>
            <button class="view-profile-btn" onclick="viewProfile(${getInterlocutorId(chat)})">
                👤 Просмотреть профиль
            </button>
        </div>
        <div class="chat-messages" id="chatMessages"></div>
        <div class="chat-input-area">
            <input type="text" id="messageInput" placeholder="Введите сообщение..." onkeypress="if(event.key==='Enter') sendMessage()">
            <button class="send-btn" onclick="sendMessage()">📤 Отправить</button>
        </div>
    `;
}

function getInterlocutorId(chat) {
    // Определяем ID собеседника (не текущего пользователя)
    if (chat.buyerId === currentUser?.id) return chat.sellerId;
    return chat.buyerId;
}

function displayMessages(chatId) {
    const chatMessages = document.getElementById('chatMessages');
    if (!chatMessages) return;
    
    const chatMessagesList = messages[chatId] || [];
    
    if (chatMessagesList.length === 0) {
        chatMessages.innerHTML = `
            <div style="text-align: center; padding: 40px; color: #95a5a6;">
                💬 Напишите первое сообщение<br>
                <small>Продавец ответит вам в ближайшее время</small>
            </div>
        `;
        return;
    }
    
    // MessageDto: id, senderId, senderName, content, isRead, createdAt
    chatMessages.innerHTML = chatMessagesList.map(msg => `
        <div class="message ${msg.senderId === currentUser?.id ? 'sent' : 'received'}">
            <div class="message-content">
                ${escapeHtml(msg.content)}
                <div class="message-time">${formatTime(msg.createdAt)}</div>
            </div>
        </div>
    `).join('');
    
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

async function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input?.value.trim();
    
    if (!text || !currentChatId) return;
    
    try {
        const response = await fetch(API.sendMessageUrl(), {
            method: 'POST',
            headers: getSecureHeaders(true),
            body: JSON.stringify({
                chatId: currentChatId,
                content: text
            }),
            credentials: 'include'
        });
        
        if (response.ok) {
            const newMessage = await response.json();
            
            if (!messages[currentChatId]) messages[currentChatId] = [];
            messages[currentChatId].push(newMessage);
            localStorage.setItem('messages', JSON.stringify(messages));
            
            input.value = '';
            displayMessages(currentChatId);
            await loadChats(); // Обновляем список чатов
            return;
        }
    } catch (error) {
        console.error('Ошибка отправки сообщения:', error);
    }
    
    // Fallback на localStorage
    const message = {
        id: Date.now(),
        chatId: currentChatId,
        senderId: currentUser?.id,
        senderName: currentUser?.fullName || currentUser?.name,
        content: text,
        isRead: false,
        createdAt: new Date().toISOString()
    };
    
    if (!messages[currentChatId]) messages[currentChatId] = [];
    messages[currentChatId].push(message);
    localStorage.setItem('messages', JSON.stringify(messages));
    
    const chat = chats.find(c => c.id === currentChatId);
    if (chat) {
        chat.lastMessage = text;
        chat.lastMessageAt = message.createdAt;
        localStorage.setItem('chats', JSON.stringify(chats));
    }
    
    input.value = '';
    displayMessages(currentChatId);
    displayChatsList();
}

async function markChatAsRead(chatId) {
    try {
        await fetch(API.markChatReadUrl(chatId), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
    } catch (error) {
        console.error('Ошибка отметки прочитанного:', error);
    }
    
    // Обновляем локально
    const chat = chats.find(c => c.id === chatId);
    if (chat) {
        chat.unreadCount = 0;
        localStorage.setItem('chats', JSON.stringify(chats));
        displayChatsList();
    }
}

function setupRealTimeUpdates() {
    // Обновление каждые 3 секунды
    pollInterval = setInterval(async () => {
        await loadChats();
        if (currentChatId) {
            await loadMessages(currentChatId);
        }
    }, 3000);
}

function checkUrlParams() {
    const urlParams = new URLSearchParams(window.location.search);
    const chatId = urlParams.get('chat');
    const productId = urlParams.get('product');
    const sellerId = urlParams.get('seller');
    
    if (chatId) {
        openChat(parseInt(chatId));
    } else if (productId && sellerId) {
        createOrOpenChat(parseInt(productId), parseInt(sellerId));
    }
}

async function createOrOpenChat(productId, sellerId) {
    try {
        const response = await fetch(API.createOrGetChatUrl(productId), {
            method: 'POST',
            headers: getSecureHeaders(true),
            credentials: 'include'
        });
        
        if (response.ok) {
            const chat = await response.json();
            await loadChats();
            openChat(chat.id);
            return;
        }
    } catch (error) {
        console.error('Ошибка создания чата:', error);
    }
    
    // Fallback на localStorage
    let existingChat = chats.find(c => 
        c.advertisementId === productId && 
        (c.sellerId === sellerId || c.buyerId === sellerId)
    );
    
    if (existingChat) {
        openChat(existingChat.id);
    } else {
        const product = getProductById(productId);
        const seller = getUserById(sellerId);
        
        const newChat = {
            id: Date.now(),
            advertisementId: productId,
            advertisementTitle: product.title || product.name,
            sellerId: sellerId,
            sellerName: seller.fullName || seller.name,
            buyerId: currentUser?.id,
            buyerName: currentUser?.fullName || currentUser?.name,
            interlocutorName: seller.fullName || seller.name,
            lastMessage: null,
            lastMessageAt: new Date().toISOString(),
            unreadCount: 0
        };
        
        chats.push(newChat);
        localStorage.setItem('chats', JSON.stringify(chats));
        openChat(newChat.id);
    }
}

function viewProfile(userId) {
    window.location.href = `public-profile.html?id=${userId}`;
}

function getProductById(productId) {
    const allListings = JSON.parse(localStorage.getItem('listings')) || [];
    const found = allListings.find(l => l.id === productId);
    if (found) return found;
    return { id: productId, title: 'Товар', name: 'Товар' };
}

function getUserById(userId) {
    const users = JSON.parse(localStorage.getItem('users')) || [];
    const found = users.find(u => u.id === userId);
    return found || { id: userId, fullName: 'Пользователь', name: 'Пользователь' };
}

function formatTime(timestamp) {
    if (!timestamp) return '';
    const date = new Date(timestamp);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);
    
    if (date >= today) {
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } else if (date >= yesterday) {
        return 'Вчера';
    } else {
        return date.toLocaleDateString([], { day: '2-digit', month: '2-digit' });
    }
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