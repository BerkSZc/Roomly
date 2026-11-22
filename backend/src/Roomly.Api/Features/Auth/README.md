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
- Identity servis kaydı (`AddIdentityCore`) UC1 (#6) ile gelir.
