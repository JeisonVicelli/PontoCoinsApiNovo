using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoCoinsApiNovo.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecisaTrocarSenhaToCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default true para novos registros (cadastro inline gera senha que precisa ser trocada).
            migrationBuilder.AddColumn<bool>(
                name: "PrecisaTrocarSenha",
                table: "Clientes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            // Clientes já existentes não escolheram a senha por esse motivo agora — não forçar troca.
            migrationBuilder.Sql("UPDATE Clientes SET PrecisaTrocarSenha = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrecisaTrocarSenha",
                table: "Clientes");
        }
    }
}
