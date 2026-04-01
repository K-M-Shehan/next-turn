using Microsoft.EntityFrameworkCore;
using NextTurn.Infrastructure.Persistence;
using NextTurn.Domain.Service.Entities;
using NextTurn.Domain.Service.Repositories;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.Infrastructure.Service;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByCodeAsync(Guid organisationId, string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return await _context.Services
            .AnyAsync(x =>
                x.OrganisationId == organisationId &&
                x.Code == normalizedCode,
                cancellationToken);
    }

    public async Task<ServiceEntity?> GetByIdAsync(Guid organisationId, Guid serviceId, CancellationToken cancellationToken)
    {
        return await _context.Services
            .FirstOrDefaultAsync(x => x.OrganisationId == organisationId && x.Id == serviceId, cancellationToken);
    }

    public async Task<(IReadOnlyList<ServiceEntity> Items, int TotalCount)> ListAsync(
        Guid organisationId,
        bool activeOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Services
            .AsNoTracking()
            .Where(x => x.OrganisationId == organisationId);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetAssignedOfficeIdsByServiceIdsAsync(
        Guid organisationId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<Guid>>();

        var assignments = await _context.ServiceOfficeAssignments
            .AsNoTracking()
            .Where(x => x.OrganisationId == organisationId && serviceIds.Contains(x.ServiceId))
            .ToListAsync(cancellationToken);

        return assignments
            .GroupBy(x => x.ServiceId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Guid>)x.Select(a => a.OfficeId).ToList());
    }

    public async Task AddAsync(ServiceEntity service, CancellationToken cancellationToken)
    {
        await _context.Services.AddAsync(service, cancellationToken);
    }

    public async Task AssignOfficesAsync(Guid organisationId, Guid serviceId, IReadOnlyCollection<Guid> officeIds, CancellationToken cancellationToken)
    {
        if (officeIds.Count == 0)
            return;

        var existingOfficeIds = await _context.ServiceOfficeAssignments
            .Where(x => x.OrganisationId == organisationId && x.ServiceId == serviceId)
            .Select(x => x.OfficeId)
            .ToListAsync(cancellationToken);

        var newOfficeIds = officeIds
            .Where(id => !existingOfficeIds.Contains(id))
            .Distinct()
            .ToList();

        foreach (var officeId in newOfficeIds)
        {
            await _context.ServiceOfficeAssignments.AddAsync(
                ServiceOfficeAssignment.Create(serviceId, officeId, organisationId),
                cancellationToken);
        }
    }

    public async Task RemoveOfficeAssignmentAsync(Guid organisationId, Guid serviceId, Guid officeId, CancellationToken cancellationToken)
    {
        var assignment = await _context.ServiceOfficeAssignments
            .FirstOrDefaultAsync(x =>
                x.OrganisationId == organisationId &&
                x.ServiceId == serviceId &&
                x.OfficeId == officeId,
                cancellationToken);

        if (assignment is null)
            return;

        _context.ServiceOfficeAssignments.Remove(assignment);
    }
}
