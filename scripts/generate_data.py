"""
ControlView - Rohdaten-Generator

Simuliert DATEV-Controlling-Extrakte und Stammdaten eines mittelstaendischen
Online-Haendlers fuer Consumer Electronics (~50 MA, ~32M EUR Umsatz, eigener Shop + Marktplaetze).

Erzeugte Raw-CSVs (data/raw/):
  buchungsjournal.csv   : GL-Buchungen (Controlling-Extrakt, SKR04)
  kontenplan.csv        : Kontenstamm (SKR04-basiert)
  kostenstellen.csv     : Kostenstellenstamm (nach Funktionsbereich)
  produktkatalog.csv    : Produktstamm (Warengruppen)

Dim- und Fact-Tabellen entstehen erst auf DB-Ebene via orchestration.sp_run_full_load.
Produktzuordnung (DB I) erfolgt in mart.sp_load_fact_journal
via mart.konto_produkt_mapping.

Deckungsbeitrag I je Warengruppe = Umsatz - variable Kosten (Wareneinsatz, Versand,
Payment-Gebuehren, Retourenkosten). Geraete (Smartphones, Notebooks, Smart Home)
tragen duenne bis mittlere Handelswaren-Margen; Zubehoer ist Eigenmarke und traegt
die Marge (Profit-Engine).

Saisonalitaet:
  Plan : Q4-lastig (Weihnachtsgeschaeft), Fruehjahr/Sommer schwach (keine Geschenksaison)
  Ist  : weicht vom saisonalen Plan durch Erloes-/Kosten-Schwankung und Jahr-Shape ab
         Erloes-Varianz: +/-10% (0.90-1.10 x plan_base), Kosten-Varianz: +/-4% (0.96-1.04 x plan_base)
         Jahr-Shape: +/-5% pro Jahr/Monat

Wachstum: 2023 +7,0% ggue. Vorjahr, 2024 +7,5% ggue. Vorjahr
(growth-Werte im Code sind Multiplikatoren auf die fixe 2022-Basis, kein Kettenwachstum)

Plan EBIT-Basis 2022: ~7% Marge (Details siehe base_plan in generate_buchungsjournal)
"""

import logging
import pandas as pd
import numpy as np
from calendar import monthrange
from pathlib import Path

logging.basicConfig(level=logging.INFO, format="%(levelname)s  %(message)s")

SEED = 42
np.random.seed(SEED)

OUTPUT_DIR = Path("data/raw")
OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

MONTH_NAMES_DE = {
    1: "Januar", 2: "Februar", 3: "Maerz", 4: "April",
    5: "Mai", 6: "Juni", 7: "Juli", 8: "August",
    9: "September", 10: "Oktober", 11: "November", 12: "Dezember"
}


def generate_kontenplan():
    rows = [
        # Erloese (4xxx)
        ("4000", "Umsatzerlöse Smartphones & Tablets",   "Erlös"),
        ("4100", "Umsatzerlöse Notebooks & PC",          "Erlös"),
        ("4200", "Umsatzerlöse Audio & Wearables",       "Erlös"),
        ("4300", "Umsatzerlöse Zubehör",                 "Erlös"),
        ("4400", "Umsatzerlöse Smart Home",              "Erlös"),
        # COGS (5xxx) - variable Kosten je Warengruppe (Basis Deckungsbeitrag I)
        ("5000", "Wareneinsatz Handelswaren",            "Aufwand"),
        ("5100", "Versand & Fulfillment",                "Aufwand"),
        ("5200", "Payment- & Transaktionsgebühren",      "Aufwand"),
        ("5300", "Retourenkosten & Wertberichtigung",     "Aufwand"),
        # Personalkosten (6xxx)
        ("6000", "Gehälter Einkauf & Category Management", "Aufwand"),
        ("6010", "Gehälter Marketing & E-Commerce",      "Aufwand"),
        ("6020", "Gehälter Kundenservice",               "Aufwand"),
        ("6030", "Gehälter Verwaltung & Leitung",        "Aufwand"),
        ("6100", "Sozialabgaben & Benefits",             "Aufwand"),
        ("6200", "Betriebliche Altersvorsorge",          "Aufwand"),
        # Sachkosten (6xxx)
        ("6300", "Performance Marketing & Werbung",      "Aufwand"),
        ("6400", "Software, Shop & Tools",               "Aufwand"),
        ("6500", "Lager & Logistik",                     "Aufwand"),
        ("6600", "IT & Zahlungsinfrastruktur",           "Aufwand"),
        ("6700", "Abschreibungen",                       "Aufwand"),
        ("6800", "Sonstige Betriebskosten",              "Aufwand"),
    ]
    return pd.DataFrame(rows, columns=["konto_id", "konto_bezeichnung", "konto_art"])


