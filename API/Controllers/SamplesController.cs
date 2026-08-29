using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.API.Extensions;
using GenomeTrack.Application.Constants;
using GenomeTrack.Application.DTOs.Custody;
using GenomeTrack.Application.DTOs.Sample;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenomeTrack.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/samples")]
public class SamplesController : ControllerBase
{
    private readonly ISampleService _samples;
    private readonly ICustodyService _custody;

    public SamplesController(ISampleService samples, ICustodyService custody)
    {
        _samples = samples;
        _custody = custody;
    }

    [HttpPost]
    [Authorize(Policy = LabPolicy.TechnicianOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<SampleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterSampleDto dto, CancellationToken ct) =>
        (await _samples.RegisterAsync(dto, ct)).ToActionResult();

    [HttpPost("{sampleId:guid}/accession")]
    [Authorize(Policy = LabPolicy.TechnicianOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<SampleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accession(
        Guid sampleId,
        [FromBody] AccessionSampleDto dto,
        CancellationToken ct
    ) => (await _samples.AccessionAsync(sampleId, dto, ct)).ToActionResult();

    [HttpGet("{sampleId:guid}")]
    [ProducesResponseType(typeof(SuccessResult<SampleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid sampleId, CancellationToken ct) =>
        (await _samples.GetByIdAsync(sampleId, ct)).ToActionResult();

    [HttpGet]
    [ProducesResponseType(typeof(SuccessResult<PagedData<SampleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SampleFilter filter, CancellationToken ct) =>
        (await _samples.SearchAsync(filter, ct)).ToActionResult();

    [HttpPost("{sampleId:guid}/custody")]
    [Authorize(Policy = LabPolicy.TechnicianOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<CustodyEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Transfer(
        Guid sampleId,
        [FromBody] TransferCustodyDto dto,
        CancellationToken ct
    ) => (await _custody.AppendAsync(sampleId, dto, ct)).ToActionResult();

    /// <summary>
    /// Re-hashes the sample's chain and reports whether it still verifies. Answers 200 with
    /// <c>isIntact: false</c> when it does not — a broken chain is a finding to act on, not a
    /// failed request.
    /// </summary>
    [HttpGet("{sampleId:guid}/custody")]
    [ProducesResponseType(typeof(SuccessResult<ChainVerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyChain(Guid sampleId, CancellationToken ct) =>
        (await _custody.VerifyChainAsync(sampleId, ct)).ToActionResult();
}
