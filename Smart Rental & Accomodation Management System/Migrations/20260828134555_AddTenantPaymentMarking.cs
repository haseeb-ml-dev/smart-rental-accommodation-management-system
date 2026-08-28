using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Rental___Accomodation_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPaymentMarking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentSlipFileName",
                table: "UtilityBillShares",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantMarkedPaidAt",
                table: "UtilityBillShares",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentSlipFileName",
                table: "RentInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TenantMarkedPaidAt",
                table: "RentInvoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentSlipFileName",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "TenantMarkedPaidAt",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "PaymentSlipFileName",
                table: "RentInvoices");

            migrationBuilder.DropColumn(
                name: "TenantMarkedPaidAt",
                table: "RentInvoices");
        }
    }
}
