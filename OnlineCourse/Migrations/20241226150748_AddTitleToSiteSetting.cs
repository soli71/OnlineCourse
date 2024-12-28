using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourse.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleToSiteSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: (byte)1,
                column: "Title",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "SiteSettings");
        }
    }
}
