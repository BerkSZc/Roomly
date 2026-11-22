import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/config/app_config.dart';
import '../../../core/theme/app_tokens.dart';
import '../application/health_status_provider.dart';

/// Ortamı ve sunucuya ulaşılıp ulaşılamadığını gösteren tanı ekranı.
class SystemStatusScreen extends ConsumerWidget {
  const SystemStatusScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final config = ref.watch(appConfigProvider);
    final health = ref.watch(healthStatusProvider);
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      appBar: AppBar(title: const Text('Sistem durumu')),
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(AppSpacing.xl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('Roomly', style: textTheme.headlineLarge),
                const SizedBox(height: AppSpacing.xl),
                Text(config.useMock ? 'Mod: Mock veri' : 'Mod: Gerçek API'),
                if (!config.useMock) Text('Adres: ${config.apiBaseUrl}'),
                const SizedBox(height: AppSpacing.lg),
                health.when(
                  loading: () => const Text('Sunucu kontrol ediliyor...'),
                  data: (status) => Text(
                    status.isHealthy
                        ? 'Sunucu hazır'
                        : 'Sunucu çalışıyor, veritabanına ulaşamıyor',
                  ),
                  error: (error, _) => const Text('Sunucuya ulaşılamıyor'),
                ),
                const SizedBox(height: AppSpacing.lg),
                TextButton(
                  onPressed: () => ref.invalidate(healthStatusProvider),
                  child: const Text('Tekrar dene'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
