using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeToContainerLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "in_vehicle_type",
                table: "container_lifecycles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "out_vehicle_type",
                table: "container_lifecycles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "in_vehicle_type",
                table: "container_lifecycles");

            migrationBuilder.DropColumn(
                name: "out_vehicle_type",
                table: "container_lifecycles");
        }
    }
}
