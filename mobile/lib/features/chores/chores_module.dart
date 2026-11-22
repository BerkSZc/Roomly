import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC7 · Görev nöbeti yönetimi · Sorumlu: Öykü.
///
/// Placeholder'ı gerçek ekranla değiştirmek için yalnızca bu klasörde çalış:
/// ekranını `presentation/` altına yaz ve `screenBuilder: (context) => const XScreen()` ver.
/// İstersen `dashboardCardBuilder` ile Ana Sayfa kartını, `routes` ile alt rotalarını ekle.
/// Ayrıntı: `lib/features/README.md`.
const choresModule = AppModule(
  id: 'chores',
  title: 'Görevler',
  ucCode: 'UC7',
  owner: 'Öykü',
  icon: Icons.cleaning_services_outlined,
  placement: ModulePlacement.tab,
);
