using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Office.Common;
using NextTurn.Domain.Office.Repositories;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Application.Office.Commands.CreateOffice;

public sealed class CreateOfficeCommandHandler : IRequestHandler<CreateOfficeCommand, OfficeDto>
{
    private readonly IOfficeRepository _officeRepository;
    private readonly IApplicationDbContext _context;

    public CreateOfficeCommandHandler(
        IOfficeRepository officeRepository,
        IApplicationDbContext context)
    {
        _officeRepository = officeRepository;
        _context = context;
    }

    public async Task<OfficeDto> Handle(CreateOfficeCommand request, CancellationToken cancellationToken)
    {
        var office = OfficeEntity.Create(
            request.OrganisationId,
            request.Name,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.OpeningHours);

        await _officeRepository.AddAsync(office, cancellationToken);
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
