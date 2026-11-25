# Roomly tasarım sistemi

Kaynak karar: [spec 0003](../docs/specs/0003-design-system.md). Bu dosya ekranları yazarken başvurduğun kısa rehber.

## İlke

Material 3, ek paket yok. Marka dili: açık yeşil zemin, koyu yeşil mürekkep, lime ana düğme, hap (stadyum) biçimli alan ve düğmeler. Ekranın görünümünü tema verir; sen yalnızca doğru bileşeni ve doğru token'ı seçersin.

## Renk

| Token (`AppColors`) | Değer | Tema rolü | Kullanım |
|---|---|---|---|
| `ink` (= `seed`) | `#1F2E23` | açıkta `primary`, `onSurface`, `onPrimaryContainer` | yazı, ikon, odak kenarlığı, logo |
| `background` | `#F6F8F3` | açıkta `surface`, scaffold zemini | ekran zemini |
| `lime` | `#A7F432` | `primaryContainer` | ana eylem düğmesi (`PrimaryButton`) dolgusu |
| `limeAccent` | `#93E81E` | yok | yalnızca marka alt başlığı; zeminde kontrastı düşük, gövde metni için kullanma |
| `fieldFill` | `#F8FAF6` | açıkta alan dolgusu | form alanı |
| `fieldBorder` | `#9E9E9E` | açıkta `outline` | alan ve düğme kenarlığı |

- Palet `ColorScheme.fromSeed(seedColor: AppColors.seed)` ile türetilir, yukarıdaki roller üstüne yazılır. Koyu tema aynı seed'den türer, yalnızca `primaryContainer` lime kalır; uygulama sistem temasını izler.
- Renkleri her zaman `Theme.of(context).colorScheme` üzerinden oku: `primary`, `onPrimary`, `primaryContainer`, `surface`, `onSurface`, `outline`, `error` vb.
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
| `AppRadius.sm / md / lg` | 8 / 12 / 16 | çip / küçük öğe / kart |
| `AppRadius.pill` | 28 | hap biçimli form alanı (düğmeler `StadiumBorder`) |
| `AppSizes.minTouch` | 48 | en küçük dokunma hedefi |
| `AppSizes.control` | 54 | düğme ve form alanı yüksekliği |

## Bileşenler (`lib/core/widgets/`)

| Bileşen | Kullanım |
|---|---|
| `AppTextField` | Her form alanı, hap biçimli. `obscure: true` şifre alanı, göster / gizle düğmesiyle. `prefixIcon` soldaki ikon, `floatingLabel: false` etiketi yer tutucu gibi içte tutar. `autofillHints` ver. |
| `PrimaryButton` | Ekranın ana eylemi, tam genişlik, lime dolgulu hap. Etiket büyük harf ("GİRİŞ YAP"). İstek sürerken `isLoading: true`. |
| `SecondaryButton` | İkincil eylem, tam genişlik, kenarlıklı hap. |
| `RoomlyLogo` | CustomPaint bina logosu; `size`, isteğe bağlı `color` (varsayılan `onSurface`). |
| `BrandHeader` | Logo + "ROOMLY" + lime alt başlık. |
| `BrandFormScaffold` | Marka başlıklı, kaydırılabilir, duyarlı form ekranı (giriş, kayıt); alanlar en fazla 520 dp. |
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
