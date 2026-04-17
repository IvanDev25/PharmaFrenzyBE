using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    public partial class AddModulesHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_Name",
                table: "Subjects");

            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO [Modules] ([Name], [Description], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES ('General Module', 'Auto-created for existing subjects during migration.', 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            ");

            migrationBuilder.AddColumn<int>(
                name: "ModuleId",
                table: "Subjects",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                DECLARE @DefaultModuleId INT;
                SELECT TOP(1) @DefaultModuleId = [Id]
                FROM [Modules]
                WHERE [Name] = 'General Module'
                ORDER BY [Id];

                UPDATE [Subjects]
                SET [ModuleId] = @DefaultModuleId
                WHERE [ModuleId] IS NULL;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "ModuleId",
                table: "Subjects",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ModuleId_Name",
                table: "Subjects",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modules_Name",
                table: "Modules",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Modules_ModuleId",
                table: "Subjects",
                column: "ModuleId",
                principalTable: "Modules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Modules_ModuleId",
                table: "Subjects");

            migrationBuilder.DropTable(
                name: "Modules");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_ModuleId_Name",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "Subjects");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name",
                unique: true);
        }
    }
}
