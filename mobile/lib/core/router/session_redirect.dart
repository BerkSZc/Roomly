import '../auth/session.dart';

/// Oturuma göre yönlendirme kuralları (spec 0004 AC-16). Saf fonksiyon, ayrıca test edilir.
/// `null` = olduğun yerde kal.
abstract final class SessionRedirect {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const onboarding = '/onboarding';
  static const join = '/join';
  static const home = '/home';
  static const system = '/system';

  static const _publicRoutes = {login, register};

  /// Grubu olmayan kullanıcının girebildiği rotalar (ve alt rotaları).
  static const _noGroupRoutes = {onboarding, join};

  static String? redirect(SessionState session, String location) {
    if (location == system) return null;

    return switch (session) {
      SessionUnknown() ||
      SessionRestoreFailed() => location == splash ? null : splash,
      SessionUnauthenticated() =>
        _publicRoutes.contains(location) ? null : login,
      SessionAuthenticated(:final user) => _authenticated(user, location),
    };
  }

  static String? _authenticated(CurrentUser user, String location) {
    final entryRoute = location == splash || _publicRoutes.contains(location);
    final inNoGroupArea = _noGroupRoutes.any(
      (route) => location == route || location.startsWith('$route/'),
    );

    if (!user.hasGroup) {
      return inNoGroupArea ? null : onboarding;
    }
    return entryRoute || inNoGroupArea ? home : null;
  }
}
