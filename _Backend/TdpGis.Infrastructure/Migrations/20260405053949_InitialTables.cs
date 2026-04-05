using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TdpGis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSourceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionString = table.Column<string>(type: "text", nullable: false),
                    DatabaseType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GisWorkspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GisWorkspaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GisConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    GeometryType = table.Column<string>(type: "text", nullable: false),
                    QueryField = table.Column<string>(type: "text", nullable: false),
                    Entity = table.Column<string>(type: "text", nullable: false),
                    EntityLabel = table.Column<string>(type: "text", nullable: false),
                    GisWorkspaceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataSourceId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GisConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GisConnections_DataSourceSettings_DataSourceId",
                        column: x => x.DataSourceId,
                        principalTable: "DataSourceSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GisConnections_GisWorkspaces_GisWorkspaceId",
                        column: x => x.GisWorkspaceId,
                        principalTable: "GisWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GisWorkspaceAccessTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GisWorkspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiredDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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

            migrationBuilder.CreateTable(
                name: "PropertyMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ColumnType = table.Column<string>(type: "text", nullable: false),
                    PropertyName = table.Column<string>(type: "text", nullable: false),
                    PropertyLabel = table.Column<string>(type: "text", nullable: false),
                    GisConnectionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyMappings_GisConnections_GisConnectionId",
                        column: x => x.GisConnectionId,
                        principalTable: "GisConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GisConnections_DataSourceId",
                table: "GisConnections",
                column: "DataSourceId");

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

            migrationBuilder.CreateIndex(
                name: "IX_PropertyMappings_GisConnectionId",
                table: "PropertyMappings",
                column: "GisConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GisWorkspaceAccessTokens");

            migrationBuilder.DropTable(
                name: "PropertyMappings");

            migrationBuilder.DropTable(
                name: "GisConnections");

            migrationBuilder.DropTable(
                name: "DataSourceSettings");

            migrationBuilder.DropTable(
                name: "GisWorkspaces");
        }
    }
}
