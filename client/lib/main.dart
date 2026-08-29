import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'auth/auth_cubit.dart';
import 'auth/login_page.dart';
import 'core/api_client.dart';
import 'core/models.dart';
import 'core/theme.dart';
import 'samples/samples_cubit.dart';
import 'samples/samples_page.dart';
import 'variants/variants_cubit.dart';
import 'variants/variants_page.dart';

/// Overridable at build time so the same bundle can point at a deployed API:
/// `flutter build web --dart-define=API_BASE_URL=https://…`
const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://localhost:8080',
);

void main() => runApp(const GenomeTrackApp());

class GenomeTrackApp extends StatelessWidget {
  const GenomeTrackApp({super.key});

  @override
  Widget build(BuildContext context) {
    final api = ApiClient(baseUrl: apiBaseUrl);

    return MultiBlocProvider(
      providers: [
        BlocProvider(create: (_) => AuthCubit(api)),
        BlocProvider(create: (_) => SamplesCubit(api)),
        BlocProvider(create: (_) => VariantsCubit(api)),
      ],
      child: MaterialApp(
        title: 'GenomeTrack',
        debugShowCheckedModeBanner: false,
        theme: buildTheme(),
        home: const _Root(),
      ),
    );
  }
}

class _Root extends StatelessWidget {
  const _Root();

  @override
  Widget build(BuildContext context) {
    return BlocListener<AuthCubit, AuthState>(
      // Both tabs are reloaded on sign-in rather than lazily on first view: what a role can see
      // is the thing being demonstrated, and it should already be on screen when you switch.
      listenWhen: (previous, current) => current is AuthSignedIn,
      listener: (context, state) {
        context.read<SamplesCubit>().load();
        context.read<VariantsCubit>().load();
      },
      child: BlocBuilder<AuthCubit, AuthState>(
        builder: (context, state) =>
            state is AuthSignedIn ? _Shell(session: state.session) : const LoginPage(),
      ),
    );
  }
}

class _Shell extends StatefulWidget {
  const _Shell({required this.session});

  final Session session;

  @override
  State<_Shell> createState() => _ShellState();
}

class _ShellState extends State<_Shell> {
  int _tab = 0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('GenomeTrack'),
        bottom: TabBarShim(
          index: _tab,
          onChanged: (i) => setState(() => _tab = i),
          labels: const ['Samples', 'Variant calls'],
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(widget.session.displayName, style: theme.textTheme.bodyMedium),
                Text(
                  widget.session.role.label,
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: 'Sign out',
            icon: const Icon(Icons.logout),
            onPressed: () => context.read<AuthCubit>().signOut(),
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: IndexedStack(
        index: _tab,
        children: const [SamplesPage(), VariantsPage()],
      ),
    );
  }
}

/// A minimal segmented control in the app bar. A full TabController buys nothing for two fixed
/// tabs whose state has to survive switching anyway.
class TabBarShim extends StatelessWidget implements PreferredSizeWidget {
  const TabBarShim({
    super.key,
    required this.index,
    required this.onChanged,
    required this.labels,
  });

  final int index;
  final ValueChanged<int> onChanged;
  final List<String> labels;

  @override
  Size get preferredSize => const Size.fromHeight(48);

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Padding(
        padding: const EdgeInsets.only(left: 12, bottom: 8),
        child: SegmentedButton<int>(
          segments: [
            for (var i = 0; i < labels.length; i++)
              ButtonSegment(value: i, label: Text(labels[i])),
          ],
          selected: {index},
          showSelectedIcon: false,
          onSelectionChanged: (s) => onChanged(s.first),
        ),
      ),
    );
  }
}
