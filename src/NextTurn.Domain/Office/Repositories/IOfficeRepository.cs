using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Domain.Office.Repositories;

public interface IOfficeRepository
{
    Task AddAsync(OfficeEntity office, CancellationToken cancellationToken);

    Task<OfficeEntity?> GetByIdAsync(Guid organisationId, Guid officeId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OfficeEntity> Items, int TotalCount)> ListAsync(
        Guid organisationId,
        bool? isActive,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
