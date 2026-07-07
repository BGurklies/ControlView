-- dim_date als zentrale Datumstabelle generieren (2022-01-01 bis 2024-12-31)
--
-- Kein Quelldaten-Import; Datumslogik wird direkt im SP berechnet.

CREATE OR ALTER PROCEDURE mart.sp_load_dim_date
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM mart.dim_date;

        DECLARE @start DATE = '2022-01-01';
        DECLARE @end   DATE = '2024-12-31';
        DECLARE @cur   DATE = @start;

        WHILE @cur <= @end
        BEGIN
            INSERT INTO mart.dim_date
                (date_key, full_date, year, quarter, quarter_name, month, month_name,
                 month_short, week, day, weekday_name, is_weekend, year_month)
            VALUES (
                CAST(FORMAT(@cur, 'yyyyMMdd') AS INT),
                @cur,
                YEAR(@cur),
                DATEPART(QUARTER, @cur),
                'Q' + CAST(DATEPART(QUARTER, @cur) AS NVARCHAR),
                MONTH(@cur),
                DATENAME(MONTH, @cur),
                LEFT(DATENAME(MONTH, @cur), 3),
                DATEPART(ISO_WEEK, @cur),
                DAY(@cur),
                DATENAME(WEEKDAY, @cur),
                CASE WHEN DATEPART(WEEKDAY, @cur) IN (1, 7) THEN 1 ELSE 0 END,
                FORMAT(@cur, 'yyyy-MM')
            );
            SET @cur = DATEADD(DAY, 1, @cur);
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
