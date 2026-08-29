namespace GenomeTrack.Domain.Enums;

/// <summary>
/// The five-tier ACMG/AMP classification. Ordered least to most actionable so that
/// "at least likely pathogenic" is expressible as a single comparison.
/// </summary>
public enum ClinicalSignificance
{
    Benign = 1,
    LikelyBenign = 2,
    UncertainSignificance = 3,
    LikelyPathogenic = 4,
    Pathogenic = 5,
}
