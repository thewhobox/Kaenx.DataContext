using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kaenx.DataContext.Migrations.Catalog
{
    public partial class Baggage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "AppParameters");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "Applications");

            migrationBuilder.CreateTable(
                name: "Baggages",
                columns: table => new
                {
                    UId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id = table.Column<string>(type: "TEXT", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: true),
                    PictureType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Baggages", x => x.UId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Baggages");

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "AppParameters",
                type: "TEXT",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "Applications",
                type: "TEXT",
                maxLength: 40,
                nullable: true);
        }
    }
}
