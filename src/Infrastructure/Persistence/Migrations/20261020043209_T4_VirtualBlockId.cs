using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnpDepotManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class T4_VirtualBlockId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_container_lifecycles_container_id",
                table: "container_lifecycles");

            migrationBuilder.AddColumn<Guid>(
                name: "virtual_block_id",
                table: "container_lifecycles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_container_lifecycles_container_id_status_inyard",
                table: "container_lifecycles",
                column: "container_id",
                filter: "\"status\" = 'InYard'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_container_lifecycles_container_id_status_inyard",
                table: "container_lifecycles");

            migrationBuilder.DropColumn(
                name: "virtual_block_id",
                table: "container_lifecycles");

            migrationBuilder.CreateIndex(
                name: "ix_container_lifecycles_container_id",
                table: "container_lifecycles",
                column: "container_id");
        }
    }
}
