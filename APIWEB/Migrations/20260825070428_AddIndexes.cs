using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APIWEB.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Values_ResultId",
                table: "Values");

            migrationBuilder.CreateIndex(
                name: "IX_Values_ResultId_Date",
                table: "Values",
                columns: new[] { "ResultId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Results_FileName",
                table: "Results",
                column: "FileName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Values_ResultId_Date",
                table: "Values");

            migrationBuilder.DropIndex(
                name: "IX_Results_FileName",
                table: "Results");

            migrationBuilder.CreateIndex(
                name: "IX_Values_ResultId",
                table: "Values",
                column: "ResultId");
        }
    }
}
