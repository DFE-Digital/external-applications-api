# 📋 DfE External Applications API

The **External Applications API** is the backend service for the **External Applications Tool (EAT)** — a template-driven form engine that transforms JSON schemas into dynamic, multi-step web forms for Government services.

This API manages templates, applications, user permissions, file uploads, and real-time notifications. It serves as the data and business logic layer for the EAT Web frontend, enabling rapid deployment of new government application forms without code changes.

---

## 🚀 Features

- 📝 **JSON Template Engine** — Configurable form schemas stored in versioned templates  
- 📨 **Event-Driven Architecture** — Azure Service Bus + MassTransit messaging  
- 🔐 **Multi-tenant Permissions** — Fine-grained access control per template and application  
- 📂 **Secure File Uploads** — Azure File Share storage with automatic virus scanning via ClamAV  
- 🔔 **Real-time Notifications** — SignalR integration for instant user feedback  
- ✉️ **GOV.UK Notify** — Email notifications for application events  
- 📊 **Full Audit Trail** — SQL Server temporal tables for complete change history  
- ⚡ **Rate Limiting** — Built-in throttling for API protection  
- 🧠 **Redis Caching** — Performance optimization via distributed cache  
- 🧩 **Auto-generated Client SDK** — NSwag-generated strongly-typed .NET client  

---

## 🏗️ Architecture Overview

This project follows a strict **Domain-Driven Design (DDD)** and **Clean Architecture** pattern, ensuring clear separation of concerns and maintainability.

| Layer | Project | Purpose |
|-------|---------|---------|
| **Presentation** | `GovUK.Dfe.FlexForms.Api` | REST API, SignalR hubs, authentication, Swagger |
| **Application** | `GovUK.Dfe.FlexForms.Application` | CQRS handlers, validators, domain event handlers |
| **Domain** | `GovUK.Dfe.FlexForms.Domain` | Entities, value objects, domain events, interfaces |
| **Infrastructure** | `GovUK.Dfe.FlexForms.Infrastructure` | EF Core, repositories, external service implementations |
| **Utilities** | `GovUK.Dfe.FlexForms.Utils` | Cross-cutting utilities (file hashing, etc.) |
| **Client SDK** | `GovUK.GovUK.Dfe.FlexForms.Api.Client` | Auto-generated API client for consumers |

---

## 🔄 System Design Diagram

The following diagram illustrates the complete system architecture, showing how the EAT Web Frontend interacts with the API, and how data flows through the Clean Architecture layers to Azure platform services.

```mermaid
flowchart LR
    %% =========================
    %% External Systems
    %% =========================
    subgraph External["External Systems"]
        Web["🌐 EAT Web Frontend"]
        Notify["📧 GOV.UK Notify"]
        ClamAV["🛡️ ClamAV Scanner"]
    end

    %% =========================
    %% Azure Platform
    %% =========================
    subgraph Azure["Azure Platform"]
        SB["📬 Azure Service Bus"]
        FS["📁 Azure File Share"]
        ASR["🔔 Azure SignalR"]
        Redis["⚡ Redis Cache"]
        SQL["🗄️ Azure SQL (Temporal Tables)"]
    end

    %% =========================
    %% API
    %% =========================
    subgraph API["External Applications API"]
        direction TB

        %% Presentation
        subgraph Presentation["Presentation Layer"]
            Controllers["🎮 REST Controllers"]
            Hubs["📡 SignalR Hub"]
        end

        %% Application
        subgraph Application["Application Layer"]
            Commands["📝 Commands"]
            Queries["🔍 Queries"]
            Validators["✅ Validators"]
            AppEvents["⚡ Application Event Handlers"]
            Consumers["📥 MassTransit Consumers"]
        end

        %% Domain
        subgraph Domain["Domain Layer"]
            Entities["📦 Aggregates & Entities"]
            DomainEvents["🎯 Domain Events"]
            Factories["🏭 Factories"]
        end

        %% Infrastructure
        subgraph Infrastructure["Infrastructure Layer"]
            DbContext["🗃️ EF Core DbContext"]
            Repos["📚 Repositories"]
            Dispatcher["📤 Domain Event Dispatcher"]
        end
    end

    %% =========================
    %% Web Interaction
    %% =========================
    Web -->|HTTP / REST| Controllers
    Web -->|WebSocket| Hubs
    Hubs --> ASR

    %% =========================
    %% Request Flow
    %% =========================
    Controllers --> Commands
    Controllers --> Queries

    Commands --> Validators
    Commands --> Entities
    Commands --> DomainEvents

    Queries --> Repos
    Queries --> Redis
    Commands --> Redis

    %% =========================
    %% Domain & Persistence
    %% =========================
    Repos --> DbContext
    DbContext --> SQL
    Dispatcher --> DomainEvents

    %% =========================
    %% Events & Messaging
    %% =========================
    DomainEvents --> AppEvents
    AppEvents --> Notify
    AppEvents -->|Publish ScanRequestedEvent| SB

    Consumers -->|Consume ScanResultEvent| SB
    ClamAV -->|Publish ScanResultEvent| SB

    %% =========================
    %% File Handling
    %% =========================
    Commands -->|Upload files| FS
    Queries -->|Download files| FS
    ClamAV -->|Read file via SAS URL| FS

```

