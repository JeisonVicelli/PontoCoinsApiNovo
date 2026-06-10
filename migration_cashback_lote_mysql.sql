-- ============================================================
-- Migration: CashbackLotes
-- Execute no MySQL Workbench ANTES de rodar a aplicação
-- ============================================================

CREATE TABLE IF NOT EXISTS `CashbackLotes` (
    `Id`             INT            NOT NULL AUTO_INCREMENT,
    `CpfCliente`     VARCHAR(14)    NOT NULL,
    `Valor`          DECIMAL(10,2)  NOT NULL,
    `DataCriacao`    DATETIME       NOT NULL DEFAULT NOW(),
    `DataExpiracao`  DATETIME       NOT NULL,
    `Utilizado`      TINYINT(1)     NOT NULL DEFAULT 0,
    `Origem`         VARCHAR(50)    NULL COMMENT 'Compra | Bonus | Ajuste',
    PRIMARY KEY (`Id`),
    INDEX `idx_cpf`        (`CpfCliente`),
    INDEX `idx_expiracao`  (`DataExpiracao`),
    INDEX `idx_ativo`      (`CpfCliente`, `Utilizado`, `DataExpiracao`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
