## Обязательные настройки Core

В `Development` часть настроек может отсутствовать. В `Staging`/`Production` приложение стартует fail-fast, если найдены небезопасные placeholders или отсутствуют обязательные значения.

```env
ASPNETCORE_ENVIRONMENT=Production

POSTGRES_HOST=db.example.local
POSTGRES_PORT=5432
POSTGRES_DB=simed_sso
POSTGRES_USER=simed_sso
POSTGRES_PASSWORD=strong-secret

SSO_ISSUER=https://sso.example.com/
SSO_ADMIN_EMAIL=admin@example.com
SSO_ADMIN_PASSWORD=strong-admin-password

DATA_PROTECTION_KEYS_PATH=/var/lib/simed-sso/data-protection-keys

OIDC_SIGNING_CERT_PATH=/etc/simed-sso/certs/oidc-signing.pfx
OIDC_SIGNING_CERT_PASSWORD=strong-signing-password
OIDC_ENCRYPTION_CERT_PATH=/etc/simed-sso/certs/oidc-encryption.pfx
OIDC_ENCRYPTION_CERT_PASSWORD=strong-encryption-password

SSO_REQUIRE_EMAIL_VERIFICATION=true
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USERNAME=sso@example.com
SMTP_PASSWORD=strong-smtp-password
FROM_EMAIL=sso@example.com
FROM_NAME=SiMed SSO
```

`SSO_ISSUER` должен быть публичным HTTPS URL сервера. Он должен совпадать с `issuer` в discovery metadata и `iss` в `id_token`.

## Data Protection keys

`DATA_PROTECTION_KEYS_PATH` обязателен вне `Development`. Эта папка хранит ключи ASP.NET Core Data Protection, которыми защищаются auth-cookie, antiforgery и Identity tokens.

Папка должна:

- сохраняться между рестартами;
- быть доступной на чтение/запись пользователю приложения;
- быть общей для всех инстансов одного приложения, если их несколько.

Для `SampleClient` используется отдельная переменная:

```env
SAMPLECLIENT_DATA_PROTECTION_KEYS_PATH=/var/lib/simed-sampleclient/data-protection-keys
```

## Reverse proxy

Если приложение работает за nginx/Caddy/Traefik/load balancer, укажите доверенные proxy:

```env
TRUSTED_PROXIES=10.0.0.10,10.0.0.11
TRUSTED_NETWORKS=10.0.0.0/24
```

Приложение принимает `X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host` только от этих IP/сетей. Если reverse proxy нет, переменные можно не задавать.

## Health checks

Core:

```text
GET /health/live
GET /health/ready
```

SampleClient:

```text
GET /health/live
GET /health/ready
```

`/health/live` проверяет, что процесс отвечает. `/health/ready` проверяет подключение к БД, pending EF migrations и production-зависимости. Если есть pending migrations, ready endpoint возвращает unhealthy: миграции нужно применять вручную или через CI/CD до запуска новой версии.

## Rate limiting

Включены лимиты на auth и OIDC endpoints:

- login/register/forgot/reset/MFA/resend confirmation;
- `/connect/authorize`;
- `/connect/token`, `/connect/revocation`, `/connect/introspection`.

При превышении лимита возвращается `429 Too Many Requests`.

## SampleClient production SMTP

В `Development` SampleClient может логировать confirmation/reset links. В `Staging`/`Production` SMTP обязателен:

```env
SAMPLECLIENT_SMTP_HOST=smtp.example.com
SAMPLECLIENT_SMTP_PORT=587
SAMPLECLIENT_SMTP_USERNAME=sampleclient@example.com
SAMPLECLIENT_SMTP_PASSWORD=strong-smtp-password
SAMPLECLIENT_FROM_EMAIL=sampleclient@example.com
SAMPLECLIENT_FROM_NAME=SiMed SampleClient
```

## Smoke test перед релизом

1. Применить EF migrations для Core и SampleClient.
2. Запустить приложения с `ASPNETCORE_ENVIRONMENT=Production`.
3. Проверить `/health/live` и `/health/ready`.
4. Проверить discovery: `/.well-known/openid-configuration`.
5. Проверить JWKS: `/.well-known/jwks`.
6. Выполнить Authorization Code Flow with PKCE.
7. Проверить login/register/password reset/MFA.
8. Перезапустить приложение и убедиться, что существующая cookie не стала недействительной из-за потери Data Protection keys.
