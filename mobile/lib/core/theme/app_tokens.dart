import 'package:material_ui/material_ui.dart';

/// Tasarım sisteminin sabitleri (spec 0003, `mobile/design.md`).
/// Ekran kodu renk, boşluk ve köşe değerlerini yalnızca buradan okur.
abstract final class AppColors {
  /// Material 3 açık ve koyu paleti bu tek renkten türetir.
  static const Color seed = Color(0xFF00796B);
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
}

/// Dokunma hedefi ve düğme yüksekliği için en küçük değer.
abstract final class AppSizes {
  static const double minTouch = 48;
}