def generate_kostenstellen():
    rows = [
        # Einkauf
        ("KST-100", "Einkauf & Category Management",     "Einkauf",     "EMP-1041"),
        ("KST-110", "Lieferanten & Disposition",         "Einkauf",     "EMP-1063"),
        # Marketing
        ("KST-200", "Performance Marketing",             "Marketing",   "EMP-2011"),
        ("KST-210", "Content & Marktplätze",             "Marketing",   "EMP-2024"),
        ("KST-220", "CRM & Retention",                   "Marketing",   "EMP-2035"),
        # Service
        ("KST-300", "Kundenservice",                     "Service",     "EMP-3007"),
        ("KST-310", "Retouren & Reklamation",            "Service",     "EMP-3019"),
        # Logistik
        ("KST-400", "Lager & Kommissionierung",          "Logistik",    "EMP-4005"),
        ("KST-410", "Versand & Fulfillment",             "Logistik",    "EMP-4017"),
        ("KST-420", "Wareneingang",                      "Logistik",    "EMP-4028"),
        # Verwaltung
        ("KST-500", "Finance & Controlling",             "Verwaltung",  "EMP-5002"),
        ("KST-600", "People & Culture",                  "Verwaltung",  "EMP-5014"),
        ("KST-700", "IT & Shop-Technik",                 "Verwaltung",  "EMP-5023"),
        ("KST-900", "Geschäftsführung",                  "Verwaltung",  "EMP-5001"),
    ]
    return pd.DataFrame(rows, columns=[
        "kostenstelle_id", "kostenstelle_bezeichnung", "bereich", "cost_owner_id"
    ])


def generate_produktkatalog():
    rows = [
        ("PRD-100", "Smartphones & Tablets",   "Handelsware", "Volumen"),
        ("PRD-200", "Notebooks & PC",          "Handelsware", "Volumen"),
        ("PRD-300", "Audio & Wearables",       "Handelsware", "Standard"),
        ("PRD-400", "Zubehör",                 "Eigenmarke",  "Hochmarge"),
        ("PRD-500", "Smart Home",              "Handelsware", "Standard"),
        ("PRD-900", "Gemeinkosten",            "Gemeinkosten", "n/a"),
    ]
    return pd.DataFrame(rows, columns=[
        "produkt_id", "produkt_bezeichnung", "produkt_typ", "margenklasse"
    ])


