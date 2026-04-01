using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Common;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.Application.Service.Commands.UpdateService;

public sealed class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IApplicationDbContext _context;

    public UpdateServiceCommandHandler(IServiceRepository serviceRepository, IApplicationDbContext context)
    {
        _serviceRepository = serviceRepository;
        _context = context;
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdAsync(request.OrganisationId, request.ServiceId, cancellationToken);
        if (service is null)
            throw new DomainException("Service not found.");

        service.UpdateDetails(request.Name, request.Description, request.EstimatedDurationMinutes);
        await _context.SaveChangesAsync(cancellationToken);

        return Map(service);
    }

    private static ServiceDto Map(ServiceEntity service) =>
        new(
            service.Id,
            service.Name,
            service.Code,
            service.Description,
            service.EstimatedDurationMinutes,
            service.IsActive,
            [],
            service.DeactivatedAt,
            service.CreatedAt,
            service.UpdatedAt);
}
