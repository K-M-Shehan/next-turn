using MediatR;
using NextTurn.Application.Office.Common;
using NextTurn.Domain.Common;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Application.Office.Queries.GetOfficeById;

public sealed class GetOfficeByIdQueryHandler : IRequestHandler<GetOfficeByIdQuery, OfficeDto>
{
    private readonly IOfficeRepository _officeRepository;

    public GetOfficeByIdQueryHandler(IOfficeRepository officeRepository)
    {
        _officeRepository = officeRepository;
    }

    public async Task<OfficeDto> Handle(GetOfficeByIdQuery request, CancellationToken cancellationToken)
    {
        var office = await _officeRepository.GetByIdAsync(request.OrganisationId, request.OfficeId, cancellationToken);
        if (office is null)
            throw new DomainException("Office not found.");

        return Map(office);
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
