using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MSM.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RealtorId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId_Unique",
                table: "Reviews",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RealtorId_SlotStart_Active",
                table: "Appointments",
                columns: new[] { "RealtorId", "SlotStart" },
                unique: true,
                filter: "[Status] != 'cancelled'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_AppointmentId_Unique",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RealtorId_SlotStart_Active",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_AppointmentId",
                table: "Reviews",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RealtorId",
                table: "Appointments",
                column: "RealtorId");
        }
    }
}
