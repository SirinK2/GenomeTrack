import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../core/api_client.dart';
import '../core/models.dart';

class VariantsState extends Equatable {
  const VariantsState({
    this.calls = const [],
    this.loading = true,
    this.error,
    this.releasingId,
  });

  final List<VariantCall> calls;
  final bool loading;
  final String? error;
  final String? releasingId;

  VariantsState copyWith({
    List<VariantCall>? calls,
    bool? loading,
    String? error,
    String? releasingId,
    bool clearError = false,
    bool clearReleasing = false,
  }) =>
      VariantsState(
        calls: calls ?? this.calls,
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
        releasingId: clearReleasing ? null : (releasingId ?? this.releasingId),
      );

  @override
  List<Object?> get props => [calls, loading, error, releasingId];
}

class VariantsCubit extends Cubit<VariantsState> {
  VariantsCubit(this._api) : super(const VariantsState());

  final ApiClient _api;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));

    try {
      emit(state.copyWith(calls: await _api.variantCalls(), loading: false));
    } on ApiException catch (e) {
      emit(state.copyWith(loading: false, error: e.message));
    }
  }

  Future<void> release(String callId) async {
    emit(state.copyWith(releasingId: callId, clearError: true));

    try {
      await _api.release(callId);
      final calls = await _api.variantCalls();
      emit(state.copyWith(calls: calls, clearReleasing: true));
    } on ApiException catch (e) {
      // The server's own refusal is shown verbatim. A client-side guess at why would drift from
      // whatever the API actually enforces.
      emit(state.copyWith(error: e.message, clearReleasing: true));
    }
  }
}
