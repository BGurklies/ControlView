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

        ;WITH kalender AS (
            SELECT DATEADD(DAY, s.value, @start) AS full_date
            FROM GENERATE_SERIES(0, DATEDIFF(DAY, @start, @end)) s
        ),
        monat (m_num, m_name, m_short) AS (
            SELECT * FROM (VALUES
                ( 1, N'Januar',    N'Jan'),
                ( 2, N'Februar',   N'Feb'),
                ( 3, N'März',      N'Mär'),
                ( 4, N'April',     N'Apr'),
                ( 5, N'Mai',       N'Mai'),
                ( 6, N'Juni',      N'Jun'),
                ( 7, N'Juli',      N'Jul'),
                ( 8, N'August',    N'Aug'),
                ( 9, N'September', N'Sep'),
                (10, N'Oktober',   N'Okt'),
                (11, N'November',  N'Nov'),
                (12, N'Dezember',  N'Dez')
            ) v (m_num, m_name, m_short)
        ),
        wochentag (dow, wd_name) AS (
            -- dow = Tagesabstand zum 1900-01-01 (Montag) modulo 7
            SELECT * FROM (VALUES
                (0, N'Montag'),
                (1, N'Dienstag'),
                (2, N'Mittwoch'),
                (3, N'Donnerstag'),
                (4, N'Freitag'),
                (5, N'Samstag'),
                (6, N'Sonntag')
            ) v (dow, wd_name)
        )
        INSERT INTO mart.dim_date
            (date_key, full_date, year, quarter, quarter_name, month, month_name,
             month_short, week, day, weekday_name, is_weekend, year_month)
        SELECT
            YEAR(k.full_date) * 10000 + MONTH(k.full_date) * 100 + DAY(k.full_date),
            k.full_date,
            YEAR(k.full_date),
            DATEPART(QUARTER, k.full_date),
            N'Q' + CAST(DATEPART(QUARTER, k.full_date) AS NVARCHAR(1)),
            MONTH(k.full_date),
            mo.m_name,
            mo.m_short,
            DATEPART(ISO_WEEK, k.full_date),
            DAY(k.full_date),
            wt.wd_name,
            CASE WHEN wt.dow IN (5, 6) THEN 1 ELSE 0 END,
            FORMAT(k.full_date, 'yyyy-MM')
        FROM kalender k
        JOIN monat     mo ON mo.m_num = MONTH(k.full_date)
        JOIN wochentag wt ON wt.dow   = DATEDIFF(DAY, '19000101', k.full_date) % 7;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
