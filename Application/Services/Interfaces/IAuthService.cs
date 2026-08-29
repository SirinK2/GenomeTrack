using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Auth;
using GenomeTrack.Application.Response;

namespace GenomeTrack.Application.Services.Interfaces;

public interface IAuthService
{
    Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
}

public interface ITokenIssuer
{
    (string Token, int ExpiresInSeconds) Issue(GenomeTrack.Domain.Entity.LabUser user);
}
