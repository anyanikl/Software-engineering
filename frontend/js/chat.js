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
    await checkUrlParams();
    setupRealTimeUpdates();
};

window.onbeforeunload = function() {
    if (pollInterval) {
        clearInterval(pollInterval);
    }
};

async function loadChats() {
    try {
        const response = await fetch(API.getChatsUrl(), {
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(await parseApiError(response, 'Не удалось загрузить чаты'));
        }

        chats = await response.json();
        displayChatsList();
    } catch (error) {
        console.error('Chat list load failed:', error);
        chats = [];
        displayChatsList();
    }
}

async function loadMessages(chatId) {
    try {
        const response = await fetch(API.getChatUrl(chatId), {
            credentials: 'include'
        });

        if (!response.ok) {
            throw new Error(await parseApiError(response, 'Не удалось загрузить сообщения'));
        }

        const chatData = await response.json();
        messages[chatId] = Array.isArray(chatData.messages) ? chatData.messages : [];
        displayMessages(chatId);
        await markChatAsRead(chatId);
    } catch (error) {
        console.error('Message load failed:', error);
    }
}

function displayChatsList() {
    const chatsList = document.getElementById('chatsList');
    if (!chatsList) return;

    if (chats.length === 0) {
        chatsList.innerHTML = '<div style="padding:20px;text-align:center;color:#95a5a6;">У вас пока нет чатов</div>';
        return;
    }

    chats.sort((left, right) => new Date(right.lastMessageAt || 0) - new Date(left.lastMessageAt || 0));

    chatsList.innerHTML = chats.map(chat => `
        <div class="chat-item ${currentChatId === chat.id ? 'active' : ''}" onclick="openChat(${chat.id})">
            <div class="chat-item-header">
                <span class="chat-item-name">${escapeHtml(chat.interlocutorName)}</span>
                <span class="chat-item-time">${formatTime(chat.lastMessageAt)}</span>
            </div>
            <div class="chat-item-product">${escapeHtml(chat.advertisementTitle)}</div>
            <div class="chat-item-preview">
                ${chat.lastMessage ? escapeHtml(chat.lastMessage) : 'Напишите первое сообщение'}
                ${chat.unreadCount > 0 ? `<span class="chat-item-unread" title="${chat.unreadCount}"></span>` : ''}
            </div>
        </div>
    `).join('');
}

function openChat(chatId) {
    currentChatId = chatId;
    const chat = chats.find(item => item.id === chatId);
    if (!chat) return;

    displayChatWindow(chat);
    loadMessages(chatId);
    displayChatsList();
}

function displayChatWindow(chat) {
    const noChatSelected = document.getElementById('noChatSelected');
    if (noChatSelected) {
        noChatSelected.style.display = 'none';
    }

    const chatMain = document.getElementById('chatMain');
    if (!chatMain) return;

    chatMain.innerHTML = `
        <div class="chat-header">
            <div class="chat-header-info">
                <h2>${escapeHtml(chat.interlocutorName)}</h2>
                <p>Товар: <a href="shop.html?product=${chat.advertisementId}" class="product-link">${escapeHtml(chat.advertisementTitle)}</a></p>
            </div>
            <button class="view-profile-btn" onclick="viewProfile(${chat.interlocutorId})">
                Профиль
            </button>
        </div>
        <div class="chat-messages" id="chatMessages"></div>
        <div class="chat-input-area">
            <input type="text" id="messageInput" placeholder="Введите сообщение" onkeypress="if(event.key==='Enter'){sendMessage();}">
            <button class="send-btn" onclick="sendMessage()">Отправить</button>
        </div>
    `;
}

function displayMessages(chatId) {
    const chatMessages = document.getElementById('chatMessages');
    if (!chatMessages) return;

    const chatMessagesList = messages[chatId] || [];
    if (chatMessagesList.length === 0) {
        chatMessages.innerHTML = `
            <div style="text-align:center;padding:40px;color:#95a5a6;">
                Напишите первое сообщение
            </div>
        `;
        return;
    }

    chatMessages.innerHTML = chatMessagesList.map(message => `
        <div class="message ${message.senderId === currentUser?.id ? 'sent' : 'received'}">
            <div class="message-content">
                ${escapeHtml(message.content)}
                <div class="message-time">${formatTime(message.createdAt)}</div>
            </div>
        </div>
    `).join('');

    chatMessages.scrollTop = chatMessages.scrollHeight;
}

async function sendMessage() {
    const input = document.getElementById('messageInput');
    const text = input?.value.trim();

    if (!text || !currentChatId) {
        return;
    }

    try {
        const newMessage = await requestJson(API.sendMessageUrl(), {
            method: 'POST',
            body: JSON.stringify({
                chatId: currentChatId,
                content: text
            }),
            fallbackMessage: 'Не удалось отправить сообщение'
        });

        if (!messages[currentChatId]) {
            messages[currentChatId] = [];
        }

        messages[currentChatId].push(newMessage);
        input.value = '';
        displayMessages(currentChatId);
        await loadChats();
    } catch (error) {
        showError(error.message || 'Не удалось отправить сообщение');
    }
}

async function markChatAsRead(chatId) {
    try {
        await requestNoContent(API.markChatReadUrl(chatId), {
            method: 'POST',
            fallbackMessage: 'Не удалось отметить чат как прочитанный'
        });

        const chat = chats.find(item => item.id === chatId);
        if (chat) {
            chat.unreadCount = 0;
            displayChatsList();
        }
    } catch (error) {
        console.error('Mark as read failed:', error);
    }
}

function setupRealTimeUpdates() {
    pollInterval = setInterval(async () => {
        await loadChats();
        if (currentChatId) {
            await loadMessages(currentChatId);
        }
    }, 5000);
}

async function checkUrlParams() {
    const urlParams = new URLSearchParams(window.location.search);
    const chatId = urlParams.get('chat');
    const productId = urlParams.get('product');
    const participantId = urlParams.get('participant');

    if (chatId) {
        openChat(Number(chatId));
        return;
    }

    if (productId) {
        await createOrOpenChat(Number(productId), participantId ? Number(participantId) : null);
    }
}

async function createOrOpenChat(productId, participantId) {
    try {
        const chat = await requestJson(API.createOrGetChatUrl(productId, participantId), {
            method: 'POST',
            fallbackMessage: 'Не удалось создать чат'
        });

        await loadChats();
        openChat(chat.id);
    } catch (error) {
        showError(error.message || 'Не удалось создать чат');
    }
}

function viewProfile(userId) {
    window.location.href = `public-profile.html?id=${userId}`;
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
    }

    if (date >= yesterday) {
        return 'Вчера';
    }

    return date.toLocaleDateString([], { day: '2-digit', month: '2-digit' });
}
