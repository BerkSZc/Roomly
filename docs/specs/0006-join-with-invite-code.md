# 0006. Davet koduyla gruba katılım (UC3)

**Date**: 2026-11-22
**Status**: In Progress

## Summary

Grubu olmayan kullanıcı, yöneticinin paylaştığı davet kodunu (ör. `ABCD-EFGH-JKLM`) girip gruba üye olarak katılır. Kod büyük küçük harf, boşluk ve tire farkı gözetmeden okunur. Bütün kurallar (kod geçerli mi, süresi doldu mu, iptal mi, grup dolu mu, kullanıcı zaten üye mi) sunucuda, grubun satırı kilitlenmiş tek bir veritabanı işlemi içinde kontrol edilir; böylece aynı anda gelen istekler kapasiteyi aşamaz. Kod 48 saat geçerlidir (tam 48. saatte hâlâ kabul edilir) ve grup dolana kadar tekrar kullanılabilir. Katılınca uygulama kullanıcıyı doğrudan dashboard'a götürür.

## Context

Scope #8, Slice 3. Spec 0002 `invite_codes` (yalnızca SHA-256 özeti, `expires_at`, `revoked_at`), `group_memberships` (tek grup, bileşik PK) ve `groups.capacity` (yönetici dahil) kurallarını, `InviteCodeHasher` normalizasyonunu, yazma sırasını ve "kilitli satır altında `count(*)`" doluluk kuralını sabitledi; hata önceliği `INVITE_REVOKED` > `INVITE_EXPIRED`. Spec 0004 oturumu ve `GET /api/auth/me` grup bilgisini; spec 0005 dashboard'u kurdu; `/join` şimdilik placeholder. Gereksinimler: BND-01 (kapasite aşılamaz), BND-03 (48 saat, zaman `IClock`'tan), TRG-01 (kurallar backend'de, `FOR UPDATE` ile kilitli tek transaction içinde). Kod üretimi ve iptali scope #10, deep link scope #11.

## Requirements

**User stories**:
- Grubu olmayan kullanıcı olarak arkadaşımın paylaştığı kodla gruba katılıp hemen dashboard'u görmek istiyorum.
- Kullanıcı olarak kod neden kabul edilmediyse (yanlış, süresi dolmuş, iptal, grup dolu) bunu anlaşılır Türkçe bir mesajla görmek istiyorum.
- Yönetici olarak grubun kapasitesinin aynı anda gelen katılımlarla bile aşılmamasını istiyorum.

