using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAtUtc",
                table: "Incidents",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "Incidents");
        }
    }
}
