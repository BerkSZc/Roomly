import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/widgets/state_views.dart';
import '../application/session_controller.dart';

/// Açılışta saklı oturum kontrol edilirken; sunucuya ulaşılamazsa hata ve "Tekrar dene".
class SplashScreen extends ConsumerWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    return Scaffold(
      body: SafeArea(
        child: switch (session) {
          SessionRestoreFailed(:final message) => ErrorView(
            message: message,
            onRetry: () =>
                ref.read(sessionControllerProvider.notifier).restore(),
          ),
          _ => const LoadingView(message: 'Roomly açılıyor...'),
        },
      ),
    );
  }
}
