using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIRecruitment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecruiterVerificationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecruiterVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RecruiterAccessRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ApplicantFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApplicantPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ApplicantPosition = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CompanyAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CompanyDescription = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    ApplicantNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DocumentOriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DocumentStoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruiterAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruiterAccessRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecruiterAccessRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterAccessRequests_ReviewedByUserId",
                table: "RecruiterAccessRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterAccessRequests_TaxCode_Status",
                table: "RecruiterAccessRequests",
                columns: new[] { "TaxCode", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterAccessRequests_UserId_Status",
                table: "RecruiterAccessRequests",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecruiterAccessRequests");

            migrationBuilder.DropColumn(
                name: "IsRecruiterVerified",
                table: "Users");
        }
    }
}
