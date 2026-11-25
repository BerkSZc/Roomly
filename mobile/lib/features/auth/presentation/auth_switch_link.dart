import 'package:material_ui/material_ui.dart';

/// "Hesabın yok mu? Kayıt ol" biçimli, ekranlar arası geçiş bağlantısı.
/// TextButton olduğu için 48 dp dokunma alanı ve düğme semantiği taşır.
class AuthSwitchLink extends StatelessWidget {
  const AuthSwitchLink({
    super.key,
    required this.question,
    required this.action,
    required this.onPressed,
  });

  final String question;
  final String action;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: TextButton(
        onPressed: onPressed,
        child: Text.rich(
          TextSpan(
            style: const TextStyle(fontWeight: FontWeight.w700),
            children: [
              TextSpan(text: '$question '),
              TextSpan(
                text: action,
                style: const TextStyle(fontWeight: FontWeight.w800),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
