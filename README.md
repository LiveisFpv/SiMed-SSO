# SiMed-SSO

SiMed-SSO - SSO-сервис для интеграции внутренних и внешних систем с единой авторизацией. Проект включает регистрацию и вход пользователей, админ-панель для управления пользователями и OAuth/OIDC clients, SMTP-отправку писем, управление сессиями, MFA, OAuth2/OpenID Connect API на базе OpenIddict и отдельный demo-client для проверки интеграции.

## Стек

- .NET 10, C#
- ASP.NET Core Razor Pages
- ASP.NET Core Identity
- Entity Framework Core, Code First
- PostgreSQL
- OpenIddict OAuth2/OpenID Connect
- MailKit SMTP
- Redis, MinIO S3 - запланированы для следующих этапов

## Возможности

- Регистрация и вход по email.
- Подтверждение email и восстановление пароля через SMTP.
- Роли `Admin` и `User`.
- Админ-панель пользователей: список, карточка, роли, деактивация и реактивация.
- Управление cookie-сессиями пользователя.
- MFA через authenticator app, email-коды и recovery codes.
- Реестр OAuth/OIDC clients в админ-панели.
- Authorization Code Flow with PKCE.
- `access_token`, `id_token`, rotating `refresh_token`.
- Discovery metadata, JWKS, UserInfo, revocation и introspection endpoints.
- `SampleClient` как пример интеграции confidential client.

## Документация

- [OIDC quickstart](docs/oidc-quickstart.md) - быстрая ручная проверка discovery, JWKS, authorization code flow, refresh token и UserInfo.
- [API reference](docs/api-reference.md) - публичный OIDC/API contract для интеграторов.
- [Руководство по интеграции клиента](docs/client-integration.md) - регистрация client и интеграция Authorization Code Flow with PKCE.
- [SampleClient](docs/sample-client.md) - запуск demo-client и проверка входа через SiMed SSO.
- [Production OIDC certificates](docs/oidc-production-certificates.md) - настройка signing/encryption certificates для Staging и Production.

## План разработки

- [x] Скелет приложения
- [x] Базовая авторизация и регистрация
- [x] Админ-панель
- [x] SMTP
- [x] Управление сессиями
- [x] Реестр OAuth/OIDC clients
- [x] OAuth2/OIDC Authorization Code Flow
- [x] Discovery, JWKS, UserInfo
- [x] Production-настройка signing/encryption certificates
- [x] MFA
- [x] Подробная документация API
- [x] Русский UI и документация
- [x] Клиентское приложение как пример интеграции
- [ ] Audit log
