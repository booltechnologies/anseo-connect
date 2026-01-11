# Deployment Scripts

This directory contains PowerShell scripts to automate deployment steps for Anseo Connect v0.1.

## Prerequisites

- PowerShell 7.0+ (for local development scripts)
- Azure CLI installed and logged in (`az login`) - for Service Bus scripts
- .NET SDK 10.0+ installed
- Valid Azure subscription (for Service Bus setup)

## Scripts

### 1. `apply-migrations.ps1`

Applies database migrations to your SQL Server database.

**Usage:**
```powershell
.\scripts\apply-migrations.ps1
```

**What it does:**
- Lists available migrations
- Shows pending migrations
- Prompts for confirmation
- Applies pending migrations
- Verifies migration status

**Prerequisites:**
- Valid SQL connection string configured in user secrets or environment variable
- Database server accessible
- User has permissions to create tables

**Example:**
```powershell
cd E:\Development\AnseoConnect
.\scripts\apply-migrations.ps1
```

**WhatIf mode:**
```powershell
.\scripts\apply-migrations.ps1 -WhatIf
```

---

### 2. `setup-dev-secrets.ps1`

Sets up user secrets for local development across all services.

**Usage:**
```powershell
.\scripts\setup-dev-secrets.ps1
```

**What it does:**
- Prompts for SQL connection string
- Prompts for Service Bus connection string
- Generates JWT secret automatically
- Prompts for service-specific secrets (Wonde token, Twilio credentials)
- Sets all secrets using `dotnet user-secrets` for each service

**Prerequisites:**
- None (prompts for all values)

**Example:**
```powershell
cd E:\Development\AnseoConnect
.\scripts\setup-dev-secrets.ps1
```

**Non-interactive mode (for CI/CD):**
```powershell
# Not supported yet - script requires interactive prompts
# Use environment variables instead for CI/CD
```

**What it sets:**
- **ApiGateway**: SQL, Service Bus, JWT secret/issuer/audience
- **Ingestion.Wonde**: SQL, Service Bus, Wonde token/domain
- **Workflow**: SQL, Service Bus
- **Comms**: SQL, Service Bus, Twilio credentials

---

### 3. `create-servicebus-topics.ps1`

Creates Azure Service Bus topics and subscriptions.

**Usage:**
```powershell
.\scripts\create-servicebus-topics.ps1 -ResourceGroupName <rg-name> -ServiceBusNamespace <ns-name>
```

**Parameters:**
- `-ResourceGroupName` (Required): Azure resource group name
- `-ServiceBusNamespace` (Required): Service Bus namespace name
- `-Location` (Optional): Azure region (default: "northeurope")

**What it does:**
- Verifies namespace exists
- Creates topics: `attendance`, `comms`, `workflow`
- Creates subscriptions:
  - `workflow-attendance-ingested` on `attendance` topic
  - `comms-send-message` on `comms` topic
  - `workflow-message-events` on `comms` topic
- Retrieves and displays connection string
- Verifies all resources were created

**Prerequisites:**
- Azure CLI installed (`az --version` to check)
- Logged in to Azure (`az login`)
- Appropriate permissions (Contributor or Service Bus Data Owner)
- Service Bus namespace already created

**Example:**
```powershell
cd E:\Development\AnseoConnect
.\scripts\create-servicebus-topics.ps1 -ResourceGroupName "anseo-connect-rg" -ServiceBusNamespace "anseo-connect-sb"
```

**Output:**
- Lists all created topics
- Lists all created subscriptions
- Displays connection string (save this for environment variables)

---

## Quick Start Guide

### Step 1: Apply Migrations

```powershell
cd E:\Development\AnseoConnect

# First, ensure you have a SQL connection string configured
# Option A: Set in appsettings.json
# Option B: Use user secrets (see Step 2)

# List migrations
dotnet ef migrations list --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext

# Apply migrations
.\scripts\apply-migrations.ps1

# Or manually:
dotnet ef database update --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext
```

### Step 2: Configure Development Secrets

