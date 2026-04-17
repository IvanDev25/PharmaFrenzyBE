using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    public partial class AddRankingBadges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RankingPeriodAwards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: true),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AwardedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankingPeriodAwards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RankingPeriodAwards_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentRankingBadges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ModuleId = table.Column<int>(type: "int", nullable: true),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AwardedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRankingBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentRankingBadges_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentRankingBadges_Modules_ModuleId",
                        column: x => x.ModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RankingPeriodAwards_ModuleId",
                table: "RankingPeriodAwards",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RankingPeriodAwards_Scope_ModuleId_PeriodType_PeriodStartUtc_PeriodEndUtc",
                table: "RankingPeriodAwards",
                columns: new[] { "Scope", "ModuleId", "PeriodType", "PeriodStartUtc", "PeriodEndUtc" },
                unique: true,
                filter: "[ModuleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRankingBadges_ModuleId",
                table: "StudentRankingBadges",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRankingBadges_StudentId_Scope_ModuleId_PeriodType_Rank_PeriodStartUtc_PeriodEndUtc",
                table: "StudentRankingBadges",
                columns: new[] { "StudentId", "Scope", "ModuleId", "PeriodType", "Rank", "PeriodStartUtc", "PeriodEndUtc" },
                unique: true,
                filter: "[ModuleId] IS NOT NULL");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX [IX_RankingPeriodAwards_GlobalPeriod]
                  ON [RankingPeriodAwards] ([Scope], [PeriodType], [PeriodStartUtc], [PeriodEndUtc])
                  WHERE [ModuleId] IS NULL;");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX [IX_StudentRankingBadges_GlobalPeriod]
                  ON [StudentRankingBadges] ([StudentId], [Scope], [PeriodType], [Rank], [PeriodStartUtc], [PeriodEndUtc])
                  WHERE [ModuleId] IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX [IX_RankingPeriodAwards_GlobalPeriod] ON [RankingPeriodAwards];");
            migrationBuilder.Sql("DROP INDEX [IX_StudentRankingBadges_GlobalPeriod] ON [StudentRankingBadges];");

            migrationBuilder.DropTable(
                name: "RankingPeriodAwards");

            migrationBuilder.DropTable(
                name: "StudentRankingBadges");
        }
    }
}
