using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T7_MasterDataIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_tax_code",
                table: "customers");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "line_operators",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "tenant_id",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tenant_id_tax_code",
                table: "customers",
                columns: new[] { "tenant_id", "tax_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_tenant_id_tax_code",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "line_operators");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "customers");

            migrationBuilder.CreateIndex(
                name: "ix_customers_tax_code",
                table: "customers",
                column: "tax_code",
                unique: true);
        }
    }
}
