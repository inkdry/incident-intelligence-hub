using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentIntelligence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Incidents",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SummaryGeneratedAtUtc",
                table: "Incidents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryModel",
                table: "Incidents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SummarySourceVersion",
                table: "Incidents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Incidents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SummaryGeneratedAtUtc",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SummaryModel",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SummarySourceVersion",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Incidents");
        }
    }
}
