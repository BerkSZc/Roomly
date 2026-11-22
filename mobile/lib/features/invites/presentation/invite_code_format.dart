/// Davet kodu normalizasyonu ve biçimi; backend `InviteCodeHasher.Normalize` ve
/// `InviteCodeFormat` ile aynı (spec 0006 AC-2).
abstract final class InviteCodeFormat {
  static final _remove = RegExp(r'[\s \-–]');
  static final _pattern = RegExp(r'^[A-Z0-9]{8,32}$');

  static String normalize(String code) =>
      code.replaceAll(_remove, '').toUpperCase();

  static bool isWellFormed(String code) => _pattern.hasMatch(normalize(code));

  static String? validate(String? value) {
    if (value == null || value.trim().isEmpty) return 'Davet kodu zorunludur.';
    return isWellFormed(value) ? null : 'Davet kodu geçersiz görünüyor.';
  }
}
