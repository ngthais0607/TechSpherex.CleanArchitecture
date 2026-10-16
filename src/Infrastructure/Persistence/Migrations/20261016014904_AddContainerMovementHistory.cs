using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerMovementHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "movement_history_version",
                table: "container_lifecycles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "container_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_lifecycle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_position_block_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_position_block_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    from_position_bay = table.Column<int>(type: "integer", nullable: true),
                    from_position_row = table.Column<int>(type: "integer", nullable: true),
                    from_position_tier = table.Column<int>(type: "integer", nullable: true),
                    to_position_block_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_position_block_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_position_bay = table.Column<int>(type: "integer", nullable: true),
                    to_position_row = table.Column<int>(type: "integer", nullable: true),
                    to_position_tier = table.Column<int>(type: "integer", nullable: true),
                    vehicle_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    vehicle_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    delivery_order_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_container_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_movements_container_lifecycles_container_lifecycl",
                        column: x => x.container_lifecycle_id,
                        principalTable: "container_lifecycles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_container_movements_containers_container_id",
                        column: x => x.container_id,
                        principalTable: "containers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_container_movements_container_id",
                table: "container_movements",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_movements_container_lifecycle_id",
                table: "container_movements",
                column: "container_lifecycle_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_movements_tenant_id_container_id_occurred_at_utc_",
                table: "container_movements",
                columns: new[] { "tenant_id", "container_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_container_movements_tenant_id_container_lifecycle_id",
                table: "container_movements",
                columns: new[] { "tenant_id", "container_lifecycle_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "container_movements");

            migrationBuilder.DropColumn(
                name: "movement_history_version",
                table: "container_lifecycles");
        }
    }
}
