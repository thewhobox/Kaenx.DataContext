using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Local
{
    public partial class Thumbs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbHeight",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ThumbWidth",
                table: "Projects");
        }
    }
}
