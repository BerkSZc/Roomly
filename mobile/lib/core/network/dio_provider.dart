import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';

/// Uygulamanın tek `Dio` örneği. Bütün `RemoteXRepository` sınıfları bunu kullanır.
/// Auth interceptor'ı ve refresh kilidi "Kayıt ve giriş (UC1)" ile buraya eklenir.
final dioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  final dio = Dio(
    BaseOptions(
      baseUrl: config.apiBaseUrl,
      connectTimeout: const Duration(seconds: 5),
      receiveTimeout: const Duration(seconds: 10),
      contentType: Headers.jsonContentType,
      responseType: ResponseType.json,
    ),
  );
  ref.onDispose(dio.close);
  return dio;
});
