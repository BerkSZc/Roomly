import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:roomly/app.dart';
import 'package:roomly/core/config/app_config.dart';

void main() {
  testWidgets('mock modunda iskelet açılır ve sunucuyu hazır gösterir', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appConfigProvider.overrideWithValue(
            const AppConfig(apiBaseUrl: 'http://unused', useMock: true),
          ),
        ],
        child: const RoomlyApp(),
      ),
    );

    expect(find.text('Roomly'), findsOneWidget);
    expect(find.text('Mod: Mock veri'), findsOneWidget);

    await tester.pump(const Duration(milliseconds: 400));
    expect(find.text('Sunucu hazır'), findsOneWidget);
  });
}
