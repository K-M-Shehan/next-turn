using System.Security.Cryptography;
using System.Text;
using MediatR;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Staff.Common;
using NextTurn.Domain.Auth;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.Application.Staff.Commands.CreateStaff;

public sealed class CreateStaffCommandHandler : IRequestHandler<CreateStaffCommand, StaffDto>
{
    private static readonly TimeSpan InviteTtl = TimeSpan.FromDays(2);

    private readonly IUserRepository _userRepository;
    private readonly ITenantContext _tenantContext;

    public CreateStaffCommandHandler(IUserRepository userRepository, ITenantContext tenantContext)
    {
        _userRepository = userRepository;
        _tenantContext = tenantContext;
    }

    public async Task<StaffDto> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);
        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            throw new DomainException("Email address is already in use.");

        var officesExist = await _userRepository.OfficesExistAsync(request.OfficeIds, cancellationToken);
        if (!officesExist)
            throw new DomainException("One or more offices were not found for this tenant.");

        var bootstrapPasswordHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var staffUser = User.Create(
            _tenantContext.TenantId,
            request.Name,
            email,
            request.Phone,
            bootstrapPasswordHash,
            UserRole.Staff);

        staffUser.UpdateStaffProfile(
            request.Name,
            request.Phone,
            request.CounterName,
            request.ShiftStart,
            request.ShiftEnd);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        staffUser.StartStaffInvite(ComputeSha256(rawToken), DateTimeOffset.UtcNow.Add(InviteTtl));

        await _userRepository.AddAsync(staffUser, cancellationToken);
        await _userRepository.ReplaceStaffOfficeAssignmentsAsync(staffUser.Id, request.OfficeIds, cancellationToken);

        return new StaffDto(
            staffUser.Id,
            staffUser.Name,
            staffUser.Email.Value,
            staffUser.Phone,
            staffUser.IsActive,
            staffUser.CounterName,
            ToShiftString(staffUser.ShiftStart),
            ToShiftString(staffUser.ShiftEnd),
            request.OfficeIds.Distinct().ToList(),
            staffUser.CreatedAt);
    }

    private static string? ToShiftString(TimeSpan? value)
    {
        return value.HasValue ? value.Value.ToString(@"hh\:mm") : null;
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
