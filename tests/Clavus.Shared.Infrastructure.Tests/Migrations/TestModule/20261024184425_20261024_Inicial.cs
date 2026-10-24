using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Clavus.Shared.Infrastructure.Tests.Migrations.TestModule
{
    /// <inheritdoc />
    public partial class _20261024_Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "teste_convencoes");

            migrationBuilder.CreateTable(
                name: "pedido_item",
                schema: "teste_convencoes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    data_criacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_entrega = table.Column<DateOnly>(type: "date", nullable: false),
                    data_cancelamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedido_item", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pedido_item",
                schema: "teste_convencoes");
        }
    }
}
