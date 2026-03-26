using NextTurn.Domain.Common;

namespace NextTurn.Domain.Service.Entities;

public sealed class Service
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string Description { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Service()
    {
        Name = default!;
        Code = default!;
        Description = default!;
    }

    private Service(
        Guid id,
        Guid organisationId,
        string name,
        string code,
        string description,
        int estimatedDurationMinutes,
        bool isActive,
        DateTimeOffset? deactivatedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        Code = code;
        Description = description;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        IsActive = isActive;
        DeactivatedAt = deactivatedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Service Create(
        Guid organisationId,
        string name,
        string code,
        string description,
        int estimatedDurationMinutes,
        bool isActive)
    {
        ValidateName(name);
        ValidateCode(code);
        ValidateDescription(description);
        ValidateDuration(estimatedDurationMinutes);

        var utcNow = DateTimeOffset.UtcNow;

        return new Service(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            name: name.Trim(),
            code: code.Trim().ToUpperInvariant(),
            description: description.Trim(),
            estimatedDurationMinutes: estimatedDurationMinutes,
            isActive: isActive,
            deactivatedAt: isActive ? null : utcNow,
            createdAt: utcNow,
            updatedAt: utcNow);
    }

    public void UpdateDetails(string name, string description, int estimatedDurationMinutes)
    {
        ValidateName(name);
        ValidateDescription(description);
        ValidateDuration(estimatedDurationMinutes);

        Name = name.Trim();
        Description = description.Trim();
        EstimatedDurationMinutes = estimatedDurationMinutes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Service is already deactivated.");

        IsActive = false;
        DeactivatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Service name is required.");

        if (name.Trim().Length > 120)
            throw new DomainException("Service name must not exceed 120 characters.");
    }

    private static void ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Service code is required.");

        if (code.Trim().Length > 40)
            throw new DomainException("Service code must not exceed 40 characters.");
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Service description is required.");

        if (description.Trim().Length > 500)
            throw new DomainException("Service description must not exceed 500 characters.");
    }

    private static void ValidateDuration(int estimatedDurationMinutes)
    {
        if (estimatedDurationMinutes < 1 || estimatedDurationMinutes > 1440)
            throw new DomainException("Estimated duration must be between 1 and 1440 minutes.");
    }
}
