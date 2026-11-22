import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/system/presentation/system_status_screen.dart';

/// Uygulamanın tek router'ı.
/// Oturuma göre yönlendirme (UC1), dashboard sekmeleri ve modül rotaları
/// ilgili özelliklerle buraya eklenir.
final routerProvider = Provider<GoRouter>((ref) {
  final router = GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => const SystemStatusScreen(),
      ),
    ],
  );
  ref.onDispose(router.dispose);
  return router;
});
