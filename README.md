# Firebird Metadata Generator

Narzędzie konsolowe .NET 8.0 do zarządzania metadanymi bazy danych Firebird 5.0.

## 🎯 Funkcjonalności

Aplikacja obsługuje trzy główne operacje:

1. **build-db** - Budowa nowej bazy danych ze skryptów SQL
2. **export-scripts** - Eksport metadanych z istniejącej bazy do plików SQL
3. **update-db** - Aktualizacja istniejącej bazy na podstawie skryptów (synchronizacja)

Aplikacja obsługuje:
- **Domeny** (DOMAIN) - typy niestandardowe z walidacją
- **Tabele** (TABLE) - struktury danych z kolumnami
- **Procedury składowane** (STORED PROCEDURE) - logika biznesowa w bazie

*Uwaga: Constrainty, triggery i indeksy nie są obsługiwane w tej uproszczonej wersji.*

## 🛠️ Wymagania

- **.NET 8.0 SDK** lub nowszy - [Instrukcje instalacji](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Firebird 5.0** zainstalowany lokalnie - [Instrukcje instalacji](https://firebirdsql.org/en/firebird-5-0/)
  - Domyślny użytkownik: `SYSDBA`
  - Domyślne hasło: `masterkey`

## 📦 Instalacja i Kompilacja

```bash
dotnet restore DbMetaTool.csproj
dotnet build DbMetaTool.csproj
```

## 🚀 Użycie

Aplikację można uruchamiać bezpośrednio przez `dotnet run` (wskazując plik projektu) lub używając skompilowanego pliku `.exe`.

**Ważne:** Ze względu na strukturę projektu, przy używaniu `dotnet run` należy zawsze wskazywać plik projektu flagą `--project`.

### 1. Build Database (Zbuduj bazę)

Tworzy nową pustą bazę danych Firebird i wypełnia ją obiektami ze skryptów SQL.
Rozwiązuje zależności cykliczne między procedurami.

**Składnia:**
```bash
dotnet run --project DbMetaTool.csproj -- build-db --db-dir <katalog-bazy> --scripts-dir <katalog-skryptów>
```

**Przykład:**
```bash
dotnet run --project DbMetaTool.csproj -- build-db --db-dir "C:\db" --scripts-dir "C:\scripts"
```

### 2. Export Scripts (Wygeneruj skrypty)

Eksportuje metadane z istniejącej bazy danych do plików SQL w strukturze katalogów (`domains/`, `tables/`, `procedures/`).

**Składnia:**
```bash
dotnet run --project DbMetaTool.csproj -- export-scripts --connection-string <conn-string> --output-dir <katalog-wyjściowy>
```

**Przykład:**
```bash
dotnet run --project DbMetaTool.csproj -- export-scripts --connection-string "User ID=SYSDBA;Password=masterkey;Database=C:\db\database.fdb;DataSource=localhost;Port=3050;Dialect=3;charset=UTF8" --output-dir "C:\export"
```

### 3. Update Database (Zaktualizuj bazę)

Synchronizuje istniejącą bazę danych ze stanem opisanym w plikach skryptów.
- Usuwa nadmiarowe tabele i procedury
- Dodaje brakujące tabele i domeny
- Aktualizuje kolumny w tabelach (dodaje/usuwa)
- Aktualizuje procedury (rozwiązując zależności cykliczne)

**Składnia:**
```bash
dotnet run --project DbMetaTool.csproj -- update-db --connection-string <conn-string> --scripts-dir <katalog-skryptów>
```

**Przykład:**
```bash
dotnet run --project DbMetaTool.csproj -- update-db --connection-string "User ID=SYSDBA;Password=masterkey;Database=C:\db\database.fdb;DataSource=localhost;Port=3050;Dialect=3;charset=UTF8" --scripts-dir "C:\scripts"
```

## 🏗️ Architektura

Projekt wykorzystuje **Clean Architecture** z podziałem na warstwy:

```
DbMetaTool/
├── Domain/                    # Warstwa domenowa (modele + interfejsy)
├── Infrastructure/            # Warstwa infrastruktury (Firebird)
├── Application/               # Warstwa aplikacji (serwisy)
├── Common/                    # Utilities i wyjątki
└── Program.cs                 # Entry point (CLI)
```

### Kluczowe rozwiązania techniczne

- **Dependency Injection** - Zarządzanie zależnościami
- **Serilog** - Strukturalne logowanie operacji
- **Obsługa UTF8** - Poprawne przeliczanie długości pól tekstowych (bajty vs znaki)
- **Circular Dependency Handling** - Dwufazowe wgrywanie procedur (Stub Pass -> Full Pass)
- **Inteligentna synchronizacja** - Porównywanie metadanych zamiast ślepego wgrywania skryptów

## 🔧 Zależności

- **FirebirdSql.Data.FirebirdClient** (10.3.1) - ADO.NET provider dla Firebird
- **Microsoft.Extensions.DependencyInjection** (8.0.1) - Kontener DI
- **Serilog** (4.1.0) - Strukturalne logowanie

## 👨‍💻 Autor

Implementacja wykorzystuje AI (GitHub Copilot) zgodnie z wymaganiami zadania.
