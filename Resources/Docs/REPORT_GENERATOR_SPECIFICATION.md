# Report Generator - Technical Specification

> **Status:** Implementiert
> **Last Updated:** 2026-02-06
> **Architecture:** Clean Architecture (Domain/Application/Infrastructure/Presentation)

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Architecture Overview](#2-architecture-overview)
3. [Domain Model](#3-domain-model)
4. [Data Sources & Data Sets](#4-data-sources--data-sets)
5. [Data Binding & Available Fields](#5-data-binding--available-fields)
6. [Report Designer UI](#6-report-designer-ui)
7. [PDF Generation](#7-pdf-generation)
8. [API Reference](#8-api-reference)
9. [File Structure](#9-file-structure)
10. [Data Flow](#10-data-flow)

---

## 1. Executive Summary

### 1.1 Purpose

Konfigurierbarer Report Generator fuer verschiedene Datenquellen (Schedule, Adressen, Gruppen, Schichten, etc.). Erstellt PDF-Reports mit konfigurierbaren Header-, Tabellen- und Footer-Sektionen.

### 1.2 Key Capabilities

- **Multi-Source Design**: 7 verschiedene Datenquellen (Schedule, Abwesenheiten, Adressen, Gruppen, Schichten, Container-Templates)
- **Multi-DataSet Support**: Schedule-Source unterstuetzt Multi-Select (Work, WorkChange, Break, Expenses gleichzeitig)
- **Template-Based Design**: Wiederverwendbare Report-Templates mit CRUD via REST API
- **Drag-and-Drop Designer**: Felder per CDK Drag & Drop in Sektionen ziehen
- **Frontend PDF-Generierung**: Client-seitige PDF-Erstellung mit jsPDF + jspdf-autotable
- **Multi-Language Support**: i18n in de, en, fr, it via ngx-translate
- **Pro-Mitarbeiter Reports**: Automatische Gruppierung nach Client mit je einer PDF-Seite (bei Schedule-Source)
- **Field Prefix System**: Automatische Prefix-Erkennung bei Label-Kollisionen zwischen DataSets
- **Rich Styling**: Font-Familie, Groesse, Bold/Italic/Underline, Textfarbe, Ausrichtung pro Feld
- **Header-Bilder**: Unterstuetzung fuer Bilder im Header (Firmenlogo etc.)

### 1.3 Report-Struktur (Beispiel: Schedule mit Work + Expenses)

```
+--------------------------------------------------+
|  HEADER                                          |
|  [Logo] [Name] [Vorname]   [Zeitraum]           |
|         [Firma]             [Gruppe]             |
+--------------------------------------------------+
|  TABELLE: Work/WorkChange/Break/Expenses         |
|  Spalten via Drag & Drop konfiguriert            |
|  [Datum] [Von] [Bis] [Stunden] [Typ] ...        |
+--------------------------------------------------+
|  FOOTER                                          |
|  [Summe Stunden] [Summe Zuschlaege] [Summe EUR] |
+--------------------------------------------------+
```

---

## 2. Architecture Overview

### 2.1 Schichten

```
+------------------------------------------------------------------+
|  FRONTEND (Angular)                                              |
|  +-- Presentation: Reports-Komponenten, Designer, Modal          |
|  +-- Domain: Models, Services (PDF, DataProvider, State)         |
|  +-- Infrastructure: API Services (HTTP)                         |
+------------------------------------------------------------------+
          | HTTP REST (Template CRUD)
          | HTTP POST  (Daten laden pro Source)
+------------------------------------------------------------------+
|  BACKEND (.NET)                                                  |
|  +-- Presentation: ReportTemplatesController                     |
|  +-- Application: Commands, Queries, Handlers                    |
|  +-- Domain: Models, Enums                                       |
|  +-- Infrastructure: Repository (Entity Framework + JSONB)       |
+------------------------------------------------------------------+
```

### 2.2 Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend Framework | Angular 21, TypeScript, SCSS |
| UI Components | ng-bootstrap, Angular CDK (Drag & Drop) |
| PDF Generierung | jsPDF + jspdf-autotable (client-seitig) |
| Backend Framework | .NET 10, ASP.NET Core |
| Data Access | Entity Framework Core 10, PostgreSQL |
| Mediation | Custom Mediator (BaseHandler) |
| Mapping | Riok.Mapperly |
| i18n | ngx-translate (de, en, fr, it) |

### 2.3 Design-Entscheidungen

- **PDF-Generierung im Frontend**: Kein Backend-Roundtrip noetig, schnellere Vorschau
- **Multi-Source-Architektur**: DataSource/DataSet-System fuer verschiedene Datenquellen
- **Provider-Pattern**: Jede DataSource hat einen eigenen Provider fuer Datenabruf + Feld-Aufloesung
- **Backend nur fuer Template-Persistenz**: Kein serverseitiges PDF-Rendering
- **JSONB-Speicherung**: Sections, PageSetup und DataSetIds als JSONB in PostgreSQL

---

## 3. Domain Model

### 3.1 Frontend Models

#### ReportTemplate

```typescript
interface ReportTemplate {
  id?: string;
  name: string;
  description: string;
  type: ReportType;
  sourceId?: string;           // DataSource-ID, z.B. 'schedule', 'group'
  dataSetIds?: string[];       // Ausgewaehlte DataSets, z.B. ['work', 'expenses']
  pageSetup: ReportPageSetup;
  sections: ReportSection[];
  isDeleted?: boolean;
  createdAt?: Date;
  updatedAt?: Date;
}
```

#### ReportSection

```typescript
interface ReportSection {
  id?: string;
  type: ReportSectionType;
  fields: ReportField[];
  visible: boolean;
  sortOrder: number;
}

enum ReportSectionType {
  Header = 0,
  WorkTable = 1,
  ExpensesTable = 2,
  Footer = 3
}
```

#### ReportField

```typescript
interface ReportField {
  id?: string;
  name: string;
  dataBinding: string;
  type: ReportFieldType;
  width: number;
  height: number;
  style: FieldStyle;
  format?: string;
  sortOrder: number;        // Im Header: Zeilen-Index
  imageUrl?: string;        // Fuer Image-Felder
}

enum ReportFieldType {
  Text = 0, Date = 1, Number = 2, Currency = 3,
  Boolean = 4, Time = 5, Image = 6
}
```

#### FieldStyle

```typescript
interface FieldStyle {
  fontSize: number;
  bold: boolean;
  italic: boolean;
  underline: boolean;
  fontFamily: string;         // 'helvetica', 'times', 'courier'
  alignment: TextAlignment;   // Left=0, Center=1, Right=2
  textColor: string;          // Hex '#000000'
  backgroundColor: string;
}
```

#### Page Setup

```typescript
interface ReportPageSetup {
  orientation: ReportOrientation;  // Portrait=0, Landscape=1
  size: ReportPageSize;            // A4=0, A3=1, Letter=2
  margins: ReportMargins;          // top, bottom, left, right (mm)
}
// Default: Landscape A4, 20mm Margins
```

### 3.2 Enums

| Enum | Werte | Verwendung |
|------|-------|-----------|
| `ReportType` | Schedule=0, Client=1, Invoice=2, Absence=3 | Template-Typ |
| `ReportSectionType` | Header=0, WorkTable=1, ExpensesTable=2, Footer=3 | Sektions-Typ |
| `ReportFieldType` | Text=0, Date=1, Number=2, Currency=3, Boolean=4, Time=5, Image=6 | Feld-Datentyp |
| `ReportOrientation` | Portrait=0, Landscape=1 | Seitenausrichtung |
| `ReportPageSize` | A4=0, A3=1, Letter=2 | Seitengroesse |
| `TextAlignment` | Left=0, Center=1, Right=2 | Textausrichtung |
| `FieldCategory` | header, workTable, expensesTable, footer | Palette-Gruppierung |

### 3.3 Backend Model (C#)

```csharp
public class ReportTemplate : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ReportType Type { get; set; }
    public string SourceId { get; set; } = "schedule";
    public List<string> DataSetIds { get; set; } = ["work"];
    public ReportPageSetup PageSetup { get; set; }      // JSONB
    public List<ReportSection> Sections { get; set; }    // JSONB
}
```

`PageSetup`, `Sections` und `DataSetIds` werden als JSONB-Spalten in PostgreSQL gespeichert.

---

## 4. Data Sources & Data Sets

### 4.1 Konzept

Jede **DataSource** repraesentiert einen Bereich der Applikation. DataSources haben ein oder mehrere **DataSets**. Bei `multiSelect: true` koennen mehrere DataSets gleichzeitig ausgewaehlt werden.

### 4.2 Uebersicht

| Source ID | Pfad | DataSets | Multi-Select | Benoetigt Datum |
|-----------|------|----------|-------------|-----------------|
| `schedule` | workplace/schedule | work, workChange, break, expenses | Ja | Ja |
| `absence-gantt` | workplace/absence-gantt | absences | Nein | Ja |
| `all-address` | workplace/address/all-address | clients | Nein | Nein |
| `edit-address` | workplace/address/edit-address | details | Nein | Nein |
| `group` | workplace/group | groups | Nein | Nein |
| `shift-table` | workplace/shift/all-shift/shift-table | shifts | Nein | Nein |
| `container-template` | workplace/shift/container-template | items | Nein | Nein |

### 4.3 Schedule DataSets (Multi-Select)

| DataSet | ID | Entry-Typ | Tabellenfelder |
|---|---|---|---|
| Arbeit | `work` | Work(0) | `entry.*` |
| Arbeitsaenderung | `workChange` | WorkChange(1) | `entry.*` (gleiche Felder wie Work) |
| Pause | `break` | Break(3) | `entry.*` (gleiche Felder wie Work) |
| Spesen | `expenses` | Expenses(2) | `expense.*` (eigene Felder) |

Work/WorkChange/Break teilen sich dieselben `entry.*`-Felder. Expenses hat eigene `expense.*`-Felder.

**API**: `DataWorkScheduleService.getWorkSchedule()` → `IWorkScheduleResponse`. Filterung nach `entryType` im Frontend via `mapDataSetIdsToEntryTypes()`.

### 4.4 Per-DataSet Palette-Gruppen

Bei Multi-Select (z.B. Schedule mit 4 DataSets) werden Designer-Palette-Gruppen pro DataSet-Gruppe erstellt:
- DataSets die dieselben Tabellenfelder teilen werden zusammengefasst
- Beispiel: Work + WorkChange + Break → gemeinsame Gruppe "Arbeit / Arbeitsaenderung / Pause"
- Expenses → eigene Gruppe "Spesen"

### 4.5 Field Prefix System

Wenn bei Multi-Select verschiedene DataSets Felder mit gleichem Label haben (z.B. "Datum" existiert bei `entry.*` und `expense.*`), generiert `getFieldPrefixMap()` automatisch Prefixe. Im PDF erscheinen die Spaltenueberschriften dann als "Arbeit.Datum" und "Spesen.Datum".

Die Funktion erkennt Label-Kollisionen zur Laufzeit via Translation-Lookup und erzeugt nur dort Prefixe, wo tatsaechlich Mehrdeutigkeit besteht.

---

## 5. Data Binding & Available Fields

### 5.1 Header-Felder (alle Sources)

**Client-Felder** (nur bei Sources mit Client-Kontext):

| Key | i18n Key | Typ |
|-----|----------|-----|
| `client.name` | `setting.report.field.clientName` | Text |
| `client.firstName` | `setting.report.field.clientFirstName` | Text |
| `client.company` | `setting.report.field.clientCompany` | Text |
| `client.idNumber` | `setting.report.field.clientIdNumber` | Number |

**Allgemeine Felder** (alle Sources):

| Key | i18n Key | Typ |
|-----|----------|-----|
| `report.period` | `setting.report.field.reportPeriod` | Text |
| `report.date` | `setting.report.field.reportDate` | Date |
| `report.groupName` | `setting.report.field.groupName` | Text |
| `report.image` | `setting.report.field.reportImage` | Image |
| `report.customText` | `setting.report.field.customText` | Text |

### 5.2 Schedule: Entry-Felder (entry.*)

Verwendet von Work, WorkChange, Break DataSets:

| Key | i18n Key | Typ | Quelle |
|-----|----------|-----|--------|
| `entry.date` | `setting.report.field.entryDate` | Date | IScheduleCell.entryDate |
| `entry.weekday` | `setting.report.field.entryWeekday` | Text | Berechnet |
| `entry.startTime` | `setting.report.field.entryStartTime` | Time | IScheduleCell.startTime |
| `entry.endTime` | `setting.report.field.entryEndTime` | Time | IScheduleCell.endTime |
| `entry.hours` | `setting.report.field.entryHours` | Number | IScheduleCell.changeTime |
| `entry.surcharges` | `setting.report.field.entrySurcharges` | Number | IScheduleCell.surcharges |
| `entry.shiftName` | `setting.report.field.entryShiftName` | Text | IScheduleCell.entryName |
| `entry.shiftAbbr` | `setting.report.field.entryShiftAbbr` | Text | IScheduleCell.abbreviation |
| `entry.type` | `setting.report.field.entryType` | Text | Work/Break/WorkChange |
| `entry.information` | `setting.report.field.entryInformation` | Text | IScheduleCell.information |
| `entry.description` | `setting.report.field.entryDescription` | Text | IScheduleCell.description (multilang) |

### 5.3 Schedule: Expense-Felder (expense.*)

| Key | i18n Key | Typ | Quelle |
|-----|----------|-----|--------|
| `expense.date` | `setting.report.field.expenseDate` | Date | IScheduleCell.entryDate |
| `expense.description` | `setting.report.field.expenseDescription` | Text | IScheduleCell.description (multilang) |
| `expense.amount` | `setting.report.field.expenseAmount` | Currency | IScheduleCell.amount |
| `expense.taxable` | `setting.report.field.expenseTaxable` | Boolean | IScheduleCell.taxable |
| `expense.shiftName` | `setting.report.field.expenseShiftName` | Text | IScheduleCell.entryName |

### 5.4 Schedule: Footer-Felder (sum.*)

| Key | i18n Key | Typ | Berechnung |
|-----|----------|-----|-----------|
| `sum.hours` | `setting.report.field.sumHours` | Number | SUM(changeTime) |
| `sum.surcharges` | `setting.report.field.sumSurcharges` | Number | SUM(surcharges) |
| `sum.expenses` | `setting.report.field.sumExpenses` | Currency | SUM(amount) |
| `sum.workDays` | `setting.report.field.sumWorkDays` | Number | COUNT DISTINCT Dates (nur Work) |

### 5.5 Absence-Felder (absence.*)

| Key | i18n Key | Typ |
|-----|----------|-----|
| `absence.clientName` | `setting.report.field.absenceClientName` | Text |
| `absence.clientFirstName` | `setting.report.field.absenceClientFirstName` | Text |
| `absence.absenceName` | `setting.report.field.absenceName` | Text |
| `absence.from` | `setting.report.field.absenceFrom` | Date |
| `absence.until` | `setting.report.field.absenceUntil` | Date |
| `absence.information` | `setting.report.field.absenceInformation` | Text |
| `absence.totalCount` | `setting.report.field.absenceTotalCount` | Number (Footer) |

### 5.6 Address-Felder

**Adressliste (client.list.*)**:

| Key | i18n Key | Typ |
|-----|----------|-----|
| `client.list.idNumber` | `setting.report.field.clientListIdNumber` | Number |
| `client.list.company` | `setting.report.field.clientListCompany` | Text |
| `client.list.firstName` | `setting.report.field.clientListFirstName` | Text |
| `client.list.name` | `setting.report.field.clientListName` | Text |
| `client.list.gender` | `setting.report.field.clientListGender` | Text |
| `client.list.type` | `setting.report.field.clientListType` | Text |
| `client.list.birthdate` | `setting.report.field.clientListBirthdate` | Date |
| `client.totalCount` | `setting.report.field.clientTotalCount` | Number (Footer) |

**Adress-Details (address.*)**:

| Key | i18n Key | Typ |
|-----|----------|-----|
| `address.type` | `setting.report.field.addressType` | Text |
| `address.street` | `setting.report.field.addressStreet` | Text |
| `address.zip` | `setting.report.field.addressZip` | Text |
| `address.city` | `setting.report.field.addressCity` | Text |
| `address.country` | `setting.report.field.addressCountry` | Text |
| `address.validFrom` | `setting.report.field.addressValidFrom` | Date |

### 5.7 Group-Felder (group.*)

| Key | i18n Key | Typ |
|-----|----------|-----|
| `group.name` | `setting.report.field.groupTableName` | Text |
| `group.description` | `setting.report.field.groupDescription` | Text |
| `group.validFrom` | `setting.report.field.groupValidFrom` | Date |
| `group.validUntil` | `setting.report.field.groupValidUntil` | Date |
| `group.clientsCount` | `setting.report.field.groupClientsCount` | Number |
| `group.shiftsCount` | `setting.report.field.groupShiftsCount` | Number |
| `group.totalCount` | `setting.report.field.groupTotalCount` | Number (Footer) |

### 5.8 Shift-Felder (shift.*)

| Key | i18n Key | Typ |
|-----|----------|-----|
| `shift.name` | `setting.report.field.shiftName` | Text |
| `shift.abbreviation` | `setting.report.field.shiftAbbreviation` | Text |
| `shift.startShift` | `setting.report.field.shiftStartShift` | Time |
| `shift.endShift` | `setting.report.field.shiftEndShift` | Time |
| `shift.fromDate` | `setting.report.field.shiftFromDate` | Date |
| `shift.untilDate` | `setting.report.field.shiftUntilDate` | Date |
| `shift.workTime` | `setting.report.field.shiftWorkTime` | Number |
| `shift.description` | `setting.report.field.shiftDescription` | Text |
| `shift.totalCount` | `setting.report.field.shiftTotalCount` | Number (Footer) |

### 5.9 Container-Template-Felder (ct.*)

| Key | i18n Key | Typ |
|-----|----------|-----|
| `ct.weekday` | `setting.report.field.ctWeekday` | Text |
| `ct.fromTime` | `setting.report.field.ctFromTime` | Time |
| `ct.untilTime` | `setting.report.field.ctUntilTime` | Time |
| `ct.shiftName` | `setting.report.field.ctShiftName` | Text |
| `ct.briefingTime` | `setting.report.field.ctBriefingTime` | Time |
| `ct.debriefingTime` | `setting.report.field.ctDebriefingTime` | Time |
| `ct.travelTimeBefore` | `setting.report.field.ctTravelTimeBefore` | Time |
| `ct.travelTimeAfter` | `setting.report.field.ctTravelTimeAfter` | Time |
| `ct.totalCount` | `setting.report.field.ctTotalCount` | Number (Footer) |

---

## 6. Report Designer UI

### 6.1 Modal-Tabs

Das Report-Modal hat **4 Tabs**:

1. **Allgemein** → Name, Beschreibung, Orientierung (Portrait/Landscape)
2. **Quelle** → DataSource-Auswahl (Karten-Grid) + DataSet-Auswahl (bei Multi-Select)
3. **Designer** → Drag-Drop-Designer fuer Sektionen/Felder
4. **Vorschau** → Parameter-Eingabe + PDF-Generierung

### 6.2 Source-Tab

- DataSources als Karten in einem Grid
- Bei Klick auf eine Source werden die Sektionen zurueckgesetzt
- Bei Multi-Select-Sources (schedule): Checkboxen fuer DataSet-Auswahl
- Mindestens ein DataSet muss ausgewaehlt sein

### 6.3 Designer-Layout

```
+------------------+----------------------------------+
|  FELD-PALETTE    |  REPORT-VORSCHAU                 |
|                  |                                  |
|  > Header (5)    |  +- HEADER ------------------+  |
|    [Name]        |  |  [Logo] [Name] [Vorname]   |  |
|    [Vorname]     |  |  [Zeitraum]    [Gruppe]    |  |
|    [Logo]        |  +----------------------------+  |
|    ...           |                                  |
|                  |  +- TABELLE ----------------+   |
|  > Arbeit/       |  |  Datum | Von | Bis | Std  |  |
|    Aenderung/    |  +----------------------------+  |
|    Pause (11)    |                                  |
|    [Datum]       |  +- FOOTER ------------------+  |
|    [Von]         |  |  [Sum Std] [Sum Zuschlaege]|  |
|    ...           |  +----------------------------+  |
|                  |                                  |
|  > Spesen (5)    |                                  |
|    [Betrag]      |                                  |
|    ...           |                                  |
|                  |                                  |
|  > Footer (4)    |                                  |
|    [Sum Stunden] |                                  |
+------------------+----------------------------------+
```

### 6.4 Palette-Gruppen

- Bei Single-DataSet: Standard-Gruppen (Header, Tabelle, Footer)
- Bei Multi-Select: Per-DataSet-Gruppen fuer Tabelle/Footer
  - DataSets mit gleichen Tabellenfeldern werden zusammengefasst
  - Kombinierter Titel: "Arbeit / Arbeitsaenderung / Pause"
- Gruppen sind collapsible (Klick auf Header)

### 6.5 Header-Sektion

- **Zeilen-basiert**: `sortOrder` = Zeilen-Index (gleicher sortOrder = gleiche Zeile)
- **3 Zonen pro Zeile**: Links, Mitte, Rechts (via `TextAlignment`)
- **Bild-Unterstuetzung**: Image-Felder mit konfigurierbarer Breite/Hoehe
- **Freitext**: `report.customText` mit mehrzeiligem Text (\\n)
- Neue Zeile via Dropdown oder auto-increment

### 6.6 Body-Tabelle

- Spaltenbasiert mit konfigurierbarer Reihenfolge (Drag)
- Spaltenbreite in relativen Einheiten (proportional auf Seitenbreite verteilt)
- Duplikat-Schutz: Jedes Feld kann nur einmal hinzugefuegt werden

### 6.7 Footer-Sektion

- Aggregat-Felder (Summen)
- Reihenfolge per Drag aenderbar
- Darstellung als "Label: Wert"-Zeilen

### 6.8 Feld-Styling

Pro Feld konfigurierbar:
- Schriftfamilie: Helvetica, Times, Courier
- Schriftgroesse (pt)
- Bold, Italic, Underline Toggles
- Textfarbe (Hex-Color-Picker)
- Ausrichtung (Links/Mitte/Rechts)
- Spaltenbreite (Tabelle/Footer)
- Bild-Dimensionen (nur Image-Typ)

### 6.9 Speichern

Zwei Buttons im Modal-Footer:
- **Speichern** → Speichert, Modal bleibt offen
- **Speichern & Schliessen** → Speichert und schliesst Modal

---

## 7. PDF Generation

### 7.1 Service: ReportPdfService

```typescript
interface ReportGenerationContext {
  template: ReportTemplate;
  provider: ReportDataProvider;     // Source-spezifischer Provider
  data: ReportData;                 // Abgerufene Daten
  groupName: string;
  startDate: string;
  endDate: string;
  imageCache?: Map<string, string>; // Vorgeladene Bilder als DataURL
}
```

### 7.2 Pipeline

```
ReportGenerationContext
  |
  v
preloadImages()
  +-- Alle Image-Felder als DataURLs via LoadFile/DownLoad API laden
  +-- Caching zwischen Preview-Aufrufen
  |
  v
forEach Client (oder einmal wenn keine Clients):
  +-- renderHeader()   -> Zeilen mit L/M/R-Zonen, Bilder, Text
  +-- renderTable()    -> Body-Sektion(en) als autoTable
  +-- renderFooter()   -> Summen mit Trennlinie
  +-- Neue Seite fuer naechsten Client
  |
  v
jsPDF.output('blob') -> Blob
```

### 7.3 Header-Rendering

- Felder werden nach `sortOrder` in Zeilen gruppiert
- Pro Zeile 3 Zonen: Links, Mitte, Rechts
- Zone-StartX wird berechnet basierend auf Gesamt-Textbreite
- Bilder werden inline gerendert (JPEG/PNG auto-detect)
- Freitext (`report.customText`) unterstuetzt mehrzeilige Darstellung

### 7.4 Tabellen-Rendering

- jspdf-autotable mit Grid-Theme
- Spaltenbreiten proportional: `(field.width / totalWidth) * contentWidth`
- Header-Zeile: Dunkelgrau (#424242), weisse Schrift, Bold, 9pt
- Body: 9pt, Styles aus FieldStyle (Groesse, Bold/Italic, Alignment)
- Alternierende Zeilen: #F5F5F5

### 7.5 Footer-Rendering

- Horizontale Trennlinie
- Felder als "Label: Wert"-Zeilen
- Individuelle Styles pro Feld
- Underline-Unterstuetzung

### 7.6 Spaltenueberschriften mit Prefix

`translateFieldName()` uebersetzt Feld-Bindings zu i18n-Labels. Bei Multi-Select mit Label-Kollisionen wird der DataSet-Name als Prefix vorangestellt:
- "Datum" → "Arbeit.Datum" / "Spesen.Datum"
- "Schicht" → "Arbeit.Schicht" / "Spesen.Schicht"

### 7.7 Formatierung

| Typ | Format | Beispiel |
|-----|--------|---------|
| Datum | dd.MM.yyyy (de-CH) | 05.02.2026 |
| Zeit | HH:mm (substring 0-5) | 08:30 |
| Stunden | H:mm (aus Dezimal) | 8:30, -1:15 |
| Waehrung | 2 Dezimalstellen | 150.00 |
| Boolean | Ja/Nein (uebersetzt) | Ja |
| Wochentag | 2-Buchstaben | Mo, Di, Mi |
| Description | Aktuelle Sprache aus MultiLanguage-Objekt | "Beschreibung" |

---

## 8. API Reference

### 8.1 Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/backend/reporttemplates` | Alle Templates laden |
| GET | `/api/backend/reporttemplates/{id}` | Template nach ID |
| GET | `/api/backend/reporttemplates/by-type/{type}` | Templates nach Typ |
| POST | `/api/backend/reporttemplates` | Template erstellen |
| PUT | `/api/backend/reporttemplates/{id}` | Template aktualisieren |
| DELETE | `/api/backend/reporttemplates/{id}` | Template loeschen |

**Authentifizierung:** `[Authorize(Roles = "Admin")]`

### 8.2 Request/Response Beispiel

**POST /api/backend/reporttemplates**
```json
{
  "name": "Monatsreport",
  "description": "Monatlicher Schedule-Report",
  "type": 0,
  "sourceId": "schedule",
  "dataSetIds": ["work", "expenses"],
  "pageSetup": {
    "orientation": 1,
    "size": 0,
    "margins": { "top": 20, "bottom": 20, "left": 20, "right": 20 }
  },
  "sections": [
    {
      "type": 0,
      "visible": true,
      "sortOrder": 0,
      "fields": [
        {
          "name": "Name",
          "dataBinding": "client.name",
          "type": 0,
          "width": 30,
          "height": 0,
          "style": {
            "fontSize": 12,
            "bold": true,
            "italic": false,
            "underline": false,
            "fontFamily": "helvetica",
            "alignment": 0,
            "textColor": "#000000",
            "backgroundColor": "#FFFFFF"
          },
          "sortOrder": 0
        }
      ]
    }
  ]
}
```

---

## 9. File Structure

### 9.1 Frontend

```
Klacks.Ui/src/app/
+-- domain/
|   +-- models/report/
|   |   +-- report-template.model.ts       # ReportTemplate, PageSetup, Enums
|   |   +-- report-section.model.ts        # ReportSection, SectionType
|   |   +-- report-field.model.ts          # ReportField, FieldStyle, DataBindingDefinition
|   |   +-- report-data-source.model.ts    # DataSource/DataSet-Definitionen, Hilfsfunktionen
|   +-- services/report/
|   |   +-- report.service.ts              # downloadPdf(), openPdfPreview()
|   |   +-- report-pdf.service.ts          # PDF-Generierung mit jsPDF
|   |   +-- report-data-provider.service.ts # Daten-Provider pro Source
|   |   +-- data-management-report.service.ts # State Management (Signals)
|   +-- models/
|       +-- work-schedule-class.ts         # IScheduleCell, IWorkScheduleClient
|
+-- infrastructure/api/
|   +-- report/
|   |   +-- data-report-api.service.ts     # HTTP CRUD
|   +-- data-work-schedule.service.ts      # Schedule-Daten
|   +-- data-break-placeholder.service.ts  # Abwesenheiten
|   +-- data-client.service.ts             # Adressen
|   +-- data-group.service.ts              # Gruppen
|   +-- data-shift.service.ts              # Schichten
|   +-- data-container-template.service.ts # Container-Templates
|
+-- presentation/workplace/settings/reports/
    +-- reports.component.ts/html/scss             # Hauptliste
    +-- report-header/
    |   +-- report-header.component.ts/html        # Listen-Header
    +-- report-row/
    |   +-- report-row.component.ts/html/scss      # Modal mit 4 Tabs
    +-- report-designer/
        +-- report-designer.component.ts/html/scss # Drag & Drop Designer
```

### 9.2 Backend

```
Klacks.Api/
+-- Domain/
|   +-- Models/Reports/
|   |   +-- ReportTemplate.cs         # Entity mit SourceId + DataSetIds
|   |   +-- ReportSection.cs
|   |   +-- ReportField.cs
|   |   +-- ReportPageSetup.cs
|   |   +-- ReportMargins.cs
|   |   +-- FieldStyle.cs
|   |   +-- BorderStyle.cs
|   +-- Interfaces/
|       +-- IReportTemplateRepository.cs
|
+-- Application/
|   +-- DTOs/Reports/
|   |   +-- ReportTemplateResource.cs  # DTO mit SourceId + DataSetIds
|   +-- Commands/Reports/
|   +-- Queries/Reports/
|   +-- Handlers/Reports/
|   +-- Mappers/Reports/
|       +-- ReportTemplateMapper.cs    # Riok.Mapperly
|
+-- Infrastructure/
|   +-- Repositories/Reports/
|   +-- Persistence/
|       +-- DataBaseContext.cs          # JSONB-Konvertierung
|
+-- Presentation/
    +-- Controllers/UserBackend/Reports/
        +-- ReportTemplatesController.cs
```

---

## 10. Data Flow

### 10.1 Template erstellen

```
User klickt "+"
  -> DataManagementReportService.createDefaultTemplate()
  -> Template mit 3 Sektionen (Header, WorkTable, Footer) + Default PageSetup
  -> ReportRowComponent.openModal()
  -> Tab 1: Name/Beschreibung/Orientierung
  -> Tab 2: DataSource + DataSet(s) waehlen
  -> Tab 3: Felder per Drag & Drop konfigurieren
  -> Tab 4: Preview testen
  -> "Speichern" / "Speichern & Schliessen"
  -> DataManagementReportService.addTemplate()
  -> HTTP POST -> Backend -> PostgreSQL
```

### 10.2 PDF generieren (Preview)

```
User waehlt Parameter im Preview-Tab
  -> Optional: Gruppe aus Dropdown (oder "Alle")
  -> Optional: Von/Bis-Datum mit ngbDatepicker (Default: aktueller Monat)
  -> "Generieren" klicken
  -> ReportDataProviderService.getProvider(sourceId, dataSetIds)
  -> Provider.fetchData(params) -> API-Aufruf je nach Source
  -> ReportGenerationContext zusammenbauen
  -> ReportPdfService.generatePdf(context)
     +-- preloadImages() -> Bilder vorladen
     +-- Pro Client (oder einmal):
     |   +-- renderHeader() -> Zeilen mit Zonen
     |   +-- renderTable()  -> Body als autoTable
     |   +-- renderFooter() -> Summen
     |   +-- Neue Seite
     +-- jsPDF.output('blob')
  -> ReportService.openPdfPreview(blob) -> neuer Browser-Tab
```

### 10.3 Data Provider Pattern

```typescript
interface ReportDataProvider {
  fetchData(params: ReportFetchParams): Promise<ReportData>;
  resolveFieldValue(field: ReportField, row: any): string;
  resolveHeaderValue(field: ReportField, context: ReportHeaderContext): string;
  resolveFooterValue(field: ReportField, rows: any[]): string;
}
```

Jede DataSource implementiert dieses Interface mit source-spezifischer Logik:
- `fetchData()` ruft die passende API auf und liefert normalisierte Daten
- `resolveFieldValue()` mappt ein Feld-Binding auf den Zeilenwert
- `resolveHeaderValue()` mappt ein Feld-Binding auf den Header-Kontext
- `resolveFooterValue()` berechnet Aggregat-Werte (Summen, Counts)

### 10.4 Hilfsfunktionen (report-data-source.model.ts)

| Funktion | Beschreibung |
|---|---|
| `getDataSource(sourceId)` | DataSource per ID finden |
| `getDataSet(sourceId, dataSetId)` | DataSet innerhalb einer Source finden |
| `getAllFieldsForDataSet(sourceId, dataSetId)` | Alle Felder eines DataSets |
| `getAllFieldsForDataSets(sourceId, dataSetIds)` | Deduplizierte Felder mehrerer DataSets |
| `getFieldPrefixMap(sourceId, dataSetIds, translateFn)` | Prefix-Map fuer Label-Kollisionen |

---

**END OF SPECIFICATION**
