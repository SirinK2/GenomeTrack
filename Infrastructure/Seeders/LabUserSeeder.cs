using System;
using System.Linq;
using System.Threading.Tasks;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Domain.Entity;
using GenomeTrack.Domain.Enums;
using GenomeTrack.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Infrastructure.Seeders;

/// <summary>
/// Seeds one account per role so the API is explorable the moment it starts. The passwords are
/// deliberately obvious and the seeder refuses to run outside Development — a demo credential
/// that reaches production is a back door, not a convenience.
/// </summary>
public static class LabUserSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, bool isDevelopment)
    {
        if (!isDevelopment)
            return;

        if (await db.LabUsers.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;

        var users = new[]
        {
            new LabUser
            {
                Email = "tech@genometrack.local",
                DisplayName = "Tala the Technician",
                Role = LabRole.Technician,
                PasswordHash = hasher.Hash("Passw0rd!"),
                CreatedAt = now,
            },
            new LabUser
            {
                Email = "analyst@genometrack.local",
                DisplayName = "Amir the Analyst",
                Role = LabRole.Analyst,
                PasswordHash = hasher.Hash("Passw0rd!"),
                CreatedAt = now,
            },
            new LabUser
            {
                Email = "pi@genometrack.local",
                DisplayName = "Dr. Pinar",
                Role = LabRole.PrincipalInvestigator,
                PasswordHash = hasher.Hash("Passw0rd!"),
                CreatedAt = now,
            },
        };

        db.LabUsers.AddRange(users);
        await db.SaveChangesAsync();
    }
}
