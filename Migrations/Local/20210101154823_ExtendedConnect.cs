using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Local
{
    public partial class ExtendedConnect : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Remotes",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "Remotes",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSecure",
                table: "Remotes",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "Remotes");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Remotes");

            migrationBuilder.DropColumn(
                name: "IsSecure",
                table: "Remotes");
        }
    }
}
