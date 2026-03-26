using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NextTurn.Domain.Service.Entities;

namespace NextTurn.Infrastructure.Persistence.Configurations.Service;

public sealed class ServiceOfficeAssignmentConfiguration : IEntityTypeConfiguration<ServiceOfficeAssignment>
{
    public void Configure(EntityTypeBuilder<ServiceOfficeAssignment> builder)
    {
        builder.ToTable("ServiceOfficeAssignments");

        builder.HasKey(x => new { x.OrganisationId, x.ServiceId, x.OfficeId });

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne<NextTurn.Domain.Service.Entities.Service>()
            .WithMany()
            .HasForeignKey(x => new { x.OrganisationId, x.ServiceId })
            .HasPrincipalKey(x => new { x.OrganisationId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<NextTurn.Domain.Office.Entities.Office>()
            .WithMany()
            .HasForeignKey(x => new { x.OrganisationId, x.OfficeId })
            .HasPrincipalKey(x => new { x.OrganisationId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
