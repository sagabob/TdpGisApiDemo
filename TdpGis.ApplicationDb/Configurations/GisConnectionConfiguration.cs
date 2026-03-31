using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TdpGis.Models;

namespace TdpGis.ApplicationDb.Configurations;

public class GisConnectionConfiguration : IEntityTypeConfiguration<GisConnection>
{
    public void Configure(EntityTypeBuilder<GisConnection> builder)
    {
        builder.ToTable("GisConnections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired();

        builder.Property(x => x.Entity)
            .IsRequired();

        builder.Property(x => x.EntityLabel)
            .IsRequired();

        builder.Property(x => x.QueryField)
            .IsRequired();

        builder.Property(x => x.GeometryType)
            .IsRequired()
            .HasConversion<string>();

        // Defining the relationships

        // One-to-Many with PropertyMapping:
        // A GisConnection has many PropertyMappings. We use a shadow foreign key "GisConnectionId"
        builder.HasMany(x => x.PropertyMappings)
            .WithOne() // PropertyMapping has no navigation property back
            .HasForeignKey("GisConnectionId")
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-One with DataSourceSetting:
        // A GisConnection has one DataSource, which can be reused by multiple connections.
        builder.HasOne(x => x.DataSource)
            .WithMany() // DataSource has no navigation property back
            .HasForeignKey("DataSourceId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}