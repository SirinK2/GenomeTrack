using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.API.Extensions;
using GenomeTrack.Application.Constants;
using GenomeTrack.Application.DTOs.Run;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenomeTrack.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/runs")]
public class RunsController : ControllerBase
{
    private readonly ISequencingRunService _runs;

    public RunsController(ISequencingRunService runs) => _runs = runs;

    [HttpPost]
    [Authorize(Policy = LabPolicy.AnalystOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<SequencingRunDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateRunDto dto, CancellationToken ct) =>
        (await _runs.CreateAsync(dto, ct)).ToActionResult();

    [HttpPost("{runId:guid}/samples")]
    [Authorize(Policy = LabPolicy.AnalystOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<SequencingRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LoadSample(
        Guid runId,
        [FromBody] LoadSampleDto dto,
        CancellationToken ct
    ) => (await _runs.LoadSampleAsync(runId, dto, ct)).ToActionResult();

    [HttpPost("{runId:guid}/start")]
    [Authorize(Policy = LabPolicy.AnalystOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<SequencingRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Start(Guid runId, CancellationToken ct) =>
        (await _runs.StartAsync(runId, ct)).ToActionResult();

    [HttpPost("{runId:guid}/complete")]
    [Authorize(Policy = LabPolicy.AnalystOrAbove)]
    [ProducesResponseType(typeof(SuccessResult<SequencingRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete(
        Guid runId,
        [FromBody] CompleteRunDto dto,
        CancellationToken ct
    ) => (await _runs.CompleteAsync(runId, dto, ct)).ToActionResult();

    [HttpGet("{runId:guid}")]
    [ProducesResponseType(typeof(SuccessResult<SequencingRunDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid runId, CancellationToken ct) =>
        (await _runs.GetByIdAsync(runId, ct)).ToActionResult();
}
