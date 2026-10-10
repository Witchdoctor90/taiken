# tests

## Структура

```
tests/
├── Taiken.Testing                  спільна тестова інфраструктура (не містить тестів)
├── Taiken.<Name>.Tests            юніт/інтеграційні тести модуля <Name>
├── Taiken.ArchitectureTests        перевіряє правила залежностей з README кожної src-папки
├── Taiken.ContractTests            один набір тестів, який проходять і фейковий, і справжній
│                                   адаптер одного Kind (Integrations)
└── Taiken.E2E                      повний флоу через реальний host, з M7
```

`Taiken.ArchitectureTests`, `Taiken.ContractTests` і `Taiken.Testing` з'являються в M2; `Taiken.E2E` — в M7.

## Taiken.Testing

Централізована тестова інфраструктура, не окремий тестовий проєкт (без `[Fact]`). Зараз — абстрактний
`PostgreSqlFixture` (Testcontainers, реальний Postgres — CLAUDE.md забороняє InMemory/SQLite для цих
тестів). Проєкт модуля, якому потрібна БД, успадковує його й реалізує `CreateSchemaAsync()` своїм
`DbContext`:

```csharp
public sealed class MyModuleFixture : PostgreSqlFixture
{
    protected override async Task CreateSchemaAsync() { /* EnsureCreatedAsync свого DbContext */ }
}
```

Нове сюди потрапляє, коли реально повторилось у ≥2 тестових проєктах (як і сама фікстура,
централізована TAI-12 з `Taiken.Persistence.Tests` і `Taiken.Messaging.Tests`) — не наперед.

## Taiken.ArchitectureTests

Правила залежностей (TAI-5) перевіряються на графі `ProjectReference` із `.csproj`-файлів під
`src/`, зчитаних напряму з диска (`ProjectGraph`), а не на скомпільованих асемблі. Це означає, що
нові модулі, адаптери чи фейки підпадають під перевірку автоматично, щойно з'являється їхня папка й
проєкт — без реєстрації тут.

Виняток — перевірка видимості (`VisibilityTests`, ADR 0004 / CLAUDE.md "Видимість"): вона працює
через `NetArchTest` на реальних асемблі, тож новий модуль чи адаптер додає свій `Assembly` у
`VisibilityTests.ModuleAndAdapterAssemblies` вручну.

Список дозволених місць для `IgnoreQueryFilters()` (ADR 0002) — у
`IgnoreQueryFiltersAllowlistTests.Allowlist`. Використання поза списком валить CI; новий дозволений
виклик (резолвінг QR, webhook lookup, reconciler) додається туди явно, з посиланням на ADR.
