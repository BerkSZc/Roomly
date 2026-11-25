import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/core/modules/app_module.dart';
import 'package:roomly/core/modules/module_registry.dart';

AppModule _m(String id, ModulePlacement p) => AppModule(
  id: id,
  title: id,
  ucCode: 'UC0',
  owner: 'X',
  icon: Icons.circle,
  placement: p,
);

void main() {
  test('registry 8 modülü menü sırasıyla ve doğru yollarla tutar DASH-6', () {
    expect(appModules.map((m) => '${m.ucCode}:${m.path}').toList(), [
      'UC4:/expenses',
      'UC5:/debts',
      'UC7:/chores',
      'UC6:/more/bills',
      'UC8:/more/shopping',
      'UC9:/more/rules',
      'UC2:/more/space',
      'UC10:/more/leave',
    ]);
    expect(appModules.map((m) => m.owner).toSet(), {
      'Eda',
      'Mehmet',
      'Öykü',
      'Yusuf',
      'Başak',
      'Berat',
    });
    expect(appModules.where((m) => !m.isPlaceholder).map((m) => m.ucCode), [
      'UC2',
      'UC10',
    ]);
  });

  test('gerçek registry kurallara uyar DASH-7', () {
    expect(validateModules(appModules), isNull);
  });

  test('tekrarlı id, geçersiz id ve yanlış sekme sayısı reddedilir DASH-7', () {
    const tab = ModulePlacement.tab;
    const more = ModulePlacement.more;
    expect(
      validateModules([_m('a', tab), _m('a', tab), _m('b', tab)]),
      contains('benzersiz'),
    );
    expect(
      validateModules([_m('A', tab), _m('b', tab), _m('c', tab)]),
      contains('Geçersiz'),
    );
    expect(
      validateModules([_m('a', tab), _m('b', tab), _m('c', more)]),
      contains('Alt barda'),
    );
  });
}
