using NextTurn.Domain.Appointment.Enums;
using NextTurn.Domain.Common;

namespace NextTurn.Domain.Appointment.Entities;

/// <summary>
/// Aggregate root representing a booked appointment slot.
/// </summary>
public class Appointment
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset SlotStart { get; private set; }
    public DateTimeOffset SlotEnd { get; private set; }
    public AppointmentStatus Status { get; private set; }

    protected Appointment()
    {
    }

    private Appointment(
        Guid id,
        Guid organisationId,
        Guid userId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        AppointmentStatus status)
    {
        Id = id;
        OrganisationId = organisationId;
        UserId = userId;
        SlotStart = slotStart;
        SlotEnd = slotEnd;
        Status = status;
    }

    public static Appointment Create(
        Guid organisationId,
        Guid userId,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd)
    {
        if (organisationId == Guid.Empty)
            throw new DomainException("Organisation ID is required.");

        if (userId == Guid.Empty)
            throw new DomainException("User ID is required.");

        if (slotEnd <= slotStart)
            throw new DomainException("Slot end must be after slot start.");

        return new Appointment(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            userId: userId,
            slotStart: slotStart,
            slotEnd: slotEnd,
            status: AppointmentStatus.Confirmed);
    }

    public bool Overlaps(DateTimeOffset slotStart, DateTimeOffset slotEnd)
    {
        return SlotStart < slotEnd && slotStart < SlotEnd;
    }

    public void Cancel()
    {
        if (Status == AppointmentStatus.Cancelled)
            return;

        Status = AppointmentStatus.Cancelled;
    }

    public Appointment Reschedule(DateTimeOffset slotStart, DateTimeOffset slotEnd)
    {
        if (SlotStart <= DateTimeOffset.UtcNow)
            throw new DomainException("Past appointments cannot be rescheduled.");

        if (Status != AppointmentStatus.Confirmed)
            throw new DomainException("Only confirmed appointments can be rescheduled.");

        if (slotEnd <= slotStart)
            throw new DomainException("Slot end must be after slot start.");

        if (slotStart <= DateTimeOffset.UtcNow)
            throw new DomainException("New slot must be in the future.");

        // Mark the original entry as no longer active so its slot becomes available.
        Status = AppointmentStatus.Rescheduled;

        // Create a new appointment for the replacement slot.
        return Create(OrganisationId, UserId, slotStart, slotEnd);
    }
}
