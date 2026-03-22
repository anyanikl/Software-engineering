# Контракты для фронта

## Базовый URL

- Локальный Api только для меня (Кирилла, чтобы я не искал его при настройке nginx): `http://localhost:8080`
  - вообще так как подниматься сайт будет на nginx, и я буду его настраивать и поднимать как обратный прокси в целом все запросы делай по этим же относительным адресам

## Auth, cookies, CSRF

- Серверная часть проводит авторизацию по cookie
- Интерфейс должен отправлять запросы с учетными данными: "include"
- Для всех запросов на изменение состояния ("POST", "PUT", "DELETE") сервер ожидает заголовок CSRF
- Серверная часть возвращает токен CSRF в теле ответа, а также записывает файлы cookie
- Для запросов на запись отправить заголовок:

```https
X-XSRF-TOKEN: <token>
```

- Конечные эндпоинты, доступные только для чтения, помеченные ниже как "GET" без требования CSRF, могут быть вызваны без этого заголовка

## Распространенная форма ошибки

- Бизнес-ошибки обычно проявляются в виде:

```json
{
  "message": "Text"
}
```

- Конечные точки аутентификации возвращают:

```json
{
  "isSuccess": false,
  "user": null,
  "errors": ["Text"]
}
```

## Контракты основные

Ниже расписаны основные контракты на которые я буду ссылаться в разделе "Эндпоинты, они же адреса..". Я решил прописать их в начале так как на некоторые буду ссылаться несколько раз и чтобы не дублировать

### AuthResponseDto

```json
{
  "isSuccess": true,
  "user": {
    "id": 1,
    "email": "user@university.ru",
    "fullName": "Иванов Иван",
    "phoneNumber": "+79991234567",
    "role": "user"
  },
  "errors": []
}
```

### CsrfTokenDto

```json
{
  "token": "csrf-token-value"
}
```

### AdvertisementCardDto

```json
{
  "id": 10,
  "title": "Учебник по математике",
  "shortDescription": "Краткое описание",
  "type": "book",
  "price": 1500,
  "sellerName": "Иван Петров",
  "mainImageUrl": "/uploads/advertisements/10/main.jpg",
  "isFavorite": false,
  "isInCart": false
}
```

### AdvertisementDto

```json
{
  "id": 10,
  "title": "Учебник по математике",
  "description": "Полное описание",
  "course": 2,
  "type": "book",
  "price": 1500,
  "location": "Главный корпус",
  "sellerName": "Иван Петров",
  "status": "approved",
  "moderatorComment": null,
  "imageUrls": ["/uploads/advertisements/10/main.jpg"]
}
```

### MyAdvertisementDto

```json
{
  "id": 10,
  "title": "Учебник по математике",
  "description": "Описание",
  "price": 1500,
  "location": "Главный корпус",
  "status": "pending",
  "moderatorComment": "Добавьте фото",
  "ordersCount": 0,
  "mainImageUrl": "/uploads/advertisements/10/main.jpg"
}
```

### ModerationAdvertisementDto

```json
{
  "id": 10,
  "title": "Учебник по математике",
  "description": "Описание",
  "price": 1500,
  "location": "Главный корпус",
  "sellerName": "Иван Петров",
  "imageUrls": ["/uploads/advertisements/10/main.jpg"]
}
```

### UserProfileDto

```json
{
  "id": 1,
  "fullName": "Иван Петров",
  "email": "ivan@university.ru",
  "phone": "+79991234567",
  "avatarUrl": null,
  "faculty": "ФКН",
  "rating": 4.8,
  "reviewsCount": 5,
  "salesCount": 3,
  "purchasesCount": 2,
  "activeAdvertisementsCount": 4
}
```

### PublicUserProfileDto

```json
{
  "id": 1,
  "fullName": "Иван Петров",
  "avatarUrl": null,
  "faculty": "ФКН",
  "rating": 4.8,
  "reviewsCount": 5,
  "reviews": [],
  "activeAdvertisements": []
}
```

### CartDto

```json
{
  "items": [],
  "totalCount": 0,
  "totalPrice": 0
}
```

### OrderDto