#### Layer Responsibilities

| Layer | Components | Responsibility |
|-------|------------|----------------|
| **External Systems** | EAT Web, GOV.UK Notify, ClamAV | Frontend UI, email delivery, virus scanning |
| **Azure Platform** | Service Bus, File Share, SignalR, Redis, SQL | Messaging, storage, real-time comms, caching, persistence |
| **Presentation** | Controllers, SignalR Hub | HTTP endpoints, WebSocket connections, request/response handling |
| **Application** | Commands, Queries, Validators, Event Handlers, Consumers | Business logic orchestration, validation, event processing |
| **Domain** | Entities, Domain Events, Factories | Core business rules, aggregate roots, domain event raising |
| **Infrastructure** | DbContext, Repositories, Event Dispatcher | Data persistence, external service integration, event dispatch |

#### Key Data Flows

1. **Request Flow**: Web → Controllers → Commands/Queries → Validators → Domain → Repositories → SQL
2. **Event Flow**: Domain Events → Application Event Handlers → GOV.UK Notify / Service Bus
3. **File Flow**: Commands → Azure File Share ↔ ClamAV Scanner (via SAS URLs)
4. **Real-time Flow**: SignalR Hub → Azure SignalR Service → Connected clients

---

## 📚 Domain Model

The domain model represents the core business entities and their relationships. This is a DDD-aligned model where each aggregate root (User, Template, Application) encapsulates its own business rules and child entities.

```mermaid
erDiagram
    %% =========================
    %% Core Actors
    %% =========================
    User ||--o{ Application : creates
    User ||--o{ Permission : granted
    User ||--o{ TemplatePermission : granted
    User ||--o{ File : uploads
    User ||--o{ Role : assigned

    %% =========================
    %% Templates
    %% =========================
    Template ||--o{ TemplateVersion : versions
    Template ||--o{ TemplatePermission : access

    %% =========================
    %% Applications
    %% =========================
    TemplateVersion ||--o{ Application : used_by
    Application ||--o{ ApplicationResponse : contains
    Application ||--o{ File : attachments
    Application ||--o{ Permission : access

    %% =========================
    %% Entities
    %% =========================
    User {
        guid UserId PK
        string Name
        string Email
        string ExternalProviderId
        datetime CreatedOn
    }

    Role {
        guid RoleId PK
        string Name
    }

    Template {
        guid TemplateId PK
        string Name
        datetime CreatedOn
        guid CreatedBy FK
    }

    TemplateVersion {
        guid TemplateVersionId PK
        guid TemplateId FK
        string VersionNumber
        json JsonSchema
        datetime CreatedOn
    }

    Application {
        guid ApplicationId PK
        string ApplicationReference
        guid TemplateVersionId FK
        enum Status
        datetime CreatedOn
        guid CreatedBy FK
    }

    ApplicationResponse {
        guid ResponseId PK
        guid ApplicationId FK
        json ResponseBody
        datetime CreatedOn
        guid CreatedBy FK
    }

    File {
        guid FileId PK
        guid ApplicationId FK
        string Name
        string FileName
        string OriginalFileName
        string Path
        bigint FileSize
        datetime UploadedOn
    }

    Permission {
        guid PermissionId PK
        guid UserId FK
        guid ApplicationId FK
        enum ResourceType
        string ResourceKey
        enum AccessType
    }

    TemplatePermission {
        guid TemplatePermissionId PK
        guid UserId FK
        guid TemplateId FK
        enum AccessType
    }

```

#### Entity Descriptions

