import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC4 · Gider ekleme ve bölüşme · Sorumlu: Eda.
///
/// Placeholder'ı gerçek ekranla değiştirmek için yalnızca bu klasörde çalış:
/// ekranını `presentation/` altına yaz ve `screenBuilder: (context) => const XScreen()` ver.
/// İstersen `dashboardCardBuilder` ile Ana Sayfa kartını, `routes` ile alt rotalarını ekle.
/// Ayrıntı: `lib/features/README.md`.
const expensesModule = AppModule(
  id: 'expenses',
  title: 'Giderler',
  ucCode: 'UC4',
  owner: 'Eda',
  icon: Icons.receipt_long_outlined,
  placement: ModulePlacement.tab,
);
