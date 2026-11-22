# Scope: Roomly

Roomly, ev arkadaşları ve yurt oda arkadaşlarının ortak yaşamını (giderler, borçlar, faturalar, nöbetler, alışveriş, kurallar) tek yerden yönetmesi için bir Android uygulaması. Bu scope, monorepo iskeletini, Berk'in modüllerini (UC1 kayıt ve giriş, UC3 davet koduyla katılım) ve ekibin kendi use case'lerini takacağı dashboard iskeletini kapsar.

**Build approach:** Tracer Bullet (önce DB → API → Flutter arasında çalışan tek ince yol, sonra her dilim o yolu kalınlaştırır).
**Workflow:** GA (/develop sonrası /check verify, /test, ayrı modelle /check review, sonra /document). Auth, kişisel veri (KVKK) ve 7 kişilik ekip kodu olduğu için. Bir özellik kendi etiketiyle (ör. `· Beta`) daha hafif ya da ağır olabilir.

_Bunlar düzenli ilerlemek için öneriler, zorunluluk değil. Bir özelliği nasıl yapacağını zaten biliyorsan `/architect` adımını atlayıp `/develop` ile devam edebilirsin. Bir özelliğin `done` olduğuna sen karar verirsin._

**Sabit kararlar:** 22.11.2026 tarihli konuşmada verilen 16 karar (kapasite aralıkları, tek yönetici, tek aktif kod, Base32 kod formatı, hata kodları, limitler, portlar 5080 / 5433, yalnızca Türkçe arayüz, ek paket olmadan deep link vb.) ilgili spec'lere `/architect` tarafından işlenir. Repodaki `README.md`, `CONTRIBUTING.md` ve `docs/` bu scope'tan daha günceldir.

## At a glance

| # | Feature | Phase | Status |
|---|---------|-------|--------|
| 1 | Stack & architecture | Foundation | done |
| 2 | Coding standards & tooling | Foundation | done |
| 3 | Data model | Foundation | done |
| 4 | Design system & UI foundation | Foundation | in-progress |
| 5 | Local dev environment & demo data | Foundation | in-progress |
| 6 | Kayıt ve giriş (UC1) | Slice 1 | in-progress |
| 7 | Dashboard iskeleti ve modül kaydı | Slice 2 | in-progress |
| 8 | Davet koduyla katılım (UC3) | Slice 3 | in-progress |
| 9 | Grup izolasyonu (SEC-03) | Slice 4 | planned |
| 10 | Davet kodu yönetimi (UC3) | Slice 4 | planned |
| 11 | Deep link ile katılım (UC3) | Slice 5 | planned |
| 12 | Hesap silme ve anonimleştirme (UC1) | Slice 6 | planned |

## Foundations

### 1. Stack & architecture · done
Kararlaştırılmış stack'i (Flutter + Riverpod + dio + go_router, ASP.NET Core 8 + EF Core Npgsql + Identity, PostgreSQL) ve feature first monorepo yerleşimini kayda geçir, çalışan boş iskeleti kur.
**Done when:** stack bir spec'te kayıtlı; `backend/` derlenip 5080'de açılıyor, `mobile/` emülatörde boş ekranla açılıyor; mock / gerçek API ve base URL seçimi yapılandırmayla değişiyor.
- [x] Decide the stack (spec): `/architect stack & architecture`
- [x] Scaffold from the decision: `/develop stack & architecture`
Spec [0001](../specs/0001-stack-architecture/index.md) · code in `backend/`, `mobile/`

