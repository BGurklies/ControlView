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
        Name   = "CF EBIT Abweichung Farbe",
        Folder = "Base",
        Dax    = @"IF([Abweichung €] >= 0, ""#4CA18D"", ""#CD6155"")"
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
        Name   = "Rohertrag Badge Text",
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
        Name   = "Rohertrag Badge Text Color",
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
        Name   = "Rohertrag Badge BG Color",
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
        Name   = "OpEx Quote YTD %",
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
                [OpEx Quote YTD %],
                SAMEPERIODLASTYEAR('mart dim_date'[full_date])
            )
        "
    },

    // ── OpEx \ Badges ──────────────────────────────────────────────────────────
    new {
        Name   = "OpEx Quote Badge Text",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Quote YTD %] - [OpEx Quote Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "OpEx Quote Badge Text Color",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Quote YTD %] - [OpEx Quote Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Red"",
                    _diff < 0, ""Green"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "OpEx Quote Badge BG Color",
        Folder = "OpEx\\Badges",
        Dax    = @"
            VAR _diff = [OpEx Quote YTD %] - [OpEx Quote Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#FFDCDC"",
                    _diff < 0, ""#EAF8EC"",
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
    new {
        Name   = "Konten Gesamt",
        Folder = "Abweichungsanalyse",
        Dax    = @"CALCULATE(DISTINCTCOUNT('mart dim_account'[account_name]), ALL('mart dim_account'[account_name]))"
    },
    new {
        Name   = "Konten je Kategorie (Text)",
        Folder = "Abweichungsanalyse",
        Dax    = @"
            VAR _erloese  = CALCULATE(DISTINCTCOUNT('mart dim_account'[account_name]), ALL('mart dim_account'[account_name]), 'mart dim_account'[account_category] = ""Erlöse"")
            VAR _cogs     = CALCULATE(DISTINCTCOUNT('mart dim_account'[account_name]), ALL('mart dim_account'[account_name]), 'mart dim_account'[account_category] = ""COGS"")
            VAR _personal = CALCULATE(DISTINCTCOUNT('mart dim_account'[account_name]), ALL('mart dim_account'[account_name]), 'mart dim_account'[account_category] = ""Personalkosten"")
            VAR _sach     = CALCULATE(DISTINCTCOUNT('mart dim_account'[account_name]), ALL('mart dim_account'[account_name]), 'mart dim_account'[account_category] = ""Sachkosten"")
            RETURN _erloese & "" Erlöse · "" & _cogs & "" COGS · "" & _personal & "" Personal · "" & _sach & "" Sachkosten""
        "
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
                [Umsatz],
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
        Name   = "Wareneinsatz Ist YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Ist YTD], 'mart dim_account'[account_id] = ""5000"")"
    },
    new {
        Name   = "Wareneinsatz Budget YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Budget YTD], 'mart dim_account'[account_id] = ""5000"")"
    },
    new {
        Name   = "Wareneinsatzquote YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Wareneinsatz Ist YTD]), ABS([Umsatz Ist YTD]))"
    },
    new {
        Name   = "Wareneinsatzquote Budget YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Wareneinsatz Budget YTD]), ABS([Umsatz Budget YTD]))"
    },
    new {
        Name   = "Wareneinsatzquote Vorjahr YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Wareneinsatzquote YTD %], SAMEPERIODLASTYEAR('mart dim_date'[full_date]))"
    },
    new {
        Name   = "Personalkosten YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Ist YTD], 'mart dim_account'[account_category] = ""Personalkosten"")"
    },
    new {
        Name   = "Personalkosten Budget YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Budget YTD], 'mart dim_account'[account_category] = ""Personalkosten"")"
    },
    new {
        Name   = "Sachkosten YTD",
        Folder = "GuV-Struktur",
        Dax    = @"CALCULATE([Ist YTD], 'mart dim_account'[account_category] = ""Sachkosten"")"
    },
    new {
        Name   = "Personalkostenquote YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Personalkosten YTD]), ABS([Umsatz Ist YTD]))"
    },
    new {
        Name   = "Personalkostenquote Budget YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Personalkosten Budget YTD]), ABS([Umsatz Budget YTD]))"
    },
    new {
        Name   = "Sachkostenquote YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([Sachkosten YTD]), ABS([Umsatz Ist YTD]))"
    },
    new {
        Name   = "Break-Even-Umsatz YTD",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([OpEx Ist YTD]), [Rohertragsmarge YTD %])"
    },
    new {
        Name   = "Break-Even-Umsatz Budget YTD",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE(ABS([OpEx Budget YTD]), [Rohertragsmarge Budget YTD %])"
    },
    new {
        Name   = "Sicherheitsabstand YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE([Umsatz Ist YTD] - [Break-Even-Umsatz YTD], [Umsatz Ist YTD])"
    },
    new {
        Name   = "Sicherheitsabstand Budget YTD %",
        Folder = "GuV-Struktur",
        Dax    = @"DIVIDE([Umsatz Budget YTD] - [Break-Even-Umsatz Budget YTD], [Umsatz Budget YTD])"
    },

    // ── GuV-Struktur \ Badges ──────────────────────────────────────────────────
    new {
        Name   = "Wareneinsatzquote Badge Text",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Wareneinsatzquote YTD %] - [Wareneinsatzquote Budget YTD %]
            VAR _pp   = FORMAT(ABS(_diff) * 100, ""0.00"") & "" pp""
            RETURN
                IF(_diff > 0,
                    UNICHAR(9650) & "" "" & _pp,
                    UNICHAR(9660) & "" "" & _pp
                )
        "
    },
    new {
        Name   = "Wareneinsatzquote Badge Text Color",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Wareneinsatzquote YTD %] - [Wareneinsatzquote Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""Red"",
                    _diff < 0, ""Green"",
                    ""Grey""
                )
        "
    },
    new {
        Name   = "Wareneinsatzquote Badge BG Color",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Wareneinsatzquote YTD %] - [Wareneinsatzquote Budget YTD %]
            RETURN
                SWITCH(TRUE(),
                    _diff > 0, ""#FFDCDC"",
                    _diff < 0, ""#EAF8EC"",
                    ""#F2F2F2""
                )
        "
    },
    new {
        Name   = "Sicherheitsabstand Badge Text",
        Folder = "GuV-Struktur\\Badges",
        Dax    = @"
            VAR _diff = [Sicherheitsabstand YTD %] - [Sicherheitsabstand Budget YTD %]
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
            VAR _diff = [Sicherheitsabstand YTD %] - [Sicherheitsabstand Budget YTD %]
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
            VAR _diff = [Sicherheitsabstand YTD %] - [Sicherheitsabstand Budget YTD %]
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
        Name   = "COGS Ist (absolut)",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"ABS([COGS Ist])"
    },
    new {
        Name   = "OpEx Ist (absolut)",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"ABS([OpEx Ist])"
    },
    new {
        Name   = "Anteil COGS %",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"DIVIDE(ABS([COGS Ist]), ABS([Umsatz]))"
    },
    new {
        Name   = "Anteil OpEx %",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"DIVIDE(ABS([OpEx Ist]), ABS([Umsatz]))"
    },
    new {
        Name   = "Anteil EBIT %",
        Folder = "GuV-Struktur\\Umsatzverwendung",
        Dax    = @"DIVIDE([Ist], ABS([Umsatz]))"
    },
};

foreach(var d in defs) {
    t.AddMeasure(d.Name, Dedent(d.Dax), d.Folder);
}
