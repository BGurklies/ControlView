-- Referenztabelle fuer Produkt-Zuordnung (Deckungsbeitragsrechnung)
-- Definiert wie variable Konten (5xxx) auf Warengruppen aufgeteilt werden.
-- Konten ohne Eintrag -> PRD-900 Gemeinkosten (in mart_sp_load_fact_journal).
-- Mapping wird vom Controlling gepflegt; nicht aus Quelldaten importiert.

DROP TABLE IF EXISTS mart.konto_produkt_mapping;
GO

CREATE TABLE mart.konto_produkt_mapping (
    konto_id   CHAR(4)       NOT NULL,
    produkt_id NVARCHAR(10)  NOT NULL,
    gewicht    DECIMAL(5,4)  NOT NULL,
    CONSTRAINT PK_konto_produkt_mapping PRIMARY KEY (konto_id, produkt_id)
);
GO

INSERT INTO mart.konto_produkt_mapping (konto_id, produkt_id, gewicht) VALUES
-- Erloeskonten: 1:1-Zuordnung -- jedes Konto gehoert exklusiv einer Warengruppe
('4000', 'PRD-100', 1.0000),
('4100', 'PRD-200', 1.0000),
('4200', 'PRD-300', 1.0000),
('4300', 'PRD-400', 1.0000),
('4400', 'PRD-500', 1.0000),
-- Wareneinsatz: Geraete (Smartphones, Notebooks) mit hohem Wareneinsatz-Anteil
-- relativ zum Umsatz (duenne Marge); Zubehoer (Eigenmarke) mit niedrigem Anteil (Hochmarge)
('5000', 'PRD-100', 0.4400),
('5000', 'PRD-200', 0.2900),
('5000', 'PRD-300', 0.1700),
('5000', 'PRD-400', 0.0400),
('5000', 'PRD-500', 0.0600),
-- Versand & Fulfillment: nach Paketvolumen/-gewicht (Notebooks/Smart Home groesser)
('5100', 'PRD-100', 0.2000),
('5100', 'PRD-200', 0.2500),
('5100', 'PRD-300', 0.2000),
('5100', 'PRD-400', 0.1500),
('5100', 'PRD-500', 0.2000),
-- Payment-Gebuehren: proportional zum Umsatzanteil (Gebuehr = % vom Transaktionswert)
('5200', 'PRD-100', 0.3800),
('5200', 'PRD-200', 0.2600),
('5200', 'PRD-300', 0.2000),
('5200', 'PRD-400', 0.0800),
('5200', 'PRD-500', 0.0800),
-- Retourenkosten: hoechste Ruecksendequote bei Smartphones/Tablets (Fehlkaeufe,
-- Kompatibilitaet) plus groesstes Verkaufsvolumen; niedrig bei Zubehoer (kaum Retouren)
('5300', 'PRD-100', 0.4000),
('5300', 'PRD-200', 0.3000),
('5300', 'PRD-300', 0.1500),
('5300', 'PRD-400', 0.0500),
('5300', 'PRD-500', 0.1000);
GO
