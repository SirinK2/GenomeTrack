using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.Services.Persistence;
using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Infrastructure.Repository;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<LabUser> LabUsers => Set<LabUser>();
    public DbSet<Sample> Samples => Set<Sample>();
    public DbSet<CustodyEvent> CustodyEvents => Set<CustodyEvent>();
    public DbSet<SequencingRun> SequencingRuns => Set<SequencingRun>();
    public DbSet<RunSample> RunSamples => Set<RunSample>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<VariantCall> VariantCalls => Set<VariantCall>();

    /// <summary>
    /// No global soft-delete query filters. They look like a safety net and behave like one
    /// until a required navigation crosses them: EF turns the filter into an inner join, so a
    /// soft-deleted actor silently removes every custody event that references them. In an
    /// audit domain a vanished audit row is the worst possible failure, and it fails quietly.
    /// Every service filters <c>IsDeleted</c> explicitly instead, which is visible at the call
    /// site and cannot amputate a chain as a side effect.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }

    /// <summary>
    /// Custody events are append-only, and this is where that stops being a convention and
    /// becomes a rule. Anything that reaches SaveChanges holding a modified or deleted custody
    /// row is rejected outright — a caller that wants to correct a mistake appends a correcting
    /// event, which is what an auditor expects to see anyway.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var tampered = ChangeTracker
            .Entries<CustodyEvent>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (tampered)
            throw new InvalidOperationException(
                "Custody events are append-only. Append a correcting event instead of editing history."
            );

        return base.SaveChangesAsync(cancellationToken);
    }
}