| Entity | Type | Description |
|--------|------|-------------|
| **User** | Aggregate Root | Represents authenticated users with roles and permissions |
| **Role** | Entity | Defines user roles (e.g., Admin, User) for coarse-grained access |
| **Template** | Aggregate Root | Form template definition containing versioned JSON schemas |
| **TemplateVersion** | Entity | Specific version of a template's JSON schema |
| **Application** | Aggregate Root | User's form submission linked to a template version |
| **ApplicationResponse** | Entity | JSON response data for an application (supports version history) |
| **File** | Entity | Uploaded file metadata linked to an application |
| **Permission** | Entity | Fine-grained access control for specific applications |
| **TemplatePermission** | Entity | Access control for which users can use which templates |

#### Key Relationships

- **User → Application**: Users create and own applications
- **Template → TemplateVersion**: Templates have multiple versions (schema evolution)
- **TemplateVersion → Application**: Applications are bound to a specific template version
- **Application → ApplicationResponse**: Applications contain multiple response versions (draft history)
- **Application → File**: Applications can have multiple file attachments
- **User/Template → Permission/TemplatePermission**: Fine-grained access control

---

## 📬 Event Flow: File Upload & Virus Scanning

This sequence diagram shows the complete lifecycle of a file upload, from initial upload through virus scanning to final result handling. The process is fully asynchronous — users receive immediate upload confirmation while scanning happens in the background.

```mermaid
sequenceDiagram
    participant U as User
    participant API as External Applications API
    participant FS as Azure File Share
    participant SB as Azure Service Bus
    participant VS as ClamAV Scanner Function
    participant SR as SignalR Hub

    U->>API: POST /applications/{id}/files
    API->>FS: Upload file
    API->>API: Create File entity
    API->>API: Raise FileUploadedDomainEvent
    API-->>U: 201 Created (FileId)
    
    API->>SB: Publish ScanRequestedEvent
    SB->>VS: Trigger scan
    
    VS->>FS: Download file (via SAS URL)
    VS->>VS: Scan with ClamAV
    VS->>SB: Publish ScanResultEvent
    
    SB->>API: ScanResultConsumer receives result
    
    alt File is Clean
        API->>API: Log clean status
    else File is Infected
        API->>FS: Delete infected file
        API->>API: Remove File record
        API->>SR: Push notification to user
        SR-->>U: Real-time infected file alert
    end
```

#### Process Steps

| Phase | Steps | Description |
|-------|-------|-------------|
| **Upload** | 1-5 | User uploads file → API stores in Azure File Share → File entity created → `FileUploadedDomainEvent` raised → User receives `201 Created` |
| **Scan Request** | 6-7 | API publishes `ScanRequestedEvent` to Service Bus → ClamAV Scanner Function triggered |
| **Scanning** | 8-10 | Scanner downloads file via SAS URL → ClamAV performs virus scan → `ScanResultEvent` published |
| **Result Handling** | 11-16 | API's `ScanResultConsumer` receives result → Clean files logged, infected files deleted with real-time notification |

#### Key Design Decisions

- **Async Processing**: Users don't wait for scans; immediate response improves UX
- **SAS URLs**: Time-limited, read-only access tokens for secure file transfer
- **Event Sourcing**: All file events tracked via domain events and Service Bus
- **Real-time Alerts**: SignalR notifies users immediately if infected files detected
- **Automatic Cleanup**: Infected files are automatically deleted from both storage and database

---

## 🗂️ Project Structure

