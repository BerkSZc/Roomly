import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

/// Modülün menüdeki yeri: alt bar sekmesi ya da "Diğer" listesi.
enum ModulePlacement { tab, more }

/// Bir ekip modülünün tanımı (spec 0005). Her modül bunu kendi klasöründeki
/// `<modul>_module.dart` dosyasında verir; placeholder'ı gerçek ekranla değiştirmek için
/// yalnızca [screenBuilder] (ve isterse [dashboardCardBuilder], [routes]) doldurulur.
class AppModule {
  const AppModule({
    required this.id,
    required this.title,
    required this.ucCode,
    required this.owner,
    required this.icon,
    required this.placement,
    this.screenBuilder,
    this.dashboardCardBuilder,
    this.routes = const [],
  });

  /// Rota parçası, benzersiz: `^[a-z][a-z0-9-]*$`.
  final String id;
  final String title;
  final String ucCode;
  final String owner;
  final IconData icon;
  final ModulePlacement placement;

  /// Modülün ana ekranı. `null` ise "Geliştirme Aşamasında" ekranı gösterilir.
  final WidgetBuilder? screenBuilder;

  /// Ana Sayfa kartı. `null` ise varsayılan kart gösterilir.
  final WidgetBuilder? dashboardCardBuilder;

  /// Modülün kendi alt rotaları; [path] altına eklenir (ör. `'new'` → `/expenses/new`).
  final List<RouteBase> routes;

  /// `tab` → `/<id>`, `more` → `/more/<id>`.
  String get path => switch (placement) {
    ModulePlacement.tab => '/$id',
    ModulePlacement.more => '/more/$id',
  };

  bool get isPlaceholder => screenBuilder == null;
}
