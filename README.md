# GSC Importer

Importeur mensuel des exports ZIP de Google Search Console vers une base SQLite normalisée. Il traite plusieurs propriétés GSC, notamment les sites de Cyril et Marie, sans dépendre de l’ordre des pages ou des requêtes dans les exports.

## Architecture

La solution applique une architecture hexagonale :

- `GscImporter.Domain` contient les concepts métier sans dépendance technique ;
- `GscImporter.Application` orchestre le cas d’usage et définit les ports ;
- `GscImporter.Infrastructure` adapte ZIP/CSV, SQLite et système de fichiers ;
- `GscImporter.Cli` compose l’application et fournit le point d’entrée ;
- `GscImporter.Tests` couvre le domaine, le cas d’usage et les adaptateurs importants.

Le domaine et l’application ne référencent ni SQLite, ni `System.IO.Compression`, ni l’implémentation du système de fichiers.

## Prérequis

- SDK .NET 10

SQLite est utilisé via `Microsoft.Data.Sqlite`. L’installation séparée de `sqlite3` est facultative, mais pratique pour consulter la base.

## Configuration

Adapter `src/GscImporter.Cli/appsettings.json` :

```json
{
  "Database": "data/gsc.db",
  "ImportDirectory": "imports",
  "ArchiveDirectory": "archives"
}
```

Les chemins relatifs sont résolus depuis le répertoire contenant le fichier de configuration.

## Utilisation

1. Créer les répertoires configurés si nécessaire.
2. Déposer tous les exports GSC `.zip` dans `imports`.
3. Lancer :

```bash
dotnet run --project src/GscImporter.Cli
```

Avec un autre fichier de configuration :

```bash
dotnet run --project src/GscImporter.Cli -- --config C:\chemin\appsettings.json
```

Après succès, chaque ZIP est déplacé vers `archives/AAAA-MM/`. En cas d’erreur de lecture ou d’écriture en base, il reste dans `imports` et la commande retourne le code `1`.

## Règles d’import

- Le site est extrait du nom produit par GSC, par exemple `https___cyril.cophignon.net_-Performance-on-Search-2026-09-05.zip`.
- Les URL de `Pages.csv` sont contrôlées par rapport au site détecté.
- Le mois est déduit des dates ISO de `Graphique.csv`.
- L’export doit couvrir exactement un mois civil complet.
- `Pages.csv` et `Requêtes.csv` sont transformés en mesures normalisées.
- Le CTR est stocké sous forme décimale : `31.82%` devient `0.3182`.
- Réimporter un site et un mois remplace transactionnellement toutes leurs anciennes mesures.
- Un nom de ZIP déjà présent dans l’archive reçoit un suffixe `_2`, `_3`, etc. Aucun fichier archivé n’est écrasé.

## Modèle SQLite

La clé primaire métier de `Measurements` est :

```text
SiteId + ReportingMonth + DimensionType + Element + Metric
```

Exemple de lecture :

```sql
SELECT s.Url, m.ReportingMonth, m.DimensionType, m.Element, m.Metric, m.Value
FROM Measurements m
JOIN Sites s ON s.Id = m.SiteId
ORDER BY s.Url, m.ReportingMonth, m.DimensionType, m.Element, m.Metric;
```

## Tests

```bash
dotnet test
```

Les tests couvrent notamment :

- normalisation et validation des URL de site ;
- validation du mois civil complet ;
- conversion des pages et requêtes en métriques ;
- rejet d’une page appartenant à un autre site ;
- remplacement complet lors d’un réimport ;
- ordre persistance puis archivage ;
- absence d’archivage lorsque la persistance échoue.

## Publication autonome facultative

```bash
dotnet publish src/GscImporter.Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
