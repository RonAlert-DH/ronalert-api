using Microsoft.EntityFrameworkCore.Migrations;

namespace RonALert.Infrastructure.Database.Migrations
{
    public partial class AddNearestPosition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "NearestDistance",
                table: "PersonPositions",
                nullable: false,
                defaultValue: 0.0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NearestDistance",
                table: "PersonPositions");
        }
    }
}
