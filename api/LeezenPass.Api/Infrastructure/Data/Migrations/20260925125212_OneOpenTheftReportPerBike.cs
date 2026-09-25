using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeezenPass.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OneOpenTheftReportPerBike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_theft_reports_bike_id_open",
                table: "theft_reports",
                column: "bike_id",
                unique: true,
                filter: "status = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_theft_reports_bike_id_open",
                table: "theft_reports");
        }
    }
}
