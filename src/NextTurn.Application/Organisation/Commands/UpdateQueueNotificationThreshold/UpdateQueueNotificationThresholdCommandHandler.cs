using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Organisation.Commands.UpdateQueueNotificationThreshold;

public sealed class UpdateQueueNotificationThresholdCommandHandler
    : IRequestHandler<UpdateQueueNotificationThresholdCommand, Unit>
{
    private readonly IApplicationDbContext _context;

    public UpdateQueueNotificationThresholdCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateQueueNotificationThresholdCommand request, CancellationToken cancellationToken)
    {
        var organisation = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == request.OrganisationId, cancellationToken);

        if (organisation is null)
            throw new DomainException("Organisation not found.");

        organisation.SetQueueNotificationThreshold(request.QueueNotificationThreshold);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
