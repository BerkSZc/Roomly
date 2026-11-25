import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/theme/app_tokens.dart';
import '../../../core/widgets/app_buttons.dart';
import '../../auth/application/session_controller.dart';
import '../application/space_details_provider.dart';

class SpaceDetailsScreen extends ConsumerWidget {
  const SpaceDetailsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    if (session is! SessionAuthenticated || session.user.group == null) {
      return const SizedBox.shrink();
    }
    final provider = spaceDetailsProvider((
      groupId: session.user.group!.id,
      userId: session.user.id,
    ));
    return Scaffold(
      appBar: AppBar(title: const Text('Alan Bilgileri')),
      body: SafeArea(
        child: ref
            .watch(provider)
            .when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ListView(
                padding: const EdgeInsets.all(AppSpacing.xl),
                children: [
                  Text(
                    error is ApiException
                        ? error.message
                        : 'Alan bilgileri alınamadı.',
                  ),
                  const SizedBox(height: AppSpacing.md),
                  SecondaryButton(
                    label: 'Tekrar Dene',
                    onPressed: () => ref.invalidate(provider),
                  ),
                ],
              ),
              data: (space) => ListView(
                padding: const EdgeInsets.all(AppSpacing.xl),
                children: [
                  Text(
                    space.group.name,
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: AppSpacing.md),
                  ListTile(
                    leading: const Icon(Icons.home_outlined),
                    title: const Text('Alan türü'),
                    subtitle: Text(space.group.type),
                  ),
                  ListTile(
                    leading: const Icon(Icons.people_outline),
                    title: const Text('Doluluk'),
                    subtitle: Text(
                      '${space.memberCount} / ${space.capacity} kişi',
                    ),
                  ),
                  ListTile(
                    leading: const Icon(Icons.person_outline),
                    title: const Text('Rolün'),
                    subtitle: Text(space.group.isAdmin ? 'Yönetici' : 'Üye'),
                  ),
                ],
              ),
            ),
      ),
    );
  }
}
