import 'package:equatable/equatable.dart';

enum LabRole { technician, analyst, principalInvestigator }

LabRole roleFromApi(int value) => switch (value) {
      2 => LabRole.analyst,
      3 => LabRole.principalInvestigator,
      _ => LabRole.technician,
    };

extension LabRoleX on LabRole {
  String get label => switch (this) {
        LabRole.technician => 'Technician',
        LabRole.analyst => 'Analyst',
        LabRole.principalInvestigator => 'Principal Investigator',
      };

  /// Only a principal investigator may release a result. Mirrored from the API rather than
  /// trusted: the button is hidden here for clarity, and the server refuses regardless.
  bool get canRelease => this == LabRole.principalInvestigator;
}

class Session extends Equatable {
  const Session({required this.token, required this.displayName, required this.role});

  final String token;
  final String displayName;
  final LabRole role;

  @override
  List<Object?> get props => [token, displayName, role];
}

enum SampleStatus { registered, accessioned, inSequencing, sequenced, depleted, rejected }

SampleStatus sampleStatusFromApi(int value) => switch (value) {
      2 => SampleStatus.accessioned,
      3 => SampleStatus.inSequencing,
      4 => SampleStatus.sequenced,
      5 => SampleStatus.depleted,
      6 => SampleStatus.rejected,
      _ => SampleStatus.registered,
    };

extension SampleStatusX on SampleStatus {
  String get label => switch (this) {
        SampleStatus.registered => 'Registered',
        SampleStatus.accessioned => 'Accessioned',
        SampleStatus.inSequencing => 'In sequencing',
        SampleStatus.sequenced => 'Sequenced',
        SampleStatus.depleted => 'Depleted',
        SampleStatus.rejected => 'Rejected',
      };
}

class Sample extends Equatable {
  const Sample({
    required this.id,
    required this.barcode,
    required this.subjectRef,
    required this.status,
    required this.currentLocation,
    required this.collectedAt,
  });

  factory Sample.fromJson(Map<String, dynamic> json) => Sample(
        id: json['id'] as String,
        barcode: json['barcode'] as String,
        subjectRef: json['subjectRef'] as String,
        status: sampleStatusFromApi(json['status'] as int),
        currentLocation: json['currentLocation'] as String? ?? '',
        collectedAt: DateTime.parse(json['collectedAt'] as String),
      );

  final String id;
  final String barcode;
  final String subjectRef;
  final SampleStatus status;
  final String currentLocation;
  final DateTime collectedAt;

  @override
  List<Object?> get props => [id, barcode, status, currentLocation];
}

const _custodyActions = {
  1: 'Collected',
  2: 'Received',
  3: 'Transferred',
  4: 'Aliquoted',
  5: 'Placed in storage',
  6: 'Removed from storage',
  7: 'Consumed',
  8: 'Discarded',
};

class CustodyEvent extends Equatable {
  const CustodyEvent({
    required this.sequence,
    required this.action,
    required this.fromLocation,
    required this.toLocation,
    required this.actorName,
    required this.occurredAt,
    required this.hash,
    this.note,
  });

  factory CustodyEvent.fromJson(Map<String, dynamic> json) => CustodyEvent(
        sequence: json['sequence'] as int,
        action: _custodyActions[json['action'] as int] ?? 'Unknown',
        fromLocation: json['fromLocation'] as String? ?? '',
        toLocation: json['toLocation'] as String? ?? '',
        actorName: json['actorName'] as String? ?? 'unknown',
        occurredAt: DateTime.parse(json['occurredAt'] as String),
        hash: json['hash'] as String? ?? '',
        note: json['note'] as String?,
      );

  final int sequence;
  final String action;
  final String fromLocation;
  final String toLocation;
  final String actorName;
  final DateTime occurredAt;
  final String hash;
  final String? note;

  @override
  List<Object?> get props => [sequence, hash];
}

/// The verifier's answer. [brokenAtSequence] names the first link whose stored hash disagrees
/// with its recomputed one, so the UI can mark that exact row rather than the whole chain.
class ChainVerification extends Equatable {
  const ChainVerification({
    required this.isIntact,
    required this.eventCount,
    required this.events,
    this.brokenAtSequence,
    this.explanation,
  });

  factory ChainVerification.fromJson(Map<String, dynamic> json) => ChainVerification(
        isIntact: json['isIntact'] as bool,
        eventCount: json['eventCount'] as int,
        brokenAtSequence: json['brokenAtSequence'] as int?,
        explanation: json['explanation'] as String?,
        events: (json['events'] as List<dynamic>)
            .map((e) => CustodyEvent.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  final bool isIntact;
  final int eventCount;
  final int? brokenAtSequence;
  final String? explanation;
  final List<CustodyEvent> events;

  @override
  List<Object?> get props => [isIntact, eventCount, brokenAtSequence];
}

const _significance = {
  1: 'Benign',
  2: 'Likely benign',
  3: 'Uncertain significance',
  4: 'Likely pathogenic',
  5: 'Pathogenic',
};

class VariantCall extends Equatable {
  const VariantCall({
    required this.id,
    required this.barcode,
    required this.gene,
    required this.locus,
    required this.change,
    required this.significance,
    required this.significanceRank,
    required this.readDepth,
    required this.isReleased,
  });

  factory VariantCall.fromJson(Map<String, dynamic> json) {
    final rank = json['significance'] as int;

    return VariantCall(
      id: json['id'] as String,
      barcode: json['barcode'] as String? ?? '',
      gene: json['gene'] as String? ?? '',
      locus: 'chr${json['chromosome']}:${json['position']}',
      change: '${json['referenceAllele']}>${json['alternateAllele']}',
      significance: _significance[rank] ?? 'Unknown',
      significanceRank: rank,
      readDepth: json['readDepth'] as int,
      isReleased: json['isReleased'] as bool,
    );
  }

  final String id;
  final String barcode;
  final String gene;
  final String locus;
  final String change;
  final String significance;
  final int significanceRank;
  final int readDepth;
  final bool isReleased;

  @override
  List<Object?> get props => [id, isReleased];
}
