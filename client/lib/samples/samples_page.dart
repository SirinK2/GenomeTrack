import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:intl/intl.dart';

import '../core/models.dart';
import '../core/theme.dart';
import 'samples_cubit.dart';

class SamplesPage extends StatelessWidget {
  const SamplesPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<SamplesCubit, SamplesState>(
      builder: (context, state) {
        if (state.loading) return const Center(child: CircularProgressIndicator());

        if (state.error != null) {
          return Center(child: Text(state.error!));
        }

        if (state.samples.isEmpty) {
          return const Center(child: Text('No samples registered yet.'));
        }

        // Side by side when there is room, stacked when there is not. The timeline is unreadable
        // in a narrow column next to a list.
        return LayoutBuilder(
          builder: (context, constraints) {
            final wide = constraints.maxWidth > 840;

            final list = _SampleList(state: state);
            final detail = _ChainPanel(state: state);

            if (!wide) {
              return ListView(
                padding: const EdgeInsets.all(16),
                children: [list, const SizedBox(height: 16), detail],
              );
            }

            return Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(width: 320, child: list),
                  const SizedBox(width: 16),
                  Expanded(child: detail),
                ],
              ),
            );
          },
        );
      },
    );
  }
}

class _SampleList extends StatelessWidget {
  const _SampleList({required this.state});

  final SamplesState state;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          for (final sample in state.samples)
            ListTile(
              selected: sample.id == state.selected?.id,
              selectedTileColor: Theme.of(context).colorScheme.primaryContainer.withValues(alpha: 0.3),
              title: Text(sample.barcode,
                  style: const TextStyle(fontFamily: 'monospace', fontWeight: FontWeight.w600)),
              subtitle: Text('${sample.subjectRef} · ${sample.status.label}'),
              trailing: Text(
                sample.currentLocation,
                style: Theme.of(context).textTheme.bodySmall,
                textAlign: TextAlign.end,
              ),
              onTap: () => context.read<SamplesCubit>().select(sample),
            ),
        ],
      ),
    );
  }
}

class _ChainPanel extends StatelessWidget {
  const _ChainPanel({required this.state});

  final SamplesState state;

  @override
  Widget build(BuildContext context) {
    if (state.chainLoading) {
      return const Card(
        child: SizedBox(height: 240, child: Center(child: CircularProgressIndicator())),
      );
    }

    final chain = state.chain;
    if (chain == null) return const SizedBox.shrink();

    final theme = Theme.of(context);
    final intact = chain.isIntact;
    final colour = intact ? GenomeColors.intact : GenomeColors.broken;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text('Chain of custody', style: theme.textTheme.titleMedium),
                ),
                IconButton(
                  tooltip: 'Re-verify',
                  icon: const Icon(Icons.refresh),
                  onPressed: () => context.read<SamplesCubit>().refreshChain(),
                ),
              ],
            ),
            const SizedBox(height: 12),
            // The verdict, stated plainly. This is the one thing a visitor should read.
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: colour.withValues(alpha: 0.08),
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: colour.withValues(alpha: 0.4)),
              ),
              child: Row(
                children: [
                  Icon(intact ? Icons.verified_user : Icons.gpp_bad, color: colour),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          intact
                              ? 'Verified intact · ${chain.eventCount} events'
                              : 'Broken at event #${chain.brokenAtSequence}',
                          style: theme.textTheme.titleSmall?.copyWith(color: colour),
                        ),
                        if (!intact && chain.explanation != null) ...[
                          const SizedBox(height: 2),
                          Text(chain.explanation!, style: theme.textTheme.bodySmall),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            for (final event in chain.events)
              _CustodyRow(event: event, broken: event.sequence == chain.brokenAtSequence),
          ],
        ),
      ),
    );
  }
}

class _CustodyRow extends StatelessWidget {
  const _CustodyRow({required this.event, required this.broken});

  final CustodyEvent event;
  final bool broken;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final colour = broken ? GenomeColors.broken : theme.colorScheme.outline;

    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 26,
                height: 26,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: colour.withValues(alpha: broken ? 0.15 : 0.08),
                  border: Border.all(color: colour),
                ),
                child: Text('${event.sequence}',
                    style: theme.textTheme.labelSmall?.copyWith(color: colour)),
              ),
              Container(width: 1, height: 44, color: theme.colorScheme.outlineVariant),
            ],
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(event.action, style: theme.textTheme.titleSmall),
                    if (broken) ...[
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 1),
                        decoration: BoxDecoration(
                          color: GenomeColors.broken,
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: const Text('ALTERED',
                            style: TextStyle(color: Colors.white, fontSize: 10)),
                      ),
                    ],
                  ],
                ),
                Text(
                  '${event.fromLocation} → ${event.toLocation}',
                  style: theme.textTheme.bodySmall,
                ),
                Text(
                  '${DateFormat('d MMM y HH:mm').format(event.occurredAt.toLocal())} · ${event.actorName}',
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: theme.colorScheme.onSurfaceVariant),
                ),
                // Truncated because the full digest adds nothing on screen, but showing part of
                // it makes the point that each link is hashed.
                Text(
                  '${event.hash.substring(0, 24)}…',
                  style: const TextStyle(fontFamily: 'monospace', fontSize: 11, color: Colors.grey),
                ),
                const SizedBox(height: 10),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
