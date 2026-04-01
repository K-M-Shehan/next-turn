using MediatR;

namespace NextTurn.Application.Staff.Queries.ListStaff;

public sealed record ListStaffQuery(int PageNumber, int PageSize) : IRequest<ListStaffResult>;
