using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Variant;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Application.Services.Persistence;
using GenomeTrack.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Application.Services.Implementation;

public class VariantService : IVariantService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public VariantService(IAppDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<PagedData<VariantCallDto>>> SearchAsync(
        VariantCallFilter filter,
        CancellationToken ct = default
    )
    {
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _db
            .VariantCalls.AsNoTracking()
            .Include(c => c.Variant)
            .Include(c => c.Sample)
            .Where(c => !c.IsDeleted);

        if (filter.SampleId.HasValue)
            query = query.Where(c => c.SampleId == filter.SampleId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Gene))
            query = query.Where(c => c.Variant!.Gene == filter.Gene.Trim());

        if (filter.MinimumSignificance.HasValue)
            query = query.Where(c => c.Variant!.Significance >= filter.MinimumSignificance.Value);

        // A technician handles tubes, not interpretations. Unreleased calls are provisional and
        // stay inside the analyst/PI boundary regardless of what the caller asked for.
        if (_currentUser.Role == LabRole.Technician)
            query = query.Where(c => c.ReleasedAt != null);
        else if (filter.ReleasedOnly == true)
            query = query.Where(c => c.ReleasedAt != null);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Variant!.Chromosome)
            .ThenBy(c => c.Variant!.Position)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var page = new PagedData<VariantCallDto>(
            items
                .Select(c => new VariantCallDto
                {
                    Id = c.Id,
                    SampleId = c.SampleId,
                    Barcode = c.Sample?.Barcode ?? string.Empty,
                    Gene = c.Variant?.Gene ?? string.Empty,
                    Chromosome = c.Variant?.Chromosome ?? string.Empty,
                    Position = c.Variant?.Position ?? 0,
                    ReferenceAllele = c.Variant?.ReferenceAllele ?? string.Empty,
                    AlternateAllele = c.Variant?.AlternateAllele ?? string.Empty,
                    Significance = c.Variant?.Significance ?? ClinicalSignificance.UncertainSignificance,
                    ReadDepth = c.ReadDepth,
                    QualityScore = c.QualityScore,
                    Zygosity = c.Zygosity,
                    ReleasedAt = c.ReleasedAt,
                    IsReleased = c.ReleasedAt.HasValue,
                })
                .ToList(),
            new Pagination
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            }
        );

        return Result<PagedData<VariantCallDto>>.Success(page);
    }

    public async Task<Result<VariantCallDto>> ReleaseAsync(Guid callId, CancellationToken ct = default)
    {
        // Enforced here as well as at the endpoint. The attribute keeps honest callers out; this
        // keeps the rule true for any future caller that reaches the service another way.
        if (_currentUser.Role != LabRole.PrincipalInvestigator)
            return Result<VariantCallDto>.Forbidden("Only a principal investigator may release a result.");

        var call = await _db
            .VariantCalls.Include(c => c.Variant)
            .Include(c => c.Sample)
            .Include(c => c.SequencingRun)
            .FirstOrDefaultAsync(c => c.Id == callId && !c.IsDeleted, ct);

        if (call is null)
            return Result<VariantCallDto>.NotFound("Variant call not found.");

        if (call.ReleasedAt.HasValue)
            return Result<VariantCallDto>.Conflict("This call has already been released.");

        // Results only leave the lab from a finished run. A call recorded against a run that
        // later failed is not a finding, it is an artefact.
        if (call.SequencingRun?.Status != RunStatus.Completed)
            return Result<VariantCallDto>.Conflict(
                "A call can only be released once its sequencing run has completed."
            );

        var now = _clock.UtcNow;
        call.ReleasedAt = now;
        call.ReleasedById = _currentUser.UserId;
        call.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        return Result<VariantCallDto>.Success(
            new VariantCallDto
            {
                Id = call.Id,
                SampleId = call.SampleId,
                Barcode = call.Sample?.Barcode ?? string.Empty,
                Gene = call.Variant?.Gene ?? string.Empty,
                Chromosome = call.Variant?.Chromosome ?? string.Empty,
                Position = call.Variant?.Position ?? 0,
                ReferenceAllele = call.Variant?.ReferenceAllele ?? string.Empty,
                AlternateAllele = call.Variant?.AlternateAllele ?? string.Empty,
                Significance = call.Variant?.Significance ?? ClinicalSignificance.UncertainSignificance,
                ReadDepth = call.ReadDepth,
                QualityScore = call.QualityScore,
                Zygosity = call.Zygosity,
                ReleasedAt = call.ReleasedAt,
                IsReleased = true,
            },
            "Result released."
        );
    }
}