```
external-applications-api/
├── 📄 README.md
├── 📄 Dockerfile                          # Multi-stage build with EF migrations
├── 📄 GovUK.Dfe.FlexForms.Api.sln
├── 📄 Directory.Build.props               # Shared MSBuild properties
│
├── 📁 src/
│   ├── 📁 GovUK.Dfe.FlexForms.Api/           # Presentation Layer
│   │   ├── Controllers/                           # REST API endpoints
│   │   ├── Hubs/                                  # SignalR hubs
│   │   ├── Security/                              # Authorization handlers
│   │   ├── ExceptionHandlers/                     # Global exception handling
│   │   ├── Middleware/                            # Custom middleware
│   │   ├── Swagger/                               # OpenAPI configuration
│   │   └── Program.cs                             # Application entry point
│   │
│   ├── 📁 GovUK.Dfe.FlexForms.Application/   # Application Layer
│   │   ├── Applications/                          # Application aggregate handlers
│   │   │   ├── Commands/                          # Create, Update, Submit, Upload
│   │   │   ├── Queries/                           # Get, List, Download
│   │   │   ├── EventHandlers/                     # Domain event handlers
│   │   │   └── QueryObjects/                      # Reusable query specifications
│   │   ├── Templates/                             # Template management
│   │   ├── Users/                                 # User management
│   │   ├── Notifications/                         # Notification handling
│   │   ├── Consumers/                             # MassTransit consumers
│   │   ├── Common/                                # Shared behaviors & exceptions
│   │   │   ├── Behaviours/                        # MediatR pipeline behaviors
│   │   │   ├── Exceptions/                        # Custom exceptions
│   │   │   └── Models/                            # Configuration models
│   │   └── Services/                              # Application services
│   │
│   ├── 📁 GovUK.Dfe.FlexForms.Domain/        # Domain Layer
│   │   ├── Entities/                              # Aggregate roots & entities
│   │   ├── Events/                                # Domain events
│   │   ├── ValueObjects/                          # Strongly-typed IDs
│   │   ├── Factories/                             # Entity factories
│   │   ├── Interfaces/                            # Repository contracts
│   │   ├── Services/                              # Domain services
│   │   └── Common/                                # Base classes & interfaces
│   │
│   ├── 📁 GovUK.Dfe.FlexForms.Infrastructure/# Infrastructure Layer
│   │   ├── Database/                              # EF Core DbContext
│   │   │   └── Interceptors/                      # Domain event dispatcher
│   │   ├── Repositories/                          # Repository implementations
│   │   ├── Migrations/                            # EF Core migrations
│   │   ├── Services/                              # External service implementations
│   │   └── Security/                              # Auth implementations
│   │
│   ├── 📁 GovUK.Dfe.FlexForms.Utils/         # Utilities
│   │   └── File/                                  # File utilities (hashing, etc.)
│   │
│   ├── 📁 GovUK.GovUK.Dfe.FlexForms.Api.Client/  # Client SDK
│   │   ├── Generated/                             # NSwag auto-generated client
│   │   ├── Security/                              # Auth helpers
│   │   └── Extensions/                            # DI extensions
│   │
│   ├── 📁 Benchmarks/                             # Performance benchmarks
│   │
│   └── 📁 Tests/
│       ├── GovUK.Dfe.FlexForms.Api.Tests/              # API unit tests
│       ├── GovUK.Dfe.FlexForms.Api.Tests.Integration/  # Integration tests
│       ├── GovUK.Dfe.FlexForms.Application.Tests/      # Application layer tests
│       ├── GovUK.Dfe.FlexForms.Domain.Tests/           # Domain layer tests
│       └── GovUK.Dfe.FlexForms.Tests.Common/           # Shared test utilities
│
├── 📁 terraform/                          # Infrastructure as Code
│   ├── container-apps-hosting.tf          # Azure Container Apps module
│   ├── variables.tf                       # Terraform variables
│   └── ...
│
├── 📁 docs/
│   └── adrs/                              # Architecture Decision Records
│       ├── 20251125_azure_service_bus_and_signal_r.md
│       ├── 20251125_configurable_json_templates.md
│       ├── 20251125_temporal_tables_for_auditing.md
│       ├── 20251125_use_azure_file_share.md
│       └── 20251125_use_clamav_for_virus_scanning.md
│
└── 📁 .github/workflows/                  # CI/CD Pipelines
    ├── deploy.yml                         # Deployment pipeline
    ├── build-test-template.yml            # Reusable build & test
    ├── ci-pack-api-client.yml             # Client SDK packaging
    └── docker-test.yml                    # Docker build tests
```

---

## 🔐 Security & Authorization

The API implements a comprehensive authorization system:

### Authorization Policies

| Policy | Description |
|--------|-------------|
| `CanCreateAnyApplication` | Create applications for accessible templates |
| `CanReadAnyApplication` | Read applications user has access to |
| `CanUpdateApplication` | Update specific applications |
| `CanReadTemplate` | Read template schemas |
| `CanWriteTemplate` | Create template versions (Admin) |
| `CanReadApplicationFiles` | Download application files |
| `CanWriteApplicationFiles` | Upload files to applications |
| `CanDeleteApplicationFiles` | Remove files from applications |

### Authentication Methods

| Method | Use Case |
|--------|----------|
| **Azure Entra ID / DfE Sign-in** | User authentication via OIDC |
| **Service Principal** | Machine-to-machine authentication for internal services |
| **EA Exchange Token (OBO)** | On-Behalf-Of token for API access |

### Authentication & Authorization Flow

The API uses a multi-stage authentication flow combining Azure Entra ID with a custom On-Behalf-Of (OBO) token exchange for fine-grained authorization:

