using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Service.Repositories;

namespace NextTurn.Application.Service.Commands.RemoveServiceOfficeAssignment;

public sealed class RemoveServiceOfficeAssignmentCommandHandler : IRequestHandler<RemoveServiceOfficeAssignmentCommand, Unit>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IApplicationDbContext _context;

    public RemoveServiceOfficeAssignmentCommandHandler(IServiceRepository serviceRepository, IApplicationDbContext context)
    {
        _serviceRepository = serviceRepository;
        _context = context;
    }

    public async Task<Unit> Handle(RemoveServiceOfficeAssignmentCommand request, CancellationToken cancellationToken)
    {
        await _serviceRepository.RemoveOfficeAssignmentAsync(
            request.OrganisationId,
            request.ServiceId,
            request.OfficeId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
