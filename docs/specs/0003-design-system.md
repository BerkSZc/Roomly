# 0003. Material 3 tabanlı tasarım sistemi ve temel bileşenler

**Date**: 2026-11-22
**Status**: In Progress

## Summary

Roomly'nin bütün ekranları tek bir Material 3 temasından (renk, yazı, boşluk kuralları) ve küçük bir ortak bileşen setinden beslenir. Ana renk teal (`#00796B`); Material 3 açık ve koyu paletin tamamını bu tek renkten türetir. Bileşenler `mobile/lib/core/theme/` ve `mobile/lib/core/widgets/` içinde durur, kurallar `mobile/design.md` dosyasında yazılıdır. Böylece 7 kişi kendi modülünü yazarken aynı görünümü renk ya da boşluk uydurmadan elde eder, henüz yapılmamış modüller de ortak bir "Geliştirme Aşamasında" ekranıyla görünür.

## Context

Scope #4. Mobil iskelet `ThemeData(colorSchemeSeed: Colors.teal)` ile açılıyor ve `mobile/AGENTS.md` "tema gelene kadar ekran başına özel stil yazma" diyor. UC1, dashboard ve UC3 ekranları bu dilimden hemen sonra yazılacak; arkasından 6 ekip arkadaşı kendi modüllerini ekleyecek. Ortak bir tema ve bileşen seti yoksa her ekran kendi rengini, boşluğunu ve hata görünümünü icat eder.

Kısıtlar: yalnızca Android, yalnızca Türkçe (l10n yok), Riverpod, Material import'u `package:material_ui/material_ui.dart`. Ek paket eklemek `pubspec.yaml` (ortak dosya) onayı ister, bu yüzden tasarım sistemi ek paketsiz kurulmalı. Tasarım kaynağı olarak Figma ya da ekran görüntüsü yok; engineer "Material 3 tabanlı yön öner" ve ana renk olarak teal `#00796B` seçti.

## Requirements

**User stories**:
- Bir modül sahibi olarak, hazır tema ve bileşenler istiyorum, böylece ekranımı renk ve boşluk uydurmadan ekibin geri kalanıyla aynı görünümde yazabilirim.
- Bir kullanıcı olarak, form hatalarını, yükleniyor ve boş durumlarını her ekranda aynı biçimde görmek istiyorum.
- Ekip olarak, henüz yapılmamış modüllerin hangi UC'ye ve kime ait olduğunu uygulamada görmek istiyoruz.

**Acceptance criteria**:
- **AC-1**: Uygulama `AppTheme.light()` ve `AppTheme.dark()` ile açılır; ikisi de `ColorScheme.fromSeed(seedColor: Color(0xFF00796B))` ile türetilir, `useMaterial3: true`, `themeMode: ThemeMode.system`.
- **AC-2**: Boşluk ve köşe ölçüleri `AppSpacing` (`xs 4`, `sm 8`, `md 12`, `lg 16`, `xl 24`, `xxl 32`) ve `AppRadius` (`sm 8`, `md 12`, `lg 16`) sabitlerinden okunur; `AppSpacing.screen` (16) ekran kenar boşluğudur.
- **AC-3**: Tema bileşen ayarları: `FilledButton` ve `OutlinedButton` en az 48 dp yükseklik ve `AppRadius.md` köşe; `InputDecorationTheme` dolgulu (`filled`), kenarlıklı (`OutlineInputBorder`, `AppRadius.md`); `CardTheme` `AppRadius.lg` köşe, gölge 0, `surfaceContainerLow` dolgu; `SnackBarTheme` `floating`.
- **AC-4**: `AppTextField` etiket, ipucu, hata metni, klavye tipi, `textInputAction`, `autofillHints` ve validator alır. `obscure: true` verilince sağda göster / gizle düğmesi çıkar; düğmenin erişilebilirlik etiketi duruma göre "Şifreyi göster" ya da "Şifreyi gizle" olur ve basınca metin görünür olur.
- **AC-5**: `PrimaryButton` (dolgulu) ve `SecondaryButton` (kenarlıklı) tam genişliktedir. `isLoading: true` iken etiket yerine 20 dp `CircularProgressIndicator` gösterir ve `onPressed` çağrılmaz; `onPressed: null` iken devre dışıdır.
- **AC-6**: Durum görünümleri: `LoadingView` (ortalanmış gösterge, isteğe bağlı mesaj), `ErrorView` (hata ikonu, mesaj, `onRetry` verilirse "Tekrar dene" düğmesi), `EmptyView` (ikon, başlık, isteğe bağlı açıklama ve eylem düğmesi).
- **AC-7**: `showAppSnackBar(context, message, {isError})` mevcut snackbar'ı kapatıp yenisini gösterir; `isError: true` iken arka plan `colorScheme.error`, metin `onError`.
- **AC-8**: `UnderDevelopmentScreen(title, ucCode, owner, icon)` başlığı AppBar'da, ortada ikonu, "Geliştirme Aşamasında" başlığını, `"<ucCode> · Sorumlu: <owner>"` satırını ve "Bu modül yakında burada olacak." açıklamasını gösterir.
- **AC-9**: `mobile/design.md` renk, yazı, boşluk, köşe, bileşen listesi ve kullanım kurallarını (ekranlarda sabit renk ya da sayı yazılmaz, metinler Türkçe ve sabit, dokunma alanı en az 48 dp) anlatır.
- **AC-10**: `lib/core/widgets/` ve `lib/core/theme/` dışındaki ekran kodunda `Color(0x...)`, `Colors.<renk>` ya da `TextStyle(fontSize: ...)` yoktur (iskeletteki `Colors.teal` kaldırılır); `SystemStatusScreen` ortak bileşenlere geçirilir.

