# Mitarbeiter-Verwaltung

## Übersicht
Mitarbeiter (Clients) sind die zentralen Entitäten in Klacks für die Personalplanung.

## Mitarbeiter anlegen

### Pflichtfelder
- Vorname
- Nachname
- Geschlecht (Herr/Frau/Divers)

### Optionale Felder
- Geburtsdatum
- E-Mail
- Telefon
- Adresse (Strasse, PLZ, Ort, Kanton, Land)
- Gruppe/Abteilung

## Mitarbeiter-Status

| Status | Beschreibung |
|--------|-------------|
| Aktiv | Mitarbeiter ist aktiv und planbar |
| Inaktiv | Mitarbeiter ist temporär nicht verfügbar |
| Ausgetreten | Mitarbeiter hat das Unternehmen verlassen |

## Gruppen und Abteilungen

Mitarbeiter können Gruppen zugeordnet werden:
- Hierarchische Struktur möglich
- Mehrfachzuordnung erlaubt
- Berechtigungen auf Gruppenebene

### Gruppen-Pfad
Format: `Firma > Abteilung > Team`
Beispiel: `Klacks AG > IT > Entwicklung`

## Import-Möglichkeiten

### Manueller Import
CSV-Datei mit Spalten:
```
Vorname;Nachname;Email;Geburtsdatum;Strasse;PLZ;Ort
```

### LDAP/AD-Synchronisation
Automatischer Import aus Active Directory oder LDAP.
Siehe: Identity Provider Dokumentation

## Verträge

Jeder Mitarbeiter kann mehrere Verträge haben:
- Verschiedene Pensen (z.B. 80%, 100%)
- Zeitlich begrenzt oder unbefristet
- Pro Kanton unterschiedlich

### Vertragstypen
| Typ | Stunden/Monat | Beschreibung |
|-----|---------------|-------------|
| Vollzeit 160 | 160h | Standard Vollzeit |
| Vollzeit 180 | 180h | Erhöhte Vollzeit |
| Teilzeit 80 | 128h | 80% Pensum |
| Teilzeit 50 | 80h | 50% Pensum |

## Suche

### Suchfelder
- Name (Vor- und Nachname)
- E-Mail
- Personalnummer
- Gruppe

### Filteroptionen
- Nach Kanton
- Nach Status
- Nach Gruppe
- Nach Vertragstyp
