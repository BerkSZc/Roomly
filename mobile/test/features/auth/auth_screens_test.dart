import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/core/storage/token_storage.dart';

import '../../support/fakes.dart';

Future<void> _settle(WidgetTester tester) async {
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 400));
  await tester.pumpAndSettle();
}

Future<void> _fill(WidgetTester tester, String label, String text) =>
    tester.enterText(find.widgetWithText(TextFormField, label), text);

void main() {
  testWidgets('oturum yokken uygulama giriş ekranında açılır AUTH-16', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);

    expect(find.text('GİRİŞ YAP'), findsOneWidget);
  });

  testWidgets('boş formda istemci doğrulama mesajları görünür AUTH-17', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);

    await tester.tap(find.text('GİRİŞ YAP'));
    await tester.pump();

    expect(find.text('E-posta zorunludur.'), findsOneWidget);
    expect(find.text('Şifre zorunludur.'), findsOneWidget);
  });

  testWidgets('yanlış şifrede sunucu mesajı alan altında görünür AUTH-17', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);

    await _fill(tester, 'E-posta', 'demo@roomly.app');
    await _fill(tester, 'Şifre', 'Yanlis123');
    await tester.tap(find.text('GİRİŞ YAP'));
    await _settle(tester);

    expect(find.text('E-posta ya da şifre hatalı.'), findsOneWidget);
  });

  testWidgets('demo hesabıyla giriş ana ekrana, çıkış girişe götürür AUTH-17', (
    tester,
  ) async {
    final storage = InMemoryTokenStorage();
    await tester.pumpWidget(mockApp(storage: storage));
    await _settle(tester);

    await _fill(tester, 'E-posta', 'demo@roomly.app');
    await _fill(tester, 'Şifre', 'Demo1234');
    await tester.tap(find.text('GİRİŞ YAP'));
    await _settle(tester);

    expect(find.text('Demo Ev'), findsOneWidget);
    expect(await storage.read(), isNotNull);

    await tester.tap(find.text('Diğer'));
    await _settle(tester);
    await scrollAndTap(tester, find.text('Çıkış yap'));
    await _settle(tester);

    expect(find.text('GİRİŞ YAP'), findsOneWidget);
    expect(await storage.read(), isNull);
  });

  testWidgets('yeni kayıt grupsuz kullanıcıyı onboarding e götürür AUTH-17', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);
    await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
    await _settle(tester);

    await _fill(tester, 'Ad', 'Berk');
    await _fill(tester, 'E-posta', 'berk@test.local');
    await _fill(tester, 'Şifre', 'Sifre123');
    await tester.tap(find.widgetWithText(FilledButton, 'KAYIT OL'));
    await _settle(tester);

    expect(find.text('Merhaba Berk'), findsOneWidget);
    expect(find.text('Alan Kur'), findsOneWidget);
    expect(find.text('Koda Katıl'), findsOneWidget);
  });

  testWidgets(
    'kayıtlı e-postayla kayıtta hata e-posta alanı altında görünür AUTH-17',
    (tester) async {
      await tester.pumpWidget(mockApp());
      await _settle(tester);
      await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
      await _settle(tester);

      await _fill(tester, 'Ad', 'Berk');
      await _fill(tester, 'E-posta', 'demo@roomly.app');
      await _fill(tester, 'Şifre', 'Sifre123');
      await tester.tap(find.widgetWithText(FilledButton, 'KAYIT OL'));
      await _settle(tester);

      expect(
        find.text('Bu e-posta adresiyle kayıtlı bir hesap var.'),
        findsOneWidget,
      );
    },
  );

  testWidgets('kayıtta zayıf şifre istemcide reddedilir AUTH-17', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);
    await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
    await _settle(tester);

    await _fill(tester, 'Ad', 'Berk');
    await _fill(tester, 'E-posta', 'berk@test.local');
    await _fill(tester, 'Şifre', 'sadeceharf');
    await tester.tap(find.widgetWithText(FilledButton, 'KAYIT OL'));
    await tester.pump();

    expect(find.text('Şifre en az bir rakam içermelidir.'), findsOneWidget);
  });

  testWidgets('kayıttan bağlantı ve geri hareketi girişe döner AUTH-17', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);
    await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
    await _settle(tester);
    expect(find.text('KAYIT OL'), findsOneWidget);

    await tester.tap(find.text('Zaten hesabın var mı? Giriş yap'));
    await _settle(tester);
    expect(find.text('GİRİŞ YAP'), findsOneWidget);

    await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
    await _settle(tester);
    await tester.binding.handlePopRoute();
    await _settle(tester);
    expect(find.text('GİRİŞ YAP'), findsOneWidget);
  });
}
