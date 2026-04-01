using NextTurn.Domain.Common;

namespace NextTurn.Domain.Office.Entities;

public sealed class Office
{
    public Guid Id { get; }
    public Guid OrganisationId { get; private set; }
    public string Name { get; private set; }
    public string Address { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string OpeningHours { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? DeactivatedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Office()
    {
        Name = default!;
        Address = default!;
        OpeningHours = default!;
    }

    private Office(
        Guid id,
        Guid organisationId,
        string name,
        string address,
        decimal? latitude,
        decimal? longitude,
        string openingHours,
        bool isActive,
        DateTimeOffset? deactivatedAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        OrganisationId = organisationId;
        Name = name;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        OpeningHours = openingHours;
        IsActive = isActive;
        DeactivatedAt = deactivatedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Office Create(
        Guid organisationId,
        string name,
        string address,
        decimal? latitude,
        decimal? longitude,
        string openingHours)
    {
        ValidateName(name);
        ValidateAddress(address);
        ValidateCoordinates(latitude, longitude);
        ValidateOpeningHours(openingHours);

        var utcNow = DateTimeOffset.UtcNow;

        return new Office(
            id: Guid.NewGuid(),
            organisationId: organisationId,
            name: name.Trim(),
            address: address.Trim(),
            latitude: latitude,
            longitude: longitude,
            openingHours: openingHours.Trim(),
            isActive: true,
            deactivatedAt: null,
            createdAt: utcNow,
            updatedAt: utcNow);
    }

    public void UpdateDetails(
        string name,
        string address,
        decimal? latitude,
        decimal? longitude,
        string openingHours)
    {
        ValidateName(name);
        ValidateAddress(address);
        ValidateCoordinates(latitude, longitude);
        ValidateOpeningHours(openingHours);

        Name = name.Trim();
        Address = address.Trim();
        Latitude = latitude;
        Longitude = longitude;
        OpeningHours = openingHours.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Office is already deactivated.");

        IsActive = false;
        DeactivatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Office name is required.");

        if (name.Trim().Length > 120)
            throw new DomainException("Office name must not exceed 120 characters.");
    }

    private static void ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new DomainException("Office address is required.");

        if (address.Trim().Length > 300)
            throw new DomainException("Office address must not exceed 300 characters.");
    }

    private static void ValidateCoordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude.HasValue && (latitude.Value < -90m || latitude.Value > 90m))
            throw new DomainException("Latitude must be between -90 and 90.");

        if (longitude.HasValue && (longitude.Value < -180m || longitude.Value > 180m))
            throw new DomainException("Longitude must be between -180 and 180.");
    }

    private static void ValidateOpeningHours(string openingHours)
    {
        if (string.IsNullOrWhiteSpace(openingHours))
            throw new DomainException("Opening hours are required.");

        if (openingHours.Trim().Length > 4000)
            throw new DomainException("Opening hours must not exceed 4000 characters.");
    }
}
