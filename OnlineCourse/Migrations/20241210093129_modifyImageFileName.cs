using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortezaApp.Migrations
{
    /// <inheritdoc />
    public partial class modifyImageFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "Courses",
                newName: "ImageFileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageFileName",
                table: "Courses",
                newName: "ImagePath");
        }
    }
}
