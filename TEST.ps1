# Automated test script for Firebird Metadata Generator
param([string]$TestDir = "C:\temp\auto_test")

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "  FIREBIRD METADATA GENERATOR - AUTOMATED FUNCTIONAL TEST" -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""

# Cleanup and prepare
Write-Host "[STEP 0] Preparing test environment..." -ForegroundColor Yellow
if (Test-Path $TestDir) { Remove-Item -Path $TestDir -Recurse -Force }
New-Item -Path $TestDir -ItemType Directory -Force | Out-Null
New-Item -Path "$TestDir\input_scripts" -ItemType Directory -Force | Out-Null
Write-Host "  OK: Created test directory: $TestDir" -ForegroundColor Green

# Create test SQL scripts
$script1 = 'CREATE DOMAIN EMAIL AS VARCHAR(100) CHECK (VALUE LIKE ''%@%'');'
$script1 | Out-File "$TestDir\input_scripts\001_domain_email.sql" -Encoding UTF8

$script2 = 'CREATE DOMAIN PRICE AS DECIMAL(10,2) CHECK (VALUE >= 0);'
$script2 | Out-File "$TestDir\input_scripts\002_domain_price.sql" -Encoding UTF8

$script3 = @'
CREATE TABLE USERS (
    ID INTEGER NOT NULL,
    USERNAME VARCHAR(50) NOT NULL,
    EMAIL EMAIL NOT NULL,
    CREATED_DATE TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
'@
$script3 | Out-File "$TestDir\input_scripts\003_table_users.sql" -Encoding UTF8

$script4 = @'
CREATE TABLE PRODUCTS (
    ID INTEGER NOT NULL,
    NAME VARCHAR(100) NOT NULL,
    PRICE PRICE NOT NULL
);
'@
$script4 | Out-File "$TestDir\input_scripts\004_table_products.sql" -Encoding UTF8

$script5 = @'
SET TERM ^ ;
CREATE PROCEDURE GET_USER(USER_ID INTEGER)
RETURNS (USERNAME VARCHAR(50), USER_EMAIL VARCHAR(100))
AS BEGIN
    SELECT USERNAME, EMAIL FROM USERS WHERE ID = :USER_ID
    INTO :USERNAME, :USER_EMAIL;
    SUSPEND;
END^
SET TERM ; ^
'@
$script5 | Out-File "$TestDir\input_scripts\005_procedure_get_user.sql" -Encoding UTF8

Write-Host "  OK: Created 5 test SQL scripts" -ForegroundColor Green
Write-Host ""

# TEST 1: BUILD DATABASE
Write-Host "[STEP 1] BUILD DATABASE - Creating database from scripts..." -ForegroundColor Yellow
& dotnet run -- build-db --db-dir "$TestDir\db_original" --scripts-dir "$TestDir\input_scripts" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Database build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  OK: Database created successfully" -ForegroundColor Green
Write-Host ""

# TEST 2: EXPORT SCRIPTS
Write-Host "[STEP 2] EXPORT SCRIPTS - Exporting metadata from database..." -ForegroundColor Yellow
$connStr = "User=SYSDBA;Password=masterkey;Database=$TestDir\db_original\database.fdb;DataSource=localhost;Port=3050;Charset=UTF8"
& dotnet run -- export-scripts --connection-string $connStr --output-dir "$TestDir\exported_scripts" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Script export failed!" -ForegroundColor Red
    exit 1
}
$exportedFiles = Get-ChildItem "$TestDir\exported_scripts" -Recurse -File
Write-Host "  OK: Exported $($exportedFiles.Count) SQL files" -ForegroundColor Green
Write-Host ""

# TEST 3: REBUILD DATABASE
Write-Host "[STEP 3] BUILD FROM EXPORTED - Creating new database from exported scripts..." -ForegroundColor Yellow
& dotnet run -- build-db --db-dir "$TestDir\db_rebuilt" --scripts-dir "$TestDir\exported_scripts" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Database rebuild failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  OK: New database created from exported scripts" -ForegroundColor Green
Write-Host ""

# TEST 4: VERIFY IDENTITY
Write-Host "[STEP 4] VERIFY - Checking structural identity..." -ForegroundColor Yellow
$connStr2 = "User=SYSDBA;Password=masterkey;Database=$TestDir\db_rebuilt\database.fdb;DataSource=localhost;Port=3050;Charset=UTF8"
& dotnet run -- export-scripts --connection-string $connStr2 --output-dir "$TestDir\verification_scripts" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Verification export failed!" -ForegroundColor Red
    exit 1
}

# Compare files
$differences = 0
Get-ChildItem "$TestDir\exported_scripts" -Recurse -File | ForEach-Object {
    $original = $_.FullName
    $rebuilt = $original.Replace("exported_scripts", "verification_scripts")
    if (Test-Path $rebuilt) {
        $diff = Compare-Object (Get-Content $original) (Get-Content $rebuilt)
        if ($diff) { $differences++ }
    } else {
        $differences++
    }
}

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Cyan
if ($differences -eq 0) {
    Write-Host "                   TEST PASSED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  All files are identical." -ForegroundColor White
    Write-Host "  Both databases have IDENTICAL structure." -ForegroundColor White
} else {
    Write-Host "                      TEST FAILED!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Found $differences differences between databases." -ForegroundColor Red
}
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test directory: $TestDir" -ForegroundColor Gray
Write-Host ""

if ($differences -eq 0) { exit 0 } else { exit 1 }
