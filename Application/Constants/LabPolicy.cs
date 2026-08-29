using System;

namespace GenomeTrack.Application.Constants;

public static class LabPolicy
{
    public const string TechnicianOrAbove = nameof(TechnicianOrAbove);
    public const string AnalystOrAbove = nameof(AnalystOrAbove);
    public const string PrincipalInvestigatorOnly = nameof(PrincipalInvestigatorOnly);
}

public static class CustodyChain
{
    /// <summary>
    /// PreviousHash of the first event in every chain. A fixed, recognisable constant means a
    /// chain that starts anywhere else is detectably truncated rather than merely short.
    /// </summary>
    public const string GenesisHash = "GENESIS";

    /// <summary>
    /// Rounds an instant down to whole milliseconds before it is hashed or stored.
    ///
    /// A .NET <see cref="DateTimeOffset"/> counts 100-nanosecond ticks; PostgreSQL timestamptz
    /// keeps microseconds, and other providers keep less. Hashing the un-rounded value and then
    /// letting the database round it on write produces a row whose stored timestamp no longer
    /// reproduces its own hash — every chain verifies as broken at its first link, on data
    /// nobody touched. Truncating on the way in makes the hashed value and the stored value the
    /// same thing at any precision a mainstream database offers.
    /// </summary>
    public static DateTimeOffset Normalize(DateTimeOffset instant)
    {
        var ticks = instant.Ticks - (instant.Ticks % TimeSpan.TicksPerMillisecond);

        return new DateTimeOffset(ticks, instant.Offset);
    }
}
