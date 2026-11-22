import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/modules/module_registry.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../auth/application/session_controller.dart';
import 'module_card.dart';

/// Ana Sayfa: grup başlığı ve her modülün kartı (spec 0005 AC-1, AC-4).
class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  static String typeLabel(String type) => type == 'Yurt' ? 'Yurt' : 'Ev';

  static String roleLabel(String role) => role == 'Admin' ? 'Yönetici' : 'Üye';

  Future<void> _refresh(BuildContext context, WidgetRef ref) async {
    try {
      await ref.read(sessionControllerProvider.notifier).refreshUser();
    } on ApiException catch (e) {
      if (context.mounted) showAppSnackBar(context, e.message, isError: true);
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    final modules = ref.watch(appModulesProvider);
    if (session is! SessionAuthenticated) return const SizedBox.shrink();
    final user = session.user;
    final group = user.group;
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(group?.name ?? 'Roomly')),
      body: RefreshIndicator(
        onRefresh: () => _refresh(context, ref),
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.screen),
          children: [
            Text(
              'Merhaba ${user.displayName}',
              style: theme.textTheme.headlineSmall,
            ),
            if (group != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Wrap(
                spacing: AppSpacing.sm,
                children: [
                  Chip(
                    avatar: const Icon(Icons.home_outlined, size: 18),
                    label: Text(typeLabel(group.type)),
                  ),
                  Chip(
                    avatar: const Icon(Icons.person_outline, size: 18),
                    label: Text(roleLabel(group.role)),
                  ),
                ],
              ),
            ],
            const SizedBox(height: AppSpacing.xl),
            Text('Modüller', style: theme.textTheme.titleMedium),
            const SizedBox(height: AppSpacing.md),
            for (final module in modules) ...[
              ModuleCard(module: module),
              const SizedBox(height: AppSpacing.md),
            ],
          ],
        ),
      ),
    );
  }
}
