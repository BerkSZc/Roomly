import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/session.dart';
import '../../../core/config/app_config.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/network/dio_provider.dart';
import '../../../core/storage/token_storage.dart';

/// Kayıt / giriş yanıtındaki token çifti.
class AuthTokens {
  const AuthTokens({required this.accessToken, required this.refreshToken});

  factory AuthTokens.fromJson(Map<String, dynamic> json) => AuthTokens(
    accessToken: json['accessToken'] as String,
    refreshToken: json['refreshToken'] as String,
  );

  final String accessToken;
  final String refreshToken;

  StoredTokens toStored() =>
      StoredTokens(accessToken: accessToken, refreshToken: refreshToken);
}

/// Auth uçları (spec 0004). Hatalar [ApiException] olarak atılır.
abstract interface class AuthRepository {
  Future<AuthTokens> register({
    required String displayName,
    required String email,
    required String password,
  });

  Future<AuthTokens> login({required String email, required String password});

  /// Saklı token'la `GET /api/auth/me`. Erişim token'ının süresi dolmuşsa interceptor yeniler.
  Future<CurrentUser> me();

  Future<void> logout(String refreshToken);
}

class RemoteAuthRepository implements AuthRepository {
  RemoteAuthRepository(this._dio);

  final Dio _dio;

  @override
  Future<AuthTokens> register({
    required String displayName,
    required String email,
    required String password,
  }) => _tokens('/api/auth/register', {
    'displayName': displayName,
    'email': email,
    'password': password,
  });

  @override
  Future<AuthTokens> login({required String email, required String password}) =>
      _tokens('/api/auth/login', {'email': email, 'password': password});

  @override
  Future<CurrentUser> me() async {
    try {
      final response = await _dio.get<Map<String, dynamic>>('/api/auth/me');
      return CurrentUser.fromJson(response.data!);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  @override
  Future<void> logout(String refreshToken) async {
    try {
      await _dio.post<void>(
        '/api/auth/logout',
        data: {'refreshToken': refreshToken},
      );
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  Future<AuthTokens> _tokens(String path, Map<String, String> body) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(path, data: body);
      return AuthTokens.fromJson(response.data!);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Bellekte çalışan sahte backend (`USE_MOCK=true`). `demo@roomly.app` / `Demo1234` grubu olan
/// kullanıcıdır; yeni kayıt olan kullanıcının grubu yoktur (spec 0004 AC-18).
class MockAuthRepository implements AuthRepository {
  MockAuthRepository(this._storage);

  static const demoEmail = 'demo@roomly.app';
  static const demoPassword = 'Demo1234';
  static const demoGroup = GroupSummary(
    id: 'mock-group-1',
    name: 'Demo Ev',
    type: 'Ev',
    role: 'Admin',
  );

  final TokenStorage _storage;
  final Map<String, _MockUser> _users = {
    demoEmail: const _MockUser(
      id: 'mock-user-demo',
      displayName: 'Demo Kullanıcı',
      email: demoEmail,
      password: demoPassword,
      group: demoGroup,
    ),
  };

  static const _latency = Duration(milliseconds: 300);

  @override
  Future<AuthTokens> register({
    required String displayName,
    required String email,
    required String password,
  }) async {
    await Future<void>.delayed(_latency);
    final key = email.trim().toLowerCase();
    if (_users.containsKey(key)) {
      throw const ApiException(
        code: 'EMAIL_TAKEN',
        message: 'Bu e-posta adresiyle kayıtlı bir hesap var.',
        status: 409,
      );
    }
    final user = _MockUser(
      id: 'mock-user-${_users.length + 1}',
      displayName: displayName.trim(),
      email: key,
      password: password,
    );
    _users[key] = user;
    return _tokensFor(user);
  }

  @override
  Future<AuthTokens> login({
    required String email,
    required String password,
  }) async {
    await Future<void>.delayed(_latency);
    final user = _users[email.trim().toLowerCase()];
    if (user == null || user.password != password) {
      throw const ApiException(
        code: 'INVALID_CREDENTIALS',
        message: 'E-posta ya da şifre hatalı.',
        status: 401,
      );
    }
    return _tokensFor(user);
  }

  @override
  Future<CurrentUser> me() async {
    await Future<void>.delayed(_latency);
    final tokens = await _storage.read();
    final email = tokens?.accessToken.replaceFirst('mock-access:', '');
    final user = _users[email];
    if (user == null) {
      throw const ApiException(
        code: 'UNAUTHORIZED',
        message: 'Oturumunun süresi doldu. Tekrar giriş yap.',
        status: 401,
      );
    }
    return user.toCurrentUser();
  }

  /// Mock grup işlemlerinden sonra `me` güncel üyeliği döner.
  Future<void> assignGroupToCurrentUser(GroupSummary? group) async {
    final tokens = await _storage.read();
    final email = tokens?.accessToken.replaceFirst('mock-access:', '');
    final user = _users[email];
    if (user == null) return;
    _users[user.email] = _MockUser(
      id: user.id,
      displayName: user.displayName,
      email: user.email,
      password: user.password,
      group: group,
    );
  }

  @override
  Future<void> logout(String refreshToken) async {}

  /// Mock token'ı kullanıcıyı e-postasıyla taşır; uygulama yeniden açılınca `me` onu bulur.
  AuthTokens _tokensFor(_MockUser user) => AuthTokens(
    accessToken: 'mock-access:${user.email}',
    refreshToken: 'mock-refresh:${user.email}',
  );
}

class _MockUser {
  const _MockUser({
    required this.id,
    required this.displayName,
    required this.email,
    required this.password,
    this.group,
  });

  final String id;
  final String displayName;
  final String email;
  final String password;
  final GroupSummary? group;

  CurrentUser toCurrentUser() =>
      CurrentUser(id: id, displayName: displayName, email: email, group: group);
}

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return config.useMock
      ? MockAuthRepository(ref.watch(tokenStorageProvider))
      : RemoteAuthRepository(ref.watch(dioProvider));
});
