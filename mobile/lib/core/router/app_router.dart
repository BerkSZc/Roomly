import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../features/auth/application/session_controller.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/onboarding_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/splash_screen.dart';
import '../../features/dashboard/presentation/dashboard_shell.dart';
import '../../features/dashboard/presentation/home_screen.dart';
import '../../features/dashboard/presentation/more_screen.dart';
import '../../features/invites/presentation/join_screen.dart';
import '../../features/space/presentation/create_space_screen.dart';
import '../../features/system/presentation/system_status_screen.dart';
import '../modules/app_module.dart';
import '../modules/module_registry.dart';
import '../widgets/under_development_screen.dart';
import 'session_redirect.dart';

/// Uygulamanın tek router'ı. Oturum değişince `redirect` yeniden çalışır (spec 0004 AC-16);
/// dashboard sekmeleri ve modül rotaları registry'den üretilir (spec 0005).
final routerProvider = Provider<GoRouter>((ref) {
  final modules = ref.watch(appModulesProvider);
  final sessionChanges = ValueNotifier<int>(0);
  ref.listen(sessionControllerProvider, (_, _) => sessionChanges.value++);

  final tabModules = modules
      .where((m) => m.placement == ModulePlacement.tab)
      .toList();
  final moreModules = modules
      .where((m) => m.placement == ModulePlacement.more)
      .toList();

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
            builder: (context, state) => const CreateSpaceScreen(),
          ),
        ],
      ),
      GoRoute(
        path: SessionRedirect.join,
        builder: (context, state) =>
            JoinScreen(initialCode: state.uri.queryParameters['code']),
      ),
      GoRoute(
        path: SessionRedirect.system,
        builder: (context, state) => const SystemStatusScreen(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) =>
            DashboardShell(navigationShell: shell, tabModules: tabModules),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: SessionRedirect.home,
                builder: (context, state) => const HomeScreen(),
              ),
            ],
          ),
          for (final module in tabModules)
            StatefulShellBranch(routes: [_moduleRoute(module, module.path)]),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: '/more',
                builder: (context, state) => const MoreScreen(),
                routes: [
                  for (final module in moreModules)
                    _moduleRoute(module, module.id),
                ],
              ),
            ],
          ),
        ],
      ),
    ],
  );
  ref.onDispose(() {
    router.dispose();
    sessionChanges.dispose();
  });
  return router;
});

GoRoute _moduleRoute(AppModule module, String path) => GoRoute(
  path: path,
  builder: (context, state) =>
      module.screenBuilder?.call(context) ??
      UnderDevelopmentScreen(
        title: module.title,
        ucCode: module.ucCode,
        owner: module.owner,
        icon: module.icon,
      ),
  routes: module.routes,
);
