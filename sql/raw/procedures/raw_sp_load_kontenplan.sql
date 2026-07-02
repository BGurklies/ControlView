-- Kontenplan aus CSV in Raw laden

CREATE OR ALTER PROCEDURE raw.sp_load_kontenplan
    @DataPath NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE raw.kontenplan;

    DECLARE @sql NVARCHAR(MAX) =
        N'BULK INSERT raw.kontenplan'
        + N' FROM '''        + REPLACE(@DataPath, N'''', N'''''') + N'\kontenplan.csv'''
        + N' WITH ('
        +     N'FORMAT          = ''CSV'','
        +     N'FIRSTROW        = 2,'
        +     N'FIELDTERMINATOR = '','','
        +     N'ROWTERMINATOR   = ''\n'','
        +     N'CODEPAGE        = ''65001'','
        +     N'TABLOCK'
        + N')';

    EXEC sp_executesql @sql;
END;
GO
