import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Derleme anında `--dart-define` ile verilen ortam ayarları.
///
/// ```
/// flutter run --dart-define=USE_MOCK=true
/// flutter run --dart-define=API_BASE_URL=http://localhost:5080   // cihaz + adb reverse
/// ```
///
/// Hiçbir define verilmezse: emülatör (`10.0.2.2:5080`) + gerçek API.
/// Değerler derlemeye gömülür; değiştirmek için uygulamayı yeniden başlatın.
class AppConfig {
  const AppConfig({required this.apiBaseUrl, required this.useMock});

  factory AppConfig.fromEnvironment() => const AppConfig(
    apiBaseUrl: String.fromEnvironment(
      'API_BASE_URL',
      defaultValue: 'http://10.0.2.2:5080',
    ),
    useMock: bool.fromEnvironment('USE_MOCK', defaultValue: false),
  );

  /// API'nin kök adresi, sonunda `/` olmadan. Rotalar `/api/...` ile başlar.
  final String apiBaseUrl;

  /// `true` ise her modül kendi `MockXRepository` sınıfını kullanır.
  final bool useMock;
}

/// Testlerde `appConfigProvider.overrideWithValue(...)` ile değiştirilir.
final appConfigProvider = Provider<AppConfig>(
  (ref) => AppConfig.fromEnvironment(),
);
