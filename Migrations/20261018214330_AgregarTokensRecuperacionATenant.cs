using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioSaaS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTokensRecuperacionATenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpires",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpires",
                table: "Tenants");
        }
    }
}
