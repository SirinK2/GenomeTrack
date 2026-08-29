using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Application.Services.Persistence;

public interface IAppDbContext
{
    DbSet<LabUser> LabUsers { get; }
    DbSet<Sample> Samples { get; }
    DbSet<CustodyEvent> CustodyEvents { get; }
    DbSet<SequencingRun> SequencingRuns { get; }
    DbSet<RunSample> RunSamples { get; }
    DbSet<Variant> Variants { get; }
    DbSet<VariantCall> VariantCalls { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
