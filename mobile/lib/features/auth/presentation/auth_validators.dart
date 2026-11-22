/// İstemci tarafı doğrulama; kurallar backend `AuthRules` ile aynı (spec 0004 AC-2).
abstract final class AuthValidators {
  static final _email = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  static String? displayName(String? value) {
    final name = value?.trim() ?? '';
    if (name.isEmpty) return 'Ad zorunludur.';
    if (name.length < 2 || name.length > 50) {
      return 'Ad 2 ile 50 karakter olmalıdır.';
    }
    return null;
  }

  static String? email(String? value) {
    final email = value?.trim() ?? '';
    if (email.isEmpty) return 'E-posta zorunludur.';
    if (email.length > 256 || !_email.hasMatch(email)) {
      return 'Geçerli bir e-posta adresi girin.';
    }
    return null;
  }

  /// Girişte yalnızca boş olmaması aranır.
  static String? loginPassword(String? value) =>
      (value == null || value.isEmpty) ? 'Şifre zorunludur.' : null;

  static String? newPassword(String? value) {
    final password = value ?? '';
    if (password.isEmpty) return 'Şifre zorunludur.';
    if (password.length < 8 || password.length > 128) {
      return 'Şifre en az 8 karakter olmalıdır.';
    }
    if (!password.contains(RegExp(r'\p{L}', unicode: true))) {
      return 'Şifre en az bir harf içermelidir.';
    }
    if (!password.contains(RegExp(r'\d'))) {
      return 'Şifre en az bir rakam içermelidir.';
    }
    return null;
  }
}
