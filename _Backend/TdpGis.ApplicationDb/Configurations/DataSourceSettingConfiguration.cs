using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TdpGis.Models;

namespace TdpGis.ApplicationDb.Configurations;

public class DataSourceSettingConfiguration : IEntityTypeConfiguration<DataSourceSetting>
{
    public void Configure(EntityTypeBuilder<DataSourceSetting> builder)
    {
        builder.ToTable("DataSourceSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConnectionString)
            .IsRequired();

        builder.Property(x => x.DatabaseType)
            .IsRequired()
            .HasConversion<string>(); // Optional: storing enum as string in DB
    }
}