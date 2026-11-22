import 'package:material_ui/material_ui.dart';

import '../theme/app_tokens.dart';

/// Henüz yapılmamış bir modülün yer tutucu ekranı.
/// Modül sahibi kendi ekranını yazınca bunun yerine kendi ekranını kaydeder.
class UnderDevelopmentScreen extends StatelessWidget {
  const UnderDevelopmentScreen({
    super.key,
    required this.title,
    required this.ucCode,
    required this.owner,
    required this.icon,
    this.showAppBar = true,
  });

  final String title;
  final String ucCode;
  final String owner;
  final IconData icon;

  /// Kendi AppBar'ı olan bir kabuğun içinde gösterilirken `false`.
  final bool showAppBar;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: showAppBar ? AppBar(title: Text(title)) : null,
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.xl),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 64, color: theme.colorScheme.primary),
              const SizedBox(height: AppSpacing.xl),
              Text(
                'Geliştirme Aşamasında',
                style: theme.textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(
                '$ucCode · Sorumlu: $owner',
                style: theme.textTheme.titleSmall?.copyWith(
                  color: theme.colorScheme.primary,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.md),
              Text(
                'Bu modül yakında burada olacak.',
                style: theme.textTheme.bodyMedium,
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
