using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerminBooking.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Services.StaffId (nullable, bez defaulta 0)
            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "Services",
                type: "int",
                nullable: true);

            // Appointments.StaffId (nullable, bez defaulta 0)
            migrationBuilder.AddColumn<int>(
                name: "StaffId",
                table: "Appointments",
                type: "int",
                nullable: true);

            // Staff tablica
            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Skills = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ColorHex = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                });

            // Indeksi
            migrationBuilder.CreateIndex(
                name: "IX_Services_StaffId",
                table: "Services",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StaffId_Start_End",
                table: "Appointments",
                columns: new[] { "StaffId", "Start", "End" });

            // FK-ovi (SetNull jer su nullable)
            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Staff_StaffId",
                table: "Appointments",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Staff_StaffId",
                table: "Services",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Staff_StaffId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_Staff_StaffId",
                table: "Services");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Services_StaffId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_StaffId_Start_End",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Appointments");
        }
    }
}
