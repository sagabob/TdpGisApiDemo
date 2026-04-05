using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TdpGis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class starter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSourceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatabaseType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSourceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GisConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeometryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QueryField = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "PropertyMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PropertyLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GisConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                name: "IX_PropertyMappings_GisConnectionId",
                table: "PropertyMappings",
                column: "GisConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyMappings");

            migrationBuilder.DropTable(
                name: "GisConnections");

            migrationBuilder.DropTable(
                name: "DataSourceSettings");
        }
    }
}
