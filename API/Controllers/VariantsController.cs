using System;
using System.Threading;
using System.Threading.Tasks;
using GenomeTrack.API.Extensions;
using GenomeTrack.Application.Constants;
using GenomeTrack.Application.DTOs.Variant;
using GenomeTrack.Application.Response;
using GenomeTrack.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenomeTrack.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/variant-calls")]
public class VariantsController : ControllerBase
{
    private readonly IVariantService _variants;

    public VariantsController(IVariantService variants) => _variants = variants;

    [HttpGet]
    [ProducesResponseType(typeof(SuccessResult<PagedData<VariantCallDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] VariantCallFilter filter, CancellationToken ct) =>
        (await _variants.SearchAsync(filter, ct)).ToActionResult();

    /// <summary>
    /// Releases a provisional call. Restricted to the principal investigator both here and in
    /// the service — the endpoint keeps honest callers out, the service keeps the rule true.
    /// </summary>
    [HttpPost("{callId:guid}/release")]
    [Authorize(Policy = LabPolicy.PrincipalInvestigatorOnly)]
    [ProducesResponseType(typeof(SuccessResult<VariantCallDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(FailureResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Release(Guid callId, CancellationToken ct) =>
        (await _variants.ReleaseAsync(callId, ct)).ToActionResult();
}
