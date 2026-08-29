using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Run;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Application.Services.Persistence;
using GenomeTrack.Domain.Entity;
using GenomeTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Application.Services.Implementation;

public class SequencingRunService : ISequencingRunService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SequencingRunService(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<SequencingRunDto>> CreateAsync(CreateRunDto dto, CancellationToken ct = default)
    {
        var code = dto.RunCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(code))
            return Result<SequencingRunDto>.Failure("Run code is required.");

        if (await _db.SequencingRuns.AnyAsync(r => r.RunCode == code && !r.IsDeleted, ct))
            return Result<SequencingRunDto>.Conflict($"Run code '{code}' already exists.");

        var run = new SequencingRun
        {
            RunCode = code,
            Platform = dto.Platform?.Trim() ?? string.Empty,
            Status = RunStatus.Draft,
            CreatedById = _currentUser.UserId,
            CreatedAt = _clock.UtcNow,
        };

        _db.SequencingRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        return Result<SequencingRunDto>.Success(Map(run, new List<RunSampleDto>()), "Run created.");
    }

    public async Task<Result<SequencingRunDto>> LoadSampleAsync(
        Guid runId,
        LoadSampleDto dto,
        CancellationToken ct = default
    )
    {
        var run = await _db
            .SequencingRuns.Include(r => r.RunSamples)
            .FirstOrDefaultAsync(r => r.Id == runId && !r.IsDeleted, ct);

        if (run is null)
            return Result<SequencingRunDto>.NotFound("Run not found.");

        if (run.Status != RunStatus.Draft)
            return Result<SequencingRunDto>.Conflict(
                $"Samples can only be loaded onto a draft run; this one is {run.Status}."
            );

        var sample = await _db.Samples.FirstOrDefaultAsync(s => s.Id == dto.SampleId && !s.IsDeleted, ct);

        if (sample is null)
            return Result<SequencingRunDto>.NotFound("Sample not found.");

        // The rule the whole accessioning step exists to enforce: material the lab never
        // confirmed receiving must not reach a flow cell.
        if (sample.Status != SampleStatus.Accessioned)
            return Result<SequencingRunDto>.Conflict(
                $"Only an accessioned sample can be loaded; '{sample.Barcode}' is {sample.Status}."
            );

        if (run.RunSamples.Any(rs => rs.SampleId == sample.Id))
            return Result<SequencingRunDto>.Conflict($"Sample '{sample.Barcode}' is already on this run.");

        if (run.RunSamples.Any(rs => rs.LaneIndex == dto.LaneIndex))
            return Result<SequencingRunDto>.Conflict($"Lane {dto.LaneIndex} is already occupied on this run.");

        var runSample = new RunSample
        {
            SequencingRunId = run.Id,
            SampleId = sample.Id,
            LaneIndex = dto.LaneIndex,
            CreatedAt = _clock.UtcNow,
        };

        // Added through the set rather than through run.RunSamples. BaseEntity assigns its own
        // Guid, so the key is already populated by the time change detection sees the instance;
        // discovered through the navigation collection EF reads that as an existing row and
        // tracks it Modified, then fails on save because there is nothing to update. Calling
        // Add sets the state explicitly.
        _db.RunSamples.Add(runSample);
        run.RunSamples.Add(runSample);

        run.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(run.Id, ct);
    }

    public async Task<Result<SequencingRunDto>> StartAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _db
            .SequencingRuns.Include(r => r.RunSamples)
            .ThenInclude(rs => rs.Sample)
            .FirstOrDefaultAsync(r => r.Id == runId && !r.IsDeleted, ct);

        if (run is null)
            return Result<SequencingRunDto>.NotFound("Run not found.");

        if (run.Status != RunStatus.Draft)
            return Result<SequencingRunDto>.Conflict($"Only a draft run can be started; this one is {run.Status}.");

        if (run.RunSamples.Count == 0)
            return Result<SequencingRunDto>.Conflict("A run needs at least one sample before it can start.");

        var now = _clock.UtcNow;
        run.Status = RunStatus.Running;
        run.StartedAt = now;
        run.UpdatedAt = now;

        foreach (var rs in run.RunSamples)
        {
            if (rs.Sample is null)
                continue;

            rs.Sample.Status = SampleStatus.InSequencing;
            rs.Sample.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(run.Id, ct);
    }

    public async Task<Result<SequencingRunDto>> CompleteAsync(
        Guid runId,
        CompleteRunDto dto,
        CancellationToken ct = default
    )
    {
        var run = await _db
            .SequencingRuns.Include(r => r.RunSamples)
            .ThenInclude(rs => rs.Sample)
            .FirstOrDefaultAsync(r => r.Id == runId && !r.IsDeleted, ct);

        if (run is null)
            return Result<SequencingRunDto>.NotFound("Run not found.");

        if (run.Status != RunStatus.Running)
            return Result<SequencingRunDto>.Conflict($"Only a running run can be completed; this one is {run.Status}.");

        var loadedSampleIds = run.RunSamples.Select(rs => rs.SampleId).ToHashSet();

        // Validate the whole payload before writing any of it. A partial import would leave the
        // run half-called with no way to tell which rows made it.
        var stray = dto.Calls.Where(c => !loadedSampleIds.Contains(c.SampleId)).ToList();

        if (stray.Count > 0)
            return Result<SequencingRunDto>.Failure(
                "Every call must belong to a sample loaded on this run.",
                stray
                    .Select(c => new Detail
                    {
                        Field = nameof(c.SampleId),
                        Message = $"Sample {c.SampleId} is not on run '{run.RunCode}'.",
                    })
                    .ToList()
            );

        var now = _clock.UtcNow;

        foreach (var call in dto.Calls)
        {
            var variant = await ResolveVariantAsync(call, now, ct);

            _db.VariantCalls.Add(
                new VariantCall
                {
                    SampleId = call.SampleId,
                    VariantId = variant.Id,
                    SequencingRunId = run.Id,
                    ReadDepth = call.ReadDepth,
                    QualityScore = call.QualityScore,
                    Zygosity = call.Zygosity,
                    CreatedAt = now,
                }
            );
        }

        run.Status = RunStatus.Completed;
        run.CompletedAt = now;
        run.UpdatedAt = now;

        foreach (var rs in run.RunSamples)
        {
            if (rs.Sample is null)
                continue;

            rs.Sample.Status = SampleStatus.Sequenced;
            rs.Sample.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(run.Id, ct);
    }

    public async Task<Result<SequencingRunDto>> GetByIdAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await _db
            .SequencingRuns.AsNoTracking()
            .Include(r => r.RunSamples)
            .ThenInclude(rs => rs.Sample)
            .FirstOrDefaultAsync(r => r.Id == runId && !r.IsDeleted, ct);

        if (run is null)
            return Result<SequencingRunDto>.NotFound("Run not found.");

        var samples = run
            .RunSamples.OrderBy(rs => rs.LaneIndex)
            .Select(rs => new RunSampleDto
            {
                SampleId = rs.SampleId,
                Barcode = rs.Sample?.Barcode ?? string.Empty,
                LaneIndex = rs.LaneIndex,
            })
            .ToList();

        return Result<SequencingRunDto>.Success(Map(run, samples));
    }

    /// <summary>
    /// Finds the catalogued variant for a call, or catalogues it. Two samples carrying the same
    /// substitution must resolve to one <see cref="Variant"/> row, otherwise "how many subjects
    /// carry this?" becomes uncountable.
    /// </summary>
    private async Task<Variant> ResolveVariantAsync(
        VariantCallInputDto call,
        DateTimeOffset now,
        CancellationToken ct
    )
    {
        var gene = call.Gene?.Trim() ?? string.Empty;
        var chromosome = call.Chromosome?.Trim() ?? string.Empty;
        var reference = call.ReferenceAllele?.Trim() ?? string.Empty;
        var alternate = call.AlternateAllele?.Trim() ?? string.Empty;

        var existing = await _db.Variants.FirstOrDefaultAsync(
            v =>
                v.Chromosome == chromosome
                && v.Position == call.Position
                && v.ReferenceAllele == reference
                && v.AlternateAllele == alternate,
            ct
        );

        if (existing is not null)
            return existing;

        var variant = new Variant
        {
            Gene = gene,
            Chromosome = chromosome,
            Position = call.Position,
            ReferenceAllele = reference,
            AlternateAllele = alternate,
            Significance = call.Significance,
            CreatedAt = now,
        };

        _db.Variants.Add(variant);
        await _db.SaveChangesAsync(ct);

        return variant;
    }

    private static SequencingRunDto Map(SequencingRun r, List<RunSampleDto> samples) =>
        new()
        {
            Id = r.Id,
            RunCode = r.RunCode,
            Platform = r.Platform,
            Status = r.Status,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            FailureReason = r.FailureReason,
            Samples = samples,
        };
}
