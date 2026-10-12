using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "containers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    container_number = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    iso_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    size = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tare_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    max_gross_weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    date_of_manufacture = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    owner_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_containers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_code = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    order_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    line_operator = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    customer_tax_code = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    outbound_vessel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "line_operators",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_line_operators", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plate_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "voyages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    voyage_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    etd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voyages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "yard_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_virtual = table.Column<bool>(type: "boolean", nullable: false),
                    block_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    max_bays = table.Column<int>(type: "integer", nullable: false),
                    max_rows = table.Column<int>(type: "integer", nullable: false),
                    max_tiers = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_yard_blocks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "container_lifecycles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    classification = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    in_vehicle_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    in_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    out_vehicle_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    out_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    current_slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_container_lifecycles", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_lifecycles_containers_container_id",
                        column: x => x.container_id,
                        principalTable: "containers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delivery_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    required_quantity = table.Column<int>(type: "integer", nullable: false),
                    delivered_quantity = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_delivery_order_items_delivery_orders_delivery_order_id",
                        column: x => x.delivery_order_id,
                        principalTable: "delivery_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_roles_application_users_user_id",
                        column: x => x.user_id,
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "yard_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    block_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_block = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    position_bay = table.Column<int>(type: "integer", nullable: false),
                    position_row = table.Column<int>(type: "integer", nullable: false),
                    position_tier = table.Column<int>(type: "integer", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_occupied = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_yard_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_yard_slots_yard_blocks_block_id",
                        column: x => x.block_id,
                        principalTable: "yard_blocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_users_email",
                table: "application_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_application_users_user_name",
                table: "application_users",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_container_lifecycles_container_id",
                table: "container_lifecycles",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_lifecycles_status",
                table: "container_lifecycles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_container_lifecycles_tenant_id_container_id",
                table: "container_lifecycles",
                columns: new[] { "tenant_id", "container_id" },
                unique: true,
                filter: "\"status\" = 'InYard'");

            migrationBuilder.CreateIndex(
                name: "ix_containers_tenant_id_container_number",
                table: "containers",
                columns: new[] { "tenant_id", "container_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_tax_code",
                table: "customers",
                column: "tax_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_order_items_delivery_order_id",
                table: "delivery_order_items",
                column: "delivery_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_orders_expiry_date",
                table: "delivery_orders",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "ix_delivery_orders_tenant_id_order_number",
                table: "delivery_orders",
                columns: new[] { "tenant_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_line_operators_code",
                table: "line_operators",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id_role_id",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_plate_number",
                table: "vehicles",
                column: "plate_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_voyages_vessel_name_voyage_number",
                table: "voyages",
                columns: new[] { "vessel_name", "voyage_number" });

            migrationBuilder.CreateIndex(
                name: "ix_yard_blocks_tenant_id_code",
                table: "yard_blocks",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_yard_slots_block_id",
                table: "yard_slots",
                column: "block_id");

            migrationBuilder.CreateIndex(
                name: "ix_yard_slots_tenant_id",
                table: "yard_slots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_yard_slots_tenant_block_bay_row_tier",
                table: "yard_slots",
                columns: new[] { "tenant_id", "block_id", "position_bay", "position_row", "position_tier" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "container_lifecycles");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "delivery_order_items");

            migrationBuilder.DropTable(
                name: "line_operators");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "voyages");

            migrationBuilder.DropTable(
                name: "yard_slots");

            migrationBuilder.DropTable(
                name: "containers");

            migrationBuilder.DropTable(
                name: "delivery_orders");

            migrationBuilder.DropTable(
                name: "application_users");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "yard_blocks");
        }
    }
}
