import 'package:material_ui/material_ui.dart';

import 'app_tokens.dart';

/// Uygulamanın tek teması: Material 3, seed `AppColors.seed`, açık ve koyu.
/// Marka rolleri (zemin, mürekkep, lime ana düğme) türetilen paletin üstüne yazılır.
abstract final class AppTheme {
  static ThemeData light() => _build(Brightness.light);

  static ThemeData dark() => _build(Brightness.dark);

  /// Marka rolleri yazılmış renk paleti.
  static ColorScheme colorScheme(Brightness brightness) {
    final base =
        ColorScheme.fromSeed(
          seedColor: AppColors.seed,
          brightness: brightness,
        ).copyWith(
          primaryContainer: AppColors.lime,
          onPrimaryContainer: AppColors.ink,
        );
    if (brightness == Brightness.dark) return base;
    return base.copyWith(
      primary: AppColors.ink,
      onPrimary: Colors.white,
      surface: AppColors.background,
      onSurface: AppColors.ink,
      outline: AppColors.fieldBorder,
    );
  }

  static ThemeData _build(Brightness brightness) {
    final scheme = colorScheme(brightness);
    final isLight = brightness == Brightness.light;

    const buttonSize = Size(64, AppSizes.control);
    const buttonText = TextStyle(
      fontSize: 16,
      fontWeight: FontWeight.w800,
      letterSpacing: 0.6,
    );
    final buttonShape = StadiumBorder(
      side: BorderSide(
        color: scheme.outline.withValues(alpha: 0.75),
        width: 1.2,
      ),
    );

    OutlineInputBorder pill(Color color, double width) => OutlineInputBorder(
      borderRadius: BorderRadius.circular(AppRadius.pill),
      borderSide: BorderSide(color: color, width: width),
    );
    const fieldText = TextStyle(fontSize: 16, fontWeight: FontWeight.w500);

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: scheme.surface,
      appBarTheme: AppBarTheme(
        centerTitle: false,
        backgroundColor: scheme.surface,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: buttonSize,
          shape: buttonShape,
          backgroundColor: scheme.primaryContainer,
          foregroundColor: scheme.onPrimaryContainer,
          disabledBackgroundColor: scheme.primaryContainer.withValues(
            alpha: 0.6,
          ),
          disabledForegroundColor: scheme.onPrimaryContainer,
          textStyle: buttonText,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: buttonSize,
          shape: buttonShape,
          side: BorderSide(color: scheme.outline, width: 1.2),
          textStyle: buttonText,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          minimumSize: const Size(64, AppSizes.minTouch),
          foregroundColor: scheme.onSurface,
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: isLight
            ? AppColors.fieldFill
            : scheme.surfaceContainerHighest,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 22,
          vertical: 15,
        ),
        labelStyle: fieldText.copyWith(color: scheme.onSurface),
        hintStyle: fieldText.copyWith(color: scheme.onSurfaceVariant),
        prefixIconColor: scheme.onSurface,
        suffixIconColor: scheme.onSurface,
        border: pill(scheme.outline, 1.2),
        enabledBorder: pill(scheme.outline, 1.2),
        focusedBorder: pill(scheme.onSurface, 1.6),
        errorBorder: pill(scheme.error, 1.3),
        focusedErrorBorder: pill(scheme.error, 1.6),
        disabledBorder: pill(scheme.outline.withValues(alpha: 0.5), 1.2),
      ),
      cardTheme: CardThemeData(
        elevation: 0,
        color: scheme.surfaceContainerLow,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(AppRadius.lg),
        ),
      ),
      snackBarTheme: const SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
      ),
    );
  }
}
