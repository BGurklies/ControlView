-- Raw-Tabellen

DROP TABLE IF EXISTS raw.kontenplan;
GO
CREATE TABLE raw.kontenplan (
    konto_id          CHAR(4)       NOT NULL,
    konto_bezeichnung NVARCHAR(100) NOT NULL,
    konto_art         NVARCHAR(20)  NOT NULL   -- 'Erloes' | 'Aufwand'
);
GO

DROP TABLE IF EXISTS raw.kostenstellen;
GO
CREATE TABLE raw.kostenstellen (
    kostenstelle_id          NVARCHAR(10)  NOT NULL,
    kostenstelle_bezeichnung NVARCHAR(100) NOT NULL,
    bereich                  NVARCHAR(50)  NOT NULL,
    cost_owner_id            NVARCHAR(10)  NOT NULL
);
GO

DROP TABLE IF EXISTS raw.produktkatalog;
GO
CREATE TABLE raw.produktkatalog (
    produkt_id          NVARCHAR(10)  NOT NULL,
    produkt_bezeichnung NVARCHAR(100) NOT NULL,
    produkt_typ         NVARCHAR(20)  NOT NULL,
    margenklasse        NVARCHAR(20)  NOT NULL
);
GO

DROP TABLE IF EXISTS raw.buchungsjournal;
GO
CREATE TABLE raw.buchungsjournal (
    buchungsdatum   DATE          NOT NULL,
    belegnr         NVARCHAR(20)  NOT NULL,
    konto_id        CHAR(4)       NOT NULL,
    kostenstelle_id NVARCHAR(10)  NOT NULL,
    betrag          DECIMAL(18,2) NOT NULL,   -- immer positiv; Richtung per soll_haben
    soll_haben      CHAR(1)       NOT NULL,   -- 'S' = Soll/Aufwand | 'H' = Haben/Erloes
    buchungstext    NVARCHAR(200) NOT NULL,
    szenario        NVARCHAR(10)  NOT NULL,   -- 'Plan' | 'Ist'
    geschaeftsjahr  SMALLINT      NOT NULL,
    periode         TINYINT       NOT NULL
);
GO
