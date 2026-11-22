# 0005. Dashboard iskeleti ve modül kaydı

**Date**: 2026-11-22
**Status**: In Progress

## Summary

Grubu olan kullanıcı girişten sonra alt sekmeli bir ana ekrana düşer: Ana Sayfa, Giderler, Borçlar, Görevler ve Diğer. Diğer sekmesinde Faturalar, Alışveriş, Kurallar, Alan Bilgileri ve Alandan Ayrıl modülleri ile profil ve çıkış durur. Her modül kendini kendi klasöründeki tek bir tanım dosyasıyla (başlık, UC kodu, sorumlu, ikon, ekran, dashboard kartı) kaydeder; ekran henüz yoksa ortak "Geliştirme Aşamasında" ekranı görünür. Böylece 6 ekip arkadaşı placeholder'ını gerçek ekranla değiştirmek için yalnızca kendi `features/<modul>/` klasörüne dokunur.

## Context

Scope #7, Slice 2. UC1 (spec 0004) oturum, `me` (grup bilgisi dahil) ve oturuma göre yönlendirmeyi kurdu; `/home` şimdilik geçici bir ekran. Tasarım sistemi (spec 0003) `NavigationBar`, kart ve placeholder bileşenlerini verdi. Engineer gezinme için "alt bar + Diğer" seçti. 8 modül ve sahipleri scope'un Deferred listesinden: UC2 ve UC10 Berat, UC4 Eda, UC5 Mehmet, UC6 Yusuf, UC7 Öykü, UC8 ve UC9 Başak. Kural: modül sahibi yalnızca kendi klasörüne dokunur; router ve `lib/core/` ortak, onaya bağlı.

## Requirements

**User stories**:
- Grubu olan kullanıcı olarak açılışta doğrudan ana ekranı ve grubumu görmek istiyorum.
- Kullanıcı olarak bütün modüllere alt bardan ya da Diğer menüsünden ulaşmak istiyorum.
- Modül sahibi olarak placeholder'ımı yalnızca kendi klasörümü değiştirerek gerçek ekranla değiştirmek istiyorum.

