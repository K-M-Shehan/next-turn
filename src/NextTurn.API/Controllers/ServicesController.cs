using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.API.Models.Services;
using NextTurn.Application.Service.Commands.AssignServiceOffices;
using NextTurn.Application.Service.Commands.CreateService;
using NextTurn.Application.Service.Commands.DeactivateService;
using NextTurn.Application.Service.Commands.RemoveServiceOfficeAssignment;
using NextTurn.Application.Service.Commands.UpdateService;
using NextTurn.Application.Service.Queries.ListServices;

namespace NextTurn.API.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public sealed class ServicesController : ControllerBase
{
    private readonly ISender _sender;

    public ServicesController(ISender sender)
    {
        _sender = sender;
    }

    private bool TryGetOrganisationId(out Guid organisationId)
    {
        var tenantIdClaim = User.FindFirstValue("tid");
        return Guid.TryParse(tenantIdClaim, out organisationId) && organisationId != Guid.Empty;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> List(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var query = new ListServicesQuery(organisationId, activeOnly, pageNumber, pageSize);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateServiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new CreateServiceCommand(
            organisationId,
            request.Name,
            request.Code,
            request.Description,
            request.EstimatedDurationMinutes,
            request.IsActive);

        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), null, result);
    }

    [HttpPut("{serviceId:guid}")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid serviceId,
        [FromBody] UpdateServiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new UpdateServiceCommand(
            organisationId,
            serviceId,
            request.Name,
            request.Description,
            request.EstimatedDurationMinutes);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{serviceId:guid}/deactivate")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid serviceId, CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new DeactivateServiceCommand(organisationId, serviceId);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    [HttpPost("{serviceId:guid}/offices")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignOffices(
        Guid serviceId,
        [FromBody] AssignServiceOfficesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new AssignServiceOfficesCommand(organisationId, serviceId, request.OfficeIds);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{serviceId:guid}/offices/{officeId:guid}")]
    [Authorize(Roles = "OrgAdmin,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RemoveOfficeAssignment(
        Guid serviceId,
        Guid officeId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOrganisationId(out var organisationId))
            return Unauthorized();

        var command = new RemoveServiceOfficeAssignmentCommand(organisationId, serviceId, officeId);
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
}
