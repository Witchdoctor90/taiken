using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taiken.Venues.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "venues");

            migrationBuilder.CreateTable(
                name: "venues",
                schema: "venues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    pos_provider = table.Column<string>(type: "text", nullable: false),
                    psp_provider = table.Column<string>(type: "text", nullable: false),
                    pos_location_ref = table.Column<string>(type: "text", nullable: true),
                    config = table.Column<string>(type: "jsonb", nullable: false),
                    config_version = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "venue_secrets",
                schema: "venues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value_enc = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_secrets", x => x.id);
                    table.ForeignKey(
                        name: "fk_venue_secrets_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "venues",
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_venue_secrets_venue_id_scope_key",
                schema: "venues",
                table: "venue_secrets",
                columns: new[] { "venue_id", "scope", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venues_slug",
                schema: "venues",
                table: "venues",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "venue_secrets",
                schema: "venues");

            migrationBuilder.DropTable(
                name: "venues",
                schema: "venues");
        }
    }
}
