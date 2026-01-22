using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOutboxLockExpiresColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LockedExpiresAuUtc",
                table: "OutboxMessages",
                newName: "LockExpireAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LockExpireAtUtc",
                table: "OutboxMessages",
                newName: "LockedExpireAuUtc");
        }
    }
}
