using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.DTOs.Auth;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public LabRole Role { get; set; }
    public int ExpiresInSeconds { get; set; }
}
