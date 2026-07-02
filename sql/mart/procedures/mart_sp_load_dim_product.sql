-- dim_product aus raw.produktkatalog aufbauen
--
-- Weist Surrogatschluessel zu; Stammdaten werden unveraendert uebernommen.
CREATE OR ALTER PROCEDURE mart.sp_load_dim_product
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM mart.dim_product;

    INSERT INTO mart.dim_product
        (product_key, product_id, product_name, product_type, margin_class)
    SELECT
        ROW_NUMBER() OVER (ORDER BY p.produkt_id) AS product_key,
        p.produkt_id,
        p.produkt_bezeichnung,
        p.produkt_typ,
        p.margenklasse
    FROM raw.produktkatalog p
    ORDER BY p.produkt_id;

END;
GO
