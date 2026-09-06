using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TdpGis.Infrastructure.Persistence;

#nullable disable

namespace TdpGis.Infrastructure.Migrations;

[DbContext(typeof(GisAppDbContext))]
[Migration("20260905120000_DataSourceSettingName")]
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
