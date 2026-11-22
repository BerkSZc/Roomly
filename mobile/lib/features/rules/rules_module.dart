import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC9 · Kural önerme ve oylama · Sorumlu: Başak.
///
/// Placeholder'ı gerçek ekranla değiştirmek için yalnızca bu klasörde çalış:
/// ekranını `presentation/` altına yaz ve `screenBuilder: (context) => const XScreen()` ver.
/// İstersen `dashboardCardBuilder` ile Ana Sayfa kartını, `routes` ile alt rotalarını ekle.
/// Ayrıntı: `lib/features/README.md`.
const rulesModule = AppModule(
  id: 'rules',
  title: 'Kurallar',
  ucCode: 'UC9',
  owner: 'Başak',
  icon: Icons.gavel_outlined,
  placement: ModulePlacement.more,
);
