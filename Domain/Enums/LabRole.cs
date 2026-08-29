namespace GenomeTrack.Domain.Enums;

/// <summary>
/// Who may do what in the lab. The ladder is deliberate: a technician moves physical
/// material, an analyst interprets it, and only a principal investigator may release a
/// result to the ordering clinician. Releasing is the point of no return — once a variant
/// call leaves the lab it informs care — so it sits alone at the top.
/// </summary>
public enum LabRole
{
    Technician = 1,
    Analyst = 2,
    PrincipalInvestigator = 3,
}
