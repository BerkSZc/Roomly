import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/session.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/storage/token_storage.dart';
import '../data/auth_repository.dart';

/// Oturumun tek sahibi. Router bu durumu okuyarak yönlendirir (spec 0004 AC-16).
class SessionController extends Notifier<SessionState> {
  late AuthRepository _repository;
  late TokenStorage _storage;

  @override
  SessionState build() {
    _repository = ref.watch(authRepositoryProvider);
    _storage = ref.watch(tokenStorageProvider);

    final subscription = ref
        .watch(sessionEventsProvider)
        .expired
        .listen((_) => state = const SessionUnauthenticated());
    ref.onDispose(subscription.cancel);

    Future.microtask(restore);
    return const SessionUnknown();
  }

  /// Açılışta (ve "Tekrar dene" ile) saklı oturumu geri yükler.
  Future<void> restore() async {
    state = const SessionUnknown();
    if (await _storage.read() == null) {
      state = const SessionUnauthenticated();
      return;
    }
    try {
      state = SessionAuthenticated(await _repository.me());
    } on ApiException catch (e) {
      if (e.isNetwork) {
        state = SessionRestoreFailed(e.message);
      } else {
        await _storage.clear();
        state = const SessionUnauthenticated();
      }
    }
  }

  /// Hata olursa [ApiException] atar; ekran mesajı gösterir.
  Future<void> login({required String email, required String password}) =>
      _signIn(() => _repository.login(email: email, password: password));

  Future<void> register({
    required String displayName,
    required String email,
    required String password,
  }) => _signIn(
    () => _repository.register(
      displayName: displayName,
      email: email,
      password: password,
    ),
  );

  /// Kullanıcı bilgisini (ör. gruba katıldıktan sonra) yeniden okur.
  Future<void> refreshUser() async {
    state = SessionAuthenticated(await _repository.me());
  }

  /// Sunucuya çıkışı bildirmeyi dener; başarısız olsa da yerel oturum kapanır.
  Future<void> logout() async {
    final tokens = await _storage.read();
    if (tokens != null) {
      try {
        await _repository.logout(tokens.refreshToken);
      } on ApiException {
        // Sunucuya ulaşılamasa da çıkış yerelde tamamlanır.
      }
    }
    await _storage.clear();
    state = const SessionUnauthenticated();
  }

  Future<void> _signIn(Future<AuthTokens> Function() call) async {
    final tokens = await call();
    await _storage.save(tokens.toStored());
    try {
      state = SessionAuthenticated(await _repository.me());
    } on ApiException {
      await _storage.clear();
      rethrow;
    }
  }
}

final sessionControllerProvider =
    NotifierProvider<SessionController, SessionState>(SessionController.new);
