using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;

namespace NextTurn.Application.Service.Commands.AssignServiceOffices;

public sealed class AssignServiceOfficesCommandHandler : IRequestHandler<AssignServiceOfficesCommand, Unit>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IApplicationDbContext _context;

    public AssignServiceOfficesCommandHandler(IServiceRepository serviceRepository, IApplicationDbContext context)
    {
        _serviceRepository = serviceRepository;
        _context = context;
    }

    public async Task<Unit> Handle(AssignServiceOfficesCommand request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.OrganisationId, request.ServiceId, cancellationToken);
        if (service is null)
            throw new DomainException("Service not found.");

        await _serviceRepository.AssignOfficesAsync(
            request.OrganisationId,
            request.ServiceId,
            request.OfficeIds.Distinct().ToList(),
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
