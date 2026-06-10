using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PontoCoinsApiNovo.Migrations
{
    /// <inheritdoc />
    public partial class RefatorarUsuarioIndependente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL não permite ADD COLUMN AUTO_INCREMENT sem PK simultânea.
            // Solução: recriar a tabela Usuarios do zero (dados de admin são dispensáveis).
            migrationBuilder.Sql("DROP TABLE IF EXISTS `Usuarios`;");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cargo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCadastro = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UserName",
                table: "Usuarios",
                column: "UserName",
                unique: true);

            // Adiciona DataNascimento apenas se ainda não existir (idempotente)
            migrationBuilder.Sql(@"
                DROP PROCEDURE IF EXISTS `_AddColIfNotExists`;
                CREATE PROCEDURE `_AddColIfNotExists`()
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.COLUMNS
                        WHERE TABLE_SCHEMA = SCHEMA()
                          AND TABLE_NAME   = 'Clientes'
                          AND COLUMN_NAME  = 'DataNascimento'
                    ) THEN
                        ALTER TABLE `Clientes` ADD COLUMN `DataNascimento` datetime(6) NULL;
                    END IF;
                END;
                CALL `_AddColIfNotExists`();
                DROP PROCEDURE `_AddColIfNotExists`;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `cashbacklote` (
                    `Id`               int            NOT NULL AUTO_INCREMENT,
                    `CpfCliente`       varchar(14)    NOT NULL,
                    `Valor`            decimal(10,2)  NOT NULL,
                    `Restante`         decimal(10,2)  NOT NULL,
                    `DataGerado`       datetime(6)    NOT NULL,
                    `DataExpiracao`    datetime(6)    NOT NULL,
                    `Ativo`            tinyint(1)     NOT NULL,
                    `HistoricoOrigemId` int           NULL,
                    CONSTRAINT `PK_cashbacklote` PRIMARY KEY (`Id`)
                ) CHARACTER SET=utf8mb4;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cashbacklote");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_UserName",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DataNascimento",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Usuarios",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Usuarios",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataUltimaMovimentacao",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "Usuarios",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Usuarios",
                table: "Usuarios",
                column: "Cpf");
        }
    }
}
