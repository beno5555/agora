using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace colosseumbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClaimNoticeSent",
                table: "Deals",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReviewReminderSent",
                table: "Deals",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "ReviewWindowSeconds",
                table: "Deals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimNoticeSent",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ReviewReminderSent",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ReviewWindowSeconds",
                table: "Deals");
        }
    }
}
