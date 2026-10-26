# 0004. Value Objects as Immutable Records and EF Core Mapping Decisions

## Context
In Domain-Driven Design, Value Objects represent descriptive concepts defined entirely by their attributes rather than identity. They must be immutable, self-validating, and compared by value. When mapping value objects to relational schemas in EF Core, teams historically relied on Owned Entity Types (`OwnsOne`), which created hidden primary keys, change tracker complexities, and navigation quirks.

## Decision
1. **Domain Modeling**: I implemented Value Objects (`Money`, `Address`, `Quantity`, `Currency`) as immutable C# records with private constructors and static `Result`-returning factory methods. To prevent external consumers from using C# `with` expressions to bypass domain invariant validation, I made the record copy constructors private.
2. **EF Core 10 Complex Properties for Root Value Objects**:
   - `Order.Total` is mapped as an EF Core Complex Property (`ComplexProperty(o => o.Total)`).
   - `Order.ShippingAddress` is mapped as an optional (nullable) Complex Property (`ComplexProperty(o => o.ShippingAddress)`). EF Core 10 supports nullable complex types containing required properties, flattening their columns directly into the `orders` table without shadow keys.
3. **Owned Entities (`OwnsMany` and Nested `OwnsOne`) for Child Collections**:
   - `Order.Lines` is mapped as an owned collection (`OwnsMany`, table `order_lines`).
   - For `UnitPrice` inside `OrderLine`: In EF Core 10, `OwnedNavigationBuilder` does not support `.ComplexProperty()` directly on owned entities in an `OwnsMany` collection. Therefore, I mapped `OrderLine.UnitPrice` using a nested `OwnsOne(l => l.UnitPrice)`. Both `Total` and `UnitPrice` store monetary amounts as integer minor units.

## Consequences
- **Positive**: Root-level value objects use pure complex types, eliminating synthetic surrogate keys.
- **Positive**: `Address` is correctly modeled as optional on the draft aggregate and hydrated as `null` when unassigned.
- **Positive**: Lines are encapsulated with `PropertyAccessMode.Field` and `AutoInclude()`, ensuring the aggregate root always hydrates whole.
- **Deviation Documented**: `ComplexProperty` was used on root entity value objects, while nested `OwnsOne` was used on owned collection entities due to EF Core 10 metadata API constraints on `OwnedNavigationBuilder`.
