using Microsoft.EntityFrameworkCore.Migrations;

namespace RonALert.Infrastructure.Database.Migrations
{
    public partial class AddFaceMaskFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FaceMask",
                table: "PersonPositions",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceMask",
                table: "PersonPositions");
        }
    }
}
