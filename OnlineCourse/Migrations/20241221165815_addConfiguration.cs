using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourse.Migrations
{
    /// <inheritdoc />
    public partial class addConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MainPageContent",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainPageImage",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleAboutUs",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleAddress",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleEmail",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleFooterContent",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleInstagramLink",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleMainPageBlogs",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleMainPageContent",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleMainPageCourses",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleMainPageImage",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleMap",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisiblePhoneNumber",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisiblePostalCode",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "VisibleTelegramLink",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: (byte)1,
                columns: new[] { "MainPageContent", "MainPageImage", "VisibleAboutUs", "VisibleAddress", "VisibleEmail", "VisibleFooterContent", "VisibleInstagramLink", "VisibleMainPageBlogs", "VisibleMainPageContent", "VisibleMainPageCourses", "VisibleMainPageImage", "VisibleMap", "VisiblePhoneNumber", "VisiblePostalCode", "VisibleTelegramLink" },
                values: new object[] { null, null, false, false, false, false, false, false, false, false, false, false, false, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MainPageContent",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "MainPageImage",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleAboutUs",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleAddress",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleEmail",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleFooterContent",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleInstagramLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleMainPageBlogs",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleMainPageContent",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleMainPageCourses",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleMainPageImage",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleMap",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisiblePhoneNumber",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisiblePostalCode",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "VisibleTelegramLink",
                table: "SiteSettings");
        }
    }
}
