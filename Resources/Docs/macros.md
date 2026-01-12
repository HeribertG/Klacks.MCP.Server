# Makro-System

## Übersicht
Makros sind Textvorlagen mit Platzhaltern, die automatisch durch echte Werte ersetzt werden.

## Platzhalter-Syntax

Platzhalter werden in doppelten geschweiften Klammern geschrieben:
```
{{Platzhalter}}
```

## Verfügbare Platzhalter

### Mitarbeiter-Daten
| Platzhalter | Beschreibung | Beispiel |
|-------------|-------------|----------|
| `{{Vorname}}` | Vorname | Max |
| `{{Nachname}}` | Nachname | Muster |
| `{{Name}}` | Vollständiger Name | Max Muster |
| `{{Anrede}}` | Herr/Frau | Herr |
| `{{Email}}` | E-Mail-Adresse | max@example.com |
| `{{Geburtsdatum}}` | Geburtsdatum | 15.03.1985 |

### Adress-Daten
| Platzhalter | Beschreibung | Beispiel |
|-------------|-------------|----------|
| `{{Strasse}}` | Strasse | Hauptstrasse 1 |
| `{{PLZ}}` | Postleitzahl | 3000 |
| `{{Ort}}` | Stadt/Ort | Bern |
| `{{Kanton}}` | Kanton | BE |
| `{{Land}}` | Land | Schweiz |

### Vertrags-Daten
| Platzhalter | Beschreibung | Beispiel |
|-------------|-------------|----------|
| `{{Vertragstyp}}` | Art des Vertrags | Vollzeit 160 |
| `{{Pensum}}` | Arbeitspensum | 100% |
| `{{Eintrittsdatum}}` | Startdatum | 01.04.2024 |

### Datum/Zeit
| Platzhalter | Beschreibung | Beispiel |
|-------------|-------------|----------|
| `{{Heute}}` | Aktuelles Datum | 12.01.2026 |
| `{{Jahr}}` | Aktuelles Jahr | 2026 |
| `{{Monat}}` | Aktueller Monat | Januar |

## Beispiel-Makro

**Abkürzung:** `begrüssung`

**Text:**
```
Sehr geehrte{{Anrede == 'Frau' ? 'r' : ''}} {{Anrede}} {{Nachname}}

Willkommen bei unserem Unternehmen! Wir freuen uns, Sie ab dem
{{Eintrittsdatum}} in unserem Team begrüssen zu dürfen.

Mit freundlichen Grüssen
```

## Makro-Typen

### Standard-Makros
Einfache Textvorlagen für häufig verwendete Texte.

### Bedingte Makros
Mit Bedingungen für unterschiedliche Ausgaben:
```
{{Anrede == 'Herr' ? 'Sehr geehrter Herr' : 'Sehr geehrte Frau'}}
```

## Tipps

1. **Kurze Abkürzungen:** Verwende kurze, merkbare Abkürzungen
2. **Konsistenz:** Halte Schreibweise einheitlich
3. **Testen:** Prüfe Makros mit verschiedenen Mitarbeiterdaten
