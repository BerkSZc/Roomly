import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/features/space/data/space_repository.dart';

import '../../support/fakes.dart';

Future<void> _settle(WidgetTester tester) async {
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 400));
  await tester.pump(const Duration(milliseconds: 400));
  await tester.pumpAndSettle();
}

void main() {
  test('Ev ve Yurt kapasite sınırları dahildir BND-02', () {
    for (final type in SpaceType.values) {
      expect(type.accepts(1), isFalse);
      expect(type.accepts(2), isTrue);
      expect(type.accepts(type == SpaceType.ev ? 20 : 6), isTrue);
      expect(type.accepts(type == SpaceType.ev ? 21 : 7), isFalse);
    }
  });

  testWidgets('alan kurar, bilgileri gösterir ve onayla ayrılır FR-02 FR-10', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await _settle(tester);
    await tester.tap(find.text('Hesabın yok mu? Kayıt ol'));
    await _settle(tester);
    await tester.enterText(find.widgetWithText(TextFormField, 'Ad'), 'Berat');
    await tester.enterText(
      find.widgetWithText(TextFormField, 'E-posta'),
      'berat@test.local',
    );
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Şifre'),
      'Sifre123',
    );
    await tester.tap(find.widgetWithText(FilledButton, 'KAYIT OL'));
    await _settle(tester);
    final router = GoRouter.of(tester.element(find.text('Koda Katıl')));
    router.go('/onboarding/create-group');
    await _settle(tester);
    await tester.enterText(
      find.widgetWithText(TextFormField, 'Alan adı'),
      'Test Yurdu',
    );
    await tester.tap(find.text('Yurt'));
    final capacity = find.widgetWithText(TextFormField, 'Kapasite');
    await tester.enterText(capacity, '7');
    final create = find.widgetWithText(FilledButton, 'Yaşam Alanı Kur');
    await tester.ensureVisible(create);
    await tester.tap(create);
    await _settle(tester);
    expect(find.text('Kapasite 2–6 kişi olmalı.'), findsOneWidget);
    await tester.enterText(capacity, '6');
    await tester.ensureVisible(create);
    await tester.tap(create);
    await _settle(tester);
    expect(find.text('Test Yurdu'), findsOneWidget);

    router.go('/more/space');
    await _settle(tester);
    expect(find.text('1 / 6 kişi'), findsOneWidget);
    expect(find.text('Yönetici'), findsOneWidget);

    router.go('/more/leave');
    await _settle(tester);
    await tester.tap(find.text('Ayrılmayı Başlat'));
    await _settle(tester);
    await tester.tap(find.text('Vazgeç'));
    await _settle(tester);
    expect(find.text('Ayrılmayı Başlat'), findsOneWidget);
    await tester.tap(find.text('Ayrılmayı Başlat'));
    await _settle(tester);
    await tester.tap(find.text('Ayrıl'));
    await _settle(tester);
    expect(find.text('Koda Katıl'), findsOneWidget);
  });
}
