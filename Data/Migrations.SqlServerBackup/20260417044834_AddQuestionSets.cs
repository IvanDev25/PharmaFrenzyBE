using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Data.Migrations
{
    public partial class AddQuestionSets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_SubjectId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_SubjectId",
                table: "ExamAttempts");

            migrationBuilder.AddColumn<int>(
                name: "QuestionSetNumber",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "QuestionSetNumber",
                table: "ExamAttempts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SubjectId_QuestionSetNumber",
                table: "Questions",
                columns: new[] { "SubjectId", "QuestionSetNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_SubjectId_QuestionSetNumber_StudentId_Status",
                table: "ExamAttempts",
                columns: new[] { "SubjectId", "QuestionSetNumber", "StudentId", "Status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_SubjectId_QuestionSetNumber",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_SubjectId_QuestionSetNumber_StudentId_Status",
                table: "ExamAttempts");

            migrationBuilder.DropColumn(
                name: "QuestionSetNumber",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionSetNumber",
                table: "ExamAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SubjectId",
                table: "Questions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_SubjectId",
                table: "ExamAttempts",
                column: "SubjectId");
        }
    }
}
