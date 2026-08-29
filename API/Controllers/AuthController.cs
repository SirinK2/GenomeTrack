using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.API.Extensions;
using GenomeTrack.Application.DTOs.Auth;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenomeTrack.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SuccessResult<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct) =>
        (await _auth.LoginAsync(dto, ct)).ToActionResult();
}
