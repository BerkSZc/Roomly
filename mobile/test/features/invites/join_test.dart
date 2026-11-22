import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/core/errors/api_exception.dart';
import 'package:roomly/core/storage/token_storage.dart';
import 'package:roomly/features/auth/data/auth_repository.dart';
import 'package:roomly/features/invites/data/invite_repository.dart';
import 'package:roomly/features/invites/presentation/invite_code_format.dart';

import '../../support/fakes.dart';

Future<void> _settle(WidgetTester tester) async {
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 400));
  await tester.pumpAndSettle();
}

/// Mock modunda yeni (grupsuz) kullanıcıyla kayıt olup Koda Katıl ekranını açar.
Future<void> _openJoin(WidgetTester tester, {String path = '/join'}) async {
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
  GoRouter.of(tester.element(find.text('Koda Katıl'))).go(path);
  await _settle(tester);
}

Future<void> _join(WidgetTester tester, String code) async {
  await tester.enterText(
    find.widgetWithText(TextFormField, 'Davet kodu'),
    code,
  );
  await tester.tap(find.widgetWithText(FilledButton, 'Katıl'));
  await _settle(tester);
}

void main() {
  group('biçim', () {
    test('normalizasyon backend ile aynıdır JOIN-2', () {
      expect(InviteCodeFormat.normalize(' demo-room–2026 '), 'DEMOROOM2026');
      expect(InviteCodeFormat.isWellFormed('demo room 2026'), isTrue);
      expect(InviteCodeFormat.isWellFormed('ABCDEFG'), isFalse);
      expect(InviteCodeFormat.isWellFormed('KOD!KOD!KOD'), isFalse);
      expect(InviteCodeFormat.validate(''), 'Davet kodu zorunludur.');
    });
  });

  testWidgets('geçersiz biçim istemcide reddedilir JOIN-11', (tester) async {
    await _openJoin(tester);

    await _join(tester, 'ab');

    expect(find.text('Davet kodu geçersiz görünüyor.'), findsOneWidget);
  });

  testWidgets('sunucu hatası alan altında Türkçe gösterilir JOIN-11', (
    tester,
  ) async {
    await _openJoin(tester);

    await _join(tester, 'dolu-grup-2026');
    expect(find.text('Bu grup dolu, yeni üye kabul edemiyor.'), findsOneWidget);

    await _join(tester, 'YANLIS-KOD-0001');
    expect(
      find.text('Bu davet kodu geçersiz. Kodu kontrol edip tekrar dene.'),
      findsOneWidget,
    );
  });

  testWidgets(
    'başarılı katılım Ana Sayfa ya götürür ve mesaj gösterir JOIN-12',
    (tester) async {
      await _openJoin(tester);

      await tester.enterText(
        find.widgetWithText(TextFormField, 'Davet kodu'),
        'demo-room-2026',
      );
      await tester.tap(find.widgetWithText(FilledButton, 'Katıl'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.text('Demo Ev grubuna katıldın.'), findsOneWidget);
      await _settle(tester);

      expect(find.byType(NavigationBar), findsOneWidget);
      expect(find.text('Merhaba Berk'), findsOneWidget);
      expect(find.text('Üye'), findsOneWidget);
    },
  );

  testWidgets('code parametresiyle açılınca alan dolu gelir JOIN-11', (
    tester,
  ) async {
    await _openJoin(tester, path: '/join?code=QWER-TYUP-2026');

    final field = tester.widget<EditableText>(find.byType(EditableText));
    expect(field.controller.text, 'QWER-TYUP-2026');
  });

  group('mock katılım', () {
    test('demo kodu üye yapar, özel kodlar hata üretir JOIN-13', () async {
      final storage = InMemoryTokenStorage();
      final auth = MockAuthRepository(storage);
      final tokens = await auth.register(
        displayName: 'Yeni',
        email: 'yeni@test.local',
        password: 'Sifre123',
      );
      await storage.save(tokens.toStored());
      final invites = MockInviteRepository(auth);

      final group = await invites.join('demo room 2026');
      expect(group.role, 'Member');
      expect((await auth.me()).group?.name, 'Demo Ev');

      for (final entry in {
        'DOLU-GRUP-2026': 'CAPACITY_EXCEEDED',
        'ESKI-KOD-2026': 'INVITE_EXPIRED',
        'IPTAL-KOD-2026': 'INVITE_REVOKED',
        'BASKA-KOD-2026': 'INVITE_INVALID',
      }.entries) {
        await expectLater(
          invites.join(entry.key),
          throwsA(
            isA<ApiException>().having((e) => e.code, 'code', entry.value),
          ),
        );
      }
    });
  });
}
