import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/auth/session.dart';
import '../../../core/config/app_config.dart';
import '../../../core/errors/api_exception.dart';
import '../../../core/network/dio_provider.dart';
import '../../auth/data/auth_repository.dart';

enum SpaceType {
  ev('Ev', 20),
  yurt('Yurt', 6);

  const SpaceType(this.label, this.maxCapacity);
  final String label;
  final int maxCapacity;
  static const minCapacity = 2;

  bool accepts(int? capacity) =>
      capacity != null && capacity >= minCapacity && capacity <= maxCapacity;
}

class SpaceDetails {
  const SpaceDetails(this.group, this.capacity, this.memberCount);

  factory SpaceDetails.fromJson(Map<String, dynamic> json) => SpaceDetails(
    GroupSummary.fromJson(json),
    json['capacity'] as int,
    json['memberCount'] as int,
  );

  final GroupSummary group;
  final int capacity;
  final int memberCount;
}

typedef LeaveOutcome = ({bool archived, bool adminTransferred});

abstract interface class SpaceRepository {
  Future<SpaceDetails> create(String name, SpaceType type, int capacity);
  Future<SpaceDetails> get(String groupId);
  Future<LeaveOutcome> leave(String groupId);
}

class RemoteSpaceRepository implements SpaceRepository {
  RemoteSpaceRepository(this._dio);
  final Dio _dio;

  @override
  Future<SpaceDetails> create(String name, SpaceType type, int capacity) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/api/groups',
        data: {'name': name, 'type': type.label, 'capacity': capacity},
      );
      return SpaceDetails.fromJson(response.data!);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  @override
  Future<SpaceDetails> get(String groupId) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/api/groups/$groupId',
      );
      return SpaceDetails.fromJson(response.data!);
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }

  @override
  Future<LeaveOutcome> leave(String groupId) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/api/groups/$groupId/leave',
      );
      final data = response.data!;
      return (
        archived: data['archived'] as bool,
        adminTransferred: data['adminTransferred'] as bool,
      );
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}

class MockSpaceRepository implements SpaceRepository {
  MockSpaceRepository(this._auth);
  final AuthRepository _auth;
  final _spaces = <String, int>{};
  int _nextId = 0;

  @override
  Future<SpaceDetails> create(String name, SpaceType type, int capacity) async {
    if ((await _auth.me()).hasGroup) {
      throw const ApiException(
        code: 'ALREADY_IN_GROUP',
        message: 'Zaten bir yaşam alanına üyesin.',
        status: 409,
      );
    }
    final group = GroupSummary(
      id: 'mock-space-${++_nextId}',
      name: name,
      type: type.label,
      role: 'Admin',
    );
    final auth = _auth;
    if (auth is MockAuthRepository) await auth.assignGroupToCurrentUser(group);
    _spaces[group.id] = capacity;
    return SpaceDetails(group, capacity, 1);
  }

  @override
  Future<SpaceDetails> get(String groupId) async {
    final group = (await _auth.me()).group;
    if (group == null || group.id != groupId) {
      throw const ApiException(
        code: 'SPACE_NOT_FOUND',
        message: 'Yaşam alanı bulunamadı.',
        status: 404,
      );
    }
    return SpaceDetails(group, _spaces[groupId] ?? 4, group.isAdmin ? 1 : 2);
  }

  @override
  Future<LeaveOutcome> leave(String groupId) async {
    final details = await get(groupId);
    final auth = _auth;
    if (auth is MockAuthRepository) await auth.assignGroupToCurrentUser(null);
    final archived = details.memberCount == 1;
    if (archived) _spaces.remove(groupId);
    return (archived: archived, adminTransferred: false);
  }
}

final spaceRepositoryProvider = Provider<SpaceRepository>((ref) {
  return ref.watch(appConfigProvider).useMock
      ? MockSpaceRepository(ref.watch(authRepositoryProvider))
      : RemoteSpaceRepository(ref.watch(dioProvider));
});
