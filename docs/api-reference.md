# API reference

Этот документ описывает публичный OIDC/OAuth2 contract SiMed SSO для интеграторов клиентских систем.

Админ-панель и account pages являются server-rendered Razor Pages и не считаются публичным API. Для машинной проверки OIDC используйте discovery metadata.

## Base URL и issuer

Локальный HTTPS issuer:

```text
https://localhost:7269/
```

В production значение `SSO_ISSUER` должно совпадать с публичным URL сервера. `issuer` в discovery metadata и claim `iss` в `id_token` должны совпадать.

## Discovery

```http
GET /.well-known/openid-configuration
```

Discovery возвращает актуальные endpoint URLs, supported scopes, grant types и response types.

Ключевые поля:

- `issuer`
- `authorization_endpoint`
- `token_endpoint`
- `userinfo_endpoint`
- `revocation_endpoint`
- `introspection_endpoint`
- `jwks_uri`
- `scopes_supported`
- `response_types_supported`
- `grant_types_supported`

## JWKS

```http
GET /.well-known/jwks
```

JWKS возвращает публичные signing keys. Клиенты используют эти ключи для проверки подписи `id_token`.

## Authorization endpoint

```http
GET /connect/authorize
```

Поддерживается Authorization Code Flow with PKCE.

Параметры query string:

| Parameter | Required | Description |
| --- | --- | --- |
| `client_id` | yes | Client ID из `/Admin/Clients`. |
| `response_type` | yes | Только `code`. |
| `redirect_uri` | yes | Должен точно совпадать с registered redirect URI. |
| `scope` | yes | Scopes через пробел. |
| `code_challenge` | yes | Base64url SHA-256 hash от `code_verifier`. |
| `code_challenge_method` | yes | Только `S256`. |
| `state` | no | Opaque client state, возвращается клиенту. |

Если пользователь не вошел, сервер перенаправит его на `/Account/Login`, затем вернет на consent page.

После accept сервер делает redirect на `redirect_uri` с параметром `code`.

## Token endpoint

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded
```

### Authorization code exchange

Поля form body:

| Field | Required | Description |
| --- | --- | --- |
| `grant_type` | yes | `authorization_code`. |
| `client_id` | yes | Client ID. |
| `client_secret` | yes | Client secret. |
| `redirect_uri` | yes | Тот же redirect URI, что был в authorize request. |
| `code` | yes | Authorization code. Одноразовый. |
| `code_verifier` | yes | Исходный verifier для PKCE. |

Ожидаемый ответ:

```json
{
  "access_token": "...",
  "id_token": "...",
  "refresh_token": "...",
  "token_type": "Bearer",
  "expires_in": 3599,
  "scope": "openid profile email offline_access"
}
```

### Refresh token

Поля form body:

| Field | Required | Description |
| --- | --- | --- |
| `grant_type` | yes | `refresh_token`. |
| `client_id` | yes | Client ID. |
| `client_secret` | yes | Client secret. |
| `refresh_token` | yes | Последний refresh token. |

Refresh tokens ротируются. После каждого успешного refresh request клиент должен заменить старый `refresh_token` новым значением из ответа.

## UserInfo endpoint

```http
GET /connect/userinfo
Authorization: Bearer ACCESS_TOKEN
```

Response claims зависят от scopes:

| Claim | Scope | Description |
| --- | --- | --- |
| `sub` | always | User ID. |
| `name` | `profile` | Display/user name. |
| `preferred_username` | `profile` | Preferred username. |
| `email` | `email` | User email. |
| `email_verified` | `email` | Email confirmation state. |

Inactive/deleted users отклоняются.

## Revocation endpoint

```http
POST /connect/revocation
Content-Type: application/x-www-form-urlencoded
```

Поля form body:

| Field | Required | Description |
| --- | --- | --- |
| `client_id` | yes | Client ID. |
| `client_secret` | yes | Client secret. |
| `token` | yes | Token для отзыва, обычно `refresh_token`. |
| `token_type_hint` | no | Например `refresh_token`. |

После успешного ответа token считается недействительным. Повторный отзыв уже недействительного token не должен раскрывать лишние детали клиенту.

## Introspection endpoint

```http
POST /connect/introspection
Content-Type: application/x-www-form-urlencoded
```

Поля form body:

| Field | Required | Description |
| --- | --- | --- |
| `client_id` | yes | Client ID. |
| `client_secret` | yes | Client secret. |
| `token` | yes | Token для проверки. |
| `token_type_hint` | no | Например `access_token` или `refresh_token`. |

Ключевое поле ответа:

```json
{
  "active": true
}
```

Для отозванного, просроченного или неверного token возвращается `active=false`.

## Scopes

| Scope | Description |
| --- | --- |
| `openid` | Required for OIDC and `id_token`. |
| `profile` | Enables basic profile claims. |
| `email` | Enables email claims. |
| `offline_access` | Enables refresh token issuance. |

Scopes должны быть разрешены для client в `/Admin/Clients`.

## Token lifetimes

| Token | Lifetime |
| --- | --- |
| Authorization code | 5 minutes |
| Access token | 1 hour |
| Refresh token | 30 days |

## Common errors

| Error | Typical cause |
| --- | --- |
| `invalid_request` | Missing `client_id`, invalid/missing parameter, malformed request. |
| `invalid_grant` | Reused/expired code, wrong `code_verifier`, invalid refresh token. |
| `invalid_scope` | Requested scope is not allowed for the client. |
| `unauthorized_client` | Client is inactive or not allowed for the requested flow. |
| `invalid_client` | Неверная client authentication на token/revocation/introspection endpoint. |

## OpenAPI stub

Development Swagger UI:

```text
https://localhost:7269/swagger
```

Raw OpenAPI YAML in Development:

```text
https://localhost:7269/openapi/simed-sso.yaml
```

Swagger UI намеренно доступен только в `Development`. Для production-интеграции используйте discovery metadata и Markdown-документацию в репозитории.
