using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.Domain.Service.Repositories;

public interface IServiceRepository
{
    Task AddAsync(ServiceEntity service, CancellationToken cancellationToken);

    Task<ServiceEntity?> GetByIdAsync(Guid organisationId, Guid serviceId, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(Guid organisationId, string code, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ServiceEntity> Items, int TotalCount)> ListAsync(
        Guid organisationId,
        bool activeOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetAssignedOfficeIdsByServiceIdsAsync(
        Guid organisationId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken);

    Task AssignOfficesAsync(Guid organisationId, Guid serviceId, IReadOnlyCollection<Guid> officeIds, CancellationToken cancellationToken);

    Task RemoveOfficeAssignmentAsync(Guid organisationId, Guid serviceId, Guid officeId, CancellationToken cancellationToken);
}
