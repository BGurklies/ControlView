-- Vorzeichenkorrekter Join aller Dimensionen auf fact_journal.

CREATE OR ALTER VIEW mart.v_pl_monthly AS
SELECT
    -- Datum
    d.full_date,
    d.year,
    d.quarter,
    d.quarter_name,
    d.month,
    d.month_name,
    d.month_short,
    d.year_month,

    -- Konto
    a.account_id,
    a.account_name,
    a.account_category,
    a.pl_line,
    a.pl_sort,
    a.sign,

    -- Kostenstelle
    cc.costcenter_id,
    cc.costcenter_name,
    cc.area,
    cc.cost_owner_id,

    -- Produkt
    p.product_id,
    p.product_name,
    p.product_type,
    p.margin_class,

    -- Szenario
    s.scenario_id,
    s.scenario_label,
    s.scenario_order,

    -- Kostenart
    ct.cost_type_id,
    ct.cost_type_label,
    ct.cost_type_order,

    -- Betrag
    f.amount                    AS raw_amount,
    f.amount * a.sign           AS signed_amount,
    f.currency

FROM mart.fact_journal          f
JOIN mart.dim_date          d   ON d.date_key        = f.date_key
JOIN mart.dim_account       a   ON a.account_key     = f.account_key
JOIN mart.dim_costcenter    cc  ON cc.costcenter_key = f.costcenter_key
JOIN mart.dim_product       p   ON p.product_key     = f.product_key
JOIN mart.dim_scenario      s   ON s.scenario_key    = f.scenario_key
JOIN mart.dim_cost_type     ct  ON ct.cost_type_key  = f.cost_type_key;
GO
