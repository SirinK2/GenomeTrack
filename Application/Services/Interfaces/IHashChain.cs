using System;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.Services.Interfaces;

public interface IHashChain
{
    /// <summary>
    /// Hashes one custody link. Every field that a dispute could turn on is inside the hash —
    /// who moved the sample, when, from where, to where, and its position in the chain — so
    /// changing any of them after the fact is detectable.
    /// </summary>
    string Compute(
        string previousHash,
        Guid sampleId,
        int sequence,
        CustodyAction action,
        string fromLocation,
        string toLocation,
        Guid actorId,
        DateTimeOffset occurredAt
    );
}
