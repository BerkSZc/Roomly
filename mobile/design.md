# Roomly tasarım sistemi

Kaynak karar: [spec 0003](../docs/specs/0003-design-system.md). Bu dosya ekranları yazarken başvurduğun kısa rehber.

## İlke

Material 3, tek seed rengi, ek paket yok. Ekranın görünümünü tema verir; sen yalnızca doğru bileşeni ve doğru token'ı seçersin.

## Renk

- Seed: teal `#00796B` (`AppColors.seed`). Açık ve koyu palet `ColorScheme.fromSeed` ile türetilir, uygulama sistem temasını izler.
- Renkleri her zaman `Theme.of(context).colorScheme` üzerinden oku: `primary`, `onPrimary`, `surface`, `surfaceContainerLow`, `error`, `onError` vb.
- Ekran kodunda `Color(0x...)` ya da `Colors.<renk>` yazma.

## Yazı

- Roboto (Android sistemi, Türkçe karakterler tam). Material 3 tip ölçeği değişmez.
- Ekran başlığı `headlineSmall`, kart ve bölüm başlığı `titleMedium`, gövde `bodyLarge` / `bodyMedium`, yardımcı metin `bodySmall`.
- `TextStyle(fontSize: ...)` yazma; `textTheme.x?.copyWith(color: ...)` ile yalnızca rengi değiştir.

## Boşluk ve köşe

| Token | Değer | Ne zaman |
|---|---|---|
| `AppSpacing.xs` | 4 | ikon ile metin arası |
| `AppSpacing.sm` | 8 | sıkı öğeler |
| `AppSpacing.md` | 12 | liste öğeleri arası |
| `AppSpacing.lg` | 16 | form alanları arası, kart iç boşluğu |
| `AppSpacing.xl` | 24 | bölümler arası |
| `AppSpacing.xxl` | 32 | ekran üstü büyük boşluk |
| `AppSpacing.screen` | 16 | ekran kenar boşluğu |
| `AppRadius.sm / md / lg` | 8 / 12 / 16 | çip / düğme ve alan / kart |

## Bileşenler (`lib/core/widgets/`)

| Bileşen | Kullanım |
|---|---|
| `AppTextField` | Her form alanı. `obscure: true` şifre alanı, göster / gizle düğmesiyle. `autofillHints` ver. |
| `PrimaryButton` | Ekranın ana eylemi, tam genişlik. İstek sürerken `isLoading: true`. |
| `SecondaryButton` | İkincil eylem, tam genişlik, kenarlıklı. |
| `LoadingView` | Veri yüklenirken tüm alanı kaplayan gösterge. |
| `ErrorView` | Yükleme hatası; `onRetry` ile "Tekrar dene". |
| `EmptyView` | Liste boşken ikon, başlık, açıklama, isteğe bağlı eylem. |
| `showAppSnackBar` | Kısa geri bildirim; hata için `isError: true`. |
| `UnderDevelopmentScreen` | Henüz yapılmamış modül: başlık, UC kodu, sorumlu. |

## Kurallar

- Metinler sabit Türkçe, l10n yok. Hata mesajları kullanıcının anlayacağı dilde ("Sunucuya ulaşılamadı, bağlantını kontrol et.").
- Dokunma hedefi en az 48 dp (tema düğmeleri zaten öyle). İkon düğmelerine `tooltip` ver (ekran okuyucu etiketi olur).
- Form düzeni: dikey, alanlar arası `AppSpacing.lg`, en altta `PrimaryButton`. Klavye açılınca içerik kaydırılabilir olsun (`SingleChildScrollView`).
- Her asenkron ekran üç durumu gösterir: yükleniyor (`LoadingView`), hata (`ErrorView`), veri (boşsa `EmptyView`).
- Gezinme: dashboard'da M3 `NavigationBar` (dashboard spec'i).
- Yeni ortak bileşen gerekiyorsa `lib/core/widgets/`'a ekle (ortak klasör, ekip onayı) ve bu tabloya yaz.
