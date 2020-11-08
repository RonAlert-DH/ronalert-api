using Microsoft.EntityFrameworkCore.Migrations;

namespace RonALert.Infrastructure.Database.Migrations
{
    public partial class ExtendAlarms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Alarms",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Alarms");
        }
    }
}