```mermaid
sequenceDiagram
    autonumber

    participant User as End User (Browser)
    participant FE as External Applications Frontend (EA Web)
    participant IdP as Azure Entra ID / DfE Sign-in
    participant API as EA API
    participant AuthZ as EA Authz Service
    participant TokenStore as Token Store / Cache

    %% ───────────────────────────────
    %% USER LOGIN + TOKEN ACQUISITION
    %% ───────────────────────────────
    User->>FE: Access secured page
    FE->>IdP: Redirect for authentication (OIDC)
    IdP-->>FE: IdP Token (id_token + access_token)
    FE->>TokenStore: Store IdP token

    %% ───────────────────────────────
    %% SERVICE TOKEN ACQUISITION
    %% ───────────────────────────────
    FE->>IdP: Request Service Token<br/>(Client Credentials)
    IdP-->>FE: Service Token
    FE->>TokenStore: Store Service Token

    %% ───────────────────────────────
    %% OBO TOKEN EXCHANGE
    %% ───────────────────────────────
    FE->>API: POST /tokens/exchange<br/>(IdP Token + Service Token)
    API->>AuthZ: Validate user identity and tokens
    AuthZ-->>API: Generate EA Exchange Token
    API-->>FE: EA OBO Token
    FE->>TokenStore: Cache EA Exchange token

    %% ───────────────────────────────
    %% AUTHENTICATED API REQUEST
    %% ───────────────────────────────
    FE->>API: Authenticated API request<br/>(EA Exchange Token)
    API->>AuthZ: Check Coarse-Grained Access
    AuthZ-->>API: Access allowed?

    alt Coarse Access Granted
        API->>AuthZ: Check Fine-Grained Permissions<br/>(Resource + Action)
        AuthZ-->>API: Authorised / Denied
        API-->>FE: Return data or forbidden
    else Coarse Access Denied
        API-->>FE: 403 Forbidden
    end
```

#### Flow Explanation

| Step | Description |
|------|-------------|
| **1-4** | User authenticates via Azure Entra ID / DfE Sign-in, frontend receives IdP tokens |
| **5-7** | Frontend acquires a service token using client credentials |
| **8-12** | Frontend exchanges both tokens with the API for an EA Exchange Token (OBO flow) |
| **13-18** | API requests use the EA token; authorization happens in two phases: coarse-grained (role-based) then fine-grained (resource-specific permissions) |

---

## 🏢 Multi-Tenancy Architecture

The API is designed to serve **multiple frontend applications** (tenants) simultaneously, each with completely isolated configurations. This enables a single API deployment to support different government services with distinct authentication providers, connection strings, and security settings.

### Multi-Tenant Design Pattern

```mermaid
flowchart TB
    subgraph Frontends["Frontend Applications"]
        FA["🌐 Frontend A<br/>(Tenant A)"]
        FB["🌐 Frontend B<br/>(Tenant B)"]
        FC["🌐 Frontend C<br/>(Tenant C)"]
    end

    subgraph API["External Applications API (Azure Container App)"]
        direction TB
        
        subgraph Middleware["Request Pipeline"]
            TM["🔑 Tenant Resolution<br/>Middleware"]
            TCA["📋 Tenant Context<br/>Accessor"]
        end
        
        subgraph Config["Configuration"]
            TC["⚙️ Tenant Configurations<br/>(Loaded at Startup)"]
        end
        
        subgraph Auth["Multi-Provider Authentication"]
            AzureAD["🔐 Azure AD<br/>(Per-Tenant)"]
            DfESign["🔐 DfE Sign-In<br/>(Per-Tenant)"]
            Validator["✅ External Identity<br/>Validator"]
        end
        
        subgraph Services["Tenant-Isolated Services"]
            CORS["🌍 Dynamic CORS"]
            SignalR["📡 SignalR Endpoints"]
            ServiceBus["📬 Service Bus"]
        end
    end

    subgraph Azure["Azure Platform (Per-Tenant Resources)"]
        direction LR
        ADA["🔐 Azure AD A"]
        ADB["🔐 Azure AD B"]
        ADC["🔐 Azure AD C"]
        SBA["📬 Service Bus A"]
        SBB["📬 Service Bus B"]
        SBC["📬 Service Bus C"]
    end

    FA -->|"X-Tenant-ID: A"| TM
    FB -->|"X-Tenant-ID: B"| TM
    FC -->|"X-Tenant-ID: C"| TM
    
    TM --> TCA
    TCA --> TC
    TC --> Auth
    TC --> Services
    
    AzureAD --> ADA
    AzureAD --> ADB
    AzureAD --> ADC
    
    ServiceBus --> SBA
    ServiceBus --> SBB
    ServiceBus --> SBC

    style API fill:#e1f5fe
    style Middleware fill:#fff3e0
    style Auth fill:#f3e5f5
    style Services fill:#e8f5e9
```

