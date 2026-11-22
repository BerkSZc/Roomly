import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../features/auth/application/session_controller.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/onboarding_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/splash_screen.dart';
import '../../features/auth/presentation/temporary_home_screen.dart';
import '../../features/system/presentation/system_status_screen.dart';
import '../widgets/under_development_screen.dart';
import 'session_redirect.dart';

/// Uygulamanın tek router'ı. Oturum değişince `redirect` yeniden çalışır (spec 0004 AC-16).
final routerProvider = Provider<GoRouter>((ref) {
  final sessionChanges = ValueNotifier<int>(0);
  ref.listen(sessionControllerProvider, (_, _) => sessionChanges.value++);

  final router = GoRouter(
    initialLocation: SessionRedirect.splash,
    refreshListenable: sessionChanges,
    redirect: (context, state) => SessionRedirect.redirect(
      ref.read(sessionControllerProvider),
      state.matchedLocation,
    ),
    routes: [
      GoRoute(
        path: SessionRedirect.splash,
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(
        path: SessionRedirect.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: SessionRedirect.register,
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: SessionRedirect.onboarding,
        builder: (context, state) => const OnboardingScreen(),
        routes: [
          GoRoute(
            path: 'create-group',
            builder: (context, state) => const UnderDevelopmentScreen(
              title: 'Alan Kur',
              ucCode: 'UC2',
              owner: 'Berat',
              icon: Icons.add_home_outlined,
            ),
          ),
        ],
      ),
      GoRoute(
        path: SessionRedirect.join,
        builder: (context, state) => const UnderDevelopmentScreen(
          title: 'Koda Katıl',
          ucCode: 'UC3',
          owner: 'Berk',
          icon: Icons.vpn_key_outlined,
        ),
      ),
      GoRoute(
        path: SessionRedirect.home,
        builder: (context, state) => const TemporaryHomeScreen(),
      ),
      GoRoute(
        path: SessionRedirect.system,
        builder: (context, state) => const SystemStatusScreen(),
      ),
    ],
  );
  ref.onDispose(() {
    router.dispose();
    sessionChanges.dispose();
  });
  return router;
});
