import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../core/api_client.dart';
import '../core/models.dart';

class SamplesState extends Equatable {
  const SamplesState({
    this.samples = const [],
    this.loading = true,
    this.error,
    this.selected,
    this.chain,
    this.chainLoading = false,
  });

  final List<Sample> samples;
  final bool loading;
  final String? error;
  final Sample? selected;
  final ChainVerification? chain;
  final bool chainLoading;

  SamplesState copyWith({
    List<Sample>? samples,
    bool? loading,
    String? error,
    Sample? selected,
    ChainVerification? chain,
    bool? chainLoading,
    bool clearError = false,
    bool clearChain = false,
  }) =>
      SamplesState(
        samples: samples ?? this.samples,
        loading: loading ?? this.loading,
        error: clearError ? null : (error ?? this.error),
        selected: selected ?? this.selected,
        chain: clearChain ? null : (chain ?? this.chain),
        chainLoading: chainLoading ?? this.chainLoading,
      );

  @override
  List<Object?> get props => [samples, loading, error, selected, chain, chainLoading];
}

class SamplesCubit extends Cubit<SamplesState> {
  SamplesCubit(this._api) : super(const SamplesState());

  final ApiClient _api;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));

    try {
      final samples = await _api.samples();
      emit(state.copyWith(samples: samples, loading: false));

      // Open the first sample straight away. The custody timeline is the point of the app and
      // an empty right-hand pane on first load buries it behind a click.
      if (samples.isNotEmpty && state.selected == null) await select(samples.first);
    } on ApiException catch (e) {
      emit(state.copyWith(loading: false, error: e.message));
    }
  }

  Future<void> select(Sample sample) async {
    emit(state.copyWith(selected: sample, chainLoading: true, clearChain: true));

    try {
      emit(state.copyWith(chain: await _api.custody(sample.id), chainLoading: false));
    } on ApiException catch (e) {
      emit(state.copyWith(chainLoading: false, error: e.message));
    }
  }

  Future<void> refreshChain() async {
    final sample = state.selected;
    if (sample != null) await select(sample);
  }
}
