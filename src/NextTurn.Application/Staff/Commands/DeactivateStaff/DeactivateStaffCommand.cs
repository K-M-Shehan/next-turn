using MediatR;

namespace NextTurn.Application.Staff.Commands.DeactivateStaff;

public sealed record DeactivateStaffCommand(Guid StaffUserId) : IRequest<Unit>;
