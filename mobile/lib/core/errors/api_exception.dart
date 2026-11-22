import 'package:dio/dio.dart';

/// Sunucudan (`ProblemDetails` + `code`) ya da ağdan gelen hata, kullanıcıya gösterilecek Türkçe mesajla.
class ApiException implements Exception {
  const ApiException({
    required this.code,
    required this.message,
    this.status,
    this.fieldErrors = const {},
  });

  /// Bağlantı kurulamadı, zaman aşımı vb.
  static const network = 'NETWORK';
  static const unknown = 'UNKNOWN';

  final String code;
  final String message;
  final int? status;

  /// Alan adı (camelCase, istek gövdesiyle aynı) → ilk hata mesajı.
  final Map<String, String> fieldErrors;

  bool get isNetwork => code == network;

  factory ApiException.fromDio(DioException error) {
    final response = error.response;
    if (response == null) {
      return const ApiException(
        code: network,
        message: 'Sunucuya ulaşılamadı. Bağlantını kontrol et.',
      );
    }

    final data = response.data;
    if (data is Map<String, dynamic>) {
      final code = data['code'] as String? ?? unknown;
      final errors = <String, String>{};
      final raw = data['errors'];
      if (raw is Map<String, dynamic>) {
        raw.forEach((key, value) {
          if (value is List && value.isNotEmpty) errors[key] = '${value.first}';
        });
      }
      return ApiException(
        code: code,
        message:
            apiErrorMessages[code] ??
            data['title'] as String? ??
            _genericMessage,
        status: response.statusCode,
        fieldErrors: errors,
      );
    }

    return ApiException(
      code: unknown,
      message: _genericMessage,
      status: response.statusCode,
    );
  }

  static const _genericMessage = 'Beklenmeyen bir hata oluştu. Tekrar dene.';

  @override
  String toString() => 'ApiException($code, $status)';
}

/// İstemcinin bildiği kodların mesajları. Modüller kendi kodlarını kendi ekranlarında eşleyebilir;
/// burada olmayan kodlarda sunucunun Türkçe `title` alanı gösterilir.
const apiErrorMessages = <String, String>{
  'INVALID_CREDENTIALS': 'E-posta ya da şifre hatalı.',
  'ACCOUNT_LOCKED':
      'Çok fazla hatalı deneme yaptın. Hesabın 15 dakika kilitlendi.',
  'EMAIL_TAKEN': 'Bu e-posta adresiyle kayıtlı bir hesap var.',
  'RATE_LIMITED': 'Çok fazla istek gönderdin. Biraz bekleyip tekrar dene.',
  'INVALID_REFRESH_TOKEN': 'Oturumunun süresi doldu. Tekrar giriş yap.',
  'UNAUTHORIZED': 'Oturumunun süresi doldu. Tekrar giriş yap.',
  'INTERNAL_ERROR': 'Sunucuda bir hata oluştu. Biraz sonra tekrar dene.',
};
