using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourse.Migrations
{
    /// <inheritdoc />
    public partial class addSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Map",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "SiteSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Id", "AboutUs", "Address", "Email", "FooterContent", "InstagramLink", "Map", "PhoneNumber", "PostalCode", "TelegramLink" },
                values: new object[] { (byte)1, "درباره ما", "آدرس", "ایمیل", "محتوای فوتر", null, "نقشه", "شماره تلفن", "کد پستی", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: (byte)1);

            migrationBuilder.DropColumn(
                name: "Address",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "Map",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "SiteSettings");
        }
    }
}