```json
{
  "id": 1,
  "advertisementId": 10,
  "advertisementTitle": "Учебник",
  "buyerId": 2,
  "buyerName": "Покупатель",
  "sellerId": 1,
  "sellerName": "Продавец",
  "price": 1500,
  "status": "created",
  "createdAt": "2026-03-22T09:00:00Z",
  "completedAt": null
}
```

### ReviewDto

```json
{
  "id": 1,
  "orderId": 5,
  "authorId": 2,
  "authorName": "Мария",
  "rating": 5,
  "comment": "Все отлично",
  "createdAt": "2026-03-22T09:00:00Z"
}
```

### ChatListItemDto

```json
{
  "id": 1,
  "interlocutorName": "Мария",
  "advertisementTitle": "Учебник",
  "lastMessage": "Здравствуйте",
  "lastMessageAt": "2026-03-22T09:00:00Z",
  "unreadCount": 2
}
```

### ChatDto

```json
{
  "id": 1,
  "advertisementId": 10,
  "advertisementTitle": "Учебник",
  "buyerId": 2,
  "buyerName": "Мария",
  "sellerId": 1,
  "sellerName": "Иван",
  "messages": []
}
```

### MessageDto

```json
{
  "id": 1,
  "senderId": 2,
  "senderName": "Мария",
  "content": "Здравствуйте",
  "isRead": false,
  "createdAt": "2026-03-22T09:00:00Z"
}
```

### NotificationDto

```json
{
  "id": 1,
  "type": "order",
  "title": "Новый заказ",
  "content": "По вашему объявлению создан заказ",
  "isRead": false,
  "createdAt": "2026-03-22T09:00:00Z"
}
```

### AdminStatsDto

```json
{
  "id": 1,
  "fullName": "Иван Петров",
  "email": "ivan@university.ru",
  "role": "user",
  "createdAt": "2026-03-22T09:00:00Z",
  "isBlocked": false
}
```

### UserAdminDto

```json
{
  "totalUsers": 100,
  "activeAdvertisements": 45,
  "completedOrders": 20,
  "blockedUsers": 3
}
```

## Эндпоинты, они же адреса куда отправлять запросы

Разбиты по контроллерам, чтобы я потом мог хоть что-то найти если не будет работать

## 1. Auth

### GET `/api/auth/csrf`

- Auth: no
- CSRF: no
- Response `200`:

```json
{
  "token": "csrf-token-value"
}
```

### POST `/api/auth/login`

- Auth: no
- CSRF: yes
- Body:

```json
{
  "email": "user@university.ru",
  "password": "secret123"
}
```

- Response `200`: `AuthResponseDto`
- Response `401`: `AuthResponseDto` with `isSuccess = false`

### POST `/api/auth/register`

- Auth: no
- CSRF: yes
- Body:

```json
{
  "email": "user@university.ru",
  "password": "secret123",
  "confirmPassword": "secret123",
  "fullName": "Иванов Иван Иванович",
  "phone": "+79991234567",
  "university": "МГУ",
  "faculty": "ФКН"
}
```

- Response `200`: `AuthResponseDto`
- Response `400`: `AuthResponseDto` with `errors`

### POST `/api/auth/forgot-password`

- Auth: no
- CSRF: yes
- Body:

```json
{
  "email": "user@university.ru"
}
```

- Response `204`

### POST `/api/auth/reset-password`

- Auth: no
- CSRF: yes
- Body:

```json
{
  "token": "reset-token",
  "newPassword": "newSecret123",
  "confirmPassword": "newSecret123"
}
```

- Response `204`
- Response `400`:

```json
{
  "message": "Passwords do not match"
}
```

### POST `/api/auth/logout`

- Auth: yes
- CSRF: yes
- Response `204`

### GET `/api/auth/me`

- Auth: yes
- CSRF: no
- Response `200`: `AuthResponseDto`
- Response `401`: `AuthResponseDto`

## 2. Users

### GET `/api/users/me`

- Auth: yes
- CSRF: no
- Response `200`: `UserProfileDto`

