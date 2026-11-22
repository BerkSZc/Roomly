import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC5 · Borç görüntüleme ve hesaplaşma · Sorumlu: Mehmet.
///
/// Placeholder'ı gerçek ekranla değiştirmek için yalnızca bu klasörde çalış:
/// ekranını `presentation/` altına yaz ve `screenBuilder: (context) => const XScreen()` ver.
/// İstersen `dashboardCardBuilder` ile Ana Sayfa kartını, `routes` ile alt rotalarını ekle.
/// Ayrıntı: `lib/features/README.md`.
const debtsModule = AppModule(
  id: 'debts',
  title: 'Borçlar',
  ucCode: 'UC5',
  owner: 'Mehmet',
  icon: Icons.account_balance_wallet_outlined,
  placement: ModulePlacement.tab,
);
