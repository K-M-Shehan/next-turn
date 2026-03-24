using Microsoft.EntityFrameworkCore;
using NextTurn.Domain.Office.Repositories;
using NextTurn.Infrastructure.Persistence;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Infrastructure.Office;

public sealed class OfficeRepository : IOfficeRepository
{
    private readonly ApplicationDbContext _context;

    public OfficeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OfficeEntity office, CancellationToken cancellationToken)
    {
        await _context.Offices.AddAsync(office, cancellationToken);
    }

    public async Task<OfficeEntity?> GetByIdAsync(Guid organisationId, Guid officeId, CancellationToken cancellationToken)
    {
        return await _context.Offices
            .FirstOrDefaultAsync(
                x => x.OrganisationId == organisationId && x.Id == officeId,
                cancellationToken);
    }

    public async Task<(IReadOnlyList<OfficeEntity> Items, int TotalCount)> ListAsync(
        Guid organisationId,
        bool? isActive,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Offices
            .Where(x => x.OrganisationId == organisationId)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(normalized) ||
                x.Address.Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