### Request Flow

```mermaid
sequenceDiagram
    autonumber
    
    participant FE as Frontend (Tenant A)
    participant API as External Applications API
    participant TM as Tenant Middleware
    participant TC as Tenant Config
    participant Val as Identity Validator
    participant Svc as Business Services
    
    FE->>API: Request with X-Tenant-ID header
    API->>TM: Resolve tenant from header
    
    alt Valid Tenant ID
        TM->>TC: Load tenant configuration
        TC-->>TM: Tenant A config (AzureAd, DfESignIn, ConnectionStrings)
        TM->>API: Set ITenantContextAccessor.CurrentTenant
        
        API->>Val: Validate token (multi-provider)
        
        alt Token matches Tenant A's provider
            Val-->>API: ClaimsPrincipal
            API->>Svc: Execute request with tenant context
            Svc-->>FE: Response
        else Token from different provider
            Val-->>API: SecurityTokenValidationException
            API-->>FE: 401 Unauthorized
        end
    else Invalid/Missing Tenant ID
        TM-->>FE: 400 Bad Request<br/>("Missing or invalid X-Tenant-ID")
    end
```

### Configuration Structure

Each tenant is configured as a separate section in `appsettings.json`:

```json
{
  "Tenants": {
    "11111111-1111-1111-1111-111111111111": {
      "Id": "11111111-1111-1111-1111-111111111111",
      "Name": "Service A",
      "Frontend": {
        "Origin": "https://service-a.education.gov.uk"
      },
      "AzureAd": {
        "Instance": "https://login.microsoftonline.com/",
        "TenantId": "tenant-a-guid",
        "ClientId": "client-a-id",
        "Audience": "api://client-a-id"
      },
      "DfESignIn": {
        "Issuer": "https://oidc.service-a.signin.education.gov.uk/",
        "ClientId": "service-a-client",
        "DiscoveryEndpoint": "https://oidc.service-a.signin.education.gov.uk/.well-known/openid-configuration"
      },
      "ConnectionStrings": {
        "ServiceBus": "Endpoint=sb://service-a.servicebus.windows.net/;...",
        "AzureSignalR": "Endpoint=https://service-a-signalr.signalr.net;..."
      }
    },
    "22222222-2222-2222-2222-222222222222": {
      "Id": "22222222-2222-2222-2222-222222222222",
      "Name": "Service B",
      "Frontend": {
        "Origin": "https://service-b.education.gov.uk"
      },
      "AzureAd": {
        "Instance": "https://login.microsoftonline.com/",
        "TenantId": "tenant-b-guid",
        "ClientId": "client-b-id",
        "Audience": "api://client-b-id"
      },
      "DfESignIn": {
        "Issuer": "https://oidc.service-b.signin.education.gov.uk/",
        "ClientId": "service-b-client",
        "DiscoveryEndpoint": "https://oidc.service-b.signin.education.gov.uk/.well-known/openid-configuration"
      },
      "ConnectionStrings": {
        "ServiceBus": "Endpoint=sb://service-b.servicebus.windows.net/;...",
        "AzureSignalR": "Endpoint=https://service-b-signalr.signalr.net;..."
      }
    }
  }
}
```

### Key Components

| Component | Purpose |
|-----------|---------|
| **`TenantResolutionMiddleware`** | Extracts `X-Tenant-ID` header and resolves tenant configuration |
| **`ITenantContextAccessor`** | Scoped service providing access to the current request's tenant |
| **`ITenantConfigurationProvider`** | Singleton cache of all tenant configurations loaded at startup |
| **`TenantCorsPolicyProvider`** | Dynamic CORS policy based on tenant's frontend origin |
| **`ExternalIdentityValidator`** | Multi-provider OIDC validator (isolated per tenant, no cross-tenant leaks) |

### Security Features