**Acceptance criteria**:
- **AC-1**: `POST /api/groups/join` (bearer) `{ "code": "..." }` geçerli kodla 200 ve `{ group: { id, name, type, role: "Member" } }` döner; `group_memberships`'e `role = Member`, `joined_at = IClock.UtcNow` satırı yazılır. Kod tüketilmez; aynı kod kapasite dolana kadar başka kullanıcılarca da kullanılabilir.
- **AC-2**: Kod `InviteCodeHasher.Normalize` ile okunur (boşluk, tab, NBSP, `-`, `U+2013` silinir, büyük harfe çevrilir); `demo-room-2026`, `DEMO ROOM 2026` ve `DEMO-ROOM-2026` aynı koddur. Normalize edilmiş kod `^[A-Z0-9]{8,32}$` değilse veritabanına bakılmadan `INVITE_INVALID` döner. Boş kod 400 `VALIDATION_FAILED`.
- **AC-3**: Bilinmeyen kod ya da kapatılmış (`closed_at` dolu) gruba ait kod 404 `INVITE_INVALID` döner.
- **AC-4** (BND-03): Kod `IClock.UtcNow <= expires_at` iken geçerlidir: oluşturulmasından tam 48 saat sonra katılım kabul edilir, 48 saat + 1 saniyede 410 `INVITE_EXPIRED` döner.
- **AC-5**: İptal edilmiş (`revoked_at` dolu) kod 410 `INVITE_REVOKED` döner; kod hem iptal hem süresi dolmuşsa `INVITE_REVOKED` önceliklidir.
- **AC-6**: Kullanıcı zaten bu grubun üyesiyse 409 `ALREADY_MEMBER` ("Zaten bu grubun üyesisiniz.") döner, kodun süresi dolmuş ya da iptal olsa bile; başka bir grubun üyesiyse 409 `ALREADY_IN_GROUP` döner.
- **AC-7** (BND-01): Üye sayısı (yönetici dahil) kapasiteye eşitse 409 `CAPACITY_EXCEEDED` döner.
- **AC-8** (BND-01, TRG-01): Kontroller ve yazma tek transaction içinde, grup satırı `SELECT ... FOR UPDATE` ile kilitliyken yapılır; kapasitesi 1 boş yeri olan gruba aynı anda 5 farklı kullanıcı katılmaya çalışınca tam 1'i başarılı olur, kalanlar `CAPACITY_EXCEEDED` alır ve üye sayısı kapasiteyi aşmaz. Aynı kullanıcının yarışan iki isteğinde ikincisi `ALREADY_MEMBER` alır (500 değil).
- **AC-9**: Hata önceliği: `VALIDATION_FAILED` → `INVITE_INVALID` → `ALREADY_MEMBER` → `INVITE_REVOKED` → `INVITE_EXPIRED` → `ALREADY_IN_GROUP` → `CAPACITY_EXCEEDED`.
- **AC-10**: Uç kullanıcı başına dakikada 10 istekle sınırlıdır (`RateLimiting:Join`), aşılınca 429 `RATE_LIMITED`; oturumsuz istek 401. Loglarda düz kod yazılmaz.
- **AC-11**: Mobil `/join` ekranı "Davet kodu" alanını (büyük harf klavye, ipucu `ABCD-EFGH-JKLM`) ve "Katıl" düğmesini gösterir; istemci AC-2'deki biçimi kontrol eder ("Davet kodu geçersiz görünüyor."); istek sürerken düğme yükleniyor durumundadır; sunucu hataları alan altında Türkçe mesajla, bağlantı hatası snackbar'da gösterilir. Ekran `?code=` ile açılırsa alan dolu gelir.
- **AC-12**: Katılım başarılı olunca uygulama `me`'yi yeniden okur, kullanıcı Ana Sayfa'ya yönlenir ve "<grup adı> grubuna katıldın." snackbar'ı görünür.
- **AC-13**: `USE_MOCK=true` iken katılım bellekte çalışır: `DEMO-ROOM-2026` "Demo Ev"e üye yapar; `DOLU-GRUP-2026` `CAPACITY_EXCEEDED`, `ESKI-KOD-2026` `INVITE_EXPIRED`, `IPTAL-KOD-2026` `INVITE_REVOKED`, diğer biçimi geçerli kodlar `INVITE_INVALID` üretir.

## Options considered

### Option 1: Grup satırını kilitleyen tek transaction, üyelik satırı ekleme (seçilen)

`invite_codes`'tan özetle bul, `groups` satırını `FOR UPDATE` ile kilitle, kuralları kilit altında kontrol et, `count(*)` ile doluluğa bak, üyeliği yaz, commit.
**Pros**: spec 0002'nin yazma sırası ve doluluk kuralıyla aynı; yönetici kod iptali (#10) ve grup kapatma (UC10) aynı kilidi aldığı için iptal / kapatma ile yarışta da tutarlı. **Cons**: aynı gruba katılımlar sıraya girer (ders ölçeğinde önemsiz).

### Option 2: `SERIALIZABLE` izolasyon ve yeniden deneme

**Pros**: elle kilit yok. **Cons**: serileştirme hatasında yeniden deneme döngüsü gerekir, testte belirsiz; ekip kuralı `FOR UPDATE`.

### Option 3: `groups`'ta saklanan üye sayacı ve koşullu `UPDATE`

**Pros**: tek satır güncelleme. **Cons**: spec 0002 üye sayısını saklamamayı seçti; sayaç ile gerçek satırlar ayrışabilir.

## Decision

**Chosen option**: Option 1: `JoinGroupService` grup satırını `FOR UPDATE` ile kilitleyip kuralları kilit altında kontrol eden tek transaction; mobilde `features/invites/` modülü.

**Implementation skills**: `csharp-xunit` (`~/.claude/skills/csharp-xunit/`, backend testleri)

## Rationale

