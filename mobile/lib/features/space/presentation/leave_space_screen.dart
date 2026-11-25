import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_buttons.dart';
import '../../../core/widgets/app_snack_bar.dart';
import '../../auth/application/session_controller.dart';
import '../data/space_repository.dart';

class LeaveSpaceScreen extends ConsumerStatefulWidget {
  const LeaveSpaceScreen({super.key});

  @override
  ConsumerState<LeaveSpaceScreen> createState() => _LeaveSpaceScreenState();
}

class _LeaveSpaceScreenState extends ConsumerState<LeaveSpaceScreen> {
  bool _submitting = false;

  Future<void> _leave(GroupSummary group) async {
    if (_submitting) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Ayrılmayı onayla'),
        content: Text('${group.name} alanındaki üyeliğin sona erecek.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Ayrıl'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted || _submitting) return;
    setState(() => _submitting = true);
    final session = ref.read(sessionControllerProvider.notifier);
    try {
      final result = await ref.read(spaceRepositoryProvider).leave(group.id);
      if (mounted) {
        final message = switch (result) {
          (archived: true, adminTransferred: _) =>
            'Alandan ayrıldın ve alan arşivlendi.',
          (archived: false, adminTransferred: true) =>
            'Alandan ayrıldın. Yöneticilik devredildi.',
          _ => 'Alandan ayrıldın.',
        };
        showAppSnackBar(context, message);
      }
      await session.restore();
    } on ApiException catch (error) {
      if (mounted) showAppSnackBar(context, error.message, isError: true);
      if (error.code == 'SPACE_NOT_FOUND') await session.restore();
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final session = ref.watch(sessionControllerProvider);
    final group = session is SessionAuthenticated ? session.user.group : null;
    if (group == null) return const SizedBox.shrink();
    return Scaffold(
      appBar: AppBar(title: const Text('Alandan Ayrıl')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: [
            Text(group.name, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: AppSpacing.md),
            const Text('Ayrıldığında bu alanın verilerine erişimin sona erer.'),
            if (group.isAdmin) ...[
              const SizedBox(height: AppSpacing.sm),
              const Text(
                'Başka üye varsa yöneticilik en eski kalan üyeye geçer.',
              ),
            ],
            const SizedBox(height: AppSpacing.sm),
            const Text(
              'Son üye ayrıldığında alan arşivlenir ve davet kodları geçersiz olur.',
            ),
            const SizedBox(height: AppSpacing.xl),
            PrimaryButton(
              label: 'Ayrılmayı Başlat',
              isLoading: _submitting,
              onPressed: () => _leave(group),
            ),
          ],
        ),
      ),
    );
  }
}
