using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.API.Models.Offices;
using NextTurn.Application.Office.Commands.CreateOffice;
using NextTurn.Application.Office.Commands.DeactivateOffice;
using NextTurn.Application.Office.Commands.UpdateOffice;
using NextTurn.Application.Office.Queries.GetOfficeById;
using NextTurn.Application.Office.Queries.ListOffices;

namespace NextTurn.API.Controllers;

[ApiController]
[Route("api/offices")]
[Authorize(Roles = "OrgAdmin,SystemAdmin")]
public sealed class OfficesController : ControllerBase
{
    private readonly ISender _sender;

    public OfficesController(ISender sender)
    {
        _sender = sender;
    }

    private bool TryGetOrganisationId(out Guid organisationId)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        return Guid.TryParse(tenantIdClaim, out organisationId) && organisationId != Guid.Empty;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOfficeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new CreateOfficeCommand(
            organisationId,
            request.Name,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.OpeningHours);

        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { officeId = result.OfficeId }, result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> List(
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var query = new ListOfficesQuery(organisationId, isActive, search, pageNumber, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{officeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetById(Guid officeId, CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var query = new GetOfficeByIdQuery(organisationId, officeId);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{officeId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid officeId,
        [FromBody] UpdateOfficeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new UpdateOfficeCommand(
            organisationId,
            officeId,
            request.Name,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.OpeningHours);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{officeId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid officeId, CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new DeactivateOfficeCommand(organisationId, officeId);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
}
