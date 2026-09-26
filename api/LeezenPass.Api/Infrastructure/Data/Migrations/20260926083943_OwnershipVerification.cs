using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeezenPass.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OwnershipVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ownership_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bike_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    photo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ai_result = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ownership_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_ownership_evidence_bikes_bike_id",
                        column: x => x.bike_id,
                        principalTable: "bikes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ownership_evidence_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "possession_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bike_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_possession_challenges", x => x.id);
                    table.ForeignKey(
                        name: "fk_possession_challenges_bikes_bike_id",
                        column: x => x.bike_id,
                        principalTable: "bikes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_possession_challenges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ownership_evidence_bike_id_user_id",
                table: "ownership_evidence",
                columns: new[] { "bike_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ownership_evidence_user_id",
                table: "ownership_evidence",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_possession_challenges_bike_id_user_id",
                table: "possession_challenges",
                columns: new[] { "bike_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_possession_challenges_user_id",
                table: "possession_challenges",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ownership_evidence");

            migrationBuilder.DropTable(
                name: "possession_challenges");
        }
    }
}
