import 'dart:async';

import 'package:dio/dio.dart';

import '../storage/token_storage.dart';

/// Korumalı isteklere bearer ekler. 401 gelirse tek bir refresh yapar (aynı anda gelen 401'ler
/// aynı refresh'i bekler) ve isteği bir kez tekrarlar. Refresh reddedilirse token'ları siler ve
/// [onSessionExpired] çağırır (spec 0004 AC-15).
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required this._retryClient,
    required this._refreshClient,
    required this._storage,
    required this._onSessionExpired,
  });

  static const _retriedKey = 'roomly.retried';

  /// Bu yollara token eklenmez ve 401'de refresh yapılmaz.
  static const _anonymousPaths = {
    '/api/auth/login',
    '/api/auth/register',
    '/api/auth/refresh',
    '/api/auth/logout',
  };

  final Dio _retryClient;
  final Dio _refreshClient;
  final TokenStorage _storage;
  final void Function() _onSessionExpired;

  Future<String?>? _refreshing;

  bool _isAnonymous(RequestOptions options) =>
      _anonymousPaths.contains(options.path);

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    if (!_isAnonymous(options)) {
      final tokens = await _storage.read();
      if (tokens != null) {
        options.headers['Authorization'] = 'Bearer ${tokens.accessToken}';
      }
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final options = err.requestOptions;
    final canRefresh =
        err.response?.statusCode == 401 &&
        !_isAnonymous(options) &&
        options.extra[_retriedKey] != true;
    if (!canRefresh) return handler.next(err);

    final String? newAccess;
    try {
      newAccess = await (_refreshing ??= _refresh().whenComplete(
        () => _refreshing = null,
      ));
    } on DioException catch (refreshError) {
      // Ağ hatası: oturumu düşürme, asıl hatayı ilet.
      return handler.next(refreshError);
    }

    if (newAccess == null) return handler.next(err);

    options.extra[_retriedKey] = true;
    options.headers['Authorization'] = 'Bearer $newAccess';
    try {
      handler.resolve(await _retryClient.fetch<dynamic>(options));
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  /// Yeni erişim token'ı ya da (refresh reddedildiyse) `null`. Ağ hatasında [DioException] atar.
  Future<String?> _refresh() async {
    final tokens = await _storage.read();
    if (tokens == null) {
      _onSessionExpired();
      return null;
    }

    try {
      final response = await _refreshClient.post<Map<String, dynamic>>(
        '/api/auth/refresh',
        data: {'refreshToken': tokens.refreshToken},
      );
      final data = response.data!;
      final access = data['accessToken'] as String;
      await _storage.save(
        StoredTokens(
          accessToken: access,
          refreshToken: data['refreshToken'] as String,
        ),
      );
      return access;
    } on DioException catch (e) {
      if (e.response == null) rethrow;
      await _storage.clear();
      _onSessionExpired();
      return null;
    }
  }
}
