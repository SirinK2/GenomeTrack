using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.Constants;
using GenomeTrack.Application.DTOs.Sample;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Application.Services.Persistence;
using GenomeTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SampleEntity = GenomeTrack.Domain.Entity.Sample;

namespace GenomeTrack.Application.Services.Implementation;

public class SampleService : ISampleService
{
    private readonly IAppDbContext _db;
    private readonly ICustodyService _custody;
    private readonly IClock _clock;

    public SampleService(IAppDbContext db, ICustodyService custody, IClock clock)
    {
        _db = db;
        _custody = custody;
        _clock = clock;
    }

    public async Task<Result<SampleDto>> RegisterAsync(RegisterSampleDto dto, CancellationToken ct = default)
    {
        var barcode = dto.Barcode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(barcode))
            return Result<SampleDto>.Failure("Barcode is required.");

        // Barcodes are reused once a sample is discarded, so the uniqueness check is scoped to
        // live rows. A global unique index would refuse a legitimately recycled label.
        var taken = await _db
            .Samples.AnyAsync(s => s.Barcode == barcode && !s.IsDeleted, ct);

        if (taken)
            return Result<SampleDto>.Conflict($"Barcode '{barcode}' is already in use by a live sample.");

        var sample = new SampleEntity
        {
            Barcode = barcode,
            SubjectRef = dto.SubjectRef?.Trim() ?? string.Empty,
            Type = dto.Type,
            Status = SampleStatus.Registered,
            CollectedAt = dto.CollectedAt,
            CurrentLocation = dto.CollectedAtLocation?.Trim() ?? string.Empty,
            CreatedAt = _clock.UtcNow,
        };

        _db.Samples.Add(sample);
        await _db.SaveChangesAsync(ct);

        // The chain opens at collection, not at accessioning. A sample that goes missing between
        // the two is the case the chain most needs to describe, and it cannot if the first link
        // is written only on arrival.
        await _custody.AppendAsync(
            sample.Id,
            new DTOs.Custody.TransferCustodyDto
            {
                Action = CustodyAction.Collected,
                ToLocation = sample.CurrentLocation,
                Note = "Sample registered.",
            },
            ct
        );

        return Result<SampleDto>.Success(Map(sample), "Sample registered.");
    }

    public async Task<Result<SampleDto>> AccessionAsync(Guid sampleId, AccessionSampleDto dto, CancellationToken ct = default)
    {
        var sample = await _db.Samples.FirstOrDefaultAsync(s => s.Id == sampleId && !s.IsDeleted, ct);

        if (sample is null)
            return Result<SampleDto>.NotFound("Sample not found.");

        if (sample.Status != SampleStatus.Registered)
            return Result<SampleDto>.Conflict(
                $"Only a registered sample can be accessioned; this one is {sample.Status}."
            );

        sample.Status = SampleStatus.Accessioned;
        sample.UpdatedAt = _clock.UtcNow;

        // The location is deliberately not set here. CustodyService reads the sample's current
        // location to fill the event's "from", then moves it — so assigning the destination
        // first would record a move from the destination to itself and lose where the sample
        // actually came from. The append saves both changes.
        var custody = await _custody.AppendAsync(
            sample.Id,
            new DTOs.Custody.TransferCustodyDto
            {
                Action = CustodyAction.Received,
                ToLocation = dto.ReceivedAtLocation?.Trim() ?? string.Empty,
                Note = dto.Note,
            },
            ct
        );

        if (!custody.IsSuccess)
            return Result<SampleDto>.Failure("Sample could not be accessioned.", custody.Details?.ToList());

        return Result<SampleDto>.Success(Map(sample), "Sample accessioned.");
    }

    public async Task<Result<SampleDto>> GetByIdAsync(Guid sampleId, CancellationToken ct = default)
    {
        var sample = await _db
            .Samples.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sampleId && !s.IsDeleted, ct);

        return sample is null
            ? Result<SampleDto>.NotFound("Sample not found.")
            : Result<SampleDto>.Success(Map(sample));
    }

    public async Task<Result<PagedData<SampleDto>>> SearchAsync(SampleFilter filter, CancellationToken ct = default)
    {
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;

        // Clamp rather than reset. A caller asking for 5,000 rows wants as many as we allow,
        // and silently handing back 20 reads as a bug on their side.
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _db.Samples.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Barcode))
            query = query.Where(s => s.Barcode.Contains(filter.Barcode.Trim()));

        if (!string.IsNullOrWhiteSpace(filter.SubjectRef))
            query = query.Where(s => s.SubjectRef == filter.SubjectRef.Trim());

        if (filter.Status.HasValue)
            query = query.Where(s => s.Status == filter.Status.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CollectedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var page = new PagedData<SampleDto>(
            items.Select(Map).ToList(),
            new Pagination
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            }
        );

        return Result<PagedData<SampleDto>>.Success(page);
    }

    private static SampleDto Map(SampleEntity s) =>
        new()
        {
            Id = s.Id,
            Barcode = s.Barcode,
            SubjectRef = s.SubjectRef,
            Type = s.Type,
            Status = s.Status,
            CollectedAt = s.CollectedAt,
            CurrentLocation = s.CurrentLocation,
        };
}
