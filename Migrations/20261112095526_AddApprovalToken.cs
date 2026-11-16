using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSafe.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalToken",
                table: "ClubMembers",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_unicode_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalToken",
                table: "ClubMembers");
        }
    }
}
