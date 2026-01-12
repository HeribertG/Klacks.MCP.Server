# Schichtplanung

## Übersicht
Die Schichtplanung ermöglicht die Zuweisung von Arbeitszeiten an Mitarbeiter.

## Schicht-Typen

### Reguläre Schichten
- Feste Start- und Endzeit
- Wiederkehrend (täglich, wöchentlich)
- Mit oder ohne Pause

### Bereitschaft
- On-Call Dienste
- Pikettdienst
- Rufbereitschaft

### Abwesenheiten
- Ferien
- Krankheit
- Weiterbildung
- Sonderurlaub

## Schicht anlegen

### Pflichtfelder
| Feld | Beschreibung |
|------|-------------|
| Mitarbeiter | Zugewiesener Mitarbeiter |
| Datum | Tag der Schicht |
| Startzeit | Beginn der Schicht |
| Endzeit | Ende der Schicht |
| Schichttyp | Art der Schicht |

### Optionale Felder
- Pause (Dauer in Minuten)
- Bemerkung
- Standort
- Projekt/Kostenstelle

## Ansichten

### Tagesansicht
- Alle Schichten eines Tages
- Sortiert nach Startzeit
- Überschneidungen sichtbar

### Wochenansicht
- Montag bis Sonntag
- Mitarbeiter als Zeilen
- Schichten als Blöcke

### Monatsansicht
- Kalenderübersicht
- Aggregierte Stunden
- Feiertage markiert

## Regeln und Validierung

### Arbeitszeit-Regeln
- Maximale Tagesstunden
- Ruhezeit zwischen Schichten
- Wochenarbeitszeit-Limite

### Konflikte
- Überlappende Schichten werden markiert
- Doppelbuchungen werden verhindert
- Warnungen bei Regelverstoß

## Vorlagen

### Schicht-Vorlagen
Häufig verwendete Schichten als Vorlage speichern:
- Frühschicht: 06:00 - 14:00
- Spätschicht: 14:00 - 22:00
- Nachtschicht: 22:00 - 06:00

### Wochen-Vorlagen
Ganze Wochenpläne als Vorlage speichern und wiederverwenden.

## Export

### Formate
- PDF (Druckansicht)
- Excel (Tabelle)
- iCal (Kalender-Import)

### Berichte
- Stundenübersicht pro Mitarbeiter
- Abteilungs-Auslastung
- Überstunden-Report
