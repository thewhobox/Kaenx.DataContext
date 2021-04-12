using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Project
{
    public partial class Deactivate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "LineDevices",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastGroupCount",
                table: "LineDevices",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "LineDevices");

            migrationBuilder.DropColumn(
                name: "LastGroupCount",
                table: "LineDevices");
        }
    }
}
