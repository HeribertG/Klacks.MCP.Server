# Macro Scripting Handbuch

## Übersicht
Mit dem Macro Editor lassen sich Skripte schreiben, die als Rechengrundlage für Dienste, Beschäftigungen etc. verwendet werden.

**Anwendungsbeispiele:**
- Exakte Berechnung der Stunden bei Ferien oder Militär gemäss Beschäftigungsgrad
- Berechnung von Zuschlägen für Nachtarbeit, Wochenenden und Feiertage
- Flexible Anpassung an neue Anforderungen

*Der Script Interpreter läuft in einer sicheren Sandbox-Umgebung ohne Zugriff auf das Dateisystem oder Netzwerk.*

## Variablen und Konstanten

Alle Variablen und Konstanten sind typenlos.

```basic
CONST PI = 3.1415
DIM a, b, c
IMPORT betrag, rabatt
```

### Importierte Variablen (IMPORT)

Diese Variablen werden aus dem jeweiligen Dienst oder der Beschäftigung generiert:

| Variable | Typ | Beschreibung |
|----------|-----|--------------|
| Hour | Decimal | Arbeitsstunden |
| FromHour | String | Startzeit im Format "HH:MM" |
| UntilHour | String | Endzeit im Format "HH:MM" |
| Weekday | Integer | Wochentag (1=Mo, 2=Di, ..., 6=Sa, 7=So) |
| Holiday | Integer | Feiertagsstatus (0=kein, 1=Feiertag, -1=Vortag, -2=Nachtag) |
| NightRate | Decimal | Nachtzuschlag-Satz (z.B. 0.10 = 10%) |
| HolidayRate | Decimal | Feiertagszuschlag-Satz (z.B. 0.15 = 15%) |
| WeekendRate | Decimal | Wochenendzuschlag-Satz (z.B. 0.10 = 10%) |
| GuaranteedHours | Decimal | Garantierte Monatsstunden |
| FullTime | Decimal | Beschäftigungsgrad in % (z.B. 100.0) |

## Kontrollstrukturen

**Wichtig:** Schlüsselwörter wie `ENDIF`, `ENDFUNCTION`, `ENDSUB` müssen zusammengeschrieben werden.

### IF-THEN-ELSE

```basic
IF x > 10 THEN
    OUTPUT 1, "gross"
ELSEIF x > 5 THEN
    OUTPUT 1, "mittel"
ELSE
    OUTPUT 1, "klein"
ENDIF
```

### Einzeiliges IF

```basic
IF x > 10 THEN OUTPUT 1, "gross" ENDIF
```

### SELECT CASE

```basic
SELECT CASE weekday
    CASE 6, 7
        OUTPUT 1, "Wochenende"
    CASE 1, 2, 3, 4, 5
        OUTPUT 1, "Arbeitstag"
    CASE ELSE
        OUTPUT 1, "Unbekannt"
END SELECT
```

### FOR-NEXT Schleife

```basic
FOR i = 1 TO 10
    summe += i
NEXT

FOR i = 10 TO 1 STEP -1
    IF x > y THEN EXIT FOR
NEXT
```

### DO-LOOP Schleife

```basic
DO WHILE a > 0
    a -= 1
LOOP

DO
    x += 1
LOOP UNTIL x >= 10
```

## Funktionen und Prozeduren

**Wichtig:** Funktionen (FUNCTION) und Prozeduren (SUB) müssen *oberhalb* des ersten Aufrufs definiert werden.

### SUB (Prozedur ohne Rückgabewert)

```basic
SUB berechne(a, b)
    IF a = b THEN EXIT SUB ENDIF
    OUTPUT 1, a + b
ENDSUB
```

### FUNCTION (Funktion mit Rückgabewert)

```basic
FUNCTION verdoppeln(x)
    verdoppeln = x * 2
ENDFUNCTION

DIM ergebnis
ergebnis = verdoppeln(21)
OUTPUT 1, ergebnis
```

## Operatoren

### Mathematische Operatoren

| Operator | Beschreibung | Beispiel |
|----------|-------------|----------|
| + | Addition | 5 + 3 = 8 |
| - | Subtraktion | 5 - 3 = 2 |
| * | Multiplikation | 5 * 3 = 15 |
| / | Division | 10 / 4 = 2.5 |
| \ | Ganzzahldivision | 10 \ 4 = 2 |
| MOD | Modulo (Rest) | 10 MOD 3 = 1 |
| ^ | Potenz | 2 ^ 3 = 8 |

### Vergleichsoperatoren

| Operator | Beschreibung |
|----------|-------------|
| = | Gleich |
| <> | Ungleich |
| < | Kleiner als |
| > | Grösser als |
| <= | Kleiner oder gleich |
| >= | Grösser oder gleich |

### Logische Operatoren

| Operator | Beschreibung |
|----------|-------------|
| AND | Bitweises UND |
| OR | Bitweises ODER |
| NOT | Negation |
| ANDALSO | Logisches UND (Short-Circuit) |
| ORELSE | Logisches ODER (Short-Circuit) |

### Zuweisungsoperatoren

```basic
a = 10      ' Einfache Zuweisung
a += 5      ' a = a + 5
a -= 3      ' a = a - 3
a *= 2      ' a = a * 2
a /= 4      ' a = a / 4
a &= "!"    ' String-Verkettung
```

