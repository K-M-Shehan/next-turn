using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Office.Common;
using NextTurn.Domain.Common;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Application.Office.Commands.UpdateOffice;

public sealed class UpdateOfficeCommandHandler : IRequestHandler<UpdateOfficeCommand, OfficeDto>
{
    private readonly IOfficeRepository _officeRepository;
    private readonly IApplicationDbContext _context;

    public UpdateOfficeCommandHandler(IOfficeRepository officeRepository, IApplicationDbContext context)
    {
        _officeRepository = officeRepository;
        _context = context;
    }

    public async Task<OfficeDto> Handle(UpdateOfficeCommand request, CancellationToken cancellationToken)
    {
        var office = await _officeRepository.GetByIdAsync(request.OrganisationId, request.OfficeId, cancellationToken);
        if (office is null)
            throw new DomainException("Office not found.");

        office.UpdateDetails(
            request.Name,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.OpeningHours);

        await _context.SaveChangesAsync(cancellationToken);

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
