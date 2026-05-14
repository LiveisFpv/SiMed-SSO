# SampleClient

`SampleClient` - отдельное demo-приложение на ASP.NET Core Razor Pages. Оно показывает интеграцию confidential client с SiMed SSO через Authorization Code Flow with PKCE.

Корневой `.env` относится только к основному SSO server. Настройки demo-client лежат отдельно в `SampleClient/.env`.

## 1. Создайте OAuth client

1. Запустите SiMed SSO.
2. Войдите под пользователем с ролью `Admin`.
3. Откройте `/Admin/Clients`.
4. Создайте OAuth client со следующими параметрами:
   - Redirect URI: `https://localhost:7290/signin-oidc`
   - Scopes: `openid profile email offline_access`
   - Require PKCE: enabled
   - Active: yes
5. Скопируйте `Client ID` и `Client secret` сразу после создания.

`Client secret` показывается только один раз. Если secret потерян, используйте rotate secret в карточке client.

## 2. Настройте `SampleClient/.env`

Скопируйте `SampleClient/.env.example` в `SampleClient/.env` и подставьте значения созданного OAuth client:

```env
SAMPLECLIENT_AUTHORITY=https://localhost:7269/
SAMPLECLIENT_CLIENT_ID=simed_replace_with_client_id
SAMPLECLIENT_CLIENT_SECRET=replace-with-client-secret
SAMPLECLIENT_CALLBACK_PATH=/signin-oidc
```

`SAMPLECLIENT_AUTHORITY` должен совпадать с `SSO_ISSUER` и discovery `issuer` основного SSO-сервера.

Если `SampleClient/.env` отсутствует, приложение также может читать эти значения из обычных environment variables.

## 3. Запустите приложения

SSO:

```powershell
dotnet run --project Core\Core.csproj --launch-profile https
```

SampleClient:

```powershell
dotnet run --project SampleClient\SampleClient.csproj --launch-profile https
```

Откройте:

```text
https://localhost:7290/
```

Нажмите `Войти через SiMed SSO`, пройдите вход, MFA при необходимости и consent. После redirect откройте `/Profile`.

## Что показывает `/Profile`

- Claims из auth-cookie demo-client.
- Masked summary для `access_token`, `id_token`, `refresh_token`.
- `expires_at` для access token.
- Ответ `/connect/userinfo`, полученный backend-side через `access_token`.

Полные значения tokens намеренно не выводятся в UI.

## Logout

Кнопка выхода очищает только cookie `SampleClient`. SSO-сессия в SiMed SSO остается активной, поэтому повторный вход может пройти без ввода пароля, если SSO-cookie еще действительна.

## Типичные ошибки

- `redirect_uri` mismatch: redirect URI в `/Admin/Clients` должен быть ровно `https://localhost:7290/signin-oidc`.
- `invalid_client`: неверный `SAMPLECLIENT_CLIENT_ID` или `SAMPLECLIENT_CLIENT_SECRET`.
- `invalid_scope`: в client не разрешены все scopes `openid profile email offline_access`.
- Ошибка TLS/dev certificate: выполните `dotnet dev-certs https --trust`.
- Приложение не стартует: задайте `SAMPLECLIENT_CLIENT_ID` и `SAMPLECLIENT_CLIENT_SECRET` в `SampleClient/.env` или environment variables.
