-- ControlView Orchestration: Vollstaendiger ETL-Durchlauf
-- Fuehrt alle Raw- und Mart-SPs in Abhaengigkeitsreihenfolge aus.
--
-- Voraussetzung: DDL-Skripte in setup/, raw/schemas/, mart/schemas/ wurden ausgefuehrt (einmalig).
--
-- Ausfuehrung:
--   EXEC orchestration.sp_run_full_load @DataPath = 'C:\pfad\zu\data\raw';

CREATE OR ALTER PROCEDURE orchestration.sp_run_full_load
    @DataPath NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    -- Raw: Quelldaten laden
    EXEC raw.sp_load_kontenplan      @DataPath = @DataPath;
    EXEC raw.sp_load_kostenstellen   @DataPath = @DataPath;
    EXEC raw.sp_load_produktkatalog  @DataPath = @DataPath;
    EXEC raw.sp_load_buchungsjournal @DataPath = @DataPath;

    -- Fact zuerst leeren damit Dim-DELETEs nicht an FK-Constraints scheitern
    TRUNCATE TABLE mart.fact_journal;

    -- Mart: Dimensionen aufbauen
    EXEC mart.sp_load_dim_account;
    EXEC mart.sp_load_dim_costcenter;
    EXEC mart.sp_load_dim_product;
    EXEC mart.sp_load_dim_date;
    EXEC mart.sp_load_dim_scenario;
    EXEC mart.sp_load_dim_cost_type;

    -- Mart: Faktentabelle aufbauen
    EXEC mart.sp_load_fact_journal;

    -- Verifikation
    SELECT 'buchungsjournal' AS [table], COUNT(*) AS [rows] FROM raw.buchungsjournal
    UNION ALL SELECT 'dim_date',       COUNT(*) FROM mart.dim_date
    UNION ALL SELECT 'dim_account',    COUNT(*) FROM mart.dim_account
    UNION ALL SELECT 'dim_costcenter', COUNT(*) FROM mart.dim_costcenter
    UNION ALL SELECT 'dim_product',    COUNT(*) FROM mart.dim_product
    UNION ALL SELECT 'dim_scenario',   COUNT(*) FROM mart.dim_scenario
    UNION ALL SELECT 'dim_cost_type',  COUNT(*) FROM mart.dim_cost_type
    UNION ALL SELECT 'fact_journal',   COUNT(*) FROM mart.fact_journal;
END;
GO
