import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:material_ui/material_ui.dart';
import 'package:roomly/app.dart';
import 'package:roomly/core/config/app_config.dart';
import 'package:roomly/core/storage/token_storage.dart';

/// Yanıtları sırayla ya da yola göre üreten sahte HTTP katmanı.
class FakeAdapter implements HttpClientAdapter {
  FakeAdapter(this.handler);

  final Future<FakeResponse> Function(RequestOptions options) handler;
  final List<RequestOptions> requests = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    final response = await handler(options);
    return ResponseBody.fromString(
      jsonEncode(response.body),
      response.status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

class FakeResponse {
  const FakeResponse(this.status, [this.body = const <String, dynamic>{}]);

  final int status;
  final Object body;
}

/// Mock modunda, bellekteki token deposuyla bütün uygulama.
Widget mockApp({TokenStorage? storage, List overrides = const []}) =>
    ProviderScope(
      overrides: [
        appConfigProvider.overrideWithValue(
          const AppConfig(apiBaseUrl: 'http://unused', useMock: true),
        ),
        tokenStorageProvider.overrideWithValue(
          storage ?? InMemoryTokenStorage(),
        ),
        ...overrides,
      ],
      child: const RoomlyApp(),
    );
