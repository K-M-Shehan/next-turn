using MediatR;
using Microsoft.EntityFrameworkCore;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Organisation.Queries.GetQueueNotificationThreshold;

public sealed class GetQueueNotificationThresholdQueryHandler
    : IRequestHandler<GetQueueNotificationThresholdQuery, QueueNotificationThresholdResult>
{
    private readonly IApplicationDbContext _context;

    public GetQueueNotificationThresholdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QueueNotificationThresholdResult> Handle(
        GetQueueNotificationThresholdQuery request,
        CancellationToken cancellationToken)
    {
        var organisation = await _context.Organisations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganisationId, cancellationToken);

        if (organisation is null)
            throw new DomainException("Organisation not found.");

        return new QueueNotificationThresholdResult(organisation.QueueNotificationThreshold);
    }
}
