# OrderFlow

OrderFlow is a portfolio project built to practice common backend engineering patterns in **C# / .NET 8**, with a focus on **reliability** and **clean separation of responsibilities**.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-.NET_8-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core/)
[![EF Core](https://img.shields.io/badge/EF_Core-8.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-Express-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react&logoColor=white)](https://react.dev/)
[![Vite](https://img.shields.io/badge/Vite-5-646CFF?logo=vite&logoColor=white)](https://vitejs.dev/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## Overview

OrderFlow showcases common backend engineering patterns in C# and .NET 8. The system implements a simplified order management workflow with emphasis on data consistency, reliability, and observability.

**Key Focus Areas:**
- Clean architecture with explicit layer boundaries
- Transactional consistency using the Outbox pattern
- Reliable message processing with retry logic and dead-letter handling
- Request tracing and correlation
- Focused unit and integration tests for core reliability scenarios

> **Note:** This project focuses on backend architecture and reliability. Swagger is used as the primary interface.

## Background

OrderFlow was built as a learning-focused portfolio project during my transition from C++ systems-style programming into enterprise .NET backend development.

The main goal is to practice patterns commonly found in real-world backend teams:
- transactional consistency (ACID mindset)
- reliable async processing (Outbox + worker)
- observability and debugging
- clean separation of responsibilities

## Current Version

**v0.1.0** - Learning MVP

**Features:**
- Order and order item management
- SQL Server persistence with EF Core migrations
- Simplified Transactional Outbox pattern
- Background worker with retry logic
- Request correlation middleware
- Focused test coverage (persistence, outbox processing, retries, dead-letter, concurrency)

## Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 |
| Web Framework | ASP.NET Core (Minimal APIs) |
| ORM | Entity Framework Core |
| Database | SQL Server Express |
| Web Demo UI | React + Vite |
| Testing | xUnit |

## Architecture

OrderFlow follows Clean Architecture principles with clear separation of concerns:

```
src/
├── OrderFlow.Api/              # HTTP layer, endpoints, middleware, hosted services
├── OrderFlow.Application/      # Business logic, use cases, abstractions
├── OrderFlow.Domain/           # Domain models and business rules
└── OrderFlow.Infrastructure/   # Data access, EF Core context, repositories

tests/
└── OrderFlow.Tests/            # Unit and integration tests
```

### Dependency Flow

```
OrderFlow.Api ──────────> OrderFlow.Application ──────> OrderFlow.Domain
     │                            ▲
     │                            │
     └──> OrderFlow.Infrastructure ┘
```

**Principles:**
- Domain layer has no external dependencies
- Application layer depends only on Domain abstractions
- Infrastructure implements Application interfaces
- API layer orchestrates dependency injection and hosting

## Core Features

### 🔍 Request Tracing

- **Correlation ID Middleware**: Automatically generates or propagates `X-Correlation-Id` headers
- **Structured Logging**: Correlation IDs are included in log scope for distributed tracing
- Enables end-to-end request tracking across async operations

### 💾 Transactional Outbox Pattern

Ensures data consistency when performing database updates and publishing events:

1. **Atomic Write**: Order state changes and outbox messages are saved in a single transaction
2. **Asynchronous Processing**: Background worker polls and processes messages independently
3. **At-least-once delivery pattern**: Messages are persisted before being processed, ensuring at-least-once delivery

**Example Flow:**
```
Order Confirmation Request
    ↓
Save Order Status + Outbox Message (single transaction)
    ↓
Background Worker Claims Message
    ↓
Process Event Handler
    ↓
Mark as Processed
```

### ⚙️ Background Worker

The outbox processor implements robust message handling:

- **Polling**: Continuously checks for pending messages
- **Atomic Claiming**: Uses database-level locking to prevent duplicate processing across workers
- **Retry Logic**: Exponential backoff for transient failures
- **Dead Letter Queue**: Messages exceeding retry limit are marked as dead
- **Handler Dispatch**: Type-based routing to appropriate handlers

### 📊 Admin API

Read-only endpoints for operational monitoring:

- Query messages by status (pending, processed, dead)
- Retrieve individual message details
- Useful for debugging and operational visibility

## API Reference

### Orders

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/orders` | Create a new order |
| `GET` | `/orders` | List all orders |
| `GET` | `/orders/{id}` | Get order details |
| `POST` | `/orders/{id}/items` | Add item to order |
| `POST` | `/orders/{id}/confirm` | Confirm order (triggers outbox) |
| `POST` | `/orders/{id}/cancel` | Cancel order |

### Outbox Admin

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/outbox?status={status}` | List messages by status |
| `GET` | `/outbox/{id}` | Get message details |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) (or any SQL Server instance)
- (Optional) Node.js 18+ (for the demo web UI)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/CarAraujo-Dev/OrderFlow.git
   cd OrderFlow
   ```

2. **Configure database connection**
   
   Update `appsettings.json` in `OrderFlow.Api` with your SQL Server connection string.

   Example:

   ```json
   "ConnectionStrings": {
     "Default": "Server=localhost\\SQLEXPRESS;Database=OrderFlowDbV2;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update \
     --project src/OrderFlow.Infrastructure \
     --startup-project src/OrderFlow.Api
   ```
   > This will create the database schema automatically if it does not exist.

4. **Run the application**
   ```bash
   dotnet run --project src/OrderFlow.Api
   ```

5. **Access Swagger UI**
   
   Navigate to `https://localhost:<port>/swagger`

### Web Demo UI (Optional)

The web demo UI is optional and mainly exists as a lightweight interface for demo purposes.

1. Go to the web project folder:
   ```bash
   cd src/OrderFlow.Web
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the dev server:
   ```bash
   npm run dev
   ```

4. Open the app:
   ```
   http://localhost:5173
   ```

✅ If the API runs on a different port, you may need to configure the API base URL in the web app.

## Demo Workflow

Try this sequence to see the Outbox pattern in action:

```bash
# 1. Create an order
POST /orders
{
  "customerName": "Carlos"
}

# 2. Add an item
POST /orders/1/items
{
  "name": "Melamine Desk",
  "quantity": 2,
  "unitPrice": 29.99
}

# 3. Confirm the order (publishes outbox message)
POST /orders/1/confirm

# 4. Check pending messages
GET /outbox?status=pending

# 5. Wait ~1 second for background processing

# 6. Verify message was processed
GET /outbox?status=processed
```

## Testing

### Integration Tests - Database

Integration tests use a dedicated SQL Server database via a separate `Testing` connection string.

Location:
- `tests/appsettings.Testing.json`

Example:

```json
{
  "ConnectionStrings": {
    "Testing": "Server=localhost\\SQLEXPRESS;Database=OrderFlowDb_Test;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Notes:**

> **Note:** Test parallelization is disabled because some integration tests reset the testing database.
> Running them in parallel can cause conflicts and failures due to concurrent DB teardown/setup.

**Test Coverage:**
- **Unit Tests**: Business logic and domain rules (using EF Core InMemory provider)
- **Integration Tests**: Outbox processing, concurrency scenarios, database interactions

## Design Principles

This project adheres to the following guidelines:

- **Explicit Boundaries**: Each layer has a clear responsibility
  - `Api`: HTTP hosting and transport concerns
  - `Application`: Business workflows and use case orchestration  
  - `Domain`: Pure business logic and entities
  - `Infrastructure`: Database and external system integration

- **Incremental Evolution**: Changes are made in small, tested increments
- **Pragmatic Abstractions**: Interfaces exist where they provide value, not everywhere
- **Tests as Documentation**: Integration tests run against SQL Server (using EF Core migrations) and are executed sequentially to avoid conflicts during database reset.

## Roadmap

Potential enhancements for future versions:

- [x] Worker status metrics endpoint
- [ ] Docker Compose setup for easier local development
- [ ] Improved logging and observability
- [ ] Rate limiting
- [ ] Idempotency for confirm endpoint
- [ ] CI/CD pipeline basics

## Contributing

This is a learning project, but feedback and suggestions are welcome! Feel free to open issues for discussion or suggestions on how to improve the architecture.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Carlos Araujo**  
GitHub: https://github.com/CarAraujo-Dev
