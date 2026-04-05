using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Configurations;

public class PropertyMappingConfiguration : IEntityTypeConfiguration<PropertyMapping>
{
    public void Configure(EntityTypeBuilder<PropertyMapping> builder)
    {
        builder.ToTable("PropertyMappings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PropertyName)
            .IsRequired();

        builder.Property(x => x.PropertyLabel)
            .IsRequired();

        builder.Property(x => x.ColumnType)
            .IsRequired()
            .HasConversion<string>(); // Optional: enum as string
    }
}
