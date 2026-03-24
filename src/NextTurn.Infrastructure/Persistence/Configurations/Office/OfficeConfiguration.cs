using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OfficeEntity = NextTurn.Domain.Office.Entities.Office;

namespace NextTurn.Infrastructure.Persistence.Configurations.Office;

public sealed class OfficeConfiguration : IEntityTypeConfiguration<OfficeEntity>
{
    public void Configure(EntityTypeBuilder<OfficeEntity> builder)
    {
        builder.ToTable("Offices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Latitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(x => x.Longitude)
            .HasPrecision(9, 6)
            .IsRequired(false);

        builder.Property(x => x.OpeningHours)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.DeactivatedAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.OrganisationId, x.IsActive })
            .HasDatabaseName("IX_Offices_OrganisationId_IsActive");

        builder.HasIndex(x => new { x.OrganisationId, x.Name })
            .HasDatabaseName("IX_Offices_OrganisationId_Name");
    }
}
