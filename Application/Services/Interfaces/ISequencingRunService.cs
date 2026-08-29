using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Run;
using GenomeTrack.Application.Response;

namespace GenomeTrack.Application.Services.Interfaces;

public interface ISequencingRunService
{
    Task<Result<SequencingRunDto>> CreateAsync(CreateRunDto dto, CancellationToken ct = default);
    Task<Result<SequencingRunDto>> LoadSampleAsync(Guid runId, LoadSampleDto dto, CancellationToken ct = default);
    Task<Result<SequencingRunDto>> StartAsync(Guid runId, CancellationToken ct = default);
    Task<Result<SequencingRunDto>> CompleteAsync(Guid runId, CompleteRunDto dto, CancellationToken ct = default);
    Task<Result<SequencingRunDto>> GetByIdAsync(Guid runId, CancellationToken ct = default);
}
