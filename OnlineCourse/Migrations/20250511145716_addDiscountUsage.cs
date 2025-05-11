using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourse.Migrations
{
    /// <inheritdoc />
    public partial class addDiscountUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountUsage_DiscountCodes_DiscountCodeId",
                table: "DiscountUsage");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscountUsage_Orders_OrderId",
                table: "DiscountUsage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiscountUsage",
                table: "DiscountUsage");

            migrationBuilder.RenameTable(
                name: "DiscountUsage",
                newName: "DiscountUsages");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountUsage_OrderId",
                table: "DiscountUsages",
                newName: "IX_DiscountUsages_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountUsage_DiscountCodeId",
                table: "DiscountUsages",
                newName: "IX_DiscountUsages_DiscountCodeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscountUsages",
                table: "DiscountUsages",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountUsages_DiscountCodes_DiscountCodeId",
                table: "DiscountUsages",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountUsages_Orders_OrderId",
                table: "DiscountUsages",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscountUsages_DiscountCodes_DiscountCodeId",
                table: "DiscountUsages");

            migrationBuilder.DropForeignKey(
                name: "FK_DiscountUsages_Orders_OrderId",
                table: "DiscountUsages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DiscountUsages",
                table: "DiscountUsages");

            migrationBuilder.RenameTable(
                name: "DiscountUsages",
                newName: "DiscountUsage");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountUsages_OrderId",
                table: "DiscountUsage",
                newName: "IX_DiscountUsage_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_DiscountUsages_DiscountCodeId",
                table: "DiscountUsage",
                newName: "IX_DiscountUsage_DiscountCodeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscountUsage",
                table: "DiscountUsage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountUsage_DiscountCodes_DiscountCodeId",
                table: "DiscountUsage",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiscountUsage_Orders_OrderId",
                table: "DiscountUsage",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
