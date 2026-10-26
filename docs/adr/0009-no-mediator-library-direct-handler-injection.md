# 0009. Direct Handler Injection over In-Process Mediator Libraries

## Context
In modern .NET applications, in-process mediator libraries (such as MediatR) have frequently become default dependencies for CQRS and Clean Architecture. However, using a mediator for local in-process dispatch introduces unnecessary runtime reflection, makes static call graphs opaque, obscures dependency navigation in IDEs, complicates debugging, and often promotes anemic handlers that simply forward requests. Furthermore, MediatR licensing changes (v13+) and ecosystem fatigue make zero-mediator architectures increasingly attractive.

## Decision
I deliberately excluded all mediator libraries from this repository:
- Each use case is encapsulated in an explicit, sealed command or query handler class (e.g. `CreateOrderCommandHandler`, `SubmitOrderCommandHandler`, `GetOrderQueryHandler`).
- Each handler defines a typed `HandleAsync` method and is injected directly into ASP.NET Core Minimal API endpoints via `[FromServices]`.
- Endpoints remain thin routing adapters with zero business logic, delegating immediately to the injected handler.
- Cross-cutting concerns are handled via ASP.NET Core endpoint filters, middleware, and interceptors rather than in-process mediator pipeline behaviors.

## Consequences
- **Positive**: Zero mediator dependencies and zero license ambiguities.
- **Positive**: Transparent dependency graphs and direct, one-click IDE navigation from endpoint route to handler implementation.
- **Positive**: Zero reflection overhead during HTTP request dispatch.
- **Positive**: Strong typing and compile-time verification across all use cases.
