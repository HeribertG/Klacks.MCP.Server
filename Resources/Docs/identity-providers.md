# Identity Provider Konfiguration

## Übersicht
Identity Provider ermöglichen Single Sign-On (SSO) und automatische Mitarbeiter-Synchronisation.

## Unterstützte Typen

### LDAP (Lightweight Directory Access Protocol)
- **Port Standard:** 389 (unverschlüsselt), 636 (SSL/TLS)
- **Anwendungsfall:** OpenLDAP, Synology Directory Server
- **User-Filter Beispiel:** `(objectClass=inetOrgPerson)`

### Active Directory
- **Port Standard:** 389 (LDAP), 636 (LDAPS), 3268 (Global Catalog)
- **Anwendungsfall:** Microsoft Active Directory
- **User-Filter Beispiel:** `(&(objectClass=user)(objectCategory=person))`

### OAuth2 / OpenID Connect
- **Anwendungsfall:** Azure AD, Google, Okta
- **Benötigt:** Client ID, Client Secret, Authorization URL, Token URL

## Konfigurationsfelder

| Feld | Beschreibung | Beispiel |
|------|-------------|----------|
| Host | Server-Adresse | `ldap.example.com` |
| Port | Server-Port | `389` oder `636` |
| Base DN | Suchbasis | `DC=example,DC=com` |
| Bind DN | Admin-Benutzer | `CN=admin,DC=example,DC=com` |
| User Filter | LDAP-Filter | `(objectClass=user)` |

## LDAP-Attribut-Mapping

| Klacks-Feld | AD-Attribut | OpenLDAP-Attribut |
|-------------|-------------|-------------------|
| Vorname | givenName | givenName |
| Nachname | sn | sn |
| E-Mail | mail | mail |
| Benutzername | sAMAccountName | uid |
| Strasse | streetAddress | street |
| PLZ | postalCode | postalCode |
| Stadt | l | l |

## Troubleshooting

### Verbindung fehlgeschlagen
1. Prüfe Host und Port (Firewall?)
2. Bei SSL: Zertifikat gültig?
3. Bind DN und Passwort korrekt?

### Keine Benutzer gefunden
1. Base DN korrekt?
2. User Filter zu restriktiv?
3. Hat der Bind-User Leserechte?

### Synchronisation bringt keine Daten
- Prüfe ob die LDAP-Attribute gesetzt sind
- OpenLDAP nutzt `street` statt `streetAddress`

## Tipps

### Synology NAS als LDAP-Server
1. Synology Directory Server installieren
2. Benutzer in LDAP anlegen
3. Base DN: `dc=synology,dc=local` (oder eigene Domain)
4. Bind DN: `uid=admin,cn=users,dc=synology,dc=local`
