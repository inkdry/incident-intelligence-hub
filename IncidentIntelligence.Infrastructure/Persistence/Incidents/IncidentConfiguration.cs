using IncidentIntelligence.Domain.Incidents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IncidentIntelligence.Infrastructure.Persistence.Incidents;

/// <summary>
/// Configures incident database persistence.
/// </summary>
public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");

        builder.HasKey(incident => incident.Id);

        builder.Property(incident => incident.Title).HasMaxLength(200).IsRequired();

        builder.Property(incident => incident.Description).HasMaxLength(4000).IsRequired();

        builder.Property(incident => incident.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(incident => incident.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(incident => incident.ReportedAtUtc).IsRequired();

        builder.Property(incident => incident.InvestigationStartedAtUtc);
    }
}
