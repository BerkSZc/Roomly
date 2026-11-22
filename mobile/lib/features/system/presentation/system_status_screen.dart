import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/config/app_config.dart';
import '../application/health_status_provider.dart';

/// İskeletin boş açılış ekranı: hangi ortamda çalıştığını ve sunucuya
/// ulaşılıp ulaşılamadığını gösterir. Giriş akışı (UC1) gelince yerini o alır.
class SystemStatusScreen extends ConsumerWidget {
  const SystemStatusScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final config = ref.watch(appConfigProvider);
    final health = ref.watch(healthStatusProvider);
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('Roomly', style: textTheme.headlineLarge),
                const SizedBox(height: 24),
                Text(config.useMock ? 'Mod: Mock veri' : 'Mod: Gerçek API'),
                if (!config.useMock) Text('Adres: ${config.apiBaseUrl}'),
                const SizedBox(height: 16),
                health.when(
                  loading: () => const Text('Sunucu kontrol ediliyor...'),
                  data: (status) => Text(
                    status.isHealthy
                        ? 'Sunucu hazır'
                        : 'Sunucu çalışıyor, veritabanına ulaşamıyor',
                  ),
                  error: (error, _) => const Text('Sunucuya ulaşılamıyor'),
                ),
                const SizedBox(height: 16),
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
