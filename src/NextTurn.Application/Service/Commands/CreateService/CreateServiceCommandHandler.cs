using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Service.Common;
using NextTurn.Domain.Common;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.Application.Service.Commands.CreateService;

public sealed class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IApplicationDbContext _context;

    public CreateServiceCommandHandler(IServiceRepository serviceRepository, IApplicationDbContext context)
    {
        _serviceRepository = serviceRepository;
        _context = context;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var exists = await _serviceRepository.ExistsByCodeAsync(request.OrganisationId, normalizedCode, cancellationToken);
        if (exists)
            throw new ConflictDomainException("Service code already exists in this tenant.");

        var service = ServiceEntity.Create(
            request.OrganisationId,
            request.Name,
            request.Code,
            request.Description,
            request.EstimatedDurationMinutes,
            request.IsActive);

        await _serviceRepository.AddAsync(service, cancellationToken);
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
