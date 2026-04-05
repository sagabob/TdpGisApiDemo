using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TdpGis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGisWorkspaceAccessTokenIsPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "GisWorkspaceAccessTokens",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "GisWorkspaceAccessTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "GisWorkspaceAccessTokens");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "GisWorkspaceAccessTokens",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
