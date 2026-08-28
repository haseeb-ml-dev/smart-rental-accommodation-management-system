using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Rental___Accomodation_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndDisputes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DisputeRaisedAt",
                table: "UtilityBillShares",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "UtilityBillShares",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisputeResolution",
                table: "UtilityBillShares",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisputeResolvedAt",
                table: "UtilityBillShares",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisputeStatus",
                table: "UtilityBillShares",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    LinkController = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId",
                table: "Notifications",
                column: "RecipientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "DisputeRaisedAt",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "DisputeResolution",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "DisputeResolvedAt",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "DisputeStatus",
                table: "UtilityBillShares");
        }
    }
}
