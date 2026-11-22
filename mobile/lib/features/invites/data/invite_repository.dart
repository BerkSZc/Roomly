import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/session.dart';
import '../../../core/config/app_config.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/network/dio_provider.dart';
import '../../auth/data/auth_repository.dart';
import '../presentation/invite_code_format.dart';
import '../presentation/join_error_messages.dart';

/// Davet koduyla katılım (`POST /api/groups/join`, spec 0006). Hatalar [ApiException].
abstract interface class InviteRepository {
  Future<GroupSummary> join(String code);
}

class RemoteInviteRepository implements InviteRepository {
  RemoteInviteRepository(this._dio);

  final Dio _dio;

  @override
  Future<GroupSummary> join(String code) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/api/groups/join',
        data: {'code': code},
      );
      return GroupSummary.fromJson(
        response.data!['group'] as Map<String, dynamic>,
      );
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

/// Mock katılım (AC-13). Başarıda mock auth deposundaki oturumdaki kullanıcıya grubu yazar.
class MockInviteRepository implements InviteRepository {
  MockInviteRepository(this._auth);

  static const demoCode = 'DEMOROOM2026';

  static const _failures = {
    'DOLUGRUP2026': (code: 'CAPACITY_EXCEEDED', status: 409),
    'ESKIKOD2026': (code: 'INVITE_EXPIRED', status: 410),
    'IPTALKOD2026': (code: 'INVITE_REVOKED', status: 410),
  };

  final AuthRepository _auth;

  @override
  Future<GroupSummary> join(String code) async {
    await Future<void>.delayed(const Duration(milliseconds: 300));
    final normalized = InviteCodeFormat.normalize(code);
    if (normalized == demoCode) {
      const group = GroupSummary(
        id: 'mock-group-1',
        name: 'Demo Ev',
        type: 'Ev',
        role: 'Member',
      );
      final auth = _auth;
      if (auth is MockAuthRepository) {
        await auth.assignGroupToCurrentUser(group);
      }
      return group;
    }
    final failure =
        _failures[normalized] ?? (code: 'INVITE_INVALID', status: 404);
    throw ApiException(
      code: failure.code,
      message: joinErrorMessages[failure.code]!,
      status: failure.status,
    );
  }
}

final inviteRepositoryProvider = Provider<InviteRepository>((ref) {
  final config = ref.watch(appConfigProvider);
  return config.useMock
      ? MockInviteRepository(ref.watch(authRepositoryProvider))
      : RemoteInviteRepository(ref.watch(dioProvider));
});
