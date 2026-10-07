using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conduit.Infrastructure.Migrations.SqlServer;

/// <inheritdoc />
public partial class InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Persons",
            columns: table => new
            {
                PersonId = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Hash = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                Salt = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Persons", x => x.PersonId);
            }
        );

        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                TagId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.TagId);
            }
        );

        migrationBuilder.CreateTable(
            name: "Articles",
            columns: table => new
            {
                ArticleId = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Slug = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                AuthorPersonId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Articles", x => x.ArticleId);
                table.ForeignKey(
                    name: "FK_Articles_Persons_AuthorPersonId",
                    column: x => x.AuthorPersonId,
                    principalTable: "Persons",
                    principalColumn: "PersonId"
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "FollowedPeople",
            columns: table => new
            {
                ObserverId = table.Column<int>(type: "int", nullable: false),
                TargetId = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FollowedPeople", x => new { x.ObserverId, x.TargetId });
                table.ForeignKey(
                    name: "FK_FollowedPeople_Persons_ObserverId",
                    column: x => x.ObserverId,
                    principalTable: "Persons",
                    principalColumn: "PersonId",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_FollowedPeople_Persons_TargetId",
                    column: x => x.TargetId,
                    principalTable: "Persons",
                    principalColumn: "PersonId",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "ArticleFavorites",
            columns: table => new
            {
                ArticleId = table.Column<int>(type: "int", nullable: false),
                PersonId = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArticleFavorites", x => new { x.ArticleId, x.PersonId });
                table.ForeignKey(
                    name: "FK_ArticleFavorites_Articles_ArticleId",
                    column: x => x.ArticleId,
                    principalTable: "Articles",
                    principalColumn: "ArticleId",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_ArticleFavorites_Persons_PersonId",
                    column: x => x.PersonId,
                    principalTable: "Persons",
                    principalColumn: "PersonId",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "ArticleTags",
            columns: table => new
            {
                ArticleId = table.Column<int>(type: "int", nullable: false),
                TagId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ArticleTags", x => new { x.ArticleId, x.TagId });
                table.ForeignKey(
                    name: "FK_ArticleTags_Articles_ArticleId",
                    column: x => x.ArticleId,
                    principalTable: "Articles",
                    principalColumn: "ArticleId",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_ArticleTags_Tags_TagId",
                    column: x => x.TagId,
                    principalTable: "Tags",
                    principalColumn: "TagId",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                CommentId = table
                    .Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                AuthorId = table.Column<int>(type: "int", nullable: false),
                ArticleId = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.CommentId);
                table.ForeignKey(
                    name: "FK_Comments_Articles_ArticleId",
                    column: x => x.ArticleId,
                    principalTable: "Articles",
                    principalColumn: "ArticleId",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_Comments_Persons_AuthorId",
                    column: x => x.AuthorId,
                    principalTable: "Persons",
                    principalColumn: "PersonId",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_ArticleFavorites_PersonId",
            table: "ArticleFavorites",
            column: "PersonId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Articles_AuthorPersonId",
            table: "Articles",
            column: "AuthorPersonId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_ArticleTags_TagId",
            table: "ArticleTags",
            column: "TagId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Comments_ArticleId",
            table: "Comments",
            column: "ArticleId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_Comments_AuthorId",
            table: "Comments",
            column: "AuthorId"
        );

        migrationBuilder.CreateIndex(
            name: "IX_FollowedPeople_TargetId",
            table: "FollowedPeople",
            column: "TargetId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ArticleFavorites");

        migrationBuilder.DropTable(name: "ArticleTags");

        migrationBuilder.DropTable(name: "Comments");

        migrationBuilder.DropTable(name: "FollowedPeople");

        migrationBuilder.DropTable(name: "Tags");

        migrationBuilder.DropTable(name: "Articles");

        migrationBuilder.DropTable(name: "Persons");
    }
}
