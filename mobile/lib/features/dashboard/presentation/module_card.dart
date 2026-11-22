import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/modules/app_module.dart';
import '../../../core/theme/app_tokens.dart';

/// Ana Sayfa'da bir modülün kartı. Modül kendi kartını verdiyse o, yoksa varsayılan kart.
/// Karta dokununca modülün rotasına gidilir (spec 0005 AC-4).
class ModuleCard extends StatelessWidget {
  const ModuleCard({super.key, required this.module});

  final AppModule module;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final custom = module.dashboardCardBuilder;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.go(module.path),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: custom != null
              ? custom(context)
              : Row(
                  children: [
                    Icon(
                      module.icon,
                      size: 32,
                      color: theme.colorScheme.primary,
                    ),
                    const SizedBox(width: AppSpacing.lg),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            module.title,
                            style: theme.textTheme.titleMedium,
                          ),
                          const SizedBox(height: AppSpacing.xs),
                          Text(
                            'Geliştirme aşamasında · ${module.ucCode} · ${module.owner}',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                    const Icon(Icons.chevron_right),
                  ],
                ),
        ),
      ),
    );
  }
}