## Eingebaute Funktionen

### String-Funktionen

| Funktion | Beschreibung | Beispiel |
|----------|-------------|----------|
| Len(s) | Länge eines Strings | Len("Hallo") = 5 |
| Left(s, n) | Linke n Zeichen | Left("Hallo", 2) = "Ha" |
| Right(s, n) | Rechte n Zeichen | Right("Hallo", 2) = "lo" |
| Mid(s, start, len) | Teilstring | Mid("Hallo", 2, 3) = "all" |
| InStr(s, search) | Position suchen | InStr("Hallo", "l") = 3 |
| Replace(s, old, new) | Ersetzen | Replace("Hallo", "l", "x") = "Haxxo" |
| Trim(s) | Leerzeichen entfernen | Trim("  Hi  ") = "Hi" |
| UCase(s) | Grossbuchstaben | UCase("hallo") = "HALLO" |
| LCase(s) | Kleinbuchstaben | LCase("HALLO") = "hallo" |

### Mathematische Funktionen

| Funktion | Beschreibung | Beispiel |
|----------|-------------|----------|
| Abs(x) | Absolutwert | Abs(-5) = 5 |
| Round(x, d) | Runden | Round(3.456, 2) = 3.46 |
| Sqr(x) | Quadratwurzel | Sqr(16) = 4 |
| Log(x) | Natürlicher Logarithmus | Log(2.718) = 1 |
| Exp(x) | Exponential (e^x) | Exp(1) = 2.718 |
| Sgn(x) | Vorzeichen (-1, 0, 1) | Sgn(-5) = -1 |
| Rnd() | Zufallszahl 0-1 | Rnd() = 0.xxxxx |

### Zeit-Funktionen

| Funktion | Beschreibung | Beispiel |
|----------|-------------|----------|
| TimeToHours(s) | Zeit-String zu Dezimalstunden | TimeToHours("08:30") = 8.5 |
| TimeOverlap(s1, e1, s2, e2) | Überlappung zweier Zeiträume in Stunden | TimeOverlap("23:00", "06:00", "22:00", "07:00") = 7 |

*TimeOverlap unterstützt Zeiträume über Mitternacht (z.B. 23:00-06:00).*

### Bedingte Funktionen

| Funktion | Beschreibung | Beispiel |
|----------|-------------|----------|
| IIF(bed, wahr, falsch) | Bedingter Ausdruck | IIF(x > 0, "positiv", "negativ") |

## Rückgabe

```basic
OUTPUT typ, wert    ' Rückgabe an Klacks (typ entspricht MacroType)
```

## Debug-Funktionen

| Funktion | Beschreibung |
|----------|-------------|
| DEBUGPRINT wert | Gibt einen Wert im Test-Fenster aus |
| DEBUGCLEAR | Leert das Test-Fenster |

```basic
DIM x
x = 42
DEBUGPRINT "Der Wert ist: " & x
```

*Die Debug-Ausgaben erscheinen im Tab "Testen" des Macro-Editors.*

## Beispiel: Zuschlagsberechnung

Berechnet Zuschläge für Nacht, Feiertag und Wochenende mit korrekter Behandlung von Schichten über Mitternacht:

```basic
IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, WeekendRate

FUNCTION CalcSegment(StartTime, EndTime, HolidayFlag, WeekdayNum)
    DIM SegmentHours, NightHours, NonNightHours
    DIM NRate, DRate, HasHoliday, HasWeekend

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = TimeOverlap("23:00", "06:00", StartTime, EndTime)
    NonNightHours = SegmentHours - NightHours

    HasHoliday = HolidayFlag = 1
    HasWeekend = WeekdayNum = 6 OrElse WeekdayNum = 7

    NRate = 0
    IF NightHours > 0 THEN NRate = NightRate ENDIF
    IF HasHoliday AndAlso HolidayRate > NRate THEN NRate = HolidayRate ENDIF
    IF HasWeekend AndAlso WeekendRate > NRate THEN NRate = WeekendRate ENDIF

    DRate = 0
    IF HasHoliday AndAlso HolidayRate > DRate THEN DRate = HolidayRate ENDIF
    IF HasWeekend AndAlso WeekendRate > DRate THEN DRate = WeekendRate ENDIF

    CalcSegment = NightHours * NRate + NonNightHours * DRate
ENDFUNCTION

DIM TotalBonus, WeekdayNextDay

WeekdayNextDay = (Weekday MOD 7) + 1

IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
    TotalBonus = CalcSegment(FromHour, "00:00", Holiday, Weekday)
    TotalBonus = TotalBonus + CalcSegment("00:00", UntilHour, HolidayNextDay, WeekdayNextDay)
ELSE
    TotalBonus = CalcSegment(FromHour, UntilHour, Holiday, Weekday)
ENDIF

OUTPUT 1, Round(TotalBonus, 2)
```

**Erklärung:**
- **Segment-Splitting:** Bei Schichten über Mitternacht wird die Schicht in zwei Segmente aufgeteilt
- **CalcSegment:** Berechnet den Zuschlag unter Berücksichtigung von Nacht-, Feiertags- und Wochenendzuschlägen
- **Höchster Zuschlag:** Es wird immer der höchste anwendbare Zuschlag verwendet
- **HolidayNextDay:** Berücksichtigt ob der Folgetag ein Feiertag ist (wichtig für Nachtschichten)
