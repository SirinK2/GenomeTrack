using GenomeTrack.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenomeTrack.Infrastructure.Mappings;

public class LabUserConfiguration : IEntityTypeConfiguration<LabUser>
{
    public void Configure(EntityTypeBuilder<LabUser> builder)
    {
        builder.ToTable("lab_users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
