using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdatesToAppUserVehicleAndOdo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapturedBy",
                table: "OdometerReadings");

            migrationBuilder.RenameColumn(
                name: "LicensePlate",
                table: "OdometerReadings",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "IsFleetAdmin",
                table: "AspNetUsers",
                newName: "IsActive");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vehicles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "OdometerReadings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OdometerReadings_UserId",
                table: "OdometerReadings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OdometerReadings_VehicleId",
                table: "OdometerReadings",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_VehicleId",
                table: "AspNetUsers",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Vehicles_VehicleId",
                table: "AspNetUsers",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OdometerReadings_AspNetUsers_UserId",
                table: "OdometerReadings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OdometerReadings_Vehicles_VehicleId",
                table: "OdometerReadings",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Vehicles_VehicleId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_OdometerReadings_AspNetUsers_UserId",
                table: "OdometerReadings");

            migrationBuilder.DropForeignKey(
                name: "FK_OdometerReadings_Vehicles_VehicleId",
                table: "OdometerReadings");

            migrationBuilder.DropIndex(
                name: "IX_OdometerReadings_UserId",
                table: "OdometerReadings");

            migrationBuilder.DropIndex(
                name: "IX_OdometerReadings_VehicleId",
                table: "OdometerReadings");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_VehicleId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "OdometerReadings");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "OdometerReadings",
                newName: "LicensePlate");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "AspNetUsers",
                newName: "IsFleetAdmin");

            migrationBuilder.AddColumn<string>(
                name: "CapturedBy",
                table: "OdometerReadings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
