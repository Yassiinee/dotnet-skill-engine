# Clean Architecture in .NET

## What Is It?

Clean Architecture, popularised by Robert C. Martin ("Uncle Bob"), organises a system into **concentric dependency layers** where every dependency points **inward** — toward the domain. The domain layer has **zero external dependencies**.

```
┌───────────────────────────────┐
│    Presentation / API         │  ← ASP.NET Core, Blazor
├───────────────────────────────┤
│    Infrastructure             │  ← EF Core, HTTP clients, files
├───────────────────────────────┤
│    Application (Use Cases)    │  ← CQRS Handlers, Services, DTOs
├───────────────────────────────┤
│    Domain                     │  ← Entities, Value Objects, Events
└───────────────────────────────┘
         ← All dependencies point inward
```

## Key Rules

1. **Domain** knows nothing about Infrastructure or Application
2. **Application** depends only on Domain interfaces (ports)
3. **Infrastructure** implements ports (adapters)
4. **Presentation** depends only on Application

## When to Use

| ✅ Good Fit            | ❌ Not a Good Fit     |
| ---------------------- | --------------------- |
| Complex business logic | Simple CRUD APIs      |
| Long-lived systems     | Prototypes / MVPs     |
| Large or growing teams | Solo projects         |
| Needs full testability | Time-boxed hackathons |

## .NET Project Structure

```
MyApp/
├── MyApp.Domain/          # Entities, Value Objects, Domain Events
│   ├── Orders/
│   │   ├── Order.cs       # Aggregate Root
│   │   ├── OrderItem.cs   # Entity
│   │   └── IOrderRepo.cs  # Port (interface)
├── MyApp.Application/     # Use cases, CQRS, DTOs
│   ├── Orders/
│   │   ├── CreateOrderCommand.cs
│   │   └── CreateOrderHandler.cs
├── MyApp.Infrastructure/  # EF Core, External APIs
│   ├── Persistence/
│   │   └── EfOrderRepo.cs # Implements IOrderRepo
└── MyApp.Api/             # ASP.NET Core Minimal API
```

## Key Patterns Paired with Clean Architecture

- **CQRS** — Separate read/write models in the Application layer
- **MediatR** — Decouple handlers from controllers
- **Domain Events** — Raise events on state changes, handle in Application
- **Repository Pattern** — Abstract persistence behind interfaces

## References

- [Microsoft: Clean architecture with ASP.NET Core](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
- [ardalis/CleanArchitecture](https://github.com/ardalis/CleanArchitecture)
