-- dim_scenario befuellen: Plan (Jahresbudget) und Ist (Aktualwerte aus Buchungsjournal).

CREATE OR ALTER PROCEDURE mart.sp_load_dim_scenario
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM mart.dim_scenario;

    INSERT INTO mart.dim_scenario (scenario_key, scenario_id, scenario_label, scenario_order)
    VALUES
        (1, 'Plan', 'Budget', 1),
        (2, 'Ist',  'Ist',    2);

END;
GO
