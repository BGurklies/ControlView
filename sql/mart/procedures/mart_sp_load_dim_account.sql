-- dim_account aus raw.kontenplan aufbauen
--
-- Reichert Rohdaten mit Controlling-Logik an:
--   account_category : Gruppierung fuer GuV-Ausweis
--   pl_line          : GuV-Position (Umsatz | COGS | OpEx)
--   pl_sort          : Sortierreihenfolge fuer Dashboard-Matrix
--   sign             : Vorzeichen (+1 Erloes / -1 Aufwand)

CREATE OR ALTER PROCEDURE mart.sp_load_dim_account
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM mart.dim_account;

        INSERT INTO mart.dim_account
            (account_key, account_id, account_name, account_category, pl_line, pl_sort, sign)
        SELECT
            ROW_NUMBER() OVER (ORDER BY k.konto_id) AS account_key,
            k.konto_id,
            k.konto_bezeichnung,
            CASE
                WHEN k.konto_id LIKE '4%'                                      THEN 'Erlöse'
                WHEN k.konto_id LIKE '5%'                                      THEN 'COGS'
                WHEN k.konto_id IN ('6000','6010','6020','6030','6100','6200') THEN 'Personalkosten'
                ELSE                                                                'Sachkosten'
            END AS account_category,
            CASE
                WHEN k.konto_id LIKE '4%' THEN 'Umsatz'
                WHEN k.konto_id LIKE '5%' THEN 'COGS'
                ELSE                           'OpEx'
            END AS pl_line,
            CASE
                WHEN k.konto_id LIKE '4%' THEN 1  -- Umsatz
                WHEN k.konto_id LIKE '5%' THEN 2  -- COGS
                ELSE                          3  -- OpEx (Personalkosten + Sachkosten)
            END AS pl_sort,
            CASE WHEN k.konto_id LIKE '4%' THEN 1 ELSE -1 END AS sign
        FROM raw.kontenplan k
        ORDER BY k.konto_id;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
