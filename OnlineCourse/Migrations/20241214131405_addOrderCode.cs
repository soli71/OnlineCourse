using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortezaApp.Migrations
{
    /// <inheritdoc />
    public partial class addOrderCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "OrderSequence",
                startValue: 2000L);

            migrationBuilder.AddColumn<string>(
                name: "OrderCode",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderCode",
                table: "Orders");

            migrationBuilder.DropSequence(
                name: "OrderSequence");
        }
    }
}
