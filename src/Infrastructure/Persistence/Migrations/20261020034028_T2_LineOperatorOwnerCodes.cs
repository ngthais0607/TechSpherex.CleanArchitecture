using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T2_LineOperatorOwnerCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "line_operator_owner_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    line_operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_line_operator_owner_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_line_operator_owner_codes_tenant_id_owner_code",
                table: "line_operator_owner_codes",
                columns: new[] { "tenant_id", "owner_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "line_operator_owner_codes");
        }
    }
}
