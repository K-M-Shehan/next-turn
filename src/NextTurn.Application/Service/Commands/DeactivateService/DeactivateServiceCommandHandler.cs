using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;

namespace NextTurn.Application.Service.Commands.DeactivateService;

public sealed class DeactivateServiceCommandHandler : IRequestHandler<DeactivateServiceCommand, Unit>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IApplicationDbContext _context;

    public DeactivateServiceCommandHandler(IServiceRepository serviceRepository, IApplicationDbContext context)
    {
        _serviceRepository = serviceRepository;
        _context = context;
    }

    public async Task<Unit> Handle(DeactivateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.OrganisationId, request.ServiceId, cancellationToken);
        if (service is null)
            throw new DomainException("Service not found.");

        service.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
