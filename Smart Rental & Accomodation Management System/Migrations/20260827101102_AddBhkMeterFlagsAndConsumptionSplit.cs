using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Rental___Accomodation_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddBhkMeterFlagsAndConsumptionSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitsConsumed",
                table: "UtilityBillShares",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalUnitsConsumed",
                table: "UtilityBills",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BhkType",
                table: "Units",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasIndividualElectricityMeter",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasIndividualWaterMeter",
                table: "Units",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitsConsumed",
                table: "UtilityBillShares");

            migrationBuilder.DropColumn(
                name: "TotalUnitsConsumed",
                table: "UtilityBills");

            migrationBuilder.DropColumn(
                name: "BhkType",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "HasIndividualElectricityMeter",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "HasIndividualWaterMeter",
                table: "Units");
        }
    }
}
