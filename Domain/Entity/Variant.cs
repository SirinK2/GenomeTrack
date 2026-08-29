using System.Collections.Generic;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Domain.Entity;

/// <summary>
/// A catalogued genomic variant, independent of any one sample. Identity is the locus plus
/// both alleles — the same position with a different substitution is a different variant, so
/// the uniqueness constraint spans all four columns rather than position alone.
/// </summary>
public class Variant : BaseEntity
{
    public string Gene { get; set; } = string.Empty;
    public string Chromosome { get; set; } = string.Empty;
    public long Position { get; set; }
    public string ReferenceAllele { get; set; } = string.Empty;
    public string AlternateAllele { get; set; } = string.Empty;

    public ClinicalSignificance Significance { get; set; } = ClinicalSignificance.UncertainSignificance;
    public string? ClinVarId { get; set; }

    public ICollection<VariantCall> Calls { get; set; } = new List<VariantCall>();
}
