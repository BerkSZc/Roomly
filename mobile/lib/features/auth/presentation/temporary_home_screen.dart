import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/widgets/state_views.dart';
import '../application/session_controller.dart';

/// UC1 diliminin geçici ana ekranı; dashboard iskeleti (scope #7) bunun yerini alır.
class TemporaryHomeScreen extends ConsumerWidget {
  const TemporaryHomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    final group = session is SessionAuthenticated ? session.user.group : null;
    return Scaffold(
      appBar: AppBar(
        title: Text(group?.name ?? 'Roomly'),
        actions: [
          IconButton(
            tooltip: 'Çıkış yap',
            icon: const Icon(Icons.logout),
            onPressed: () =>
                ref.read(sessionControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: const EmptyView(
        icon: Icons.dashboard_outlined,
        title: 'Ana ekran',
        message: 'Dashboard yakında burada olacak.',
      ),
    );
  }
}
