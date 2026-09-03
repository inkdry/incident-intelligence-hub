using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentMitigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MitigatedAtUtc",
                table: "Incidents",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MitigatedAtUtc",
                table: "Incidents");
        }
    }
}