### PUT `/api/users/me`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "fullName": "Иванов Иван",
  "phone": "+79991234567",
  "faculty": "ФКН"
}
```

- Response `200`: `UserProfileDto`

### GET `/api/users/{userId}`

- Auth: no
- CSRF: no
- Response `200`: `PublicUserProfileDto`

## 3. Advertisements

### GET `/api/advertisements`

- Auth: no
- CSRF: no
- Query params:
  - `search?: string`
  - `course?: number`
  - `type?: string`
  - `sortBy?: string`

- Supported `sortBy`:
  - `price_asc`
  - `price_desc`
  - `date_asc`
  - default: newest first

- Response `200`: `AdvertisementCardDto[]`

### GET `/api/advertisements/{id}`

- Auth: no
- CSRF: no
- Response `200`: `AdvertisementDto`
- Response `404`:

```json
{
  "message": "Advertisement not found"
}
```

### GET `/api/advertisements/my`

- Auth: yes
- CSRF: no
- Query params:
  - `status?: string`

- Response `200`: `MyAdvertisementDto[]`

### POST `/api/advertisements`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "title": "Учебник по математике",
  "course": 2,
  "type": "book",
  "description": "Хорошее состояние",
  "price": 1500,
  "location": "Главный корпус"
}
```

- Response `200`: `AdvertisementDto`

### PUT `/api/advertisements/{id}`

- Auth: yes
- CSRF: yes
- Body same as create
- Response `200`: `AdvertisementDto`

### POST `/api/advertisements/{id}/archive`

- Auth: yes
- CSRF: yes
- Response `204`

### DELETE `/api/advertisements/{id}`

- Auth: yes
- CSRF: yes
- Response `204`

## 4. Favorites

### GET `/api/favorites`

- Auth: yes
- CSRF: no
- Response `200`: `AdvertisementCardDto[]`

### POST `/api/favorites/{advertisementId}`

- Auth: yes
- CSRF: yes
- Response `204`

### DELETE `/api/favorites/{advertisementId}`

- Auth: yes
- CSRF: yes
- Response `204`

### GET `/api/favorites/{advertisementId}/exists`

- Auth: yes
- CSRF: no
- Response `200`:

```json
{
  "exists": true
}
```

## 5. Cart

### GET `/api/cart`

- Auth: yes
- CSRF: no
- Response `200`: `CartDto`

### POST `/api/cart/items/{advertisementId}`

- Auth: yes
- CSRF: yes
- Response `204`

### DELETE `/api/cart/items/{advertisementId}`

- Auth: yes
- CSRF: yes
- Response `204`

### DELETE `/api/cart`

- Auth: yes
- CSRF: yes
- Response `204`

## 6. Orders

### POST `/api/orders/from-cart`

- Auth: yes
- CSRF: yes
- Response `200`: `OrderDto`

### POST `/api/orders/single/{advertisementId}`

- Auth: yes
- CSRF: yes
- Response `200`: `OrderDto`

### GET `/api/orders/buyer`

- Auth: yes
- CSRF: no
- Response `200`: `OrderDto[]`

### GET `/api/orders/seller`

- Auth: yes
- CSRF: no
- Response `200`: `OrderDto[]`

### GET `/api/orders/{id}`

- Auth: yes
- CSRF: no
- Response `200`: `OrderDto`

### POST `/api/orders/{id}/complete`

- Auth: yes
- CSRF: yes
- Response `204`

### POST `/api/orders/{id}/cancel`

- Auth: yes
- CSRF: yes
- Response `204`

## 7. Reviews

### GET `/api/reviews/users/{userId}`

- Auth: no
- CSRF: no
- Response `200`: `ReviewDto[]`

### GET `/api/reviews/can-leave?orderId=5`

- Auth: yes
- CSRF: no
- Response `200`:

```json
{
  "canLeave": true
}
```

### POST `/api/reviews`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "orderId": 5,
  "rating": 5,
  "comment": "Все прошло отлично"
}
```

- Response `200`: `ReviewDto`

## 8. Chats

### GET `/api/chats`

- Auth: yes
- CSRF: no
- Response `200`: `ChatListItemDto[]`

### GET `/api/chats/{id}`

- Auth: yes
- CSRF: no
- Response `200`: `ChatDto`

### POST `/api/chats/or-create/{advertisementId}`

- Auth: yes
- CSRF: yes
- Response `200`: `ChatDto`

### POST `/api/chats/messages`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "chatId": 1,
  "content": "Здравствуйте"
}
```

- Response `200`: `MessageDto`

### POST `/api/chats/{chatId}/read`

- Auth: yes
- CSRF: yes
- Response `204`

