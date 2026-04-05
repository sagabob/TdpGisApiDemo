using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Configurations;

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

        builder.HasIndex("DataSourceId");
        builder.HasIndex(x => x.GisWorkspaceId);

        // One-to-many: PropertyMapping rows always belong to a GisConnection (shadow FK GisConnectionId).
        builder.HasMany(x => x.PropertyMappings)
            .WithOne()
            .HasForeignKey("GisConnectionId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-one: DataSource is required; shared across many connections.
        builder.HasOne(x => x.DataSource)
            .WithMany()
            .HasForeignKey("DataSourceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Optional workspace: clearing or deleting workspace sets GisWorkspaceId to null on connections.
        builder.HasOne(x => x.GisWorkspace)
            .WithMany(x => x.Entities)
            .HasForeignKey(x => x.GisWorkspaceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}