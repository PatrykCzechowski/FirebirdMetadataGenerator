# Firebird Metadata Generator

Narzędzie konsolowe .NET 8.0 do zarządzania metadanymi bazy danych Firebird 5.0.

## 🎯 Funkcjonalności

Aplikacja obsługuje trzy główne operacje:

1. **build-db** - Budowa nowej bazy danych ze skryptów SQL
2. **export-scripts** - Eksport metadanych z istniejącej bazy do plików SQL
3. **update-db** - Aktualizacja istniejącej bazy na podstawie skryptów

Aplikacja obsługuje:
- **Domeny** (DOMAIN) - typy niestandardowe z walidacją
- **Tabele** (TABLE) - struktury danych z kolumnami
- **Procedury składowane** (STORED PROCEDURE) - logika biznesowa w bazie

*Uwaga: Constrainty, triggery i indeksy nie są obsługiwane w tej uproszczonej wersji.*

## 🛠️ Wymagania

- **.NET 8.0 SDK** lub nowszy - [Instrukcje instalacji](#instalacja)
- **Firebird 5.0** zainstalowany lokalnie - [Instrukcje instalacji](#instalacja)
  - Domyślny użytkownik: `SYSDBA`
  - Domyślne hasło: `masterkey`

## 📦 Instalacja

### Wymagania

1. **.NET 8.0 SDK** - https://dotnet.microsoft.com/download/dotnet/8.0
2. **Firebird 5.0 Server** - https://firebirdsql.org/en/firebird-5-0/

### Kompilacja

```bash
dotnet restore
dotnet build
```

## 🚀 Użycie

### Uruchamianie przez dotnet run

```bash
dotnet run -- <komenda> <argumenty>
```

### Uruchamianie skompilowanego exe

```bash
DbMetaTool.exe <komenda> <argumenty>
```

## 📖 Komendy

### 1. Build Database

Tworzy nową pustą bazę danych Firebird i wypełnia ją obiektami ze skryptów SQL.

**Składnia:**
```bash
DbMetaTool build-db --db-dir <katalog-bazy> --scripts-dir <katalog-skryptów>
```

**Przykład:**
```bash
dotnet run -- build-db --db-dir "C:\databases\mydb" --scripts-dir "C:\scripts"
```

**Parametry:**
- `--db-dir` - Katalog, w którym zostanie utworzona baza danych (plik `database.fdb`)
- `--scripts-dir` - Katalog zawierający pliki `.sql` ze skryptami

**Działanie:**
1. Tworzy katalog docelowy (jeśli nie istnieje)
2. Tworzy pustą bazę danych Firebird (`database.fdb`)
3. Wykonuje skrypty w kolejności: domeny → tabele → procedury
4. Raportuje postęp i błędy

---

### 2. Export Scripts

Eksportuje metadane z istniejącej bazy danych do plików SQL.

**Składnia:**
```bash
DbMetaTool export-scripts --connection-string <conn-string> --output-dir <katalog-wyjściowy>
```

**Przykład:**
```bash
dotnet run -- export-scripts --connection-string "database=C:\databases\mydb\database.fdb;user=SYSDBA;password=masterkey" --output-dir "C:\exported-scripts"
```

**Parametry:**
- `--connection-string` - Connection string do istniejącej bazy Firebird
- `--output-dir` - Katalog, do którego zostaną zapisane wygenerowane pliki

**Format connection string:**
```
database=C:\ścieżka\do\bazy.fdb;user=SYSDBA;password=masterkey;charset=UTF8
```

**Struktura wyjściowa:**
```
output-dir/
├── domains/
│   ├── 001_domain_price.sql
│   ├── 002_domain_name.sql
│   └── ...
├── tables/
│   ├── 001_table_customers.sql
│   ├── 002_table_orders.sql
│   └── ...
└── procedures/
    ├── 001_procedure_calculate_total.sql
    └── ...
```

---

### 3. Update Database

Aktualizuje istniejącą bazę danych, wykonując skrypty z katalogu.

**Składnia:**
```bash
DbMetaTool update-db --connection-string <conn-string> --scripts-dir <katalog-skryptów>
```

**Przykład:**
```bash
dotnet run -- update-db --connection-string "database=C:\databases\mydb\database.fdb;user=SYSDBA;password=masterkey" --scripts-dir "C:\scripts"
```

**Parametry:**
- `--connection-string` - Connection string do istniejącej bazy Firebird
- `--scripts-dir` - Katalog zawierający pliki `.sql` ze skryptami

**Uwaga:** Obecna implementacja wykonuje wszystkie skrypty. W wersji produkcyjnej powinna porównywać metadane i generować tylko potrzebne zmiany (ALTER statements).

## 🏗️ Architektura

Projekt wykorzystuje **Clean Architecture** z podziałem na warstwy:

```
DbMetaTool/
├── Domain/                    # Warstwa domenowa (modele + interfejsy)
│   ├── Models/               
│   │   ├── Domain.cs         # Model domeny
│   │   ├── Table.cs          # Model tabeli
│   │   ├── Column.cs         # Model kolumny
│   │   └── StoredProcedure.cs
│   └── Interfaces/
│       ├── IMetadataReader.cs
│       ├── IScriptGenerator.cs
│       ├── IScriptExecutor.cs
│       └── IDatabaseCreator.cs
│
├── Infrastructure/            # Warstwa infrastruktury (Firebird)
│   ├── FirebirdMetadataReader.cs
│   ├── FirebirdDatabaseCreator.cs
│   ├── FirebirdScriptExecutor.cs
│   └── SqlScriptGenerator.cs
│
├── Application/              # Warstwa aplikacji (serwisy)
│   ├── Services/
│   │   ├── DatabaseBuildService.cs
│   │   ├── MetadataExportService.cs
│   │   └── DatabaseUpdateService.cs
│   └── Validators/
│       └── ParameterValidator.cs
│
├── Common/                   # Utilities i wyjątki
│   └── Exceptions/
│       ├── DatabaseOperationException.cs
│       └── ValidationException.cs
│
└── Program.cs                # Entry point (CLI)
```

### Zasady projektowe (SOLID)

- **Single Responsibility** - Każda klasa ma jedną odpowiedzialność
- **Open/Closed** - Rozszerzalność przez interfejsy
- **Liskov Substitution** - Poprawna hierarchia dziedziczenia
- **Interface Segregation** - Dedykowane, wąskie interfejsy
- **Dependency Inversion** - Zależności skierowane na abstrakcje

## 🧪 Testowanie

Uruchom skrypt testowy:

```powershell
powershell -ExecutionPolicy Bypass -File .\TEST.ps1
```

Skrypt automatycznie weryfikuje poprawność działania aplikacji.

## 📝 Znane ograniczenia

### Obecna wersja

- **Constraints** (PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK na tabelach) - nie są obsługiwane
- **Indeksy** - nie są obsługiwane
- **Triggery** - nie są obsługiwane
- **Generators/Sequences** - nie są obsługiwane
- **Views** - nie są obsługiwane
- **Update Database** - wykonuje wszystkie skrypty zamiast tylko różnice

### Poprawka dla UTF8

**Problem rozwiązany:** Firebird przechowuje długości VARCHAR w **bajtach**, nie znakach.

**Rozwiązanie zaimplementowane:**
```csharp
// UTF8 używa 4 bajtów na znak
var bytesPerChar = charSetId == 4 ? 4 : 1;
var charLength = fieldLength / bytesPerChar;
```

**Efekt:** `VARCHAR(100)` jest eksportowany prawidłowo (nie jako `VARCHAR(400)`).

### Potencjalne rozszerzenia

- Wsparcie dla constraints i indeksów
- Inteligentne porównywanie metadanych w `update-db`
- Generowanie ALTER statements zamiast pełnego odtworzenia
- Wsparcie dla transakcji długotrwałych
- Backup/restore przed update-db
- Szczegółowe logowanie do pliku
- Obsługa wielu dialektów SQL

## 🔧 Zależności

- **FirebirdSql.Data.FirebirdClient** (10.3.1) - ADO.NET provider dla Firebird
- **Microsoft.Extensions.DependencyInjection** (8.0.1) - Kontener DI
- **Serilog** (4.1.0) - Strukturalne logowanie

## 🆘 Rozwiązywanie problemów

### "Database file already exists"
- Usuń istniejący plik `.fdb` z katalogu docelowego

### "Connection string must contain database path"
- Upewnij się, że connection string zawiera parametr `database=...`

### "Directory does not exist"
- Sprawdź poprawność ścieżek, użyj pełnych ścieżek bezwzględnych

### "Failed to execute script"
- Sprawdź składnię SQL w plikach skryptów
- Upewnij się, że Firebird Server jest uruchomiony
- Sprawdź uprawnienia użytkownika SYSDBA

### "Unable to load DLL 'fbembed'"
- Użyj `DataSource=localhost;Port=3050` zamiast `ServerType=1`

## 📄 Licencja

Projekt utworzony jako zadanie rekrutacyjne.

## 👨‍💻 Autor

Implementacja wykorzystuje AI (GitHub Copilot) zgodnie z wymaganiami zadania.
