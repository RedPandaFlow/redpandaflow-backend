using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedPandaFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveAvatarToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "Users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "AvatarData",
                table: "Users",
                type: "bytea",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Users\" SET \"AvatarUrl\" = NULL WHERE \"AvatarUrl\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarData",
                table: "Users");
        }
    }
}
