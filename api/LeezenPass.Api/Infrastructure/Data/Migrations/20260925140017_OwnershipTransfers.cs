using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeezenPass.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OwnershipTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // bikes/ownership_transfers use the Postgres system column xmin as concurrency token; it already exists.

            migrationBuilder.AddColumn<string>(
                name: "verify_token",
                table: "ownership_transfers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_ownership_transfers_verify_token",
                table: "ownership_transfers",
                column: "verify_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ownership_transfers_verify_token",
                table: "ownership_transfers");

            migrationBuilder.DropColumn(
                name: "verify_token",
                table: "ownership_transfers");

        }
    }
}
