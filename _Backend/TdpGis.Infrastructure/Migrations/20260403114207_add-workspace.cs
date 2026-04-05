using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TdpGis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addworkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "GisConnectionId",
                table: "PropertyMappings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GisWorkspaceId",
                table: "GisConnections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GisWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GisWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GisWorkspaceAccessTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GisWorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessToken = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiredDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GisWorkspaceAccessTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GisWorkspaceAccessTokens_GisWorkspaces_GisWorkspaceId",
                        column: x => x.GisWorkspaceId,
                        principalTable: "GisWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GisConnections_GisWorkspaceId",
                table: "GisConnections",
                column: "GisWorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_GisWorkspaceAccessTokens_AccessToken",
                table: "GisWorkspaceAccessTokens",
                column: "AccessToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GisWorkspaceAccessTokens_GisWorkspaceId",
                table: "GisWorkspaceAccessTokens",
                column: "GisWorkspaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_GisConnections_GisWorkspaces_GisWorkspaceId",
                table: "GisConnections",
                column: "GisWorkspaceId",
                principalTable: "GisWorkspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GisConnections_GisWorkspaces_GisWorkspaceId",
                table: "GisConnections");

            migrationBuilder.DropTable(
                name: "GisWorkspaceAccessTokens");

            migrationBuilder.DropTable(
                name: "GisWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_GisConnections_GisWorkspaceId",
                table: "GisConnections");

            migrationBuilder.DropColumn(
                name: "GisWorkspaceId",
                table: "GisConnections");

            migrationBuilder.AlterColumn<Guid>(
                name: "GisConnectionId",
                table: "PropertyMappings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
