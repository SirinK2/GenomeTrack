using System;
using System.Security.Claims;
using GenomeTrack.Application.Services.Interfaces;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.API.Authorization;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;

    public string DisplayName => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public LabRole Role =>
        Enum.TryParse<LabRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : LabRole.Technician;
}
