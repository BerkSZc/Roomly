import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/auth/session.dart';
import '../../../core/theme/app_tokens.dart';
import '../application/session_controller.dart';

/// Grubu olmayan kullanıcının ilk ekranı: alan kur (UC2) ya da koda katıl (UC3).
class OnboardingScreen extends ConsumerWidget {
  const OnboardingScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final session = ref.watch(sessionControllerProvider);
    final name = session is SessionAuthenticated
        ? session.user.displayName
        : '';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Hoş geldin'),
        actions: [
          IconButton(
            tooltip: 'Çıkış yap',
            icon: const Icon(Icons.logout),
            onPressed: () =>
                ref.read(sessionControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          children: [
            Text('Merhaba $name', style: theme.textTheme.headlineSmall),
            const SizedBox(height: AppSpacing.sm),
            Text(
              'Başlamak için yeni bir yaşam alanı kur ya da ev arkadaşının paylaştığı davet koduyla katıl.',
              style: theme.textTheme.bodyMedium,
            ),
            const SizedBox(height: AppSpacing.xl),
            _ChoiceCard(
              icon: Icons.add_home_outlined,
              title: 'Alan Kur',
              subtitle: 'Ev ya da yurt odası oluştur, arkadaşlarını davet et.',
              onTap: () => context.go('/onboarding/create-group'),
            ),
            const SizedBox(height: AppSpacing.md),
            _ChoiceCard(
              icon: Icons.vpn_key_outlined,
              title: 'Koda Katıl',
              subtitle: 'Sana gönderilen davet koduyla mevcut bir alana katıl.',
              onTap: () => context.go('/join'),
            ),
          ],
        ),
      ),
    );
  }
}

class _ChoiceCard extends StatelessWidget {
  const _ChoiceCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      clipBehavior: Clip.antiAlias,
      child: ListTile(
        contentPadding: const EdgeInsets.all(AppSpacing.lg),
        leading: Icon(icon, size: 32, color: theme.colorScheme.primary),
        title: Text(title, style: theme.textTheme.titleMedium),
        subtitle: Text(subtitle),
        trailing: const Icon(Icons.chevron_right),
        onTap: onTap,
      ),
    );
  }
}
