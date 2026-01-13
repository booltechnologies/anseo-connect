# PowerShell script to create Azure Service Bus topics and subscriptions
# Prerequisites: Azure CLI installed and logged in (az login)

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$ServiceBusNamespace,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "northeurope",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreateNamespaceIfMissing
)

Write-Host "Creating Service Bus topics and subscriptions..." -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Cyan
Write-Host "Namespace: $ServiceBusNamespace" -ForegroundColor Cyan
Write-Host "Location: $Location" -ForegroundColor Cyan
Write-Host ""

# Verify namespace exists
Write-Host "Verifying namespace exists..." -ForegroundColor Yellow
$namespaceExists = az servicebus namespace show --resource-group $ResourceGroupName --name $ServiceBusNamespace --query "name" --output tsv 2>$null

if (-not $namespaceExists) {
    Write-Host "Namespace '$ServiceBusNamespace' not found in resource group '$ResourceGroupName'" -ForegroundColor Yellow
    
    if ($CreateNamespaceIfMissing) {
        Write-Host "Creating namespace '$ServiceBusNamespace'..." -ForegroundColor Yellow
        
        # Verify resource group exists
        $rgExists = az group show --name $ResourceGroupName --query "name" --output tsv 2>$null
        if (-not $rgExists) {
            Write-Host "ERROR: Resource group '$ResourceGroupName' not found." -ForegroundColor Red
            Write-Host "Please create the resource group first:" -ForegroundColor Yellow
            Write-Host "  az group create --name $ResourceGroupName --location $Location" -ForegroundColor Gray
            exit 1
        }
        
        # Create the namespace
        az servicebus namespace create `
            --resource-group $ResourceGroupName `
            --name $ServiceBusNamespace `
            --location $Location `
            --sku Standard `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Namespace '$ServiceBusNamespace' created successfully" -ForegroundColor Green
            Write-Host "  Waiting for namespace to be ready..." -ForegroundColor Yellow
            Start-Sleep -Seconds 10
        } else {
            Write-Host "✗ Failed to create namespace '$ServiceBusNamespace'" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "ERROR: Namespace '$ServiceBusNamespace' not found in resource group '$ResourceGroupName'" -ForegroundColor Red
        Write-Host ""
        Write-Host "Options:" -ForegroundColor Yellow
        Write-Host "  1. Create the namespace manually:" -ForegroundColor Yellow
        Write-Host "     az servicebus namespace create --resource-group $ResourceGroupName --name $ServiceBusNamespace --location $Location --sku Standard" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  2. Or run this script with -CreateNamespaceIfMissing to auto-create:" -ForegroundColor Yellow
        Write-Host "     .\scripts\create-servicebus-topics.ps1 -ResourceGroupName $ResourceGroupName -ServiceBusNamespace $ServiceBusNamespace -CreateNamespaceIfMissing" -ForegroundColor Gray
        exit 1
    }
} else {
    Write-Host "✓ Namespace found" -ForegroundColor Green
}

Write-Host ""

# Create topics
$topics = @("attendance", "comms", "workflow")

foreach ($topic in $topics) {
    Write-Host "Creating topic: $topic..." -ForegroundColor Yellow
    
    $existing = az servicebus topic show `
        --resource-group $ResourceGroupName `
        --namespace-name $ServiceBusNamespace `
        --name $topic `
        --query "name" `
        --output tsv 2>$null
    
    if ($existing) {
        Write-Host "  Topic '$topic' already exists, skipping..." -ForegroundColor Yellow
    } else {
        az servicebus topic create `
            --resource-group $ResourceGroupName `
            --namespace-name $ServiceBusNamespace `
            --name $topic `
            --default-message-time-to-live P7D `
            --enable-duplicate-detection true `
            --duplicate-detection-history-time-window PT10M `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Topic '$topic' created" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Failed to create topic '$topic'" -ForegroundColor Red
            exit 1
        }
    }
}

Write-Host ""

# Create subscriptions for 'attendance' topic
Write-Host "Creating subscriptions for 'attendance' topic..." -ForegroundColor Yellow

$subName = "workflow-attendance-ingested"
$existing = az servicebus topic subscription show `
    --resource-group $ResourceGroupName `
    --namespace-name $ServiceBusNamespace `
    --topic-name attendance `
    --name $subName `
    --query "name" `
    --output tsv 2>$null

if ($existing) {
    Write-Host "  Subscription '$subName' already exists, skipping..." -ForegroundColor Yellow
} else {
    az servicebus topic subscription create `
        --resource-group $ResourceGroupName `
        --namespace-name $ServiceBusNamespace `
        --topic-name attendance `
        --name $subName `
        --lock-duration PT1M `
        --max-delivery-count 10 `
        --output none
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Subscription '$subName' created" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Failed to create subscription '$subName'" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Create subscriptions for 'comms' topic
Write-Host "Creating subscriptions for 'comms' topic..." -ForegroundColor Yellow

$subscriptions = @("comms-send-message", "workflow-message-events")

foreach ($subName in $subscriptions) {
    $existing = az servicebus topic subscription show `
        --resource-group $ResourceGroupName `
        --namespace-name $ServiceBusNamespace `
        --topic-name comms `
        --name $subName `
        --query "name" `
        --output tsv 2>$null

    if ($existing) {
        Write-Host "  Subscription '$subName' already exists, skipping..." -ForegroundColor Yellow
    } else {
        az servicebus topic subscription create `
            --resource-group $ResourceGroupName `
            --namespace-name $ServiceBusNamespace `
            --topic-name comms `
            --name $subName `
            --lock-duration PT1M `
            --max-delivery-count 10 `
            --output none
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Subscription '$subName' created" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Failed to create subscription '$subName'" -ForegroundColor Red
            exit 1
        }
    }
}

Write-Host ""

# Get connection string
Write-Host "Retrieving connection string..." -ForegroundColor Yellow
$connectionString = az servicebus namespace authorization-rule keys list `
    --resource-group $ResourceGroupName `
    --namespace-name $ServiceBusNamespace `
    --name RootManageSharedAccessKey `
    --query primaryConnectionString `
    --output tsv

if ($connectionString) {
    Write-Host ""
    Write-Host "✓ Service Bus setup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Connection String (save this for environment variables):" -ForegroundColor Cyan
    Write-Host $connectionString -ForegroundColor White
    Write-Host ""
    Write-Host "Use this as ANSEO_SERVICEBUS environment variable in all services." -ForegroundColor Yellow
} else {
    Write-Host "✗ Failed to retrieve connection string" -ForegroundColor Red
    Write-Host "You can get it manually from Azure Portal:" -ForegroundColor Yellow
    Write-Host "  Service Bus Namespace → Shared access policies → RootManageSharedAccessKey → Primary connection string" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Verification:" -ForegroundColor Cyan
az servicebus topic list --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --output table
Write-Host ""
Write-Host "Subscriptions under 'attendance' topic:" -ForegroundColor Cyan
az servicebus topic subscription list --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --topic-name attendance --output table
Write-Host ""
Write-Host "Subscriptions under 'comms' topic:" -ForegroundColor Cyan
az servicebus topic subscription list --resource-group $ResourceGroupName --namespace-name $ServiceBusNamespace --topic-name comms --output table
