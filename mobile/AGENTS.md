# Mobile (Flutter, Android)

## Overview

Roomly'nin Android uygulaması. Ortak altyapı `lib/core/` altında, her modül `lib/features/<modul>/{data,application,presentation}` içinde. Backend bitmeden ekranlar mock repository ile çalışabilir.

## Key files

| File | Owns |
|---|---|
| `lib/main.dart`, `lib/app.dart` | `ProviderScope` ve `MaterialApp.router` |
| `lib/core/config/app_config.dart` | `API_BASE_URL` ve `USE_MOCK` (`--dart-define`), `appConfigProvider` |
| `lib/core/network/dio_provider.dart` | Tek `Dio` örneği (auth interceptor UC1 ile buraya gelir) |
| `lib/core/router/app_router.dart` | Tek `GoRouter` (`routerProvider`) |
| `lib/features/system/` | Mock / gerçek repository kalıbının örnek modülü (`/health`) |
| `android/app/src/debug/res/xml/network_security_config.xml` | Debug'da düz HTTP yalnızca `10.0.2.2` ve `localhost` |

## Commands

```bash
flutter pub get
flutter run                                                    # emülatör + gerçek API (10.0.2.2:5080)
flutter run --dart-define=USE_MOCK=true                        # mock veri
adb reverse tcp:5080 tcp:5080 && flutter run --dart-define=API_BASE_URL=http://localhost:5080   # USB cihaz
flutter analyze && flutter test
dart format lib test
```

## Conventions

- Katmanlar: `data/` (repository arayüzü, `MockXRepository`, `RemoteXRepository`, DTO'lar), `application/` (Riverpod provider'ları, `Notifier` / `AsyncNotifier`), `presentation/` (ekranlar ve widget'lar).
- Her modül kendi `xRepositoryProvider`'ında `appConfigProvider.useMock` değerine bakarak Mock ya da Remote seçer. Ekran repository'yi doğrudan değil provider üzerinden kullanır.
- HTTP yalnızca `dioProvider` ile; yeni `Dio()` oluşturma. Rotalar `/api/...`.
- Riverpod code generation kullanılmaz; provider'lar elle yazılır. Testlerde `ProviderScope(overrides: [...])` ile sahte verilir.
- Material import'u `package:material_ui/material_ui.dart` (Flutter 3.50 şablonu), `flutter/material.dart` değil.
- Metinler sabit Türkçe. Tema, renk ve tipografi "Design system & UI foundation" (`design.md`) ile gelecek; o gelene kadar ekran başına özel stil yazma.
- Token'lar yalnızca `flutter_secure_storage` içinde (SEC-05).

## Gotchas

- `--dart-define` değerleri derlemeye gömülür; ortam değiştirmek için uygulamayı yeniden başlat (hot reload yetmez).
- Riverpod 3 hata veren provider'ları varsayılan olarak tekrar dener; hatanın hemen görünmesi gerekiyorsa provider'a `retry: (_, __) => null` ver.
- Paket adı `com.roomly.app` (`MainActivity` `kotlin/com/roomly/app/` altında). minSdk Flutter tabanı olan 24.
- Release derleme düz HTTP kabul etmez; HTTPS'li sunucu gerekir.
- Flutter 3.50, uygulama dışından gelen intent'lerin release derlemede yok sayılabileceği uyarısını veriyor; deep link özelliği (#11) tasarlanırken kontrol et.

## Related specs

- [0001 Stack ve mimari](../docs/specs/0001-stack-architecture/index.md)

_Drafted by /audit from the repo, worth a quick human pass. Edit freely: once a line stops matching this draft, later runs treat it as curated and will flag rather than overwrite it._
