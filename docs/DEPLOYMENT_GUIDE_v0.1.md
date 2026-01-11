# Anseo Connect v0.1 - Deployment Guide

This guide walks through steps 1-3: Database Migrations, Environment Configuration, and Service Bus Setup.

---

## Step 1: Apply Database Migrations

### Option A: Using EF Core CLI (Recommended)

#### Prerequisites
- SQL Server instance running (local or Azure SQL)
- Connection string with appropriate permissions
- .NET SDK installed

#### Steps

1. **Update Connection String**

   Edit `src/Services/AnseoConnect.ApiGateway/appsettings.json` or set environment variable:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=<your-server>;Database=<your-database>;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
     }
   }
   ```

   Or set environment variable:
   ```bash
   $env:ANSEO_SQL="Server=<your-server>;Database=<your-database>;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
   ```

2. **Verify Migrations**

   List available migrations:
   ```bash
   dotnet ef migrations list --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext
   ```

   You should see:
   - `20260108090629_Initial`
   - `20260110122806_Step0_AddIdentity`
   - `20260110130326_Step3_AddCommsEntities`
   - `20260110131336_Step4_AddCaseEntities`

3. **Apply Migrations**

   Apply all pending migrations:
   ```bash
   dotnet ef database update --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext
   ```

4. **Verify Migration Status**

   Check which migrations have been applied:
   ```bash
   dotnet ef migrations list --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext
   ```

   All migrations should show as applied (no "Pending" status).

### Option B: Using DBMigrator Tool

1. **Configure Connection String**

   Set environment variable or update `tools/DBMigrator/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=<your-server>;Database=<your-database>;User Id=<user>;Password=<password>;TrustServerCertificate=True;"
     }
   }
   ```

2. **Run Migrator**

   ```bash
   dotnet run --project tools/DBMigrator
   ```

### Verification

After migrations are applied, verify tables were created:

```sql
USE <your-database>;
GO

-- Check core tables
SELECT name FROM sys.tables 
WHERE name IN ('Tenants', 'Schools', 'Students', 'Guardians', 'AttendanceMarks')
ORDER BY name;
GO

-- Check identity tables
SELECT name FROM sys.tables 
WHERE name LIKE 'AspNet%'
ORDER BY name;
GO

-- Check comms tables
SELECT name FROM sys.tables 
WHERE name IN ('ConsentStates', 'Messages')
ORDER BY name;
GO

-- Check workflow tables
SELECT name FROM sys.tables 
WHERE name IN ('Cases', 'CaseTimelineEvents', 'SafeguardingAlerts')
ORDER BY name;
GO
```

You should see:
- **Core**: Tenants, Schools, Students, Guardians, StudentGuardians, AttendanceMarks
- **Identity**: AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
- **Comms**: ConsentStates, Messages
- **Workflow**: Cases, CaseTimelineEvents, SafeguardingAlerts

---

## Step 2: Configure Environment Variables

### Overview

Each service requires different configuration. Use **User Secrets** for local development and **Environment Variables** for production/Azure.

### For Local Development (User Secrets)

#### ApiGateway

```bash
cd src/Services/AnseoConnect.ApiGateway
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<server>;Database=<db>;User Id=<user>;Password=<pwd>;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:ServiceBus" "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>;"
dotnet user-secrets set "Jwt:Secret" "<256-bit-secret-at-least-32-bytes-long>"
dotnet user-secrets set "Jwt:Issuer" "AnseoConnect"
dotnet user-secrets set "Jwt:Audience" "AnseoConnect"
```

**Generate JWT Secret:**
```powershell
# PowerShell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Host "JWT Secret: $secret"
```

```bash
# Bash
openssl rand -base64 32
```

#### Ingestion.Wonde

```bash
cd src/Services/AnseoConnect.Ingestion.Wonde
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<server>;Database=<db>;User Id=<user>;Password=<pwd>;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:ServiceBus" "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>;"
dotnet user-secrets set "Wonde:ApiToken" "<your-wonde-api-token>"
dotnet user-secrets set "Wonde:DefaultDomain" "api.wonde.com"
```

#### Workflow

```bash
cd src/Services/AnseoConnect.Workflow
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<server>;Database=<db>;User Id=<user>;Password=<pwd>;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:ServiceBus" "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>;"
```

#### Comms

```bash
cd src/Services/AnseoConnect.Comms
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<server>;Database=<db>;User Id=<user>;Password=<pwd>;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:ServiceBus" "Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>;"

