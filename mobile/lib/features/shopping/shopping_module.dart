import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC8 · Ortak alışveriş listesi · Sorumlu: Başak.
///
/// Placeholder'ı gerçek ekranla değiştirmek için yalnızca bu klasörde çalış:
/// ekranını `presentation/` altına yaz ve `screenBuilder: (context) => const XScreen()` ver.
/// İstersen `dashboardCardBuilder` ile Ana Sayfa kartını, `routes` ile alt rotalarını ekle.
/// Ayrıntı: `lib/features/README.md`.
const shoppingModule = AppModule(
  id: 'shopping',
  title: 'Alışveriş',
  ucCode: 'UC8',
  owner: 'Başak',
  icon: Icons.shopping_cart_outlined,
  placement: ModulePlacement.more,
);
