namespace GenomeTrack.Domain.Enums;

/// <summary>
/// The physical life of a sample. <see cref="Registered"/> means the requisition exists but
/// the tube has not been checked in; <see cref="Accessioned"/> means the lab has it in hand
/// and verified it against the requisition. Only an accessioned sample may join a run —
/// sequencing something the lab never confirmed receiving is how results get attributed to
/// the wrong patient.
/// </summary>
public enum SampleStatus
{
    Registered = 1,
    Accessioned = 2,
    InSequencing = 3,
    Sequenced = 4,
    Depleted = 5,
    Rejected = 6,
}
