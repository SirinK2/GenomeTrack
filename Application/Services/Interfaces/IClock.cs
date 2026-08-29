using System;

namespace GenomeTrack.Application.Services.Interfaces;

/// <summary>
/// Injected time. Custody hashes cover a timestamp, so the tests need to control it to assert
/// on a known hash rather than on whatever the wall clock said that millisecond.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
