# Auth

Sahip: Berk. Kayıt, giriş ve oturum (UC1). Şema [spec 0002](../../../../../docs/specs/0002-data-model/index.md), davranış UC1 spec'inde.

## Tablolar

- `asp_net_users` (`AppUser`): Identity kullanıcısı, ek olarak `display_name` (en fazla 50), `created_at`, `deleted_at`. `user_name` her zaman e-postanın kopyasıdır.
- `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens`: Identity'nin varsayılan tabloları. Rol tabloları yok (`IdentityUserContext`).
- `refresh_tokens` (`RefreshToken`): yalnızca 32 baytlık özet; rotasyon için `family_id`, `replaced_by_id`, `revoked_at`.

## Kurallar

- Kısıt adları `AuthDbNames` içinde; hata eşlemesinde `PostgresException.ConstraintName` bu sabitlerle karşılaştırılır.
- Ömür `RefreshTokenRules.Lifetime` (7 gün). Zamanlar `IClock.UtcNow`'dan, şemada varsayılan yok.
- Kullanıcı satırı silinmez, anonimleştirilir (özellik #12). Diğer modüllerin `asp_net_users`'a FK'leri `Restrict` olmalı.
- Token yalnızca bir kez döndürülür: `UPDATE refresh_tokens SET revoked_at = @now WHERE id = @id AND revoked_at IS NULL`, etkilenen satır 1 değilse yeniden kullanım. `ux_refresh_tokens_replaced_by_id` ikinci güvencedir.

## Varsayımlar ve paketler

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.34 ve `EFCore.NamingConventions` 8.0.3 (snake_case).
- Adlandırma paketi açıkça verilmiş adları çevirmez: Identity'nin tablo adları (`AspNetUsers` vb.) ve iki index adı (`EmailIndex`, `UserNameIndex`) `AppUserConfiguration` ve `IdentityUserTablesConfiguration` içinde elle snake_case verilir.
- Identity servis kaydı `AuthServiceCollectionExtensions.AddRoomlyAuth` içinde (spec 0004).
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.34 ve `FluentValidation.DependencyInjectionExtensions` 11.11.0.

## Uç noktalar (spec 0004)

| Uç | Ne yapar |
|---|---|
| `POST /api/auth/register` | Kayıt + otomatik giriş (201). `EMAIL_TAKEN` 409 |
| `POST /api/auth/login` | Giriş. `INVALID_CREDENTIALS` 401, `ACCOUNT_LOCKED` 423 (5 hata, 15 dk) |
| `POST /api/auth/refresh` | Rotasyon; yeniden kullanımda aile iptali. `INVALID_REFRESH_TOKEN` 401 |
| `POST /api/auth/logout` | Ailenin iptali, her zaman 204 |
| `GET /api/auth/me` | Kullanıcı + açık grubu (`group` ya da `null`) |

- Bütün uçlar varsayılan olarak oturum ister (fallback policy). Anonim uç `[AllowAnonymous]` ile açılır; sınıf düzeyinde `[AllowAnonymous]` aksiyondaki `[Authorize]`'ı ezer, bu yüzden aksiyon düzeyinde yazılır.
- Korumalı uçta kullanıcı: `User.GetUserId()` (`Common/Auth`).
- `/api/auth/*` IP başına dakikada 10 istek (`RateLimiting:Auth`), aşılınca 429 `RATE_LIMITED`.
- Loglarda e-posta `LogMask.Email`, token hiç yazılmaz (SEC-07).
- Identity kilit zamanını `IClock`'tan değil kendi saatinden okur; testte `lockout_end` geçmişe çekilir.