```powershell
cd E:\Development\AnseoConnect

# Run interactive setup
.\scripts\setup-dev-secrets.ps1

# This will prompt for:
# - SQL Connection String
# - Service Bus Connection String
# - Wonde API Token
# - Twilio Account SID
# - Twilio Auth Token
# - Twilio From Number
```

### Step 3: Create Service Bus Topics

```powershell
cd E:\Development\AnseoConnect

# Login to Azure first
az login

# Set variables
$resourceGroup = "your-resource-group-name"
$namespace = "your-servicebus-namespace"

# Create topics and subscriptions
.\scripts\create-servicebus-topics.ps1 -ResourceGroupName $resourceGroup -ServiceBusNamespace $namespace

# Save the connection string that's displayed
```

---

## Manual Steps (Not Automated)

Some steps require manual configuration:

### Database Setup

If you don't have a SQL Server instance:

1. **Azure SQL Database:**
   - Create in Azure Portal
   - Get connection string from Overview page
   - Update firewall rules to allow your IP

2. **Local SQL Server:**
   - Install SQL Server Express or Developer Edition
   - Enable TCP/IP in SQL Server Configuration Manager
   - Connection string: `Server=localhost;Database=AnseoConnect;Integrated Security=true;`

### Service Bus Namespace Creation

If you don't have a Service Bus namespace:

1. **Azure Portal:**
   - Create Resource → Service Bus Namespace
   - Choose pricing tier (Basic/Standard/Premium)
   - Select resource group and location

2. **Azure CLI:**
   ```bash
   az servicebus namespace create \
     --resource-group <rg-name> \
     --name <ns-name> \
     --location <location> \
     --sku Standard
   ```

### Twilio Account Setup

1. Sign up at https://www.twilio.com
2. Get Account SID and Auth Token from Dashboard
3. Get a phone number (trial or paid)
4. Configure webhook URLs in Twilio Console:
   - Delivery: `https://your-apigateway-url/webhooks/twilio/delivery`
   - Reply: `https://your-apigateway-url/webhooks/twilio/reply`

### Wonde API Token

1. Contact Wonde support or your school administrator
2. Request API token for your school(s)
3. Get school ID(s) from Wonde dashboard

---

## Troubleshooting

### Migration Errors

**"Login failed for user 'sa'"**
- Check SQL connection string
- Verify SQL Server is running
- Check firewall rules (Azure SQL)
- Verify credentials

**"Cannot open database"**
- Verify database name exists
- Check connection string database name
- Ensure user has CREATE DATABASE permission

### Service Bus Errors

**"Namespace not found"**
- Verify resource group name
- Verify namespace name
- Check you're logged in to correct Azure subscription (`az account show`)

**"Authorization failed"**
- Check you have Contributor or Service Bus Data Owner role
- Verify subscription is active
- Try creating namespace manually first

### Secret Errors

**"User secrets not found"**
- Run `dotnet user-secrets init --project <project-path>` first
- Or ensure you're in the correct directory

**"Access denied"**
- On Windows, ensure you have write permissions to user profile
- Check user secrets location: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`

---

## Next Steps

After completing Steps 1-3:

1. **Verify Setup:**
   - Test database connection
   - Test Service Bus connection (publish a test message)
   - Verify all environment variables are set

2. **Seed Initial Data:**
   - Create tenant(s) in database
   - Create school(s) with Wonde IDs
   - Create test user(s) for authentication

3. **Start Services:**
   ```powershell
   # Terminal 1: ApiGateway
   dotnet run --project src/Services/AnseoConnect.ApiGateway

   # Terminal 2: Ingestion
   dotnet run --project src/Services/AnseoConnect.Ingestion.Wonde

   # Terminal 3: Workflow
   dotnet run --project src/Services/AnseoConnect.Workflow

   # Terminal 4: Comms
   dotnet run --project src/Services/AnseoConnect.Comms
   ```

4. **Run Integration Tests:**
   - See `docs/IMPLEMENTATION_STATUS_v0.1.md` for testing checklist

---

## Support

For issues or questions:
1. Check `docs/DEPLOYMENT_GUIDE_v0.1.md` for detailed instructions
2. Review error messages carefully
3. Verify all prerequisites are met
4. Check Azure Portal for Service Bus/database status
