using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoSeedRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demo_seed_runs",
                columns: table => new
                {
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dataset_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_demo_seed_runs", x => new { x.tenant_id, x.dataset_version });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_seed_runs");
        }
    }
}
