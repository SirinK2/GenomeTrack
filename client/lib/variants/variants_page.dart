import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../auth/auth_cubit.dart';
import '../core/models.dart';
import '../core/theme.dart';
import 'variants_cubit.dart';

class VariantsPage extends StatelessWidget {
  const VariantsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthCubit>().state;
    final role = auth is AuthSignedIn ? auth.session.role : LabRole.technician;

    return BlocBuilder<VariantsCubit, VariantsState>(
      builder: (context, state) {
        if (state.loading) return const Center(child: CircularProgressIndicator());

        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (state.error != null)
              Card(
                color: GenomeColors.broken.withValues(alpha: 0.06),
                child: ListTile(
                  leading: const Icon(Icons.block, color: GenomeColors.broken),
                  title: Text(state.error!),
                ),
              ),
            // Explains an empty table before the visitor concludes the app is broken. A
            // technician seeing nothing here is the authorisation rule working.
            if (state.calls.isEmpty)
              Card(
                child: ListTile(
                  leading: const Icon(Icons.visibility_off_outlined),
                  title: const Text('No calls visible to you'),
                  subtitle: Text(
                    role == LabRole.technician
                        ? 'Unreleased calls are provisional interpretations. Sign in as an '
                            'analyst or the PI to see them.'
                        : 'No sequencing run has produced calls yet.',
                  ),
                ),
              ),
            for (final call in state.calls)
              _CallCard(call: call, canRelease: role.canRelease, state: state),
          ],
        );
      },
    );
  }
}

class _CallCard extends StatelessWidget {
  const _CallCard({required this.call, required this.canRelease, required this.state});

  final VariantCall call;
  final bool canRelease;
  final VariantsState state;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final actionable = call.significanceRank >= 4;
    final busy = state.releasingId == call.id;

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(call.gene,
                          style: theme.textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.w700)),
                      const SizedBox(width: 10),
                      Text('${call.locus}  ${call.change}',
                          style: const TextStyle(fontFamily: 'monospace', fontSize: 12)),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: (actionable ? GenomeColors.pathogenic : GenomeColors.benign)
                              .withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          call.significance,
                          style: TextStyle(
                            fontSize: 11,
                            color: actionable ? GenomeColors.pathogenic : GenomeColors.benign,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Text('${call.barcode} · depth ${call.readDepth}',
                          style: theme.textTheme.bodySmall),
                    ],
                  ),
                ],
              ),
            ),
            if (call.isReleased)
              const Chip(
                avatar: Icon(Icons.check, size: 16, color: GenomeColors.intact),
                label: Text('Released'),
              )
            else if (canRelease)
              FilledButton.tonal(
                onPressed: busy ? null : () => context.read<VariantsCubit>().release(call.id),
                child: busy
                    ? const SizedBox(
                        height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                    : const Text('Release'),
              )
            else
              Tooltip(
                message: 'Only a principal investigator may release a result.',
                child: Chip(
                  avatar: const Icon(Icons.lock_outline, size: 16),
                  label: const Text('Provisional'),
                  backgroundColor: theme.colorScheme.surfaceContainerHighest,
                ),
              ),
          ],
        ),
      ),
    );
  }
}
