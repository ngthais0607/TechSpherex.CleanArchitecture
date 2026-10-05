# SnpDepotManagement

Container Depot Management System built with .NET 10 and Clean Architecture.

## Architecture Overview
- **Domain**: Core business entities, value objects, domain rules, and enums. Independent of external frameworks.
- **Application**: Application business rules, CQRS / Use Cases, FluentValidation.
- **Infrastructure**: Persistence (EF Core + PostgreSQL), Identity, JwtBearer, HybridCache.
- **Api**: ASP.NET Core Web API, OpenAPI, Scalar.
- **Tests**:
  - `Domain.UnitTests`: Unit tests for domain logic and business rules using xUnit.v3 and FluentAssertions.
  - `Application.UnitTests`: Unit tests for application handlers using EF InMemory and NSubstitute.
  - `Architecture.Tests`: Architectural rules verification using NetArchTest.Rules.
