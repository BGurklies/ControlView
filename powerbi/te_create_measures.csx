// Tabular Editor 2 — Erzeugt saemtliche DAX-Measures fuer das ControlView-Dashboard.
// Verwendung: Power BI Desktop mit dem Modell oeffnen, Tabular Editor ueber External Tools
//             starten, dieses Skript in den C#-Script-Tab einfuegen, F5, dann Strg+S.
//
// Der mehrzeilige DAX unten ist zur besseren Lesbarkeit eingerueckt; Dedent() entfernt
// die gemeinsame Grundeinrueckung, damit die gespeicherten Measures sauber bleiben.

Func<string, string> Dedent = (raw) => {
    var lines = raw.Replace("\r\n", "\n").Split('\n').ToList();
    while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
    while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
    if (lines.Count == 0) return "";
    int indent = lines.Where(l => l.Trim().Length > 0)
                      .Select(l => l.Length - l.TrimStart(' ').Length)
                      .Min();
    return string.Join("\n", lines.Select(l => l.Length >= indent ? l.Substring(indent) : l.TrimStart()));
};

foreach(var m in Model.Tables["_Measures"].Measures.ToList()) m.Delete();

var t = Model.Tables["_Measures"];

var defs = new[] {
    // ── Base ──────────────────────────────────────────────────────────────────
    new {
        Name   = "Ist",
        Folder = "Base",
        Dax    = @"
            CALCULATE(
                SUMX('mart fact_journal', 'mart fact_journal'[amount] * RELATED('mart dim_account'[sign])),
                'mart dim_scenario'[scenario_id] = ""Ist""
            )
        "
    },
    new {
        Name   = "Budget",
        Folder = "Base",
        Dax    = @"
            CALCULATE(
                SUMX('mart fact_journal', 'mart fact_journal'[amount] * RELATED('mart dim_account'[sign])),
                'mart dim_scenario'[scenario_id] = ""Plan""
            )
        "
    },
    new {
        Name   = "Ist YTD",
        Folder = "Base",
        Dax    = @"
            CALCULATE(
                [Ist],
                DATESYTD('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Budget YTD",
        Folder = "Base",
        Dax    = @"
            CALCULATE(
                [Budget],
                DATESYTD('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Abweichung €",
        Folder = "Base",
        Dax    = @"IF(ISBLANK([Ist]), BLANK(), [Ist] - [Budget])"
    },
    new {
        Name   = "Abweichung YTD €",
        Folder = "Base",
        Dax    = @"[Ist YTD] - [Budget YTD]"
    },
    new {
        Name   = "Abweichung YTD %",
        Folder = "Base",
        Dax    = @"DIVIDE([Abweichung YTD €], ABS([Budget YTD]))"
    },
    new {
        Name   = "CF EBIT Abweichung Farbe",
        Folder = "Base",
        Dax    = @"IF([Abweichung €] >= 0, ""#276749"", ""#C53030"")"
    },

    // ── Umsatz ────────────────────────────────────────────────────────────────
    new {
        Name   = "Umsatz",
        Folder = "Umsatz",
        Dax    = @"
            CALCULATE(
                [Ist],
                'mart dim_account'[pl_line] = ""Umsatz""
            )
        "
    },
    new {
        Name   = "Umsatz Budget",
        Folder = "Umsatz",
        Dax    = @"
            CALCULATE(
                [Budget],
                'mart dim_account'[pl_line] = ""Umsatz""
            )
        "
    },
    new {
        Name   = "Umsatz Ist YTD",
        Folder = "Umsatz",
        Dax    = @"
            CALCULATE(
                [Ist],
                'mart dim_account'[pl_line] = ""Umsatz"",
                DATESYTD('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Umsatz Budget YTD",
        Folder = "Umsatz",
        Dax    = @"
            CALCULATE(
                [Budget],
                'mart dim_account'[pl_line] = ""Umsatz"",
                DATESYTD('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Umsatz Abweichung €",
        Folder = "Umsatz",
        Dax    = @"[Umsatz Ist YTD] - [Umsatz Budget YTD]"
    },
    new {
        Name   = "Umsatz Abweichung %",
        Folder = "Umsatz",
        Dax    = @"DIVIDE([Umsatz Abweichung €], ABS([Umsatz Budget YTD]))"
    },

    // ── Umsatz \ Badges ───────────────────────────────────────────────────────
    new {
        Name   = "Umsatz Badge Text",
        Folder = "Umsatz\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz Abweichung €]
            VAR _perc = FORMAT([Umsatz Abweichung %], ""0.0%;0.0%"")
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _perc,
                    UNICHAR(9660) & "" "" & _perc
                )
        "
    },
    new {
        Name   = "Umsatz Badge Text Color",
        Folder = "Umsatz\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz Abweichung €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "Umsatz Badge BG Color",
        Folder = "Umsatz\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz Abweichung €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── EBIT ──────────────────────────────────────────────────────────────────
    new {
        Name   = "EBIT Marge %",
        Folder = "EBIT",
        Dax    = @"DIVIDE([Ist], ABS([Umsatz]))"
    },
    new {
        Name   = "EBIT Marge Budget %",
        Folder = "EBIT",
        Dax    = @"IF(ISBLANK([Ist]), BLANK(), DIVIDE([Budget], ABS([Umsatz Budget])))"
    },
    new {
        Name   = "EBIT Marge YTD Ist %",
        Folder = "EBIT",
        Dax    = @"
            DIVIDE(
                [Ist YTD],
                ABS([Umsatz Ist YTD])
            )
        "
    },
    new {
        Name   = "EBIT Marge Budget YTD %",
        Folder = "EBIT",
        Dax    = @"
            DIVIDE(
                [Budget YTD],
                ABS([Umsatz Budget YTD])
            )
        "
    },
    new {
        Name   = "EBIT Marge Vorjahr YTD %",
        Folder = "EBIT",
        Dax    = @"
            CALCULATE(
                [EBIT Marge YTD Ist %],
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "EBIT Budget Gesamtjahr",
        Folder = "EBIT",
        Dax    = @"
            VAR _jahr = SELECTEDVALUE('mart dim_date'[year], MAX('mart dim_date'[year]))
            RETURN
                CALCULATE(
                    [Budget],
                    FILTER(ALL('mart dim_date'), 'mart dim_date'[year] = _jahr)
                )
        "
    },

    // ── EBIT \ Badges ─────────────────────────────────────────────────────────
    new {
        Name   = "EBIT Badge Text",
        Folder = "EBIT\\Badges",
        Dax    = @"
            VAR _diff = [Abweichung YTD €]
            VAR _perc = FORMAT([Abweichung YTD %], ""0.0%;0.0%"")
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _perc,
                    UNICHAR(9660) & "" "" & _perc
                )
        "
    },
    new {
        Name   = "EBIT Badge Text Color",
        Folder = "EBIT\\Badges",
        Dax    = @"
            VAR _diff = [Abweichung YTD €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "EBIT Badge BG Color",
        Folder = "EBIT\\Badges",
        Dax    = @"
            VAR _diff = [Abweichung YTD €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },
    new {
        Name   = "EBIT Marge YTD Badge Text",
        Folder = "EBIT\\Badges",
        Dax    = @"
            VAR _diff = [EBIT Marge YTD Ist %] - [EBIT Marge Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.0"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "EBIT Marge YTD Badge Text Color",
        Folder = "EBIT\\Badges",
        Dax    = @"
            VAR _diff = [EBIT Marge YTD Ist %] - [EBIT Marge Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "EBIT Marge YTD Badge BG Color",
        Folder = "EBIT\\Badges",
        Dax    = @"
            VAR _diff = [EBIT Marge YTD Ist %] - [EBIT Marge Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── Rohertragsmarge ───────────────────────────────────────────────────────
    new {
        Name   = "Rohertragsmarge YTD %",
        Folder = "Rohertragsmarge",
        Dax    = @"
            VAR _rohertrag =
                CALCULATE(
                    [Ist],
                    'mart dim_account'[pl_line] IN { ""Umsatz"", ""COGS"" },
                    DATESYTD('mart dim_date'[full_date])
                )
            RETURN
                DIVIDE(_rohertrag, ABS([Umsatz Ist YTD]))
        "
    },
    new {
        Name   = "Rohertragsmarge Budget YTD %",
        Folder = "Rohertragsmarge",
        Dax    = @"
            VAR _rohertrag =
                CALCULATE(
                    [Budget],
                    'mart dim_account'[pl_line] IN { ""Umsatz"", ""COGS"" },
                    DATESYTD('mart dim_date'[full_date])
                )
            RETURN
                DIVIDE(_rohertrag, ABS([Umsatz Budget YTD]))
        "
    },
    new {
        Name   = "Rohertragsmarge Vorjahr YTD %",
        Folder = "Rohertragsmarge",
        Dax    = @"
            CALCULATE(
                [Rohertragsmarge YTD %],
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },

    // ── Rohertragsmarge \ Badges ──────────────────────────────────────────────
    new {
        Name   = "Rohertragsmarge YTD Badge Text",
        Folder = "Rohertragsmarge\\Badges",
        Dax    = @"
            VAR _diff = [Rohertragsmarge YTD %] - [Rohertragsmarge Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.0"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "Rohertragsmarge YTD Badge Text Color",
        Folder = "Rohertragsmarge\\Badges",
        Dax    = @"
            VAR _diff = [Rohertragsmarge YTD %] - [Rohertragsmarge Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "Rohertragsmarge YTD Badge BG Color",
        Folder = "Rohertragsmarge\\Badges",
        Dax    = @"
            VAR _diff = [Rohertragsmarge YTD %] - [Rohertragsmarge Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── YoY ───────────────────────────────────────────────────────────────────
    new {
        Name   = "Umsatz Vorjahr",
        Folder = "YoY",
        Dax    = @"
            VAR _auswahl = SELECTEDVALUE('mart dim_date'[year])
            VAR _jahr =
                IF(ISBLANK(_auswahl), MAX('mart dim_date'[year]), _auswahl)
            VAR _vj = _jahr - 1
            VAR _vj_existiert =
                CALCULATE(
                    COUNTROWS('mart dim_date'),
                    ALL('mart dim_date'),
                    'mart dim_date'[year] = _vj
                ) > 0
            RETURN
                IF(
                    NOT _vj_existiert,
                    BLANK(),
                    CALCULATE(
                        [Ist],
                        'mart dim_account'[pl_line] = ""Umsatz"",
                        FILTER(ALL('mart dim_date'), 'mart dim_date'[year] = _vj)
                    )
                )
        "
    },
    new {
        Name   = "Budget Vorjahr",
        Folder = "YoY",
        Dax    = @"
            VAR _auswahl = SELECTEDVALUE('mart dim_date'[year])
            VAR _jahr =
                IF(ISBLANK(_auswahl), MAX('mart dim_date'[year]), _auswahl)
            VAR _vj = _jahr - 1
            VAR _vj_existiert =
                CALCULATE(
                    COUNTROWS('mart dim_date'),
                    ALL('mart dim_date'),
                    'mart dim_date'[year] = _vj
                ) > 0
            RETURN
                IF(
                    NOT _vj_existiert,
                    BLANK(),
                    CALCULATE(
                        [Budget],
                        'mart dim_account'[pl_line] = ""Umsatz"",
                        FILTER(ALL('mart dim_date'), 'mart dim_date'[year] = _vj)
                    )
                )
        "
    },
    new {
        Name   = "Umsatz YoY %",
        Folder = "YoY",
        Dax    = @"
            VAR _auswahl = SELECTEDVALUE('mart dim_date'[year])
            VAR _jahr =
                IF(ISBLANK(_auswahl), MAX('mart dim_date'[year]), _auswahl)
            VAR _ist =
                CALCULATE(
                    [Ist],
                    'mart dim_account'[pl_line] = ""Umsatz"",
                    FILTER(ALL('mart dim_date'), 'mart dim_date'[year] = _jahr)
                )
            VAR _vj = [Umsatz Vorjahr]
            RETURN
                IF(ISBLANK(_vj), BLANK(), DIVIDE(_ist - _vj, ABS(_vj)))
        "
    },
    new {
        Name   = "Umsatz YoY Budget %",
        Folder = "YoY",
        Dax    = @"
            VAR _plan_wachstum = [Budget YoY Wachstum €]
            VAR _plan_vorjahr  = [Budget Vorjahr]
            RETURN
                IF(
                    OR(ISBLANK(_plan_wachstum), ISBLANK(_plan_vorjahr)),
                    BLANK(),
                    DIVIDE(_plan_wachstum, ABS(_plan_vorjahr))
                )
        "
    },
    new {
        Name   = "Umsatz YoY Wachstum €",
        Folder = "YoY",
        Dax    = @"
            VAR _auswahl = SELECTEDVALUE('mart dim_date'[year])
            VAR _jahr =
                IF(ISBLANK(_auswahl), MAX('mart dim_date'[year]), _auswahl)
            VAR _ist =
                CALCULATE(
                    [Ist],
                    'mart dim_account'[pl_line] = ""Umsatz"",
                    FILTER(ALL('mart dim_date'), 'mart dim_date'[year] = _jahr)
                )
            VAR _vj = [Umsatz Vorjahr]
            RETURN
                IF(ISBLANK(_vj), BLANK(), _ist - _vj)
        "
    },
    new {
        Name   = "Budget YoY Wachstum €",
        Folder = "YoY",
        Dax    = @"
            VAR _auswahl = SELECTEDVALUE('mart dim_date'[year])
            VAR _jahr =
                IF(ISBLANK(_auswahl), MAX('mart dim_date'[year]), _auswahl)
            VAR _plan =
                CALCULATE(
                    [Budget],
                    'mart dim_account'[pl_line] = ""Umsatz"",
                    FILTER(ALL('mart dim_date'), 'mart dim_date'[year] = _jahr)
                )
            VAR _vj = [Budget Vorjahr]
            RETURN
                IF(ISBLANK(_vj), BLANK(), _plan - _vj)
        "
    },
    new {
        Name   = "Umsatz YoY Wachstum Abw. %",
        Folder = "YoY",
        Dax    = @"
            VAR _ist_wachstum  = [Umsatz YoY Wachstum €]
            VAR _plan_wachstum = [Budget YoY Wachstum €]
            RETURN
                IF(
                    OR(ISBLANK(_ist_wachstum), ISBLANK(_plan_wachstum)),
                    BLANK(),
                    DIVIDE(_ist_wachstum - _plan_wachstum, ABS(_plan_wachstum))
                )
        "
    },
    new {
        Name   = "Umsatz YoY Wachstum Abw. €",
        Folder = "YoY",
        Dax    = @"
            VAR _ist_wachstum  = [Umsatz YoY Wachstum €]
            VAR _plan_wachstum = [Budget YoY Wachstum €]
            RETURN
                IF(
                    OR(ISBLANK(_ist_wachstum), ISBLANK(_plan_wachstum)),
                    BLANK(),
                    _ist_wachstum - _plan_wachstum
                )
        "
    },

    // ── YoY \ Badges ──────────────────────────────────────────────────────────
    new {
        Name   = "Umsatz YoY Badge Text",
        Folder = "YoY\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz YoY %] - [Umsatz YoY Budget %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.0"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "Umsatz YoY Badge Text Color",
        Folder = "YoY\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz YoY %] - [Umsatz YoY Budget %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "Umsatz YoY Badge BG Color",
        Folder = "YoY\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz YoY %] - [Umsatz YoY Budget %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── Forecast ──────────────────────────────────────────────────────────────
    new {
        Name   = "Letzter Ist-Monat",
        Folder = "Forecast",
        Dax    = @"
            CALCULATE(
                MAX('mart dim_date'[month]),
                'mart dim_scenario'[scenario_id] = ""Ist"",
                ALLEXCEPT('mart dim_date', 'mart dim_date'[year])
            )
        "
    },
    new {
        Name   = "Forecast EBIT",
        Folder = "Forecast",
        Dax    = @"
            VAR _jahr         = SELECTEDVALUE('mart dim_date'[year], MAX('mart dim_date'[year]))
            VAR _letzterMonat = [Letzter Ist-Monat]
            VAR _ytdIst =
                CALCULATE(
                    [Ist],
                    FILTER(ALL('mart dim_date'),
                        'mart dim_date'[year] = _jahr
                        && 'mart dim_date'[month] <= _letzterMonat)
                )
            VAR _restBudget =
                CALCULATE(
                    [Budget],
                    FILTER(ALL('mart dim_date'),
                        'mart dim_date'[year] = _jahr
                        && 'mart dim_date'[month] > _letzterMonat)
                )
            RETURN
                IF(ISBLANK(_letzterMonat), BLANK(), _ytdIst + _restBudget)
        "
    },
    new {
        Name   = "Forecast EBIT Abweichung €",
        Folder = "Forecast",
        Dax    = @"[Forecast EBIT] - [EBIT Budget Gesamtjahr]"
    },
    new {
        Name   = "Forecast EBIT Abweichung %",
        Folder = "Forecast",
        Dax    = @"DIVIDE([Forecast EBIT Abweichung €], ABS([EBIT Budget Gesamtjahr]))"
    },

    // ── Forecast \ Badges ─────────────────────────────────────────────────────
    new {
        Name   = "Forecast EBIT Badge Text",
        Folder = "Forecast\\Badges",
        Dax    = @"
            VAR _diff = [Forecast EBIT Abweichung %]
            RETURN
                IF(ISBLANK(_diff), BLANK(),
                    IF(_diff > 0, UNICHAR(9650), UNICHAR(9660)) & "" "" & FORMAT(ABS(_diff), ""0.0%""))
        "
    },
    new {
        Name   = "Forecast EBIT Badge Text Color",
        Folder = "Forecast\\Badges",
        Dax    = @"
            IF(ISBLANK([Forecast EBIT Abweichung %]), BLANK(),
                SWITCH(TRUE(),
                    [Forecast EBIT Abweichung %] > 0, ""Green"",
                    [Forecast EBIT Abweichung %] < 0, ""Red"",
                    ""Grey""))
        "
    },
    new {
        Name   = "Forecast EBIT Badge BG Color",
        Folder = "Forecast\\Badges",
        Dax    = @"
            IF(ISBLANK([Forecast EBIT Abweichung %]), BLANK(),
                SWITCH(TRUE(),
                    [Forecast EBIT Abweichung %] > 0, ""#EAF8EC"",
                    [Forecast EBIT Abweichung %] < 0, ""#FFDCDC"",
                    ""#F2F2F2""))
        "
    },

    // ── Display ───────────────────────────────────────────────────────────────
    new {
        Name   = "Label Vorjahr",
        Folder = "Display",
        Dax    = @"
            VAR _auswahl = SELECTEDVALUE('mart dim_date'[year])
            VAR _aktuelles_jahr =
                IF(ISBLANK(_auswahl), MAX('mart dim_date'[year]), _auswahl)
            VAR _vorjahr = _aktuelles_jahr - 1
            VAR _vorjahr_existiert =
                CALCULATE(
                    COUNTROWS('mart dim_date'),
                    ALL('mart dim_date'),
                    'mart dim_date'[year] = _vorjahr
                ) > 0
            RETURN
                IF(
                    NOT _vorjahr_existiert,
                    BLANK(),
                    ""Vorjahr ("" & FORMAT(_vorjahr, ""0"") & "") ·""
                )
        "
    },
    new {
        Name   = "Datenstand",
        Folder = "Display",
        Dax    = @"
            VAR _datum =
                CALCULATE(
                    MAX('mart dim_date'[full_date]),
                    'mart dim_scenario'[scenario_id] = ""Ist"",
                    ALL('mart dim_date')
                )
            VAR _monatname =
                LOOKUPVALUE('mart dim_date'[month_name], 'mart dim_date'[full_date], _datum)
            RETURN
                IF(ISBLANK(_datum), BLANK(),
                    ""Datenstand: "" & FORMAT(_datum, ""DD"") & "". "" & _monatname & "" "" & FORMAT(_datum, ""YYYY""))
        "
    },

    // ── Display \ AxisMax ─────────────────────────────────────────────────────
    new {
        Name   = "Axis Max Umsatz Produkt",
        Folder = "Display\\AxisMax",
        Dax    = @"
            CALCULATE(
                MAXX(
                    VALUES('mart dim_product'[product_name]),
                    [Umsatz]
                ),
                ALLSELECTED('mart dim_product'[product_name])
            ) * 1.275
        "
    },
};

foreach(var d in defs) {
    t.AddMeasure(d.Name, Dedent(d.Dax), d.Folder);
}
