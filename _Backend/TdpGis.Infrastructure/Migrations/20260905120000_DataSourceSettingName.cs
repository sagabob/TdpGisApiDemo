using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TdpGis.Infrastructure.Migrations;

/// <inheritdoc />
public partial class DataSourceSettingName : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "DataSourceSettings",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Name",
            table: "DataSourceSettings");
    }
}
