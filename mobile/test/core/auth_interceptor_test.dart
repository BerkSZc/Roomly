import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:roomly/core/network/auth_interceptor.dart';
import 'package:roomly/core/storage/token_storage.dart';

import '../support/fakes.dart';

class _Harness {
  _Harness({
    required Future<FakeResponse> Function(RequestOptions) api,
    required Future<FakeResponse> Function(RequestOptions) refresh,
  }) {
    dio.httpClientAdapter = apiAdapter = FakeAdapter(api);
    refreshDio.httpClientAdapter = refreshAdapter = FakeAdapter(refresh);
    dio.interceptors.add(
      AuthInterceptor(
        retryClient: dio,
        refreshClient: refreshDio,
        storage: storage,
        onSessionExpired: () => expiredCount++,
      ),
    );
  }

  final dio = Dio(BaseOptions(baseUrl: 'http://api'));
  final refreshDio = Dio(BaseOptions(baseUrl: 'http://api'));
  final storage = InMemoryTokenStorage(
    const StoredTokens(accessToken: 'old-access', refreshToken: 'old-refresh'),
  );
  late final FakeAdapter apiAdapter;
  late final FakeAdapter refreshAdapter;
  int expiredCount = 0;
}

Future<FakeResponse> _okWhenNewToken(RequestOptions o) async =>
    o.headers['Authorization'] == 'Bearer new-access'
    ? const FakeResponse(200, {'ok': true})
    : const FakeResponse(401, {'code': 'UNAUTHORIZED'});

Future<FakeResponse> _refreshOk(RequestOptions o) async {
  await Future<void>.delayed(const Duration(milliseconds: 20));
  return const FakeResponse(200, {
    'accessToken': 'new-access',
    'refreshToken': 'new-refresh',
  });
}

void main() {
  test(
    'korumalı isteğe bearer eklenir, giriş isteğine eklenmez AUTH-15',
    () async {
      final h = _Harness(
        api: (_) async => const FakeResponse(200),
        refresh: _refreshOk,
      );

      await h.dio.get<void>('/api/auth/me');
      await h.dio.post<void>('/api/auth/login', data: {});

      expect(
        h.apiAdapter.requests[0].headers['Authorization'],
        'Bearer old-access',
      );
      expect(
        h.apiAdapter.requests[1].headers.containsKey('Authorization'),
        isFalse,
      );
    },
  );

  test(
    '401 alınca token yenilenir ve istek bir kez tekrarlanır AUTH-15',
    () async {
      final h = _Harness(api: _okWhenNewToken, refresh: _refreshOk);

      final response = await h.dio.get<Map<String, dynamic>>('/api/x');

      expect(response.data, {'ok': true});
      expect(h.refreshAdapter.requests.single.data, {
        'refreshToken': 'old-refresh',
      });
      final saved = await h.storage.read();
      expect(saved!.accessToken, 'new-access');
      expect(saved.refreshToken, 'new-refresh');
      expect(h.apiAdapter.requests, hasLength(2));
    },
  );

  test('aynı anda gelen 401 lerde tek refresh yapılır AUTH-15', () async {
    final h = _Harness(api: _okWhenNewToken, refresh: _refreshOk);

    await Future.wait([
      h.dio.get<void>('/api/a'),
      h.dio.get<void>('/api/b'),
      h.dio.get<void>('/api/c'),
    ]);

    expect(h.refreshAdapter.requests, hasLength(1));
  });

  test(
    'refresh reddedilirse token lar silinir ve oturum düşer AUTH-15',
    () async {
      final h = _Harness(
        api: (_) async => const FakeResponse(401, {'code': 'UNAUTHORIZED'}),
        refresh: (_) async =>
            const FakeResponse(401, {'code': 'INVALID_REFRESH_TOKEN'}),
      );

      await expectLater(
        h.dio.get<void>('/api/x'),
        throwsA(isA<DioException>()),
      );

      expect(await h.storage.read(), isNull);
      expect(h.expiredCount, 1);
    },
  );

  test('tekrarlanan istek yine 401 alırsa döngüye girmez AUTH-15', () async {
    final h = _Harness(
      api: (_) async => const FakeResponse(401, {'code': 'UNAUTHORIZED'}),
      refresh: _refreshOk,
    );

    await expectLater(h.dio.get<void>('/api/x'), throwsA(isA<DioException>()));

    expect(h.refreshAdapter.requests, hasLength(1));
    expect(h.apiAdapter.requests, hasLength(2));
  });
}
