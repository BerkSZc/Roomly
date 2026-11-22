import 'package:flutter_test/flutter_test.dart';
import 'package:roomly/core/auth/session.dart';
import 'package:roomly/core/router/session_redirect.dart';

const _noGroup = CurrentUser(id: '1', displayName: 'A', email: 'a@b.co');
const _withGroup = CurrentUser(
  id: '2',
  displayName: 'B',
  email: 'b@b.co',
  group: GroupSummary(id: 'g', name: 'Ev', type: 'Ev', role: 'Member'),
);

String? _go(SessionState s, String location) =>
    SessionRedirect.redirect(s, location);

void main() {
  test('oturum bilinmiyorken ya da geri yükleme başarısızken splash gösterilir AUTH-16', () {
    expect(_go(const SessionUnknown(), '/home'), '/splash');
    expect(_go(const SessionUnknown(), '/splash'), isNull);
    expect(_go(const SessionRestoreFailed('x'), '/login'), '/splash');
  });

  test('oturum yokken korumalı rotalar girişe yönlenir AUTH-16', () {
    const s = SessionUnauthenticated();
    expect(_go(s, '/home'), '/login');
    expect(_go(s, '/onboarding'), '/login');
    expect(_go(s, '/splash'), '/login');
    expect(_go(s, '/login'), isNull);
    expect(_go(s, '/register'), isNull);
  });

  test(
    'grubu olmayan kullanıcı onboarding ve katılım alanında kalır AUTH-16',
    () {
      const s = SessionAuthenticated(_noGroup);
      expect(_go(s, '/login'), '/onboarding');
      expect(_go(s, '/splash'), '/onboarding');
      expect(_go(s, '/home'), '/onboarding');
      expect(_go(s, '/onboarding'), isNull);
      expect(_go(s, '/onboarding/create-group'), isNull);
      expect(_go(s, '/join'), isNull);
    },
  );

  test('grubu olan kullanıcı ana ekrana gider, onboarding ve katılıma giremez AUTH-16', () {
    const s = SessionAuthenticated(_withGroup);
    expect(_go(s, '/login'), '/home');
    expect(_go(s, '/register'), '/home');
    expect(_go(s, '/splash'), '/home');
    expect(_go(s, '/onboarding'), '/home');
    expect(_go(s, '/join'), '/home');
    expect(_go(s, '/home'), isNull);
  });

  test('sistem durumu ekranı her durumda açılır AUTH-16', () {
    expect(_go(const SessionUnauthenticated(), '/system'), isNull);
    expect(_go(const SessionUnknown(), '/system'), isNull);
  });
}
