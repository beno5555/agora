using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace colosseumbackend.Data.Migrations
{
    /// <summary>
    /// Replaces the free-text UnitLabel with the optional ListingUnit enum (stored by name). Known words,
    /// singular or plural in any case, carry over; anything else becomes null ("parts").
    /// </summary>
    public partial class ListingUnitEnum : Migration
    {
        // The enum names when this migration was written; kept here so later enum changes can't alter it.
        private static readonly string[] Units =
            ["Match", "Game", "Round", "Win", "Run", "Level", "Rank", "Hour", "Day", "Session", "Item"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Listings",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            var cases = string.Join(" ", Units.Select(unit =>
                $"WHEN '{unit.ToLowerInvariant()}' THEN '{unit}' WHEN '{Plural(unit)}' THEN '{unit}'"));
            migrationBuilder.Sql($"UPDATE Listings SET Unit = CASE lower(trim(UnitLabel)) {cases} ELSE NULL END;");

            migrationBuilder.DropColumn(
                name: "UnitLabel",
                table: "Listings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnitLabel",
                table: "Listings",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE Listings SET UnitLabel = lower(coalesce(Unit, 'part'));");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Listings");
        }

        private static string Plural(string unit)
        {
            string lower = unit.ToLowerInvariant();
            return lower.EndsWith("ch") ? lower + "es" : lower + "s";
        }
    }
}
