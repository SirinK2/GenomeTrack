import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../core/api_client.dart';
import '../core/models.dart';

sealed class AuthState extends Equatable {
  const AuthState();

  @override
  List<Object?> get props => [];
}

class AuthSignedOut extends AuthState {
  const AuthSignedOut({this.error});

  final String? error;

  @override
  List<Object?> get props => [error];
}

class AuthBusy extends AuthState {
  const AuthBusy();
}

class AuthSignedIn extends AuthState {
  const AuthSignedIn(this.session);

  final Session session;

  @override
  List<Object?> get props => [session];
}

class AuthCubit extends Cubit<AuthState> {
  AuthCubit(this._api) : super(const AuthSignedOut());

  final ApiClient _api;

  Future<void> signIn(String email, String password) async {
    emit(const AuthBusy());

    try {
      emit(AuthSignedIn(await _api.login(email, password)));
    } on ApiException catch (e) {
      emit(AuthSignedOut(error: e.message));
    } catch (_) {
      // Anything that is not an API failure is the API not being reachable at all, which for a
      // local demo is nearly always "the stack is not running".
      emit(const AuthSignedOut(error: 'Could not reach the API. Is docker compose up?'));
    }
  }

  void signOut() {
    _api.token = null;
    emit(const AuthSignedOut());
  }
}
