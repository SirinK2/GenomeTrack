using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.Application.DTOs.Custody;
using GenomeTrack.Application.Response;

namespace GenomeTrack.Application.Services.Interfaces;

public interface ICustodyService
{
    Task<Result<CustodyEventDto>> AppendAsync(Guid sampleId, TransferCustodyDto dto, CancellationToken ct = default);
    Task<Result<ChainVerificationDto>> VerifyChainAsync(Guid sampleId, CancellationToken ct = default);
}
