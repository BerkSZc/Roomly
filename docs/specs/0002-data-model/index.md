# 0002. Kullanıcı, oturum, grup, üyelik ve davet kodu veri modeli

**Date**: 2026-11-22
**Status**: In Progress

## Summary

Bu spec, Roomly'nin ilk veritabanı şemasını (tablolar, sütunlar ve kurallar) belirler: kullanıcı (Identity), refresh token, grup, grup üyeliği ve davet kodu. Önemli kurallar yalnızca kodda değil, veritabanının kendisinde de durur: bir kullanıcı tek gruptadır, grupta tek yönetici vardır, grupta tek aktif kod vardır, kapasite grup türüne göre sınırlıdır (BND-02). Davet kodu düz metin olarak hiç saklanmaz, yalnızca SHA-256 özeti (geri çevrilemeyen parmak izi) durur, bu yüzden yönetici kodu yalnızca üretildiği anda görür. Her şey tek bir migration ile (şemayı kuran kod dosyası) gelir ve ekip üyeleri kendi tablolarını `AppDbContext`'e dokunmadan ekleyebilir.

## Requirements

**User stories**:
- Bir modül sahibi olarak, kullanıcı ve grup tablolarına `Guid` ile bağlanmak istiyorum, böylece kendi özelliğimi ortak dosyalara dokunmadan kurabilirim.
- Berat (UC2 / UC10) olarak, grup oluşturma ve kapatma için gereken alanların hazır olmasını istiyorum, böylece şemayı kırmadan ekleme yapabilirim.
- Ekip olarak, iş kurallarının veritabanında da korunmasını istiyorum, böylece bir hata ya da yarış durumu (iki isteğin aynı anda gelmesi) bozuk veri yazamaz.

**Acceptance criteria**:
- **AC-1**: `InitialDataModel` migration'ı boş bir PostgreSQL 16 veritabanına hatasız uygulanır ve şu tabloları kurar: `asp_net_users`, `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens`, `refresh_tokens`, `groups`, `group_memberships`, `invite_codes`, `__ef_migrations_history`. Rol tabloları (`asp_net_roles` vb.) yoktur.
- **AC-2**: Tüm tablo, sütun, index ve kısıt adları snake_case'dir (ör. `group_id`, `asp_net_users`); Identity'nin `EmailIndex` ve `UserNameIndex` adları ile migration geçmişi tablosu da dahil. Ham SQL tırnaksız yazılabilir.
- **AC-3**: Aynı `(group_id, user_id)` çiftiyle ikinci üyelik satırı veritabanı tarafından reddedilir.
- **AC-4**: Bir kullanıcı aynı anda iki grubun üyesi olamaz; farklı bir gruba ikinci üyelik satırı veritabanı tarafından reddedilir.
- **AC-5**: Bir grupta `role = 'Admin'` olan ikinci üyelik satırı veritabanı tarafından reddedilir.
- **AC-6** (BND-02): Kapasite grup türüne göre sınırlıdır ve yöneticiyi de sayar: `Ev` için 2 ile 20, `Yurt` için 2 ile 6 (sınırlar dahil). `Ev` için 1 ve 21, `Yurt` için 1 ve 7 veritabanı tarafından reddedilir; 2, 20 (`Ev`) ve 2, 6 (`Yurt`) kabul edilir. Sınırlar kodda tek bir yerde tanımlıdır ve hem veritabanı kısıtı hem UC2 validator'ı onu kullanır.
- **AC-7**: Davet kodu yalnızca 32 baytlık SHA-256 özeti olarak saklanır; hiçbir tabloda düz metin kod sütunu yoktur. Özet, normalize edilmiş kodun UTF-8 baytlarından alınır. Normalizasyon: tüm boşluk karakterleri (`char.IsWhiteSpace`: boşluk, tab, NBSP), `-` ve `U+2013` (en dash) silinir, sonra `ToUpperInvariant`. `DEMO-ROOM-2026`, `demo-room-2026`, `DEMO ROOM 2026` ve tab ya da NBSP içeren yazımı aynı özeti verir.
- **AC-8**: Bir grupta iptal edilmemiş (`revoked_at IS NULL`) en fazla bir davet kodu bulunur; ikincisi veritabanı tarafından reddedilir. `code_hash` tüm tabloda benzersizdir. Belgelenen sıra (önce eski kodu iptal eden ayrı bir `UPDATE`, sonra yeni kodu `INSERT`, tek transaction) hatasız çalışır.
- **AC-9**: Refresh token yalnızca 32 baytlık özet olarak saklanır ve rotasyon için `family_id`, `replaced_by_id`, `revoked_at` sütunlarını taşır; `token_hash` ve `replaced_by_id` benzersizdir.
- **AC-10**: `display_name` zorunludur ve en fazla 50 karakterdir (`DeletedUser_<Guid>`, 48 karakter, sığar); `normalized_email` ve `normalized_user_name` benzersizdir; `deleted_at` boş olabilir.
- **AC-11**: `groups.closed_at` boş olabilir. Bir grup satırı silinirse üyelikleri ve davet kodları da silinir; üyeliği ya da ürettiği davet kodu olan bir kullanıcı satırı silinemez.
- **AC-12**: Yeni entity'ler `AppDbContext`'e `DbSet` eklenmeden kaydolur: her entity'nin `IEntityTypeConfiguration` sınıfı kendi modül klasöründe durur ve `ApplyConfigurationsFromAssembly` ile bulunur; erişim `Set<T>()` ile yapılır.
- **AC-13**: Tüm zaman sütunları `timestamptz` (UTC) tipindedir ve değerleri uygulama tarafından `IClock` ile yazılır; şemada `now()` varsayılanı yoktur.
- **AC-14**: Her index, benzersiz index ve check kısıtı aşağıdaki *Kısıt adları* tablosundaki adla oluşur; adlar kodda sabit olarak durur, böylece sonraki özellikler PostgreSQL hatasını (`PostgresException.ConstraintName`) hata koduna çevirebilir.

