# 0004. Kayıt, giriş ve oturum yönetimi (UC1)

**Date**: 2026-11-22
**Status**: In Progress

## Summary

Kullanıcı ad, e-posta ve şifreyle kayıt olur ve hemen oturum açılmış olur; sonra e-posta ve şifreyle giriş yapabilir. Sunucu 15 dakikalık bir erişim anahtarı (JWT) ve 7 günlük bir yenileme anahtarı (refresh token) verir; yenileme anahtarı her kullanımda değişir ve çalınıp tekrar kullanılırsa bütün zinciri iptal edilir. Uygulama anahtarları yalnızca telefonun şifreli deposunda tutar, süresi dolan erişim anahtarını kullanıcıya hissettirmeden yeniler ve kullanıcıyı oturumuna göre giriş, "Alan Kur / Koda Katıl" ya da ana ekrana yönlendirir. 5 hatalı denemede hesap 15 dakika kilitlenir, IP başına dakikada 10 istek sınırı vardır, loglarda e-posta ve anahtarlar maskelidir.

## Context

Scope #6, Tracer Bullet'ın ilk ince yolu: DB → API → Flutter. Spec 0001 ASP.NET Core Identity (`AddIdentityCore`, çerezsiz) + JWT bearer + rotasyonlu refresh token'ı, yerleşik rate limiter'ı ve `flutter_secure_storage`'ı seçti; spec 0002 `asp_net_users` ve `refresh_tokens` tablolarını, rotasyon sütunlarını ve hesap silme değer kaynaklarını kurdu. Bu spec davranışı, uç noktaları, hata kodlarını ve mobil oturum akışını sabitler. Gereksinimler: SEC-01 (JWT kimlik doğrulama, şifre politikası ve 5 hatalı deneme kilidi), SEC-04 (rate limit), SEC-05 (güvenli saklama), SEC-07 (log maskeleme). Şifre kuralını engineer seçti: en az 8 karakter, en az bir harf ve bir rakam. E-posta doğrulama ve şifre sıfırlama kapsam dışı. Hesap silme scope #12'de.

## Requirements

**User stories**:
- Yeni kullanıcı olarak ad, e-posta ve şifreyle kayıt olup hemen uygulamayı kullanmak istiyorum.
- Kayıtlı kullanıcı olarak giriş yapıp 7 gün boyunca tekrar şifre girmeden oturumumun açık kalmasını istiyorum.
- Grubu olmayan kullanıcı olarak girişten sonra alan kurma ya da koda katılma seçeneklerini görmek istiyorum.
- Ekip olarak hesapların kaba kuvvetle denenmesini ve kişisel verinin loglara sızmasını engellemek istiyoruz.

