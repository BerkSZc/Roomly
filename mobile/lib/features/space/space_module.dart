import 'package:material_ui/material_ui.dart';

import '../../core/modules/app_module.dart';
import 'presentation/leave_space_screen.dart';
import 'presentation/space_details_screen.dart';

Widget _spaceScreen(BuildContext context) => const SpaceDetailsScreen();
Widget _leaveScreen(BuildContext context) => const LeaveSpaceScreen();
Widget _spaceCard(BuildContext context) => _moduleCard(spaceModule);
Widget _leaveCard(BuildContext context) => _moduleCard(leaveModule);

Widget _moduleCard(AppModule module) => ListTile(
  contentPadding: EdgeInsets.zero,
  leading: Icon(module.icon),
  title: Text(module.title),
  trailing: const Icon(Icons.chevron_right),
);

/// UC2 · Yaşam alanı bilgileri · Sorumlu: Berat.
const spaceModule = AppModule(
  id: 'space',
  title: 'Alan Bilgileri',
  ucCode: 'UC2',
  owner: 'Berat',
  icon: Icons.home_outlined,
  placement: ModulePlacement.more,
  screenBuilder: _spaceScreen,
  dashboardCardBuilder: _spaceCard,
);

/// UC10 · Alandan ayrılma ve alanı kapatma · Sorumlu: Berat.
const leaveModule = AppModule(
  id: 'leave',
  title: 'Alandan Ayrıl',
  ucCode: 'UC10',
  owner: 'Berat',
  icon: Icons.logout_outlined,
  placement: ModulePlacement.more,
  screenBuilder: _leaveScreen,
  dashboardCardBuilder: _leaveCard,
);
