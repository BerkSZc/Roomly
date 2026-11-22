import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Kullanıcının açık grubu (`GET /api/auth/me` → `group`).
class GroupSummary {
  const GroupSummary({
    required this.id,
    required this.name,
    required this.type,
    required this.role,
  });

  factory GroupSummary.fromJson(Map<String, dynamic> json) => GroupSummary(
    id: json['id'] as String,
    name: json['name'] as String,
    type: json['type'] as String,
    role: json['role'] as String,
  );

  final String id;
  final String name;

  /// `Ev` ya da `Yurt`.
  final String type;

  /// `Admin` ya da `Member`.
  final String role;

  bool get isAdmin => role == 'Admin';
}

/// Oturumdaki kullanıcı.
class CurrentUser {
  const CurrentUser({
    required this.id,
    required this.displayName,
    required this.email,
    this.group,
  });

  factory CurrentUser.fromJson(Map<String, dynamic> json) => CurrentUser(
    id: json['id'] as String,
    displayName: json['displayName'] as String,
    email: json['email'] as String,
    group: json['group'] == null
        ? null
        : GroupSummary.fromJson(json['group'] as Map<String, dynamic>),
  );

  final String id;
  final String displayName;
  final String email;
  final GroupSummary? group;

  bool get hasGroup => group != null;
}

/// Router'ın ve ekranların okuduğu oturum durumu.
sealed class SessionState {
  const SessionState();
}

/// Açılışta saklı oturum henüz kontrol edilmedi.
class SessionUnknown extends SessionState {
  const SessionUnknown();
}

/// Açılışta sunucuya ulaşılamadı; token'lar silinmedi, tekrar denenebilir.
class SessionRestoreFailed extends SessionState {
  const SessionRestoreFailed(this.message);

  final String message;
}

class SessionUnauthenticated extends SessionState {
  const SessionUnauthenticated();
}

class SessionAuthenticated extends SessionState {
  const SessionAuthenticated(this.user);

  final CurrentUser user;
}

/// Ağ katmanının "oturum düştü" (refresh başarısız) sinyali. Auth interceptor yayınlar,
/// oturum denetleyicisi dinler. Böylece `core/network` oturum denetleyicisine bağlanmaz.
class SessionEvents {
  final _expired = StreamController<void>.broadcast();

  Stream<void> get expired => _expired.stream;

  void notifyExpired() => _expired.add(null);

  Future<void> dispose() => _expired.close();
}

final sessionEventsProvider = Provider<SessionEvents>((ref) {
  final events = SessionEvents();
  ref.onDispose(events.dispose);
  return events;
});
