using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoCoinsApiNovo.Migrations
{
    /// <inheritdoc />
    public partial class AddCashbackToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CashbackAcumulado",
                table: "Clientes",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashbackAcumulado",
                table: "Clientes");
        }
    }
}
