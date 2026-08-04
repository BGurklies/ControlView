// Tabular Editor 2 — Erzeugt alle DAX-Measures fuer das ControlView-Dashboard.
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
        Name   = "Abweichung %",
        Folder = "Base",
        Dax    = @"DIVIDE([Abweichung €], ABS([Budget]))"
    },
    new {
        Name   = "Abweichung % (natürlich)",
        Folder = "Base",
        Dax    = @"
            VAR _wert     = [Abweichung %]
            VAR _minSign  = MIN('mart dim_account'[sign])
            VAR _maxSign  = MAX('mart dim_account'[sign])
            RETURN
                IF(_minSign = _maxSign && _minSign = -1, -_wert, _wert)
        "
    },
    new {
        Name   = "Abweichung € (natürlich)",
        Folder = "Base",
        Dax    = @"
            VAR _wert    = [Abweichung €]
            VAR _minSign = MIN('mart dim_account'[sign])
            VAR _maxSign = MAX('mart dim_account'[sign])
            RETURN
                IF(_minSign = _maxSign && _minSign = -1, -_wert, _wert)
        "
    },
    new {
        Name   = "CF EBIT Abweichung Farbe",
        Folder = "Base",
        Dax    = @"IF([Abweichung €] >= 0, ""#4CA18D"", ""#CD6155"")"
    },
    new {
        Name   = "Ist Vorjahr",
        Folder = "Base",
        Dax    = @"
            CALCULATE(
                [Ist],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Abweichung Vorjahr €",
        Folder = "Base",
        Dax    = @"IF(ISBLANK([Ist]), BLANK(), [Ist] - [Ist Vorjahr])"
    },
    new {
        Name   = "Abweichung Vorjahr %",
        Folder = "Base",
        Dax    = @"DIVIDE([Abweichung Vorjahr €], ABS([Ist Vorjahr]))"
    },
    new {
        Name   = "Abweichung Vorjahr € (natürlich)",
        Folder = "Base",
        Dax    = @"
            VAR _diff    = [Ist] - [Ist Vorjahr]
            VAR _minSign = MIN('mart dim_account'[sign])
            VAR _maxSign = MAX('mart dim_account'[sign])
            RETURN
                IF(ISBLANK([Ist]), BLANK(),
                    IF(_minSign = _maxSign && _minSign = -1, -_diff, _diff))
        "
    },
    new {
        Name   = "Abweichung Vorjahr % (natürlich)",
        Folder = "Base",
        Dax    = @"DIVIDE([Abweichung Vorjahr € (natürlich)], ABS([Ist Vorjahr]))"
    },
    new {
        Name   = "Anteil an Kategorie %",
        Folder = "Base",
        Dax    = @"
            IF(
                ISINSCOPE('mart dim_account'[account_name]),
                DIVIDE(
                    ABS([Ist]),
                    CALCULATE(
                        ABS([Ist]),
                        ALLEXCEPT('mart dim_account', 'mart dim_account'[account_category])
                    )
                )
            )
        "
    },

    // ── Umsatz ────────────────────────────────────────────────────────────────
    new {
        Name   = "Umsatz Ist",
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
        Name   = "Umsatz Abweichung YTD €",
        Folder = "Umsatz",
        Dax    = @"[Umsatz Ist YTD] - [Umsatz Budget YTD]"
    },
    new {
        Name   = "Umsatz Abweichung YTD %",
        Folder = "Umsatz",
        Dax    = @"DIVIDE([Umsatz Abweichung YTD €], ABS([Umsatz Budget YTD]))"
    },

    // ── Umsatz \ Badges ───────────────────────────────────────────────────────
    new {
        Name   = "Umsatz Badge Text",
        Folder = "Umsatz\\Badges",
        Dax    = @"
            VAR _diff = [Umsatz Abweichung YTD €]
            VAR _perc = FORMAT([Umsatz Abweichung YTD %], ""0.0%;0.0%"")
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
            VAR _diff = [Umsatz Abweichung YTD €]
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
            VAR _diff = [Umsatz Abweichung YTD €]
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
        Name   = "EBIT Marge Ist %",
        Folder = "EBIT",
        Dax    = @"DIVIDE([Ist], ABS([Umsatz Ist]))"
    },
    new {
        Name   = "EBIT Marge Budget %",
        Folder = "EBIT",
        Dax    = @"IF(ISBLANK([Ist]), BLANK(), DIVIDE([Budget], ABS([Umsatz Budget])))"
    },
    new {
        Name   = "EBIT Marge Ist YTD %",
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
                [EBIT Marge Ist YTD %],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
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
            VAR _diff = [EBIT Marge Ist YTD %] - [EBIT Marge Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
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
            VAR _diff = [EBIT Marge Ist YTD %] - [EBIT Marge Budget YTD %]
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
            VAR _diff = [EBIT Marge Ist YTD %] - [EBIT Marge Budget YTD %]
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
        Name   = "Rohertragsmarge Ist YTD %",
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
                [Rohertragsmarge Ist YTD %],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },

    // ── Rohertragsmarge \ Badges ──────────────────────────────────────────────
    new {
        Name   = "Rohertragsmarge YTD Badge Text",
        Folder = "Rohertragsmarge\\Badges",
        Dax    = @"
            VAR _diff = [Rohertragsmarge Ist YTD %] - [Rohertragsmarge Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
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
            VAR _diff = [Rohertragsmarge Ist YTD %] - [Rohertragsmarge Budget YTD %]
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
            VAR _diff = [Rohertragsmarge Ist YTD %] - [Rohertragsmarge Budget YTD %]
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
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
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
                    [Umsatz Ist]
                ),
                ALLSELECTED('mart dim_product'[product_name])
            ) * 1.375
        "
    },

    // ── Rohertrag (€) ──────────────────────────────────────────────────────────
    new {
        Name   = "Rohertrag Ist YTD",
        Folder = "Rohertrag",
        Dax    = @"CALCULATE([Ist YTD], 'mart dim_account'[pl_line] IN { ""Umsatz"", ""COGS"" })"
    },
    new {
        Name   = "Rohertrag Budget YTD",
        Folder = "Rohertrag",
        Dax    = @"CALCULATE([Budget YTD], 'mart dim_account'[pl_line] IN { ""Umsatz"", ""COGS"" })"
    },
    new {
        Name   = "Rohertrag Abweichung YTD €",
        Folder = "Rohertrag",
        Dax    = @"IF(ISBLANK([Rohertrag Ist YTD]), BLANK(), [Rohertrag Ist YTD] - [Rohertrag Budget YTD])"
    },
    new {
        Name   = "Rohertrag Abweichung YTD %",
        Folder = "Rohertrag",
        Dax    = @"DIVIDE([Rohertrag Abweichung YTD €], ABS([Rohertrag Budget YTD]))"
    },

    // ── Rohertrag \ Badges ─────────────────────────────────────────────────────
    new {
        Name   = "Rohertrag YTD Badge Text",
        Folder = "Rohertrag\\Badges",
        Dax    = @"
            VAR _diff = [Rohertrag Abweichung YTD €]
            VAR _perc = FORMAT([Rohertrag Abweichung YTD %], ""0.0%;0.0%"")
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _perc,
                    UNICHAR(9660) & "" "" & _perc
                )
        "
    },
    new {
        Name   = "Rohertrag YTD Badge Text Color",
        Folder = "Rohertrag\\Badges",
        Dax    = @"
            VAR _diff = [Rohertrag Abweichung YTD €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "Rohertrag YTD Badge BG Color",
        Folder = "Rohertrag\\Badges",
        Dax    = @"
            VAR _diff = [Rohertrag Abweichung YTD €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── OpEx ───────────────────────────────────────────────────────────────────
    new {
        Name   = "OpEx Ist",
        Folder = "OpEx",
        Dax    = @"
            CALCULATE(
                [Ist],
                'mart dim_account'[pl_line] = ""OpEx""
            )
        "
    },
    new {
        Name   = "OpEx Budget",
        Folder = "OpEx",
        Dax    = @"
            CALCULATE(
                [Budget],
                'mart dim_account'[pl_line] = ""OpEx""
            )
        "
    },
    new {
        Name   = "OpEx Ist YTD",
        Folder = "OpEx",
        Dax    = @"CALCULATE([OpEx Ist], DATESYTD('mart dim_date'[full_date]))"
    },
    new {
        Name   = "OpEx Budget YTD",
        Folder = "OpEx",
        Dax    = @"CALCULATE([OpEx Budget], DATESYTD('mart dim_date'[full_date]))"
    },
    new {
        Name   = "OpEx Abweichung €",
        Folder = "OpEx",
        Dax    = @"IF(ISBLANK([OpEx Ist]), BLANK(), [OpEx Ist] - [OpEx Budget])"
    },
    new {
        Name   = "OpEx Abweichung %",
        Folder = "OpEx",
        Dax    = @"DIVIDE([OpEx Abweichung €], ABS([OpEx Budget]))"
    },
    new {
        Name   = "OpEx Abweichung YTD €",
        Folder = "OpEx",
        Dax    = @"[OpEx Ist YTD] - [OpEx Budget YTD]"
    },
    new {
        Name   = "OpEx Abweichung YTD %",
        Folder = "OpEx",
        Dax    = @"DIVIDE([OpEx Abweichung YTD €], ABS([OpEx Budget YTD]))"
    },
    new {
        Name   = "OpEx Quote Ist YTD %",
        Folder = "OpEx",
        Dax    = @"DIVIDE(ABS([OpEx Ist YTD]), ABS([Umsatz Ist YTD]))"
    },
    new {
        Name   = "OpEx Quote Budget YTD %",
        Folder = "OpEx",
        Dax    = @"DIVIDE(ABS([OpEx Budget YTD]), ABS([Umsatz Budget YTD]))"
    },
    new {
        Name   = "OpEx Quote Vorjahr YTD %",
        Folder = "OpEx",
        Dax    = @"
            CALCULATE(
                [OpEx Quote Ist YTD %],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },

    // ── OpEx \ Badges ──────────────────────────────────────────────────────────
    new {
        Name   = "OpEx Quote YTD Badge Text",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Quote Ist YTD %] - [OpEx Quote Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "OpEx Quote YTD Badge Text Color",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Quote Ist YTD %] - [OpEx Quote Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Red"",
                    _diff < 0, ""Green"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "OpEx Quote YTD Badge BG Color",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Quote Ist YTD %] - [OpEx Quote Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#FFDCDC"",
                    _diff < 0, ""#EAF8EC"",
                    ""#F2F2F2""
                )
        "
    },
    new {
        Name   = "OpEx YTD Badge Text",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Abweichung YTD €]
            VAR _perc = FORMAT([OpEx Abweichung YTD %], ""0.0%;0.0%"")
            RETURN
                IF(_diff > 0,
                    UNICHAR(9660) & "" "" & _perc,
                    UNICHAR(9650) & "" "" & _perc
                )
        "
    },
    new {
        Name   = "OpEx YTD Badge Text Color",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Abweichung YTD €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "OpEx YTD Badge BG Color",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Abweichung YTD €]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── Abweichungsanalyse ─────────────────────────────────────────────────────
    new {
        Name   = "Toleranzschwelle %",
        Folder = "Abweichungsanalyse",
        Dax    = @"0.025"
    },
    new {
        Name   = "Konten außerhalb Toleranz (Anzahl)",
        Folder = "Abweichungsanalyse",
        Dax    = @"
            VAR _count =
                COUNTROWS(
                    FILTER(
                        VALUES('mart dim_account'[account_name]),
                        ABS(CALCULATE([Abweichung YTD %])) > [Toleranzschwelle %]
                    )
                )
            RETURN
                IF(ISBLANK(_count), 0, _count)
        "
    },
    new {
        Name   = "Konten Gesamt (Anzahl)",
        Folder = "Abweichungsanalyse",
        Dax    = @"COUNTROWS(ALL('mart dim_account'[account_name]))"
    },
    new {
        Name   = "Konten außerhalb Toleranz Text",
        Folder = "Abweichungsanalyse",
        Dax    = @"FORMAT([Konten außerhalb Toleranz (Anzahl)], ""0"") & "" von "" & FORMAT([Konten Gesamt (Anzahl)], ""0"")"
    },
    new {
        Name   = "Toleranzschwelle Text",
        Folder = "Abweichungsanalyse",
        Dax    = @"FORMAT([Toleranzschwelle %], ""0.0%"") & "" ggü. Plan"""
    },

    // ── Wachstum ───────────────────────────────────────────────────────────────
    new {
        Name   = "COGS Ist",
        Folder = "Wachstum",
        Dax    = @"
            CALCULATE(
                [Ist],
                'mart dim_account'[pl_line] = ""COGS""
            )
        "
    },
    new {
        Name   = "COGS Vorjahr",
        Folder = "Wachstum",
        Dax    = @"CALCULATE([COGS Ist], SAMEPERIODLASTYEAR('mart dim_date'[full_date]))"
    },
    new {
        Name   = "COGS-Wachstum YoY",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([COGS Ist] - [COGS Vorjahr], ABS([COGS Vorjahr]))"
    },
    new {
        Name   = "Rohertrag Ist",
        Folder = "Wachstum",
        Dax    = @"CALCULATE([Ist], 'mart dim_account'[pl_line] IN { ""Umsatz"", ""COGS"" })"
    },
    new {
        Name   = "Rohertrag Vorjahr",
        Folder = "Wachstum",
        Dax    = @"CALCULATE([Rohertrag Ist], SAMEPERIODLASTYEAR('mart dim_date'[full_date]))"
    },
    new {
        Name   = "Rohertrag-Wachstum YoY",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([Rohertrag Ist] - [Rohertrag Vorjahr], ABS([Rohertrag Vorjahr]))"
    },
    new {
        Name   = "EBIT Vorjahr",
        Folder = "Wachstum",
        Dax    = @"CALCULATE([Ist], SAMEPERIODLASTYEAR('mart dim_date'[full_date]))"
    },
    new {
        Name   = "EBIT-Wachstum YoY",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([Ist] - [EBIT Vorjahr], ABS([EBIT Vorjahr]))"
    },
    new {
        Name   = "OpEx Vorjahr",
        Folder = "Wachstum",
        Dax    = @"CALCULATE([OpEx Ist], SAMEPERIODLASTYEAR('mart dim_date'[full_date]))"
    },
    new {
        Name   = "OpEx-Wachstum YoY",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([OpEx Ist] - [OpEx Vorjahr], ABS([OpEx Vorjahr]))"
    },
    new {
        Name   = "Umsatz YoY % (Monat)",
        Folder = "Wachstum",
        Dax    = @"
            VAR _ist = CALCULATE([Ist], 'mart dim_account'[pl_line] = ""Umsatz"")
            VAR _vj  =
                CALCULATE(
                    [Ist],
                    'mart dim_account'[pl_line] = ""Umsatz"",
                    ALL('mart dim_date'[year]),
                    ALL('mart dim_date'[quarter]),
                    SAMEPERIODLASTYEAR('mart dim_date'[full_date])
                )
            RETURN
                IF(ISBLANK(_vj), BLANK(), DIVIDE(_ist - _vj, ABS(_vj)))
        "
    },
    new {
        Name   = "Umsatz YTD Vorjahr (Monat)",
        Folder = "Wachstum",
        Dax    = @"
            CALCULATE(
                [Umsatz Ist],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR(DATESYTD('mart dim_date'[full_date]))
            )
        "
    },
    new {
        Name   = "Umsatzwachstum YTD YoY (Monat)",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([Umsatz Ist YTD] - [Umsatz YTD Vorjahr (Monat)], ABS([Umsatz YTD Vorjahr (Monat)]))"
    },
    new {
        Name   = "Rohertrag YTD Vorjahr (Monat)",
        Folder = "Wachstum",
        Dax    = @"
            CALCULATE(
                [Rohertrag Ist],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR(DATESYTD('mart dim_date'[full_date]))
            )
        "
    },
    new {
        Name   = "Rohertrag-Wachstum YTD YoY (Monat)",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([Rohertrag Ist YTD] - [Rohertrag YTD Vorjahr (Monat)], ABS([Rohertrag YTD Vorjahr (Monat)]))"
    },
    new {
        Name   = "EBIT YTD Vorjahr (Monat)",
        Folder = "Wachstum",
        Dax    = @"
            CALCULATE(
                [Ist],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR(DATESYTD('mart dim_date'[full_date]))
            )
        "
    },
    new {
        Name   = "EBIT-Wachstum YTD YoY (Monat)",
        Folder = "Wachstum",
        Dax    = @"DIVIDE([Ist YTD] - [EBIT YTD Vorjahr (Monat)], ABS([EBIT YTD Vorjahr (Monat)]))"
    },

    // ── GuV-Struktur ───────────────────────────────────────────────────────────
    new {
        Name   = "Personalkosten Ist YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Ist YTD], 'mart dim_account'[account_category] = ""Personalkosten"")"
    },
    new {
        Name   = "Personalkosten Budget YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Budget YTD], 'mart dim_account'[account_category] = ""Personalkosten"")"
    },
    new {
        Name   = "Sachkosten Ist YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Ist YTD], 'mart dim_account'[account_category] = ""Sachkosten"")"
    },
    new {
        Name   = "Sachkosten Budget YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Budget YTD], 'mart dim_account'[account_category] = ""Sachkosten"")"
    },
    new {
        Name   = "Personalkostenquote Ist YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Personalkosten Ist YTD]), ABS([Umsatz Ist YTD]))"
    },
    new {
        Name   = "Personalkostenquote Budget YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Personalkosten Budget YTD]), ABS([Umsatz Budget YTD]))"
    },
    new {
        Name   = "Sachkostenquote Ist YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Sachkosten Ist YTD]), ABS([Umsatz Ist YTD]))"
    },
    new {
        Name   = "Break-Even-Umsatz Ist YTD",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([OpEx Ist YTD]), [Rohertragsmarge Ist YTD %])"
    },
    new {
        Name   = "Break-Even-Umsatz Budget YTD",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([OpEx Budget YTD]), [Rohertragsmarge Budget YTD %])"
    },
    new {
        Name   = "Sicherheitsabstand Ist YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE([Umsatz Ist YTD] - [Break-Even-Umsatz Ist YTD], [Umsatz Ist YTD])"
    },
    new {
        Name   = "Sicherheitsabstand Budget YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE([Umsatz Budget YTD] - [Break-Even-Umsatz Budget YTD], [Umsatz Budget YTD])"
    },

    // ── GuV-Struktur \ Badges ──────────────────────────────────────────────────
    new {
        Name   = "Sicherheitsabstand Badge Text",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Sicherheitsabstand Ist YTD %] - [Sicherheitsabstand Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "Sicherheitsabstand Badge Text Color",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Sicherheitsabstand Ist YTD %] - [Sicherheitsabstand Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Green"",
                    _diff < 0, ""Red"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "Sicherheitsabstand Badge BG Color",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Sicherheitsabstand Ist YTD %] - [Sicherheitsabstand Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#EAF8EC"",
                    _diff < 0, ""#FFDCDC"",
                    ""#F2F2F2""
                )
        "
    },

    // ── GuV-Struktur \ Umsatzverwendung ────────────────────────────────────────
    new {
        Name   = "COGS Ist (abs)",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"ABS([COGS Ist])"
    },
    new {
        Name   = "OpEx Ist (abs)",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"ABS([OpEx Ist])"
    },
    new {
        Name   = "Anteil COGS %",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"DIVIDE(ABS([COGS Ist]), ABS([Umsatz Ist]))"
    },
    new {
        Name   = "Anteil OpEx %",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"DIVIDE(ABS([OpEx Ist]), ABS([Umsatz Ist]))"
    },
    new {
        Name   = "Anteil EBIT %",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"DIVIDE([Ist], ABS([Umsatz Ist]))"
    },

    // ── Kostencontrolling ──────────────────────────────────────────────────────
    new {
        Name   = "Personalkosten Ist YTD (abs)",
        Folder = "Kostencontrolling",
        Dax    = @"ABS([Personalkosten Ist YTD])"
    },
    new {
        Name   = "Sachkosten Ist YTD (abs)",
        Folder = "Kostencontrolling",
        Dax    = @"ABS([Sachkosten Ist YTD])"
    },
    new {
        Name   = "OpEx Ist YTD (abs)",
        Folder = "Kostencontrolling",
        Dax    = @"ABS([OpEx Ist YTD])"
    },
    new {
        Name   = "OpEx Budget YTD (abs)",
        Folder = "Kostencontrolling",
        Dax    = @"ABS([OpEx Budget YTD])"
    },
    new {
        Name   = "OpEx Budget (abs)",
        Folder = "Kostencontrolling",
        Dax    = @"ABS([OpEx Budget])"
    },
    new {
        Name   = "Personalkostenintensität Ist YTD %",
        Folder = "Kostencontrolling",
        Dax    = @"DIVIDE(ABS([Personalkosten Ist YTD]), ABS([OpEx Ist YTD]))"
    },
    new {
        Name   = "Sachkostenintensität Ist YTD %",
        Folder = "Kostencontrolling",
        Dax    = @"DIVIDE(ABS([Sachkosten Ist YTD]), ABS([OpEx Ist YTD]))"
    },
    new {
        Name   = "Personalkostenintensität Vorjahr YTD %",
        Folder = "Kostencontrolling",
        Dax    = @"
            CALCULATE(
                [Personalkostenintensität Ist YTD %],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Sachkostenintensität Vorjahr YTD %",
        Folder = "Kostencontrolling",
        Dax    = @"
            CALCULATE(
                [Sachkostenintensität Ist YTD %],
                ALL('mart dim_date'[year]),
                ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },
    new {
        Name   = "Toleranzschwelle Monat %",
        Folder = "Kostencontrolling",
        Dax    = @"0.05"
    },
    new {
        Name   = "Abweichende Bereichsmonate",
        Folder = "Kostencontrolling",
        Dax    = @"
            COUNTROWS(
                FILTER(
                    CROSSJOIN(
                        VALUES('mart dim_costcenter'[area]),
                        SUMMARIZE('mart dim_date', 'mart dim_date'[year], 'mart dim_date'[month])
                    ),
                    ABS(CALCULATE([OpEx Abweichung %])) > [Toleranzschwelle Monat %]
                )
            )
        "
    },
    new {
        Name   = "Bereichsmonate Gesamt",
        Folder = "Kostencontrolling",
        Dax    = @"
            COUNTROWS(
                FILTER(
                    CROSSJOIN(
                        VALUES('mart dim_costcenter'[area]),
                        SUMMARIZE('mart dim_date', 'mart dim_date'[year], 'mart dim_date'[month])
                    ),
                    NOT ISBLANK(CALCULATE([OpEx Ist]))
                )
            )
        "
    },
    new {
        Name   = "Toleranzschwelle Monat Text",
        Folder = "Kostencontrolling",
        Dax    = @"FORMAT([Toleranzschwelle Monat %], ""0%"") & "" ggü. Plan"""
    },
    new {
        Name   = "Abweichende Bereichsmonate Text",
        Folder = "Kostencontrolling",
        Dax    = @"FORMAT([Abweichende Bereichsmonate], ""0"") & "" von "" & FORMAT([Bereichsmonate Gesamt], ""0"")"
    },
    new {
        Name   = "Bereichsmonate Basis Text",
        Folder = "Kostencontrolling",
        Dax    = @"
            VAR _b = COUNTROWS(VALUES('mart dim_costcenter'[area]))
            VAR _m = COUNTROWS(SUMMARIZE('mart dim_date', 'mart dim_date'[year], 'mart dim_date'[month]))
            RETURN FORMAT(_b, ""0"") & "" Bereiche × "" & FORMAT(_m, ""0"") & "" Monate""
        "
    },
    new {
        Name   = "Cost Owner",
        Folder = "Kostencontrolling",
        Dax    = @"SELECTEDVALUE('mart dim_costcenter'[cost_owner_id])"
    },
    new {
        Name   = "OpEx Abweichung Titel",
        Folder = "Kostencontrolling",
        Dax    = @"
            VAR _area = SELECTEDVALUE('mart dim_costcenter'[area])
            RETURN
                ""OpEx-Abweichung: "" &
                IF(ISBLANK(_area), ""alle Bereiche"", ""Bereich "" & _area)
        "
    },

    // ── Kostencontrolling \ Badges ─────────────────────────────────────────────
    new {
        Name   = "Personalkostenintensität YTD Badge Text",
        Folder = "Kostencontrolling\\Badges",
        Dax    = @"
            VAR _diff = [Personalkostenintensität Ist YTD %] - [Personalkostenintensität Vorjahr YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
            RETURN
                IF(_diff >= 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "Sachkostenintensität YTD Badge Text",
        Folder = "Kostencontrolling\\Badges",
        Dax    = @"
            VAR _diff = [Sachkostenintensität Ist YTD %] - [Sachkostenintensität Vorjahr YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
            RETURN
                IF(_diff >= 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },

    // ── Produktmarge ─────────────────────────────────────────────────────────
    new {
        Name   = "DB I Ist",
        Folder = "Produktmarge",
        Dax    = @"
            CALCULATE([Ist], 'mart dim_cost_type'[cost_type_id] = ""variabel"")
        "
    },
    new {
        Name   = "DB I Budget",
        Folder = "Produktmarge",
        Dax    = @"
            CALCULATE([Budget], 'mart dim_cost_type'[cost_type_id] = ""variabel"")
        "
    },

    // ── Produktmarge \ Hochmarge ─────────────────────────────────────────────
    new {
        Name   = "DB I Hochmarge Ist",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            CALCULATE([DB I Ist], 'mart dim_product'[margin_class] = ""Hochmarge"")
        "
    },
    new {
        Name   = "Umsatz Hochmarge Ist",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            CALCULATE([Umsatz Ist], 'mart dim_product'[margin_class] = ""Hochmarge"")
        "
    },
    new {
        Name   = "DB-Anteil Hochmarge %",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            DIVIDE([DB I Hochmarge Ist], CALCULATE([DB I Ist], ALL('mart dim_product')))
        "
    },
    new {
        Name   = "Umsatzanteil Hochmarge %",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            DIVIDE([Umsatz Hochmarge Ist], CALCULATE([Umsatz Ist], ALL('mart dim_product')))
        "
    },
    new {
        Name   = "Hochmargen-Hebel",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            DIVIDE([DB-Anteil Hochmarge %], [Umsatzanteil Hochmarge %])
        "
    },
    new {
        Name   = "Hochmargen-Hebel Text",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            FORMAT([Hochmargen-Hebel], ""0.0"") & ""×""
        "
    },
    new {
        Name   = "Hochmargen-Beitrag Text",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            FORMAT([DB-Anteil Hochmarge %], ""0.0%"") & "" DB bei "" &
            FORMAT([Umsatzanteil Hochmarge %], ""0.0%"") & "" Umsatz""
        "
    },
    new {
        Name   = "Hochmargen-DB Text",
        Folder = "Produktmarge\\Hochmarge",
        Dax    = @"
            FORMAT([DB I Hochmarge Ist] / 1000000, ""0.00"") & "" Mio. € DB""
        "
    },

    // ── Produktmarge \ Volumenmarge ──────────────────────────────────────────
    new {
        Name   = "DB I Volumen Ist",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            CALCULATE([DB I Ist], 'mart dim_product'[margin_class] = ""Volumen"")
        "
    },
    new {
        Name   = "Umsatz Volumen Ist",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            CALCULATE([Umsatz Ist], 'mart dim_product'[margin_class] = ""Volumen"")
        "
    },
    new {
        Name   = "DB-Anteil Volumen %",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            DIVIDE([DB I Volumen Ist], CALCULATE([DB I Ist], ALL('mart dim_product')))
        "
    },
    new {
        Name   = "Umsatzanteil Volumen %",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            DIVIDE([Umsatz Volumen Ist], CALCULATE([Umsatz Ist], ALL('mart dim_product')))
        "
    },
    new {
        Name   = "Volumen-Hebel",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            DIVIDE([DB-Anteil Volumen %], [Umsatzanteil Volumen %])
        "
    },
    new {
        Name   = "Volumen-Hebel Text",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            FORMAT([Volumen-Hebel], ""0.0"") & ""×""
        "
    },
    new {
        Name   = "Volumen-Beitrag Text",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            FORMAT([DB-Anteil Volumen %], ""0.0%"") & "" DB bei "" &
            FORMAT([Umsatzanteil Volumen %], ""0.0%"") & "" Umsatz""
        "
    },
    new {
        Name   = "Volumen-DB Text",
        Folder = "Produktmarge\\Volumenmarge",
        Dax    = @"
            FORMAT([DB I Volumen Ist] / 1000000, ""0.00"") & "" Mio. € DB""
        "
    },
    new {
        Name   = "DB I Marge Ist %",
        Folder = "Produktmarge",
        Dax    = @"
            DIVIDE([DB I Ist], ABS([Umsatz Ist]))
        "
    },

    // ── Produktmarge \ Marge-Spread ──────────────────────────────────────────
    new {
        Name   = "DB I Marge Max %",
        Folder = "Produktmarge\\Marge-Spread",
        Dax    = @"
            VAR _m =
                FILTER(
                    ADDCOLUMNS(
                        FILTER(
                            VALUES('mart dim_product'[product_name]),
                            'mart dim_product'[product_name] <> ""Gemeinkosten""
                        ),
                        ""@marge"", [DB I Marge Ist %]
                    ),
                    NOT ISBLANK([@marge])
                )
            RETURN MAXX(_m, [@marge])
        "
    },
    new {
        Name   = "DB I Marge Min %",
        Folder = "Produktmarge\\Marge-Spread",
        Dax    = @"
            VAR _m =
                FILTER(
                    ADDCOLUMNS(
                        FILTER(
                            VALUES('mart dim_product'[product_name]),
                            'mart dim_product'[product_name] <> ""Gemeinkosten""
                        ),
                        ""@marge"", [DB I Marge Ist %]
                    ),
                    NOT ISBLANK([@marge])
                )
            RETURN MINX(_m, [@marge])
        "
    },
    new {
        Name   = "DB I Marge Spread pp",
        Folder = "Produktmarge\\Marge-Spread",
        Dax    = @"
            ([DB I Marge Max %] - [DB I Marge Min %]) * 100
        "
    },
    new {
        Name   = "DB I Marge Spread Text",
        Folder = "Produktmarge\\Marge-Spread",
        Dax    = @"
            FORMAT([DB I Marge Spread pp], ""0.0"") & "" pp""
        "
    },
    new {
        Name   = "DB I Marge Spanne Text",
        Folder = "Produktmarge\\Marge-Spread",
        Dax    = @"
            ""Max: "" & FORMAT([DB I Marge Max %], ""0.0%"") &
            "" · Min: "" & FORMAT([DB I Marge Min %], ""0.0%"")
        "
    },
    new {
        Name   = "DB I Marge Portfolio Text",
        Folder = "Produktmarge\\Marge-Spread",
        Dax    = @"
            ""Portfolio-Marge · "" & FORMAT([DB I Marge Ist %], ""0.0%"")
        "
    },

    // ── Produktmarge \ Standardmarge ─────────────────────────────────────────
    new {
        Name   = "DB I Standard Ist",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            CALCULATE([DB I Ist],   'mart dim_product'[margin_class] = ""Standard"")
        "
    },
    new {
        Name   = "Umsatz Standard Ist",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            CALCULATE([Umsatz Ist], 'mart dim_product'[margin_class] = ""Standard"")
        "
    },
    new {
        Name   = "DB-Anteil Standard %",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            DIVIDE([DB I Standard Ist],   CALCULATE([DB I Ist],   ALL('mart dim_product')))
        "
    },
    new {
        Name   = "Umsatzanteil Standard %",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            DIVIDE([Umsatz Standard Ist], CALCULATE([Umsatz Ist], ALL('mart dim_product')))
        "
    },
    new {
        Name   = "Standard-Hebel",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            DIVIDE([DB-Anteil Standard %], [Umsatzanteil Standard %])
        "
    },
    new {
        Name   = "Standard-Hebel Text",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            FORMAT([Standard-Hebel], ""0.0"") & ""×""
        "
    },
    new {
        Name   = "Standard-Beitrag Text",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
                FORMAT([DB-Anteil Standard %], ""0.0%"") & "" DB bei "" &
                FORMAT([Umsatzanteil Standard %], ""0.0%"") & "" Umsatz""
        "
    },
    new {
        Name   = "Standard-DB Text",
        Folder = "Produktmarge\\Standardmarge",
        Dax    = @"
            FORMAT([DB I Standard Ist] / 1000000, ""0.00"") & "" Mio. € DB""
        "
    },
    new {
        Name   = "Axis Max DB I Produkt",
        Folder = "Produktmarge",
        Dax    = @"
            CALCULATE(
                MAXX(VALUES('mart dim_product'[product_name]), [DB I Ist]),
                ALLSELECTED('mart dim_product')
            ) * 1.15
        "
    },
    new {
        Name   = "DB I Marge Budget %",
        Folder = "Produktmarge",
        Dax    = @"
            DIVIDE([DB I Budget], ABS([Umsatz Budget]))
        "
    },
    new {
        Name   = "DB I Marge Abweichung pp",
        Folder = "Produktmarge",
        Dax    = @"
            ([DB I Marge Ist %] - [DB I Marge Budget %]) * 100
        "
    },
    new {
        Name   = "DB I Marge Vorjahr %",
        Folder = "Produktmarge",
        Dax    = @"
            CALCULATE([DB I Marge Ist %],
                ALL('mart dim_date'[year]), ALL('mart dim_date'[quarter]),
                SAMEPERIODLASTYEAR('mart dim_date'[full_date]))
        "
    },
    new {
        Name   = "DB I Marge Abweichung Vorjahr pp",
        Folder = "Produktmarge",
        Dax    = @"
            ([DB I Marge Ist %] - [DB I Marge Vorjahr %]) * 100
        "
    },
    new {
        Name   = "DB-Anteil %",
        Folder = "Produktmarge",
        Dax    = @"
            DIVIDE([DB I Ist], CALCULATE([DB I Ist], ALLSELECTED('mart dim_product')))
        "
    },
    new {
        Name   = "CF Margenklasse Farbe",
        Folder = "Produktmarge",
        Dax    = @"
            SWITCH(
                SELECTEDVALUE('mart dim_product'[margin_class]),
                ""Hochmarge"", ""#173B5B"",
                ""Standard"",  ""#235889"",
                ""Volumen"",   ""#ABC8E2"",
                ""#FFFFFF""
            )
        "
    },
    new {
        Name   = "CF Margenklasse Textfarbe",
        Folder = "Produktmarge",
        Dax    = @"
            SWITCH(
                SELECTEDVALUE('mart dim_product'[margin_class]),
                ""Hochmarge"", ""#FFFFFF"",
                ""Standard"",  ""#FFFFFF"",
                ""Volumen"",   ""#333333"",
                ""#333333""
            )
        "
    },
};

// Formatzeichenfolgen
var formats = new Dictionary<string, string> {

    // ── Währung, 0 Nachkommastellen ────────────────────────────────────────
    { "Abweichung €", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "Abweichung € (natürlich)", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "Budget", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "DB I Ist", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "Ist", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "Ist Vorjahr", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "OpEx Abweichung €", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "OpEx Budget (abs)", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },
    { "OpEx Ist (abs)", @"""€""\ #,0;-""€""\ #,0;""€""\ #,0" },

    // ── Währung, 2 Nachkommastellen ────────────────────────────────────────
    { "Abweichung YTD €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Break-Even-Umsatz Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Budget YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Budget YoY Wachstum €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "EBIT Budget Gesamtjahr", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "OpEx Abweichung YTD €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "OpEx Budget YTD (abs)", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "OpEx Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "OpEx Ist YTD (abs)", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Personalkosten Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Personalkosten Ist YTD (abs)", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Rohertrag Abweichung YTD €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Rohertrag Budget YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Rohertrag Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Sachkosten Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Sachkosten Ist YTD (abs)", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz Abweichung YTD €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz Budget YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz Ist", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz Ist YTD", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz Vorjahr", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz YoY Wachstum Abw. €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },
    { "Umsatz YoY Wachstum €", @"""€""\ #,0.00;-""€""\ #,0.00;""€""\ #,0.00" },

    // ── Prozent, 2 Nachkommastellen ────────────────────────────────────────
    { "Abweichung %", @"0.00%;-0.00%;0.00%" },
    { "Abweichung % (natürlich)", @"0.00%;-0.00%;0.00%" },
    { "Abweichung Vorjahr %", @"0.00%;-0.00%;0.00%" },
    { "Abweichung Vorjahr % (natürlich)", @"0.00%;-0.00%;0.00%" },
    { "Abweichung YTD %", @"0.00%;-0.00%;0.00%" },
    { "Anteil COGS %", @"0.00%;-0.00%;0.00%" },
    { "Anteil EBIT %", @"0.00%;-0.00%;0.00%" },
    { "Anteil OpEx %", @"0.00%;-0.00%;0.00%" },
    { "Anteil an Kategorie %", @"0.00%;-0.00%;0.00%" },
    { "EBIT Marge Budget %", @"0.00%;-0.00%;0.00%" },
    { "EBIT Marge Budget YTD %", @"0.00%;-0.00%;0.00%" },
    { "EBIT Marge Ist %", @"0.00%;-0.00%;0.00%" },
    { "EBIT Marge Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "EBIT Marge Vorjahr YTD %", @"0.00%;-0.00%;0.00%" },
    { "EBIT-Wachstum YTD YoY (Monat)", @"0.00%;-0.00%;0.00%" },
    { "EBIT-Wachstum YoY", @"0.00%;-0.00%;0.00%" },
    { "OpEx Abweichung %", @"0.00%;-0.00%;0.00%" },
    { "OpEx Quote Budget YTD %", @"0.00%;-0.00%;0.00%" },
    { "OpEx Quote Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "OpEx Quote Vorjahr YTD %", @"0.00%;-0.00%;0.00%" },
    { "Personalkostenintensität Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "Personalkostenintensität Vorjahr YTD %", @"0.00%;-0.00%;0.00%" },
    { "Personalkostenquote Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "Rohertrag-Wachstum YTD YoY (Monat)", @"0.00%;-0.00%;0.00%" },
    { "Rohertrag-Wachstum YoY", @"0.00%;-0.00%;0.00%" },
    { "Rohertragsmarge Budget YTD %", @"0.00%;-0.00%;0.00%" },
    { "Rohertragsmarge Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "Rohertragsmarge Vorjahr YTD %", @"0.00%;-0.00%;0.00%" },
    { "Sachkostenintensität Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "Sachkostenintensität Vorjahr YTD %", @"0.00%;-0.00%;0.00%" },
    { "Sicherheitsabstand Budget YTD %", @"0.00%;-0.00%;0.00%" },
    { "Sicherheitsabstand Ist YTD %", @"0.00%;-0.00%;0.00%" },
    { "Toleranzschwelle Monat %", @"0.00%;-0.00%;0.00%" },
    { "Umsatz YoY %", @"0.00%;-0.00%;0.00%" },
    { "Umsatz YoY Budget %", @"0.00%;-0.00%;0.00%" },
    { "Umsatzwachstum YTD YoY (Monat)", @"0.00%;-0.00%;0.00%" },

    // ── Prozent, 1 Nachkommastelle ─────────────────────────────────────────
    { "DB I Marge Budget %", @"0.0%;-0.0%;0.0%" },
    { "DB I Marge Ist %", @"0.0%;-0.0%;0.0%" },
    { "DB I Marge Vorjahr %", @"0.0%;-0.0%;0.0%" },
    { "DB-Anteil %", @"0.0%;-0.0%;0.0%" },
    { "Toleranzschwelle %", @"0.0%;-0.0%;0.0%" },

    // ── Ganzzahl ───────────────────────────────────────────────────────────
    { "Abweichende Bereichsmonate", @"0" },
    { "Bereichsmonate Gesamt", @"0" },
    { "Konten Gesamt (Anzahl)", @"0" },
    { "Konten außerhalb Toleranz (Anzahl)", @"0" },

    // ── Custom: erzwungenes Vorzeichen + Einheit ───────────────────────────
    // nicht über die Oberfläche erzeugbar
    { "DB I Marge Abweichung Vorjahr pp", @"+0.0"" pp"";-0.0"" pp"";0.0"" pp""" },
    { "DB I Marge Abweichung pp", @"+0.0"" pp"";-0.0"" pp"";0.0"" pp""" },
};

foreach(var d in defs) {
    var m = t.AddMeasure(d.Name, Dedent(d.Dax), d.Folder);
    if (formats.ContainsKey(d.Name)) m.FormatString = formats[d.Name];
}
