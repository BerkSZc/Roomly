import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';

import 'core/router/app_router.dart';

class RoomlyApp extends ConsumerWidget {
  const RoomlyApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return MaterialApp.router(
      title: 'Roomly',
      debugShowCheckedModeBanner: false,
      // Tema, renk ve tipografi "Design system & UI foundation" ile gelir.
      theme: ThemeData(colorSchemeSeed: Colors.teal),
      routerConfig: ref.watch(routerProvider),
    );
  }
}
