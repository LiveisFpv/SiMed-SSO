# SampleClient

`SampleClient` - отдельное Razor Pages приложение-шаблон для интеграции с SiMed SSO. У него есть собственная PostgreSQL БД, локальные пользователи ASP.NET Core Identity, вход по email/password и вход через SiMed SSO как внешний OpenID Connect provider.

Корневой `.env` относится только к основному SiMed SSO server. Настройки шаблона лежат отдельно в `SampleClient/.env`.

## Что умеет шаблон

- Локальная регистрация и вход по email/password.
- Опциональное подтверждение email для локальных пользователей.
- Восстановление локального пароля через email reset link.
- Вход через SiMed SSO по Authorization Code Flow with PKCE.
- Автоматическое создание локального пользователя при первом SSO-входе.
- Автоматическая привязка SSO к существующему локальному пользователю, если email от SSO подтвержден.
- Хранение `access_token`, `id_token`, `refresh_token` в ASP.NET Core Identity external tokens.
- Профиль с локальными данными пользователя, linked SSO status, masked token summary и backend-side вызовом `/connect/userinfo`.

Полные значения tokens намеренно не выводятся в UI.

## 1. Подготовьте OAuth client в SiMed SSO

1. Запустите SiMed SSO.
2. Войдите под пользователем с ролью `Admin`.
3. Откройте `/Admin/Clients`.
4. Создайте OAuth client:
   - Redirect URI: `https://localhost:7290/signin-oidc`
   - Scopes: `openid profile email offline_access`
   - Require PKCE: enabled
   - Active: yes
5. Скопируйте `Client ID` и `Client secret` сразу после создания.

`Client secret` показывается только один раз. Если secret потерян, используйте rotate secret в карточке client.

## 2. Настройте `SampleClient/.env`

Скопируйте `SampleClient/.env.example` в `SampleClient/.env` и подставьте значения:

```env
SAMPLECLIENT_AUTHORITY=https://localhost:7269/
SAMPLECLIENT_CLIENT_ID=simed_replace_with_client_id
SAMPLECLIENT_CLIENT_SECRET=replace-with-client-secret
SAMPLECLIENT_CALLBACK_PATH=/signin-oidc

SAMPLECLIENT_DATA_PROTECTION_KEYS_PATH=/var/lib/simed-sampleclient/data-protection-keys

SAMPLECLIENT_POSTGRES_HOST=localhost
SAMPLECLIENT_POSTGRES_PORT=5432
SAMPLECLIENT_POSTGRES_DATABASE=simed_sso_sample_client
SAMPLECLIENT_POSTGRES_USERNAME=postgres
SAMPLECLIENT_POSTGRES_PASSWORD=postgres

SAMPLECLIENT_REQUIRE_EMAIL_VERIFICATION=false

SAMPLECLIENT_SMTP_HOST=smtp.gmail.com
SAMPLECLIENT_SMTP_PORT=587
SAMPLECLIENT_SMTP_USERNAME=user@mail.com
SAMPLECLIENT_SMTP_PASSWORD=userpass
SAMPLECLIENT_FROM_EMAIL=sampleclient@mail.com
SAMPLECLIENT_FROM_NAME=SiMed SampleClient
```

`SAMPLECLIENT_AUTHORITY` должен совпадать с `SSO_ISSUER` и discovery `issuer` основного SSO-сервера.

Если `SampleClient/.env` отсутствует, приложение также может читать значения из обычных environment variables. Для строки подключения можно вместо `SAMPLECLIENT_POSTGRES_*` задать `ConnectionStrings__SampleClient`.

## 3. Поднимите БД SampleClient

Через отдельный compose-файл:

```powershell
docker compose --env-file SampleClient\.env -f SampleClient\docker-compose.yml up -d
```

Затем примените миграции:

```powershell
dotnet ef database update --project SampleClient --startup-project SampleClient
```

Миграция `InitialSampleClientIdentity` создает стандартные таблицы ASP.NET Core Identity в отдельной БД SampleClient.

## 4. Запустите приложения

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

## Локальный вход

1. Откройте `/Account/Register`.
2. Создайте локального пользователя.
3. Если `SAMPLECLIENT_REQUIRE_EMAIL_VERIFICATION=false`, пользователь войдет сразу.
4. Если `SAMPLECLIENT_REQUIRE_EMAIL_VERIFICATION=true`, confirmation link будет залогирован в консоль SampleClient в Development.
5. После подтверждения email можно войти через `/Account/Login`.

Для восстановления локального пароля откройте `/Account/ForgotPassword`. В Development reset link логируется в консоль SampleClient, если SMTP не настроен. В non-Development SMTP обязателен. Ответ страницы generic: приложение не раскрывает, существует ли пользователь и подтвержден ли email.

## Вход через SiMed SSO

1. На `/Account/Login` нажмите `Войти через SiMed SSO`.
2. Пройдите вход, MFA при необходимости и consent на стороне SiMed SSO.
3. После callback SampleClient создаст или найдет локального пользователя:
   - если внешний login уже связан, будет использован этот local user;
   - если local user не найден, он будет создан по `sub`, `email`, `email_verified`, `name`;
   - если local user с таким email уже есть и `email_verified=true`, SSO login будет привязан к нему;
   - если email занят, но не подтвержден со стороны SSO, linking будет отклонен безопасным generic сообщением.

## Что показывает `/Profile`

- Local user данные: ID, email, display name, подтверждение email, дата создания, последний вход.
- Статус локального password login.
- Статус привязки SiMed SSO и `SsoSubject`.
- Claims текущей cookie SampleClient.
- Masked summary для `access_token`, `id_token`, `refresh_token`.
- `expires_at` для access token.
- Ответ `/connect/userinfo`, полученный backend-side через текущий `access_token`.

## Logout

Кнопка выхода очищает только cookie `SampleClient`. SSO-сессия в SiMed SSO остается активной, поэтому повторный вход через SSO может пройти без ввода пароля, если SSO-cookie еще действительна.

## Типичные ошибки

- `redirect_uri` mismatch: redirect URI в `/Admin/Clients` должен быть ровно `https://localhost:7290/signin-oidc`.
- `invalid_client`: неверный `SAMPLECLIENT_CLIENT_ID` или `SAMPLECLIENT_CLIENT_SECRET`.
- `invalid_scope`: в client не разрешены все scopes `openid profile email offline_access`.
- Ошибка TLS/dev certificate: выполните `dotnet dev-certs https --trust`.
- SampleClient не стартует из-за БД: проверьте `SAMPLECLIENT_POSTGRES_*` или `ConnectionStrings__SampleClient`.
- SampleClient не стартует в non-Development: проверьте `SAMPLECLIENT_DATA_PROTECTION_KEYS_PATH`, `SAMPLECLIENT_SMTP_*` и отсутствие placeholder secrets.
- SSO login не привязывается к существующему local user: убедитесь, что SiMed SSO возвращает `email_verified=true`.
