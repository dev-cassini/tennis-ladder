using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tennis.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLadders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ladders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LaunchedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ladders", x => x.Id);
                    table.CheckConstraint("CK_ladders_status", "\"Status\" IN ('Draft', 'Active')");
                    table.ForeignKey(
                        name: "FK_ladders_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ladder_memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LadderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ladder_memberships", x => x.Id);
                    table.CheckConstraint("CK_ladder_memberships_position", "(\"Role\" = 'Organizer' AND \"Position\" IS NULL) OR (\"Role\" = 'Player' AND \"Position\" > 0)");
                    table.CheckConstraint("CK_ladder_memberships_role", "\"Role\" IN ('Organizer', 'Player')");
                    table.ForeignKey(
                        name: "FK_ladder_memberships_ladders_LadderId",
                        column: x => x.LadderId,
                        principalTable: "ladders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ladder_memberships_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ladder_memberships_LadderId_Position",
                table: "ladder_memberships",
                columns: new[] { "LadderId", "Position" },
                unique: true,
                filter: "\"Position\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ladder_memberships_LadderId_UserId_Role",
                table: "ladder_memberships",
                columns: new[] { "LadderId", "UserId", "Role" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ladder_memberships_UserId",
                table: "ladder_memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ladders_CreatedByUserId",
                table: "ladders",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ladder_memberships");

            migrationBuilder.DropTable(
                name: "ladders");
        }
    }
}
