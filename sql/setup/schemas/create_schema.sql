-- Schema: raw              : Rohdaten-Tabellen fuer CSV-Importe (1:1 Quelldaten)
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'raw')
    EXEC('CREATE SCHEMA raw');
GO
-- Schema: mart             : Dimensionen, Faktentabelle, Referenzdaten
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'mart')
    EXEC('CREATE SCHEMA mart');
GO
-- Schema: orchestration    : ETL-Orchestrierungs-SP (CSV -> Raw -> Mart)
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'orchestration')
    EXEC('CREATE SCHEMA orchestration');
GO
