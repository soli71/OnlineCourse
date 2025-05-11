using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineCourse.Migrations
{
    /// <inheritdoc />
    public partial class addDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscountCodeId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountCodeId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountEndDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DiscountStartDate",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "OrderStatusHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Carts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountCodeId",
                table: "Carts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "CartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    For = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsageLimit = table.Column<int>(type: "int", nullable: true),
                    MinimumOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscountUsage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscountCodeId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountUsage_DiscountCodes_DiscountCodeId",
                        column: x => x.DiscountCodeId,
                        principalTable: "DiscountCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountUsage_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscountCodeId",
                table: "Users",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DiscountCodeId",
                table: "Products",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_UserId",
                table: "OrderStatusHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_UserId1",
                table: "OrderStatusHistories",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_DiscountCodeId",
                table: "Carts",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUsage_DiscountCodeId",
                table: "DiscountUsage",
                column: "DiscountCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUsage_OrderId",
                table: "DiscountUsage",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_DiscountCodes_DiscountCodeId",
                table: "Carts",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusHistories_Users_UserId",
                table: "OrderStatusHistories",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusHistories_Users_UserId1",
                table: "OrderStatusHistories",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_DiscountCodes_DiscountCodeId",
                table: "Products",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_DiscountCodes_DiscountCodeId",
                table: "Users",
                column: "DiscountCodeId",
                principalTable: "DiscountCodes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_DiscountCodes_DiscountCodeId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusHistories_Users_UserId",
                table: "OrderStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusHistories_Users_UserId1",
                table: "OrderStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_DiscountCodes_DiscountCodeId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_DiscountCodes_DiscountCodeId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "DiscountUsage");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropIndex(
                name: "IX_Users_DiscountCodeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_DiscountCodeId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_UserId",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_UserId1",
                table: "OrderStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Carts_DiscountCodeId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountEndDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountStartDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "OrderStatusHistories");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "DiscountCodeId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "CartItems");
        }
    }
}