### 2. Coding standards & tooling · done
Gerçek iskeletten kuralları çıkar (isimlendirme, test adının gereksinim ID'siyle bitmesi, commit kuralları, ortak dosya onayı), sonra lint ve format araçlarını kur.
**Done when:** kök `AGENTS.md` gerçek stack'i anlatıyor; `flutter analyze` ve `dotnet format` temiz geçiyor.
- [x] Capture conventions + tooling choices: `/audit`

### 3. Data model · done
Kullanıcı (Identity, yalnızca görünen ad, e-posta, şifre hash'i), refresh token, en az alanlı grup, üyelik ve davet kodu. Berat'ın UC2 / UC10 alanlarını kırmadan ekleyebileceği şekilde.
**Done when:** ilk migration temiz bir PostgreSQL'e uygulanıyor; `(group_id, user_id)` unique; davet kodu yalnızca SHA-256 hash olarak duruyor; grup başına tek aktif kod kuralı modellenmiş.
- [x] Design it (spec): `/architect data model`
- [x] Build it: `/develop data model`
  - [x] Paketler, snake_case ve `IdentityUserContext` geçişi (AC-1, AC-2, AC-12)
  - [x] Auth ve Groups entity'leri, kurallar, hasher ve adlı kısıtlar (AC-3 ile AC-11, AC-13, AC-14)
  - [x] `InitialDataModel` migration'ı ve SQL incelemesi (AC-1, AC-2, AC-13, AC-14)
  - [x] Paylaşılan Postgres fixture'ı, kısıt ve yazma sırası testleri, traceability (AC-1 ile AC-14)
- [x] Verify it: `/check verify data model`
- [x] Test it: `/test data model`
- [ ] Review it (fresh model): `/check review data model`
- [ ] Document it: `/document data model`
Spec [0002](../specs/0002-data-model/index.md) · code in `backend/src/Roomly.Api/Features/Auth/`, `backend/src/Roomly.Api/Features/Groups/`, `backend/src/Roomly.Api/Infrastructure/Persistence/`

### 4. Design system & UI foundation · in-progress · Beta
Türkçe arayüz için tema, renk, tipografi, boşluk ve temel bileşenler (form alanı, buton, hata / boş / yükleniyor durumları, "Geliştirme Aşamasında" ekranı).
**Done when:** `design.md` var; temel bileşenler auth ve katılım ekranlarında kullanılıyor; placeholder ekranı UC numarası ve sorumlu adını gösteriyor.
- [x] Design it (spec): `/architect design system & UI foundation`
- [x] Build it: `/develop design system & UI foundation`
  - [x] Tema ve token'lar (AC-1, AC-2, AC-3)
  - [x] Form, buton, durum bileşenleri ve placeholder ekranı (AC-4 ile AC-8)
  - [x] `design.md`, iskeletin geçişi ve testler (AC-9, AC-10)
- [ ] Verify it: `/check verify design system & UI foundation`
- [ ] Test it: `/test design system & UI foundation`
Spec [0003](../specs/0003-design-system.md) · code in `mobile/lib/core/theme/`, `mobile/lib/core/widgets/`, `mobile/design.md`

### 5. Local dev environment & demo data · in-progress
Docker ile PostgreSQL (5433), Development ortamında otomatik migration ve seed (demo kullanıcılar, "Demo Ev" grubu, `DEMO-ROOM-2026` kodu), VS Code çalıştırma profilleri, emülatör (`10.0.2.2`) ve cihaz (`adb reverse`) bağlantısı. Kararları stack spec'inde ve data model spec'inde.
**Done when:** `docker compose up -d` ve `dotnet run` sonrası demo hesapla giriş yapılabiliyor; seed tekrar çalışınca kopya veri basmıyor; debug derlemede HTTP'ye izin var, release derlemede yok.
- [ ] Build it: `/develop local dev environment & demo data`
  - [x] Development'ta otomatik migration ve idempotent seed: demo yönetici `demo@roomly.app` / `Demo1234`, "Demo Ev" grubu, `DEMO-ROOM-2026` kodu
  - [ ] VS Code çalıştırma profilleri, debug / release ağ ayarlarının doğrulanması
code in `backend/src/Roomly.Api/Infrastructure/Persistence/DevDataSeeder.cs`, `DevelopmentDatabaseExtensions.cs`

## Slice 1: Kayıt ve giriş (tracer bullet)

### 6. Kayıt ve giriş (UC1) · in-progress
İlk ince yol: kayıt, giriş, token'ların güvenli saklanması, otomatik yenileme, çıkış ve oturuma göre yönlendirme. Girişten sonra grubu olmayan kullanıcı "Alan Kur / Koda Katıl" ekranını görür.
**Done when:** kullanıcı kayıt olup giriş yapabiliyor; 15 dk JWT ve 7 günlük rotasyonlu refresh çalışıyor; 5 hatalı denemede 15 dk kilit ve dakikada 10 istek limiti uygulanıyor; token'lar `flutter_secure_storage`'da; loglarda e-posta ve token maskeli (SEC-01, SEC-04, SEC-05, SEC-07).
- [x] Design it (spec): `/architect kayıt ve giriş`
- [x] Build it: `/develop kayıt ve giriş`
  - [x] Backend: auth altyapısı, kayıt, giriş, kilit, `me` (AC-1 ile AC-5, AC-7, AC-8, AC-12)
  - [x] Backend: refresh rotasyonu, logout, rate limit, log maskeleme ve testler (AC-6, AC-9 ile AC-11, AC-13)
  - [x] Mobil: token deposu, interceptor, oturum ve yönlendirme (AC-14 ile AC-16, AC-18)
  - [x] Mobil: giriş, kayıt, onboarding ekranları ve testler (AC-17)
- [x] Verify it: `/check verify kayıt ve giriş`
- [ ] Test it: `/test kayıt ve giriş`
- [ ] Review it (fresh model): `/check review kayıt ve giriş`
- [ ] Document it: `/document kayıt ve giriş`
Spec [0004](../specs/0004-auth-register-login.md) · code in `backend/src/Roomly.Api/Features/Auth/`, `mobile/lib/features/auth/`, `mobile/lib/core/{auth,network,storage,errors,router}/`

## Slice 2: Ekibin üzerine inşa edeceği iskelet

### 7. Dashboard iskeleti ve modül kaydı · in-progress
Giriş sonrası alt sekmeli ana ekran ve her modülün kendi rotasını ve dashboard kartını kendi klasöründen kaydettiği yapı. UC2, UC4–UC10 için placeholder ekranlar.
**Done when:** aktif grubu olan kullanıcı doğrudan dashboard'a düşüyor; 8 placeholder ekranına menüden gidilebiliyor; bir ekip arkadaşı yalnızca kendi `features/<modul>/` klasörünü değiştirerek placeholder'ını gerçek ekranla değiştirebiliyor.
- [x] Design it (spec): `/architect dashboard iskeleti ve modül kaydı`
- [x] Build it: `/develop dashboard iskeleti ve modül kaydı`
  - [x] Modül tanımı, registry ve 8 modül dosyası (AC-5, AC-6, AC-7, AC-9)
  - [x] Kabuk, Ana Sayfa, Diğer ve router (AC-1 ile AC-5, AC-8)
  - [x] Testler ve modül ekleme rehberi (AC-1 ile AC-9)
- [ ] Verify it: `/check verify dashboard iskeleti ve modül kaydı`
- [ ] Test it: `/test dashboard iskeleti ve modül kaydı`
- [ ] Review it (fresh model): `/check review dashboard iskeleti ve modül kaydı`
- [ ] Document it: `/document dashboard iskeleti ve modül kaydı`
Spec [0005](../specs/0005-dashboard-module-registry.md) · code in `mobile/lib/features/dashboard/`, `mobile/lib/core/modules/`, `mobile/lib/features/<modul>/<modul>_module.dart`

## Slice 3: Davet koduyla katılım

### 8. Davet koduyla katılım (UC3) · in-progress
Kullanıcı tireli Base32 kodu girip gruba katılır. Kapasite, süre ve iptal kuralları backend'de kilitli bir transaction içinde kontrol edilir.
**Done when:** geçerli kodla katılım başarılı; dolu grupta `CAPACITY_EXCEEDED`; tam 48. saatte kabul, 48 saat + 1 sn'de `INVITE_EXPIRED`; iptal edilmişte `INVITE_REVOKED`; yanlışta `INVITE_INVALID`; zaten üyede 409 `ALREADY_MEMBER` ve "Zaten bu grubun üyesisiniz" uyarısı; paralel isteklerde kapasite aşılmıyor (BND-01, BND-03, TRG-01).
- [x] Design it (spec): `/architect davet koduyla katılım`
- [x] Build it: `/develop davet koduyla katılım`
  - [x] Backend: hata kodları, kilitli transaction, kurallar ve uç (AC-1 ile AC-10)
  - [x] Backend testleri: sınır zamanları, eşzamanlılık, HTTP (AC-1 ile AC-10)
  - [x] Mobil: katılım ekranı, mock, yönlendirme ve testler (AC-11 ile AC-13)
- [x] Verify it: `/check verify davet koduyla katılım`
- [ ] Test it: `/test davet koduyla katılım`
- [ ] Review it (fresh model): `/check review davet koduyla katılım`
- [ ] Document it: `/document davet koduyla katılım`
Spec [0006](../specs/0006-join-with-invite-code.md) · code in `backend/src/Roomly.Api/Features/Groups/` (Join*), `mobile/lib/features/invites/`

## Slice 4: Grup güvenliği ve kod yönetimi

### 9. Grup izolasyonu (SEC-03)
`/api/groups/{groupId}/...` altındaki her istek için ortak üyelik kontrolü; üye olmayana 404. Bütün ekip bunu kullanır. Data model spec'ine ve katılım spec'ine dayanır.
**Done when:** üye olmayan kullanıcı başka bir grubun herhangi bir uç noktasına eriştiğinde 404 alıyor; ekibin yeni controller'larına tek satırla eklenebiliyor.
- [ ] Build it: `/develop grup izolasyonu`

### 10. Davet kodu yönetimi (UC3)
Yönetici aktif kodu görür, paylaşır, iptal eder ya da yenisini üretir; yeni kod eskisini otomatik iptal eder. Katılım spec'ine dayanır.
**Done when:** yalnızca yönetici kod üretip iptal edebiliyor; grupta aynı anda tek aktif kod var; kod 48 saat geçerli ve kapasite dolana kadar tekrar kullanılabiliyor (SEC-02).
- [ ] Build it: `/develop davet kodu yönetimi`

## Slice 5: Deep link

### 11. Deep link ile katılım (UC3)
`roomly://join?code=...` bağlantısı uygulamayı katılım ekranında, kod dolu olarak açar. Oturum yoksa kod bekletilir, girişten sonra akış kaldığı yerden devam eder. Ek paket yok, go_router redirect ile.
**Done when:** `adb shell am start -d "roomly://join?code=..."` ile uygulama katılım ekranını açıyor; çıkış yapmış kullanıcı giriş sonrası aynı koda yönleniyor.
- [ ] Build it: `/develop deep link ile katılım`

## Slice 6: KVKK

### 12. Hesap silme ve anonimleştirme (UC1)
`DELETE /api/auth/me` ile kullanıcı hesabını siler; ad ve e-posta `DeletedUser_<Guid>` olarak anonimleştirilir, finansal geçmiş korunur. Auth spec'ine dayanır.
**Done when:** silinen hesapla giriş yapılamıyor; kişisel veri tabloda kalmıyor; refresh token'lar iptal; yönetici silindiyse yöneticilik en eski üyeye geçiyor (SEC-07, TRG-02).
- [ ] Build it: `/develop hesap silme ve anonimleştirme`

## Deferred
Bu geçişin dışında, plan dürüst kalsın diye burada.
- **Yaşam alanı kurma (UC2)**: Berat · placeholder'dan gerçek ekrana
- **Gider ekleme ve bölüşme (UC4)**: Eda
- **Borç görüntüleme ve hesaplaşma (UC5)**: Mehmet
- **Kira, aidat ve fatura yönetimi (UC6)**: Yusuf
- **Görev nöbeti yönetimi (UC7)**: Öykü
- **Ortak alışveriş listesi (UC8)**: Başak
- **Kural önerme ve oylama (UC9)**: Başak
- **Alandan ayrılma ve alanı kapatma (UC10)**: Berat
- **E-posta doğrulama, şifre sıfırlama**: kapsam dışı
- **https App Links, iOS, birden fazla aktif grup**: kapsam dışı

## Legend

**The decision box.** Her özellikte tam bir tane, etiketi `(spec)` ile biten kutu. Diğer kutular uygulama kutusudur.

- **Next step** = ilk işaretlenmemiş kutu.
- **needs a decision** = önce `/architect`; yoksa doğrudan `/develop` (standartlar için `/audit`).
- **Atomik görevler spec'in `## Build plan` bölümünde**, burada yalnızca kilometre taşları.
- **Status** `planned` → `in-progress` → `done`, ayrıca `existing` ve `dropped`.
- **Workflow tier tag** (ör. `· Beta`) o özelliğin titizliğini proje varsayılanından ayırır.
- **Workflow** GA = `/check verify`, `/test`, `/check review`, `/document`.
- **Pointer line** (`spec <n> · code in <path>`): spec linkini `/architect`, kod yolunu `/develop` ekler.
