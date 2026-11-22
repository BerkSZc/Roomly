import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/session.dart';
import '../config/app_config.dart';
import '../storage/token_storage.dart';
import 'auth_interceptor.dart';

BaseOptions _baseOptions(AppConfig config) => BaseOptions(
  baseUrl: config.apiBaseUrl,
  connectTimeout: const Duration(seconds: 5),
  receiveTimeout: const Duration(seconds: 10),
  contentType: Headers.jsonContentType,
  responseType: ResponseType.json,
);

/// Uygulamanın tek `Dio` örneği. Bütün `RemoteXRepository` sınıfları bunu kullanır.
/// Auth interceptor bearer ekler ve 401'de token'ı yeniler (spec 0004).
final dioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  final dio = Dio(_baseOptions(config));
  final refreshClient = Dio(_baseOptions(config));
  dio.interceptors.add(
    AuthInterceptor(
      retryClient: dio,
      refreshClient: refreshClient,
      storage: ref.watch(tokenStorageProvider),
      onSessionExpired: ref.read(sessionEventsProvider).notifyExpired,
    ),
  );
  ref.onDispose(() {
    dio.close();
    refreshClient.close();
  });
  return dio;
});