Spec 0002 bu kilidi ve yazma sırasını zaten bütün grup yazmaları için bağlayıcı yaptı; katılımın aynı kalıbı kullanması #10 (kod iptali) ve UC10 (kapatma) ile tutarlılığı garanti eder. Kodun tüketilmemesi ve kapasite dolana kadar tekrar kullanılabilmesi scope #10'un kuralı. `ALREADY_MEMBER`'ın süre ve iptalden önce gelmesi, eski bir kodu tekrar giren üyeye "kod geçersiz" yerine doğru durumu söyler. Kullanıcı başına rate limit, kısa kodların deneme yanılmayla bulunmasını yavaşlatır (spec 0002 güvenlik notu).

## Feature design

### Backend yerleşimi (`Features/Groups/`, Berk'in kısmı)

| Dosya | Sorumluluk |
|---|---|
| `JoinGroupController` | `POST /api/groups/join`, `[Authorize]`, rate limit `join` |
| `JoinGroupService` | Kurallar ve öncelik (AC-9), `Result<JoinGroupResponse>` |
| `JoinGroupRepository` (`IJoinGroupRepository`) | Transaction, `FOR UPDATE`, sayım, üyelik ekleme, kısıt hatası eşlemesi |
| `GroupErrors` | `INVITE_INVALID` 404, `INVITE_EXPIRED` 410, `INVITE_REVOKED` 410, `ALREADY_MEMBER` 409, `ALREADY_IN_GROUP` 409, `CAPACITY_EXCEEDED` 409 |
| `InviteCodeFormat` | `IsWellFormed(normalized)`: `^[A-Z0-9]{8,32}$` |
| `Dtos/JoinGroupDtos.cs`, `Validators/JoinGroupRequestValidator.cs` | İstek / yanıt, boş kod kontrolü |
| `GroupsServiceCollectionExtensions` | `AddRoomlyGroups(configuration)`: servisler ve `join` rate limit politikası |

`Program.cs`'e tek satır: `AddRoomlyGroups`. Yeni paket ya da migration yok.

### Data model sketch

Şema değişikliği yok (spec 0002). Yazılan tek satır `group_memberships (group_id, user_id, role = 'Member', joined_at)`.

### API surface

| Endpoint | Method | Key inputs | Key outputs | Auth | Key errors |
|---|---|---|---|---|---|
| `/api/groups/join` | POST | `code` (string, zorunlu) | 200 `{ group: GroupSummaryDto }` | bearer | 400 `VALIDATION_FAILED`, 401, 404 `INVITE_INVALID`, 409 `ALREADY_MEMBER` / `ALREADY_IN_GROUP` / `CAPACITY_EXCEEDED`, 410 `INVITE_EXPIRED` / `INVITE_REVOKED`, 429 |

`GroupSummaryDto` spec 0004'teki ile aynı (`id`, `name`, `type`, `role`).

### Akış (tek transaction, ReadCommitted)

1. `normalized = InviteCodeHasher.Normalize(code)`; biçim geçersizse `INVITE_INVALID`.
2. `SELECT id, group_id FROM invite_codes WHERE code_hash = @hash`; yoksa `INVITE_INVALID`.
3. `SELECT ... FROM groups WHERE id = @groupId FOR UPDATE`; `closed_at` doluysa `INVITE_INVALID`.
4. Kod satırı kilit altında tekrar okunur (yarışan iptali görmek için).
5. Kullanıcının üyeliği: bu grupsa `ALREADY_MEMBER`.
6. `revoked_at` doluysa `INVITE_REVOKED`; `IClock.UtcNow > expires_at` ise `INVITE_EXPIRED`.
7. Kullanıcının başka grupta üyeliği varsa `ALREADY_IN_GROUP`.
8. `SELECT count(*) FROM group_memberships WHERE group_id = @groupId`; `count >= capacity` ise `CAPACITY_EXCEEDED`.
9. Üyelik `INSERT`, commit. `SaveChanges`'te 23505: `pk_group_memberships` → `ALREADY_MEMBER`, `ux_group_memberships_user_id` → `ALREADY_IN_GROUP` (iki farklı grubu kilitleyen yarış).

Hata durumunda transaction geri alınır.

### Value sourcing

