using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSafe.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "ClubMembers",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_unicode_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "ClubMembers");
        }
    }
}
