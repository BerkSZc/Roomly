import 'package:material_ui/material_ui.dart';

/// Varsa açık snackbar'ı kapatıp yenisini gösterir.
void showAppSnackBar(
  BuildContext context,
  String message, {
  bool isError = false,
}) {
  final scheme = Theme.of(context).colorScheme;
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(
        content: Text(
          message,
          style: isError ? TextStyle(color: scheme.onError) : null,
        ),
        backgroundColor: isError ? scheme.error : null,
      ),
    );
}
