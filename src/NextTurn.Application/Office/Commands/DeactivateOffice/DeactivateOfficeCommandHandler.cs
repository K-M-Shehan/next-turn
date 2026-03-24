using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;
using NextTurn.Domain.Office.Repositories;

namespace NextTurn.Application.Office.Commands.DeactivateOffice;

public sealed class DeactivateOfficeCommandHandler : IRequestHandler<DeactivateOfficeCommand, Unit>
{
    private readonly IOfficeRepository _officeRepository;
    private readonly IApplicationDbContext _context;

    public DeactivateOfficeCommandHandler(IOfficeRepository officeRepository, IApplicationDbContext context)
    {
        _officeRepository = officeRepository;
        _context = context;
    }

    public async Task<Unit> Handle(DeactivateOfficeCommand request, CancellationToken cancellationToken)
    {
        var office = await _officeRepository.GetByIdAsync(request.OrganisationId, request.OfficeId, cancellationToken);
        if (office is null)
            throw new DomainException("Office not found.");

        office.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
