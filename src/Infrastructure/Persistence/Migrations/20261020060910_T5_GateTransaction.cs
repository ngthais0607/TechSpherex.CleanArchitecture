using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T5_GateTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gate_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    vehicle_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    vehicle_type = table.Column<int>(type: "integer", nullable: false),
                    driver_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    virtual_block_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gate_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gate_transaction_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    new_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gate_transaction_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_gate_transaction_changes_gate_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "gate_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gate_transaction_changes_transaction_id",
                table: "gate_transaction_changes",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_gate_transactions_tenant_id_container_id_status",
                table: "gate_transactions",
                columns: new[] { "tenant_id", "container_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_gate_transactions_tenant_id_created_at",
                table: "gate_transactions",
                columns: new[] { "tenant_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gate_transaction_changes");

            migrationBuilder.DropTable(
                name: "gate_transactions");
        }
    }
}
