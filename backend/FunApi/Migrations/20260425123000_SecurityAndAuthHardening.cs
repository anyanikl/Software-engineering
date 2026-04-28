using System;
using FunApi.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FunApi.Migrations
{
    [DbContext(typeof(FunDBcontext))]
    [Migration("20260425123000_SecurityAndAuthHardening")]
    public partial class SecurityAndAuthHardening : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiresAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH duplicate_chats AS (
                    SELECT
                        "Id",
                        MIN("Id") OVER (PARTITION BY "AdvertisementId", "BuyerId", "SellerId") AS "CanonicalId"
                    FROM "Chats"
                )
                UPDATE "Messages" AS m
                SET "ChatId" = d."CanonicalId"
                FROM duplicate_chats AS d
                WHERE m."ChatId" = d."Id"
                  AND d."Id" <> d."CanonicalId";
                """);

            migrationBuilder.Sql(
                """
                WITH duplicate_chats AS (
                    SELECT
                        "Id",
                        MIN("Id") OVER (PARTITION BY "AdvertisementId", "BuyerId", "SellerId") AS "CanonicalId"
                    FROM "Chats"
                )
                DELETE FROM "Chats" AS c
                USING duplicate_chats AS d
                WHERE c."Id" = d."Id"
                  AND d."Id" <> d."CanonicalId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_AdvertisementId_BuyerId_SellerId",
                table: "Chats",
                columns: new[] { "AdvertisementId", "BuyerId", "SellerId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_AdvertisementId_BuyerId_SellerId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenHash",
                table: "Users");
        }
    }
}
