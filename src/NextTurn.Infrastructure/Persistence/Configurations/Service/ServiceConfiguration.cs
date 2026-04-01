using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTurn.Domain.Service.Entities;
using ServiceEntity = NextTurn.Domain.Service.Entities.Service;

namespace NextTurn.Infrastructure.Persistence.Configurations.Service;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<ServiceEntity>
{
    public void Configure(EntityTypeBuilder<ServiceEntity> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganisationId)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.EstimatedDurationMinutes)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.DeactivatedAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.OrganisationId, x.Code })
            .IsUnique();

        builder.HasMany<ServiceOfficeAssignment>()
            .WithOne()
            .HasForeignKey(x => new { x.OrganisationId, x.ServiceId })
            .HasPrincipalKey(x => new { x.OrganisationId, x.Id });
    }
}
