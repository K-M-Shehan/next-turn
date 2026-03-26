using MediatR;
using NextTurn.Application.Service.Common;
using NextTurn.Domain.Service.Repositories;

namespace NextTurn.Application.Service.Queries.ListServices;

public sealed class ListServicesQueryHandler : IRequestHandler<ListServicesQuery, ListServicesResult>
{
    private readonly IServiceRepository _serviceRepository;

    public ListServicesQueryHandler(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public async Task<ListServicesResult> Handle(ListServicesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _serviceRepository.ListAsync(
            request.OrganisationId,
            request.ActiveOnly,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var assignmentMap = await _serviceRepository.GetAssignedOfficeIdsByServiceIdsAsync(
            request.OrganisationId,
            items.Select(x => x.Id).ToList(),
            cancellationToken);

        var data = items.Select(service => new ServiceDto(
            service.Id,
            service.Name,
            service.Code,
            service.Description,
            service.EstimatedDurationMinutes,
            service.IsActive,
            assignmentMap.TryGetValue(service.Id, out var assignedOfficeIds) ? assignedOfficeIds : [],
            service.DeactivatedAt,
            service.CreatedAt,
            service.UpdatedAt)).ToList();

        return new ListServicesResult(data, request.PageNumber, request.PageSize, totalCount);
    }
}