**Acceptance criteria**:
- **AC-1**: `POST /api/auth/register` geçerli `displayName`, `email`, `password` ile 201 ve token yanıtı (bkz. API surface) döner; kullanıcı `asp_net_users`'a `user_name = email` (kırpılmış, küçük harf), `created_at = IClock.UtcNow` ile yazılır, şifre Identity hash'iyle saklanır.
- **AC-2** (SEC-01): Kayıt doğrulaması: `displayName` kırpıldıktan sonra 2 ile 50 karakter; `email` geçerli biçim, en fazla 256; `password` 8 ile 128 karakter, en az bir harf ve bir rakam. İhlal 400 `VALIDATION_FAILED` + alan hataları (Türkçe mesaj) döner.
- **AC-3**: Kayıtlı bir e-postayla (büyük / küçük harf farkı gözetmeden) kayıt 409 `EMAIL_TAKEN` döner; aynı anda gelen iki kayıtta ikincisi de 409 alır (DB kısıtı `ux_asp_net_users_normalized_email` hata koduna çevrilir), 500 değil.
- **AC-4**: `POST /api/auth/login` doğru e-posta ve şifreyle 200 ve token yanıtı döner, `access_failed_count` sıfırlanır. Yanlış şifre, kayıtsız e-posta ya da silinmiş (`deleted_at` dolu) kullanıcı aynı yanıtı alır: 401 `INVALID_CREDENTIALS`, "E-posta ya da şifre hatalı."
- **AC-5** (SEC-01): Aynı hesaba art arda 5 yanlış şifre girilince 5. deneme ve kilit süresince her deneme (doğru şifreyle bile) 423 `ACCOUNT_LOCKED` döner; kilit 15 dakika sürer, süre bitince doğru şifreyle giriş başarılıdır.
- **AC-6** (SEC-04): `/api/auth/*` uçlarına aynı IP'den bir dakikada 11. istek 429 `RATE_LIMITED` döner (`ProblemDetails`); sınır `RateLimiting:Auth:PermitLimit` (10) ve `WindowSeconds` (60) ayarından okunur.
- **AC-7** (SEC-01): Erişim anahtarı HS256 JWT'dir: `sub` = kullanıcı `Guid`'i, `jti`, `iss` = `Jwt:Issuer`, `aud` = `Jwt:Audience`, `exp` = `IClock.UtcNow + 15 dk`. Korumalı uçlar geçersiz, süresi dolmuş ya da eksik anahtarda 401 döner (saat kayması toleransı 30 sn).
- **AC-8**: Refresh token 32 rastgele bayttır (base64url, 43 karakter); DB'de yalnızca SHA-256 özeti durur. Giriş ve kayıtta yeni `family_id` ile yazılır, `expires_at = created_at + 7 gün`.
- **AC-9**: `POST /api/auth/refresh` geçerli refresh token ile yeni erişim ve refresh token döner; eski satıra `revoked_at` ve `replaced_by_id` yazılır, yeni satır aynı `family_id`'yi alır. Süresi dolmuş, bilinmeyen ya da silinmiş kullanıcıya ait token 401 `INVALID_REFRESH_TOKEN` döner.
- **AC-10**: Daha önce döndürülmüş (iptal edilmiş) bir refresh token tekrar kullanılırsa 401 `INVALID_REFRESH_TOKEN` döner ve aynı `family_id`'deki bütün iptal edilmemiş token'lar iptal edilir (yeniden kullanım tespiti). Aynı token'la paralel iki istekten yalnızca biri başarılı olur.
- **AC-11**: `POST /api/auth/logout` verilen refresh token'ın ailesini iptal eder ve 204 döner; token bilinmiyor ya da zaten iptalse de 204 (idempotent).
- **AC-12**: `GET /api/auth/me` (bearer) kullanıcının `id`, `displayName`, `email` bilgisini ve açık bir grubu varsa `group` (`id`, `name`, `type`, `role`), yoksa `group: null` döner.
- **AC-13** (SEC-07): Auth logları e-postayı `LogMask.Email` ile (`b***@ornek.com`), token'ları hiç ya da `LogMask.Token` ile (`***`) yazar; hiçbir log satırında düz e-posta, şifre ya da token yoktur. Development dışında `Jwt:SigningKey` eksik, 32 bayttan kısa ya da geliştirme varsayılanına eşitse uygulama açılmaz.
- **AC-14** (SEC-05): Mobil uygulama erişim ve refresh token'ı yalnızca `flutter_secure_storage` içinde saklar (shared preferences ya da dosya yok); çıkışta ikisi de silinir.
- **AC-15**: Mobil: korumalı bir istek 401 alınca uygulama tek bir refresh isteği yapar (aynı anda gelen 401'ler aynı refresh'i bekler), başarılıysa asıl isteği bir kez tekrarlar; refresh başarısızsa token'ları siler ve kullanıcıyı giriş ekranına götürür.
- **AC-16**: Mobil yönlendirme: açılışta saklı refresh token yoksa giriş ekranı; varsa refresh + `/me` sonrası grubu yoksa "Alan Kur / Koda Katıl" ekranı (`/onboarding`), grubu varsa ana ekran (`/home`). Oturum yokken korumalı rotaya gitmek girişe, oturum varken giriş ya da kayıt rotasına gitmek uygun ana rotaya yönlendirir. Açılışta sunucuya ulaşılamazsa token'lar silinmez, hata ve "Tekrar dene" gösterilir.
- **AC-17**: Mobil giriş ve kayıt ekranları ortak bileşenlerle (spec 0003) yazılır; istemci tarafı doğrulama AC-2 ile aynı kurallardadır; istek sürerken düğme yükleniyor durumundadır; sunucu hataları Türkçe mesajla gösterilir (`INVALID_CREDENTIALS`, `ACCOUNT_LOCKED`, `EMAIL_TAKEN` alan altında, `RATE_LIMITED` ve bağlantı hatası snackbar'da). Çıkış ana ekrandan yapılır.
- **AC-18**: `USE_MOCK=true` iken auth akışı tamamen bellekte çalışır: `demo@roomly.app` / `Demo1234` grubu olan, yeni kayıt olan kullanıcı grubu olmayan kullanıcıdır; mock da `EMAIL_TAKEN` ve `INVALID_CREDENTIALS` hatalarını üretir.

## Options considered

### Option 1: Identity `UserManager` ile elle akış + kendi token servisi (seçilen)

Kayıt ve giriş `UserManager` (`CreateAsync`, `CheckPasswordAsync`, `AccessFailedAsync`, `IsLockedOutAsync`) ile; JWT ve refresh token kendi küçük servislerimizle.
**Pros**: Spec 0001'in `AddIdentityCore` kararına uyar, çerez yok, hata kodları tam kontrolde. **Cons**: kilit akışı birkaç satır elle yazılır.

### Option 2: .NET 8 `MapIdentityApi`

**Pros**: hazır uçlar. **Cons**: opak bearer token, bizim rotasyon ve aile iptali modelimize, `Result` hata desenine ve Türkçe hata kodlarına uymaz; kendi şemasını getirir.

### Option 3: `SignInManager.CheckPasswordSignInAsync`

**Pros**: kilit hazır. **Cons**: `AddSignInManager` çerez şeması ve `IHttpContextAccessor` ister; kazanç birkaç satır.

## Decision

**Chosen option**: Option 1: `UserManager` üzerinde elle kayıt ve giriş akışı, kendi `JwtTokenIssuer` ve `RefreshTokenService` sınıfları; mobilde `SessionController` + dio auth interceptor + go_router redirect.

**Implementation skills**: `csharp-xunit` (`~/.claude/skills/csharp-xunit/`, backend testleri)

## Rationale

Spec 0001 ve 0002 çerezsiz Identity'yi, rotasyon sütunlarını ve `Result` desenini zaten sabitledi; hazır `MapIdentityApi` bunların hepsiyle çakışır. Elle akış küçük ve testte her hata kodu doğrudan doğrulanabilir. Kayıt sonrası otomatik giriş, ders ölçeğinde e-posta doğrulama olmadığı için ekstra bir giriş adımını gereksiz kılar. Kayıtta `EMAIL_TAKEN` e-postanın kayıtlı olduğunu ele verir; kayıt ekranında bunu gizlemek kullanıcıyı şaşırtır, girişte ise tek tip `INVALID_CREDENTIALS` ile hesap varlığı gizlenir.

## Feature design

### Backend yerleşimi (`Features/Auth/`, Berk)

| Dosya | Sorumluluk |
|---|---|
| `AuthController` | 5 uç; validator çağırır, servisi çağırır, `ToActionResult` |
| `AuthService` | Kayıt, giriş (kilit), refresh, logout, me; `Result<T>` |
| `AuthRepository` | Refresh token okuma / koşullu güncelleme / aile iptali, `me` sorgusu |
| `JwtTokenIssuer` (`IAccessTokenIssuer`) | JWT üretimi, `IClock` ile |
| `RefreshTokenGenerator` | 32 bayt rastgele + SHA-256 özet |
| `AuthErrors` | `EMAIL_TAKEN`, `INVALID_CREDENTIALS`, `ACCOUNT_LOCKED`, `INVALID_REFRESH_TOKEN` sabitleri ve `Error` fabrikaları |
| `JwtOptions` | `Jwt` bölümü + açılış doğrulaması |
| `AuthServiceCollectionExtensions` | `AddRoomlyAuth(configuration, environment)`: Identity, JWT bearer, rate limiter, servisler |
| `Dtos/`, `Validators/` | İstek / yanıt kayıtları, `RegisterRequestValidator`, `LoginRequestValidator`, `RefreshRequestValidator` |

Ortak (onaya bağlı): `Common/Logging/LogMask.cs`, `Common/Auth/ClaimsPrincipalExtensions.GetUserId()`, `Common/Errors/ErrorCodes.RateLimited`, `Program.cs`'e `AddRoomlyAuth` + `UseAuthentication` + `UseRateLimiter`, `appsettings*.json` `Jwt` ve `RateLimiting` bölümleri. Yeni paket: `Microsoft.AspNetCore.Authentication.JwtBearer` `8.0.34`, `FluentValidation.DependencyInjectionExtensions` `11.x`.

### Data model sketch

Şema değişikliği yok; spec 0002'nin `asp_net_users` ve `refresh_tokens` tabloları kullanılır. Migration yok.

### State transitions

Refresh token: `aktif` → `döndürüldü` (refresh: `revoked_at` + `replaced_by_id`) · `aktif` → `iptal` (logout ya da yeniden kullanım tespitinde tüm aile). `döndürüldü`/`iptal` bir token gelirse → aile iptali + 401.

### API surface

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/auth/register` | POST | `displayName`, `email`, `password` | 201 `AuthResponse` | yok | 400 `VALIDATION_FAILED`, 409 `EMAIL_TAKEN`, 429 |
| `/api/auth/login` | POST | `email`, `password` | 200 `AuthResponse` | yok | 400, 401 `INVALID_CREDENTIALS`, 423 `ACCOUNT_LOCKED`, 429 |
| `/api/auth/refresh` | POST | `refreshToken` | 200 `AuthResponse` | yok | 400, 401 `INVALID_REFRESH_TOKEN`, 429 |
| `/api/auth/logout` | POST | `refreshToken` | 204 | yok | 400, 429 |
| `/api/auth/me` | GET | bearer | 200 `MeResponse` | bearer | 401 |

`AuthResponse`: `{ accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, user: { id, displayName, email } }`.
`MeResponse`: `{ id, displayName, email, group: { id, name, type: "Ev"|"Yurt", role: "Admin"|"Member" } | null }`.
401 (bearer eksik / geçersiz) gövdesi: `ProblemDetails` + `code: UNAUTHORIZED` (JwtBearer `OnChallenge`).

### Value sourcing

| Eylem | Değer | Kaynak |
|---|---|---|
| Kayıt | `email`, `user_name` | İstek `email`, `Trim().ToLowerInvariant()` |
| Kayıt | `display_name` | İstek `displayName`, `Trim()` |
| Kayıt | `created_at` | `IClock.UtcNow` |
| Kayıt | e-posta kayıtlı mı | `UserManager.FindByEmailAsync`; yarışta `ux_asp_net_users_normalized_email` 23505 → `EMAIL_TAKEN` (Identity `DuplicateEmail`/`DuplicateUserName` hata kodu da) |
| Giriş | kilitli mi | `UserManager.IsLockedOutAsync` (`lockout_end`); Identity ayarı `MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15 dk`, `AllowedForNewUsers = true` |
| Giriş | silinmiş mi | `deleted_at IS NOT NULL` → `INVALID_CREDENTIALS` |
| Token | `exp` | `IClock.UtcNow + Jwt:AccessTokenMinutes` (15) |
| Token | `accessTokenExpiresAt` | aynı değer |
| Token | imza anahtarı, `iss`, `aud` | `Jwt:SigningKey`, `Jwt:Issuer` (`roomly-api`), `Jwt:Audience` (`roomly-mobile`) |
| Refresh | düz token | `RandomNumberGenerator.GetBytes(32)` → base64url |
| Refresh | `token_hash` | SHA-256(UTF-8(düz token)) |
| Refresh | `family_id` | Giriş / kayıtta `Guid.NewGuid()`, rotasyonda eski satırdan |
| Refresh | `expires_at`, `refreshTokenExpiresAt` | `created_at + RefreshTokenRules.Lifetime` |
| Refresh | tek kullanım | `UPDATE refresh_tokens SET revoked_at = @now WHERE id = @id AND revoked_at IS NULL`; etkilenen satır 0 → yeniden kullanım |
| Me | `group` | `group_memberships` ⋈ `groups` (`user_id = sub`, `closed_at IS NULL`) |
| Rate limit | istemci anahtarı | `HttpContext.Connection.RemoteIpAddress` |
| Mobil | saklı token'lar | `flutter_secure_storage` anahtarları `roomly.accessToken`, `roomly.refreshToken` |
| Mobil | yönlendirme | `SessionState` (`unknown`, `unauthenticated`, `authenticated(user)`), `user.group == null` → `/onboarding` |
| Mobil | hata mesajı | `ApiException.code` → `core/errors/api_error_messages.dart` tablosu; bilinmeyen kod → sunucunun `title` alanı; bağlantı hatası → "Sunucuya ulaşılamadı. Bağlantını kontrol et." |

### Key invariants

- Bir refresh token en fazla bir kez döndürülür (koşullu `UPDATE` + `ux_refresh_tokens_replaced_by_id`).
- Rotasyon sırası tek transaction: eski satırı koşullu iptal et → yeni satırı ekle → eski satıra `replaced_by_id` yaz.
- Giriş yanıtı hesap varlığını ele vermez (yanlış şifre, kayıtsız ve silinmiş aynı yanıt).
- Düz token ve şifre hiçbir zaman loglanmaz ya da DB'ye yazılmaz.
- Mobilde aynı anda en fazla bir refresh isteği uçuştadır.

### Security model

- Uçların hepsi anonim, `me` hariç (bearer). Bundan sonra ekibin bütün controller'ları varsayılan olarak `[Authorize]` ister (`AddAuthorization` fallback policy: kimliği doğrulanmış kullanıcı); anonim uçlar `[AllowAnonymous]` ile açılır (`/health`, auth uçları, Swagger).
- KVKK: kişisel veri yalnızca `display_name` ve `email`; loglarda maskeli (SEC-07).
- Kilit hesap bazlı (SEC-01), rate limit IP bazlı (SEC-04); ikisi birlikte kaba kuvveti yavaşlatır. Kilit hesap kilitleme saldırısına (başkasının hesabını kilitlemek) açıktır; ders ölçeğinde kabul.
- JWT'ler sunucuda iptal edilemez; en fazla 15 dk geçerli kalır (logout sonrası dahil).

### Configuration required

- `Jwt:SigningKey` (≥ 32 bayt; Development'ta `appsettings.Development.json`'da açıkça sahte değer; başka ortamda user-secrets / `Jwt__SigningKey`)
- `Jwt:Issuer` = `roomly-api`, `Jwt:Audience` = `roomly-mobile`, `Jwt:AccessTokenMinutes` = 15 (`appsettings.json`)
- `RateLimiting:Auth:PermitLimit` = 10, `RateLimiting:Auth:WindowSeconds` = 60 (`appsettings.json`)
- Mobil paketler (`pubspec.yaml`, ekip onayı): `flutter_secure_storage`; dev: `mocktail`

### Mobil yerleşimi

| Dosya | Sorumluluk |
|---|---|
| `core/storage/token_storage.dart` | `TokenStorage` arayüzü, `SecureTokenStorage`, `tokenStorageProvider` |
| `core/errors/api_exception.dart` | `ApiException(code, message, status, fieldErrors)`, `DioException` → `ApiException` çevirisi, Türkçe mesaj tablosu |
| `core/network/auth_interceptor.dart` | Bearer ekler; 401'de tek uçuşlu refresh ve bir kez tekrar; başarısızlıkta `onSessionExpired` |
| `core/network/dio_provider.dart` | Interceptor'ı bağlar |
| `core/auth/session.dart` | `SessionState`, `CurrentUser`, `GroupSummary`, `sessionControllerProvider` arayüzü (router'ın okuduğu tek şey) |
| `core/router/app_router.dart` | Rotalar ve oturuma göre `redirect`, `refreshListenable` |
| `features/auth/data/` | `AuthRepository`, `MockAuthRepository`, `RemoteAuthRepository`, DTO'lar |
| `features/auth/application/session_controller.dart` | `SessionController` (`Notifier<SessionState>`): `restore`, `login`, `register`, `logout`, `refreshMe` |
| `features/auth/presentation/` | `SplashScreen`, `LoginScreen`, `RegisterScreen`, `OnboardingScreen` (Alan Kur → UC2 placeholder; Koda Katıl → `/join`) |

Rotalar: `/splash`, `/login`, `/register`, `/onboarding`, `/home` (dashboard dilimi gelene kadar geçici ana ekran, çıkış düğmeli), `/join` (UC3 dilimi gelene kadar placeholder), `/system` (tanı ekranı).

### Critical test scenarios

Backend entegrasyon testleri `tests/Roomly.Api.IntegrationTests/Auth/` (`WebApplicationFactory<Program>` + paylaşılan Postgres, ortam `Testing`, rate limit yüksek), unit testler `tests/Roomly.Api.UnitTests/Features/Auth/`. Ad: gereksinim kimliği varsa o (`SEC01`, `SEC04`), yoksa `AUTH<nn>` (= AC-nn). Mobil testler `mobile/test/features/auth/` ve `mobile/test/core/`.
- Kayıt mutlu yol → 201, token'lar çalışıyor, `me` grupsuz, verifies **AC-1**, **AC-8**, **AC-12**
- Kayıt doğrulama (veri güdümlü: kısa ad, kötü e-posta, rakamsız şifre, 7 karakter) → 400 + alan, verifies **AC-2**
- Aynı e-posta (farklı harf) → 409; paralel iki kayıt → biri 201 biri 409, verifies **AC-3**
- Giriş doğru / yanlış / kayıtsız / silinmiş, verifies **AC-4**
- 5 yanlış → 423; doğru şifre de 423; `lockout_end` geçmişe çekilince 200, verifies **AC-5** (`Login_AfterFiveFailures_LocksAccountUntilLockoutEnds_SEC01`)
- Gerçek limitle 11. istek 429, verifies **AC-6** (`Auth_EleventhRequestInWindow_Returns429_SEC04`)
- JWT claim'leri ve `FakeClock` ile `exp`; süresi dolmuş token ile `me` → 401, verifies **AC-7**
- Refresh rotasyonu, süresi dolmuş token, yeniden kullanımda aile iptali, paralel iki refresh, verifies **AC-9**, **AC-10**
- Logout → sonraki refresh 401; bilinmeyen token logout 204, verifies **AC-11**
- Grubu olan kullanıcının `me` yanıtı, verifies **AC-12**
- `LogMask` unit; giriş akışında test logger'ında düz e-posta yok, verifies **AC-13** (`*_SEC07`)
- Mobil: interceptor 401 → tek refresh → tekrar; paralel 401 → tek refresh; refresh başarısız → oturum kapanır, verifies **AC-15**
- Mobil: router redirect tablosu (unknown / unauthenticated / grupsuz / gruplu), verifies **AC-16**
- Mobil: giriş ekranı doğrulama ve sunucu hata mesajı; kayıt ekranında `EMAIL_TAKEN` alan altında, verifies **AC-17**
- Mobil: çıkış token'ları siler (sahte `TokenStorage`), verifies **AC-14** (`*_SEC05`)
- Mobil: mock demo girişi gruplu, yeni kayıt grupsuz, verifies **AC-18**

## Build plan

Tracer Bullet: önce bir kullanıcının kayıt olup `me` çağırabildiği en ince uçtan uca yol, sonra güvenlik katmanları kalınlaştırır.

1. [x] Paketler (`JwtBearer`, `FluentValidation.DependencyInjectionExtensions`), `JwtOptions`, `appsettings` `Jwt` / `RateLimiting`, `AddRoomlyAuth` (Identity ayarları, JWT bearer, `UNAUTHORIZED` challenge, fallback policy), `Program.cs` bağlantısı, satisfies **AC-7**, **AC-13**
2. [x] `JwtTokenIssuer`, `RefreshTokenGenerator`, `AuthErrors`, DTO'lar ve validator'lar, satisfies **AC-2**, **AC-7**, **AC-8**
3. [x] `AuthRepository` + `AuthService` kayıt, giriş (kilit), `me`; `AuthController` register / login / me, satisfies **AC-1**, **AC-3**, **AC-4**, **AC-5**, **AC-12**
4. [x] Refresh rotasyonu, yeniden kullanım tespiti, logout, satisfies **AC-9**, **AC-10**, **AC-11**
5. [x] Rate limiter (`RATE_LIMITED`), `LogMask` ve auth logları, satisfies **AC-6**, **AC-13**
6. [x] Backend testleri (entegrasyon + unit), `AuthWebApplicationFactory`, satisfies **AC-1** ile **AC-13**
7. [x] Mobil: paketler, `TokenStorage`, `ApiException`, `AuthInterceptor`, `dioProvider`, satisfies **AC-14**, **AC-15**
8. [x] Mobil: `AuthRepository` (Mock + Remote), `SessionController`, `core/auth/session.dart`, router redirect, satisfies **AC-16**, **AC-18**
9. [x] Mobil: splash, giriş, kayıt, onboarding, geçici ana ekran, satisfies **AC-16**, **AC-17**
10. [x] Mobil testler, `docs/traceability.md`, `Features/Auth/README.md`, satisfies **AC-14** ile **AC-18**

## Consequences

**Positive**:
- Ekibin bütün uçları varsayılan olarak korumalı; kullanıcı kimliği `User.GetUserId()` ile tek satır.
- Rotasyon ve aile iptali çalınan refresh token'ı tek kullanımda etkisiz kılar.
- Router oturum durumunu tek yerden okur; modüller yönlendirme yazmaz.

**Negative / tradeoffs**:
- Identity'nin kilit zamanı `IClock`'tan değil Identity'nin kendi saatinden okunur; kilit bitişi testte `lockout_end` geçmişe çekilerek doğrulanır.
- JWT logout sonrası en fazla 15 dk geçerli kalır.
- Hesap bazlı kilit başkasının hesabını kilitlemeye açık.
- `EMAIL_TAKEN` kayıt ekranında e-postanın kayıtlı olduğunu ele verir.
- Fallback policy yüzünden ekip anonim uç açarken `[AllowAnonymous]` yazmayı unutmamalı.

**Neutral**:
- Şema değişikliği yok.
- `/home` ve `/join` bu dilimde geçici; dashboard ve UC3 dilimleri onları değiştirir.

## Follow-up

- [ ] Yerel ortam (#5): demo kullanıcıları gerçek API'de de `demo@roomly.app` / `Demo1234` ile seed et (mock ile aynı).
- [ ] `backend/AGENTS.md`: fallback policy ve `[AllowAnonymous]`, `User.GetUserId()` (`/sync`).
