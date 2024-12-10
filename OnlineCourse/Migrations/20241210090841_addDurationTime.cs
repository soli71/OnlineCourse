using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortezaApp.Migrations
{
    /// <inheritdoc />
    public partial class addDurationTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationTime",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpotPlayerCourseId",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationTime",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SpotPlayerCourseId",
                table: "Courses");
        }
    }
}
