using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSafe.Migrations
{
    /// <inheritdoc />
    public partial class AddProofOfPaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProofOfPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ContributionId = table.Column<int>(type: "int", nullable: true),
                    FineId = table.Column<int>(type: "int", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedById = table.Column<int>(type: "int", nullable: false),
                    FilePath = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci"),
                    FileName = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci"),
                    Status = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci"),
                    AdminNotes = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci"),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProofOfPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProofOfPayments_Contributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "Contributions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProofOfPayments_Fines_FineId",
                        column: x => x.FineId,
                        principalTable: "Fines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProofOfPayments_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfPayments_ContributionId",
                table: "ProofOfPayments",
                column: "ContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfPayments_FineId",
                table: "ProofOfPayments",
                column: "FineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProofOfPayments_UploadedById",
                table: "ProofOfPayments",
                column: "UploadedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProofOfPayments");
        }
    }
}
