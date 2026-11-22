import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/config/app_config.dart';
import '../../../core/network/dio_provider.dart';

/// Backend'in `GET /health` yanıtı.
class HealthStatus {
  const HealthStatus({required this.status, required this.checks});

  factory HealthStatus.fromJson(Map<String, dynamic> json) => HealthStatus(
        status: json['status'] as String,
        checks: (json['checks'] as Map<String, dynamic>? ?? const {})
            .map((key, value) => MapEntry(key, value as String)),
      );

  final String status;
  final Map<String, String> checks;

  bool get isHealthy => status == 'Healthy';
}

/// Her modülde aynı kalıp: arayüz + Mock + Remote, seçim provider'da.
abstract interface class HealthRepository {
  Future<HealthStatus> check();
}

class MockHealthRepository implements HealthRepository {
  @override
  Future<HealthStatus> check() async {
    await Future<void>.delayed(const Duration(milliseconds: 300));
    return const HealthStatus(status: 'Healthy', checks: {'database': 'Healthy'});
  }
}

class RemoteHealthRepository implements HealthRepository {
  RemoteHealthRepository(this._dio);

  final Dio _dio;

  @override
  Future<HealthStatus> check() async {
    // 503 (Unhealthy) de geçerli bir yanıt: gövdeyi okumak için hata sayma.
    final response = await _dio.get<Map<String, dynamic>>(
      '/health',
      options: Options(validateStatus: (code) => code == 200 || code == 503),
    );
    return HealthStatus.fromJson(response.data!);
  }
}

final healthRepositoryProvider = Provider<HealthRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return config.useMock
      ? MockHealthRepository()
      : RemoteHealthRepository(ref.watch(dioProvider));
});
