# 🫧 Бубнилка - Веб-мессенджер

Веб-мессенджер на C# Blazor с实时 сообщениями через SignalR, похожий на Telegram.

## 🔐 Безопасность

Проект разработан с соблюдением всех современных стандартов безопасности:

### Защита от уязвимостей
- **SQL-инъекции**: Используется Entity Framework Core с параметризованными запросами
- **XSS**: Все пользовательские данные экранируются через `HtmlEncoder`
- **CSRF**: JWT токены с правильной валидацией и ограниченным временем жизни
- **Утечка паролей**: BCrypt хеширование с солью
- **JWT токены**: Валидация issuer, audience, lifetime и signing key
- **CORS**: Настроен на конкретные origins (не используется AllowAnyOrigin)
- **Null reference exceptions**: Включены Nullable reference types

### Дополнительные меры защиты
- Rate limiting для защиты от брутфорса (5 попыток, блокировка на 15 мин)
- Валидация всех входных данных через DataAnnotations
- HTTPS по умолчанию
- Безопасное хранение токенов в localStorage
- Авторизация через JWT Bearer tokens

## 🛠 Технологии

- **.NET 8** - последняя LTS версия
- **Blazor WebAssembly** - клиентская часть
- **ASP.NET Core** - серверная часть
- **SignalR** -实时 сообщения
- **Entity Framework Core** - работа с БД
- **SQLite** - база данных
- **BCrypt.Net** - хеширование паролей
- **JWT** - аутентификация

## 🎨 Цветовая схема

- Основной: `#00B4B4` (бирюзовый)
- Вторичный: `#1A2B2B` (темно-бирюзовый)
- Фон: `#E0F7F7` (светло-бирюзовый)
- Акцент: `#00D4D4` (яркий бирюзовый)

## 📁 Структура проекта

```
Bubnilka.sln
├── Bubnilka.Shared/          # Общие модели и DTO
│   ├── Models/
│   │   ├── User.cs           # Модель пользователя
│   │   ├── Chat.cs           # Модель чата
│   │   └── Message.cs        # Модель сообщения
│   └── DTOs/
│       ├── RegisterRequest.cs
│       ├── LoginRequest.cs
│       ├── AuthResponse.cs
│       └── CreateChatRequest.cs
├── Bubnilka.Server/          # Серверная часть
│   ├── Controllers/
│   │   ├── AuthController.cs  # Авторизация
│   │   └── ChatsController.cs # Чаты и сообщения
│   ├── Hubs/
│   │   └── ChatHub.cs         # SignalR хаб
│   ├── Services/
│   │   └── TokenService.cs    # Генерация JWT
│   ├── Data/
│   │   └── AppDbContext.cs    # EF Core контекст
│   └── Program.cs             # Точка входа
└── Bubnilka.Client/          # Клиентская часть
    ├── Pages/
    │   ├── Login.razor
    │   ├── Register.razor
    │   └── Chats.razor
    ├── Services/
    │   ├── AuthService.cs
    │   ├── ChatService.cs
    │   └── SignalRService.cs
    └── Program.cs
```

## 🚀 Быстрый старт

### Требования
- .NET 8 SDK
- Git

### Установка

1. Клонируйте репозиторий:
```bash
git clone <repository-url>
cd Bubnilka
```

2. Восстановите пакеты:
```bash
dotnet restore
```

3. Запустите сервер:
```bash
cd Bubnilka.Server
dotnet run
```

4. Запустите клиент (в новом терминале):
```bash
cd Bubnilka.Client
dotnet run
```

5. Откройте браузер:
- Клиент: https://localhost:5001 или http://localhost:5000
- Сервер API: https://localhost:5001/api
- Swagger: https://localhost:5001/swagger

## 📝 Использование

### Регистрация
1. Перейдите на страницу регистрации
2. Введите email (должен быть корректным)
3. Придумайте пароль (минимум 8 символов, заглавные, строчные буквы и цифры)
4. Подтвердите пароль
5. Нажмите "Зарегистрироваться"

### Вход
1. Введите email и пароль
2. Нажмите "Войти"
3. При 5 неудачных попытках аккаунт блокируется на 15 минут

### Чаты
1. Найдите пользователя через поиск (минимум 2 символа)
2. Кликните на пользователя для создания чата
3. Пишите сообщения в реальном времени
4. Статус онлайн/офлайн отображается автоматически

## 🔑 Конфигурация

### appsettings.json (Сервер)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=bubnilka.db"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!ChangeInProduction",
    "Issuer": "Bubnilka",
    "Audience": "BubnilkaClient"
  }
}
```

**Важно**: Измените `SecretKey` перед публикацией в продакшен!

## 🧪 Тестирование

### Проверка компиляции
```bash
dotnet build
```

### Проверка всех проектов
```bash
dotnet build Bubnilka.sln
```

## 📦 Публикация

### Server (Production)
```bash
cd Bubnilka.Server
dotnet publish -c Release -o ./publish
```

### Client (WASM)
```bash
cd Bubnilka.Client
dotnet publish -c Release -o ./publish/wwwroot
```

## 🔒 Чеклист безопасности

- [x] Entity Framework вместо сырого SQL
- [x] BCrypt для хеширования паролей
- [x] JWT с валидацией всех параметров
- [x] CORS настроен на конкретные origins
- [x] XSS защита через HtmlEncoder
- [x] Валидация всех входных данных
- [x] Rate limiting для login
- [x] HTTPS redirection
- [x] Nullable reference types включены
- [x] Нет чувствительных данных в ответах API

## 📄 Лицензия

MIT License

## 👥 Авторы

Разработано с помощью Qwen Coder AI

---

**Бубнилка** © 2024 - Общайтесь безопасно! 🫧
