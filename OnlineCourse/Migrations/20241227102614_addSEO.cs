using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourse.Migrations
{
    /// <inheritdoc />
    public partial class addSEO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTagDescription",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Courses",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaKeywords",
                table: "Blogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTagDescription",
                table: "Blogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "Blogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Blogs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Slug",
                table: "Courses",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Blogs_Slug",
                table: "Blogs",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_Slug",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Blogs_Slug",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MetaTagDescription",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MetaKeywords",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "MetaTagDescription",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "Blogs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Blogs");
        }
    }
}
