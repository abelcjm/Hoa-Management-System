using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HOA_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class AddDelinquencyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DelinquencyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    DelinquencyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalHomeowners = table.Column<int>(type: "int", nullable: false),
                    DelinquentCount = table.Column<int>(type: "int", nullable: false),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelinquencyRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DelinquencyRecords");
        }
    }
}