```mermaid
flowchart LR
    subgraph Isolation["Tenant Isolation"]
        A1["🔐 Token from<br/>Tenant A Provider"]
        A2["🔐 Token from<br/>Tenant B Provider"]
    end
    
    subgraph Validator["Multi-Provider Validator"]
        V1["Provider A Config<br/>(Issuer A, Audience A)"]
        V2["Provider B Config<br/>(Issuer B, Audience B)"]
    end
    
    subgraph Result["Validation Result"]
        R1["✅ Valid for Tenant A"]
        R2["❌ Invalid - Cross-tenant attempt blocked"]
    end
    
    A1 --> V1
    V1 --> R1
    
    A2 -.->|"Attempt to use on<br/>Tenant A endpoint"| V1
    V1 -.-> R2
    
    style R1 fill:#c8e6c9
    style R2 fill:#ffcdd2
```

**Key Security Properties:**
- ✅ **Complete Isolation**: Each tenant's authentication is validated against its own OIDC provider
- ✅ **No Cross-Tenant Leaks**: Tokens from Tenant A cannot be used for Tenant B requests
- ✅ **Startup Validation**: All tenant configurations validated at application startup
- ✅ **Dynamic CORS**: Only the current tenant's frontend origin is allowed
- ✅ **Isolated Connection Strings**: Each tenant uses its own Service Bus, SignalR, etc.

### Using the API Client with Multi-Tenancy

When consuming this API from a frontend application, configure the tenant ID in the client:

```csharp
// In your frontend's Startup/Program.cs
services.AddExternalApplicationsApiClient(options =>
{
    options.BaseUrl = "https://external-applications-api.azurecontainerapps.io";
    options.TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
});
```

The client automatically includes the `X-Tenant-ID` header in all requests.

---

## 📦 Dependencies

### GovUK.Dfe.CoreLibs Ecosystem

| Package | Purpose |
|---------|---------|
| `GovUK.Dfe.CoreLibs.Contracts` | Shared DTOs and enums |
| `GovUK.Dfe.CoreLibs.Caching` | Redis & memory caching |
| `GovUK.Dfe.CoreLibs.Security` | Authorization framework |
| `GovUK.Dfe.CoreLibs.Messaging.MassTransit` | Service Bus integration |
| `GovUK.Dfe.CoreLibs.FileStorage` | Azure File Share operations |
| `GovUK.Dfe.CoreLibs.Email` | GOV.UK Notify integration |
| `GovUK.Dfe.CoreLibs.Notifications` | Real-time notifications |
| `GovUK.Dfe.CoreLibs.Http` | HTTP utilities & correlation |
| `GovUK.Dfe.CoreLibs.Utilities` | Rate limiting & helpers |

### Core Framework Dependencies

| Package | Purpose |
|---------|---------|
| `MediatR` | CQRS pattern implementation |
| `MassTransit` | Message bus abstraction |
| `AutoMapper` | Object mapping |
| `FluentValidation` | Request validation |
| `Entity Framework Core` | ORM & data access |
| `Serilog` | Structured logging |
| `NSwag` | OpenAPI & client generation |

---

## ⚙️ Configuration

### Environment Variables

| Key | Description | Example |
|-----|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server (application data) | `Server=...;Database=ExternalApplications;` |
| `ConnectionStrings__TenantConfigDatabase` | SQL Server (tenant config / SaaS) — required by init `/sql/migratedb` | `Server=...;Database=TenantConfig;` |
| `ConnectionStrings__ServiceBus` | Azure Service Bus | `Endpoint=sb://...servicebus.windows.net/;` |
| `ConnectionStrings__Redis` | Redis cache | `localhost:6379` |
| `ConnectionStrings__AzureSignalR` | SignalR Service | `Endpoint=https://...signalr.net;` |
| `FileStorage__Azure__ConnectionString` | File share connection | Azure Storage connection string |
| `Email__GovUkNotify__ApiKey` | GOV.UK Notify API key | *(secure)* |
| `Frontend__Origin` | CORS allowed origin | `https://eat.education.gov.uk` |

### Feature Configuration

```json
{
  "CacheSettings": {
    "Memory": {
      "DefaultDurationInSeconds": 60
    }
  },
  "NotificationService": {
    "StorageProvider": "Redis",
    "MaxNotificationsPerUser": 50
  },
  "FileStorage": {
    "Provider": "Hybrid",
    "Local": { "BaseDirectory": "/uploads" },
    "Azure": { "ShareName": "extapi-storage" }
  }
}
```

---

## 🧪 Testing

### Test Projects

| Project | Type | Coverage |
|---------|------|----------|
| `GovUK.Dfe.FlexForms.Domain.Tests` | Unit | Entities, Value Objects, Factories |
| `GovUK.Dfe.FlexForms.Application.Tests` | Unit | Handlers, Validators, Services |
| `GovUK.Dfe.FlexForms.Api.Tests` | Unit | Security, Claim Providers |
| `GovUK.Dfe.FlexForms.Api.Tests.Integration` | Integration | Full API endpoint tests |

