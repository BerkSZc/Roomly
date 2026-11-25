# Groups

Ortak klasör: Berk (davet kodu ve katılım, UC3) ve Berat (grup oluşturma ve kapatma, UC2 / UC10). Şema [spec 0002](../../../../../docs/specs/0002-data-model/index.md).

## Tablolar

- `groups` (`Group`): ad (en fazla 50, benzersiz değil), tür (`Ev` / `Yurt`), kapasite (yönetici dahil), `closed_at`. Üye sayısı saklanmaz.
- `group_memberships` (`GroupMembership`): PK `(group_id, user_id)`, rol (`Admin` / `Member`), `joined_at`.
- `invite_codes` (`InviteCode`): yalnızca 32 baytlık `code_hash`, `expires_at`, `revoked_at`. Düz metin kod hiçbir yerde saklanmaz.

## Veritabanının da koruduğu kurallar

| Kural | Kısıt (`GroupsDbNames`) |
|---|---|
| Kullanıcı en fazla bir gruptadır | `ux_group_memberships_user_id` |
| Grupta en fazla bir yönetici | `ux_group_memberships_one_admin` |
| Grupta en fazla bir iptal edilmemiş kod | `ux_invite_codes_one_active` |
| Kapasite türe göre (BND-02) | `ck_groups_capacity_by_type`, SQL'i `GroupCapacityRules`'tan üretilir |

Kapasite sınırlarının tek kaynağı `GroupCapacityRules`: UC2 validator'ı `GroupCapacityRules.IsValid` ya da `RangeFor` kullanmalı. Sınırı değiştirmek bir migration ister.

## Yazma sırası (zorunlu)

PostgreSQL benzersiz index'i her satırda hemen kontrol eder; EF tek `SaveChanges` içinde yeni satırı eski satırın güncellemesinden önce yazabilir ve 23505 alırsınız. Kısmi benzersiz index'e dokunan her işlemde:

1. Transaction başlat, grubu kilitle: `SELECT * FROM groups WHERE id = @id FOR UPDATE`.
2. Bırakılan satırı önce ayrı bir komutla güncelle (`ExecuteUpdateAsync`): eski kodun `revoked_at`'i ya da eski yöneticinin `role = 'Member'`'ı (veya satırın silinmesi).
3. Sonra yeni satırı yaz (`SaveChangesAsync`): yeni kod ya da yeni yöneticinin `role = 'Admin'`'ı.
4. Commit.

Bu sıra davet kodu üretimi (#10), katılım (#8), yönetici devri (TRG-02) ve grup kapatma (UC10) için bağlayıcıdır. Örnek: `InviteCodeConstraintTests.RegenerateInviteCode_RevokeThenInsertInOneTransaction_Succeeds_DM08`.

## Davet kodu

- Özet: `InviteCodeHasher.Hash(code)` = SHA-256(UTF-8(`Normalize(code)`)). `Normalize` tüm boşlukları, `-` ve en dash'i siler, büyük harfe çevirir. Üretim ve katılım aynı fonksiyonu kullanır.
- Ömür `InviteCodeRules.Lifetime` (48 saat), `expires_at` satırda saklanır. Geçerli: `revoked_at IS NULL` ve `IClock.UtcNow <= expires_at`.
- Yönetici kodu yalnızca üretildiği anda görür; tekrar paylaşmak için yeni kod üretir (eskisi iptal olur).

## Diğer modüller için

`groups` ve `asp_net_users`'a FK'ler `Restrict` olmalı (kullanıcı silinmez, kapatılan grubun satırı kalır). Tablolar `IEntityTypeConfiguration` ile kendi klasöründe tanımlanır, `AppDbContext`'e `DbSet` eklenmez.

## Davet koduyla katılım (spec 0006, Berk)

- `POST /api/groups/join` (`JoinGroupController` → `JoinGroupService` → `JoinGroupRepository`).
- Tek transaction: kodu özetle bul, grup satırını `FOR UPDATE` ile kilitle, kodu kilit altında tekrar oku, kuralları kontrol et, `count(*)` ile doluluğa bak, üyeliği ekle.
- Hata önceliği: `INVITE_INVALID` (404) → `ALREADY_MEMBER` (409) → `INVITE_REVOKED` (410) → `INVITE_EXPIRED` (410) → `ALREADY_IN_GROUP` (409) → `CAPACITY_EXCEEDED` (409). Kodlar `GroupErrors`'ta.
- Kod `InviteCodeHasher.Normalize` sonrası `InviteCodeFormat` (`^[A-Z0-9]{8,32}$`) ile kontrol edilir; katılımda kod tüketilmez.
- Kod iptali ve üretimi (#10) ve grup kapatma (UC10) aynı grup kilidini almalı, aksi halde katılımla yarışır.
- Kullanıcı başına dakikada 10 deneme (`RateLimiting:Join`). Düz kod loglanmaz.

## Yaşam alanı kurma ve ayrılma (UC2 / UC10, Berat)

- `POST /api/groups` ad, `Ev` / `Yurt` türü ve kapasite alır. Grup ve kurucu üyeliği birlikte kaydedilir. Tek üyelik kısıtında çakışma `ALREADY_IN_GROUP` (409) döner.
- `GET /api/groups/{groupId}` yalnızca açık alanın üyesine ad, tür, kapasite, üye sayısı ve kendi rolünü verir.
- `POST /api/groups/{groupId}/leave` grubu katılımla aynı satır kilidi altında günceller. Yönetici devrinde `joined_at, user_id` sırası kullanılır. Son üyede `closed_at` yazılır ve davet kodları kaldırılır. Yanıt `archived` ve `adminTransferred` alanlarını taşır.
- Grup bilgisi ve ayrılma işlemi, açık alan üyeliği bulunmazsa `SPACE_NOT_FOUND` (404) döner.