## Options considered

### Option 1: Material 3 teması + ince ortak bileşen seti (ek paket yok)

`ThemeData` bileşen temalarıyla ayarlanır, birkaç ince sarmalayıcı widget (`AppTextField`, `PrimaryButton`, durum görünümleri) yazılır. Yazı tipi Roboto (Android varsayılanı, Türkçe karakterleri tam destekler).

**Pros**: ek paket ve `pubspec.yaml` onayı yok; Material bileşenleri erişilebilirliği hazır getirir; koyu tema bedava gelir.
**Cons**: görünüm "standart Material" kalır, markaya özel his az.

### Option 2: Özel tasarım dili (kendi bileşenleri, `ThemeExtension` ağırlıklı)

Kendi buton, kart, input bileşenleri ve token sistemi.

**Pros**: özgün görünüm.
**Cons**: tasarım kaynağı yok; her bileşenin erişilebilirliği ve durumları elle yazılır; 7 kişilik ders projesi için fazla iş.

### Option 3: Hazır UI kiti paketi (ör. `flex_color_scheme`, `google_fonts`)

**Pros**: hızlı zengin tema.
**Cons**: ortak `pubspec.yaml` onayı, ek bağımlılık; `google_fonts` çalışma anında font indirir (çevrimdışı ve gizlilik sorunu).

## Decision

**Chosen option**: Option 1: Material 3 teması ve ince ortak bileşen seti, ek paket olmadan.

Tek seed rengi (`#00796B`) açık ve koyu temayı üretir; ekranlar yalnızca `Theme.of(context)`, `AppSpacing`, `AppRadius` ve `core/widgets` bileşenlerini kullanır.

## Rationale

Tasarım kaynağı olmadığı ve ekibin önceliği modüllerini hızla yazmak olduğu için en az kararla en tutarlı görünümü veren seçenek Material 3. Seed rengi tüm paleti erişilebilir kontrastla türettiği için kimse renk seçmek zorunda kalmaz. Paketsiz kalmak ortak dosya onayını ve bağımlılık riskini ortadan kaldırır; ileride özgün bir görünüm istenirse `AppTheme` tek yerde değişir.

## Feature design

### Dosyalar

| Dosya | İçerik |
|---|---|
| `lib/core/theme/app_tokens.dart` | `AppColors.seed`, `AppSpacing`, `AppRadius` |
| `lib/core/theme/app_theme.dart` | `AppTheme.light()`, `AppTheme.dark()`; ortak `_build(Brightness)` |
| `lib/core/widgets/app_text_field.dart` | `AppTextField` |
| `lib/core/widgets/app_buttons.dart` | `PrimaryButton`, `SecondaryButton` |
| `lib/core/widgets/state_views.dart` | `LoadingView`, `ErrorView`, `EmptyView` |
| `lib/core/widgets/app_snack_bar.dart` | `showAppSnackBar` |
| `lib/core/widgets/under_development_screen.dart` | `UnderDevelopmentScreen` |
| `mobile/design.md` | Kurallar ve bileşen kataloğu |