| Eylem | Değer | Kaynak |
|---|---|---|
| Katılım | kullanıcı | `User.GetUserId()` (JWT `sub`) |
| Katılım | kodun özeti | `InviteCodeHasher.Hash(code)` (spec 0002 AC-7) |
| Katılım | biçim | `InviteCodeFormat.IsWellFormed(InviteCodeHasher.Normalize(code))` |
| Katılım | şimdi | `IClock.UtcNow` |
| Katılım | süre geçerli mi | `now <= invite_codes.expires_at` |
| Katılım | doluluk | kilit altında `count(*)` üyelik, `groups.capacity` ile karşılaştırma |
| Katılım | yeni üyenin `role`, `joined_at` | `Member`, `IClock.UtcNow` |
| Yanıt | `group` | kilitli `groups` satırı + `Member` |
| Rate limit | anahtar | `sub` claim'i (yoksa IP) |
| Mobil | biçim kontrolü | Aynı normalizasyon ve desen (`invite_code_format.dart`) |
| Mobil | başarı mesajı | Yanıttaki `group.name` |
| Mobil | yönlendirme | `SessionController.refreshUser()` → `user.group != null` → `/home` (spec 0004 redirect) |
| Mobil | hata mesajı | `GroupErrors` kodu → `features/invites/presentation/join_error_messages.dart` |

### Key invariants

- Üye sayısı hiçbir zaman `capacity`'yi aşmaz (kilit + sayım; DB tek grup ve bileşik PK kısıtları ikinci güvence).
- Kod düz metin olarak saklanmaz, loglanmaz.
- Kod katılımda değişmez (tüketilmez).

### Security model

- Yalnızca oturum açmış kullanıcı katılabilir; kullanıcı kimliği istekten değil token'dan gelir.
- Kural kontrolleri yalnızca sunucuda, kilitli transaction içinde (TRG-01); istemci kontrolü yalnızca kullanıcı deneyimi içindir.
- Kullanıcı başına dakikada 10 deneme; 12 karakterlik Base32 kod (60 bit) ve 48 saatlik ömürle deneme yanılma pratik değildir. Demo kodu bilerek zayıf ve yalnızca Development'ta.
- Hata yanıtı kodun başka bir gruba ait olup olmadığını ele vermez (bilinmeyen ve kapalı aynı yanıt).

### Configuration required

- `RateLimiting:Join:PermitLimit` = 10, `RateLimiting:Join:WindowSeconds` = 60 (`appsettings.json`)

### Mobil yerleşimi (`features/invites/`, Berk)

| Dosya | Sorumluluk |
|---|---|
| `data/invite_repository.dart` | `InviteRepository.join(code)`, `RemoteInviteRepository`, `MockInviteRepository`, `inviteRepositoryProvider` |
| `presentation/invite_code_format.dart` | `normalize`, `isWellFormed` |
| `presentation/join_error_messages.dart` | Kod → Türkçe mesaj |
| `presentation/join_screen.dart` | Form, hata ve başarı akışı |

Router `/join` artık `JoinScreen(initialCode: state.uri.queryParameters['code'])`.

Mock katılım, mock auth deposundaki oturumdaki kullanıcıya grubu yazar (`MockAuthRepository.assignGroupToCurrentUser`), böylece `me` yeni grubu döner.

### Critical test scenarios

