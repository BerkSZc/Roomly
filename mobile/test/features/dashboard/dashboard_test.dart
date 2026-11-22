import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/core/modules/app_module.dart';
import 'package:roomly/core/modules/module_registry.dart';
import 'package:roomly/core/storage/token_storage.dart';

import '../../support/fakes.dart';

/// Mock demo hesabının (gruplu) saklı oturumu.
InMemoryTokenStorage _demoSession() => InMemoryTokenStorage(
  const StoredTokens(
    accessToken: 'mock-access:demo@roomly.app',
    refreshToken: 'mock-refresh:demo@roomly.app',
  ),
);

Future<void> _settle(WidgetTester tester) async {
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 400));
  await tester.pumpAndSettle();
}

Future<void> _openDemo(WidgetTester tester, {List overrides = const []}) async {
  await tester.pumpWidget(
    mockApp(storage: _demoSession(), overrides: overrides),
  );
  await _settle(tester);
}

Finder _navBar(String label) =>
    find.descendant(of: find.byType(NavigationBar), matching: find.text(label));

void main() {
  testWidgets('gruplu kullanıcı açılışta Ana Sayfa da grubunu görür DASH-1', (
    tester,
  ) async {
    await _openDemo(tester);

    expect(find.text('Demo Ev'), findsOneWidget);
    expect(find.text('Merhaba Demo Kullanıcı'), findsOneWidget);
    expect(find.text('Ev'), findsOneWidget);
    expect(find.text('Yönetici'), findsOneWidget);
  });

  testWidgets('alt bar 5 sekme gösterir ve sekme placeholder ını açar DASH-2', (
    tester,
  ) async {
    await _openDemo(tester);

    for (final label in [
      'Ana Sayfa',
      'Giderler',
      'Borçlar',
      'Görevler',
      'Diğer',
    ]) {
      expect(_navBar(label), findsOneWidget);
    }

    await tester.tap(_navBar('Giderler'));
    await _settle(tester);
    expect(find.text('UC4 · Sorumlu: Eda'), findsOneWidget);

    await tester.tap(_navBar('Görevler'));
    await _settle(tester);
    expect(find.text('UC7 · Sorumlu: Öykü'), findsOneWidget);
  });

  testWidgets('sekmeler kendi gezinme durumunu korur DASH-2', (tester) async {
    await _openDemo(tester);

    await tester.tap(_navBar('Diğer'));
    await _settle(tester);
    await scrollAndTap(tester, find.text('Faturalar'));
    await _settle(tester);
    expect(find.text('UC6 · Sorumlu: Yusuf'), findsOneWidget);

    await tester.tap(_navBar('Ana Sayfa'));
    await _settle(tester);
    await tester.tap(_navBar('Diğer'));
    await _settle(tester);

    expect(find.text('UC6 · Sorumlu: Yusuf'), findsOneWidget);
  });

  testWidgets('Diğer profil, 5 modül ve çıkış satırlarını gösterir DASH-3', (
    tester,
  ) async {
    await _openDemo(tester);
    await tester.tap(_navBar('Diğer'));
    await _settle(tester);

    expect(find.text('demo@roomly.app'), findsOneWidget);
    for (final row in [
      'UC6 · Yusuf',
      'UC8 · Başak',
      'UC9 · Başak',
      'UC2 · Berat',
      'UC10 · Berat',
    ]) {
      await tester.scrollUntilVisible(
        find.text(row),
        150,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text(row), findsOneWidget);
    }

    await scrollAndTap(tester, find.text('Çıkış yap'));
    await _settle(tester);
    expect(find.text('Giriş yap'), findsOneWidget);
  });

  testWidgets(
    'Ana Sayfa da 8 modül kartı var, karta dokununca modül açılır DASH-4',
    (tester) async {
      await _openDemo(tester);

      for (final module in appModules) {
        await tester.scrollUntilVisible(
          find.text(
            'Geliştirme aşamasında · ${module.ucCode} · ${module.owner}',
          ),
          150,
          scrollable: find.byType(Scrollable).first,
        );
      }

      await tester.tap(find.text('Geliştirme aşamasında · UC10 · Berat'));
      await _settle(tester);
      expect(find.text('UC10 · Sorumlu: Berat'), findsOneWidget);
    },
  );

  testWidgets('her placeholder rotası açılır DASH-5', (tester) async {
    await _openDemo(tester);
    final router = GoRouter.of(tester.element(find.byType(NavigationBar)));

    for (final module in appModules) {
      router.go(module.path);
      await _settle(tester);
      expect(
        find.text('${module.ucCode} · Sorumlu: ${module.owner}'),
        findsOneWidget,
        reason: module.path,
      );
    }
  });

  testWidgets(
    'modülün kendi ekranı ve kartı placeholder ın yerini alır DASH-6',
    (tester) async {
      final custom = [
        AppModule(
          id: 'expenses',
          title: 'Giderler',
          ucCode: 'UC4',
          owner: 'Eda',
          icon: Icons.receipt_long_outlined,
          placement: ModulePlacement.tab,
          screenBuilder: (_) =>
              const Scaffold(body: Text('Gerçek gider ekranı')),
          dashboardCardBuilder: (_) => const Text('Bu ay 1.250 TL'),
        ),
        ...appModules.skip(1),
      ];
      await _openDemo(
        tester,
        overrides: [appModulesProvider.overrideWithValue(custom)],
      );

      expect(find.text('Bu ay 1.250 TL'), findsOneWidget);
      await tester.tap(find.text('Bu ay 1.250 TL'));
      await _settle(tester);
      expect(find.text('Gerçek gider ekranı'), findsOneWidget);
    },
  );

  testWidgets('grupsuz kullanıcı modül rotasına giremez DASH-8', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);
    await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
    await _settle(tester);
    await tester.enterText(find.widgetWithText(TextFormField, 'Ad'), 'Berk');
    await tester.enterText(
      find.widgetWithText(TextFormField, 'E-posta'),
      'berk@test.local',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Şifre'),
      'Sifre123',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Kayıt ol'));
    await _settle(tester);

    GoRouter.of(tester.element(find.text('Alan Kur'))).go('/expenses');
    await _settle(tester);

    expect(find.text('Koda Katıl'), findsOneWidget);
    expect(find.byType(NavigationBar), findsNothing);
  });
}
