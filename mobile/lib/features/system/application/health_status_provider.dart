import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../data/health_repository.dart';

/// Sunucu durumu. Hata hemen görünsün diye otomatik yeniden deneme kapalı.
final healthStatusProvider = FutureProvider.autoDispose<HealthStatus>(
  (ref) => ref.watch(healthRepositoryProvider).check(),
  retry: (retryCount, error) => null,
);
