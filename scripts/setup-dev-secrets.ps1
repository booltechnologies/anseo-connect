# PowerShell script to set up user secrets for local development
# This script prompts for values and sets them as user secrets

param(
    [Parameter(Mandatory=$false)]
    [switch]$SkipPrompts
)

Write-Host "Setting up development user secrets..." -ForegroundColor Green
Write-Host ""

# Helper function to prompt for value
function Get-SecretValue {
    param(
        [string]$Prompt,
        [string]$CurrentValue = "",
        [switch]$IsPassword = $false
    )
    
    if ($SkipPrompts -and $CurrentValue) {
        return $CurrentValue
    }
    
    if ($IsPassword) {
        $secure = Read-Host -Prompt $Prompt -AsSecureString
        $ptr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        return [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    } else {
        return Read-Host -Prompt $Prompt
    }
}

# Get common values
$sqlConnection = Get-SecretValue -Prompt "SQL Connection String (e.g., Server=localhost;Database=AnseoConnect;Integrated Security=true;)" -CurrentValue ""
$serviceBusConnection = Get-SecretValue -Prompt "Service Bus Connection String" -CurrentValue ""

Write-Host ""
Write-Host "API Gateway Configuration..." -ForegroundColor Cyan

# API Gateway
$apiGatewayPath = "src/Services/AnseoConnect.ApiGateway"
if (Test-Path $apiGatewayPath) {
    Write-Host "  Setting secrets for ApiGateway..." -ForegroundColor Yellow
    
    dotnet user-secrets init --project $apiGatewayPath | Out-Null
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $sqlConnection --project $apiGatewayPath
    dotnet user-secrets set "ConnectionStrings:ServiceBus" $serviceBusConnection --project $apiGatewayPath
    
    # Generate JWT secret
    $jwtBytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtBytes)
    $jwtSecret = [Convert]::ToBase64String($jwtBytes)
    
    dotnet user-secrets set "Jwt:Secret" $jwtSecret --project $apiGatewayPath
    dotnet user-secrets set "Jwt:Issuer" "AnseoConnect" --project $apiGatewayPath
    dotnet user-secrets set "Jwt:Audience" "AnseoConnect" --project $apiGatewayPath
    
    Write-Host "  ✓ ApiGateway secrets configured" -ForegroundColor Green
    Write-Host "    JWT Secret: $jwtSecret" -ForegroundColor Gray
} else {
    Write-Host "  ✗ ApiGateway project not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Ingestion.Wonde Configuration..." -ForegroundColor Cyan

# Ingestion.Wonde
$ingestionPath = "src/Services/AnseoConnect.Ingestion.Wonde"
if (Test-Path $ingestionPath) {
    Write-Host "  Setting secrets for Ingestion.Wonde..." -ForegroundColor Yellow
    
    dotnet user-secrets init --project $ingestionPath | Out-Null
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $sqlConnection --project $ingestionPath
    dotnet user-secrets set "ConnectionStrings:ServiceBus" $serviceBusConnection --project $ingestionPath
    
    $wondeToken = Get-SecretValue -Prompt "  Wonde API Token" -IsPassword
    $wondeDomain = Get-SecretValue -Prompt "  Wonde Domain (default: api.wonde.com)" -CurrentValue "api.wonde.com"
    
    dotnet user-secrets set "Wonde:ApiToken" $wondeToken --project $ingestionPath
    dotnet user-secrets set "Wonde:DefaultDomain" $wondeDomain --project $ingestionPath
    
    Write-Host "  ✓ Ingestion.Wonde secrets configured" -ForegroundColor Green
} else {
    Write-Host "  ✗ Ingestion.Wonde project not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Workflow Configuration..." -ForegroundColor Cyan

# Workflow
$workflowPath = "src/Services/AnseoConnect.Workflow"
if (Test-Path $workflowPath) {
    Write-Host "  Setting secrets for Workflow..." -ForegroundColor Yellow
    
    dotnet user-secrets init --project $workflowPath | Out-Null
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $sqlConnection --project $workflowPath
    dotnet user-secrets set "ConnectionStrings:ServiceBus" $serviceBusConnection --project $workflowPath
    
    Write-Host "  ✓ Workflow secrets configured" -ForegroundColor Green
} else {
    Write-Host "  ✗ Workflow project not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "Comms Configuration..." -ForegroundColor Cyan

# Comms
$commsPath = "src/Services/AnseoConnect.Comms"
if (Test-Path $commsPath) {
    Write-Host "  Setting secrets for Comms..." -ForegroundColor Yellow
    
    dotnet user-secrets init --project $commsPath | Out-Null
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $sqlConnection --project $commsPath
    dotnet user-secrets set "ConnectionStrings:ServiceBus" $serviceBusConnection --project $commsPath
    
    # Sendmode SMS configuration
    Write-Host "  Sendmode SMS Configuration (optional - leave empty to skip):" -ForegroundColor Yellow
    $sendmodeAuthMethod = Get-SecretValue -Prompt "    Sendmode auth method (API_KEY/USERNAME) [default: API_KEY]" -CurrentValue "API_KEY"
    
    if ($sendmodeAuthMethod -eq "API_KEY" -or [string]::IsNullOrWhiteSpace($sendmodeAuthMethod)) {
        $sendmodeApiKey = Get-SecretValue -Prompt "    Sendmode API Key" -IsPassword
        if (![string]::IsNullOrWhiteSpace($sendmodeApiKey)) {
            dotnet user-secrets set "Sendmode:ApiKey" $sendmodeApiKey --project $commsPath
        }
    } else {
        $sendmodeUsername = Get-SecretValue -Prompt "    Sendmode Username"
        $sendmodePassword = Get-SecretValue -Prompt "    Sendmode Password" -IsPassword
        if (![string]::IsNullOrWhiteSpace($sendmodeUsername)) {
            dotnet user-secrets set "Sendmode:Username" $sendmodeUsername --project $commsPath
        }
        if (![string]::IsNullOrWhiteSpace($sendmodePassword)) {
            dotnet user-secrets set "Sendmode:Password" $sendmodePassword --project $commsPath
        }
    }
    
    $sendmodeApiUrl = Get-SecretValue -Prompt "    Sendmode API URL (default: https://api.sendmode.com/v2/sendSMS)" -CurrentValue "https://api.sendmode.com/v2/sendSMS"
    if (![string]::IsNullOrWhiteSpace($sendmodeApiUrl)) {
        dotnet user-secrets set "Sendmode:ApiUrl" $sendmodeApiUrl --project $commsPath
    }
    
    $sendmodeFromNumber = Get-SecretValue -Prompt "    Sendmode From Number (E.164 format, optional)" -CurrentValue ""
    if (![string]::IsNullOrWhiteSpace($sendmodeFromNumber)) {
        dotnet user-secrets set "Sendmode:FromNumber" $sendmodeFromNumber --project $commsPath
    }
    
    # SendGrid Email configuration (optional)
    Write-Host "  SendGrid Email Configuration (optional - leave empty to skip):" -ForegroundColor Yellow
    $sendGridApiKey = Get-SecretValue -Prompt "    SendGrid API Key" -IsPassword
    if (![string]::IsNullOrWhiteSpace($sendGridApiKey)) {
        dotnet user-secrets set "SendGrid:ApiKey" $sendGridApiKey --project $commsPath
        
        $sendGridFromEmail = Get-SecretValue -Prompt "    SendGrid From Email (verified sender)" -CurrentValue ""
        if (![string]::IsNullOrWhiteSpace($sendGridFromEmail)) {
            dotnet user-secrets set "SendGrid:FromEmail" $sendGridFromEmail --project $commsPath
        }
        
        $sendGridFromName = Get-SecretValue -Prompt "    SendGrid From Name (default: Anseo Connect)" -CurrentValue "Anseo Connect"
        dotnet user-secrets set "SendGrid:FromName" $sendGridFromName --project $commsPath
    }
    
    Write-Host "  ✓ Comms secrets configured" -ForegroundColor Green
} else {
    Write-Host "  ✗ Comms project not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "✓ Development secrets setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run migrations: dotnet ef database update --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext" -ForegroundColor Yellow
Write-Host "  2. Start services: dotnet run --project src/Services/AnseoConnect.ApiGateway" -ForegroundColor Yellow
Write-Host ""
