import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';

/// UC2 · Yaşam alanı bilgileri · Sorumlu: Berat.
/// Placeholder'ı değiştirmek için `screenBuilder` ver (bkz. `lib/features/README.md`).
const spaceModule = AppModule(
  id: 'space',
  title: 'Alan Bilgileri',
  ucCode: 'UC2',
  owner: 'Berat',
  icon: Icons.home_outlined,
  placement: ModulePlacement.more,
);

/// UC10 · Alandan ayrılma ve alanı kapatma · Sorumlu: Berat.
const leaveModule = AppModule(
  id: 'leave',
  title: 'Alandan Ayrıl',
  ucCode: 'UC10',
  owner: 'Berat',
  icon: Icons.logout_outlined,
  placement: ModulePlacement.more,
);
