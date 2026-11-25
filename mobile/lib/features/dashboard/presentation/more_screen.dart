import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/modules/app_module.dart';
import '../../../core/modules/module_registry.dart';
import '../../../core/theme/app_tokens.dart';
import '../../auth/application/session_controller.dart';

/// "Diğer" sekmesi: profil, alt barda olmayan modüller, sistem durumu, çıkış (spec 0005 AC-3).
class MoreScreen extends ConsumerWidget {
  const MoreScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    final modules = ref
        .watch(appModulesProvider)
        .where((m) => m.placement == ModulePlacement.more);
    final theme = Theme.of(context);
    final user = session is SessionAuthenticated ? session.user : null;

    return Scaffold(
      appBar: AppBar(title: const Text('Diğer')),
      body: ListView(
        children: [
          if (user != null)
            ListTile(
              contentPadding: const EdgeInsets.symmetric(
                horizontal: AppSpacing.screen,
                vertical: AppSpacing.sm,
              ),
              leading: CircleAvatar(
                child: Text(user.displayName.characters.first.toUpperCase()),
              ),
              title: Text(user.displayName, style: theme.textTheme.titleMedium),
              subtitle: Text(user.email),
            ),
          const Divider(),
          for (final module in modules)
            ListTile(
              leading: Icon(module.icon),
              title: Text(module.title),
              subtitle: module.isPlaceholder
                  ? Text('${module.ucCode} · ${module.owner}')
                  : null,
              trailing: const Icon(Icons.chevron_right),
              onTap: () => context.go(module.path),
            ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.monitor_heart_outlined),
            title: const Text('Sistem durumu'),
            onTap: () => context.push('/system'),
          ),
          ListTile(
            leading: Icon(Icons.logout, color: theme.colorScheme.error),
            title: Text(
              'Çıkış yap',
              style: TextStyle(color: theme.colorScheme.error),
            ),
            onTap: () => ref.read(sessionControllerProvider.notifier).logout(),
          ),
        ],
      ),
    );
  }
}
