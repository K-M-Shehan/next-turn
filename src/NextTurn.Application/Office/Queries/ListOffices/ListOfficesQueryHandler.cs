using MediatR;
using NextTurn.Application.Office.Common;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Application.Office.Queries.ListOffices;

public sealed class ListOfficesQueryHandler : IRequestHandler<ListOfficesQuery, ListOfficesResult>
{
    private readonly IOfficeRepository _officeRepository;

    public ListOfficesQueryHandler(IOfficeRepository officeRepository)
    {
        _officeRepository = officeRepository;
    }

    public async Task<ListOfficesResult> Handle(ListOfficesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _officeRepository.ListAsync(
            request.OrganisationId,
            request.IsActive,
            request.Search,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return new ListOfficesResult(
            items.Select(Map).ToList(),
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    private static OfficeDto Map(OfficeEntity office) =>
        new(
            office.Id,
            office.Name,
            office.Address,
            office.Latitude,
            office.Longitude,
            office.OpeningHours,
            office.IsActive,
            office.DeactivatedAt,
            office.CreatedAt,
            office.UpdatedAt);
}
