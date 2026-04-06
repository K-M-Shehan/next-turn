using NextTurn.Domain.Common;

namespace NextTurn.Domain.Appointment.Entities;

/// <summary>
/// Assignment of a staff user to an appointment profile they can operate.
/// </summary>
public sealed class AppointmentProfileStaffAssignment
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid AppointmentProfileId { get; private set; }
    public Guid StaffUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private AppointmentProfileStaffAssignment()
    {
    }

    private AppointmentProfileStaffAssignment(
        Guid id,
        Guid organisationId,
        Guid appointmentProfileId,
        Guid staffUserId,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrganisationId = organisationId;
        AppointmentProfileId = appointmentProfileId;
        StaffUserId = staffUserId;
        CreatedAt = createdAt;
    }

    public static AppointmentProfileStaffAssignment Create(Guid organisationId, Guid appointmentProfileId, Guid staffUserId)
    {
        if (organisationId == Guid.Empty)
            throw new DomainException("Organisation ID is required.");

        if (appointmentProfileId == Guid.Empty)
            throw new DomainException("Appointment profile ID is required.");

        if (staffUserId == Guid.Empty)
            throw new DomainException("Staff user ID is required.");

        return new AppointmentProfileStaffAssignment(
            Guid.NewGuid(),
            organisationId,
            appointmentProfileId,
            staffUserId,
            DateTimeOffset.UtcNow);
    }
}