def generate_buchungsjournal():
    """
    Rohe GL-Buchungen ohne Produktzuordnung und ohne Vorzeichenlogik.
    Vorzeichen wird durch soll_haben ('S'=Aufwand / 'H'=Erloes) abgebildet.
    Betraege sind immer positiv.

    Jaehrliche Planwerte 2022 (base_plan-Eintraege sind Monatswerte, x 12):
      Umsatz Gesamt:  30.000.000 EUR  (~2.500k/Monat)
      Kosten Gesamt:  27.900.000 EUR  (~2.325k/Monat)
      EBIT:            2.100.000 EUR  (~175k/Monat, ~7% EBIT-Marge)
    """

    base_plan = {
        # Erloese (4xxx)
        "4000": 960_000,   # Smartphones & Tablets
        "4100": 660_000,   # Notebooks & PC
        "4200": 500_000,   # Audio & Wearables
        "4300": 210_000,   # Zubehoer
        "4400": 170_000,   # Smart Home
        # COGS (5xxx) - variable Kosten (Basis DB I)
        "5000": 1_820_000, # Wareneinsatz Handelswaren
        "5100":  90_000,   # Versand & Fulfillment
        "5200":  55_000,   # Payment- & Transaktionsgebuehren
        "5300":  57_000,   # Retourenkosten & Wertberichtigung
        # Personalkosten (6xxx)
        "6000":  40_000,   # Gehaelter Einkauf & Category Management
        "6010":  46_000,   # Gehaelter Marketing & E-Commerce
        "6020":  34_000,   # Gehaelter Kundenservice
        "6030":  36_000,   # Gehaelter Verwaltung & Leitung
        "6100":  40_000,   # Sozialabgaben & Benefits
        "6200":   8_000,   # Betriebliche Altersvorsorge
        # Sachkosten (6xxx)
        "6300":  48_000,   # Performance Marketing & Werbung
        "6400":  12_000,   # Software, Shop & Tools
        "6500":  20_000,   # Lager & Logistik
        "6600":   8_000,   # IT & Zahlungsinfrastruktur
        "6700":   6_000,   # Abschreibungen
        "6800":   5_000,   # Sonstige Betriebskosten
    }

    # Konto -> Funktionsbereich (Zwischenstufe fuer KST-Verteilung via area_weights)
    account_area = {
        "4000": "Marketing",
        "4100": "Marketing",
        "4200": "Marketing",
        "4300": "Marketing",
        "4400": "Marketing",
        "5000": "Einkauf",
        "5100": "Logistik",
        "5200": "Verwaltung",
        "5300": "Service",
        "6000": "Einkauf",
        "6010": "Marketing",
        "6020": "Service",
        "6030": "Verwaltung",
        "6100": "Verwaltung",
        "6200": "Verwaltung",
        "6300": "Marketing",
        "6400": "Verwaltung",
        "6500": "Logistik",
        "6600": "Verwaltung",
        "6700": "Verwaltung",
        "6800": "Verwaltung",
    }

    # Funktionsbereich -> KST-Gewichte (Summe je Bereich = 1.0)
    area_weights = {
        "Einkauf":    {"KST-100": 0.75, "KST-110": 0.25},
        "Marketing":  {"KST-200": 0.50, "KST-210": 0.30, "KST-220": 0.20},
        "Service":    {"KST-300": 0.65, "KST-310": 0.35},
        "Logistik":   {"KST-400": 0.45, "KST-410": 0.35, "KST-420": 0.20},
        "Verwaltung": {"KST-500": 0.30, "KST-600": 0.25, "KST-700": 0.20, "KST-900": 0.25},
    }

    # Konto -> Buchungstext (Freitextfeld im GL-Buchungssatz)
    buchungstext_map = {
        "4000": "Erlöse Smartphones",
        "4100": "Erlöse Notebooks",
        "4200": "Erlöse Audio",
        "4300": "Erlöse Zubehör",
        "4400": "Erlöse Smart Home",
        "5000": "Wareneinsatz",
        "5100": "Versand & Fulfillment",
        "5200": "Payment-Gebühren",
        "5300": "Retourenkosten",
        "6000": "Gehälter Einkauf",
        "6010": "Gehälter Marketing",
        "6020": "Gehälter Service",
        "6030": "Gehälter Verwaltung",
        "6100": "Sozialabgaben",
        "6200": "Betriebliche AV",
        "6300": "Performance Marketing",
        "6400": "Software & Tools",
        "6500": "Lager & Logistik",
        "6600": "IT & Infrastruktur",
        "6700": "Abschreibungen",
        "6800": "Sonstige Betriebskosten",
    }

    # Konto -> Produktverteilung fuer Ist-Umsatz-/Kostenmix ist DB-relevant und
    # wird erst in der DB (konto_produkt_mapping) aufgeloest; hier nur GL-Rohbuchung.

    # E-Commerce-typischer Umsatzverlauf; Q4-Peak (Black Friday Nov, Weihnachten Dez)
    seasonality_revenue = {
        1: 0.88,
        2: 0.82,
        3: 0.90,
        4: 0.90,
        5: 0.95,
        6: 0.92,
        7: 0.88,
        8: 0.92,
        9: 1.00,
        10: 1.08,
        11: 1.48,
        12: 1.27,
    }
    # Kosten folgen dem Umsatz gedaempft
    seasonality_cost = {
        1: 0.95,
        2: 0.93,
        3: 0.95,
        4: 0.96,
        5: 0.98,
        6: 0.97,
        7: 0.95,
        8: 0.98,
        9: 1.02,
        10: 1.08,
        11: 1.20,
        12: 1.03,
    }

    # per-year noise - verhindert identische Saisonkurven ueber Jahre
    year_shape = {
        y: {m: np.random.uniform(0.95, 1.05) for m in range(1, 13)}
        for y in [2022, 2023, 2024]
    }

    # Umsatz-Wachstumsfaktor YoY (Multiplikator auf base_plan)
    growth = {2022: 1.00, 2023: 1.07, 2024: 1.15}

    records = []
    belegnr_seq = {2022: 1, 2023: 1, 2024: 1}

    periods = pd.date_range("2022-01-01", "2024-12-01", freq="MS")  # MS = Month Start (erster Tag je Monat)

    for period in periods:
        year  = period.year
        month = period.month
        last_day = monthrange(year, month)[1]
        buchungsdatum = f"{year}-{month:02d}-{last_day:02d}"
        monat_name = MONTH_NAMES_DE[month]

        for konto_id, base in base_plan.items():
            is_erloes = konto_id.startswith("4")
            is_variabel = konto_id.startswith(("4", "5"))
            soll_haben = "H" if is_erloes else "S"
            # COGS (5xxx) ist variabel und skaliert mit dem Umsatzvolumen (seasonality_revenue);
            # nur echte Fixkosten (6xxx) folgen der gedaempften seasonality_cost.
            season = seasonality_revenue if is_variabel else seasonality_cost
            area   = account_area[konto_id]
            text   = f"{buchungstext_map[konto_id]} {monat_name} {year}"

            plan_base = round(base * growth[year] * season[month], 2)
            if is_erloes:
                variance = np.random.uniform(0.90, 1.10)   # +/-10% Erloes-Varianz
            else:
                variance = np.random.uniform(0.96, 1.04)   # +/-4% Kosten-Varianz
            ist_base  = round(plan_base * variance * year_shape[year][month], 2)

            for kst_id, kst_weight in area_weights[area].items():
                plan_betrag = round(plan_base * kst_weight, 2)
                ist_betrag  = round(ist_base  * kst_weight, 2)

                scenarios = [("Plan", plan_betrag), ("Ist", ist_betrag)]

                for szenario, betrag in scenarios:
                    belegnr = f"BLG-{year}-{belegnr_seq[year]:05d}"
                    belegnr_seq[year] += 1
                    records.append({
                        "buchungsdatum":   buchungsdatum,
                        "belegnr":         belegnr,
                        "konto_id":        konto_id,
                        "kostenstelle_id": kst_id,
                        "betrag":          betrag,
                        "soll_haben":      soll_haben,
                        "buchungstext":    text,
                        "szenario":        szenario,
                        "geschaeftsjahr":  year,
                        "periode":         month,
                    })

    return pd.DataFrame(records)


