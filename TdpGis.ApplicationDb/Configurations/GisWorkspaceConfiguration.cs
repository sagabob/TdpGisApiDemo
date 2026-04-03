using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TdpGis.Models;

namespace TdpGis.ApplicationDb.Configurations;

public class GisWorkspaceConfiguration : IEntityTypeConfiguration<GisWorkspace>
{
    public void Configure(EntityTypeBuilder<GisWorkspace> builder)
    {
        builder.ToTable("GisWorkspaces");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired();
    }
}