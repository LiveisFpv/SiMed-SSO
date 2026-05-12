# SiMed-SSO

SiMed-SSO — SSO-сервис для интеграции внутренних и внешних систем с единой авторизацией. Проект включает регистрацию и вход пользователей, админ-панель для управления пользователями и OAuth/OIDC-клиентами, SMTP-отправку писем, управление сессиями и OAuth2/OpenID Connect API на базе OpenIddict.

## Стек

- .NET 10, C#
- ASP.NET Core Razor Pages
- ASP.NET Core Identity
- Entity Framework Core, Code First
- PostgreSQL
- OpenIddict OAuth2/OpenID Connect
- MailKit SMTP
- Redis, MinIO S3 — запланированы для следующих этапов

## Возможности

- Регистрация и вход по email.
- Подтверждение email и восстановление пароля через SMTP.
- Роли `Admin` и `User`.
- Админ-панель пользователей: список, карточка, роли, деактивация/реактивация.
- Управление cookie-сессиями пользователя.
- Реестр OAuth/OIDC-клиентов в админ-панели.
- Authorization Code Flow with PKCE.
- `access_token`, `id_token`, rotating `refresh_token`.
- Discovery metadata, JWKS и UserInfo endpoint.

## Документация

- [OIDC quickstart](docs/oidc-quickstart.md) — проверка discovery, JWKS, authorization code flow, refresh token и UserInfo.
- [Production OIDC certificates](docs/oidc-production-certificates.md) — настройка signing/encryption certificates для Staging и Production.

## План разработки

- [x] Скелет приложения
- [x] Базовая авторизация и регистрация
- [x] Админ-панель
- [x] SMTP
- [x] Управление сессиями
- [x] Реестр OAuth/OIDC-клиентов
- [x] OAuth2/OIDC Authorization Code Flow
- [x] Discovery, JWKS, UserInfo
- [x] Production-настройка signing/encryption certificates
- [x] MFA
- [ ] Audit log
- [ ] Подробная документация API
- [ ] Клиентское приложение как пример интеграции