Servis düzeyi entegrasyon testleri gerçek Postgres ve `FakeClock` ile (`tests/Roomly.Api.IntegrationTests/Groups/JoinGroupServiceTests.cs`), HTTP testleri `JoinGroupEndpointTests.cs`, unit testler `tests/Roomly.Api.UnitTests/Features/Groups/`. Ad: gereksinim kimliği varsa o (`BND01`, `BND03`, `TRG01`), yoksa `JOIN<nn>`. Mobil testler `mobile/test/features/invites/`.
- Geçerli kodla katılım, satır yazılır, kod değişmez; ikinci kullanıcı aynı kodla katılır, verifies **AC-1**
- Küçük harf, boşluklu ve tireli yazım aynı koda gider; `ab` ve `KOD!` veritabanına bakmadan `INVITE_INVALID`, verifies **AC-2**
- Bilinmeyen ve kapalı grup kodu `INVITE_INVALID`, verifies **AC-3**
- `FakeClock` tam 48 saat → başarılı; 48 saat + 1 sn → `INVITE_EXPIRED`, verifies **AC-4** (`Join_AtExactlyFortyEightHours_Succeeds_BND03`, `Join_OneSecondAfterExpiry_ReturnsInviteExpired_BND03`)
- İptal → `INVITE_REVOKED`; iptal + süresi dolmuş → `INVITE_REVOKED`, verifies **AC-5**
- Üye kendi grubunun (süresi dolmuş) koduyla → `ALREADY_MEMBER`; başka grubun üyesi → `ALREADY_IN_GROUP`, verifies **AC-6**, **AC-9**
- Dolu grup → `CAPACITY_EXCEEDED`, verifies **AC-7**
- 1 boş yer, 5 paralel katılım → 1 başarı, 4 `CAPACITY_EXCEEDED`, üye sayısı = kapasite, verifies **AC-8** (`Join_ConcurrentRequests_NeverExceedCapacity_BND01`)
- Aynı kullanıcı paralel iki istek → biri 200, diğeri `ALREADY_MEMBER`, verifies **AC-8** (`TRG01`)
- HTTP: 200 gövdesi, 401 oturumsuz, hata gövdesinde `code`, sonra `me` grubu döner, verifies **AC-1**, **AC-10**
- Mobil: biçim kontrolü, sunucu hatası alan altında, başarıda Ana Sayfa ve snackbar, `?code=` dolu açılış, mock kodları, verifies **AC-11**, **AC-12**, **AC-13**

## Build plan

Tracer Bullet'ın üçüncü dilimi: UC1'in açtığı "grubu yok → onboarding" yolunun "Koda Katıl" ucu uçtan uca (DB → API → Flutter) bağlanır.

1. [x] `GroupErrors`, `InviteCodeFormat`, DTO ve validator, satisfies **AC-2**, **AC-9**
2. [x] `JoinGroupRepository` (transaction, `FOR UPDATE`, sayım, kısıt eşlemesi) ve `JoinGroupService` (kurallar, öncelik), satisfies **AC-1**, **AC-3** ile **AC-9**
3. [x] `JoinGroupController`, `AddRoomlyGroups`, `join` rate limit, `Program.cs` ve `appsettings.json`, satisfies **AC-1**, **AC-10**
4. [x] Backend testleri (servis + HTTP + unit), satisfies **AC-1** ile **AC-10**
5. [x] Mobil: `InviteRepository` (Remote + Mock), biçim ve mesajlar, `MockAuthRepository.assignGroupToCurrentUser`, satisfies **AC-2**, **AC-13**
6. [x] Mobil: `JoinScreen`, router `/join`, başarıda `refreshUser` ve snackbar, satisfies **AC-11**, **AC-12**
7. [x] Mobil testler, `docs/traceability.md`, `Features/Groups/README.md`, satisfies **AC-11** ile **AC-13**

## Consequences

**Positive**:
- Kapasite, süre ve iptal kuralları yarışta bile tutarlı; kod iptali (#10) ve kapatma (UC10) aynı kilidi kullanacak.
- Kullanıcı neden reddedildiğini net bir mesajla görür.

**Negative / tradeoffs**:
- Aynı gruba katılımlar sıraya girer.
- `ALREADY_MEMBER`, bir kodun hangi gruba ait olduğunu o grubun üyesine doğrular (zaten üye olduğu için zararsız).
- Mock katılım mock auth deposuna bağlı (yalnızca mock modunda).

**Neutral**:
- Şema değişikliği ve yeni paket yok.

## Follow-up

- [ ] Davet kodu yönetimi (#10): kod üretiminde aynı `InviteCodeFormat` deseni ve 12 karakterlik Base32 (`XXXX-XXXX-XXXX`); iptal ve üretim aynı grup kilidini alır.
- [ ] Deep link (#11): `roomly://join?code=...` → `/join?code=...`; ekran `initialCode`'u zaten kabul ediyor.
- [ ] Yerel ortam (#5): `DEMO-ROOM-2026` gerçek API'de seed edilene kadar gerçek modda katılım için kodu elle eklemek gerekir.
