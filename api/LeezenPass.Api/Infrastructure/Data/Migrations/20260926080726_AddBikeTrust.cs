using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeezenPass.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBikeTrust : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trust_level",
                table: "bikes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // Existing bikes are self-declared.
                defaultValue: "SelfDeclared");

            migrationBuilder.AddColumn<string>(
                name: "trust_source",
                table: "bikes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trust_verified_at",
                table: "bikes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trust_level",
                table: "bikes");

            migrationBuilder.DropColumn(
                name: "trust_source",
                table: "bikes");

            migrationBuilder.DropColumn(
                name: "trust_verified_at",
                table: "bikes");
        }
    }
}
