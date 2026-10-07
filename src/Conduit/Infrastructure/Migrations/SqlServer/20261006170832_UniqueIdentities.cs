using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conduit.Infrastructure.Migrations.SqlServer;

/// <inheritdoc />
public partial class UniqueIdentities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Persons",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Persons",
            type: "nvarchar(320)",
            maxLength: 320,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Slug",
            table: "Articles",
            type: "nvarchar(450)",
            maxLength: 450,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_Persons_Email",
            table: "Persons",
            column: "Email",
            unique: true,
            filter: "[Email] IS NOT NULL"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Persons_Username",
            table: "Persons",
            column: "Username",
            unique: true,
            filter: "[Username] IS NOT NULL"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Articles_Slug",
            table: "Articles",
            column: "Slug",
            unique: true,
            filter: "[Slug] IS NOT NULL"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Persons_Email", table: "Persons");

        migrationBuilder.DropIndex(name: "IX_Persons_Username", table: "Persons");

        migrationBuilder.DropIndex(name: "IX_Articles_Slug", table: "Articles");

        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Persons",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(256)",
            oldMaxLength: 256,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Persons",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(320)",
            oldMaxLength: 320,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Slug",
            table: "Articles",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)",
            oldMaxLength: 450,
            oldNullable: true
        );
    }
}
