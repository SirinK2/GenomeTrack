using System;
using GenomeTrack.Application.Services.Interfaces;

namespace GenomeTrack.Infrastructure.Service;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
