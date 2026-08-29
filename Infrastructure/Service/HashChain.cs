using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Infrastructure.Service;

/// <summary>
/// SHA-256 over a pipe-delimited canonical form.
///
/// The delimiter matters more than it looks: without one, a move from "Freezer" to "A1" and a
/// move from "Freeze" to "rA1" concatenate to the same bytes and hash identically. Timestamps
/// are normalised to UTC round-trip format so the same instant always hashes the same way
/// regardless of the offset the caller happened to send.
/// </summary>
public class HashChain : IHashChain
{
    public string Compute(
        string previousHash,
        Guid sampleId,
        int sequence,
        CustodyAction action,
        string fromLocation,
        string toLocation,
        Guid actorId,
        DateTimeOffset occurredAt
    )
    {
        var canonical = string.Join(
            '|',
            previousHash,
            sampleId.ToString("N"),
            sequence.ToString(CultureInfo.InvariantCulture),
            ((int)action).ToString(CultureInfo.InvariantCulture),
            fromLocation ?? string.Empty,
            toLocation ?? string.Empty,
            actorId.ToString("N"),
            occurredAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        );

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
