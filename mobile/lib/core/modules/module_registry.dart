import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/bills/bills_module.dart';
import '../../features/chores/chores_module.dart';
import '../../features/debts/debts_module.dart';
import '../../features/expenses/expenses_module.dart';
import '../../features/rules/rules_module.dart';
import '../../features/shopping/shopping_module.dart';
import '../../features/space/space_module.dart';
import 'app_module.dart';

/// Uygulamadaki bütün ekip modülleri, menü sırasıyla (spec 0005).
/// Ortak dosya: yalnızca yeni bir modül eklerken ya da yerini değiştirirken düzenlenir.
/// Modülün ekranı ve kartı kendi `<modul>_module.dart` dosyasında değişir.
final appModules = <AppModule>[
  expensesModule, // UC4 · Eda
  debtsModule, // UC5 · Mehmet
  choresModule, // UC7 · Öykü
  billsModule, // UC6 · Yusuf
  shoppingModule, // UC8 · Başak
  rulesModule, // UC9 · Başak
  spaceModule, // UC2 · Berat
  leaveModule, // UC10 · Berat
];

/// Alt bardaki modül sayısı (Ana Sayfa ve Diğer ile birlikte 5 sekme).
const tabModuleCount = 3;

final _validId = RegExp(r'^[a-z][a-z0-9-]*$');

/// Registry kurallarına uymayan listede hata mesajı, uyan listede `null` (AC-7).
String? validateModules(List<AppModule> modules) {
  final ids = modules.map((m) => m.id).toList();
  if (ids.toSet().length != ids.length) {
    return 'Modül id\'leri benzersiz olmalı.';
  }
  final bad = ids.where((id) => !_validId.hasMatch(id));
  if (bad.isNotEmpty) return 'Geçersiz modül id: ${bad.join(', ')}';
  final tabs = modules.where((m) => m.placement == ModulePlacement.tab).length;
  if (tabs != tabModuleCount) {
    return 'Alt barda tam $tabModuleCount modül olmalı, $tabs var.';
  }
  return null;
}

/// Testlerde `appModulesProvider.overrideWithValue([...])`.
final appModulesProvider = Provider<List<AppModule>>((ref) {
  assert(validateModules(appModules) == null, validateModules(appModules));
  return appModules;
});
