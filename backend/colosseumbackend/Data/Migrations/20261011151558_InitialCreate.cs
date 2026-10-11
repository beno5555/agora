using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace colosseumbackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Entity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    Wallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthNonces",
                columns: table => new
                {
                    Wallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthNonces", x => x.Wallet);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Wallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Wallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Discord = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Wallet);
                });

            migrationBuilder.CreateTable(
                name: "Listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SellerWallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    Milestones = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitLabel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DeliveryWindowSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Listings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Listings_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Listings_Users_SellerWallet",
                        column: x => x.SellerWallet,
                        principalTable: "Users",
                        principalColumn: "Wallet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OnchainDealId = table.Column<long>(type: "INTEGER", nullable: false),
                    DealPda = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    ListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuyerWallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    SellerWallet = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    BuyerNote = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Price = table.Column<long>(type: "INTEGER", nullable: false),
                    Milestones = table.Column<int>(type: "INTEGER", nullable: false),
                    MilestonesReleased = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyerDeposit = table.Column<long>(type: "INTEGER", nullable: false),
                    SellerDeposit = table.Column<long>(type: "INTEGER", nullable: false),
                    DeliveryWindowSeconds = table.Column<long>(type: "INTEGER", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SplitOfferBy = table.Column<string>(type: "TEXT", maxLength: 44, nullable: true),
                    SplitOfferBuyerAmount = table.Column<long>(type: "INTEGER", nullable: true),
                    LastSignature = table.Column<string>(type: "TEXT", maxLength: 88, nullable: true),
                    DeadlineReminderSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefundNoticeSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deals_Listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "Listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "IconUrl", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, null, "Fortnite", "fortnite" },
                    { 2, null, "Valorant", "valorant" },
                    { 3, null, "League of Legends", "league-of-legends" },
                    { 4, null, "Counter-Strike 2", "cs2" },
                    { 5, null, "Apex Legends", "apex-legends" },
                    { 6, null, "World of Warcraft", "world-of-warcraft" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_BuyerWallet",
                table: "Deals",
                column: "BuyerWallet");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_DealPda",
                table: "Deals",
                column: "DealPda",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deals_ListingId",
                table: "Deals",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_SellerWallet",
                table: "Deals",
                column: "SellerWallet");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_Status",
                table: "Deals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Games_Slug",
                table: "Games",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_GameId_IsActive",
                table: "Listings",
                columns: new[] { "GameId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_SellerWallet",
                table: "Listings",
                column: "SellerWallet");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Wallet_ReadAt",
                table: "Notifications",
                columns: new[] { "Wallet", "ReadAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "AuthNonces");

            migrationBuilder.DropTable(
                name: "Deals");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Listings");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
