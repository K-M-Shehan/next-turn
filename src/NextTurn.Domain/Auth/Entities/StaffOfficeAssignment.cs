namespace NextTurn.Domain.Auth.Entities;

public sealed class StaffOfficeAssignment
{
    public Guid OrganisationId { get; private set; }
    public Guid StaffUserId { get; private set; }
    public Guid OfficeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private StaffOfficeAssignment() { }

    private StaffOfficeAssignment(
        Guid organisationId,
        Guid staffUserId,
        Guid officeId,
        DateTimeOffset createdAt)
    {
        OrganisationId = organisationId;
        StaffUserId = staffUserId;
        OfficeId = officeId;
        CreatedAt = createdAt;
    }

    public static StaffOfficeAssignment Create(Guid organisationId, Guid staffUserId, Guid officeId)
    {
        return new StaffOfficeAssignment(
            organisationId,
            staffUserId,
            officeId,
            DateTimeOffset.UtcNow);
    }
}
