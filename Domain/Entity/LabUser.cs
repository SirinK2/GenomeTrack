using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Domain.Entity;

public class LabUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public LabRole Role { get; set; }
    public bool IsActive { get; set; } = true;
}
