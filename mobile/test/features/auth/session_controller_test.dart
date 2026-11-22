import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:roomly/core/auth/session.dart';
import 'package:roomly/core/errors/api_exception.dart';
import 'package:roomly/core/storage/token_storage.dart';
import 'package:roomly/features/auth/application/session_controller.dart';
import 'package:roomly/features/auth/data/auth_repository.dart';

class _MockRepo extends Mock implements AuthRepository {}

const _user = CurrentUser(id: '1', displayName: 'Berk', email: 'b@b.co');
const _tokens = StoredTokens(accessToken: 'a', refreshToken: 'r');

ProviderContainer _container(AuthRepository repo, TokenStorage storage) {
  final container = ProviderContainer(
    overrides: [
      authRepositoryProvider.overrideWithValue(repo),
      tokenStorageProvider.overrideWithValue(storage),
    ],
  );
  addTearDown(container.dispose);
  return container;
}

Future<SessionState> _settle(ProviderContainer c) async {
  c.read(sessionControllerProvider);
  await pumpEventQueue();
  return c.read(sessionControllerProvider);
}

void main() {
  late _MockRepo repo;

  setUp(() => repo = _MockRepo());

  test('saklı token yoksa oturum açılmamış sayılır AUTH-16', () async {
    final c = _container(repo, InMemoryTokenStorage());

    expect(await _settle(c), isA<SessionUnauthenticated>());
    verifyNever(() => repo.me());
  });

  test('saklı token varsa me ile oturum geri yüklenir AUTH-16', () async {
    when(() => repo.me()).thenAnswer((_) async => _user);
    final c = _container(repo, InMemoryTokenStorage(_tokens));

    final state = await _settle(c);

    expect((state as SessionAuthenticated).user.email, 'b@b.co');
  });

  test('açılışta sunucuya ulaşılamazsa token lar silinmez AUTH-16', () async {
    when(() => repo.me()).thenThrow(
      const ApiException(code: ApiException.network, message: 'Ulaşılamadı'),
    );
    final storage = InMemoryTokenStorage(_tokens);
    final c = _container(repo, storage);

    expect(await _settle(c), isA<SessionRestoreFailed>());
    expect(await storage.read(), isNotNull);
  });

  test(
    'girişte token lar güvenli depoya yazılır ve kullanıcı yüklenir SEC-05',
    () async {
      when(() => repo.login(email: 'b@b.co', password: 'Sifre123')).thenAnswer(
        (_) async => const AuthTokens(accessToken: 'a', refreshToken: 'r'),
      );
      when(() => repo.me()).thenAnswer((_) async => _user);
      final storage = InMemoryTokenStorage();
      final c = _container(repo, storage);
      await _settle(c);

      await c
          .read(sessionControllerProvider.notifier)
          .login(email: 'b@b.co', password: 'Sifre123');

      expect((await storage.read())!.refreshToken, 'r');
      expect(c.read(sessionControllerProvider), isA<SessionAuthenticated>());
    },
  );

  test('çıkış sunucuya ulaşılamasa da token ları siler SEC-05', () async {
    when(() => repo.me()).thenAnswer((_) async => _user);
    when(
      () => repo.logout('r'),
    ).thenThrow(const ApiException(code: ApiException.network, message: 'x'));
    final storage = InMemoryTokenStorage(_tokens);
    final c = _container(repo, storage);
    await _settle(c);

    await c.read(sessionControllerProvider.notifier).logout();

    expect(await storage.read(), isNull);
    expect(c.read(sessionControllerProvider), isA<SessionUnauthenticated>());
  });

  test(
    'ağ katmanı oturumun düştüğünü bildirince oturum kapanır AUTH-15',
    () async {
      when(() => repo.me()).thenAnswer((_) async => _user);
      final c = _container(repo, InMemoryTokenStorage(_tokens));
      await _settle(c);

      c.read(sessionEventsProvider).notifyExpired();
      await pumpEventQueue();

      expect(c.read(sessionControllerProvider), isA<SessionUnauthenticated>());
    },
  );

  group('mock repository', () {
    test('demo hesabı gruplu, yeni kayıt grupsuzdur AUTH-18', () async {
      final storage = InMemoryTokenStorage();
      final mock = MockAuthRepository(storage);

      final demo = await mock.login(
        email: MockAuthRepository.demoEmail,
        password: MockAuthRepository.demoPassword,
      );
      await storage.save(demo.toStored());
      expect((await mock.me()).group?.name, 'Demo Ev');

      final fresh = await mock.register(
        displayName: 'Yeni',
        email: 'yeni@test.local',
        password: 'Sifre123',
      );
      await storage.save(fresh.toStored());
      expect((await mock.me()).hasGroup, isFalse);
    });

    test('mock da EMAIL_TAKEN ve INVALID_CREDENTIALS üretir AUTH-18', () async {
      final mock = MockAuthRepository(InMemoryTokenStorage());

      await expectLater(
        mock.register(
          displayName: 'X',
          email: 'DEMO@roomly.app',
          password: 'Sifre123',
        ),
        throwsA(
          isA<ApiException>().having((e) => e.code, 'code', 'EMAIL_TAKEN'),
        ),
      );
      await expectLater(
        mock.login(email: MockAuthRepository.demoEmail, password: 'yanlis'),
        throwsA(
          isA<ApiException>().having(
            (e) => e.code,
            'code',
            'INVALID_CREDENTIALS',
          ),
        ),
      );
    });
  });
}
