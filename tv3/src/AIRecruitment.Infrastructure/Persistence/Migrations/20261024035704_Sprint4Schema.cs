using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ModelVersion",
                table: "MatchResults",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "AIModelVersionId",
                table: "MatchResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureValuesJson",
                table: "MatchResults",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Education",
                table: "CVAnalyses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsExperience",
                table: "CVAnalyses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AIModelVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Version = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Algorithm = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DatasetDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MetricsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    CVId = table.Column<int>(type: "int", nullable: true),
                    CoverLetter = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_CVs_CVId",
                        column: x => x.CVId,
                        principalTable: "CVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JobApplications_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Interviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MeetingUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interviews_JobApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Interviews_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_AIModelVersionId",
                table: "MatchResults",
                column: "AIModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchResults_JobId",
                table: "MatchResults",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CVAnalyses_CVId_CreatedAtUtc",
                table: "CVAnalyses",
                columns: new[] { "CVId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AIModelVersions_Version",
                table: "AIModelVersions",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ApplicationId_ScheduledAtUtc",
                table: "Interviews",
                columns: new[] { "ApplicationId", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_CreatedByUserId",
                table: "Interviews",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CandidateProfileId_JobId",
                table: "JobApplications",
                columns: new[] { "CandidateProfileId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CVId",
                table: "JobApplications",
                column: "CVId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobId_Status_AppliedAtUtc",
                table: "JobApplications",
                columns: new[] { "JobId", "Status", "AppliedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_CVAnalyses_CVs_CVId",
                table: "CVAnalyses",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchResults_AIModelVersions_AIModelVersionId",
                table: "MatchResults",
                column: "AIModelVersionId",
                principalTable: "AIModelVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchResults_CVs_CVId",
                table: "MatchResults",
                column: "CVId",
                principalTable: "CVs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchResults_Jobs_JobId",
                table: "MatchResults",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CVAnalyses_CVs_CVId",
                table: "CVAnalyses");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchResults_AIModelVersions_AIModelVersionId",
                table: "MatchResults");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchResults_CVs_CVId",
                table: "MatchResults");

            migrationBuilder.DropForeignKey(
                name: "FK_MatchResults_Jobs_JobId",
                table: "MatchResults");

            migrationBuilder.DropTable(
                name: "AIModelVersions");

            migrationBuilder.DropTable(
                name: "Interviews");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_MatchResults_AIModelVersionId",
                table: "MatchResults");

            migrationBuilder.DropIndex(
                name: "IX_MatchResults_JobId",
                table: "MatchResults");

            migrationBuilder.DropIndex(
                name: "IX_CVAnalyses_CVId_CreatedAtUtc",
                table: "CVAnalyses");

            migrationBuilder.DropColumn(
                name: "AIModelVersionId",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "FeatureValuesJson",
                table: "MatchResults");

            migrationBuilder.DropColumn(
                name: "Education",
                table: "CVAnalyses");

            migrationBuilder.DropColumn(
                name: "YearsExperience",
                table: "CVAnalyses");

            migrationBuilder.AlterColumn<string>(
                name: "ModelVersion",
                table: "MatchResults",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);
        }
    }
}
