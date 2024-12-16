using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortezaApp.Migrations
{
    /// <inheritdoc />
    public partial class addOrderDetailDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "OrderSequence");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "OrderDetails");

            migrationBuilder.CreateSequence<int>(
                name: "OrderSequence",
                startValue: 2000L);
        }
    }
}