def apply_ebit_floor(df, min_margin=0.02):
    """
    Stellt sicher dass kein Ist-Monat unter min_margin EBIT-Marge faellt.
    Falls doch: Erloesbuchungen des Monats proportional hochskalieren bis Marge = min_margin.
    Kosten bleiben unveraendert; Saisonalitaet bleibt erhalten.
    """
    ist_mask = df["szenario"] == "Ist"

    for (year, month), grp in df[ist_mask].groupby(["geschaeftsjahr", "periode"]):
        rev_total  = grp[grp["soll_haben"] == "H"]["betrag"].sum()
        cost_total = grp[grp["soll_haben"] == "S"]["betrag"].sum()
        ebit       = rev_total - cost_total

        if ebit < rev_total * min_margin:
            target_rev = cost_total / (1 - min_margin)
            scale      = target_rev / rev_total
            mask = (
                ist_mask
                & (df["geschaeftsjahr"] == year)
                & (df["periode"]        == month)
                & (df["soll_haben"]     == "H")
            )
            df.loc[mask, "betrag"] = (df.loc[mask, "betrag"] * scale).round(2)

    return df


def apply_q1_2024_correction(df):
    """
    2024 Jan-Maerz (Ist) wich durch Zufallsvarianz vom in 2022/2023 etablierten
    Q1-Saisonmuster ab (Jan/Feb sind strukturell die margenschwaechsten Monate).
    Skaliert Erloesbuchungen je Monat auf den Durchschnitt des jeweiligen
    Vorjahresmonats (Ø aus 2022 und 2023), analog zur apply_ebit_floor-Logik.
    """
    targets = {1: 0.032, 2: 0.020, 3: 0.0675}   # Ø-Marge aus 2022/2023 je Monat
    ist_mask = df["szenario"] == "Ist"

    for month, target_margin in targets.items():
        mask      = ist_mask & (df["geschaeftsjahr"] == 2024) & (df["periode"] == month)
        rev_mask  = mask & (df["soll_haben"] == "H")
        cost_mask = mask & (df["soll_haben"] == "S")

        rev_total  = df.loc[rev_mask, "betrag"].sum()
        cost_total = df.loc[cost_mask, "betrag"].sum()

        target_rev = cost_total / (1 - target_margin)
        scale      = target_rev / rev_total
        df.loc[rev_mask, "betrag"] = (df.loc[rev_mask, "betrag"] * scale).round(2)

    return df


