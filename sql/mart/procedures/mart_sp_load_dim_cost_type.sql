-- dim_cost_type mit Kostentyp befuellen
--
-- Nur 2 Typen: variabel (direkt zurechenbare Kosten) und fix (Gemeinkosten).

CREATE OR ALTER PROCEDURE mart.sp_load_dim_cost_type
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM mart.dim_cost_type;

    INSERT INTO mart.dim_cost_type (cost_type_key, cost_type_id, cost_type_label, cost_type_order)
    VALUES
        (1, 'variabel', 'Variable Kosten', 1),
        (2, 'fix',      'Fixkosten',       2);

END;
GO
