# PowerShell script to apply database migrations
# Prerequisites: Valid SQL connection string configured
#
# Configuration Options:
# 1. Configuration file (recommended for development):
#    Add to src/Services/AnseoConnect.ApiGateway/appsettings.Development.json:
#    {
#      "ConnectionStrings": {
#        "DefaultConnection": "Server=SERVER;Database=DATABASE;User Id=USER;Password=PASSWORD;TrustServerCertificate=True;"
#      }
#    }
#
# 2. Environment variable (recommended for CI/CD/production):
#    Set environment variable: $env:ANSEO_SQL = "Server=SERVER;Database=DATABASE;User Id=USER;Password=PASSWORD;TrustServerCertificate=True;"
#    Or in PowerShell: [Environment]::SetEnvironmentVariable("ANSEO_SQL", "YOUR_CONNECTION_STRING", "User")
#
# The script uses 'dotnet ef' commands which read from the startup project's configuration.
# It will check appsettings.Development.json first, then fall back to ANSEO_SQL environment variable.

param(
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

Write-Host "Database Migration Script" -ForegroundColor Green
Write-Host ""

$projectPath = "src/Shared/AnseoConnect.Data"
$startupProjectPath = "src/Services/AnseoConnect.ApiGateway"
$contextName = "AnseoConnectDbContext"

# Verify projects exist
if (-not (Test-Path $projectPath)) {
    Write-Host "ERROR: Data project not found at $projectPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $startupProjectPath)) {
    Write-Host "ERROR: Startup project not found at $startupProjectPath" -ForegroundColor Red
    exit 1
}

# List migrations first
Write-Host "Checking available migrations..." -ForegroundColor Yellow
$migrations = dotnet ef migrations list `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $contextName 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to list migrations" -ForegroundColor Red
    Write-Host $migrations -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Available migrations:" -ForegroundColor Cyan
$migrations | Where-Object { $_ -match "^\d" } | ForEach-Object {
    Write-Host "  - $_" -ForegroundColor White
}

Write-Host ""

# Check for pending migrations
$pendingCount = ($migrations | Where-Object { $_ -match "Pending" }).Count

if ($pendingCount -eq 0) {
    Write-Host "No pending migrations found. Database is up to date." -ForegroundColor Green
    exit 0
}

Write-Host "Found $pendingCount pending migration(s)." -ForegroundColor Yellow
Write-Host ""

if ($WhatIf) {
    Write-Host "WHAT IF: Would apply the following migrations:" -ForegroundColor Cyan
    $migrations | Where-Object { $_ -match "Pending" } | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor White
    }
    Write-Host ""
    Write-Host "To actually apply migrations, run without -WhatIf flag." -ForegroundColor Yellow
    exit 0
}

# Confirm before applying
$confirm = Read-Host "Do you want to apply pending migrations? (y/N)"
if ($confirm -ne "y" -and $confirm -ne "Y") {
    Write-Host "Migration cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Applying migrations..." -ForegroundColor Yellow

# Apply migrations
$result = dotnet ef database update `
    --project $projectPath `
    --startup-project $startupProjectPath `
    --context $contextName 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Migrations applied successfully!" -ForegroundColor Green
    
    # Verify
    Write-Host ""
    Write-Host "Verifying migration status..." -ForegroundColor Yellow
    $finalStatus = dotnet ef migrations list `
        --project $projectPath `
        --startup-project $startupProjectPath `
        --context $contextName 2>&1
    
    Write-Host $finalStatus -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "✗ Migration failed!" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Migration complete. Verify your database has all required tables." -ForegroundColor Green