## 9. Notifications

### GET `/api/notifications`

- Auth: yes
- CSRF: no
- Response `200`: `NotificationDto[]`

### GET `/api/notifications/unread-count`

- Auth: yes
- CSRF: no
- Response `200`:

```json
{
  "unreadCount": 3
}
```

### POST `/api/notifications/{notificationId}/read`

- Auth: yes
- CSRF: yes
- Response `204`

### POST `/api/notifications/read-all`

- Auth: yes
- CSRF: yes
- Response `204`

## 10. Moderation

- НЕ РЕАЛИЗОВЫВАТЬ, БУДЕТ ПЕРЕРАБАТЫВАТЬСЯ И МЕНЯТЬСЯ, ОЧЕНЬ СЫРОЙ
- Предназначен для модератора. 
- Технически endpoint group в настоящее время требуется только аутентифицированный пользовательский файл cookie. Потом будет переработан, поэтому пока просто для галочки. 

### GET `/api/moderation/pending`

- Auth: yes
- CSRF: no
- Response `200`: `ModerationAdvertisementDto[]`

### POST `/api/moderation/{advertisementId}/approve`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "comment": "Все ок"
}
```

- Response `204`

### POST `/api/moderation/{advertisementId}/reject`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "comment": "Нарушены правила"
}
```

- Response `204`
- `comment` is required

### POST `/api/moderation/{advertisementId}/revision`

- Auth: yes
- CSRF: yes
- Body:

```json
{
  "comment": "Добавьте описание и фото"
}
```

- Response `204`
- `comment` is required

## 11. Admin

- ТОЖЕ САМОЕ КАК И В ПРОШЛОМ
- Предназначен для администратора.
- Технически endpoint group в настоящее время требуется только аутентифицированный пользовательский файл cookie.

### GET `/api/admin/users`

- Auth: yes
- CSRF: no
- Query params:
  - `search?: string`
  - `isBlocked?: boolean`

- Response `200`: `AdminStatsDto[]`

### POST `/api/admin/users/{userId}/block`

- Auth: yes
- CSRF: yes
- Response `204`

### POST `/api/admin/users/{userId}/unblock`

- Auth: yes
- CSRF: yes
- Response `204`

### GET `/api/admin/stats`

- Auth: yes
- CSRF: no
- Response `200`: `UserAdminDto`

### GET `/api/admin/export/csv`

- Auth: yes
- CSRF: no
- Response `200`: string with CSV content

### GET `/api/admin/export/json`

- Auth: yes
- CSRF: no
- Response `200`: string with JSON export content

## Запрос DTOs

### LoginRequestDto

```json
{
  "email": "user@university.ru",
  "password": "secret123"
}
```

### RegisterRequestDto

```json
{
  "email": "user@university.ru",
  "password": "secret123",
  "confirmPassword": "secret123",
  "fullName": "Иванов Иван Иванович",
  "phone": "+79991234567",
  "university": "МГУ",
  "faculty": "ФКН"
}
```

### ForgotPasswordRequestDto

```json
{
  "email": "user@university.ru"
}
```

### ResetPasswordRequestDto

```json
{
  "token": "reset-token",
  "newPassword": "newSecret123",
  "confirmPassword": "newSecret123"
}
```

### CreateAdvertisementDto / UpdateAdvertisementDto

```json
{
  "title": "Учебник по математике",
  "course": 2,
  "type": "book",
  "description": "Хорошее состояние",
  "price": 1500,
  "location": "Главный корпус"
}
```

### UpdateUserProfileDto

```json
{
  "fullName": "Иванов Иван",
  "phone": "+79991234567",
  "faculty": "ФКН"
}
```

### CreateReviewDto

```json
{
  "orderId": 5,
  "rating": 5,
  "comment": "Все прошло отлично"
}
```

### SendMessageDto

```json
{
  "chatId": 1,
  "content": "Здравствуйте"
}
```

### ModerationDecisionDto

```json
{
  "comment": "Комментарий модератора"
}
```

## Frontend integration notes

## Замечания по интеграции с интерфейсом

- CORS в настоящее время разрешает запросы из `http://localhost:3000`. Поэтому даже прописанные запросы не будут работать, пока что.. Потом перенастрою под готовую песочницу, чтобы функционально проверить работспособность