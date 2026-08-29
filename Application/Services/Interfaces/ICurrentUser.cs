using System;
using GenomeTrack.Domain.Enums;

namespace GenomeTrack.Application.Services.Interfaces;

/// <summary>
/// The caller, as the application layer sees them. Keeping this an interface is what lets the
/// services be tested without an HTTP context, and what keeps <c>HttpContextAccessor</c> out of
/// the Application project entirely.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    string DisplayName { get; }
    LabRole Role { get; }
    bool IsAuthenticated { get; }
}
