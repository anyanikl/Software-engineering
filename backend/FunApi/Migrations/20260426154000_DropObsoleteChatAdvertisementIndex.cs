using FunApi.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunApi.Migrations
{
    [DbContext(typeof(FunDBcontext))]
    [Migration("20260426154000_DropObsoleteChatAdvertisementIndex")]
    public partial class DropObsoleteChatAdvertisementIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "IX_Chats_AdvertisementId";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Chats_AdvertisementId",
                table: "Chats",
                column: "AdvertisementId");
        }
    }
}
