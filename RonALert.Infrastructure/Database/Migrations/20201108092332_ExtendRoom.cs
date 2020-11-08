using Microsoft.EntityFrameworkCore.Migrations;

namespace RonALert.Infrastructure.Database.Migrations
{
    public partial class ExtendRoom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeopleLimit",
                table: "Rooms",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeopleLimit",
                table: "Rooms");
        }
    }
}