# Sendmode SMS configuration (required for SMS sending)
# Option 1: Using API Key (recommended)
dotnet user-secrets set "Sendmode:ApiKey" "<your-sendmode-api-key>"
# Option 2: Using Username/Password
dotnet user-secrets set "Sendmode:Username" "<your-sendmode-username>"
dotnet user-secrets set "Sendmode:Password" "<your-sendmode-password>"
dotnet user-secrets set "Sendmode:ApiUrl" "https://api.sendmode.com/v2/sendSMS"  # Optional, uses default if not set
dotnet user-secrets set "Sendmode:FromNumber" "<e164-phone-number>"  # Optional

# SendGrid Email configuration (optional, for email sending)
dotnet user-secrets set "SendGrid:ApiKey" "<your-sendgrid-api-key>"
dotnet user-secrets set "SendGrid:FromEmail" "<your-verified-sender-email>"
dotnet user-secrets set "SendGrid:FromName" "Anseo Connect"  # Optional
```

### For Production (Environment Variables)

#### Azure App Service

1. **Navigate to App Service** in Azure Portal
2. **Go to Configuration** → **Application settings**
3. **Add/Edit** the following settings:

   **ApiGateway:**
   ```
   ANSEO_SQL = Server=tcp:<server>.database.windows.net,1433;Database=<db>;User ID=<user>;Password=<pwd>;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
   ANSEO_SERVICEBUS = Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>;
   ANSEO_JWT_SECRET = <256-bit-secret>
   ```

   **Ingestion.Wonde:**
   ```
   ANSEO_SQL = <same-as-above>
   ANSEO_SERVICEBUS = <same-as-above>
   WONDE_API_TOKEN = <your-wonde-token>
   ```

   **Workflow:**
   ```
   ANSEO_SQL = <same-as-above>
   ANSEO_SERVICEBUS = <same-as-above>
   ```

   **Comms:**
   ```
   ANSEO_SQL = <same-as-above>
   ANSEO_SERVICEBUS = <same-as-above>
   # Sendmode SMS configuration (required for SMS sending)
   # Option 1: Using API Key (recommended)
   SENDMODE_API_KEY = <your-sendmode-api-key>
   # Option 2: Using Username/Password
   SENDMODE_USERNAME = <your-sendmode-username>
   SENDMODE_PASSWORD = <your-sendmode-password>
   SENDMODE_API_URL = https://api.sendmode.com/v2/sendSMS  # Optional
   SENDMODE_FROM_NUMBER = +1234567890  # Optional
   # SendGrid Email configuration (optional, for email sending)
   SENDGRID_API_KEY = <your-sendgrid-api-key>
   SENDGRID_FROM_EMAIL = <your-verified-sender-email>
   SENDGRID_FROM_NAME = Anseo Connect  # Optional
   ```

4. **Click Save** and restart the App Service

#### Docker / Kubernetes

Set environment variables in your deployment configuration:

```yaml
# docker-compose.yml example
services:
  apigateway:
    environment:
      - ANSEO_SQL=${ANSEO_SQL}
      - ANSEO_SERVICEBUS=${ANSEO_SERVICEBUS}
      - ANSEO_JWT_SECRET=${ANSEO_JWT_SECRET}
  # ... other services
```

```yaml
# Kubernetes ConfigMap example
apiVersion: v1
kind: ConfigMap
metadata:
  name: anseo-connect-config
data:
  ANSEO_SQL: "<connection-string>"
  ANSEO_SERVICEBUS: "<connection-string>"
  # ... other settings
