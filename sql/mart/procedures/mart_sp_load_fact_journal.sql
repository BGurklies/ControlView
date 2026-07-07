-- fact_journal aus raw.buchungsjournal aufbauen
--
-- Logik:
--   Variable Konten (in konto_produkt_mapping):
--     -> Eine Zeile je Produkt-Mapping-Eintrag, Betrag * Gewicht
--     -> cost_type = 'variabel'
--   Fixe Konten (nicht in konto_produkt_mapping; alle 6xxx OpEx-Konten):
--     -> Eine Zeile mit PRD-900 Gemeinkosten
--     -> cost_type = 'fix'
--
-- Betraege bleiben vorzeichenlos; Vorzeichen wird in der View (v_pl_monthly) per mart.dim_account.sign aufgeloest.

CREATE OR ALTER PROCEDURE mart.sp_load_fact_journal
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE mart.fact_journal;

    INSERT INTO mart.fact_journal
        (journal_key, date_key, account_key, costcenter_key, product_key,
         cost_type_key, scenario_key, amount, currency, year, month, year_month)
    SELECT
        ROW_NUMBER() OVER (
            ORDER BY src.buchungsdatum, src.konto_id, src.kostenstelle_id, src.szenario, src.produkt_id
        )                                                        AS journal_key,
        d.date_key,
        a.account_key,
        cc.costcenter_key,
        p.product_key,
        ct.cost_type_key,
        sc.scenario_key,
        ROUND(src.betrag * src.gewicht, 2)                      AS amount,
        'EUR'                                                    AS currency,
        YEAR(src.buchungsdatum)                                  AS year,
        MONTH(src.buchungsdatum)                                 AS month,
        FORMAT(src.buchungsdatum, 'yyyy-MM')                     AS year_month
    FROM (
        -- Variable Konten: Betrag auf Produkte aufteilen
        SELECT
            b.buchungsdatum, b.konto_id, b.kostenstelle_id, b.szenario,
            m.produkt_id, b.betrag, m.gewicht, 'variabel' AS cost_type
        FROM raw.buchungsjournal        b
        JOIN mart.konto_produkt_mapping m ON m.konto_id = b.konto_id

        UNION ALL

        -- Fixe Konten: nicht in Mapping -> Gemeinkosten
        SELECT
            b.buchungsdatum, b.konto_id, b.kostenstelle_id, b.szenario,
            'PRD-900', b.betrag, 1.0000, 'fix'
        FROM raw.buchungsjournal b
        WHERE NOT EXISTS (
            SELECT 1 FROM mart.konto_produkt_mapping m WHERE m.konto_id = b.konto_id
        )
    ) src
    JOIN mart.dim_account    a  ON a.account_id      = src.konto_id
    JOIN mart.dim_costcenter cc ON cc.costcenter_id  = src.kostenstelle_id
    JOIN mart.dim_product    p  ON p.product_id      = src.produkt_id
    JOIN mart.dim_scenario   sc ON sc.scenario_id    = src.szenario
    JOIN mart.dim_cost_type  ct ON ct.cost_type_id   = src.cost_type
    JOIN mart.dim_date       d  ON d.date_key        =
        YEAR(src.buchungsdatum) * 10000 + MONTH(src.buchungsdatum) * 100 + DAY(src.buchungsdatum);

END;
GO
