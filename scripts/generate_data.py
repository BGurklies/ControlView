"""
ControlView - Synthetischer Rohdaten-Generator
Simuliert DATEV-Controlling-Extrakte und Stammdaten-Exporte eines SaaS-KMUs (~80 MA, ~6M EUR ARR).

Erzeugte Raw-CSVs (data/raw/):
  buchungsjournal.csv   : GL-Buchungen (Controlling-Extrakt, SKR04)
  kontenplan.csv        : Kontenstamm (SKR04-basiert)
  kostenstellen.csv     : Kostenstellenstamm (1xx-9xx nach Funktionsbereich)
  produktkatalog.csv    : Produktstamm (CRM/Billing-System)

Dim- und Fact-Tabellen entstehen erst auf DB-Ebene via orchestration.sp_run_full_load.
Produktzuordnung (DB I) erfolgt in mart.sp_load_fact_journal
via mart.konto_produkt_mapping.

Seasonalitaet:
  Plan : flach (gleichmaessige Monatsverteilung des Jahresbudgets)
  Ist  : Q4-lastig (Enterprise SaaS Budgetzyklen)
         Erloes-Varianz: +/-13% (0.87-1.13 x plan_base), Kosten-Varianz: +/-4% (0.96-1.04 x plan_base)
         Jahr-Shape: +/-5% pro Jahr/Monat

Ist-Daten-Cutoff: September 2024 (ab Okt 2024 nur noch Plan-Daten)

Wachstum (ARR YoY): 2022=1.00 / 2023=+15% / 2024=+28%

Plan EBIT-Basis 2022: ~22% Marge (~109k/Mo EBIT auf ~495k/Mo Erlöse)
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

IST_CUTOFF = (2024, 9)  # einschließlich Sep 2024 — ab Okt 2024 nur noch Plan

MONTH_NAMES_DE = {
    1: "Januar", 2: "Februar", 3: "Maerz", 4: "April",
    5: "Mai", 6: "Juni", 7: "Juli", 8: "August",
    9: "September", 10: "Oktober", 11: "November", 12: "Dezember"
}


def generate_kontenplan():
    rows = [
        # Erloese (4xxx)
        ("4000", "Lizenzerlöse (Subscription)",          "Erlös"),
        ("4100", "Professional Services Erlöse",          "Erlös"),
        ("4200", "Support & Wartungserlöse",              "Erlös"),
        ("4300", "Consulting Erlöse",                     "Erlös"),
        # COGS (5xxx)
        ("5000", "Cloud-Infrastruktur & Hosting",         "Aufwand"),
        ("5100", "Drittanbieter-Softwarelizenzen",        "Aufwand"),
        ("5200", "Kosten Professional Services Delivery", "Aufwand"),
        # Personalkosten (6xxx)
        ("6000", "Löhne Produktentwicklung",              "Aufwand"),
        ("6010", "Löhne Vertrieb & Marketing",            "Aufwand"),
        ("6020", "Löhne Customer Operations",             "Aufwand"),
        ("6030", "Löhne Verwaltung & G&A",                "Aufwand"),
        ("6100", "Sozialabgaben & Benefits",              "Aufwand"),
        ("6200", "Betriebliche Altersvorsorge",           "Aufwand"),
        # Sachkosten (6xxx)
        ("6300", "Performance Marketing & Werbung",       "Aufwand"),
        ("6400", "Events & Messen",                       "Aufwand"),
        ("6500", "Softwarelizenzen (intern)",              "Aufwand"),
        ("6600", "Reisekosten",                           "Aufwand"),
        ("6700", "Abschreibungen",                        "Aufwand"),
        ("6800", "Sonstige Betriebskosten",               "Aufwand"),
    ]
    return pd.DataFrame(rows, columns=["konto_id", "konto_bezeichnung", "konto_art"])


def generate_kostenstellen():
    rows = [
        # Kundengewinnung
        ("KST-100", "Enterprise Vertrieb",        "Kundengewinnung",    "EMP-1041"),
        ("KST-110", "Inside Sales & SMB",          "Kundengewinnung",    "EMP-1055"),
        ("KST-120", "Partner & Channel",            "Kundengewinnung",    "EMP-1063"),
        ("KST-200", "Marketing",                    "Kundengewinnung",    "EMP-1072"),
        # Kundenbindung
        ("KST-300", "Customer Success",             "Kundenbindung",      "EMP-2011"),
        ("KST-310", "Onboarding & Implementierung", "Kundenbindung",      "EMP-2024"),
        ("KST-320", "Professional Services",        "Kundenbindung",      "EMP-2038"),
        # Produktentwicklung
        ("KST-400", "Softwareentwicklung",          "Produktentwicklung", "EMP-3007"),
        ("KST-410", "Produktmanagement",            "Produktentwicklung", "EMP-3019"),
        ("KST-420", "QA & Testing",                 "Produktentwicklung", "EMP-3031"),
        # Delivery
        ("KST-500", "DevOps & Infrastruktur",       "Delivery",           "EMP-4005"),
        ("KST-510", "Technical Operations",         "Delivery",           "EMP-4017"),
        # Verwaltung
        ("KST-600", "Finance & Controlling",        "Verwaltung",         "EMP-5002"),
        ("KST-700", "People & Culture",             "Verwaltung",         "EMP-5014"),
        ("KST-800", "IT & Security",                "Verwaltung",         "EMP-5023"),
        ("KST-900", "Geschäftsführung",             "Verwaltung",         "EMP-5001"),
    ]
    return pd.DataFrame(rows, columns=[
        "kostenstelle_id", "kostenstelle_bezeichnung", "bereich", "cost_owner_id"
    ])


def generate_produktkatalog():
    rows = [
        ("SWS-100", "SaaS-Lizenzen",        "Subscription", "Kernprodukt"),
        ("SWS-200", "Professional Services", "Services",     "Implementierung"),
        ("SWS-300", "Support & Wartung",     "Subscription", "Wiederkehrend"),
        ("SWS-400", "Consulting",            "Services",     "Beratung"),
        ("SWS-900", "Gemeinkosten",          "Gemeinkosten", "n/a"),
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
      Erloes Gesamt:  5.940.000 EUR  (~495k/Monat)
      Kosten Gesamt:  4.632.000 EUR  (~386k/Monat)
      EBIT:           1.308.000 EUR  (~109k/Monat, ~22% EBIT-Marge)
    """

    base_plan = {
        # Erlöse (4xxx)
        "4000": 330_000,   # Lizenzerlöse (Subscription)
        "4100":  80_000,   # Professional Services Erlöse
        "4200":  60_000,   # Support & Wartungserlöse
        "4300":  25_000,   # Consulting Erlöse
        # COGS (5xxx)
        "5000":  45_000,   # Cloud-Infrastruktur & Hosting
        "5100":  18_000,   # Drittanbieter-Softwarelizenzen
        "5200":  52_000,   # Kosten Professional Services Delivery
        # Personalkosten (6xxx)
        "6000":  72_000,   # Löhne Produktentwicklung
        "6010":  40_000,   # Löhne Vertrieb & Marketing
        "6020":  32_000,   # Löhne Customer Operations
        "6030":  16_000,   # Löhne Verwaltung & G&A
        "6100":  32_000,   # Sozialabgaben & Benefits
        "6200":   7_000,   # Betriebliche Altersvorsorge
        # Sachkosten (6xxx)
        "6300":  28_000,   # Performance Marketing & Werbung
        "6400":   9_000,   # Events & Messen
        "6500":  15_000,   # Softwarelizenzen (intern)
        "6600":   6_000,   # Reisekosten
        "6700":  10_000,   # Abschreibungen
        "6800":   4_000,   # Sonstige Betriebskosten
    }

    # Konto -> Funktionsbereich (Zwischenstufe fuer KST-Verteilung via area_weights)
    account_area = {
        "4000": "Kundengewinnung",
        "4100": "Kundenbindung",
        "4200": "Kundenbindung",
        "4300": "Kundenbindung",
        "5000": "Delivery",
        "5100": "Delivery",
        "5200": "Kundenbindung",
        "6000": "Produktentwicklung",
        "6010": "Kundengewinnung",
        "6020": "Kundenbindung",
        "6030": "Verwaltung",
        "6100": "Verwaltung",
        "6200": "Verwaltung",
        "6300": "Kundengewinnung",
        "6400": "Kundengewinnung",
        "6500": "Verwaltung",
        "6600": "Kundengewinnung",
        "6700": "Verwaltung",
        "6800": "Verwaltung",
    }

    # Funktionsbereich -> KST-Gewichte (Summe je Bereich = 1.0)
    area_weights = {
        "Kundengewinnung":    {"KST-100": 0.25, "KST-110": 0.30, "KST-120": 0.20, "KST-200": 0.25},
        "Kundenbindung":      {"KST-300": 0.35, "KST-310": 0.35, "KST-320": 0.30},
        "Produktentwicklung": {"KST-400": 0.60, "KST-410": 0.25, "KST-420": 0.15},
        "Delivery":           {"KST-500": 0.65, "KST-510": 0.35},
        "Verwaltung":         {"KST-600": 0.25, "KST-700": 0.25, "KST-800": 0.20, "KST-900": 0.30},
    }

    # Konto -> Buchungstext (Freitextfeld im GL-Buchungssatz)
    buchungstext_map = {
        "4000": "Lizenzerlöse",          "4100": "PS Erlöse",
        "4200": "Support Erlöse",        "4300": "Consulting Erlöse",
        "5000": "Cloud-Infrastruktur",   "5100": "Drittanbieter-SW-Lizenzen",
        "5200": "PS Delivery Kosten",    "6000": "Löhne Produktentwicklung",
        "6010": "Löhne Vertrieb",        "6020": "Löhne Customer Ops",
        "6030": "Löhne Verwaltung",      "6100": "Sozialabgaben",
        "6200": "Betriebliche AV",       "6300": "Performance Marketing",
        "6400": "Events & Messen",       "6500": "SW-Lizenzen intern",
        "6600": "Reisekosten",           "6700": "Abschreibungen",
        "6800": "Sonstige Betriebskosten",
    }

    # Q4-lastiger Umsatzverlauf (Enterprise SaaS Budgetzyklen); Jul Tief, Dez Max
    seasonality_revenue = {
        1: 0.80,
        2: 0.92,
        3: 1.05,
        4: 0.84,
        5: 1.00,
        6: 1.14,
        7: 0.72,
        8: 0.83,
        9: 1.08,
        10: 1.06,
        11: 1.20,
        12: 1.38,
    }
    # Kosten relativ flach, leicht Q4-erhoeht (Personalbonus, Marketing)
    seasonality_cost = {
        1: 0.96,
        2: 0.96,
        3: 1.00,
        4: 0.98,
        5: 1.00,
        6: 1.02,
        7: 0.90,
        8: 0.92,
        9: 1.00,
        10: 1.02,
        11: 1.05,
        12: 1.10,
    }

    # per-year noise — verhindert identische Saisonkurven über Jahre
    year_shape = {
        y: {m: np.random.uniform(0.95, 1.05) for m in range(1, 13)}
        for y in [2022, 2023, 2024]
    }

    # ARR-Wachstumsfaktor YoY (Multiplikator auf base_plan)
    growth = {2022: 1.00, 2023: 1.15, 2024: 1.28}

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
            soll_haben = "H" if is_erloes else "S"
            season = seasonality_revenue if is_erloes else seasonality_cost
            area   = account_area[konto_id]
            text   = f"{buchungstext_map[konto_id]} {monat_name} {year}"

            plan_base = round(base * growth[year], 2)
            if is_erloes:
                variance = np.random.uniform(0.87, 1.13)   # ±13% Erloes-Varianz
            else:
                variance = np.random.uniform(0.96, 1.04)   # ±4% Kosten-Varianz
            ist_base  = round(plan_base * season[month] * variance * year_shape[year][month], 2)

            generate_ist = not (year > IST_CUTOFF[0] or (year == IST_CUTOFF[0] and month > IST_CUTOFF[1]))

            for kst_id, kst_weight in area_weights[area].items():
                plan_betrag = round(plan_base * kst_weight, 2)
                ist_betrag  = round(ist_base  * kst_weight, 2)

                scenarios = [("Plan", plan_betrag)]
                if generate_ist:
                    scenarios.append(("Ist", ist_betrag))

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


def apply_ebit_floor(df, min_margin=0.05):
    """
    Stellt sicher dass kein Ist-Monat unter min_margin EBIT-Marge faellt.
    Falls doch: Erlösbuchungen des Monats proportional hochskalieren bis Marge = min_margin.
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
    df_bj = apply_ebit_floor(df_bj, min_margin=0.05)
    df_bj.to_csv(OUTPUT_DIR / "buchungsjournal.csv", index=False)
    log.info("buchungsjournal.csv     %d rows", len(df_bj))

    # Plausibilitaetspruefung Plan 2022
    plan_2022     = df_bj[(df_bj["geschaeftsjahr"] == 2022) & (df_bj["szenario"] == "Plan")]
    rev           = plan_2022[plan_2022["konto_id"].str.startswith("4")]["betrag"].sum()
    all_costs     = plan_2022[plan_2022["konto_id"].str.startswith(("5", "6"))]["betrag"].sum()
    ebit          = rev - all_costs
    log.info("Plan 2022 — Umsatz: %s EUR | Kosten: %s EUR | EBIT: %s EUR (%.1f%%)",
             f"{rev:,.0f}", f"{all_costs:,.0f}", f"{ebit:,.0f}", ebit / rev * 100)