### Tasarım kararları (RECOMMEND)

| Konu | Karar | İkinci aday |
|---|---|---|
| Yazı tipi | Roboto (sistem), M3 tip ölçeği değiştirilmez; başlıklar `titleLarge`/`headlineSmall`, gövde `bodyLarge`/`bodyMedium` | Inter (`google_fonts`, paket gerekir) |
| Koyu tema | Var, `ThemeMode.system` | Yalnızca açık tema |
| Hata rengi | `colorScheme.error` (seed'den) | Sabit kırmızı |
| Form düzeni | Dikey, alanlar arası `AppSpacing.lg`, alt kısımda tam genişlik birincil düğme | |
| Uygulama çubuğu | `AppBarTheme(centerTitle: false)`, M3 varsayılanı | |
| Gezinme çubuğu | M3 `NavigationBar` (dashboard spec'inde kullanılır) | `BottomNavigationBar` |

### Value sourcing

| Eylem | Değer | Kaynak |
|---|---|---|
| Tema | renkler | `ColorScheme.fromSeed(AppColors.seed, brightness)` |
| Placeholder | başlık, UC kodu, sorumlu, ikon | Constructor parametreleri; değerler dashboard spec'indeki modül listesinden |
| Şifre düğmesi | etiket | `obscured` durumuna göre "Şifreyi göster" / "Şifreyi gizle" |

### Key invariants

- Ekran kodu renk, yazı boyutu ya da köşe değerini sabit yazmaz (AC-10).
- Bütün dokunma hedefleri en az 48 dp.
- Metinler sabit Türkçe.

### Security model

Kapsam dışı (yalnızca görsel katman).

### Critical test scenarios

Widget testleri `mobile/test/core/` altında; ad `DS-<n>` ile biter.
- Tema: `AppTheme.light().colorScheme` seed'den türemiş, M3 açık; koyu tema `Brightness.dark`, verifies **AC-1**, **AC-3**
- Şifre alanı: başta gizli, düğmeye basınca görünür, etiket değişir, verifies **AC-4**
- Buton: yükleniyorken gösterge var, basınca callback çağrılmaz, verifies **AC-5**
- ErrorView: "Tekrar dene" basınca `onRetry` çağrılır; `onRetry` yoksa düğme yok, verifies **AC-6**
- Snackbar: hata rengiyle gösterilir, verifies **AC-7**
- Placeholder: UC kodu ve sorumlu satırı görünür, verifies **AC-8**

## Build plan

Tracer Bullet altında bu dilim, ince yolun mobil ucunun görsel temelidir; UC1 ekranları hemen üstüne kurulur.

1. [x] `app_tokens.dart` ve `app_theme.dart`; `app.dart` `AppTheme.light/dark` ve `ThemeMode.system` kullanır, satisfies **AC-1**, **AC-2**, **AC-3**
2. [x] `AppTextField`, `PrimaryButton`, `SecondaryButton`, satisfies **AC-4**, **AC-5**
3. [x] `LoadingView`, `ErrorView`, `EmptyView`, `showAppSnackBar`, `UnderDevelopmentScreen`, satisfies **AC-6**, **AC-7**, **AC-8**
4. [x] `SystemStatusScreen`'i ortak bileşenlere geçir, sabit renkleri kaldır, satisfies **AC-10**
5. [x] `mobile/design.md`, satisfies **AC-9**
6. [x] Widget testleri ve `docs/traceability.md` DS satırları, satisfies **AC-1** ile **AC-8**

## Consequences

**Positive**:
- Bütün modüller aynı görünür; koyu tema ek iş olmadan gelir.
- Ek paket yok, `pubspec.yaml` değişmez.

**Negative / tradeoffs**:
- Görünüm standart Material 3, markaya özel değil.
- `lib/core/` ortak klasör; bileşen değişikliği ekip onayı ister.

**Neutral**:
- Roboto Android'de sistemde var, font dosyası paketlenmez.

## Follow-up

- [ ] `mobile/AGENTS.md`'deki "tema gelene kadar ekran başına özel stil yazma" satırı `design.md`'ye işaret etsin (`/sync`).
