import 'package:material_ui/material_ui.dart';

/// Tasarım sisteminin sabitleri (spec 0003, `mobile/design.md`).
/// Ekran kodu renk, boşluk ve köşe değerlerini yalnızca buradan okur.
abstract final class AppColors {
  /// Koyu yeşil mürekkep: yazı, ikon, odak kenarlığı. Paletin seed rengi.
  static const Color ink = Color(0xFF1F2E23);

  /// Uygulama zemini (açık tema `surface`).
  static const Color background = Color(0xFFF6F8F3);

  /// Ana eylem düğmesinin dolgusu (`primaryContainer`).
  static const Color lime = Color(0xFFA7F432);

  /// Marka alt başlığı. Zeminde kontrastı düşük: yalnızca dekoratif metin.
  static const Color limeAccent = Color(0xFF93E81E);

  /// Form alanı dolgusu (açık tema).
  static const Color fieldFill = Color(0xFFF8FAF6);

  /// Form alanı ve düğme kenarlığı (açık tema `outline`).
  static const Color fieldBorder = Color(0xFF9E9E9E);

  /// Material 3 paleti bu renkten türetilir, marka rolleri üstüne yazılır.
  static const Color seed = ink;
}

abstract final class AppSpacing {
  static const double xs = 4;
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;
  static const double xxl = 32;

  /// Ekran kenar boşluğu.
  static const double screen = lg;
}

abstract final class AppRadius {
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;

  /// Hap (stadyum) biçimli form alanı; yüksekliğin yarısı.
  static const double pill = 28;
}

abstract final class AppSizes {
  /// Dokunma hedefi için en küçük değer.
  static const double minTouch = 48;

  /// Hap biçimli düğme ve form alanı yüksekliği.
  static const double control = 54;
}
