# Traceability

Her gereksinim kimliğini, onu tanımlayan spec'e ve onu doğrulayan testlere bağlar. Test adı kimlikle biter (`_DM04`, `_BND02`); bir testi bulmak için kimliği kodda arayın. Projenin gereksinim kimliği varsa (ör. `BND-02`) o kullanılır, yoksa spec'in AC'si spec önekiyle (`DM` = spec 0002).

## Spec 0002 · Veri modeli

Entegrasyon testleri `backend/tests/Roomly.Api.IntegrationTests/DataModel/`, unit testler `backend/tests/Roomly.Api.UnitTests/Features/Groups/` altında.

| ID | Gereksinim | Spec | Testler |
|---|---|---|---|
| DM01 | `InitialDataModel` boş PostgreSQL 16'ya uygulanır, beklenen tablolar var, rol tabloları yok | [0002 AC-1](specs/0002-data-model/index.md) | `SchemaTests.Migrate_OnEmptyDatabase_CreatesExpectedSchema_DM01` |
| DM02 | Tüm tablo, sütun, index ve kısıt adları snake_case | 0002 AC-2 | `SchemaTests.Migrate_AllTableColumnIndexAndConstraintNames_AreSnakeCase_DM02` |
| DM03 | Aynı `(group_id, user_id)` ikinci kez yazılamaz | 0002 AC-3 | `MembershipConstraintTests.InsertMembership_WhenSamePairExists_IsRejected_DM03` |
| DM04 | Kullanıcı aynı anda iki grupta olamaz | 0002 AC-4 | `MembershipConstraintTests.InsertMembership_WhenUserInAnotherGroup_IsRejected_DM04` |
| DM05 | Grupta tek yönetici; rol yalnızca `Admin` ya da `Member` | 0002 AC-5 | `MembershipConstraintTests.*_DM05` (3 test) |
| BND-02 | Kapasite türe göre: `Ev` 2 ile 20, `Yurt` 2 ile 6, yönetici dahil | 0002 AC-6 | `GroupCapacityConstraintTests.*_BND02` (8 durum), `GroupCapacityRulesTests.*_BND02` (unit) |
| DM07 | Davet kodu yalnızca 32 baytlık SHA-256 özeti; normalizasyon | 0002 AC-7 | `InviteCodeHasherTests.*_DM07` (unit), `SchemaTests.InviteCodes_HaveNoPlainTextCodeColumn_DM07`, `InviteCodeConstraintTests.InsertInviteCode_WithHashNot32Bytes_IsRejected_DM07` |
| DM08 | Grupta tek iptal edilmemiş kod; `code_hash` benzersiz; önce iptal sonra ekleme sırası çalışır | 0002 AC-8 | `InviteCodeConstraintTests.*_DM08` (4 test) |
| DM09 | Refresh token özeti 32 bayt, `token_hash` ve `replaced_by_id` benzersiz, rotasyon zinciri | 0002 AC-9 | `RefreshTokenConstraintTests.*_DM09` (4 test) |
| DM10 | `display_name` zorunlu, en fazla 50; e-posta benzersiz; `deleted_at` boş olabilir | 0002 AC-10 | `UserConstraintTests.*_DM10` (5 test) |
| DM11 | Grup silinince üyelik ve kodlar silinir; üyeliği ya da kodu olan kullanıcı silinemez; `closed_at` boş olabilir | 0002 AC-11 | `DeleteBehaviorTests.*_DM11` (4 test) |
| DM12 | Entity'ler `DbSet` olmadan, `IEntityTypeConfiguration` ile kaydolur, `Set<T>()` ile erişilir | 0002 AC-12 | `SchemaTests.AppDbContext_DeclaresNoDbSets_ModulesRegisterThroughConfigurations_DM12`, `DataModelRoundTripTests.*_DM12` |
| DM13 | Zaman sütunları `timestamptz`, şemada varsayılan yok | 0002 AC-13 | `SchemaTests.Migrate_TimeColumns_AreTimestamptzWithoutDefaults_DM13`, `DataModelRoundTripTests.WriteTimestamps_RoundTripAsUtc_DM13` |
| DM14 | Her index ve kısıt, kodda sabit duran adla oluşur | 0002 AC-14 | `SchemaTests.Migrate_EveryNamedConstraintConstant_ExistsInDatabase_DM14`, tüm kısıt testleri `ConstraintName` kontrol eder |

## Spec 0003 · Tasarım sistemi

Widget testleri `mobile/test/core/design_system_test.dart` altında.

| ID | Gereksinim | Spec | Testler |
|---|---|---|---|
| DS-01 | Açık ve koyu tema seed `#00796B`'den, Material 3 | [0003 AC-1](specs/0003-design-system.md) | `açık ve koyu tema seed renginden türetilir DS-01` |
| DS-02 | `AppSpacing` ve `AppRadius` değerleri | 0003 AC-2 | `boşluk ve köşe tokenları sabit değerlerdedir DS-02` |
| DS-03 | Düğme 48 dp, dolgulu alan, gölgesiz kart, floating snackbar | 0003 AC-3 | `düğme, alan ve kart temaları token kullanır DS-03` |
| DS-04 | Şifre alanı göster / gizle düğmesi ve etiketi | 0003 AC-4 | `şifre alanı göster düğmesiyle görünür olur DS-04` |
| DS-05 | Yükleniyorken düğme gösterge gösterir, dokunuş yok | 0003 AC-5 | `yükleniyorken düğme ... DS-05` |
| DS-06 | Hata ve boş durum görünümleri | 0003 AC-6 | `hata görünümü ... DS-06`, `boş görünüm ... DS-06` |
| DS-07 | Hata snackbarı `colorScheme.error` | 0003 AC-7 | `hata snackbarı hata rengiyle gösterilir DS-07` |
| DS-08 | Placeholder UC kodu ve sorumluyu gösterir | 0003 AC-8 | `placeholder ekranı UC kodu ve sorumluyu gösterir DS-08` |
