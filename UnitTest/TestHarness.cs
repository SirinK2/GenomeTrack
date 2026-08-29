using System;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Domain.Enums;
using GenomeTrack.Infrastructure.Repository;
using GenomeTrack.Infrastructure.Service;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.UnitTest;

/// <summary>
/// A fresh in-memory context per test, plus the small collaborators the services need. Time is
/// fixed and advanced by hand so custody hashes are reproducible.
/// </summary>
public sealed class TestHarness : IDisposable
{
    public AppDbContext Db { get; }
    public FakeClock Clock { get; } = new();
    public FakeCurrentUser CurrentUser { get; } = new();
    public HashChain HashChain { get; } = new();

    public TestHarness()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"genometrack-{Guid.NewGuid():N}")
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)
            )
            .Options;

        Db = new AppDbContext(options);

        // Custody events have a required Actor and the verifier includes it, so the acting user
        // has to exist for the chain queries to return anything meaningful.
        Db.LabUsers.Add(
            new GenomeTrack.Domain.Entity.LabUser
            {
                Id = CurrentUser.UserId,
                Email = "test@genometrack.local",
                DisplayName = CurrentUser.DisplayName,
                Role = CurrentUser.Role,
                PasswordHash = "not-a-real-hash",
                CreatedAt = Clock.UtcNow,
            }
        );
        Db.SaveChanges();
    }

    public void Dispose() => Db.Dispose();
}

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; } =
        new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);

    /// <summary>Adds sub-millisecond ticks, the way a real wall clock would.</summary>
    public void SetTicksWithin(long ticks) => UtcNow = UtcNow.AddTicks(ticks);
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string DisplayName { get; set; } = "Test Technician";
    public LabRole Role { get; set; } = LabRole.Technician;
    public bool IsAuthenticated => true;
}
