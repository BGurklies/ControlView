-- Faktentabelle

DROP TABLE IF EXISTS mart.fact_journal;
GO

CREATE TABLE mart.fact_journal (
    journal_key    INT           NOT NULL,
    date_key       INT           NOT NULL,
    account_key    INT           NOT NULL,
    costcenter_key INT           NOT NULL,
    product_key    INT           NOT NULL,
    cost_type_key  INT           NOT NULL,
    scenario_key   INT           NOT NULL,
    amount         DECIMAL(18,2) NOT NULL,  
    currency       CHAR(3)       NOT NULL CONSTRAINT DF_fact_journal_currency DEFAULT 'EUR',
    year           SMALLINT      NOT NULL,
    month          TINYINT       NOT NULL,
    year_month     NVARCHAR(7)   NOT NULL,
    CONSTRAINT PK_fact_journal PRIMARY KEY CLUSTERED (journal_key)
);
GO

-- FK-Constraints zu allen 6 Dimensionen
ALTER TABLE mart.fact_journal WITH CHECK
    ADD CONSTRAINT FK_fact_date       FOREIGN KEY (date_key)       REFERENCES mart.dim_date       (date_key);
ALTER TABLE mart.fact_journal WITH CHECK
    ADD CONSTRAINT FK_fact_account    FOREIGN KEY (account_key)    REFERENCES mart.dim_account    (account_key);
ALTER TABLE mart.fact_journal WITH CHECK
    ADD CONSTRAINT FK_fact_costcenter FOREIGN KEY (costcenter_key) REFERENCES mart.dim_costcenter (costcenter_key);
ALTER TABLE mart.fact_journal WITH CHECK
    ADD CONSTRAINT FK_fact_product    FOREIGN KEY (product_key)    REFERENCES mart.dim_product    (product_key);
ALTER TABLE mart.fact_journal WITH CHECK
    ADD CONSTRAINT FK_fact_scenario   FOREIGN KEY (scenario_key)   REFERENCES mart.dim_scenario   (scenario_key);
ALTER TABLE mart.fact_journal WITH CHECK
    ADD CONSTRAINT FK_fact_cost_type  FOREIGN KEY (cost_type_key)  REFERENCES mart.dim_cost_type  (cost_type_key);
GO

-- Reporting-Index: Jahr + Produkt + Konto + Szenario (haeufigste Filter-Kombination)
CREATE NONCLUSTERED INDEX IX_fact_journal_reporting
    ON mart.fact_journal (year, product_key, account_key, scenario_key)
    INCLUDE (amount, costcenter_key);
GO