```

---

## Step 3: Create Service Bus Topics & Subscriptions

### Option A: Using Azure Portal (GUI - Easiest)

#### Prerequisites
- Azure subscription
- Azure Service Bus namespace created
- Contributor or Service Bus Data Owner role

#### Steps

1. **Navigate to Service Bus Namespace**
   - Go to Azure Portal → Your Resource Group → Service Bus Namespace
   - Or: https://portal.azure.com → Search "Service Bus namespaces" → Select your namespace

2. **Create Topics**

   For each topic (`attendance`, `comms`, `workflow`):

   a. Click **"Topics"** in the left menu
   b. Click **"+ Topic"** button
   c. Enter topic name (e.g., `attendance`)
   d. **Configuration:**
      - **Default message time to live**: `7.00:00:00` (7 days)
      - **Maximum topic size**: `256 MB` (or as needed)
      - **Default message time to live**: Keep default or set to your needs
      - **Duplicate detection history**: `10.00:00:00` (10 minutes)
   e. Click **"Create"**
   f. Repeat for `comms` and `workflow` topics

3. **Create Subscriptions**

   **For `attendance` topic:**

   a. Click on **`attendance`** topic
   b. Click **"+ Subscription"** button
   c. **Subscription name**: `workflow-attendance-ingested`
   d. **Configuration:**
      - **Default message time to live**: `7.00:00:00`
      - **Lock duration**: `00:01:00` (1 minute - adjust based on processing time)
      - **Maximum delivery count**: `10` (retry failed messages up to 10 times)
      - **Enable dead lettering on message expiration**: ✅ Enabled
      - **Enable dead lettering on filter evaluation exceptions**: ✅ Enabled
   e. Click **"Create"**

   **For `comms` topic:**

   a. Click on **`comms`** topic
   b. Create subscription: `comms-send-message`
      - **Lock duration**: `00:01:00`
      - **Maximum delivery count**: `10`
      - Enable dead lettering on both options: ✅
   c. Create subscription: `workflow-message-events`
      - **Lock duration**: `00:01:00`
      - **Maximum delivery count**: `10`
      - Enable dead lettering on both options: ✅

   **For `workflow` topic:**
   
   (No subscriptions needed in v0.1, but create for future use)
   - Optionally create: `workflow-general` (for future events)

4. **Get Connection String**

   a. Go back to Service Bus Namespace overview
   b. Click **"Shared access policies"** in left menu
   c. Click **"RootManageSharedAccessKey"** (or create a custom policy)
   d. Copy the **"Primary connection string"** or **"Secondary connection string"**
   e. Use this in your environment variables as `ANSEO_SERVICEBUS`

### Option B: Using Azure CLI (Scripted)

Save as `create-servicebus.ps1`:

```powershell
# Variables
$resourceGroup = "<your-resource-group>"
$serviceBusNamespace = "<your-namespace>"
$location = "<your-location>" # e.g., "northeurope"

# Login (if not already)
# az login

# Create topics
az servicebus topic create `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --name attendance `
    --default-message-time-to-live P7D `
    --enable-duplicate-detection true `
    --duplicate-detection-history-time-window PT10M

az servicebus topic create `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --name comms `
    --default-message-time-to-live P7D `
    --enable-duplicate-detection true `
    --duplicate-detection-history-time-window PT10M

az servicebus topic create `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --name workflow `
    --default-message-time-to-live P7D `
    --enable-duplicate-detection true `
    --duplicate-detection-history-time-window PT10M

# Create subscriptions for 'attendance' topic
az servicebus topic subscription create `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --topic-name attendance `
    --name workflow-attendance-ingested `
    --lock-duration PT1M `
    --max-delivery-count 10 `
    --dead-letter-on-message-expiration true `
    --dead-letter-on-filter-evaluation-exceptions true

# Create subscriptions for 'comms' topic
az servicebus topic subscription create `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --topic-name comms `
    --name comms-send-message `
    --lock-duration PT1M `
    --max-delivery-count 10 `
    --dead-letter-on-message-expiration true `
    --dead-letter-on-filter-evaluation-exceptions true

az servicebus topic subscription create `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --topic-name comms `
    --name workflow-message-events `
    --lock-duration PT1M `
    --max-delivery-count 10 `
    --dead-letter-on-message-expiration true `
    --dead-letter-on-filter-evaluation-exceptions true

