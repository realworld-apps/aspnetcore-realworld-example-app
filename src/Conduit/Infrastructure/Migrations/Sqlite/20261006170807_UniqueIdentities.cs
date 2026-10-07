using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conduit.Infrastructure.Migrations.Sqlite;

/// <inheritdoc />
public partial class UniqueIdentities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Persons_Email",
            table: "Persons",
            column: "Email",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Persons_Username",
            table: "Persons",
            column: "Username",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Articles_Slug",
            table: "Articles",
            column: "Slug",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Persons_Email", table: "Persons");

        migrationBuilder.DropIndex(name: "IX_Persons_Username", table: "Persons");

        migrationBuilder.DropIndex(name: "IX_Articles_Slug", table: "Articles");
    }
}
