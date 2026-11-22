import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Erişim ve refresh token çifti.
class StoredTokens {
  const StoredTokens({required this.accessToken, required this.refreshToken});

  final String accessToken;
  final String refreshToken;
}

/// Token'ların tek saklama yeri (SEC-05). Yalnızca güvenli depo; shared preferences ya da dosya yok.
abstract interface class TokenStorage {
  Future<StoredTokens?> read();

  Future<void> save(StoredTokens tokens);

  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const accessKey = 'roomly.accessToken';
  static const refreshKey = 'roomly.refreshToken';

  final FlutterSecureStorage _storage;

  @override
  Future<StoredTokens?> read() async {
    final access = await _storage.read(key: accessKey);
    final refresh = await _storage.read(key: refreshKey);
    if (access == null || refresh == null) return null;
    return StoredTokens(accessToken: access, refreshToken: refresh);
  }

  @override
  Future<void> save(StoredTokens tokens) async {
    await _storage.write(key: accessKey, value: tokens.accessToken);
    await _storage.write(key: refreshKey, value: tokens.refreshToken);
  }

  @override
  Future<void> clear() async {
    await _storage.delete(key: accessKey);
    await _storage.delete(key: refreshKey);
  }
}

/// Testlerde ve mock modunda bellekte tutan sürüm.
class InMemoryTokenStorage implements TokenStorage {
  InMemoryTokenStorage([this._tokens]);

  StoredTokens? _tokens;

  @override
  Future<StoredTokens?> read() async => _tokens;

  @override
  Future<void> save(StoredTokens tokens) async => _tokens = tokens;

  @override
  Future<void> clear() async => _tokens = null;
}

/// Testlerde `tokenStorageProvider.overrideWithValue(InMemoryTokenStorage())`.
final tokenStorageProvider = Provider<TokenStorage>(
  (ref) => SecureTokenStorage(),
);
