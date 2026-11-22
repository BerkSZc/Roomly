import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC6 · Kira, aidat ve fatura yönetimi · Sorumlu: Yusuf.
///
/// Placeholder'ı gerçek ekranla değiştirmek için yalnızca bu klasörde çalış:
/// ekranını `presentation/` altına yaz ve `screenBuilder: (context) => const XScreen()` ver.
/// İstersen `dashboardCardBuilder` ile Ana Sayfa kartını, `routes` ile alt rotalarını ekle.
/// Ayrıntı: `lib/features/README.md`.
const billsModule = AppModule(
  id: 'bills',
  title: 'Faturalar',
  ucCode: 'UC6',
  owner: 'Yusuf',
  icon: Icons.request_quote_outlined,
  placement: ModulePlacement.more,
);
