# Anseo Connect v0.1 - Implementation Status

## Overview
This document summarizes the implementation status of Anseo Connect v0.1, covering Steps 0-5 of the development plan.

## ✅ Completed Components

### Step 0: Authentication (ApiGateway)
- ✅ Dual authentication schemes configured (JWT Bearer + Entra ID placeholder)
- ✅ `AuthController` with `/api/auth/login` and `/api/auth/register` endpoints
- ✅ `TenantContextMiddleware` for automatic tenant resolution from claims
- ✅ Authorization policies: `StaffOnly` policy requiring `tenant_id` claim
- ✅ Identity configured with ASP.NET Core Identity + EF Core

### Step 1: Service Bus Infrastructure
- ✅ `MessageEnvelope<T>` contract for standardized messaging
- ✅ `ServiceBusMessageBus` for publishing messages
- ✅ `ServiceBusMessageConsumer` base class for reliable message consumption
- ✅ Topics configured: `attendance`, `comms`, `workflow`
- ✅ Correlation ID and tenant propagation support

### Step 2: Wonde Ingestion Skeleton
- ✅ `WondeClient` wrapper with:
  - Regional domain support (`api.wonde.com` for Ireland)
  - Bearer token authentication
  - Pagination (offset and cursor)
  - Date filtering (`updated_after`, `updated_before`)
  - Includes for nested data (`contacts`, `contact_details`)
- ✅ `IngestionService` with:
  - Idempotent student ingestion (unique index on `ExternalStudentId`)
  - Idempotent guardian ingestion (unique index on `ExternalGuardianId`)
  - Student-guardian relationship mapping
  - Attendance mark ingestion with date filtering
- ✅ `IngestionController` with `/api/ingestion/wonde/students` and `/api/ingestion/wonde/attendance` endpoints
- ✅ `AttendanceMarksIngestedV1` event published after successful ingestion

### Step 3: Twilio Integration & Message Service
- ✅ `TwilioSender` service for sending SMS via Twilio
- ✅ `MessageService` with:
  - Consent evaluation using policy packs
  - Message sending via Twilio
  - Message persistence with status tracking
  - Delivery event publication
- ✅ `SendMessageRequestedConsumer` consuming from `comms` topic
- ✅ `TwilioWebhookController` in ApiGateway:
  - `/webhooks/twilio/delivery` - Handles delivery status updates
  - `/webhooks/twilio/reply` - Handles inbound SMS replies
  - Opt-out keyword detection ("STOP", "UNSUBSCRIBE", "CANCEL", etc.)
  - Consent state updates on opt-out
  - Event publication: `MessageDeliveryUpdatedV1`, `GuardianReplyReceivedV1`, `GuardianOptOutRecordedV1`

### Step 4: Workflow Service (Case Management & Safeguarding)
- ✅ `AbsenceDetectionService` - Detects unexplained absences after cutoff time
- ✅ `CaseService` - Manages attendance cases and timeline events
- ✅ `SafeguardingService` - Evaluates safeguarding triggers from policy packs
- ✅ `SafeguardingEvaluator` - Pattern trigger evaluation with metric comparison
- ✅ `AttendanceMarksIngestedConsumer` - Orchestrates the full attendance loop:
  - Detects unexplained absences for ingested date
  - Creates/updates attendance cases
  - Publishes message requests to guardians
  - Evaluates safeguarding triggers and creates alerts
- ✅ `MessageEventConsumer` - Updates case timelines from message events:
  - Message delivery updates
  - Guardian replies
  - Opt-out recordings

### Step 5: Minimal Staff Endpoints
- ✅ `CaseQueryService` - Read-only queries with DTO projections
- ✅ `CasesController`:
  - `GET /api/cases?status=OPEN&skip=0&take=50` - List open cases with pagination
  - `GET /api/cases/{caseId}` - Get case details with full timeline
  - `PATCH /api/cases/{caseId}/checklist/{checklistId}/complete` - Placeholder for v0.2
- ✅ `AbsencesController`:
  - `GET /api/absences/today` - List today's unexplained absences
- ✅ `ConsentController`:
  - `GET /api/consent/{guardianId}?channel=SMS` - Get consent status

### Database Schema
- ✅ Migrations created:
  - `Initial` - Core entities (Tenant, School, Student, Guardian, AttendanceMark)
  - `Step0_AddIdentity` - ASP.NET Core Identity tables
  - `Step3_AddCommsEntities` - `ConsentState`, `Message` entities
  - `Step4_AddCaseEntities` - `Case`, `CaseTimelineEvent`, `SafeguardingAlert` entities
- ✅ Unique indexes for idempotency:
  - `IX_Students_Tenant_School_ExternalId` - Prevents duplicate students
  - `IX_Guardians_Tenant_School_ExternalId` - Prevents duplicate guardians
  - `IX_AttendanceMarks_Tenant_School_Student_Date_Session` - Prevents duplicate marks
- ✅ Multi-tenancy filters applied via global query filters

## 🔧 Configuration Required

### Environment Variables / User Secrets

#### All Services (SQL Database)
```
ANSEO_SQL=Server=<server>;Database=<database>;User Id=<user>;Password=<password>;TrustServerCertificate=True;
```

#### All Services (Service Bus)
```
ANSEO_SERVICEBUS=Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<key-name>;SharedAccessKey=<key-value>;
```

#### ApiGateway (JWT)
```
ANSEO_JWT_SECRET=<256-bit-secret-key>
```

#### Comms Service (Twilio)
```
TWILIO_ACCOUNT_SID=<account-sid>
TWILIO_AUTH_TOKEN=<auth-token>
TWILIO_FROM_NUMBER=<e164-phone-number>
```

#### Ingestion Service (Wonde)
```
WONDE_API_TOKEN=<bearer-token>
```