if __name__ == "__main__":
    log = logging.getLogger(__name__)

    df_kp = generate_kontenplan()
    df_kp.to_csv(OUTPUT_DIR / "kontenplan.csv", index=False)
    log.info("kontenplan.csv          %d rows", len(df_kp))

    df_ks = generate_kostenstellen()
    df_ks.to_csv(OUTPUT_DIR / "kostenstellen.csv", index=False)
    log.info("kostenstellen.csv       %d rows", len(df_ks))

    df_pk = generate_produktkatalog()
    df_pk.to_csv(OUTPUT_DIR / "produktkatalog.csv", index=False)
    log.info("produktkatalog.csv      %d rows", len(df_pk))

    df_bj = generate_buchungsjournal()
    df_bj = apply_ebit_floor(df_bj, min_margin=0.02)
    df_bj = apply_q1_2024_correction(df_bj)
    df_bj.to_csv(OUTPUT_DIR / "buchungsjournal.csv", index=False)
    log.info("buchungsjournal.csv     %d rows", len(df_bj))

    # Plausibilitaetspruefung Plan je Jahr
    for year in (2022, 2023, 2024):
        plan_year = df_bj[(df_bj["geschaeftsjahr"] == year) & (df_bj["szenario"] == "Plan")]
        rev       = plan_year[plan_year["konto_id"].str.startswith("4")]["betrag"].sum()
        all_costs = plan_year[plan_year["konto_id"].str.startswith(("5", "6"))]["betrag"].sum()
        ebit      = rev - all_costs
        log.info("Plan %d - Umsatz: %s EUR | Kosten: %s EUR | EBIT: %s EUR (%.1f%%)",
                 year, f"{rev:,.0f}", f"{all_costs:,.0f}", f"{ebit:,.0f}", ebit / rev * 100)
