using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Auth;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Application.Services.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenomeTrack.Application.Services.Implementation;

public class AuthService : IAuthService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenIssuer _tokens;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenIssuer tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = dto.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        var user = await _db.LabUsers.FirstOrDefaultAsync(
            u => u.Email == email && !u.IsDeleted,
            ct
        );

        // One message for both "no such user" and "wrong password". Distinguishing them turns
        // the login form into a directory of who works here.
        if (user is null || !user.IsActive || !_hasher.Verify(dto.Password, user.PasswordHash))
            return Result<AuthTokenDto>.Failure("Email or password is incorrect.");

        var (token, expiresIn) = _tokens.Issue(user);

        return Result<AuthTokenDto>.Success(
            new AuthTokenDto
            {
                AccessToken = token,
                DisplayName = user.DisplayName,
                Role = user.Role,
                ExpiresInSeconds = expiresIn,
            }
        );
    }
}
