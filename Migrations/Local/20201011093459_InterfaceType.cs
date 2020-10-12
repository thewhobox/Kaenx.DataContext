using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Local
{
    public partial class InterfaceType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Interfaces",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Interfaces");
        }
    }
}