## Decision

**Chosen option**: Option 1: Kuralları veritabanında da koruyan ilişkisel model (üyelikte rol, kısmi benzersiz index'ler, check kısıtları), snake_case adlar, yalnızca kullanıcı tablolu Identity.

Beş entity (`AppUser`, `RefreshToken`, `Group`, `GroupMembership`, `InviteCode`) tek migration'la gelir; tek grup, tek yönetici, tek aktif kod ve BND-02 kapasite kuralları adı sabit index ve check kısıtlarıyla veritabanında da korunur.

**Implementation skills**: `csharp-xunit` (`~/.claude/skills/csharp-xunit/`, kısıt testlerini yazarken)

## Feature design

### Entity ve klasör yerleşimi

| Entity | Tablo | Klasör (sahip) |
|---|---|---|
| `AppUser : IdentityUser<Guid>` | `asp_net_users` | `Features/Auth/` (Berk) |
| `RefreshToken` | `refresh_tokens` | `Features/Auth/` (Berk) |
| `Group` | `groups` | `Features/Groups/` (Berk ve Berat, ortak) |
| `GroupMembership` | `group_memberships` | `Features/Groups/` |
| `InviteCode` | `invite_codes` | `Features/Groups/` |

Her entity'nin yanında `<Entity>Configuration : IEntityTypeConfiguration<<Entity>>` durur. Enum'lar: `GroupType { Ev, Yurt }`, `MembershipRole { Admin, Member }`, metin olarak saklanır (`HasConversion<string>()`, en fazla 10 karakter).

**Property tipleri**: zamanlar `DateTimeOffset` (her zaman offset 0, `IClock.UtcNow`'dan; Npgsql 8 başka offset'i reddeder), özetler `byte[]`, kimlikler `Guid`. DB varsayılanı olmadığı için zorunlu alanlar (`CreatedAt`, `DisplayName`, `Type`, `Capacity`, `Role`, `JoinedAt`, `ExpiresAt`, özetler) C# `required` property'dir; unutulan değer derlemede yakalanır.

### Data model sketch

**asp_net_users** (`AppUser`)

| Sütun | Tip | Boş | Kural |
|---|---|---|---|
| id | uuid | hayır | PK |
| display_name | varchar(50) | hayır | |
| email, normalized_email, user_name, normalized_user_name | varchar(256) | Identity varsayılanı | `user_name = email`; `normalized_email` ve `normalized_user_name` benzersiz (bkz. Kısıt adları) |
| password_hash, security_stamp, concurrency_stamp, lockout_end, lockout_enabled, access_failed_count, email_confirmed, phone_number, phone_number_confirmed, two_factor_enabled | Identity varsayılanı | | Telefon sütunları kullanılmaz, boş kalır |
| created_at | timestamptz | hayır | |
| deleted_at | timestamptz | evet | Anonimleştirmede yazılır (özellik #12) |

Identity'nin `asp_net_user_claims`, `asp_net_user_logins`, `asp_net_user_tokens` tabloları varsayılan haliyle gelir.

**refresh_tokens**

| Sütun | Tip | Boş | Kural |
|---|---|---|---|
| id | uuid | hayır | PK |
| user_id | uuid | hayır | FK → `asp_net_users`, cascade; index |
| token_hash | bytea | hayır | benzersiz; 32 bayt check'i |
| family_id | uuid | hayır | index |
| created_at | timestamptz | hayır | |
| expires_at | timestamptz | hayır | `created_at + 7 gün` |
| revoked_at | timestamptz | evet | |
| replaced_by_id | uuid | evet | FK → `refresh_tokens`, set null; benzersiz (bir token en fazla bir kez döndürülür) |

**groups**

| Sütun | Tip | Boş | Kural |
|---|---|---|---|
| id | uuid | hayır | PK |
| name | varchar(50) | hayır | benzersiz değil |
| type | varchar(10) | hayır | `Ev` ya da `Yurt` |
| capacity | integer | hayır | yöneticiyi de sayar; check: `(type = 'Ev' AND capacity BETWEEN 2 AND 20) OR (type = 'Yurt' AND capacity BETWEEN 2 AND 6)` |
| created_at | timestamptz | hayır | |
| closed_at | timestamptz | evet | UC10 kapatma |

**group_memberships**

| Sütun | Tip | Boş | Kural |
|---|---|---|---|
| group_id | uuid | hayır | PK parçası; FK → `groups`, cascade |
| user_id | uuid | hayır | PK parçası; FK → `asp_net_users`, restrict; benzersiz (tek grup) |
| role | varchar(10) | hayır | check `role IN ('Admin', 'Member')`; kısmi benzersiz `(group_id) WHERE role = 'Admin'` |
| joined_at | timestamptz | hayır | index `(group_id, joined_at)` (TRG-01 en eski üye) |

**invite_codes**

| Sütun | Tip | Boş | Kural |
|---|---|---|---|
| id | uuid | hayır | PK |
| group_id | uuid | hayır | FK → `groups`, cascade; kısmi benzersiz `(group_id) WHERE revoked_at IS NULL` |
| code_hash | bytea | hayır | benzersiz; 32 bayt check'i |
| created_by_user_id | uuid | hayır | FK → `asp_net_users`, restrict |
| created_at | timestamptz | hayır | |
| expires_at | timestamptz | hayır | `created_at + 48 saat`, saklanır |
| revoked_at | timestamptz | evet | |

**İlişkiler**: kullanıcı 1:N refresh token · kullanıcı 1:0..1 üyelik · grup 1:N üyelik · grup 1:N davet kodu (en fazla biri iptal edilmemiş) · kullanıcı 1:N ürettiği davet kodu · refresh token 0..1:0..1 yerine geçen token.

### Kısıt adları

Adlar `Features/Auth/AuthDbNames` ve `Features/Groups/GroupsDbNames` sabit sınıflarında durur; configuration'lar `HasDatabaseName` / `HasCheckConstraint(name, ...)` ile bu sabitleri kullanır, sonraki özellikler hata eşlemesinde aynı sabitleri okur. PK ve FK adları paketin ürettiği snake_case varsayılanlarda kalır (`pk_groups`, `fk_group_memberships_groups_group_id` vb.); `pk_group_memberships` da eşleme için sabitlere eklenir.

| Ad | Tablo | Ne | Sonraki kullanım |
|---|---|---|---|
| `pk_group_memberships` | group_memberships | PK `(group_id, user_id)` | #8: `ALREADY_MEMBER` |
| `ux_group_memberships_user_id` | group_memberships | benzersiz `user_id` | #8: başka grupta üye |
| `ux_group_memberships_one_admin` | group_memberships | kısmi benzersiz `(group_id) WHERE role = 'Admin'` | UC2, TRG-01 |
| `ck_group_memberships_role` | group_memberships | `role IN ('Admin', 'Member')` | |
| `ix_group_memberships_group_id_joined_at` | group_memberships | index | TRG-01 |
| `ck_groups_capacity_by_type` | groups | BND-02 | UC2 |
| `ux_invite_codes_one_active` | invite_codes | kısmi benzersiz `(group_id) WHERE revoked_at IS NULL` | #10 |
| `ux_invite_codes_code_hash` | invite_codes | benzersiz `code_hash` | #10: üretimde çakışma, yeniden dene |
| `ck_invite_codes_code_hash_length` | invite_codes | `octet_length(code_hash) = 32` | |
| `ux_refresh_tokens_token_hash` | refresh_tokens | benzersiz | |
| `ux_refresh_tokens_replaced_by_id` | refresh_tokens | benzersiz | #6 rotasyon |
| `ck_refresh_tokens_token_hash_length` | refresh_tokens | `octet_length(token_hash) = 32` | |
| `ix_refresh_tokens_user_id` | refresh_tokens | index | |
| `ix_refresh_tokens_family_id` | refresh_tokens | index | #6 aile iptali |
| `ux_asp_net_users_normalized_email` | asp_net_users | benzersiz (Identity'nin `EmailIndex`'i yeniden adlandırılır ve benzersiz yapılır) | #6: e-posta kayıtlı |
| `ux_asp_net_users_normalized_user_name` | asp_net_users | benzersiz (Identity'nin `UserNameIndex`'i yeniden adlandırılır) | |

Migration geçmişi tablosu `__ef_migrations_history` (`UseNpgsql(cs, o => o.MigrationsHistoryTable("__ef_migrations_history"))`). Paket, açıkça adı verilmiş index'leri yeniden adlandırmaz; Identity'nin iki index'i bu yüzden elle adlandırılır. Aynı durum tablo adları için de geçerlidir: Identity tablolarına açıkça `AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` adlarını verir, paket bunları çevirmez. Dört tablo `asp_net_*` adlarını elle alır (`AppUserConfiguration` ve `IdentityUserTablesConfiguration`, adlar `AuthDbNames` içinde). `HasFilter` ve `HasCheckConstraint` içindeki SQL elle yazılan snake_case sütun adları içerir; EF bunları doğrulamaz, DM testleri yakalar.

### State transitions

- **Davet kodu**: `aktif` (`revoked_at` boş, `now <= expires_at`) → `süresi dolmuş` (zamanla, satır değişmez) · `aktif` ya da `süresi dolmuş` → `iptal` (`revoked_at` yazılır: yönetici iptal etti ya da yeni kod üretildi). Hem iptal hem süresi dolmuşsa `INVITE_REVOKED` önceliklidir.
- **Refresh token**: `aktif` → `döndürüldü` (`revoked_at` + `replaced_by_id` yazılır) · `aktif` → `iptal` (çıkış, hesap silme, yeniden kullanım tespitinde tüm `family_id`). Davranış UC1 spec'inde.
- **Grup**: `açık` → `kapalı` (`closed_at` yazılır, üyelikler ve kodlar silinir, satır kalır). Davranış UC10'da.
- **Kullanıcı**: `aktif` → `silinmiş` (bkz. Value sourcing, hesap silme). Davranış özellik #12'de.

### Yazma sırası (kısmi benzersiz index'ler için zorunlu)

PostgreSQL benzersiz index'i her satırda hemen kontrol eder ve EF `SaveChanges` sırasını kısmi index koşuluna göre ayarlamaz; tek `SaveChanges` içinde yeni satır eski satırın güncellemesinden önce yazılıp 23505 verebilir. Bu yüzden:

1. Transaction başlar, grup satırı `SELECT ... FROM groups WHERE id = @id FOR UPDATE` ile kilitlenir.
2. "Bırakılan" satır önce ayrı bir komutla güncellenir: yeni kodda `ExecuteUpdateAsync` ile eski kodun `revoked_at`'i; yönetici devrinde eski yöneticinin `role = 'Member'` (ya da satırın silinmesi).
3. Sonra yeni satır yazılır (`SaveChangesAsync`): yeni kod `INSERT`; yeni yöneticinin `role = 'Admin'`.
4. Commit.

Bu sıra davet kodu üretimi (#10), katılım (#8), yönetici devri (TRG-01, #12 ve UC10) ve grup kapatma (UC10) için bağlayıcıdır.

### API surface

Bu özellik uç nokta eklemez. Şemayı kullanan uç noktalar UC1 (#6), katılım (#8), kod yönetimi (#10), hesap silme (#12) ve Berat'ın UC2 / UC10 spec'lerinde tanımlanır.

### Value sourcing

Sütunları sonraki özellikler yazar; her değerin kaynağı burada sabitlenir ki kimse uydurmasın.

| Eylem | Değer | Kaynak |
|---|---|---|
| Her yazma | `created_at`, `joined_at`, `revoked_at`, `closed_at`, `deleted_at` | `IClock.UtcNow` (DB varsayılanı yok) |
| Davet kodu üretme | `expires_at` | `created_at + InviteCodeRules.Lifetime` (48 saat) |
| Katılım | kod geçerli mi | `revoked_at IS NULL` ve `IClock.UtcNow <= expires_at` (tam 48. saat kabul) |
| Davet kodu üretme ve katılım | `code_hash` | `InviteCodeHasher.Hash(code)` = SHA-256(UTF-8(`Normalize(code)`)), AC-7'deki normalizasyon |
| Davet kodu üretme | düz metin kod | Yalnızca yanıtta bir kez döner, saklanmaz. Alfabe ve en az uzunluk katılım spec'inde (#8). `ux_invite_codes_code_hash` çakışmasında yeni kodla yeniden denenir |
| Grup oluşturma | `type`, `capacity`, `name` | İstek girdisi (UC2); sınırlar `GroupCapacityRules` |
| Grup oluşturma | kurucunun `role` | `Admin` (aynı transaction'da üyelik satırı) |
| Katılım | yeni üyenin `role` | `Member` |
| Katılım | doluluk | Kilitli grup satırı altında `count(*)` üyelik; `count < capacity` ise kabul (yönetici sayılır) |
| Yönetici devri (TRG-01) | yeni yönetici | Kalan üyelerden `ORDER BY joined_at, user_id` ilk satır. Kalan üye yoksa grup kapatılır (UC10'da kesinleşir) |
| Giriş | `family_id` | Yeni `Guid`; rotasyonda eski token'dan devralınır |
| Giriş ve rotasyon | `token_hash` | Rastgele token'ın SHA-256'sı (üretim UC1 spec'inde) |
| Giriş ve rotasyon | `expires_at` (refresh) | `created_at + RefreshTokenRules.Lifetime` (7 gün) |
| Kayıt | `display_name`, `email` | İstek girdisi (UC1) |
| Kayıt | `user_name` | `email` ile aynı değer |
| Hesap silme | anonim değerler | `display_name`, `email`, `user_name` = `DeletedUser_<user.Id>`; `normalized_email`, `normalized_user_name` bunların büyük harflisi; `password_hash` = null; `security_stamp` yenilenir; `deleted_at` yazılır; üyelik satırı aynı transaction'da silinir (gerekirse önce yönetici devri); refresh token'lar iptal |
| Giriş | silinmiş kullanıcı engeli | `deleted_at IS NOT NULL` ise giriş reddedilir (null `password_hash` ikinci güvence) |

Süresi dolmuş ya da iptal edilmiş refresh token ve davet kodu satırları şimdilik temizlenmez (ders ölçeğinde hacim küçük).

### Key invariants

| Kural | Nerede korunur |
|---|---|
| Kullanıcı en fazla bir gruptadır | `ux_group_memberships_user_id` |
| `(group_id, user_id)` tekildir | `pk_group_memberships` |
| Grupta en fazla bir yönetici | `ux_group_memberships_one_admin` |
| Açık grupta en az bir yönetici | Servis kuralı (UC2 kurulum, TRG-01 devir); DB tutamaz |
| Kapasite türe göre (BND-02) | `ck_groups_capacity_by_type` + `GroupCapacityRules` (validator aynı sabitleri okur) |
| Üye sayısı kapasiteyi aşmaz (BND-01) | Katılımda grup satırına `FOR UPDATE`, sonra `count(*)`; üye sayısı sütunu saklanmaz |
| Kapalı gruba katılım yok | Katılım kilitli satırda `closed_at IS NULL` kontrol eder; kapatma (UC10) aynı satır kilidini alır |
| Grupta tek iptal edilmemiş kod | `ux_invite_codes_one_active` + yazma sırası (önce iptal, sonra ekleme) |
| Kod düz metin saklanmaz | Şemada düz metin sütunu yok; `ck_invite_codes_code_hash_length` |
| Token en fazla bir kez döndürülür | `UPDATE refresh_tokens SET revoked_at = @now WHERE id = @id AND revoked_at IS NULL`, etkilenen satır 1 değilse yeniden kullanım; `ux_refresh_tokens_replaced_by_id` ikinci güvence |
| Kullanıcı satırı silinmez, anonimleştirilir | FK'ler restrict; özellik #12 |

Check kısıtının SQL metni `GroupCapacityRules` sabitlerinden üretilir (sınırlar tek yerde). Diğer modüllerin `groups` ve `asp_net_users`'a FK'leri `Restrict` olmalıdır (kullanıcı silinmez, grup kapatılınca satır kalır).

### Security model

- Kişisel veri (KVKK): yalnızca `display_name` ve `email` (`user_name` e-postanın kopyası). Telefon sütunları Identity şemasında kalır ama hiç yazılmaz. Hesap silmede kişisel veri taşıyan tüm sütunlar yeniden yazılır (Value sourcing).
- Davet kodu ve refresh token yalnızca özet olarak durur. Refresh token rastgele ve uzun olduğundan özeti geri çevrilemez. Davet kodu kısa olduğu için tuzsuz SHA-256 özeti çevrimdışı deneme yanılmayla kırılabilir; koruma kodun uzunluğuna ve 48 saatlik ömrüne dayanır, en az uzunluk #8'de belirlenir. Demo kodu (`DEMO-ROOM-2026`) bilerek zayıftır ve yalnızca Development'ta vardır.
- Şifre hash'i Identity'nin `PasswordHasher`'ı ile (SEC-04).
- Grup verisine erişim kontrolü bu spec'te değil, grup izolasyonu özelliğinde (#9, SEC-03).
- `EnableSensitiveDataLogging` kapalı kalır (SEC-07).

### Configuration required

Yeni ortam değişkeni ya da sır yok. Yeni paketler `Directory.Packages.props`'a eklenir (ortak dosya, ekip onayı):
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` `8.0.x` (mevcut Microsoft paketleriyle aynı yama, `8.0.34`)
- `EFCore.NamingConventions` `8.0.x` (EF Core 8 serisinin en güncel sürümü, NuGet'te ve Npgsql 8.0.11 ile uyumunu doğrula)

Identity servis kaydı (`AddIdentityCore<AppUser>().AddEntityFrameworkStores<AppDbContext>()`, `RequireUniqueEmail`, lockout 5 deneme / 15 dk) bu özelliğin değil, UC1'in (#6) işidir; o da ortak `Program.cs`'e dokunur.

### Critical test scenarios

Entegrasyon testleri `Roomly.Api.IntegrationTests` içinde, `backend/AGENTS.md`'deki tek paylaşılan Postgres konteyneri (xUnit collection fixture) üzerinde çalışır; normalizasyon testleri `Roomly.Api.UnitTests` içinde. Kısıt testleri hatanın EF'in izleyicisinden değil veritabanından geldiğini doğrulamak için ham SQL ya da her satır için ayrı `DbContext` kullanır ve `PostgresException.SqlState` ile `ConstraintName`'i kontrol eder. Adlandırma: proje gereksinim kimliği varsa o (`BND02`), yoksa bu spec'in AC'si `DM` önekiyle (`DM04` = AC-4); eşleme `docs/traceability.md`'de.

- Happy path: migration boş DB'ye uygulanır, beklenen tablolar var, rol tabloları yok, tüm adlar snake_case (`information_schema` ve `pg_indexes` sorgusuyla), verifies **AC-1**, **AC-2** (`Migrate_OnEmptyDatabase_CreatesExpectedSchema_DM01`)
- Mutlu yol: bir grup, bir yönetici, bir üye, bir aktif kod yazılır ve okunur, verifies **AC-12**, **AC-13**
- Kısıt: aynı kullanıcı ikinci gruba eklenince 23505 ve `ux_group_memberships_user_id`, verifies **AC-4**, **AC-14** (`InsertMembership_WhenUserInAnotherGroup_IsRejected_DM04`)
- Kısıt: aynı çift tekrar, ikinci yönetici, ikinci aktif kod; her biri beklenen kısıt adıyla, verifies **AC-3**, **AC-5**, **AC-8**, **AC-14**
- Yazma sırası: aktif kodu olan grupta önce `ExecuteUpdateAsync` ile iptal, sonra yeni kod ekleme tek transaction'da başarılı, verifies **AC-8**
- Sınır değerleri (veri güdümlü): `Ev` 1/2/20/21, `Yurt` 1/2/6/7, verifies **AC-6** (`InsertGroup_CapacityOutsideTypeRange_IsRejected_BND02`)
- Normalizasyon: tire, boşluk, tab, NBSP, `U+2013` (en dash) ve küçük harfli yazımlar aynı özeti verir, özet 32 bayt, verifies **AC-7** (unit)
- Silme davranışı: grup silinince üyelik ve kodlar gider; üyeliği olan kullanıcı silinemez (23503), verifies **AC-11**
- Kullanıcı: 51 karakterlik ad reddedilir, `DeletedUser_<Guid>` kabul edilir, aynı `normalized_email` reddedilir, verifies **AC-10**
- Refresh token: aynı `token_hash` ve aynı `replaced_by_id` reddedilir, zincir yazılır, verifies **AC-9**

## Build plan

Tracer Bullet altında bu özellik ince yolun en alt katmanıdır: tek bir migration bütün hedef modeli kurar (beş tablo küçük ve birbirine bağlı; dilimlere bölmek, ortak `ModelSnapshot`'ı birkaç kez çakıştırmaktan başka bir şey kazandırmaz). Sonraki dilimler (UC1, katılım) bu şemanın üzerine uç nokta ekler.

1. `Directory.Packages.props`'a `Microsoft.AspNetCore.Identity.EntityFrameworkCore` ve `EFCore.NamingConventions` `8.0.x` ekle, `Roomly.Api.csproj`'a sürümsüz referans ver, satisfies **AC-1**, **AC-2**
2. `Program.cs`'te `UseNpgsql(cs, o => o.MigrationsHistoryTable("__ef_migrations_history")).UseSnakeCaseNamingConvention()`; `AppDbContext`'i `IdentityUserContext<AppUser, Guid>` tabanına geçir (`DbSet` yok, `ApplyConfigurationsFromAssembly` kalır), satisfies **AC-1**, **AC-2**, **AC-12**
3. `Features/Auth/`: `AuthDbNames`, `RefreshTokenRules` (`Lifetime = 7 gün`), `AppUser` (`DisplayName`, `CreatedAt`, `DeletedAt`) + `AppUserConfiguration` (ad 50, Identity index'lerini yeniden adlandır, e-postayı benzersiz yap); `RefreshToken` + `RefreshTokenConfiguration` (32 bayt check'leri, benzersiz index'ler, `family_id` ve `user_id` index'leri, self FK set null), satisfies **AC-2**, **AC-9**, **AC-10**, **AC-13**, **AC-14**
4. `Features/Groups/`: `GroupsDbNames`, `GroupType`, `MembershipRole`, `GroupCapacityRules` (min/max türe göre, check SQL'i buradan), `InviteCodeRules` (`Lifetime = 48 saat`), `InviteCodeHasher` (`Normalize`, `Hash`), satisfies **AC-6**, **AC-7**, **AC-14**
5. `Features/Groups/`: `Group`, `GroupMembership`, `InviteCode` ve configuration'ları (adlı check kısıtları, bileşik PK, `user_id` benzersiz, kısmi index'ler, `(group_id, joined_at)` index, FK silme davranışları), satisfies **AC-3**, **AC-4**, **AC-5**, **AC-6**, **AC-8**, **AC-11**, **AC-13**, **AC-14**
6. `dotnet ef migrations add InitialDataModel --project src/Roomly.Api --output-dir Infrastructure/Persistence/Migrations`; üretilen SQL'i gözden geçir (`dotnet ef migrations script`): tüm adlar snake_case ve *Kısıt adları* tablosuyla aynı, check ve kısmi index'ler var, `now()` yok, satisfies **AC-1**, **AC-2**, **AC-13**, **AC-14**
7. `Roomly.Api.IntegrationTests`'e `backend/AGENTS.md`'de tarif edilen tek paylaşılan Postgres collection fixture'ını kur (henüz yok; `postgres:16-alpine`, açılışta `Database.MigrateAsync()`); ileride `WebApplicationFactory<Program>` aynı konteyneri kullanır, satisfies **AC-1**
8. Kısıt, yazma sırası ve silme davranışı entegrasyon testleri (yukarıdaki senaryolar), satisfies **AC-1** ile **AC-6**, **AC-8** ile **AC-14**
9. `InviteCodeHasher` unit testleri, satisfies **AC-7**
10. `docs/traceability.md`'yi oluştur, `DM01` ile `DM14` ve `BND02` satırlarını ekle; `Features/Auth/README.md` ve `Features/Groups/README.md`'ye sahiplik, yazma sırası ve varsayımları yaz, satisfies **AC-12**, **AC-14**

## Consequences

**Positive**:
- Yarış durumları ve servis hataları bozuk veri yazamaz; testler kuralı doğrudan veritabanında doğrular.
- Kısıt adları sabit, sonraki özellikler veritabanı hatasını güvenle hata koduna çevirir.
- Modüller `AppDbContext`'e dokunmadan tablo ekler; ortak dosyada çakışma yalnızca `ModelSnapshot`'ta kalır.
- snake_case sayesinde `FOR UPDATE` gibi ham SQL tırnaksız ve okunaklı.
- Rotasyon, anonimleştirme ve kapatma sütunları şimdiden var; UC1, #12 ve UC10 ortak migration dosyasına tekrar dokunmadan başlar.

**Negative / tradeoffs**:
- Yönetici kodu yalnızca üretildiği anda görür; tekrar paylaşmak için yeni kod üretmesi gerekir (eski kod iptal olur). Scope #10'daki "aktif kodu görür" ifadesi buna göre değişmeli.
- Kısmi index'ler yüzünden "önce bırak, sonra ekle" yazma sırası zorunlu; bunu unutan bir servis 23505 alır. Sıra README'de ve bu spec'te yazılı, testle korunur.
- Kapasite sınırları iki yerde etkili (check kısıtı ve validator); sınırı değiştirmek migration ister.
- Tek grup kuralını gevşetmek (birden fazla grup) bir migration ve `user_id` index'ini kaldırmak ister; dashboard'ın "kullanıcının grubu" varsayımı da değişir.
- `EFCore.NamingConventions` ek bir bağımlılıktır ve açıkça adlandırılmış index'leri ve tabloları çevirmez; Identity'nin index'leri ve dört tablosu elle adlandırılır, EF Core yükseltmelerinde onun da uyumlu sürümü beklenir.
- Kısa davet kodunun tuzsuz özeti deneme yanılmaya açıktır; güvenlik kod uzunluğuna ve kısa ömre dayanır.
- Identity'nin kullanılmayan telefon ve 2FA sütunları şemada kalır.
- `Features/Groups/` iki kişinin (Berk ve Berat) ortak klasörüdür; "sahip yalnızca kendi klasörüne dokunur" kuralına bir istisna.

**Neutral**:
- Identity tablo adları `asp_net_*` olarak kalır (başka bir ada taşınmaz). Bu adlar paketten değil, Identity'nin PascalCase adlarının configuration'da elle snake_case verilmesinden gelir.
- Süresi dolmuş token ve kod satırları temizlenmez.
- Development'ta açılışta otomatik `Migrate` ve demo seed özellik #5'in işi; bu özellikte migration testlerde fixture ile uygulanır.

## Follow-up

- [ ] Scope #10'u güncelle: yönetici kodu yalnızca üretildiği anda görür, tekrar paylaşmak için yeniden üretir (`/scope`).
- [ ] Katılım spec'i (#8): kod formatı kontrolü yalnızca uzunluk ve `A-Z0-9`; üretilen kodlar Base32 alfabesinden ve deneme yanılmaya dayanacak en az uzunlukta; normalizasyon `InviteCodeHasher` ile aynı; hata önceliği `INVITE_REVOKED` > `INVITE_EXPIRED`.
- [ ] Yerel ortam (#5): demo kodu idempotent upsert ile yazılır (`code_hash` üzerinden), `expires_at` uzak gelecekte, `created_by_user_id` demo yönetici; tekrar çalışınca kopya ya da 23505 yok.
- [ ] UC10 (Berat): son üye ayrılınca grubun kapanması ve kapatmada satır kilidi bu spec'teki yazma sırasına uyar.
- [ ] `Features/Groups/` ortak sahipliğini (Berk: davet kodu ve katılım, Berat: grup oluşturma ve kapatma) `CONTRIBUTING.md`'de ve modül README'sinde kayda geçir.
- [ ] Diğer modüller için kural: `groups` ve `asp_net_users`'a FK'ler `Restrict`; tablolar `IEntityTypeConfiguration` ile kendi klasöründe; kısmi index'lerde yazma sırası (`/sync` ile `backend/AGENTS.md`'ye).
- [ ] `EFCore.NamingConventions` 8.0.x serisinin en güncel sürümünü NuGet'te doğrula; Npgsql'in `Guid` PK'lere DB varsayılanı koymadığını migration SQL'inde kontrol et.

## Rationale

Gerekçe ve seçenekler: [rationale.md](rationale.md).