**Acceptance criteria**:
- **AC-1**: Grubu olan kullanıcı açılışta ve girişten sonra `/home` (Ana Sayfa) sekmesine düşer; Ana Sayfa grup adını, türünü ("Ev" / "Yurt") ve kullanıcının rolünü ("Yönetici" / "Üye") ile "Merhaba <ad>" selamını gösterir. Aşağı çekince `me` yeniden okunur.
- **AC-2**: Alt bar (M3 `NavigationBar`) sırasıyla Ana Sayfa (`/home`), Giderler (`/expenses`, UC4), Borçlar (`/debts`, UC5), Görevler (`/chores`, UC7), Diğer (`/more`) sekmelerini gösterir; sekmeler arası geçişte her sekme kendi gezinme durumunu korur (`StatefulShellRoute.indexedStack`).
- **AC-3**: Diğer sekmesi kullanıcının adını ve e-postasını, sırasıyla Faturalar (`/more/bills`, UC6), Alışveriş (`/more/shopping`, UC8), Kurallar (`/more/rules`, UC9), Alan Bilgileri (`/more/space`, UC2), Alandan Ayrıl (`/more/leave`, UC10) satırlarını (alt satırda `"<UC> · <sorumlu>"`), "Sistem durumu" ve "Çıkış yap" satırlarını gösterir. Çıkış oturumu kapatır ve girişe götürür.
- **AC-4**: Ana Sayfa'da kayıtlı 8 modülün her biri için bir kart vardır (sıra: alt bar modülleri, sonra Diğer modülleri); modül kendi kartını vermemişse varsayılan kart başlık, ikon ve "Geliştirme aşamasında · <UC> · <sorumlu>" gösterir. Karta dokununca modülün rotasına gidilir.
- **AC-5**: Ekranını vermemiş bir modülün rotası `UnderDevelopmentScreen`'i (başlık, UC kodu, sorumlu, ikon) gösterir; 8 placeholder ekranının hepsine menüden (alt bar ya da Diğer) gidilebilir.
- **AC-6**: Her modül `lib/features/<modul>/<modul>_module.dart` içinde tek bir `AppModule` sabitiyle tanımlanır (`id`, `title`, `ucCode`, `owner`, `icon`, `placement`: `tab` ya da `more`, isteğe bağlı `screenBuilder`, `dashboardCardBuilder`, `routes`). Rota `tab` için `/<id>`, `more` için `/more/<id>`; `routes` bu rotanın altına eklenir. Modül listesi `lib/core/modules/module_registry.dart` içinde bir kez yazılır; modülün ekranını, kartını ya da alt rotalarını değiştirmek bu dosyaya dokunmayı gerektirmez.
- **AC-7**: Registry tutarlıdır: `id`'ler benzersiz ve `^[a-z][a-z0-9-]*$`; tam 3 `tab` modülü (alt bar 5 sekmede kalır); `appModulesProvider` testte başka bir listeyle değiştirilebilir.
- **AC-8**: Grubu olmayan kullanıcı modül rotalarına giremez (onboarding'e yönlenir); oturumu olmayan girişe yönlenir (spec 0004 kuralları, değişmeden).
- **AC-9**: UC2 "Alan Kur" (`/onboarding/create-group`) placeholder'ı da Berat'ın `features/space/` klasöründen gelir; UC1'in geçici ana ekranı kaldırılır.

## Options considered

### Option 1: Modül başına `AppModule` tanımı + tek registry listesi (seçilen)

Her modül kendi klasöründe bir sabit tanım verir; ortak registry yalnızca listeyi tutar, router ve dashboard listeden üretilir.
**Pros**: modül sahibi yalnızca kendi dosyasını değiştirir; tutarlı menü ve kartlar; testte liste değiştirilebilir. **Cons**: yeni bir modül eklemek (9. modül) registry'ye bir satır ister.

### Option 2: Router'a ve dashboard'a modül başına elle satır

**Pros**: soyutlama yok. **Cons**: her placeholder değişikliği ortak router dosyasına dokunur, 6 kişi aynı dosyada çakışır.

### Option 3: Kod üretimi / otomatik keşif (build_runner)

**Pros**: registry'ye satır bile gerekmez. **Cons**: spec 0001 code generation'ı dışladı; ek paket ve derleme adımı.

## Decision

**Chosen option**: Option 1: modül başına `AppModule` tanımı, `lib/core/modules/` altında tek registry ve ondan üretilen `StatefulShellRoute` ve dashboard kartları.

## Rationale

Ekip kuralı (sahip yalnızca kendi klasörüne dokunur) belirleyici. Registry'ye 8 satırı şimdi bir kez yazmak, sonraki bütün değişiklikleri modül klasörlerine taşır. Kod üretimi bunu sıfıra indirir ama stack kararıyla çelişir.

## Feature design

### Dosyalar

| Dosya | Sahip | İçerik |
|---|---|---|
| `lib/core/modules/app_module.dart` | ortak | `AppModule`, `ModulePlacement`, `path` türetimi |
| `lib/core/modules/module_registry.dart` | ortak | `appModules` listesi, `appModulesProvider`, tutarlılık kontrolü (`assert`) |
| `lib/core/router/app_router.dart` | ortak | Shell rotası ve modül rotaları registry'den |
| `lib/features/dashboard/` | Berk | `DashboardShell` (alt bar), `HomeScreen` (Ana Sayfa), `MoreScreen` (Diğer), `ModuleCard` |
| `lib/features/expenses/expenses_module.dart` | Eda | UC4 Giderler, `tab`, `Icons.receipt_long_outlined` |
| `lib/features/debts/debts_module.dart` | Mehmet | UC5 Borçlar, `tab`, `Icons.account_balance_wallet_outlined` |
| `lib/features/chores/chores_module.dart` | Öykü | UC7 Görevler, `tab`, `Icons.cleaning_services_outlined` |
| `lib/features/bills/bills_module.dart` | Yusuf | UC6 Faturalar, `more`, `Icons.request_quote_outlined` |
| `lib/features/shopping/shopping_module.dart` | Başak | UC8 Alışveriş, `more`, `Icons.shopping_cart_outlined` |
| `lib/features/rules/rules_module.dart` | Başak | UC9 Kurallar, `more`, `Icons.gavel_outlined` |
| `lib/features/space/space_module.dart` | Berat | UC2 Alan Bilgileri (`id: space`, `more`, `Icons.home_outlined`) ve UC10 Alandan Ayrıl (`id: leave`, `more`, `Icons.logout_outlined`); `CreateSpaceScreen` placeholder'ı |

Alt bar etiketleri: "Ana Sayfa" (`Icons.dashboard_outlined`), modül başlıkları, "Diğer" (`Icons.more_horiz`). Seçili ikonlar M3 varsayılanı.

### Value sourcing

| Eylem | Değer | Kaynak |
|---|---|---|
| Ana Sayfa | grup adı, türü, rol | `SessionAuthenticated.user.group` (`GET /api/auth/me`) |
| Ana Sayfa | tür etiketi | `type` `Ev` → "Ev", `Yurt` → "Yurt" |
| Ana Sayfa | rol etiketi | `role` `Admin` → "Yönetici", `Member` → "Üye" |
| Ana Sayfa | selam | `user.displayName` |
| Yenileme | güncel grup | `SessionController.refreshUser()` |
| Menü, kart, placeholder | başlık, UC, sorumlu, ikon | Modülün `AppModule` tanımı |
| Rota | yol | `placement` + `id` (AC-6) |
| Diğer | ad, e-posta | `user.displayName`, `user.email` |

### Key invariants

- Alt bar her zaman 5 sekme (3 `tab` modülü).
- Modül rotaları yalnızca grubu olan kullanıcıya açık (`SessionRedirect`).
- Modül sahibi ekranını değiştirirken ortak dosyaya dokunmaz.

### Security model

Yalnızca istemci tarafı yönlendirme; veri erişim kontrolü backend'de (grup izolasyonu, scope #9). Bu dilim yeni uç eklemez.

### Critical test scenarios

Widget testleri `mobile/test/features/dashboard/`, registry testi `mobile/test/core/`. Ad `DASH-<n>` ile biter.
- Demo hesabı açılışta Ana Sayfa'yı, grup adını, "Ev" ve "Yönetici"yi görür, verifies **AC-1**
- Alt bar 5 sekme; Giderler sekmesi UC4 placeholder'ını gösterir; sekme değiştirip dönünce durum korunur, verifies **AC-2**, **AC-5**
- Diğer'de 5 modül satırı, ad ve e-posta; Faturalar'a dokununca UC6 placeholder'ı; çıkış girişe götürür, verifies **AC-3**, **AC-5**
- Ana Sayfa'da 8 kart; karta dokununca modül ekranı, verifies **AC-4**
- Registry: 8 modül, benzersiz `id`, 3 `tab`, yollar doğru, verifies **AC-6**, **AC-7**
- Testte kendi `screenBuilder`'ı ve kartı olan sahte modül listesiyle router o ekranı ve kartı gösterir, verifies **AC-6**, **AC-7**
- Grupsuz kullanıcı `/expenses`'e gidemez, verifies **AC-8**

## Build plan

Tracer Bullet'ın ikinci dilimi: UC1'in ince yolunun ucuna (`/home`) gerçek kabuk takılır, modüller onun üzerine kaydolur.

1. [x] `AppModule`, `ModulePlacement`, registry ve `appModulesProvider`, satisfies **AC-6**, **AC-7**
2. [x] 8 modül tanım dosyası kendi klasörlerinde; `CreateSpaceScreen`, satisfies **AC-5**, **AC-6**, **AC-9**
3. [x] `DashboardShell`, `HomeScreen`, `ModuleCard`, `MoreScreen`, satisfies **AC-1**, **AC-2**, **AC-3**, **AC-4**
4. [x] Router: `StatefulShellRoute.indexedStack` registry'den, `/more/<id>` rotaları, geçici ana ekranı kaldır, satisfies **AC-2**, **AC-5**, **AC-8**, **AC-9**
5. [x] Testler ve `docs/traceability.md`, `mobile/lib/features/README.md` (modül ekleme rehberi), satisfies **AC-1** ile **AC-9**

## Consequences

**Positive**:
- Modül sahipleri birbirinin ve ortak dosyaların üstüne yazmadan çalışır.
- Menü, kart ve placeholder tek tanımdan tutarlı üretilir.

**Negative / tradeoffs**:
- 9. bir modül ya da bir modülü alt bara taşımak registry'de (ortak dosya) bir satır değişikliği ister.
- Alt bar 3 modülle sınırlı; Diğer'deki modüller bir dokunuş daha uzakta.

**Neutral**:
- Backend değişikliği yok.

## Follow-up

- [ ] Davet kodu yönetimi (scope #10) geldiğinde yöneticiye özel "Davet Kodu" satırı Diğer'e eklenir (`AppModule`'e `adminOnly` alanı gerekebilir).
