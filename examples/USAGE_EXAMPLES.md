# Example Usage Guide

## Preparing Test Environment

### 1. Creating Database from Examples

```bash
# Windows PowerShell
dotnet run -- build-db --db-dir "C:\temp\testdb" --scripts-dir "examples"
```

This creates database `C:\temp\testdb\database.fdb` with all objects from the `examples/` directory.

### 2. Exporting Metadata

```bash
dotnet run -- export-scripts --connection-string "database=C:\temp\testdb\database.fdb;user=SYSDBA;password=masterkey" --output-dir "C:\temp\exported"
```

Check structure in `C:\temp\exported`:
- `domains/` - 3 domains
- `tables/` - 3 tables
- `procedures/` - 2 procedures

### 3. Rebuilding Database from Scripts

```bash
dotnet run -- build-db --db-dir "C:\temp\rebuilt" --scripts-dir "C:\temp\exported"
```

### 4. Verification

Compare `C:\temp\testdb\database.fdb` with `C:\temp\rebuilt\database.fdb` using FlameRobin or isql.

## Testing with Your Own Database

### Scenario A: Export from Existing Database

```bash
dotnet run -- export-scripts --connection-string "database=C:\mydatabase.fdb;user=SYSDBA;password=masterkey" --output-dir "C:\backup-scripts"
```

### Scenario B: Migration Between Environments

1. Export from DEV:
```bash
dotnet run -- export-scripts --connection-string "database=C:\dev\app.fdb;user=SYSDBA;password=masterkey" --output-dir "C:\migration"
```

2. Build on TEST:
```bash
dotnet run -- build-db --db-dir "C:\test" --scripts-dir "C:\migration"
```

### Scenario C: Update Existing Database

```bash
dotnet run -- update-db --connection-string "database=C:\mydatabase.fdb;user=SYSDBA;password=masterkey" --scripts-dir "C:\new-scripts"
```

## Common Issues

### Connection refused
- Ensure Firebird Server is running (services.msc → Firebird)

### Access denied
- Check directory permissions
- Run cmd/PowerShell as Administrator

### Invalid connection string
Correct format:
```
database=C:\path\file.fdb;user=SYSDBA;password=masterkey;charset=UTF8
```
