import 'dart:convert';

import 'package:http/http.dart' as http;

import 'models.dart';

/// Thrown for anything the API answered with a failure envelope. The message is the API's own,
/// so the UI shows the server's reason ("Only an accessioned sample can be loaded") rather than
/// a generic failure string invented on the client.
class ApiException implements Exception {
  ApiException(this.statusCode, this.message);

  final int statusCode;
  final String message;

  @override
  String toString() => message;
}

class ApiClient {
  ApiClient({required this.baseUrl, http.Client? inner})
      : _http = inner ?? http.Client();

  final String baseUrl;
  final http.Client _http;

  String? _token;

  set token(String? value) => _token = value;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (_token != null) 'Authorization': 'Bearer $_token',
      };

  Future<Map<String, dynamic>> _send(
    String method,
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final uri = Uri.parse('$baseUrl$path');
    final request = http.Request(method, uri)..headers.addAll(_headers);

    if (body != null) request.body = jsonEncode(body);

    final streamed = await _http.send(request);
    final response = await http.Response.fromStream(streamed);

    // The API answers the same envelope for every status including 401 and 403, so there is
    // one parse path here rather than a special case per status code.
    final decoded = response.body.isEmpty
        ? <String, dynamic>{}
        : jsonDecode(response.body) as Map<String, dynamic>;

    if (decoded['isSuccess'] == true) return decoded;

    throw ApiException(
      response.statusCode,
      decoded['message'] as String? ?? 'Request failed (${response.statusCode}).',
    );
  }

  Future<Session> login(String email, String password) async {
    final body = await _send('POST', '/api/v1/auth/login',
        body: {'email': email, 'password': password});

    final data = body['data'] as Map<String, dynamic>;
    _token = data['accessToken'] as String;

    return Session(
      token: _token!,
      displayName: data['displayName'] as String,
      role: roleFromApi(data['role'] as int),
    );
  }

  Future<List<Sample>> samples() async {
    final body = await _send('GET', '/api/v1/samples?pageSize=100');
    final items = (body['data'] as Map<String, dynamic>)['items'] as List<dynamic>;

    return items.map((e) => Sample.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<ChainVerification> custody(String sampleId) async {
    final body = await _send('GET', '/api/v1/samples/$sampleId/custody');

    return ChainVerification.fromJson(body['data'] as Map<String, dynamic>);
  }

  Future<List<VariantCall>> variantCalls() async {
    final body = await _send('GET', '/api/v1/variant-calls?pageSize=100');
    final items = (body['data'] as Map<String, dynamic>)['items'] as List<dynamic>;

    return items.map((e) => VariantCall.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<void> release(String callId) =>
      _send('POST', '/api/v1/variant-calls/$callId/release');
}
