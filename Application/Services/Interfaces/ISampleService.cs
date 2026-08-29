using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Sample;
using GenomeTrack.Application.Response;

namespace GenomeTrack.Application.Services.Interfaces;

public interface ISampleService
{
    Task<Result<SampleDto>> RegisterAsync(RegisterSampleDto dto, CancellationToken ct = default);
    Task<Result<SampleDto>> AccessionAsync(Guid sampleId, AccessionSampleDto dto, CancellationToken ct = default);
    Task<Result<SampleDto>> GetByIdAsync(Guid sampleId, CancellationToken ct = default);
    Task<Result<PagedData<SampleDto>>> SearchAsync(SampleFilter filter, CancellationToken ct = default);
}
