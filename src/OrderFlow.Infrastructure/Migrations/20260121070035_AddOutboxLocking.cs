using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAtUtc",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "OutboxMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedExpiresAuUtc",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedExpiresAuUtc",
                table: "OutboxMessages");
        }
    }
}
