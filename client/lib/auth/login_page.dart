import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'auth_cubit.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _email = TextEditingController(text: 'pi@genometrack.local');
  final _password = TextEditingController(text: 'Passw0rd!');

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  void _submit() =>
      context.read<AuthCubit>().signIn(_email.text.trim(), _password.text);

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(28),
                child: BlocBuilder<AuthCubit, AuthState>(
                  builder: (context, state) {
                    final busy = state is AuthBusy;

                    return Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Text('GenomeTrack', style: theme.textTheme.headlineSmall),
                        const SizedBox(height: 4),
                        Text(
                          'Sample custody and variant release',
                          style: theme.textTheme.bodyMedium
                              ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                        ),
                        const SizedBox(height: 24),
                        TextField(
                          controller: _email,
                          decoration: const InputDecoration(labelText: 'Email'),
                          onSubmitted: (_) => _submit(),
                        ),
                        const SizedBox(height: 12),
                        TextField(
                          controller: _password,
                          obscureText: true,
                          decoration: const InputDecoration(labelText: 'Password'),
                          onSubmitted: (_) => _submit(),
                        ),
                        if (state is AuthSignedOut && state.error != null) ...[
                          const SizedBox(height: 12),
                          Text(
                            state.error!,
                            style: TextStyle(color: theme.colorScheme.error),
                          ),
                        ],
                        const SizedBox(height: 20),
                        FilledButton(
                          onPressed: busy ? null : _submit,
                          child: busy
                              ? const SizedBox(
                                  height: 18,
                                  width: 18,
                                  child: CircularProgressIndicator(strokeWidth: 2),
                                )
                              : const Text('Sign in'),
                        ),
                        const SizedBox(height: 20),
                        const Divider(),
                        const SizedBox(height: 8),
                        Text('Seeded accounts', style: theme.textTheme.labelLarge),
                        const SizedBox(height: 8),
                        // Switching role is the fastest way to see the API's authorisation
                        // rules, so the demo accounts are one tap rather than something to type.
                        Wrap(
                          spacing: 8,
                          children: [
                            for (final account in const [
                              ('Technician', 'tech@genometrack.local'),
                              ('Analyst', 'analyst@genometrack.local'),
                              ('PI', 'pi@genometrack.local'),
                            ])
                              ActionChip(
                                label: Text(account.$1),
                                onPressed: busy
                                    ? null
                                    : () => setState(() => _email.text = account.$2),
                              ),
                          ],
                        ),
                      ],
                    );
                  },
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
