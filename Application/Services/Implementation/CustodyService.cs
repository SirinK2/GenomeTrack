using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.Constants;
using GenomeTrack.Application.DTOs.Custody;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Application.Services.Persistence;
using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Application.Services.Implementation;

/// <summary>
/// Owns the append-only custody chain. Nothing else in the application writes a
/// <see cref="CustodyEvent"/> — every caller goes through <see cref="AppendAsync"/> so the
/// hash linkage cannot be skipped by a well-meaning new code path.
/// </summary>
public class CustodyService : ICustodyService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IHashChain _hashChain;
    private readonly IClock _clock;

    public CustodyService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IHashChain hashChain,
        IClock clock
    )
    {
        _db = db;
        _currentUser = currentUser;
        _hashChain = hashChain;
        _clock = clock;
    }

    public async Task<Result<CustodyEventDto>> AppendAsync(
        Guid sampleId,
        TransferCustodyDto dto,
        CancellationToken ct = default
    )
    {
        var sample = await _db.Samples.FirstOrDefaultAsync(s => s.Id == sampleId && !s.IsDeleted, ct);

        if (sample is null)
            return Result<CustodyEventDto>.NotFound("Sample not found.");

        var last = await _db
            .CustodyEvents.Where(e => e.SampleId == sampleId)
            .OrderByDescending(e => e.Sequence)
            .FirstOrDefaultAsync(ct);

        var sequence = (last?.Sequence ?? 0) + 1;
        var previousHash = last?.Hash ?? CustodyChain.GenesisHash;
        var occurredAt = CustodyChain.Normalize(_clock.UtcNow);

        // The move is recorded as leaving wherever the sample currently is. Trusting a
        // caller-supplied "from" would let a client paper over a gap it had already created.
        var fromLocation = sample.CurrentLocation;
        var toLocation = dto.ToLocation?.Trim() ?? string.Empty;

        var evt = new CustodyEvent
        {
            SampleId = sampleId,
            Action = dto.Action,
            FromLocation = fromLocation,
            ToLocation = toLocation,
            ActorId = _currentUser.UserId,
            OccurredAt = occurredAt,
            Sequence = sequence,
            PreviousHash = previousHash,
            Note = dto.Note,
            CreatedAt = occurredAt,
        };

        evt.Hash = _hashChain.Compute(
            previousHash,
            sampleId,
            sequence,
            dto.Action,
            fromLocation,
            toLocation,
            _currentUser.UserId,
            occurredAt
        );

        _db.CustodyEvents.Add(evt);

        sample.CurrentLocation = toLocation;
        sample.UpdatedAt = occurredAt;

        await _db.SaveChangesAsync(ct);

        return Result<CustodyEventDto>.Success(
            new CustodyEventDto
            {
                Id = evt.Id,
                Sequence = evt.Sequence,
                Action = evt.Action,
                FromLocation = evt.FromLocation,
                ToLocation = evt.ToLocation,
                ActorName = _currentUser.DisplayName,
                OccurredAt = evt.OccurredAt,
                Hash = evt.Hash,
                Note = evt.Note,
            },
            "Custody event recorded."
        );
    }

    public async Task<Result<ChainVerificationDto>> VerifyChainAsync(
        Guid sampleId,
        CancellationToken ct = default
    )
    {
        var exists = await _db.Samples.AnyAsync(s => s.Id == sampleId && !s.IsDeleted, ct);

        if (!exists)
            return Result<ChainVerificationDto>.NotFound("Sample not found.");

        var events = await _db
            .CustodyEvents.AsNoTracking()
            .Include(e => e.Actor)
            .Where(e => e.SampleId == sampleId)
            .OrderBy(e => e.Sequence)
            .ToListAsync(ct);

        var report = new ChainVerificationDto
        {
            SampleId = sampleId,
            EventCount = events.Count,
            IsIntact = true,
            Events = events
                .Select(e => new CustodyEventDto
                {
                    Id = e.Id,
                    Sequence = e.Sequence,
                    Action = e.Action,
                    FromLocation = e.FromLocation,
                    ToLocation = e.ToLocation,
                    ActorName = e.Actor?.DisplayName ?? "unknown",
                    OccurredAt = e.OccurredAt,
                    Hash = e.Hash,
                    Note = e.Note,
                })
                .ToList(),
        };

        var expectedPrevious = CustodyChain.GenesisHash;

        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];

            // Three ways a chain breaks, and they are worth telling apart: a row was deleted
            // (sequence gap), a row was re-pointed (previous hash mismatch), or a row's own
            // fields were edited (recomputed hash mismatch).
            if (e.Sequence != i + 1)
                return Broken(report, e.Sequence, $"Expected sequence {i + 1} but found {e.Sequence}; an event was removed.");

            if (e.PreviousHash != expectedPrevious)
                return Broken(report, e.Sequence, "This event does not point at the one before it; the chain was re-linked.");

            var recomputed = _hashChain.Compute(
                e.PreviousHash,
                e.SampleId,
                e.Sequence,
                e.Action,
                e.FromLocation,
                e.ToLocation,
                e.ActorId,
                e.OccurredAt
            );

            if (recomputed != e.Hash)
                return Broken(report, e.Sequence, "This event's contents no longer match its hash; the row was edited after it was written.");

            expectedPrevious = e.Hash;
        }

        return Result<ChainVerificationDto>.Success(report, "Chain of custody is intact.");
    }

    private static Result<ChainVerificationDto> Broken(
        ChainVerificationDto report,
        int sequence,
        string explanation
    )
    {
        report.IsIntact = false;
        report.BrokenAtSequence = sequence;
        report.Explanation = explanation;

        // Still a 200 with a truthful body: "this chain is broken" is a successful answer to
        // "is this chain intact", and the caller needs the events to act on it.
        return Result<ChainVerificationDto>.Success(report, "Chain of custody is broken.");
    }
}
