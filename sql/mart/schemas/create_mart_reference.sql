-- Referenztabelle fuer Produkt-Zuordnung (Deckungsbeitragsrechnung)
-- Definiert wie variable Konten auf Produkte aufgeteilt werden.
-- Konten ohne Eintrag -> SWS-900 Gemeinkosten (in mart_sp_load_fact_journal).
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
-- Erlöskonten: 1:1-Zuordnung — jedes Konto gehoert exklusiv einem Produkt
('4000', 'SWS-100', 1.0000),
('4100', 'SWS-200', 1.0000),
('4200', 'SWS-300', 1.0000),
('4300', 'SWS-400', 1.0000),
-- Cloud-Infrastruktur: skaliert hauptsaechlich mit Lizenznutzern (SWS-100)
('5000', 'SWS-100', 0.8000),
('5000', 'SWS-200', 0.0600),
('5000', 'SWS-300', 0.1200),
('5000', 'SWS-400', 0.0200),
-- Drittanbieter-Plattformlizenzen: aehnliche Verteilung wie Cloud-Infrastruktur, etwas mehr PS-Anteil (SWS-200)
('5100', 'SWS-100', 0.7500),
('5100', 'SWS-200', 0.1000),
('5100', 'SWS-300', 0.1200),
('5100', 'SWS-400', 0.0300),
-- PS Delivery-Kosten: hauptsaechlich Professional Services (SWS-200) und Consulting (SWS-400)
('5200', 'SWS-100', 0.0500),
('5200', 'SWS-200', 0.7000),
('5200', 'SWS-300', 0.1000),
('5200', 'SWS-400', 0.1500);
GO