# Get connection string
az servicebus namespace authorization-rule keys list `
    --resource-group $resourceGroup `
    --namespace-name $serviceBusNamespace `
    --name RootManageSharedAccessKey `
    --query primaryConnectionString `
    --output tsv
```

Run with:
```powershell
.\create-servicebus.ps1
```

### Option C: Using Azure PowerShell

```powershell
# Install module if needed
# Install-Module -Name Az.ServiceBus

# Variables
$resourceGroup = "<your-resource-group>"
$namespace = "<your-namespace>"

# Create topics
New-AzServiceBusTopic `
    -ResourceGroupName $resourceGroup `
    -Namespace $namespace `
    -Name attendance `
    -DefaultMessageTimeToLive (New-TimeSpan -Days 7) `
    -EnableDuplicateDetection $true `
    -DuplicateDetectionHistoryTimeWindow (New-TimeSpan -Minutes 10)

New-AzServiceBusTopic `
    -ResourceGroupName $resourceGroup `
    -Namespace $namespace `
    -Name comms `
    -DefaultMessageTimeToLive (New-TimeSpan -Days 7) `
    -EnableDuplicateDetection $true `
    -DuplicateDetectionHistoryTimeWindow (New-TimeSpan -Minutes 10)

New-AzServiceBusTopic `
    -ResourceGroupName $resourceGroup `
    -Namespace $namespace `
    -Name workflow `
    -DefaultMessageTimeToLive (New-TimeSpan -Days 7) `
    -EnableDuplicateDetection $true `
    -DuplicateDetectionHistoryTimeWindow (New-TimeSpan -Minutes 10)

# Create subscriptions
New-AzServiceBusSubscription `
    -ResourceGroupName $resourceGroup `
    -Namespace $namespace `
    -Topic attendance `
    -Name workflow-attendance-ingested `
    -LockDuration (New-TimeSpan -Minutes 1) `
    -MaxDeliveryCount 10 `
    -DeadLetteringOnMessageExpiration `
    -DeadLetteringOnFilterEvaluationException

New-AzServiceBusSubscription `
    -ResourceGroupName $resourceGroup `
    -Namespace $namespace `
    -Topic comms `
    -Name comms-send-message `
    -LockDuration (New-TimeSpan -Minutes 1) `
    -MaxDeliveryCount 10 `
    -DeadLetterOnMessageExpiration `
    -DeadLetterOnFilterEvaluationException

New-AzServiceBusSubscription `
    -ResourceGroupName $resourceGroup `
    -Namespace $namespace `
    -Topic comms `
    -Name workflow-message-events `
    -LockDuration (New-TimeSpan -Minutes 1) `
    -MaxDeliveryCount 10 `
    -DeadLetterOnMessageExpiration `
    -DeadLetterOnFilterEvaluationException
```

### Verification

After creating topics and subscriptions, verify:

1. **In Azure Portal:**
   - Topics: `attendance`, `comms`, `workflow` exist
   - Subscriptions under `attendance`: `workflow-attendance-ingested`
   - Subscriptions under `comms`: `comms-send-message`, `workflow-message-events`

2. **Using Azure CLI:**
   ```bash
   az servicebus topic list --resource-group <rg> --namespace-name <ns> --output table
   az servicebus topic subscription list --resource-group <rg> --namespace-name <ns> --topic-name attendance --output table
   az servicebus topic subscription list --resource-group <rg> --namespace-name <ns> --topic-name comms --output table
   ```

3. **Test Connection:**
   Use the Service Bus Explorer in Azure Portal or a simple test application to verify you can publish/subscribe to topics.

---

## Summary Checklist

- [ ] **Step 1**: Migrations applied, all tables created
- [ ] **Step 2**: Environment variables configured for all services
- [ ] **Step 3**: Service Bus topics and subscriptions created
- [ ] **Verification**: All services can connect to database and Service Bus
- [ ] **Next**: Proceed to Step 4 (Deploy Services) and Step 5 (Integration Testing)

---

**Next Steps**: See `docs/IMPLEMENTATION_STATUS_v0.1.md` for deployment and testing checklists.
