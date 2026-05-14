# OIDC quickstart

Этот документ описывает ручную проверку OpenID Connect/OAuth2-интеграции SiMed-SSO через `curl.exe` и браузер.

## Локальные URL

HTTPS issuer для локальной разработки:

```text
https://localhost:7269/
```

Значение `SSO_ISSUER` в `.env` должно совпадать с публичным URL сервера, который используют клиенты. Если приложение запущено на другом host/port, обновите `SSO_ISSUER`.

Discovery metadata:

```powershell
curl.exe -k https://localhost:7269/.well-known/openid-configuration
```

JWKS:

```powershell
curl.exe -k https://localhost:7269/.well-known/jwks
```

UserInfo:

```powershell
curl.exe -k https://localhost:7269/connect/userinfo `
  -H "Authorization: Bearer ACCESS_TOKEN"
```

## Создание client

1. Войдите под администратором.
2. Откройте `/Admin/Clients`.
3. Создайте confidential client.

Рекомендуемый redirect URI для локальной проверки:

```text
http://localhost:3000/callback
```

Выберите scopes:

```text
openid profile email offline_access
```

Скопируйте `Client ID` и `Client secret` сразу после создания. Secret показывается только один раз.

## Authorization Code Flow with PKCE

Создайте `code_verifier` и `code_challenge`:

```powershell
$verifier = "test_verifier_1234567890123456789012345678901234567890123"
$bytes = [Text.Encoding]::ASCII.GetBytes($verifier)
$sha256 = [Security.Cryptography.SHA256]::Create()
$hash = $sha256.ComputeHash($bytes)
$challenge = [Convert]::ToBase64String($hash).TrimEnd('=').Replace('+','-').Replace('/','_')
$challenge
```

Откройте authorize URL в браузере:

```text
https://localhost:7269/connect/authorize?client_id=CLIENT_ID&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A3000%2Fcallback&scope=openid%20profile%20email%20offline_access&code_challenge=CODE_CHALLENGE&code_challenge_method=S256
```

После подтверждения consent браузер перейдет на redirect URI. Скопируйте только значение параметра `code`.

Пример:

```text
http://localhost:3000/callback?code=AUTHORIZATION_CODE&iss=https%3A%2F%2Flocalhost%3A7269%2F
```

Использовать нужно только:

```text
AUTHORIZATION_CODE
```

## Обмен code на tokens

```powershell
curl.exe -k -X POST https://localhost:7269/connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=authorization_code" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "redirect_uri=http://localhost:3000/callback" `
  -d "code=AUTHORIZATION_CODE" `
  -d "code_verifier=test_verifier_1234567890123456789012345678901234567890123"
```

Ожидаемые поля ответа:

```text
access_token
id_token
refresh_token
token_type
expires_in
scope
```

## Refresh token

Refresh tokens ротируются. После каждого refresh-запроса используйте новый `refresh_token` из ответа.

```powershell
curl.exe -k -X POST https://localhost:7269/connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=refresh_token" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "refresh_token=REFRESH_TOKEN"
```

## Revocation и introspection

Отзовите refresh token:

```powershell
curl.exe -k -X POST https://localhost:7269/connect/revocation `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "token=REFRESH_TOKEN"
```

Проверьте token через introspection:

```powershell
curl.exe -k -X POST https://localhost:7269/connect/introspection `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "client_id=CLIENT_ID" `
  -d "client_secret=CLIENT_SECRET" `
  -d "token=ACCESS_OR_REFRESH_TOKEN"
```

Для отозванного или недействительного token ожидайте `active=false`.

## UserInfo

Вызовите UserInfo с `access_token`:

```powershell
curl.exe -k https://localhost:7269/connect/userinfo `
  -H "Authorization: Bearer ACCESS_TOKEN"
```

При scope `profile` ответ содержит `name` и `preferred_username`. При scope `email` ответ содержит `email` и `email_verified`.

## Частые ошибки

- `invalid_request`, отсутствует `client_id`: начните browser flow заново с полного authorize URL.
- `invalid_grant`, неверный `code_verifier`: получите новый authorization code с той же парой verifier/challenge.
- `invalid_grant`, token no longer valid: authorization code одноразовый, начните flow заново.
- `invalid_request`, redirect URI mismatch: `redirect_uri` должен точно совпадать с URI в настройках client.
- issuer mismatch: значения `.env` `SSO_ISSUER`, discovery `issuer` и token `iss` должны совпадать.