### Test Frameworks

- **xUnit** — Test framework
- **NSubstitute** — Mocking framework
- **AutoFixture** — Test data generation
- **MockQueryable** — EF Core query mocking
- **Microsoft.AspNetCore.Mvc.Testing** — Integration test host

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test src/Tests/GovUK.Dfe.FlexForms.Application.Tests
```

---

## 🧱 Local Development

### Prerequisites

- **.NET 8 SDK**
- **Docker Desktop** (for SQL Server, Redis)
- **Azure Functions Core Tools** (optional, for local function testing)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/DFE-Digital/external-applications-api.git
   cd external-applications-api
   ```

2. **Start dependencies**
   ```bash
   # SQL Server
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
     -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

   # Redis
   docker run -p 6379:6379 -d redis:latest
   ```

3. **Configure user secrets**
   ```bash
   cd src/GovUK.Dfe.FlexForms.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
     "Server=localhost,1433;Database=ExternalApplications;User Id=SA;Password=YourPassword123!;TrustServerCertificate=True;"
   dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
   ```

4. **Apply database migrations**
   ```bash
   cd src/GovUK.Dfe.FlexForms.Api
   dotnet ef database update
   ```

5. **Run the API**
   ```bash
   dotnet run
   ```

6. **Access Swagger UI**
   - Navigate to `https://localhost:7001/swagger`

### Docker Build

```bash
# Build the container
docker build -t external-applications-api .

# Run with environment variables
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e ConnectionStrings__Redis="..." \
  external-applications-api
```

---

## 🚀 Deployment

### CI/CD Pipeline

The project uses GitHub Actions for continuous integration and deployment:

1. **Build & Test** (`build-test-template.yml`)
   - Restore packages from GitHub Packages
   - Build solution
   - Run unit tests with coverage
   - SonarCloud analysis

2. **Docker Build** (`deploy.yml`)
   - Multi-stage Docker build
   - Push to Azure Container Registry
   - Deploy to Azure Container Apps

3. **Client SDK** (`ci-pack-api-client.yml`)
   - Generate NSwag client
   - Pack NuGet package
   - Publish to GitHub Packages

### Infrastructure

Terraform modules provision:

- **Azure Container Apps** — Serverless container hosting
- **Azure SQL Server** — Managed database
- **Azure File Share** — File storage
- **Azure SignalR Service** — Real-time communication
- **Azure Service Bus** — Message queuing
- **Application Insights** — Monitoring & telemetry

---

## 📖 API Endpoints

### Applications

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/v1/applications` | Create new application |
| `GET` | `/v1/me/applications` | List user's applications |
| `GET` | `/v1/applications/reference/{ref}` | Get by reference |
| `POST` | `/v1/applications/{id}/responses` | Add response version |
| `POST` | `/v1/applications/{id}/submit` | Submit application |
| `GET` | `/v1/applications/{id}/contributors` | List contributors |
| `POST` | `/v1/applications/{id}/contributors` | Add contributor |
| `DELETE` | `/v1/applications/{id}/contributors/{userId}` | Remove contributor |
| `POST` | `/v1/applications/{id}/files` | Upload file |
| `GET` | `/v1/applications/{id}/files` | List files |
| `GET` | `/v1/applications/{id}/files/{fileId}/download` | Download file |
| `DELETE` | `/v1/applications/{id}/files/{fileId}` | Delete file |

### Templates

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/v1/templates/{id}/schema` | Get latest schema |
| `POST` | `/v1/templates/{id}/versions` | Create new version |

### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/v1/users/register` | Register/update user |
| `GET` | `/v1/users/{email}` | Get user by email |

---

## 📝 Architecture Decision Records

Key architectural decisions are documented in `/docs/adrs/`:

- **JSON Templates** — Configurable form schemas for rapid site deployment
- **Azure Service Bus** — Event-driven async processing for file scanning
- **SignalR** — Real-time user notifications
- **Temporal Tables** — SQL Server auditing for full change history
- **Azure File Share** — Mounted storage for uploaded files
- **ClamAV** — Open-source virus scanning with predictable costs

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow existing code style and patterns
- Add unit tests for new functionality
- Update documentation as needed
- Ensure all tests pass before submitting PR

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 📞 Support

For questions or issues:
- Create a GitHub Issue
- Contact the RSD Development Team
