namespace NextTurn.Domain.Service.Entities;

public sealed class ServiceOfficeAssignment
{
    public Guid ServiceId { get; private set; }
    public Guid OfficeId { get; private set; }
    public Guid OrganisationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ServiceOfficeAssignment() { }

    private ServiceOfficeAssignment(Guid serviceId, Guid officeId, Guid organisationId, DateTimeOffset createdAt)
    {
        ServiceId = serviceId;
        OfficeId = officeId;
        OrganisationId = organisationId;
        CreatedAt = createdAt;
    }

    public static ServiceOfficeAssignment Create(Guid serviceId, Guid officeId, Guid organisationId)
    {
        return new ServiceOfficeAssignment(serviceId, officeId, organisationId, DateTimeOffset.UtcNow);
    }
}
