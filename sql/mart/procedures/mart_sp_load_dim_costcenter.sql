-- dim_costcenter aus raw.kostenstellen aufbauen
--
-- Weist Surrogatschluessel zu; Stammdaten werden unveraendert uebernommen.

CREATE OR ALTER PROCEDURE mart.sp_load_dim_costcenter
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM mart.dim_costcenter;

    INSERT INTO mart.dim_costcenter
        (costcenter_key, costcenter_id, costcenter_name, area, cost_owner_id)
    SELECT
        ROW_NUMBER() OVER (ORDER BY k.kostenstelle_id) AS costcenter_key,
        k.kostenstelle_id,
        k.kostenstelle_bezeichnung,
        k.bereich,
        k.cost_owner_id
    FROM raw.kostenstellen k
    ORDER BY k.kostenstelle_id;

END;
GO
