using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kaenx.DataContext.Migrations.Local
{
    public partial class LocalDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastOpened",
                table: "Projects",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastOpened",
                table: "Projects");
        }
    }
}
