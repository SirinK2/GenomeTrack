using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Variant;
using GenomeTrack.Application.Response;

namespace GenomeTrack.Application.Services.Interfaces;

public interface IVariantService
{
    Task<Result<PagedData<VariantCallDto>>> SearchAsync(VariantCallFilter filter, CancellationToken ct = default);
    Task<Result<VariantCallDto>> ReleaseAsync(Guid callId, CancellationToken ct = default);
}
