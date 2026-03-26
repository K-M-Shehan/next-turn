using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTurn.Domain.Common;
using NextTurn.API.Models.Staff;
using NextTurn.Application.Staff.Commands.CreateStaff;
using NextTurn.Application.Staff.Commands.DeactivateStaff;
using NextTurn.Application.Staff.Commands.UpdateStaff;
using NextTurn.Application.Staff.Queries.ListStaff;

namespace NextTurn.API.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "OrgAdmin,SystemAdmin")]
public sealed class StaffController : ControllerBase
{
    private readonly ISender _sender;

    public StaffController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateStaffRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateStaffCommand(
            request.Name,
            request.Email,
            request.Phone,
            request.OfficeIds,
            request.CounterName,
            ParseShift(request.ShiftStart),
            ParseShift(request.ShiftEnd));

        var result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), null, result);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> List(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListStaffQuery(pageNumber, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStaffRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateStaffCommand(
            id,
            request.Name,
            request.Phone,
            request.OfficeIds,
            request.CounterName,
            ParseShift(request.ShiftStart),
            ParseShift(request.ShiftEnd));

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateStaffCommand(id), cancellationToken);
        return NoContent();
    }

    private static TimeSpan? ParseShift(string? shift)
    {
        if (string.IsNullOrWhiteSpace(shift))
            return null;

        if (!TimeSpan.TryParse(shift, out var value))
            throw new DomainException("Shift time must be a valid time value.");

        return value;
    }
}
