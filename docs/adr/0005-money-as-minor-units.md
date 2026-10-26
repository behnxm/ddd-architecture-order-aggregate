# 0005. Storing Monetary Amounts as Integer Minor Units

## Context
SQLite employs a dynamic type system without a dedicated fixed-point decimal type (unlike PostgreSQL `numeric` or SQL Server `decimal(18,2)`). When standard C# `decimal` values are mapped to SQLite, EF Core stores them as `TEXT` by default. While string storage preserves precision, it severely impairs SQL-level operations: `ORDER BY amount` sorts lexicographically rather than numerically (e.g. `"100.00"` sorts before `"20.00"`), and SQL aggregations (`SUM`, `AVG`) require casting.

## Decision
I modeled `Money` amounts in the database as integer minor units (e.g. cents, pence) using an EF Core `ValueConverter<decimal, long>`:
- The domain value object `Money` strictly enforces at most two decimal places and non-negative values.
- During persistence, the converter multiplies the decimal amount by 100: `(long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero)`.
- During hydration, the minor units are divided by 100: `minorUnits / 100m`.
- The column in SQLite is named `total_amount_minor_units` and `unit_price_minor_units` with type `INTEGER`.

## Consequences
- **Positive**: Exact, lossless representation of monetary amounts without IEEE 754 floating-point rounding errors.
- **Positive**: Native SQLite integer indexing, numerical sorting (`ORDER BY total_amount_minor_units`), and aggregate queries (`SUM(total_amount_minor_units)`) function correctly.
- **Positive**: Aligns with financial industry standards (Stripe, Adyen, and ISO 4217 minor unit representations).
