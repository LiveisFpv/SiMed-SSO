# Руководство по интеграции клиента

Этот документ описывает базовую интеграцию клиентского приложения с SiMed SSO.

Готовый server-rendered шаблон с локальными пользователями, собственной БД и входом через SiMed SSO находится в [SampleClient](sample-client.md).

## 1. Зарегистрируйте client

1. Войдите в SiMed SSO под пользователем с ролью `Admin`.
2. Откройте `/Admin/Clients`.
3. Создайте confidential client.
4. Укажите display name, redirect URIs, scopes и PKCE requirement.
5. Скопируйте `Client ID` и `Client secret` сразу после создания.

Client secret показывается только один раз. В клиентском приложении храните secret в secret storage, а не в frontend code или git.

## 2. Настройте redirect URI

Redirect URI должен совпадать полностью:

- scheme;
- host;
- port;
- path;
- trailing slash, если он есть.

Для локальной проверки удобно использовать:

```text
http://localhost:3000/callback
```

Wildcard redirect URIs не поддерживаются.

## 3. Используйте PKCE

Создайте `code_verifier`: случайную строку 43-128 символов.

Создайте `code_challenge`:

```text
BASE64URL(SHA256(code_verifier))
```

В authorize request передавайте:

```text
code_challenge=...
code_challenge_method=S256
```

В token request передавайте исходный `code_verifier`.

## 4. Запустите Authorization Code Flow

Откройте authorize URL в браузере пользователя:

```text
https://localhost:7269/connect/authorize?client_id=CLIENT_ID&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A3000%2Fcallback&scope=openid%20profile%20email%20offline_access&code_challenge=CODE_CHALLENGE&code_challenge_method=S256&state=OPAQUE_STATE
```

Пользователь войдет в SiMed SSO, пройдет MFA при необходимости и подтвердит consent.

После accept сервер перенаправит браузер на `redirect_uri`:

```text
http://localhost:3000/callback?code=AUTHORIZATION_CODE&state=OPAQUE_STATE&iss=https%3A%2F%2Flocalhost%3A7269%2F
```

Клиент должен проверить `state`, затем обменять `code` на tokens.

## 5. Обменяйте code на tokens

```powershell
curl.exe -k -X POST https://localhost:7269/connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=authorization_code" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "redirect_uri=http://localhost:3000/callback" `
  -d "code=AUTHORIZATION_CODE" `
  -d "code_verifier=CODE_VERIFIER"
```

Authorization code одноразовый. Повторный token request с тем же code вернет `invalid_grant`.

## 6. Используйте tokens

- `id_token`: identity token для клиента; проверяйте подпись, `iss`, `aud`, `exp`.
- `access_token`: bearer token для UserInfo и будущих resource APIs.
- `refresh_token`: используется только backend-частью confidential client.

Не храните refresh token в browser localStorage.

## 7. Refresh token rotation

Refresh request:

```powershell
curl.exe -k -X POST https://localhost:7269/connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=refresh_token" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "refresh_token=REFRESH_TOKEN"
```

После успешного ответа замените старый refresh token новым. Старый token считайте недействительным.

## 8. Получите UserInfo

```powershell
curl.exe -k https://localhost:7269/connect/userinfo `
  -H "Authorization: Bearer ACCESS_TOKEN"
```

Минимальный response содержит `sub`. Claims `name`, `preferred_username`, `email`, `email_verified` зависят от granted scopes.

## 9. Revocation и introspection

Отзыв refresh token:

```powershell
curl.exe -k -X POST https://localhost:7269/connect/revocation `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "token=REFRESH_TOKEN"
```

Проверка token introspection:

```powershell
curl.exe -k -X POST https://localhost:7269/connect/introspection `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "token=ACCESS_OR_REFRESH_TOKEN"
```

Logout в клиентском приложении очищает только локальную cookie клиента. SSO-сессия SiMed SSO остается активной, как в типичных внешних SSO-провайдерах.

## Integration checklist

- `SSO_ISSUER` совпадает с публичным URL сервера.
- Client active в `/Admin/Clients`.
- Redirect URI полностью совпадает с registered URI.
- Requested scopes разрешены для client.
- PKCE использует `S256`.
- Token exchange выполняется backend-частью confidential client.
- Refresh token заменяется после каждого refresh response.
- Client проверяет `state` после redirect.
- Client проверяет `id_token` signature, issuer, audience и expiration.
