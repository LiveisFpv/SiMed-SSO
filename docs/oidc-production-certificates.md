# Production OIDC certificates

Этот документ описывает настройку signing/encryption certificates для OpenIddict вне `Development`.

В `Development` приложение использует development certificates OpenIddict. Для `Staging` и `Production` нужно явно указать два `.pfx` файла:

- signing certificate - подписывает токены и metadata keys;
- encryption certificate - шифрует токены, если формат токена требует encryption.

Dev-сертификаты нельзя использовать в production.

## Environment variables

```env
SSO_ISSUER=https://sso.example.com/
OIDC_SIGNING_CERT_PATH=certs/oidc-signing.pfx
OIDC_SIGNING_CERT_PASSWORD=change-this-signing-password
OIDC_ENCRYPTION_CERT_PATH=certs/oidc-encryption.pfx
OIDC_ENCRYPTION_CERT_PASSWORD=change-this-encryption-password
```

`SSO_ISSUER` должен совпадать с публичным URL сервера, который видят OAuth/OIDC clients. Значение issuer в discovery metadata и `iss` в `id_token` должны совпадать.

Пути к сертификатам могут быть абсолютными или относительными к корню приложения.

## Self-signed certificates for staging

Для staging или локальной проверки non-Development режима можно создать self-signed `.pfx` через PowerShell:

```powershell
$certPassword = ConvertTo-SecureString "change-this-signing-password" -AsPlainText -Force
$signingCert = New-SelfSignedCertificate `
  -Subject "CN=SiMed SSO OIDC Signing" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -KeyExportPolicy Exportable `
  -KeyUsage DigitalSignature `
  -CertStoreLocation "Cert:\CurrentUser\My"
Export-PfxCertificate `
  -Cert $signingCert `
  -FilePath ".\certs\oidc-signing.pfx" `
  -Password $certPassword

$certPassword = ConvertTo-SecureString "change-this-encryption-password" -AsPlainText -Force
$encryptionCert = New-SelfSignedCertificate `
  -Subject "CN=SiMed SSO OIDC Encryption" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -KeyExportPolicy Exportable `
  -KeyUsage KeyEncipherment, DataEncipherment `
  -CertStoreLocation "Cert:\CurrentUser\My"
Export-PfxCertificate `
  -Cert $encryptionCert `
  -FilePath ".\certs\oidc-encryption.pfx" `
  -Password $certPassword
```

Для реального production храните `.pfx` и пароли через secret manager, CI/CD secrets или защищенное хранилище сервера. Не коммитьте сертификаты и пароли в git.

## Проверка

После запуска non-Development окружения проверьте discovery:

```powershell
curl.exe -k https://sso.example.com/.well-known/openid-configuration
```

Проверьте JWKS:

```powershell
curl.exe -k https://sso.example.com/.well-known/jwks
```

В discovery должны быть корректные `issuer`, `authorization_endpoint`, `token_endpoint`, `userinfo_endpoint` и `jwks_uri`. В JWKS должны вернуться публичные ключи, соответствующие signing certificate.

После этого выполните обычный Authorization Code Flow with PKCE из [OIDC quickstart](oidc-quickstart.md). Если сертификаты загружены корректно, `access_token`, `id_token` и `refresh_token` будут выдаваться как в Development.

## Типичные ошибки

- `SSO_ISSUER` отсутствует вне `Development`: приложение не стартует.
- Путь к `.pfx` неверный: приложение не стартует и указывает проблемную переменную.
- Пароль от `.pfx` неверный: приложение не стартует с ошибкой загрузки сертификата.
- `.pfx` не содержит private key: приложение не стартует, потому что OpenIddict не сможет подписывать или шифровать токены.
