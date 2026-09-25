using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeezenPass.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTheftReportDistrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "district",
                table: "theft_reports",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_theft_reports_status_stolen_at",
                table: "theft_reports",
                columns: new[] { "status", "stolen_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_theft_reports_status_stolen_at",
                table: "theft_reports");

            migrationBuilder.DropColumn(
                name: "district",
                table: "theft_reports");
        }
    }
}
