using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MortezaApp.Migrations
{
    /// <inheritdoc />
    public partial class addblog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublish",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Blogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPublish = table.Column<bool>(type: "bit", nullable: false),
                    Visit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseSeasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseSeasons_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HeadLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<byte>(type: "tinyint", nullable: false),
                    DurationTime = table.Column<int>(type: "int", nullable: false),
                    CourseSeasonId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeadLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeadLines_CourseSeasons_CourseSeasonId",
                        column: x => x.CourseSeasonId,
                        principalTable: "CourseSeasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseSeasons_CourseId",
                table: "CourseSeasons",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_HeadLines_CourseSeasonId",
                table: "HeadLines",
                column: "CourseSeasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Blogs");

            migrationBuilder.DropTable(
                name: "HeadLines");

            migrationBuilder.DropTable(
                name: "CourseSeasons");

            migrationBuilder.DropColumn(
                name: "IsPublish",
                table: "Courses");
        }
    }
}