### appsettings.json Placeholders
All services have `appsettings.json` files with placeholder values that should be replaced with environment variables or user secrets in production.

## 📋 Database Migrations

To apply migrations:
```bash
dotnet ef database update --project src/Shared/AnseoConnect.Data --startup-project src/Services/AnseoConnect.ApiGateway --context AnseoConnectDbContext
```

Or use the DBMigrator tool:
```bash
dotnet run --project tools/DBMigrator
```

## 🚀 Deployment Checklist

1. **Database Setup**
   - [ ] Create Azure SQL Database or configure SQL Server
   - [ ] Run migrations to create schema
   - [ ] Seed initial tenant(s) and school(s)

2. **Service Bus Setup**
   - [ ] Create Azure Service Bus namespace
   - [ ] Create topics: `attendance`, `comms`, `workflow`
   - [ ] Create subscriptions:
     - `workflow-attendance-ingested` on `attendance` topic
     - `comms-send-message` on `comms` topic
     - `workflow-message-events` on `comms` topic

3. **Twilio Configuration**
   - [ ] Create Twilio account and get credentials
   - [ ] Configure webhook URLs:
     - Delivery: `https://<apigateway-url>/webhooks/twilio/delivery`
     - Reply: `https://<apigateway-url>/webhooks/twilio/reply`

4. **Wonde Integration**
   - [ ] Obtain Wonde API token for each school
   - [ ] Configure school Wonde IDs in database

5. **Policy Packs**
   - [ ] Deploy policy pack JSON files to `policy-packs/{country}/{pack-id}/{version}/`
   - [ ] Verify schemas using `PolicyPackTool`

6. **Service Deployment**
   - [ ] Deploy ApiGateway (requires DB, Service Bus, JWT secret)
   - [ ] Deploy Ingestion.Wonde (requires DB, Service Bus, Wonde token)
   - [ ] Deploy Workflow (requires DB, Service Bus)
   - [ ] Deploy Comms (requires DB, Service Bus, Twilio credentials)

## 🔍 Testing Checklist

### End-to-End Flow
1. [ ] Ingest students and guardians from Wonde
2. [ ] Ingest attendance marks for a date
3. [ ] Verify `AttendanceMarksIngestedV1` event is published
4. [ ] Verify workflow detects unexplained absences
5. [ ] Verify attendance cases are created
6. [ ] Verify `SendMessageRequestedV1` commands are published
7. [ ] Verify messages are sent via Twilio (with consent checks)
8. [ ] Verify message delivery updates via webhook
9. [ ] Verify guardian replies are processed and opt-outs recorded
10. [ ] Verify safeguarding alerts are created when triggers match
11. [ ] Verify staff endpoints return correct data

### API Endpoints
- [ ] `POST /api/auth/login` - Local JWT authentication
- [ ] `POST /api/auth/register` - User registration
- [ ] `GET /api/cases` - List open cases (requires auth)
- [ ] `GET /api/cases/{id}` - Get case details (requires auth)
- [ ] `GET /api/absences/today` - List today's absences (requires auth)
- [ ] `GET /api/consent/{guardianId}?channel=SMS` - Get consent status (requires auth)
- [ ] `POST /api/ingestion/wonde/students` - Ingest students (requires auth)
- [ ] `POST /api/ingestion/wonde/attendance` - Ingest attendance (requires auth)
- [ ] `POST /webhooks/twilio/delivery` - Delivery callback (no auth, but should validate Twilio signature)
- [ ] `POST /webhooks/twilio/reply` - Reply callback (no auth, but should validate Twilio signature)

## 🐛 Known Limitations / Future Enhancements

1. **Authentication**
   - Entra ID authentication is placeholder (TODO: Add when Microsoft.Identity.Web vulnerability resolved)

2. **Consent Evaluation**
   - Simplified for v0.1 - full policy pack evaluation can be enhanced

3. **Safeguarding Evaluation**
   - Pattern triggers are evaluated, but full policy pack playbook support can be expanded

4. **Tier 2 Escalation**
   - Basic escalation logic implemented - full Tier 2 workflow in v0.2

5. **Checklist Completion**
   - Placeholder endpoint exists - full implementation in v0.2

6. **Phone Number Normalization**
   - Simplified normalization - should use proper phone number library (e.g., libphonenumber)

7. **Twilio Webhook Security**
   - Should validate Twilio request signatures for production

8. **Policy Pack Loading**
   - Currently loads from file system - should support versioned storage and caching

9. **Error Handling**
   - Basic error handling - can be enhanced with retry policies and dead-letter queues

10. **Monitoring & Logging**
    - Structured logging in place - can add Application Insights integration

## 📊 Architecture Overview

```
┌──────────────┐
│   ApiGateway │ (Auth, Webhooks, Staff Endpoints)
└──────┬───────┘
       │
       ├─────────────────┐
       │                 │
┌──────▼───────┐  ┌──────▼────────┐
│  Ingestion   │  │    Workflow   │
│  (Wonde)     │  │  (Absence/Case│
└──────┬───────┘  │  Management)  │
       │          └──────┬────────┘
       │                 │
       └────────┬────────┘
                │
        ┌───────▼────────┐
        │  Service Bus   │
        │  (Topics)      │
        └───────┬────────┘
                │
       ┌────────▼────────┐
       │     Comms       │
       │   (Twilio SMS)  │
       └─────────────────┘
```

## ✅ Implementation Complete

All core functionality for v0.1 is implemented and building successfully. The system is ready for:
1. Database migration application
2. Service Bus topic/subscription creation
3. Environment variable configuration
4. Integration testing
5. Deployment to Azure

---

**Last Updated**: 2025-01-10
**Version**: v0.1.0
**Status**: ✅ Implementation Complete - Ready for Testing
