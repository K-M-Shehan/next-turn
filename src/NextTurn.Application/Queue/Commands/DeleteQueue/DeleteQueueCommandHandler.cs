using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Domain.Common;
using NextTurn.Domain.Queue.Repositories;

namespace NextTurn.Application.Queue.Commands.DeleteQueue;

public sealed class DeleteQueueCommandHandler : IRequestHandler<DeleteQueueCommand, Unit>
{
    private readonly IQueueRepository _queueRepository;
    private readonly IApplicationDbContext _context;

    public DeleteQueueCommandHandler(
        IQueueRepository queueRepository,
        IApplicationDbContext context)
    {
        _queueRepository = queueRepository;
        _context = context;
    }

    public async Task<Unit> Handle(DeleteQueueCommand request, CancellationToken cancellationToken)
    {
        var queue = await _queueRepository.GetByIdAsync(request.QueueId, cancellationToken);
        if (queue is null || queue.OrganisationId != request.OrganisationId)
            throw new DomainException("Queue not found.");

        var deleted = await _queueRepository.DeleteQueueAsync(request.QueueId, cancellationToken);
        if (!deleted)
            throw new DomainException("Queue not found.");

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
