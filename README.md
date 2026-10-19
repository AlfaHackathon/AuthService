# AuthService

Сервис авторизации кластера AlfaHackathon: выпуск и обновление JWT-токенов. .NET 10, пакеты `AlfaHackaton.Auth` (TokenBuilder, JwtSettings) и `AlfaHackaton.Controllers`.

Пользователи хранятся в user-service — auth-service ходит в него по HTTP (internal-ручки с секретом `X-Internal-Secret`).

## Ручки

| Метод | Путь | Тело | Ответ |
|---|---|---|---|
| POST | `/auth-service/login` | `{email, password}` | `{accessToken, refreshToken}` |
| POST | `/auth-service/register` | `{email, password, lastName, firstName, middleName?}` | `{accessToken, refreshToken}` |
| POST | `/auth-service/refresh` | `{refreshToken}` | новая `{accessToken, refreshToken}` |

## Как устроено

- Пароли: **bcrypt** (`BCrypt.Net-Next`), хэширует auth-service, хранит user-service.
- Token claims: `UserId` (Guid), роль (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role` → `User`), `token_type` (`access`/`refresh` — access нельзя подсунуть в refresh).
- `JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear()` — чтобы claim роли не переименовывался в короткую форму, иначе `TokenParser` гейтвея его не увидит.
- Register: проверка занятости email (`exists-by-email`) → создание юзера → пара токенов. Повторный email — `409`.

## Конфигурация

- Сроки: `Jwt__AccessTokenExpiryInMinutes` (по умолчанию 7 дней), `Jwt__RefreshTokenExpiryInMinutes` (1 день, короче access).
- Адрес user-service: `Services__UserService`.
- Swagger UI: `/auth-service/swagger` (включается флагом `Swagger__Enabled`).
- Запуск — из папки `../cluster`: `docker compose up -d --build`.
