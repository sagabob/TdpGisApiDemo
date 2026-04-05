using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TdpGis.Domain;
using TdpGis.Infrastructure.Persistence;

namespace TdpGis.Infrastructure.Configurations;

public class GisWorkspaceAccessTokenConfiguration : IEntityTypeConfiguration<GisWorkspaceAccessToken>
{
    public void Configure(EntityTypeBuilder<GisWorkspaceAccessToken> builder)
    {
        builder.ToTable("GisWorkspaceAccessTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AccessToken)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ExpiredDateTime)
            .IsRequired()
            .HasConversion(
                v => DateTimeUtcForPostgreSql.ToUtc(v),
                v => DateTimeUtcForPostgreSql.FromStore(v));

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.AccessToken)
            .IsUnique();

        builder.HasIndex(x => x.GisWorkspaceId);

        builder.HasOne(x => x.GisWorkspace)
            .WithMany(x => x.AccessTokens)
            .HasForeignKey(x => x.GisWorkspaceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}