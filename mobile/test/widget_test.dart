import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';

import 'support/fakes.dart';

void main() {
  testWidgets('mock modunda sistem durumu ekranı sunucuyu hazır gösterir', (
    tester,
  ) async {
    await tester.pumpWidget(mockApp());
    await tester.pumpAndSettle();

    final context = tester.element(find.byType(Scaffold).first);
    GoRouter.of(context).go('/system');
    await tester.pumpAndSettle();

    expect(find.text('Mod: Mock veri'), findsOneWidget);
    expect(find.text('Sunucu hazır'), findsOneWidget);
  });
}
