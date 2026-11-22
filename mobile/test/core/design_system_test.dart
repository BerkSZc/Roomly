import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/core/theme/app_theme.dart';
import 'package:roomly/core/theme/app_tokens.dart';
import 'package:roomly/core/widgets/app_buttons.dart';
import 'package:roomly/core/widgets/app_snack_bar.dart';
import 'package:roomly/core/widgets/app_text_field.dart';
import 'package:roomly/core/widgets/state_views.dart';
import 'package:roomly/core/widgets/under_development_screen.dart';

Widget _host(Widget child) => MaterialApp(
  theme: AppTheme.light(),
  home: Scaffold(body: child),
);

void main() {
  group('tema', () {
    test('açık ve koyu tema seed renginden türetilir DS-01', () {
      final light = AppTheme.light();
      final dark = AppTheme.dark();
      final expected = ColorScheme.fromSeed(seedColor: AppColors.seed);

      expect(light.useMaterial3, isTrue);
      expect(light.colorScheme.primary, expected.primary);
      expect(dark.colorScheme.brightness, Brightness.dark);
    });

    test('düğme, alan ve kart temaları token kullanır DS-03', () {
      final theme = AppTheme.light();
      final filled = theme.filledButtonTheme.style!;
      expect(
        filled.minimumSize!.resolve({})!.height,
        greaterThanOrEqualTo(AppSizes.minTouch),
      );
      expect(theme.inputDecorationTheme.filled, isTrue);
      expect(theme.cardTheme.elevation, 0);
      expect(theme.snackBarTheme.behavior, SnackBarBehavior.floating);
    });

    test('boşluk ve köşe tokenları sabit değerlerdedir DS-02', () {
      expect(
        [AppSpacing.xs, AppSpacing.sm, AppSpacing.md, AppSpacing.lg],
        [4, 8, 12, 16],
      );
      expect([AppSpacing.xl, AppSpacing.xxl, AppSpacing.screen], [24, 32, 16]);
      expect([AppRadius.sm, AppRadius.md, AppRadius.lg], [8, 12, 16]);
    });
  });

  testWidgets('şifre alanı göster düğmesiyle görünür olur DS-04', (
    tester,
  ) async {
    await tester.pumpWidget(
      _host(const AppTextField(label: 'Şifre', obscure: true)),
    );

    EditableText field() => tester.widget(find.byType(EditableText));
    expect(field().obscureText, isTrue);
    expect(find.byTooltip('Şifreyi göster'), findsOneWidget);

    await tester.tap(find.byTooltip('Şifreyi göster'));
    await tester.pump();

    expect(field().obscureText, isFalse);
    expect(find.byTooltip('Şifreyi gizle'), findsOneWidget);
  });

  testWidgets(
    'yükleniyorken düğme gösterge gösterir ve dokunuşu yok sayar DS-05',
    (tester) async {
      var taps = 0;
      await tester.pumpWidget(
        _host(
          PrimaryButton(
            label: 'Giriş yap',
            onPressed: () => taps++,
            isLoading: true,
          ),
        ),
      );

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('Giriş yap'), findsNothing);
      await tester.tap(find.byType(FilledButton));
      expect(taps, 0);
    },
  );

  testWidgets('hata görünümü tekrar dene ile callback çağırır DS-06', (
    tester,
  ) async {
    var retried = false;
    await tester.pumpWidget(
      _host(ErrorView(message: 'Yüklenemedi', onRetry: () => retried = true)),
    );
    await tester.tap(find.text('Tekrar dene'));
    expect(retried, isTrue);

    await tester.pumpWidget(_host(const ErrorView(message: 'Yüklenemedi')));
    expect(find.text('Tekrar dene'), findsNothing);
  });

  testWidgets('boş görünüm başlık ve eylemi gösterir DS-06', (tester) async {
    await tester.pumpWidget(
      _host(
        EmptyView(
          icon: Icons.inbox,
          title: 'Henüz kayıt yok',
          actionLabel: 'Ekle',
          onAction: () {},
        ),
      ),
    );
    expect(find.text('Henüz kayıt yok'), findsOneWidget);
    expect(find.text('Ekle'), findsOneWidget);
  });

  testWidgets('hata snackbarı hata rengiyle gösterilir DS-07', (tester) async {
    await tester.pumpWidget(
      _host(
        Builder(
          builder: (context) => TextButton(
            onPressed: () => showAppSnackBar(context, 'Olmadı', isError: true),
            child: const Text('göster'),
          ),
        ),
      ),
    );
    await tester.tap(find.text('göster'));
    await tester.pump();

    final snack = tester.widget<SnackBar>(find.byType(SnackBar));
    expect(snack.backgroundColor, AppTheme.light().colorScheme.error);
    expect(find.text('Olmadı'), findsOneWidget);
  });

  testWidgets('placeholder ekranı UC kodu ve sorumluyu gösterir DS-08', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AppTheme.light(),
        home: const UnderDevelopmentScreen(
          title: 'Giderler',
          ucCode: 'UC4',
          owner: 'Eda',
          icon: Icons.receipt_long,
        ),
      ),
    );
    expect(find.text('Giderler'), findsOneWidget);
    expect(find.text('Geliştirme Aşamasında'), findsOneWidget);
    expect(find.text('UC4 · Sorumlu: Eda'), findsOneWidget);
  });
}
