import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import '../../../core/modules/app_module.dart';

/// Alt barlı kabuk: Ana Sayfa, alt bar modülleri, Diğer (spec 0005 AC-2).
class DashboardShell extends StatelessWidget {
  const DashboardShell({
    super.key,
    required this.navigationShell,
    required this.tabModules,
  });

  final StatefulNavigationShell navigationShell;
  final List<AppModule> tabModules;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: navigationShell.currentIndex,
        onDestinationSelected: (index) => navigationShell.goBranch(
          index,
          initialLocation: index == navigationShell.currentIndex,
        ),
        destinations: [
          const NavigationDestination(
            icon: Icon(Icons.dashboard_outlined),
            selectedIcon: Icon(Icons.dashboard),
            label: 'Ana Sayfa',
          ),
          for (final module in tabModules)
            NavigationDestination(icon: Icon(module.icon), label: module.title),
          const NavigationDestination(
            icon: Icon(Icons.more_horiz),
            label: 'Diğer',
          ),
        ],
      ),
    );
  }
}
