using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoCoinsApiNovo.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lojas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumeroWhatsApp = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZApiInstanceId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZApiToken = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZApiClientToken = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiKey = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lojas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Loja "seed" (Id=1) para a qual todos os dados existentes são migrados.
            // Credenciais Z-API/WhatsApp ficam vazias e devem ser preenchidas depois.
            migrationBuilder.InsertData(
                table: "Lojas",
                columns: new[] { "Id", "Nome", "NumeroWhatsApp", "ZApiInstanceId", "ZApiToken", "ZApiClientToken", "ApiKey", "Ativo", "DataCadastro" },
                values: new object[] { 1, "Gaia Skate & Surf", "", "", "", "", null, true, new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Pedido",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "HistoricoMovimentacao",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Clientes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "cashbacklote",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "LojaId",
                table: "Brindes",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_LojaId",
                table: "Usuarios",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedido_LojaId",
                table: "Pedido",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoMovimentacao_LojaId",
                table: "HistoricoMovimentacao",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_LojaId",
                table: "Clientes",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_cashbacklote_LojaId",
                table: "cashbacklote",
                column: "LojaId");

            migrationBuilder.CreateIndex(
                name: "IX_Brindes_LojaId",
                table: "Brindes",
                column: "LojaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Brindes_Lojas_LojaId",
                table: "Brindes",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cashbacklote_Lojas_LojaId",
                table: "cashbacklote",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Lojas_LojaId",
                table: "Clientes",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistoricoMovimentacao_Lojas_LojaId",
                table: "HistoricoMovimentacao",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pedido_Lojas_LojaId",
                table: "Pedido",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Lojas_LojaId",
                table: "Usuarios",
                column: "LojaId",
                principalTable: "Lojas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Brindes_Lojas_LojaId",
                table: "Brindes");

            migrationBuilder.DropForeignKey(
                name: "FK_cashbacklote_Lojas_LojaId",
                table: "cashbacklote");

            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Lojas_LojaId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_HistoricoMovimentacao_Lojas_LojaId",
                table: "HistoricoMovimentacao");

            migrationBuilder.DropForeignKey(
                name: "FK_Pedido_Lojas_LojaId",
                table: "Pedido");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Lojas_LojaId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Lojas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_LojaId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Pedido_LojaId",
                table: "Pedido");

            migrationBuilder.DropIndex(
                name: "IX_HistoricoMovimentacao_LojaId",
                table: "HistoricoMovimentacao");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_LojaId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_cashbacklote_LojaId",
                table: "cashbacklote");

            migrationBuilder.DropIndex(
                name: "IX_Brindes_LojaId",
                table: "Brindes");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Pedido");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "HistoricoMovimentacao");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "cashbacklote");

            migrationBuilder.DropColumn(
                name: "LojaId",
                table: "Brindes");
        }
    }
}
