using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Rental___Accomodation_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMessManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    MealType = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessFeedbacks_AspNetUsers_TenantId",
                        column: x => x.TenantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessFeedbacks_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessMenus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    MealType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessMenus_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessFeedbacks_PropertyId",
                table: "MessFeedbacks",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MessFeedbacks_TenantId",
                table: "MessFeedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MessMenus_PropertyId",
                table: "MessMenus",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessFeedbacks");

            migrationBuilder.DropTable(
                name: "MessMenus");
        }
    }
}
